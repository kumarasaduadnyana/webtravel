using System.ComponentModel;
using ModelContextProtocol.Server;
using Travlr.Search.Client;

namespace TravelAgent.MCP.Tools;

[McpServerToolType]
public class DestinationTool(ITravlrSearchApiClient searchClient)
{
    [McpServerTool(Name = "get_destinations", Idempotent = true, OpenWorld = false)]
    [Description("Get a list of destinations based on a query.")]
    public async Task<List<Destination>> GetDestinations(
        [Description("Query string to filter destinations")] string query,
        [Description("Number of destinations to return per page")] int pageSize = 40)
    {
        var elasticResponse = await searchClient.DestinationSearchAsync(new DestinationSearchRequestViewModel
        {
            Page = new SearchRequestViewModel_Page { Size = pageSize, Current = 1 },
            Query = query,
        });
        
        return elasticResponse.Result.Select(dest => new Destination
        {
            Id = dest.Id,
            Name = dest.Name,
            FullName = dest.FullName,
            Latitude = dest.GeoCoordinate.Latitude,
            Longitude = dest.GeoCoordinate.Longitude,
            Country = dest.Country
        }).ToList();
    }
}

public class Destination
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string FullName { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Country { get; set; }
}