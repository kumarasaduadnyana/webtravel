using ModelContextProtocol.Server;
using System.ComponentModel;
using TravelAgent.MCP.Prompts;
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
        [Description("Check-in date (yyyy-MM-dd)")] DateTime checkIn,
        [Description("Check-out date (yyyy-MM-dd)")] DateTime checkOut,
        [Description("Number of adults")] int adults = 2,
        [Description("Number of children or infants")] int children = 0,
        [Description("Number of rooms")] int rooms = 1,
        [Description("Destination city or region")] string destination = "BALI",
        [Description("Currency code (e.g. USD, EUR, AUD)")] string currency = "AUD",
        [Description("Number of hotels to show the user. Use when user says 'show me 3', 'top 5', 'only 10', etc. Min 1, max 40, default 40")] int count = 40,
        [Description("Sort order: 'price_asc' (cheapest first), 'price_desc' (most expensive first), 'rating_desc' (best rated first)")] string? sortBy = null,
        [Description("List of star ratings (1 to 5) to filter by")] List<int>? ratings = null,
        [Description("Minimum price per night")] double? minPrice = null,
        [Description("Maximum price per night")] double? maxPrice = null,
        [Description("Minimum guest rating score")] int? minRating = 1,
        [Description("List of amenities to filter by")] List<string>? amenities = null)
    {
        var hotels = await hotelService.SearchHotel(destination, checkIn, checkOut, adults, children, rooms, currency, sortBy, ratings, minPrice, maxPrice, minRating, amenities);

        var hotelFilterCount = hotels.Take(count).ToList();
        
        return new SearchHotelResponse
        {
            Hotels = hotelFilterCount,
            Meta = new MetaData()
            {
                Destination = destination,
                CheckIn = checkIn,
                CheckOut = checkOut,
                Currency = currency,
                SortBy = sortBy ?? string.Empty,
                Ratings = ratings,
                MaxPrice = maxPrice,
                MinRating = minRating,
                Amenities = amenities ?? new List<string>()
            },
            Instruction = HotelAssistantPrompt.SearchInstruction
        };
    }

    [McpServerTool(Name = "get_hotels_detail", Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Get full details for spesific hotel including room rates, cancellation")]
    //[McpMeta("ui", JsonValue = """{"resourceUri": "ui://widgets/hotel-detail-widget.html"}""")]
    //[McpMeta("ui/resourceUri", "ui://hotel-detail-widget.html")]
    public async Task<HotelDetail> SearchDetailHotel(
        [Description("Hotel ID returned from search result")] string hotel_id,
        [Description("Provider spesific hotel code returned from search_hotel")] string? hotel_code,
        [Description("Accommodation provider name")] string? provider,
        [Description("Check In Date")] DateTime check_in,
        [Description("Check Out Date")] DateTime check_out,
        [Description("Number of adult guests")] int adults = 2,
        [Description("Number of rooms")] int rooms = 1,
        [Description("Currency Code")] string currency = "AUD"
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
            RoomRates = searchHotelDetail?.RoomRates,
            StarRating = searchHotelDetail?.StarRating,
            Instruction = HotelAssistantPrompt.DetailInstruction
        };
    }
}