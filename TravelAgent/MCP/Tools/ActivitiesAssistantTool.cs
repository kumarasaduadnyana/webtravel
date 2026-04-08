using ModelContextProtocol.Server;
using Travlr.Search.Client;

namespace TravelAgent.MCP.Tools;

[McpServerToolType]
public class ActivitiesAssistantTool(ITravlrSearchApiClient searchApiClient)
{
    /*public async Task<object> SearchActivitiesByDestination()
    {
        var result = await searchApiClient.ActivitySearchByDestinationAsync(
            new ActivitySearchByDestinationRequestViewModel
            {
                Page = new SearchRequestViewModel_Page { Size = 40, Current = 1 },
                DestinationFullName = 
            });
        return new { };
    }*/
}