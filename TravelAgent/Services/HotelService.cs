using TravelAgent.Models;

namespace TravelAgent.Services
{
    public class HotelService : IHotelService
    {
        private static readonly List<Hotel> _hotels = new()
        {
            new Hotel { Id = "1", Name = "Grand Palace Hotel", Location = "Paris", Rating = 4.8, Price = 350, ImageUrl = "https://example.com/images/grand-palace.jpg" },
            new Hotel { Id = "2", Name = "Eiffel View Inn", Location = "Paris", Rating = 4.5, Price = 220, ImageUrl = "https://example.com/images/eiffel-view.jpg" },
            new Hotel { Id = "3", Name = "Times Square Suites", Location = "New York", Rating = 4.6, Price = 280, ImageUrl = "https://example.com/images/times-square.jpg" },
            new Hotel { Id = "4", Name = "Manhattan Boutique", Location = "New York", Rating = 4.3, Price = 195, ImageUrl = "https://example.com/images/manhattan.jpg" },
            new Hotel { Id = "5", Name = "Tokyo Skyline Hotel", Location = "Tokyo", Rating = 4.7, Price = 310, ImageUrl = "https://example.com/images/tokyo-skyline.jpg" },
            new Hotel { Id = "6", Name = "Shibuya Central Inn", Location = "Tokyo", Rating = 4.4, Price = 175, ImageUrl = "https://example.com/images/shibuya.jpg" },
            new Hotel { Id = "7", Name = "Bali Beach Resort", Location = "Bali", Rating = 4.9, Price = 420, ImageUrl = "https://example.com/images/bali-beach.jpg" },
            new Hotel { Id = "8", Name = "London Bridge Hotel", Location = "London", Rating = 4.5, Price = 265, ImageUrl = "https://example.com/images/london-bridge.jpg" },
        };

        public async Task<List<Hotel>> SearchHotel(string input)
        {
            await Task.CompletedTask;
            return _hotels
                .Where(h => h.Location != null && h.Location.Contains(input, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }
}
