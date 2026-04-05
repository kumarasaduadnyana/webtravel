using TravelAgent.Models;
using Travlr.Search.Client;
using Travlr.Accommodations.Application.Clients.AccommodationApi;
using HotelDetailModel = TravelAgent.Models.HotelDetail;

namespace TravelAgent.Services
{
    public class HotelService : IHotelService
    {
        private readonly ITravlrSearchApiClient _searchClient;
        private readonly IAccommodationApiClient _accommodationClient;
        private readonly ILogger<HotelService> _logger;

        public HotelService(
            ITravlrSearchApiClient searchClient,
            IAccommodationApiClient accommodationClient,
            ILogger<HotelService> logger)
        {
            _searchClient = searchClient;
            _accommodationClient = accommodationClient;
            _logger = logger;
        }

        public async Task<List<Hotel>> SearchHotel(
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
            string[]? amenities = null)
        {
            string destinationFullName = destination;
            
            try
            {
                _logger.LogInformation("Resolving destination: '{Input}'", destination);

                var destResponse = await _searchClient.AccommodationDestinationSearchAsync(
                    new DestinationSearchRequestViewModel
                    {
                        Page = new SearchRequestViewModel_Page 
                        { 
                            Size = 5, 
                            Current = 1 
                        },
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
                _logger.LogWarning("Destination resolution failed — status {Status}, response: [{Response}] — using raw input", ex.StatusCode, ex.Response);
            }

            // Build API sort and filters from caller parameters
            var apiSorts  = BuildSorts(sortBy);
            var apiFilter = BuildFilters(starRatings, maxPrice, amenities);

            try
            {
                _logger.LogInformation("Searching hotels for '{Dest}', {CheckIn} → {CheckOut}, {Adults} adults, {Rooms} rooms, {Currency}, sort={Sort}",
                    destinationFullName, checkIn.ToString("yyyy-MM-dd"), checkOut.ToString("yyyy-MM-dd"), adults, rooms, currency, sortBy);

                var searchResponse = await _searchClient.AccommodationSearchByDestinationAsync(
                    new AccommodationSearchByDestinationRequestViewModel
                    {
                        Page = new SearchRequestViewModel_Page { Size = 40, Current = 1 },
                        DestinationFullName = destinationFullName,
                        SessionId = Guid.NewGuid().ToString("N")[..20],
                        GetComparisonPrice = true,
                        IncludeFacets = true,
                        Tags = new[] { "webapp_search" },
                        Sorts = apiSorts,
                        Filters = new AccommodationSearchByDestinationRequestViewModel_Filters
                        {
                            HasRoomAvailabilityOnly = true,
                            StarRatings = apiFilter.starRatings,
                            HotelAmenities = apiFilter.amenities,
                            PriceRange = apiFilter.priceRange
                        },
                        AvailabilityRequest = new AccommodationSearchByDestinationRequestViewModel_Availability
                        {
                            CheckInDate = checkIn,
                            CheckOutDate = checkOut,
                            Currency = currency,
                            RoomsCount = rooms,
                            GuestsCount = new AccommodationSearchByDestinationRequestViewModel_GuestsCount
                            {
                                Adult = adults,
                                Child = 0,
                                Infant = 0,
                                ChildrenAges = new List<int>()
                            }
                        }
                    });

                _logger.LogInformation("Hotel search returned {Count} results", searchResponse?.Result?.Count ?? 0);
                var first = searchResponse?.Result?.FirstOrDefault();
                _logger.LogInformation("Sample images for '{Name}': [{Images}]",
                    first?.Name,
                    string.Join(", ", first?.Images ?? Enumerable.Empty<string>()));
                return ApplyClientSort(MapResults(searchResponse, destinationFullName, currency), sortBy, minRating);
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
                        Page = new SearchRequestViewModel_Page { Size = 40, Current = 1 },
                        DestinationFullName = destinationFullName,
                        SessionId = Guid.NewGuid().ToString("N")[..20],
                        GetComparisonPrice = true,
                        IncludeFacets = true,
                        Tags = new[] { "webapp_search" },
                        Sorts = apiSorts,
                        Filters = new AccommodationSearchByDestinationRequestViewModel_Filters
                        {
                            StarRatings = apiFilter.starRatings,
                            HotelAmenities = apiFilter.amenities,
                            PriceRange = apiFilter.priceRange
                        }
                    });

                _logger.LogInformation("Fallback search returned {Count} results", fallback?.Result?.Count ?? 0);
                return ApplyClientSort(MapResults(fallback, destinationFullName, currency), sortBy, minRating);
            }
            catch (TravlrSearchApiClientException ex)
            {
                _logger.LogError("Fallback hotel search also failed — status {Status}, response: [{Response}]",
                    ex.StatusCode, ex.Response);
                throw;
            }
        }

