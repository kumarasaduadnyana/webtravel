using TravelAgent.Models;

namespace TravelAgent.Services
{
    public interface IHotelService
    {
        Task<List<Hotel>> SearchHotel(string input);
    }
}
