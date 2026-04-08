using TravelAgent.Services.CarRental.CarRentalClient;

namespace TravelAgent.Models.CarRental;

public class CarAvailability
{
    public string SearchId { get; set; }
    public int TotalData { get; set; }
    public int TotalPage { get; set; }
    public int PageSize { get; set; }
    public List<Vehicle?> Vehicles { get; set; } = [];
    public DataGroupingDto Grouping { get; set; }
}

public class Vehicle
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Image { get; set; }
    public string Currency { get; set; }
    public double Price { get; set; }
    public double PricePerDay { get; set; }
    public Vendor? Vendor { get; set; }
    public LocationInfo? PickupInformation { get; set; }
    public LocationInfo? DropOffInformation { get; set; }
    public string Transmission { get; set; }
    public string Type { get; set; }
    public string Size { get; set; }
    public string Doors { get; set; }
    public string Fuel { get; set; }
}

public class Vendor
{
    public string Name { get; set; }
    public string Logo { get; set; }
    public string Code { get; set; }
}

public class LocationInfo
{
    public string Name { get; set; }
    public string CounterLocation { get; set; }
    public string Latitude { get; set; }
    public string Longitude { get; set; }
}