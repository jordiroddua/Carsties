using System;
using System.Net;
using System.Net.Http.Json;
using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.IntegrationTests.Fixtures;
using AuctionService.IntegrationTests.Util;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionService.IntegrationTests;

[Collection("Shared collection")]
public class AuctionControllerTests : IAsyncLifetime
{
    private readonly CustomWebAppFactory _factory;
    private readonly HttpClient _httpClient;
    private const string FORD_GT_ID = "afbee524-5972-4075-8800-7d1f9d7b0a0c";

    public AuctionControllerTests(CustomWebAppFactory factory)
    {
        _factory = factory;
        _httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task GetAuctions__ShouldReturn3Auctions()
    {
        // arrange
        // act
        var response = await _httpClient.GetFromJsonAsync<List<AuctionDto>>("/api/auctions");

        // assert
        Assert.Equal(3, response.Count);
    }

    [Fact]
    public async Task GetAuctionById__WithValidId__ShouldReturnAuction()
    {
        // arrange
        // act
        var response = await _httpClient.GetFromJsonAsync<AuctionDto>($"api/auctions/{FORD_GT_ID}");

        // assert
        Assert.Equal("GT", response.Model);
    }

    [Fact]
    public async Task GetAuctionById__WithInvalidId__ShouldReturn404()
    {
        // arrange
        // act
        var response = await _httpClient.GetAsync($"api/auctions/{Guid.NewGuid()}");

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAuctionById__WithInvalidGuid__ShouldReturn400()
    {
        // arrange
        // act
        var response = await _httpClient.GetAsync($"api/auctions/notaguid");

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateAuction__WithNoaAuth__ShouldReturn401()
    {
        // arrange
        var auction = new CreateAuctionDto{Make = "test"};

        // act
        var response = await _httpClient.PostAsJsonAsync($"api/auctions", auction);

        // assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateAuction__WithAuth__ShouldReturn201()
    {
        // arrange
        var auction = GetAuctionForCreate();
        _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));

        // act
        var response = await _httpClient.PostAsJsonAsync($"api/auctions", auction);

        // assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdAuction = await response.Content.ReadFromJsonAsync<AuctionDto>();
        Assert.Equal("bob", createdAuction.Seller);
    }

    [Fact]
    public async Task CreateAuction__WithInvalidCreateAuctionDto__ShouldReturn400()
    {
        // arrange
        var auction = new CreateAuctionDto{Make = "test"};
        _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));

        // act
        var response = await _httpClient.PostAsJsonAsync($"api/auctions", auction);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAuction__WithValidUpdateAuctionDtoAndUser__ShouldReturn200()
    {
        // arrange
        var auction = GetAuctionForUpdate();
        _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));

        // act
        var response = await _httpClient.PutAsJsonAsync($"api/auctions/{FORD_GT_ID}", auction);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAuction__WithValidUpdateAuctionDtoAndInvalidUser__ShouldReturn403()
    {
        // arrange
        var auction = GetAuctionForUpdate();
        _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("alice"));

        // act
        var response = await _httpClient.PutAsJsonAsync($"api/auctions/{FORD_GT_ID}", auction);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // init of db before each test not necessary
    // factory takes care of project init
    public Task InitializeAsync() => Task.CompletedTask; 
    
    // reinit the db after each test
    public Task DisposeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
        DbHelper.ReinitDbForTests(db);
        return Task.CompletedTask;
    }

    private CreateAuctionDto GetAuctionForCreate()
    {
        return new CreateAuctionDto
        {
            Make = "test",
            Model = "model",
            ImageUrl = "imageUrl",
            Color = "tets",
            Mileage = 10,
            Year = 10,
            ReservePrice = 10
        };
    }

    private UpdateAuctionDto GetAuctionForUpdate()
    {
        return new UpdateAuctionDto
        {
            Make = "update-test",
            Model = "update-model",
            Year = 100,
            Color = "update-tets",
            Mileage = 100,
            ImageUrl = "update-imageUrl"
        };
    }
}
