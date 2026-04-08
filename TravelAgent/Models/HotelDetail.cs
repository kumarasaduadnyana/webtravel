namespace TravelAgent.Models
{
    public class HotelDetail
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public double? StarRating { get; set; }
        public string? Description { get; set; }
        public List<string>? Images { get; set; }
        public List<string>? Amenities { get; set; }
        public string? Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Currency { get; set; }
        public double? Price { get; set; }
        public List<RoomRateInfo>? RoomRates { get; set; }
        public string Instruction { get; set; }
    }

    public class RoomRateInfo
    {
        public string? RoomName { get; set; }
        public string? RoomType { get; set; }
        public string? ImageUrl { get; set; }
        public string? BedConfiguration { get; set; }
        public string? MealsDescription { get; set; }
        public bool Refundable { get; set; }
        public DateTime? FreeCancellationUntil { get; set; }
        public double? Price { get; set; }
        public double? StrikeThroughPrice { get; set; }
        public string? Currency { get; set; }
    }
}