        // ── Sort / filter helpers ──────────────────────────────────────

        private static ICollection<AccommodationSearchByDestinationRequestViewModel_Sort>? BuildSorts(string? sortBy)
        {
            // Map caller-friendly sort names → API sort fields
            return sortBy switch
            {
                "cheapest"       => Sorts("CurrentCheapestPrice", SearchSortOrderEnum.Ascending),
                "most_expensive" => Sorts("CurrentCheapestPrice", SearchSortOrderEnum.Descending),
                "best_rated"     => Sorts("GuestRating",          SearchSortOrderEnum.Descending),
                "top_stars"      => Sorts("StarRating",           SearchSortOrderEnum.Descending),
                "popular"        => Sorts("Popularity",           SearchSortOrderEnum.Descending),
                _                => null   // default / recommended — let the API decide
            };

            static ICollection<AccommodationSearchByDestinationRequestViewModel_Sort> Sorts(string title, SearchSortOrderEnum order) =>
                new[] { new AccommodationSearchByDestinationRequestViewModel_Sort { Title = title, Order = order } };
        }

        private static (ICollection<int>? starRatings, ICollection<string>? amenities,
            AccommodationSearchByDestinationRequestViewModel_PriceRange? priceRange)
            BuildFilters(int[]? starRatings, double? maxPrice, string[]? amenities)
        {
            ICollection<int>? stars = starRatings?.Length > 0 ? starRatings : null;
            ICollection<string>? amen = amenities?.Length > 0 ? amenities : null;
            AccommodationSearchByDestinationRequestViewModel_PriceRange? price = maxPrice.HasValue
                ? new AccommodationSearchByDestinationRequestViewModel_PriceRange { ToPriceAUD = maxPrice }
                : null;
            return (stars, amen, price);
        }

        private static List<Hotel> ApplyClientSort(List<Hotel> hotels, string? sortBy, int? minRating)
        {
            // Client-side sort/filter as a reliable fallback (API sort may not always take effect)
            if (minRating.HasValue)
                hotels = hotels.Where(h => h.GuestRating.HasValue && h.GuestRating.Value >= minRating.Value).ToList();

            return sortBy switch
            {
                "cheapest"       => hotels.OrderBy(h => h.Price ?? double.MaxValue).ToList(),
                "most_expensive" => hotels.OrderByDescending(h => h.Price ?? 0).ToList(),
                "best_rated"     => hotels.OrderByDescending(h => h.GuestRating ?? 0).ToList(),
                "top_stars"      => hotels.OrderByDescending(h => h.StarRating ?? 0).ToList(),
                "popular"        => hotels, // rely on API order
                _                => hotels
            };
        }

