namespace TravelAgent.Models
{
    public class Hotel
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Location { get; set; }
        public double? Rating { get; set; }
        public double? StarRating { get; set; }
        public double? GuestRating { get; set; }
        public int? GuestRatingCount { get; set; }
        public double? Price { get; set; }
        public double? SupplierPrice { get; set; }
        public double? StrikeThroughPrice { get; set; }
        public string? Currency { get; set; }
        public string? ImageUrl { get; set; }
        public List<string>? Amenities { get; set; }
        public List<string>? Images { get; set; }
        public string? HotelCode { get; set; }
        public string? Provider { get; set; }
    }
}
