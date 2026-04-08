using System.ComponentModel;
using ModelContextProtocol.Server;
using TravelAgent.Models.CarRental;
using TravelAgent.Services.CarRental.CarRentalClient;

namespace TravelAgent.MCP.Tools;

[McpServerToolType]
public class CarRentalAssistantTool(Client carRentalClient)
{
    [McpServerTool(Name = "get_car_rental_destinations", Idempotent = true, OpenWorld = false)]
    [Description("Search for car rental destinations (cities or airports) by keyword.")]
    public async Task<List<DestinationDto>> GetCarRentalDestinationsAsync(
        [Description("The keyword to search for (e.g., 'Bali', 'JFK').")] string keyword)
    {
        var destination = await carRentalClient.SearchDestinationAsync(keyword: keyword, x_Tenant_Code: "10travlr", null);
        return destination.ToList();
    }

    [McpServerTool(Name = "get_car_availability", Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Search for available car rentals based on location, dates, and optional filters like vehicle category, size, or transmission.")]
    [McpMeta("ui", JsonValue = """{"resourceUri": "ui://widgets/list-vehicle.html"}""")]
    [McpMeta("ui/resourceUri", "ui://widgets/list-vehicle.html")]
    public async Task<CarAvailability> GetCarAvailabilityAsync(
        [Description("The unique numeric id for the pickup location (obtained from the car-rental destination resource). DO NOT use the 'code' field (e.g., use '626', not 'JFK').")] string pickupLocationId,
        [Description("The type of pickup location (City or Airport, obtained from the car-rental destination resource).")] string pickupLocationType,
        [Description("The unique numeric id for the drop-off location (obtained from the car-rental destination resource). DO NOT use the 'code' field (e.g., use '626', not 'JFK').")] string dropOffLocationId,
        [Description("The type of drop-off location (City or Airport, obtained from the car-rental destination resource).")] string dropOffLocationType,
        [Description("Pickup date and time in ISO 8601 format (e.g., 2023-10-27T10:00:00Z).")] string pickupDateTime,
        [Description("Drop-off date and time in ISO 8601 format (e.g., 2023-10-30T10:00:00Z).")] string dropOffDateTime,
        [Description("The age of the driver.")] int driverAge,
        [Description("Currency for prices (e.g., USD, IDR).")] string currency = "USD",
        [Description("Optional search session identifier.")] string? searchId = null,
        [Description("Filter by vehicle categories (e.g., SUV, Sedan).")] List<string>? categories = null,
        [Description("Filter by vehicle sizes (e.g., Compact, Full-size).")] List<string>? sizes = null,
        [Description("Filter by transmission types (e.g., Automatic, Manual).")] List<string>? transmissions = null,
        [Description("Filter by car rental vendors.")] List<string>? vendors = null,
        [Description("Minimum price for the rental.")] double? minPrice = null,
        [Description("Maximum price for the rental.")] double? maxPrice = null,
        [Description("The number of results per page.")] int pageSize = 20,
        [Description("The page number to retrieve.")] int pageNumber = 1)
    {
        if (!DateTimeOffset.TryParse(pickupDateTime, out var pickup))
        {
            throw new ArgumentException($"Invalid pickup date format: {pickupDateTime}. Please use ISO 8601 format.");
        }

        if (!DateTimeOffset.TryParse(dropOffDateTime, out var dropOff))
        {
            throw new ArgumentException($"Invalid drop-off date format: {dropOffDateTime}. Please use ISO 8601 format.");
        }

        if (!Enum.TryParse<SearchRequestPickupDestinationType>(pickupLocationType, true, out var pType))
        {
            throw new ArgumentException($"Invalid pickup location type: {pickupLocationType}. Please use 'City' or 'Airport'.");
        }

        if (!Enum.TryParse<SearchRequestDropOffDestinationType>(dropOffLocationType, true, out var dType))
        {
            throw new ArgumentException($"Invalid drop-off location type: {dropOffLocationType}. Please use 'City' or 'Airport'.");
        }

        var filter = new FilterRequest
        {
            MinAmount = minPrice,
            MaxAmount = maxPrice,
            Transmission = transmissions,
            Vendor = vendors,
            Category = categories,
            Size = sizes
        };
        
        var request = new SearchRequest
        {
            Currency = currency,
            SearchId = searchId,
            PickupDateTime = pickup.DateTime,
            DropOffDateTime = dropOff.DateTime,
            DriverAge = driverAge,
            PickupDestinationType = pType,
            PickupLocationId = pickupLocationId,
            DropOffDestinationType = dType,
            DropOffLocationId = dropOffLocationId,
            PageSize = pageSize,
        };

        var availabilityResult = await carRentalClient.SearchAvailabilityAsync("10travlr", null, request); 
        
        var carAvailability = availabilityResult.MapToCarAvailability();

        return carAvailability;
    }
}