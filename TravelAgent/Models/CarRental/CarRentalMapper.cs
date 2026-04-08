using TravelAgent.Services.CarRental.CarRentalClient;

namespace TravelAgent.Models.CarRental;

public static class CarRentalMapper
{
    public static CarAvailability? MapToCarAvailability(this SearchAvailabilityDto? dto)
    {
        if (dto == null) return null;

        return new CarAvailability
        {
            SearchId = dto.SearchId,
            TotalData = dto.TotalData,
            TotalPage = dto.TotalPage,
            PageSize = dto.PageSize,
            Vehicles = dto.Vehicles?.Select(v => v.MapToVehicle()).ToList() ?? [],
            Grouping = dto.Grouping
        };
    }

    public static Vehicle? MapToVehicle(this VehicleDto? dto)
    {
        if (dto == null) return null;

        return new Vehicle
        {
            Id = dto.Id,
            Name = dto.Name,
            Image = dto.Image,
            Currency = dto.Currency,
            Price = dto.SellPrice,
            PricePerDay = dto.PricePerDay,
            Vendor = dto.Vendor.MapToVendor(),
            PickupInformation = dto.PickupInformation.MapToLocationInfo(),
            DropOffInformation = dto.DropOffInformation.MapToLocationInfo(),
            Transmission = dto.Spesification.Transmission,
            Type = dto.Category.Name,
            Doors = dto.Spesification.Doors.ToString(),
            Fuel = dto.Spesification.IsElectric ? "Electric" : dto.Spesification.FuelType,
            Size = dto.VehicleSize.Name
        };
    }

    public static Vendor? MapToVendor(this VendorDto? dto)
    {
        if (dto == null) return null;

        return new Vendor
        {
            Name = dto.Name,
            Logo = dto.Logo,
            Code = dto.Code
        };
    }

    public static LocationInfo? MapToLocationInfo(this PickupOrDropOffInformationDto? dto)
    {
        if (dto == null) return null;

        return new LocationInfo
        {
            Name = dto.Name,
            CounterLocation = dto.CounterLocation,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude
        };
    }
}
