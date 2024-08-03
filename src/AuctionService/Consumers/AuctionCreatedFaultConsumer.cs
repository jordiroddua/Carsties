using Contracts;
using MassTransit;

namespace AuctionService.Consumers;

public class AuctionCreatedFaultConsumer : IConsumer<Fault<AuctionCreated>>
{
    public async Task Consume(ConsumeContext<Fault<AuctionCreated>> context)
    {
        Console.WriteLine("--> Consuming faulty creation");

        var exception = context.Message.Exceptions.First();
        
        // catch a System.ArgumentException where the car model was set to Foo and change it to a valid car model.
        // send the modified message back to the service Bus
        if(exception.ExceptionType == "System.ArgumentException")
        {
            context.Message.Message.Model = "FooBar";
            await context.Publish(context.Message.Message);
        } 
        else
        {
            Console.WriteLine("Not an argument exception");   
        }

    }
}
