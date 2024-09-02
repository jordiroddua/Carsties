using System;

namespace BiddingService.DTOs;

public class BidDto
{
    public string Id { get; set; } // automapper will do the transformation from ID to Id
    public string AuctionId { get; set; }
    public string Bidder { get; set; }
    public DateTime BidTime { get; set; }
    public int Amount { get; set; }
    public string BidStatus { get; set; } // automapper will do the transformation from integer to string
}
