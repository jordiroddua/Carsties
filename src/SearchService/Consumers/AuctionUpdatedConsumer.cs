using AutoMapper;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Http.HttpResults;
using MongoDB.Entities;

namespace SearchService;

public class AuctionUpdatedConsumer : IConsumer<AuctionUpdated>
{
    private readonly IMapper _mapper;

    public AuctionUpdatedConsumer(IMapper mapper)
    {
        _mapper = mapper;
    }
    public async Task Consume(ConsumeContext<AuctionUpdated> context)
    {
        Console.WriteLine("--> Consuming AuctionUpdated: " + context.Message.Id + " Model: " + context.Message.Model);

        var item = _mapper.Map<Item>(context.Message);

        var result = await DB.Update<Item>()
            .Match(a => a.ID == context.Message.Id)
            .ModifyOnly(e => new {
                e.Color,
                e.Make,
                e.Model,
                e.Year,
                e.Mileage
            }, item)
            .ExecuteAsync();

        if(!result.IsAcknowledged) {
            throw new MessageException(typeof(AuctionUpdated), "Problem updating mongo");
        }
    }
}
