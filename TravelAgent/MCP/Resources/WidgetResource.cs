using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace TravelAgent.MCP.Resources;

[McpServerResourceType]
public class WidgetResource(IConfiguration configuration, IHttpClientFactory httpClientFactory)
{
    [McpServerResource(UriTemplate = "ui://widgets/list-hotel.html")]
    public async Task<ResourceContents> GetHotelListWidget()
    {
        var widgetUrl = configuration["WidgetUrl"]!.TrimEnd('/');
        var http = httpClientFactory.CreateClient("Widget");
        var content = await http.GetStringAsync(widgetUrl);

        return new TextResourceContents
        {
            Uri = "ui://widgets/list-hotel.html",
            MimeType = "text/html;profile=mcp-app",
            Text = content
        };
    }

    [McpServerResource(UriTemplate = "ui://widgets/list-vehicle.html")]
    public async Task<ResourceContents> GetCarRentalListWidget()
    {
        var widgetUrl = configuration["WidgetUrl"]!.TrimEnd('/');
        var http = httpClientFactory.CreateClient("Widget");
        var content = await http.GetStringAsync($"{widgetUrl}/list-vehicle.html");

        return new TextResourceContents
        {
            Uri = "ui://widgets/list-vehicle.html",
            MimeType = "text/html;profile=mcp-app",
            Text = content
        };
    }
}
