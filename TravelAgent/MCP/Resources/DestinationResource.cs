using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using TravelAgent.MCP.Tools;
using Travlr.Search.Client;

namespace TravelAgent.MCP.Resources;

[McpServerResourceType]
public class DestinationResource(ITravlrSearchApiClient searchClient)
{
    [McpServerResource(UriTemplate = "destinations://{query:string}?pageSize={pageSize:int}")]
    [Description("Return the destinations matching the given query")]
    public async Task<ResourceContents> GetDestination(
        [Description("Query string to filter destinations")] string query,
        [Description("Number of destinations to return per page")] int pageSize = 40)
    {
        var elasticResponse = await searchClient.DestinationSearchAsync(new DestinationSearchRequestViewModel
        {
            Page = new SearchRequestViewModel_Page { Size = pageSize, Current = 1 },
            Query = query,
        });
        
        var result = elasticResponse.Result.Select(dest => new Destination
        {
            Id = dest.Id,
            Name = dest.Name,
            FullName = dest.FullName,
            Latitude = dest.GeoCoordinate.Latitude,
            Longitude = dest.GeoCoordinate.Longitude,
            Country = dest.Country
        }).ToList();
        
        return new TextResourceContents
        {
            Uri = $"destinations://{query}",
            MimeType = "application/json",
            Text = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true })
        };
    }
}