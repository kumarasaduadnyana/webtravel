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
            
        // If running in development/local, we might need to look at the project folder
        /*if (!File.Exists(filePath))
        {
            // Try looking in the source folder if base directory doesn't have it (for local dev)
            filePath = Path.Combine(Directory.GetCurrentDirectory(), "src", "Travlr.AI.Application", "UI", $"list-hotel.html");
        }*/

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
}