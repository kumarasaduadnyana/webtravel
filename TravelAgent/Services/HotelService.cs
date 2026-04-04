using Travlr.Search.Client;
using TravelAgent.Models;

namespace TravelAgent.Services
{
    public class HotelService : IHotelService
    {
        private readonly ITravlrSearchApiClient _searchClient;
        private readonly ILogger<HotelService> _logger;

        public HotelService(ITravlrSearchApiClient searchClient, ILogger<HotelService> logger)
        {
            _searchClient = searchClient;
            _logger = logger;
        }

        public async Task<List<Hotel>> SearchHotel(
            string destination,
            DateTime checkIn,
            DateTime checkOut,
            int adults,
            int rooms,
            string currency)
        {
            // Step 1: Resolve free-text destination to a canonical full name
            string destinationFullName = destination;
            try
            {
                _logger.LogInformation("Resolving destination: '{Input}'", destination);

                var destResponse = await _searchClient.AccommodationDestinationSearchAsync(
                    new DestinationSearchRequestViewModel
                    {
                        Page  = new SearchRequestViewModel_Page { Size = 5, Current = 1 },
                        Query = destination
                    });

                var resolved = destResponse?.Result?.FirstOrDefault()?.FullName;
                _logger.LogInformation("Destination results count: {Count}", destResponse?.Result?.Count ?? 0);

                if (!string.IsNullOrWhiteSpace(resolved))
                {
                    destinationFullName = resolved;
                    _logger.LogInformation("Resolved to: '{FullName}'", destinationFullName);
                }
            }
            catch (TravlrSearchApiClientException ex)
            {
                _logger.LogWarning("Destination resolution failed — status {Status}, response: [{Response}] — using raw input",
                    ex.StatusCode, ex.Response);
            }

            // Step 2: Search accommodations — try with availability, fall back without
            try
            {
                _logger.LogInformation("Searching hotels for '{Dest}', {CheckIn} → {CheckOut}, {Adults} adults, {Rooms} rooms, {Currency}",
                    destinationFullName, checkIn.ToString("yyyy-MM-dd"), checkOut.ToString("yyyy-MM-dd"), adults, rooms, currency);

                var searchResponse = await _searchClient.AccommodationSearchByDestinationAsync(
                    new AccommodationSearchByDestinationRequestViewModel
                    {
                        Page                = new SearchRequestViewModel_Page { Size = 40, Current = 1 },
                        DestinationFullName = destinationFullName,
                        SessionId           = Guid.NewGuid().ToString("N")[..20],
                        GetComparisonPrice  = true,
                        IncludeFacets       = true,
                        Tags                = new[] { "webapp_search" },
                        Filters             = new AccommodationSearchByDestinationRequestViewModel_Filters
                        {
                            HasRoomAvailabilityOnly = true
                        },
                        AvailabilityRequest = new AccommodationSearchByDestinationRequestViewModel_Availability
                        {
                            CheckInDate  = checkIn,
                            CheckOutDate = checkOut,
                            Currency     = currency,
                            RoomsCount   = rooms,
                            GuestsCount  = new AccommodationSearchByDestinationRequestViewModel_GuestsCount
                            {
                                Adult        = adults,
                                Child        = 0,
                                Infant       = 0,
                                ChildrenAges = new List<int>()
                            }
                        }
                    });

                _logger.LogInformation("Hotel search returned {Count} results", searchResponse?.Result?.Count ?? 0);
                var first = searchResponse?.Result?.FirstOrDefault();
                _logger.LogInformation("Sample images for '{Name}': [{Images}]",
                    first?.Name,
                    string.Join(", ", first?.Images ?? Enumerable.Empty<string>()));
                return MapResults(searchResponse, destinationFullName, currency);
            }
            catch (TravlrSearchApiClientException ex)
            {
                _logger.LogWarning("Hotel search with availability failed — status {Status}, response: [{Response}] — retrying without availability",
                    ex.StatusCode, ex.Response);
            }

            // Fallback: search without availability (returns reference prices only)
            try
            {
                var fallback = await _searchClient.AccommodationSearchByDestinationAsync(
                    new AccommodationSearchByDestinationRequestViewModel
                    {
                        Page                = new SearchRequestViewModel_Page { Size = 40, Current = 1 },
                        DestinationFullName = destinationFullName,
                        SessionId           = Guid.NewGuid().ToString("N")[..20],
                        GetComparisonPrice  = true,
                        IncludeFacets       = true,
                        Tags                = new[] { "webapp_search" }
                    });

                _logger.LogInformation("Fallback search returned {Count} results", fallback?.Result?.Count ?? 0);
                return MapResults(fallback, destinationFullName, currency);
            }
            catch (TravlrSearchApiClientException ex)
            {
                _logger.LogError("Fallback hotel search also failed — status {Status}, response: [{Response}]",
                    ex.StatusCode, ex.Response);
                throw;
            }
        }

        private static List<Hotel> MapResults(
            AccommodationSearchByDestinationResponseViewModel? response,
            string fallbackLocation,
            string fallbackCurrency)
        {
            if (response?.Result == null) return new List<Hotel>();

            return response.Result.Select(doc => new Hotel
            {
                Id               = doc.Id,
                Name             = doc.Name,
                Location         = doc.DestinationDetails?.DisplayName
                                   ?? doc.DestinationDetails?.City
                                   ?? fallbackLocation,
                StarRating       = doc.StarRating,
                GuestRating      = doc.GuestRating,
                GuestRatingCount = doc.GuestRatingCount,
                Price            = doc.CurrentCheapestPrice ?? doc.ReferencePrice ?? 0,
                Currency         = doc.Currency ?? fallbackCurrency,
                ImageUrl         = doc.Images?.FirstOrDefault(),
                Amenities        = doc.HotelAmenities?.ToList(),
                Images           = doc.Images?.ToList()
            }).ToList();
        }
    }
}
