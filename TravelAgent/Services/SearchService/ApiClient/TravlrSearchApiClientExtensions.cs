// Extends NSwag-generated partial classes with fields present in the actual API
// but missing from the swagger schema.
namespace Travlr.Search.Client
{
    public partial class AccommodationSearchByDestinationRequestViewModel
    {
        [Newtonsoft.Json.JsonProperty("sessionId", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string? SessionId { get; set; }

        [Newtonsoft.Json.JsonProperty("getComparisonPrice", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public bool? GetComparisonPrice { get; set; }

        [Newtonsoft.Json.JsonProperty("tags", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public System.Collections.Generic.ICollection<string>? Tags { get; set; }
    }

    public partial class AccommodationSearchByDestinationRequestViewModel_Filters
    {
        [Newtonsoft.Json.JsonProperty("hasRoomAvailabilityOnly", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public bool? HasRoomAvailabilityOnly { get; set; }
    }

    public partial class AccommodationSearchByDestinationRequestViewModel_GuestsCount
    {
        [Newtonsoft.Json.JsonProperty("childrenAges", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public System.Collections.Generic.ICollection<int>? ChildrenAges { get; set; }
    }
}
