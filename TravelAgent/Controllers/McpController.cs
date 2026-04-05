using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using TravelAgent.Models;
using TravelAgent.Services;

namespace TravelAgent.Controllers
{
    [ApiController]
    [Route("mcp")]
    public class McpController : ControllerBase
    {
        // ui:// URI — ChatGPT fetches HTML via resources/read and renders it in an iframe.
        private const string WidgetUri = "ui://widget/hotel-widget.html";
        private const string DefaultCurrency = "AUD";

        private readonly ILogger<McpController> _logger;
        private readonly IHotelService _hotelService;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public McpController(
            ILogger<McpController> logger, 
            IHotelService hotelService,
            IConfiguration configuration, 
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _hotelService = hotelService;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        // Post method to scan tools that available on MCP for connection (first time only)

        // Note, chatGPT using JsonRpcError to read any details feedback from response
        // Read this: https://www.jsonrpc.org/specification

        [HttpPost]
        public async Task<IActionResult> Handle([FromBody] McpRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Method))
            {
                return BadRequest(JsonRpcError(null, -32600, "Invalid Request"));
            }

            switch (request.Method)
            {
                case "initialize":
                    return HandleInitialize(request);
                    
                case "tools/list":
                    return HandleToolsList(request);

                case "tools/call":
                    return await HandleToolsCall(request);

                case "resources/list":
                    return HandleResourcesList(request);
                    
                case "resources/read":
                   return HandleResourcesRead(request);
                    
                default:
                    return Ok(JsonRpcError(request.Id, -32601, $"Method not found: {request.Method}"));
            }
        }

        // Initialize tools
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

        // Define tools that available on this MCP (search hotel, see detail, booking, etc...)

