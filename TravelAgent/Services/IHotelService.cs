using TravelAgent.Models;
using HotelDetailModel = TravelAgent.Models.HotelDetail;

namespace TravelAgent.Services
{
    public interface IHotelService
    {
        Task<List<Hotel>> SearchHotel(
            string destination,
            DateTime checkIn,
            DateTime checkOut,
            int adults,
            int rooms,
            string currency,
            string? sortBy = null,
            int[]? starRatings = null,
            double? maxPrice = null,
            int? minRating = null,
            string[]? amenities = null);

        Task<HotelDetailModel?> GetHotelDetails(
            string hotelId,
            string? hotelCode,
            string? provider,
            DateTime checkIn,
            DateTime checkOut,
            int adults,
            int rooms,
            string currency);
    }
}
