using AuctionService.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Data;

public class AuctionDbContext : DbContext
{
    public AuctionDbContext(DbContextOptions options) : base(options)
    {
    }

    // create Auctions table. The Items table is created autmatically because Auctions and Items are related
    public DbSet<Auction> Auctions { get; set; }
}
