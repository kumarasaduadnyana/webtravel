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
            int children,
            int rooms,
            string currency,
            string? sortBy = null,
            List<int>? starRatings = null,
            double? minPrice = null,
            double? maxPrice = null,
            int? minRating = null,
            List<string>? amenities = null,
            int pageSize = 40)
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

            // Build filters for the API; sort is applied client-side only
            // (API sort field names are not guaranteed, so we don't pass them to avoid search failures)
            var apiFilter = BuildFilters(starRatings, minPrice, maxPrice, amenities);

            try
            {
                _logger.LogInformation("Searching hotels for '{Dest}', {CheckIn} → {CheckOut}, {Adults} adults, {Rooms} rooms, {Currency}, sort={Sort}",
                    destinationFullName, checkIn.ToString("yyyy-MM-dd"), checkOut.ToString("yyyy-MM-dd"), adults, rooms, currency, sortBy);

                var searchResponse = await _searchClient.AccommodationSearchByDestinationAsync(
                    new AccommodationSearchByDestinationRequestViewModel
                    {
                        Page = new SearchRequestViewModel_Page { Size = pageSize, Current = 1 },
                        DestinationFullName = destinationFullName,
                        SessionId = Guid.NewGuid().ToString("N")[..20],
                        GetComparisonPrice = true,
                        IncludeFacets = true,
                        Tags = new[] { "webapp_search" },
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
                                Child = children,
                                Infant = 0,
                                ChildrenAges = new List<int>()
                            }
                        }
                    });
                
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

        private static (ICollection<int>? starRatings, ICollection<string>? amenities,
            AccommodationSearchByDestinationRequestViewModel_PriceRange? priceRange)
            BuildFilters(List<int>? starRatings, double? minPrice, double? maxPrice, List<string>? amenities)
        {
            ICollection<int>? stars = starRatings?.Count > 0 ? starRatings : null;
            ICollection<string>? amen = amenities?.Count > 0 ? amenities : null;
            AccommodationSearchByDestinationRequestViewModel_PriceRange? price = maxPrice.HasValue
                ? new AccommodationSearchByDestinationRequestViewModel_PriceRange { FromPriceAUD = minPrice, ToPriceAUD = maxPrice }
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
                "price_asc"       => hotels.OrderBy(h => h.Price ?? double.MaxValue).ToList(),
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
            // The search API sometimes returns a composite content item ID in the form
            // "contentId|Provider-PropertyId" (e.g. "trv-app-dev-...|Expedia-9832462").
            // The accommodation API's /single endpoint only accepts the clean contentId part.
            // Strip the pipe-separated suffix and extract provider/hotelCode from it if needed.
            var pipeParts    = hotelId.Split('|');
            var cleanId      = pipeParts[0];
            if (pipeParts.Length > 1)
            {
                // suffix looks like "Expedia-9832462" → provider = "Expedia", hotelCode = "9832462"
                var suffix   = pipeParts[1];
                var dashIdx  = suffix.LastIndexOf('-');
                if (dashIdx > 0)
                {
                    if (string.IsNullOrEmpty(provider))
                        provider  = suffix[..dashIdx];
                    if (string.IsNullOrEmpty(hotelCode))
                        hotelCode = suffix[(dashIdx + 1)..];
                }
            }

            _logger.LogInformation("Getting hotel details for '{HotelId}' (cleanId='{CleanId}'), provider: {Provider}, hotelCode: {HotelCode}",
                hotelId, cleanId, provider, hotelCode);

            // Build rooms: each room is a list of GuestCount (AgeQualifyingCode 10 = adult)
            var roomGuests = Enumerable.Range(0, rooms)
                .Select(_ => (ICollection<GuestCount>)new List<GuestCount>
                {
                    new GuestCount { AgeQualifyingCode = 10, Count = adults }
                })
                .ToList();

            var singleParams = new SingleParameters
            {
                ContentItemId     = cleanId,
                HotelCode         = hotelCode,
                Provider          = provider,
                CurrencyCode      = currency,
                CheckIn           = checkIn,
                CheckOut          = checkOut,
                RateType          = RateType.Public,
                Rooms             = roomGuests,
                CustomerSessionId = Guid.NewGuid().ToString("N")[..20],
                CustomerUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36"
            };

            // AvailabilityPropertyId (hotelCode) is the integer provider property ID;
            // ContentItemId (cleanId) is a content-store string — try hotelCode first.
            if (int.TryParse(hotelCode, out var propertyId) ||
                int.TryParse(cleanId,   out propertyId))
            {
                singleParams.PropertyId = propertyId;
            }

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
                    BedConfiguration = bedConfig?.ToString(),
                    MealsDescription = mealDesc,
                    Refundable = r.Refundable,
                    FreeCancellationUntil = r.FreeCancellation?.IsFree == true ? r.FreeCancellation.EndDate : null,
                    Price = r?.CustomerTotalAfterTax,
                    StrikeThroughPrice = r?.StrikeThroughTotalAfterTax,
                    Currency = r?.CustomerCurrencyCode ?? currency
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
            var hotelResult = new List<Hotel>();
            if (response?.Result == null) return new List<Hotel>();

            foreach (var item in response.Result)
            {
                if ((item.CurrentCheapestPrice == null && item.ReferencePrice == 0) 
                    && !item.HasRoomAvailability)
                {
                    continue;
                }

                hotelResult.Add(new Hotel
                {
                    Id = item.Id,
                    Name = item.Name,
                    Location = item.DestinationDetails?.DisplayName ?? item.DestinationDetails?.City ?? fallbackLocation,
                    Rating = item.StarRating ?? ConvertToStarRating(item.GuestRating ?? 0),
                    StarRating = item.StarRating.HasValue ? Math.Round(item.StarRating.Value, 0) : null,
                    GuestRating = item.GuestRating.HasValue ? Math.Round(item.GuestRating.Value, 0) : null,
                    GuestRatingCount = item.GuestRatingCount,
                    Price = item.CurrentCheapestPrice ?? item.ReferencePrice ?? 0,
                    SupplierPrice = item.CurrentCheapestSupplierPrice,
                    StrikeThroughPrice = item.CurrentStrikeThroughPrice,
                    Currency = item.Currency ?? fallbackCurrency,
                    ImageUrl = item.Images?.FirstOrDefault(),
                    Amenities = item.HotelAmenities?.ToList(),
                    Images = item.Images?.ToList(),
                    HotelCode = item.AvailabilityPropertyId,
                    Provider = item.AvailabilityProvider?.ToString(),
                    DestinationDetail = item?.DestinationDetails ?? new DestinationDetails()
                });
            }

            return hotelResult;
        }

        public static double ConvertToStarRating(double score)
        {
            if (score < 0 || score > 100)
                throw new ArgumentOutOfRangeException(nameof(score), "Score must be between 0 and 100.");

            var raw = (score / 100.0) * 5.0;
            return Math.Round(raw * 2, MidpointRounding.AwayFromZero) / 2.0;
        }
    }
}
