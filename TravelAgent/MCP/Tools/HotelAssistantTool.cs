using System.ComponentModel;
using ModelContextProtocol.Server;
using TravelAgent.Models;
using TravelAgent.Services;

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
}