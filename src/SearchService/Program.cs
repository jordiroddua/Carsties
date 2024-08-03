using System.Net;
using MassTransit;
using MongoDB.Driver;
using MongoDB.Entities;
using Polly;
using Polly.Extensions.Http;
using SearchService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddHttpClient<AuctionSvcHttpClient>().AddPolicyHandler(GetPolicy());
builder.Services.AddMassTransit(x => {
    // register consumers
    x.AddConsumersFromNamespaceContaining<AuctionCreatedConsumer>();
    x.AddConsumersFromNamespaceContaining<AuctionUpdatedConsumer>();

    // avoid consumer class name collisions. Queues will have the search prefix
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("search", false));

    // config mass transit to use rbmq
    // add retry policy to retreive lost messages while DB was down
    x.UsingRabbitMq((context, cfg) => 
    {
        cfg.ReceiveEndpoint("search-auction-created", e => {
            e.UseMessageRetry(r => r.Interval(5, 5));
            e.ConfigureConsumer<AuctionCreatedConsumer>(context);
        });

        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

app.UseAuthorization();

app.MapControllers();

// be able to perform requests even if the Auctions service is down
app.Lifetime.ApplicationStarted.Register(async () =>
{
    try
    {
        await DbInitializer.InitBd(app);
    }
    catch (Exception e)
    {
        
        Console.WriteLine(e);
    }
});


app.Run();

// retry failed http request on connection faillure, not found, etc every 3s until success
 static IAsyncPolicy<HttpResponseMessage> GetPolicy() 
    => HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound)
        .WaitAndRetryForeverAsync(_ =>TimeSpan.FromSeconds(3));