        public async Task<HotelDetailModel?> GetHotelDetails(
            string hotelId,
            string? hotelCode,
            string? provider,
            DateTime checkIn,
            DateTime checkOut,
            int adults,
            int rooms,
            string currency)
        {
            _logger.LogInformation("Getting hotel details for '{HotelId}', provider: {Provider}", hotelId, provider);

            // Build rooms: each room is a list of GuestCount (AgeQualifyingCode 10 = adult)
            var roomGuests = Enumerable.Range(0, rooms)
                .Select(_ => (ICollection<GuestCount>)new List<GuestCount>
                {
                    new GuestCount { AgeQualifyingCode = 10, Count = adults }
                })
                .ToList();

            var singleParams = new SingleParameters
            {
                ContentItemId = hotelId,
                HotelCode = hotelCode,
                Provider = provider,
                CurrencyCode = currency,
                CheckIn = checkIn,
                CheckOut = checkOut,
                RateType = RateType.Public,
                Rooms = roomGuests,
                CustomerSessionId = Guid.NewGuid().ToString("N")[..20]
            };

            if (int.TryParse(hotelId, out var propertyId))
                singleParams.PropertyId = propertyId;

            HotelSingleResult result;
            try
            {
                result = await _accommodationClient.SingleAsync(singleParams);
            }
            catch (Exception ex)
            {
                _logger.LogError("GetHotelDetails failed for '{HotelId}': {Msg}", hotelId, ex.Message);
                throw;
            }

            var hotel = result?.Hotel;
            if (hotel == null) return null;

            // Build address string
            var addrParts = new[] {
                hotel.Address?.AddressLine1,
                hotel.Address?.City,
                hotel.Address?.State,
                hotel.Address?.Country
            }.Where(s => !string.IsNullOrWhiteSpace(s));

            // Map room rates
            var roomRates = hotel.RoomRates?.Select(r =>
            {
                var bedConfig = r.BedGroups?.FirstOrDefault()?.Configuration?.FirstOrDefault();

                var meals = r.MealsIncluded;
                string mealDesc;
                if (meals == null || (!meals.Breakfast && !meals.Lunch && !meals.Dinner))
                {
                    mealDesc = "Meals not included";
                }
                else
                {
                    var parts = new List<string>();
                    if (meals.Breakfast) parts.Add("Breakfast");
                    if (meals.Lunch) parts.Add("Lunch");
                    if (meals.Dinner) parts.Add("Dinner");
                    mealDesc = string.Join(" & ", parts) + " included";
                }

                return new RoomRateInfo
                {
                    RoomName = r.RoomName,
                    RoomType = r.RoomType,
                    ImageUrl = r.Images?.FirstOrDefault()?.Url,
                    BedConfiguration = bedConfig,
                    MealsDescription = mealDesc,
                    Refundable = r.Refundable,
                    FreeCancellationUntil = r.FreeCancellation?.IsFree == true ? r.FreeCancellation.EndDate : null,
                    Price = r.SellPrice,
                    StrikeThroughPrice = r.StrikeThroughTotalAfterTax,
                    Currency = r.CustomerCurrencyCode ?? currency
                };
            }).ToList() ?? new List<RoomRateInfo>();

            return new HotelDetailModel
            {
                Id = hotelId,
                Name = hotel.Name,
                StarRating = hotel.StarRating,
                Description = hotel.Description?.FirstOrDefault(),
                Images = hotel.Images?.Select(i => i.Url).Where(u => !string.IsNullOrEmpty(u)).ToList(),
                Amenities = hotel.Amenities?.Select(a => a.Value).Where(v => !string.IsNullOrEmpty(v)).Take(12).ToList(),
                Address = string.Join(", ", addrParts),
                Latitude = hotel.Geo?.Latitude,
                Longitude = hotel.Geo?.Longitude,
                Currency = currency,
                Price = roomRates.Count > 0 ? roomRates.Min(r => (double?)r.Price) : null,
                RoomRates = roomRates
            };
        }

        private static List<Hotel> MapResults(
            AccommodationSearchByDestinationResponseViewModel? response,
            string fallbackLocation,
            string fallbackCurrency)
        {
            if (response?.Result == null) return new List<Hotel>();

            return response.Result.Select(doc => new Hotel
            {
                Id = doc.Id,
                Name = doc.Name,
                Location = doc.DestinationDetails?.DisplayName ?? doc.DestinationDetails?.City ?? fallbackLocation,
                StarRating = doc.StarRating,
                GuestRating = doc.GuestRating,
                GuestRatingCount = doc.GuestRatingCount,
                Price = doc.CurrentCheapestPrice ?? doc.ReferencePrice ?? 0,
                SupplierPrice = doc.CurrentCheapestSupplierPrice,
                StrikeThroughPrice = doc.CurrentStrikeThroughPrice,
                Currency = doc.Currency ?? fallbackCurrency,
                ImageUrl = doc.Images?.FirstOrDefault(),
                Amenities = doc.HotelAmenities?.ToList(),
                Images = doc.Images?.ToList(),
                HotelCode = doc.AvailabilityPropertyId,
                Provider = doc.AvailabilityProvider?.ToString()
            }).ToList();
        }
    }
}