        private IActionResult HandleToolsList(McpRequest request)
        {
            return Ok(new
            {
                jsonrpc = "2.0",
                id = request.Id,
                result = new
                {
                    tools = new object[]
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
                                    destination = new
                                    {
                                        type = "string",
                                        description = "City or destination name (e.g. 'Bali', 'Paris', 'New York')"
                                    },
                                    check_in = new
                                    {
                                        type = "string",
                                        description = "Check-in date in YYYY-MM-DD format"
                                    },
                                    check_out = new
                                    {
                                        type = "string",
                                        description = "Check-out date in YYYY-MM-DD format"
                                    },
                                    adults = new
                                    {
                                        type = "integer",
                                        description = "Number of adult guests (default: 2)",
                                        @default = 2
                                    },
                                    rooms = new
                                    {
                                        type = "integer",
                                        description = "Number of rooms required (default: 1)",
                                        @default = 1
                                    },
                                    currency = new
                                    {
                                        type = "string",
                                        description = "3-letter currency code (default: AUD)",
                                        @default = "AUD"
                                    },
                                    sort_by = new
                                    {
                                        type = "string",
                                        description = "Sort order for results. Use: 'cheapest' (lowest price first), 'most_expensive' (highest price first), 'best_rated' (highest guest rating first), 'top_stars' (highest star rating first), 'popular' (most popular first). Omit for default relevance.",
                                        @enum = new[] { "cheapest", "most_expensive", "best_rated", "top_stars", "popular" }
                                    },
                                    min_stars = new
                                    {
                                        type = "integer",
                                        description = "Minimum star rating filter (1–5). E.g. 4 for 4-star and above.",
                                        minimum = 1,
                                        maximum = 5
                                    },
                                    max_price = new
                                    {
                                        type = "number",
                                        description = "Maximum price per night (in the selected currency). E.g. 300 to show hotels under $300."
                                    },
                                    min_rating = new
                                    {
                                        type = "integer",
                                        description = "Minimum guest rating score (0–100). E.g. 80 for 'Excellent' and above.",
                                        minimum = 0,
                                        maximum = 100
                                    },
                                    count = new
                                    {
                                        type = "integer",
                                        description = "Number of hotels to return (1–10, default 5).",
                                        minimum = 1,
                                        maximum = 10,
                                        @default = 5
                                    }
                                },
                                // required property must contain on prompt, is don't then response will ask about it
                                required = new[] { "destination", "check_in", "check_out" }
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
                                ["openai/outputTemplate"] = WidgetUri,
                                ["openai/toolInvocation/invoking"] = "Searching for hotels\u2026",
                                ["openai/toolInvocation/invoked"] = "Hotel results ready",
                                ["openai/widgetAccessible"] = true
                            }
                        },
                        new
                        {
                            name = "getHotelDetails",
                            description = "Get detailed information about a specific hotel including room types, rates, amenities, and cancellation policies. Call this after searchHotels when the user wants to view details for a specific hotel.",
                            inputSchema = new
                            {
                                type = "object",
                                properties = new
                                {
                                    hotel_id = new
                                    {
                                        type = "string",
                                        description = "Hotel content item ID from the search results"
                                    },
                                    hotel_code = new
                                    {
                                        type = "string",
                                        description = "Hotel provider code from the search results (optional)"
                                    },
                                    provider = new
                                    {
                                        type = "string",
                                        description = "Hotel provider from the search results (optional)"
                                    },
                                    check_in = new
                                    {
                                        type = "string",
                                        description = "Check-in date in YYYY-MM-DD format"
                                    },
                                    check_out = new
                                    {
                                        type = "string",
                                        description = "Check-out date in YYYY-MM-DD format"
                                    },
                                    adults = new
                                    {
                                        type = "integer",
                                        description = "Number of adult guests",
                                        @default = 2
                                    },
                                    rooms = new
                                    {
                                        type = "integer",
                                        description = "Number of rooms",
                                        @default = 1
                                    },
                                    currency = new
                                    {
                                        type = "string",
                                        description = "3-letter currency code",
                                        @default = "AUD"
                                    }
                                },
                                required = new[] { "hotel_id", "check_in", "check_out" }
                            },
                            annotations = new
                            {
                                readOnlyHint = true,
                                destructiveHint = false,
                                openWorldHint = false
                            },
                            _meta = new Dictionary<string, object>
                            {
                                ["openai/outputTemplate"] = WidgetUri,
                                ["openai/toolInvocation/invoking"] = "Loading hotel details\u2026",
                                ["openai/toolInvocation/invoked"] = "Hotel details ready",
                                ["openai/widgetAccessible"] = true
                            }
                        },
                        //new
                        //{
                        //    name = "searchAvailability",
                        //    description = "Search availability room based on hotels result",
                        //    inputSchema = new
                        //    {
                        //        type = "object",
                        //        properties = new
                        //        {
                        //            destination = new
                        //            {
                        //                type = "string",
                        //                description = "City or destination name (e.g. 'Bali', 'Paris', 'New York')"
                        //            },
                        //            check_in = new
                        //            {
                        //                type = "string",
                        //                description = "Check-in date in YYYY-MM-DD format"
                        //            },
                        //            check_out = new
                        //            {
                        //                type = "string",
                        //                description = "Check-out date in YYYY-MM-DD format"
                        //            },
                        //            adults = new
                        //            {
                        //                type = "integer",
                        //                description = "Number of adult guests (default: 2)",
                        //                @default = 2
                        //            },
                        //            rooms = new
                        //            {
                        //                type = "integer",
                        //                description = "Number of rooms required (default: 1)",
                        //                @default = 1
                        //            },
                        //            currency = new
                        //            {
                        //                type = "string",
                        //                description = "3-letter currency code (default: AUD)",
                        //                @default = "AUD"
                        //            }
                        //        },
                        //        // required property must contain on prompt, is don't then response will ask about it
                        //        required = new[] { "destination", "check_in", "check_out" }
                        //    },
                        //    annotations = new
                        //    {
                        //        readOnlyHint = true,
                        //        destructiveHint = false,
                        //        openWorldHint = false
                        //    },
                        //    // Tells ChatGPT which widget template to render for this tool
                        //    _meta = new Dictionary<string, object>
                        //    {
                        //        ["openai/outputTemplate"] = WidgetUri,
                        //        ["openai/toolInvocation/invoking"] = "Searching for hotels\u2026",
                        //        ["openai/toolInvocation/invoked"] = "Hotel results ready",
                        //        ["openai/widgetAccessible"] = true
                        //    }
                        //}
                    }
                }
            });
        }

        private async Task<IActionResult> HandleToolsCall(McpRequest request)
        {
            if (request.Params == null)
            {
                return Ok(JsonRpcError(request.Id, -32602, "Invalid params: missing params"));
            }

            var root = request.Params.Value;

            if (!root.TryGetProperty("name", out var toolNameElement))
            {
                return Ok(JsonRpcError(request.Id, -32602, "Invalid params: missing tool name"));
            }

            var toolName = toolNameElement.GetString();

            try
            {
                return toolName switch
                {
                    "searchHotels"    => await HandleSearchHotels(request.Id, root),
                    "getHotelDetails" => await HandleGetHotelDetails(request.Id, root),
                    _                 => Ok(JsonRpcError(request.Id, -32601, $"Unknown tool: {toolName}"))
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in tool '{Tool}'", toolName);
                return Ok(JsonRpcError(request.Id, -32603, $"Internal error: {ex.Message}"));
            }
        }

        private async Task<IActionResult> HandleSearchHotels(object? requestId, JsonElement paramsRoot)
        {
            // Define variable, is the prompt not contain, set default value
            var destination = string.Empty;
            var checkInDate = string.Empty;
            var checkOutDate = string.Empty;
            var adults = 2;
            var rooms = 1;
            var currency = DefaultCurrency;
            string? sortBy = null;
            int[]? starRatings = null;
            double? maxPrice = null;
            int? minRating = null;
            var count = 5;

            var paramFromPrompt = paramsRoot.TryGetProperty("arguments", out var promptRequest);
            if (!string.IsNullOrEmpty(paramFromPrompt.ToString()))
            {
                promptRequest.TryGetProperty("destination", out var destinationValue);
                promptRequest.TryGetProperty("check_in", out var checkInValue);
                promptRequest.TryGetProperty("check_out", out var checkOutValue);
                promptRequest.TryGetProperty("adults", out var adultsValue);
                promptRequest.TryGetProperty("rooms", out var roomValue);
                promptRequest.TryGetProperty("currency", out var currencyValue);
                promptRequest.TryGetProperty("sort_by", out var sortByValue);
                promptRequest.TryGetProperty("min_stars", out var minStarsValue);
                promptRequest.TryGetProperty("max_price", out var maxPriceValue);
                promptRequest.TryGetProperty("min_rating", out var minRatingValue);

                destination = destinationValue.GetString() ?? string.Empty;
                checkInDate = checkInValue.GetString() ?? string.Empty;
                checkOutDate = checkOutValue.GetString() ?? string.Empty;

                if (adultsValue.ValueKind == JsonValueKind.Number)
                    adults = adultsValue.GetInt32();

                if (roomValue.ValueKind == JsonValueKind.Number)
                    rooms = roomValue.GetInt32();

                if (currencyValue.ValueKind == JsonValueKind.Undefined)
                {
                    currency = DefaultCurrency;
                }
                
                sortBy = sortByValue.GetString();

                if (minStarsValue.ValueKind == JsonValueKind.Number)
                {
                    var s = minStarsValue.GetInt32();
                    starRatings = Enumerable.Range(s, 6 - s).ToArray(); // e.g. min_stars=4 → [4,5]
                }

                if (maxPriceValue.ValueKind == JsonValueKind.Number)
                    maxPrice = maxPriceValue.GetDouble();

                if (minRatingValue.ValueKind == JsonValueKind.Number)
                    minRating = minRatingValue.GetInt32();

                promptRequest.TryGetProperty("count", out var countValue);
                if (countValue.ValueKind == JsonValueKind.Number)
                    count = Math.Clamp(countValue.GetInt32(), 1, 10);
            }

            // required value for search destination
            if (string.IsNullOrWhiteSpace(destination))
            {
                return Ok(JsonRpcError(requestId, -32602, "Invalid params: 'destination' is required"));
            }
                
            if (!DateTime.TryParse(checkInDate,  out var checkIn))
            {
                return Ok(JsonRpcError(requestId, -32602, "Invalid params: 'check_in' must be YYYY-MM-DD"));
            }
                
            if (!DateTime.TryParse(checkOutDate, out var checkOut))
            {
                return Ok(JsonRpcError(requestId, -32602, "Invalid params: 'check_out' must be YYYY-MM-DD"));
            }

            var hotels = await _hotelService.SearchHotel(destination, checkIn, checkOut, adults, rooms, currency,
                sortBy, starRatings, maxPrice, minRating);

            var httpClient = _httpClientFactory.CreateClient("ImageProxy");
            var hotelResults = await Task.WhenAll(hotels.Take(count).Select(async x =>
            {
                // Fetch images as base64 data URIs — required because ChatGPT's iframe
                // CSP only allows img-src 'self' data:, blocking external CDN URLs.
                var imageUrl = string.Empty;
                var rawUrl = x?.ImageUrl ?? string.Empty;
                var formattedUrl = await FormattedImageUrl(rawUrl, httpClient, _logger);

                if (!string.IsNullOrEmpty(formattedUrl))
                {
                    imageUrl = formattedUrl;
                }

                return new
                {
                    id = x?.Id,
                    name = x?.Name ?? string.Empty,
                    location = x?.Location ?? string.Empty,
                    star_rating = x?.StarRating ?? 0,
                    guest_rating = x?.GuestRating ?? 0,
                    guest_rating_count = x?.GuestRatingCount ?? 0,
                    price = x?.Price ?? 0,
                    supplier_price = x?.SupplierPrice ?? 0,
                    strike_through_price = x?.StrikeThroughPrice ?? 0,
                    currency = x?.Currency ?? string.Empty,
                    image_url = imageUrl,
                    amenities = x?.Amenities ?? new List<string>(),
                    hotel_code = x?.HotelCode ?? string.Empty,
                    provider = x?.Provider ?? string.Empty
                };
            }));

            // Vercel preview link (fallback for plain-text clients)
            var widgetUrl = _configuration["WidgetUrl"] ?? "";
            var dataJson = JsonConvert.SerializeObject(
                new 
                { 
                    hotels = hotelResults 
                });

            var dataBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(dataJson));
            var widgetLink = $"{widgetUrl.TrimEnd('/')}/?data={dataBase64}";

            var responseText = $"## Hotels in {destination}\n\n{hotels}\n\n[View full hotel cards]({widgetLink})";

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
                    structuredContent = new
                    {
                        hotels = hotelResults,
                        context = new { check_in = checkInDate, check_out = checkOutDate, adults, rooms, currency },
                        api_url = $"{Request.Scheme}://{Request.Host}"
                    },
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

        private async Task<IActionResult> HandleGetHotelDetails(object? requestId, JsonElement paramsRoot)
        {
            var hotelId = string.Empty;
            var hotelCode = string.Empty;
            var provider = string.Empty;
            var checkInDate = string.Empty;
            var checkOutDate = string.Empty;
            var adults = 2;
            var rooms = 1;
            var currency = DefaultCurrency;

            if (paramsRoot.TryGetProperty("arguments", out var args))
            {
                args.TryGetProperty("hotel_id", out var hotelIdEl);
                args.TryGetProperty("hotel_code", out var hotelCodeEl);
                args.TryGetProperty("provider", out var providerEl);
                args.TryGetProperty("check_in", out var checkInEl);
                args.TryGetProperty("check_out", out var checkOutEl);
                args.TryGetProperty("adults", out var adultsEl);
                args.TryGetProperty("rooms", out var roomsEl);
                args.TryGetProperty("currency", out var currencyEl);

                hotelId = hotelIdEl.GetString() ?? string.Empty;
                hotelCode = hotelCodeEl.GetString() ?? string.Empty;
                provider = providerEl.GetString() ?? string.Empty;
                checkInDate = checkInEl.GetString() ?? string.Empty;
                checkOutDate = checkOutEl.GetString() ?? string.Empty;
                if (adultsEl.ValueKind == JsonValueKind.Number) adults = adultsEl.GetInt32();
                if (roomsEl.ValueKind == JsonValueKind.Number) rooms = roomsEl.GetInt32();
                currency = currencyEl.GetString() ?? DefaultCurrency;
            }

            if (string.IsNullOrWhiteSpace(hotelId))
                return Ok(JsonRpcError(requestId, -32602, "Invalid params: 'hotel_id' is required"));

            if (!DateTime.TryParse(checkInDate, out var checkIn))
                return Ok(JsonRpcError(requestId, -32602, "Invalid params: 'check_in' must be YYYY-MM-DD"));

            if (!DateTime.TryParse(checkOutDate, out var checkOut))
                return Ok(JsonRpcError(requestId, -32602, "Invalid params: 'check_out' must be YYYY-MM-DD"));

            var detail = await _hotelService.GetHotelDetails(
                hotelId,
                string.IsNullOrEmpty(hotelCode) ? null : hotelCode,
                string.IsNullOrEmpty(provider) ? null : provider,
                checkIn, checkOut, adults, rooms, currency);

            if (detail == null)
                return Ok(JsonRpcError(requestId, -32603, "Hotel details not found"));

            return Ok(new
            {
                jsonrpc = "2.0",
                id = requestId,
                result = new
                {
                    content = new[] { new { type = "text", text = $"Hotel details for {detail.Name}" } },
                    structuredContent = new { hotel_detail = detail },
                    _meta = new Dictionary<string, object>
                    {
                        ["openai/outputTemplate"]          = WidgetUri,
                        ["openai/toolInvocation/invoking"] = "Loading hotel details\u2026",
                        ["openai/toolInvocation/invoked"]  = "Hotel details ready"
                    }
                }
            });
        }

        // REST endpoint for the widget to fetch hotel details directly
        [HttpGet("hotel/details")]
        public async Task<IActionResult> GetHotelDetailsRest(
            [FromQuery] string hotel_id,
            [FromQuery] string? hotel_code,
            [FromQuery] string? provider,
            [FromQuery] string check_in,
            [FromQuery] string check_out,
            [FromQuery] int adults = 2,
            [FromQuery] int rooms = 1,
            [FromQuery] string currency = DefaultCurrency)
        {
            if (string.IsNullOrWhiteSpace(hotel_id))
                return BadRequest("hotel_id is required");

            if (!DateTime.TryParse(check_in, out var checkIn) || !DateTime.TryParse(check_out, out var checkOut))
                return BadRequest("check_in and check_out must be YYYY-MM-DD");

            var detail = await _hotelService.GetHotelDetails(
                hotel_id, hotel_code, provider, checkIn, checkOut, adults, rooms, currency);

            if (detail == null) return NotFound();
            return Ok(detail);
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
                            uri         = WidgetUri,
                            name        = "Hotel Search Widget",
                            mimeType    = "text/html+skybridge",   // required MIME type per OpenAI Apps SDK
                            description = "Interactive hotel search results card grid",
                            _meta = new Dictionary<string, object>
                            {
                                ["openai/outputTemplate"] = WidgetUri,
                                ["openai/widgetAccessible"] = true
                            }
                        }
                    }
                }
            });
        }

        private IActionResult HandleResourcesRead(McpRequest request)
        {
            var uri = "";
            if (request.Params.HasValue &&
                request.Params.Value.TryGetProperty("uri", out var uriEl))
            {
                uri = uriEl.GetString() ?? "";
            }

            // When openai/outputTemplate is an HTTPS URL, ChatGPT loads it directly
            // as an iframe and does not call resources/read. Keep this for compatibility.
            if (string.IsNullOrEmpty(uri))
                return Ok(JsonRpcError(request.Id, -32002, "Missing uri parameter"));

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
                            uri = uri,
                            mimeType = "text/html+skybridge",
                            text = GetWidgetHtml()
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

        private async Task<string> FormattedImageUrl(string rawrlUrl, HttpClient httpClient, ILogger logger, int timeSeconds = 8)
        {
            if (string.IsNullOrWhiteSpace(rawrlUrl))
                return string.Empty;

            var preferredUrl = Regex.Replace(rawrlUrl, @"_[a-z](\.[a-z]+)$", "_b$1");
            var imageSelected = preferredUrl != rawrlUrl
                ? new[] { preferredUrl, rawrlUrl }
                : new[] { rawrlUrl };

            foreach (var url in imageSelected)
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeSeconds));

                    var reqMsg = new HttpRequestMessage(HttpMethod.Get, url);
                    if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                        reqMsg.Headers.Referrer = new Uri($"{uri.Scheme}://{uri.Host}/");

                    var response = await httpClient.SendAsync(reqMsg, cts.Token);

                    if (!response.IsSuccessStatusCode)
                    {
                        logger.LogWarning("Image fetch returned {Status} for {Url}", (int)response.StatusCode, url);
                        continue;
                    }

                    var bytes = await response.Content.ReadAsByteArrayAsync();
                    var mime = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
                    logger.LogInformation("Image fetched OK ({Bytes} bytes, {Mime}) for {Url}", bytes.Length, mime, url);

                    return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
                }
                catch (Exception ex)
                {
                    logger.LogWarning("Image fetch failed for {Url}: {Msg}", url, ex.Message);
                }
            }

            // Base64 conversion failed — fall back to raw URL.
            // The widget's onerror handler will hide the broken img if the browser can't load it.
            logger.LogWarning("Falling back to raw URL for image: {Url}", rawrlUrl);
            return rawrlUrl;
        }
    }
}
