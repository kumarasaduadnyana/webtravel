using TravelAgent.Models;

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
            string currency);
    }
}
