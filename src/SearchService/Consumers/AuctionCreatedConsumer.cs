using AutoMapper;
using Contracts;
using MassTransit;
using MongoDB.Entities;

namespace SearchService;

public class AuctionCreatedConsumer : IConsumer<AuctionCreated>
{
    public IMapper _mapper { get; }
    public AuctionCreatedConsumer(IMapper mapper)
    {
        _mapper = mapper;
    }


    public async Task Consume(ConsumeContext<AuctionCreated> context)
    {
        Console.WriteLine("--> Consuming AuctionCreated: " + context.Message.Id);

        var item = _mapper.Map<Item>(context.Message);
        
        // force exception so we can apply a RBMQ fix

        if(item.Model == "Foo") throw new ArgumentException("cannot save cars of model foo");

        await item.SaveAsync();
    }
}
