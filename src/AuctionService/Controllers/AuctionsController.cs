﻿using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Controllers;

[ApiController] //validations anb query properties binding
[Route("api/auctions")]
public class AuctionsController : ControllerBase
{
    private readonly IAuctionRepository _repo;
    private readonly IMapper _mapper;
    private readonly IPublishEndpoint _publishEndpoint;

    public AuctionsController(IAuctionRepository repo, IMapper mapper, IPublishEndpoint publishEndpoint)
    {
        _repo = repo;
        _mapper = mapper;
        _publishEndpoint = publishEndpoint;
    }

    [HttpGet]
    public async Task<ActionResult<List<AuctionDto>>> GetAllAuctions(string date)
    {
        return await _repo.GetAuctionsAsync(date);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AuctionDto>> GetAuctionById(Guid id)
    {
        var auction = await _repo.GetAuctionByIdDtoAsync(id);

        if(auction == null) return NotFound();

        return auction;
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<AuctionDto>> CreateAuction(CreateAuctionDto auctionDto)
    {
        Console.WriteLine("-----> POST Auction");
        var auction = _mapper.Map<Auction>(auctionDto);
        
        auction.Seller = User.Identity.Name;

        _repo.AddAuction(auction);

        var newAuction = _mapper.Map<AuctionDto>(auction);

        // publish message with massTransit outbox functionality
        // with outbox: if service bus is down the transaction will fail so it won't be stored in the DB until service bus recovers
        // with outbox: if service bus is up but postgres db is down it will store the messages until DB is up again and re-deliver the outbox messages
        await _publishEndpoint.Publish(_mapper.Map<AuctionCreated>(newAuction));

        var result = await _repo.SaveChangesAsync();

        if(!result) return BadRequest("Could not save changes");

        //returns a 201 and sets in the Location Header: http://localhost:7001/api/auctions/aa844ecc-44fc-4d1d-a475-cdee62fdd38d
        return CreatedAtAction(nameof(GetAuctionById), 
            new {auction.Id}, newAuction);
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateAuction(Guid id, UpdateAuctionDto updateAuctionDto)
    {
        // try to find auction in db by Id
        var auction = await _repo.GetAuctionEntityById(id);
        
        if(auction == null) return NotFound();

        if(auction.Seller != User.Identity.Name) return Forbid();

        auction.Item.Make = updateAuctionDto.Make ?? auction.Item.Make;
        auction.Item.Model = updateAuctionDto.Model ?? auction.Item.Model;
        auction.Item.Color = updateAuctionDto.Color ?? auction.Item.Color;
        auction.Item.Mileage = updateAuctionDto.Mileage ?? auction.Item.Mileage;
        auction.Item.Year = updateAuctionDto.Year ?? auction.Item.Year;

        var updatedAuction = _mapper.Map<AuctionUpdated>(auction);

        await _publishEndpoint.Publish(updatedAuction);

        var result = await _repo.SaveChangesAsync();

        if(!result) return BadRequest("Could not save changes");

        return Ok();
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAuction(Guid id)
    {
        var auction = await _repo.GetAuctionEntityById(id);

        if(auction == null) return NotFound();

        if(auction.Seller != User.Identity.Name) return Forbid();

        _repo.RemoveAuction(auction);

        // await _publishEndpoint.Publish(_mapper.Map<AuctionDeleted>(auction));
        await _publishEndpoint.Publish<AuctionDeleted>(new { Id = auction.Id.ToString() });

        var result = await _repo.SaveChangesAsync();

        if(!result) return BadRequest("Could not save changes");

        return Ok();
    }
}
