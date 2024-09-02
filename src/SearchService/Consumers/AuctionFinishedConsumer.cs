using Contracts;
using MassTransit;
using MongoDB.Entities;

namespace SearchService;

public class AuctionFinishedConsumer : IConsumer<AuctionFinished>
{
    public async Task Consume(ConsumeContext<AuctionFinished> context)
    {
        Console.WriteLine("--> Consuming auction finished");

        var auction = await DB.Find<Item>().OneAsync(context.Message.AuctionId)
            ?? throw new MessageException(typeof(AuctionFinished), "Cannot retrieve this auction");
        
        if (context.Message.ItemSold)
        {
            auction.Winner = context.Message?.Winner;
            auction.SoldAmount = context.Message?.Amount;
        }

        auction.Status = "Finished";

        await auction.SaveAsync();
    }
}
