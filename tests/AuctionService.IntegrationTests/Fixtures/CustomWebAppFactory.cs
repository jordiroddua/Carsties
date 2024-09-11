using System;
using AuctionService.Data;
using AuctionService.IntegrationTests.Util;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using WebMotions.Fake.Authentication.JwtBearer;

namespace AuctionService.IntegrationTests.Fixtures;

public class CustomWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder().Build();

    // start and init when running the tests
    // dispose it after tests finished
    public async Task InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // remove the AuctionDbContext and add the test AuctionDbContext with the test conatiner _postgreSqlContainer
        // in the program class
        builder.ConfigureTestServices(services =>
        {
            services.RemoveDbContext<AuctionDbContext>();

            services.AddDbContext<AuctionDbContext>(options => 
            {
                options.UseNpgsql(_postgreSqlContainer.GetConnectionString());
            });

            // replace mass transit configuration for a test one in the program class
            services.AddMassTransitTestHarness();

            // migrate db schema and init db with clean data
            services.EnsureCreated<AuctionDbContext>();

            services.AddAuthentication(FakeJwtBearerDefaults.AuthenticationScheme)
                .AddFakeJwtBearer(opt =>
                {
                    opt.BearerValueType = FakeJwtBearerBearerValueType.Jwt;
                });
        });
    }

    Task IAsyncLifetime.DisposeAsync() => _postgreSqlContainer.DisposeAsync().AsTask();
}
