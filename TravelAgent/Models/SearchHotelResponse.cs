namespace TravelAgent.Models;

public class SearchHotelResponse
{
    public List<Hotel> Hotels { get; set; }
    public MetaData Meta { get; set; }
    public string? Instruction { get; set; }
}

public class HotelDetailResponse
{
    public HotelDetail? Hotel { get; set; }
    public string? Instruction { get; set; }
}

public class MetaData
{
    public string Destination { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public string Currency { get; set; }
    public int Adults { get; set; }
    public int Children { get; set; }
    public int Rooms { get; set; }
    //public decimal Count { get; set; }
    public string SortBy { get; set; }
    public List<int>? Ratings { get; set; }
    public double? MinPrice { get; set; }
    public double? MaxPrice { get; set; }
    public double? MinRating { get; set; }
    public List<string> Amenities { get; set; }
}