# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build

# Run (development)
dotnet run

# Run with watch (hot reload)
dotnet watch run

# Restore packages
dotnet restore
```

The app starts on `https://localhost:7xxx` / `http://localhost:5xxx` (see `Properties/launchSettings.json`). Swagger UI is available at `/swagger` in development.

## Architecture Overview

This is an ASP.NET Core 8 Web API that serves as an **MCP (Model Context Protocol) server** for a travel booking assistant. It exposes hotel search and detail tools to AI clients (Claude, ChatGPT) via the MCP protocol.

### Dual MCP Implementation

There are **two parallel MCP implementations** — understand both before making changes:

1. **`/mcp` — Official SDK (primary)**: Uses the `ModelContextProtocol` NuGet package. Tools, resources, and prompts are defined via attributes in `MCP/Tools/`, `MCP/Resources/`, and `MCP/Prompts/`. Registered in `Program.cs` via `AddMcpServer().WithHttpTransport().WithToolsFromAssembly()...`. This is the active, production path.

2. **`/mcp-bkp` — Manual JSON-RPC (legacy)**: `Controllers/McpController.cs` implements the MCP protocol manually, hand-crafting JSON-RPC responses. Contains `searchHotels` and `getHotelDetails` tools with ChatGPT-specific `openai/*` metadata for widget rendering. Kept as fallback/reference.

### Key Data Flow

```
AI client → POST /mcp  →  MCP SDK  →  HotelAssistantTool  →  IHotelService
                                                              ├─ ITravlrSearchApiClient  (destination resolution + hotel search)
                                                              └─ IAccommodationApiClient (hotel detail / room rates)
```

`HotelService` does a **two-step search**:
1. Resolves the user's destination string to a canonical `FullName` via the Search API
2. Searches hotels with availability; falls back to a reference-price search if availability fails

### Widget System

AI clients that support OpenAI's Apps SDK render HTML widgets instead of plain text:

- **`Widgets/list-hotel.html`** — hotel search results card grid (served via `ui://widgets/list-hotel.html` MCP resource)
- **`Widgets/hotel-detail-page.html`** — hotel detail page
- **`Templates/hotel-widget.html`** / **`Templates/hotel-detail-page.html`** — legacy templates used by the backup `McpController`

Widget HTML files are copied to the output directory at build time (see `.csproj`).

### Image Handling

- `Controllers/ImageProxyController.cs` (`GET /proxy/image?url=...`) proxies hotel images from a whitelist of trusted CDNs, cached for 24 hours.
- In the legacy MCP controller, images are fetched and base64-encoded inline at search time to work around ChatGPT's CSP restrictions (`connect-src: none`).

### External Services

Configured in `appsettings.json`:

| Key | Purpose |
|-----|---------|
| `SearchService:BaseUrl` | Travlr search API (destination lookup + hotel search) |
| `AccommodationService:BaseUrl` | Accommodation API (room rates, hotel details) |
| `SearchService:ApiKey` | Optional API key injected via `TravlrSearchApiClient.SetApiKey()` |
| `WidgetUrl` | Vercel widget deployment URL (used for plain-text fallback links) |

Both API clients are NSwag-generated in `Services/SearchService/ApiClient/` and `Services/AccommodationService/ApiClient/`.

### Hotel ID Format

Hotel IDs from the search API can be composite: `contentId|Provider-PropertyId` (e.g. `trv-app-dev-...|Expedia-9832462`). `HotelService.GetHotelDetails` splits on `|` and parses provider/hotelCode before calling the accommodation API.
