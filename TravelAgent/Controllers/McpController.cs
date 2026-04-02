using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TravelAgent.Models;
using TravelAgent.Services;

namespace TravelAgent.Controllers
{
    [ApiController]
    [Route("mcp")]
    public class McpController : ControllerBase
    {
        private readonly ILogger<McpController> _logger;
        private readonly IHotelService _hotelService;
        private readonly IConfiguration _configuration;

        public McpController(ILogger<McpController> logger, IHotelService hotelService, IConfiguration configuration)
        {
            _logger = logger;
            _hotelService = hotelService;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> Handle([FromBody] McpRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Method))
                return BadRequest(JsonRpcError(null, -32600, "Invalid Request"));

            return request.Method switch
            {
                "initialize" => HandleInitialize(request),
                "tools/list" => HandleToolsList(request),
                "tools/call" => await HandleToolsCall(request),
                "resources/list" => HandleResourcesList(request),
                "resources/read" => HandleResourcesRead(request),
                _ => Ok(JsonRpcError(request.Id, -32601, $"Method not found: {request.Method}"))
            };
        }

        private IActionResult HandleInitialize(McpRequest request)
        {
            return Ok(new
            {
                jsonrpc = "2.0",
                id = request.Id,
                result = new
                {
                    protocolVersion = "2024-11-05",
                    capabilities = new { tools = new { }, resources = new { } },
                    serverInfo = new { name = "hotel-search-mcp", version = "1.0.0" }
                }
            });
        }

        private IActionResult HandleToolsList(McpRequest request)
        {
            return Ok(new
            {
                jsonrpc = "2.0",
                id = request.Id,
                result = new
                {
                    tools = new[]
                    {
                        new
                        {
                            name = "searchHotels",
                            description = "Search hotels by location and return available options with pricing and ratings",
                            inputSchema = new
                            {
                                type = "object",
                                properties = new
                                {
                                    location = new
                                    {
                                        type = "string",
                                        description = "City or location name to search hotels in (e.g. 'Paris', 'New York')"
                                    }
                                },
                                required = new[] { "location" }
                            }
                        }
                    }
                }
            });
        }

        private async Task<IActionResult> HandleToolsCall(McpRequest request)
        {
            if (request.Params == null)
                return Ok(JsonRpcError(request.Id, -32602, "Invalid params: missing params"));

            var root = request.Params.Value;

            if (!root.TryGetProperty("name", out var toolNameElement))
                return Ok(JsonRpcError(request.Id, -32602, "Invalid params: missing tool name"));

            var toolName = toolNameElement.GetString();

            return toolName switch
            {
                "searchHotels" => await HandleSearchHotels(request.Id, root),
                _ => Ok(JsonRpcError(request.Id, -32601, $"Unknown tool: {toolName}"))
            };
        }

        private async Task<IActionResult> HandleSearchHotels(object? requestId, JsonElement paramsRoot)
        {
            var location = "";

            if (paramsRoot.TryGetProperty("arguments", out var argsElement) &&
                argsElement.TryGetProperty("location", out var locationElement))
            {
                location = locationElement.GetString() ?? "";
            }

            if (string.IsNullOrWhiteSpace(location))
                return Ok(JsonRpcError(requestId, -32602, "Invalid params: 'location' is required"));

            var hotels = await _hotelService.SearchHotel(location);

            var hotelResults = hotels.Select(h => new
            {
                id = h.Id,
                name = h.Name,
                location = h.Location,
                rating = h.Rating,
                price = h.Price,
                image_url = h.ImageUrl
            }).ToList();

            const string widgetResourceUri = "ui://hotel-widget";

            return Ok(new
            {
                jsonrpc = "2.0",
                id = requestId,
                result = new
                {
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = $"Found {hotelResults.Count} hotel(s) in {location}."
                        }
                    },
                    structuredContent = new
                    {
                        hotels = hotelResults
                    },
                    _meta = new Dictionary<string, object>
                    {
                        ["openai/outputTemplate"] = widgetResourceUri
                    }
                }
            });
        }

        private IActionResult HandleResourcesList(McpRequest request)
        {
            return Ok(new
            {
                jsonrpc = "2.0",
                id = request.Id,
                result = new
                {
                    resources = new[]
                    {
                        new
                        {
                            uri = "ui://hotel-widget",
                            name = "Hotel Search Widget",
                            mimeType = "text/html;profile=mcp-app",
                            description = "Interactive hotel search results widget"
                        }
                    }
                }
            });
        }

        private IActionResult HandleResourcesRead(McpRequest request)
        {
            var uri = "";
            if (request.Params.HasValue)
            {
                var root = request.Params.Value;
                if (root.TryGetProperty("uri", out var uriElement))
                    uri = uriElement.GetString() ?? "";
            }

            if (uri != "ui://hotel-widget")
                return Ok(JsonRpcError(request.Id, -32002, $"Resource not found: {uri}"));

            var html = GetWidgetHtml();

            return Ok(new
            {
                jsonrpc = "2.0",
                id = request.Id,
                result = new
                {
                    contents = new[]
                    {
                        new
                        {
                            uri = "ui://hotel-widget",
                            mimeType = "text/html;profile=mcp-app",
                            text = html
                        }
                    }
                }
            });
        }

        private static string GetWidgetHtml()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Templates", "hotel-widget.html");
            return System.IO.File.ReadAllText(path);
        }

        private static object JsonRpcError(object? id, int code, string message) => new
        {
            jsonrpc = "2.0",
            id,
            error = new { code, message }
        };
    }
}
