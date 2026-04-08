using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace TravelAgent.MCP.Resources;

[McpServerResourceType]
public class WidgetResource
{
    [McpServerResource(UriTemplate = "ui://widgets/list-hotel.html")]
    public async Task<ResourceContents> GetHotelListWidget()
    {
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Widgets", $"list-hotel.html");

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"UI template list-hotel.html not found.");
        }

        var content = await File.ReadAllTextAsync(filePath);

        return new TextResourceContents
        {
            Uri = $"ui://widgets/list-hotel.html",
            MimeType = "text/html;profile=mcp-app",
            Text = content
        };
    }
    
    [McpServerResource(UriTemplate = "ui://widgets/list-vehicle.html")]
    public async Task<ResourceContents> GetCarRentalListWidget()
    {
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Widgets", $"list-vehicle.html");

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"UI template list-hotel.html not found.");
        }

        var content = await File.ReadAllTextAsync(filePath);

        return new TextResourceContents
        {
            Uri = $"ui://widgets/list-vehicle.html",
            MimeType = "text/html;profile=mcp-app",
            Text = content
        };
    }
    
}