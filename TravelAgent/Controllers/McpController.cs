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
        // ui:// URI ChatGPT uses to fetch the widget via resources/read
        private const string WidgetUri = "ui://widget/hotel-widget.html";

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
                "initialize"     => HandleInitialize(request),
                "tools/list"     => HandleToolsList(request),
                "tools/call"     => await HandleToolsCall(request),
                "resources/list" => HandleResourcesList(request),
                "resources/read" => HandleResourcesRead(request),
                _ => Ok(JsonRpcError(request.Id, -32601, $"Method not found: {request.Method}"))
            };
        }

        // ── initialize ────────────────────────────────────────────────────────────

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

        // ── tools/list ────────────────────────────────────────────────────────────

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
                            },
                            annotations = new
                            {
                                readOnlyHint = true,
                                destructiveHint = false,
                                openWorldHint = false
                            },
                            // Tells ChatGPT which widget template to render for this tool
                            _meta = new Dictionary<string, object>
                            {
                                ["openai/outputTemplate"]          = WidgetUri,
                                ["openai/toolInvocation/invoking"] = "Searching for hotels\u2026",
                                ["openai/toolInvocation/invoked"]  = "Hotel results ready",
                                ["openai/widgetAccessible"]        = true
                            }
                        }
                    }
                }
            });
        }

        // ── tools/call ────────────────────────────────────────────────────────────

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
                id        = h.Id,
                name      = h.Name,
                location  = h.Location,
                rating    = h.Rating,
                price     = h.Price,
                image_url = h.ImageUrl
            }).ToList();

            // Vercel preview link (fallback for plain-text clients)
            var widgetUrl  = _configuration["WidgetUrl"] ?? "";
            var dataJson   = System.Text.Json.JsonSerializer.Serialize(new { hotels = hotelResults });
            var dataBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(dataJson));
            var widgetLink = $"{widgetUrl.TrimEnd('/')}/?data={dataBase64}";

            var markdownTable = BuildMarkdownTable(
                hotelResults.Select(h => new HotelRow(h.name, h.location, h.rating, h.price)).ToList());
            var responseText = $"## Hotels in {location}\n\n{markdownTable}\n\n[View full hotel cards]({widgetLink})";

            return Ok(new
            {
                jsonrpc = "2.0",
                id = requestId,
                result = new
                {
                    // content: plain-text fallback shown if no widget is rendered
                    content = new[]
                    {
                        new { type = "text", text = responseText }
                    },
                    // structuredContent: forwarded to widget as window.openai.toolOutput
                    structuredContent = new { hotels = hotelResults },
                    // _meta: tells ChatGPT to render the widget and shows progress text
                    _meta = new Dictionary<string, object>
                    {
                        ["openai/outputTemplate"]          = WidgetUri,
                        ["openai/toolInvocation/invoking"] = "Searching for hotels\u2026",
                        ["openai/toolInvocation/invoked"]  = "Hotel results ready"
                    }
                }
            });
        }

        // ── resources/list ────────────────────────────────────────────────────────

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
                            uri         = WidgetUri,
                            name        = "Hotel Search Widget",
                            mimeType    = "text/html+skybridge",   // required MIME type per OpenAI Apps SDK
                            description = "Interactive hotel search results card grid",
                            _meta = new Dictionary<string, object>
                            {
                                ["openai/outputTemplate"]  = WidgetUri,
                                ["openai/widgetAccessible"] = true
                            }
                        }
                    }
                }
            });
        }

        // ── resources/read ────────────────────────────────────────────────────────

        private IActionResult HandleResourcesRead(McpRequest request)
        {
            var uri = "";
            if (request.Params.HasValue &&
                request.Params.Value.TryGetProperty("uri", out var uriEl))
            {
                uri = uriEl.GetString() ?? "";
            }

            if (uri != WidgetUri)
                return Ok(JsonRpcError(request.Id, -32002, $"Resource not found: {uri}"));

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
                            uri      = WidgetUri,
                            mimeType = "text/html+skybridge",
                            text     = GetWidgetHtml()
                        }
                    }
                }
            });
        }

        // ── helpers ───────────────────────────────────────────────────────────────

        private record HotelRow(string name, string location, double? rating, int price);

        private static string BuildMarkdownTable(List<HotelRow> hotels)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("| Hotel | Location | Rating | Price/Night |");
            sb.AppendLine("|-------|----------|--------|-------------|");
            foreach (var h in hotels)
            {
                var rounded = (int)Math.Round(h.rating ?? 0);
                var stars = new string('★', rounded) + new string('☆', 5 - rounded);
                sb.AppendLine($"| {h.name} | {h.location} | {stars} {h.rating} | ${h.price} |");
            }
            return sb.ToString();
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
