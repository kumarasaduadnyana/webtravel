using ModelContextProtocol.Server;
using System.ComponentModel;
using TravelAgent.Models;
using TravelAgent.Services;
using HotelDetail = TravelAgent.Models.HotelDetail;

namespace TravelAgent.MCP.Tools;

[McpServerToolType]
public class HotelAssistantTool(IHotelService hotelService)
{
    [McpServerTool(Name = "search_hotel", Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Search for hotels in a specific destination with given criteria")]
    [McpMeta("ui", JsonValue = """{"resourceUri": "ui://widgets/list-hotel.html"}""")]
    [McpMeta("ui/resourceUri", "ui://widgets/list-hotel.html")]
    public async Task<SearchHotelResponse> SearchHotel(
        [Description("Destination city or region")] string destination,
        [Description("Check-in date (yyyy-MM-dd)")] DateTime checkIn,
        [Description("Check-out date (yyyy-MM-dd)")] DateTime checkOut,
        [Description("Number of adults")] int adults = 2,
        [Description("Number of rooms")] int rooms = 1,
        [Description("Currency code (e.g. USD, EUR, AUD)")] string currency = "AUD",
        [Description("Sort criteria (e.g. price, rating)")] string? sortBy = null,
        [Description("List of star ratings (1 to 5) to filter by")] List<int>? ratings = null,
        [Description("Maximum price per night")] double? maxPrice = null,
        [Description("Minimum guest rating score")] int? minRating = 1,
        [Description("List of amenities to filter by")] List<string>? amenities = null)
    {
        var hotels = await hotelService.SearchHotel(destination, checkIn, checkOut, adults, rooms, currency, sortBy, ratings, maxPrice, minRating, amenities);
        
        return new SearchHotelResponse
        {
            Hotels = hotels,
            Meta = new MetaData()
            {
                Destination = destination,
                CheckIn = checkIn,
                CheckOut = checkOut,
                Currency = currency,
                SortBy = sortBy,
                Ratings = ratings,
                MaxPrice = maxPrice,
                MinRating = minRating,
                Amenities = amenities
            }
        };
    }

    // TODO create tools for search detail page
    [McpServerTool(Name = "get_hotels_detail", Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Search for detail hotel")]
    [McpMeta("ui", JsonValue = """{"resourceUri": "ui://hotel-detail-widget.html"}""")]
    [McpMeta("ui/resourceUri", "ui://hotel-detail-widget.html")]
    public async Task<HotelDetail> SearchDetailHotel(
        [Description("Hotel Id")] string hotel_id,
        [Description("Hotel Code")] string? hotel_code,
        [Description("Provider")] string? provider,
        [Description("Check In")] DateTime check_in,
        [Description("Check Out")] DateTime check_out,
        [Description("Adult")] int adults = 2,
        [Description("Room")] int rooms = 1,
        [Description("Default Currency")] string currency = "AUD"
        )
    {
        var searchHotelDetail = await hotelService.GetHotelDetails(hotel_id, hotel_code, provider, check_in, check_out, adults, rooms, currency);

        return new HotelDetail
        {
            Address = searchHotelDetail?.Address ?? string.Empty,
            Amenities = searchHotelDetail?.Amenities,
            Currency = searchHotelDetail?.Currency ?? string.Empty,
            Description = searchHotelDetail?.Description ?? string.Empty,
            Id = searchHotelDetail?.Id,
            Images = searchHotelDetail?.Images,
            Latitude = searchHotelDetail?.Latitude,
            Longitude = searchHotelDetail?.Longitude,
            Name = searchHotelDetail?.Name,
            Price = searchHotelDetail?.Price,
            RoomRates = searchHotelDetail.RoomRates,
            StarRating = searchHotelDetail.StarRating
        };
    }
}