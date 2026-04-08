# Research: Separating Widget and MCP into Different Repositories

**Date**: 2026-04-08  
**Requested by**: Senior Engineer  
**Question**: Can the widget and MCP server be placed in separate repositories? Is it feasible to deploy the widget on Vercel as a standalone web app (using React Router to differentiate widget sources) and use the Vercel URL as the widget source in the MCP?

---

## TL;DR

**Yes — it is feasible and well-supported by the current architecture.** The widget is already nearly self-contained. The MCP server already has a placeholder `WidgetUrl` config key pointing to a Vercel URL. The separation requires roughly 3 focused changes: a standalone Vite build config, a Vercel deploy, and updating the `WidgetResource.cs` handler to fetch from a URL instead of the local filesystem.

---

## 1. Current Architecture Overview

```
webtravel/
├── widget/                  ← React 19 + Vite + TypeScript (widget source)
│   ├── vite.config.ts       ← builds single-file HTML into TravelAgent/Widgets/
│   └── src/
│       ├── App.tsx
│       ├── main.tsx
│       ├── hooks/useMcpApp.ts        ← MCP ext-apps SDK integration
│       └── components/
│           ├── HotelCard.tsx
│           └── HotelCarousel.tsx
├── TravelAgent/             ← .NET 8 MCP Server (ASP.NET Core)
│   ├── MCP/
│   │   ├── Tools/HotelAssistantTool.cs    ← defines search_hotel tool
│   │   └── Resources/WidgetResource.cs    ← serves ui://widgets/list-hotel.html
│   ├── Widgets/                           ← build output from widget/
│   │   └── list-hotel.html               ← 331 KB self-contained HTML bundle
│   └── appsettings.json                  ← has WidgetUrl = "https://webtravel-olive.vercel.app/"
└── hotel-widget/            ← minimal Vercel placeholder (currently unused)
    ├── index.html
    └── vercel.json          ← CORS/CSP headers already configured
```

### How the Widget Works Today

1. `npm run build` (inside `widget/`) runs Vite with `vite-plugin-singlefile`
2. Output: a single self-contained `list-hotel.html` written to `TravelAgent/Widgets/`
3. At runtime, `WidgetResource.cs` reads that file from disk and serves it as `text/html;profile=mcp-app` under the resource URI `ui://widgets/list-hotel.html`
4. The MCP tool (`search_hotel`) references that resource URI via `[McpMeta("ui/resourceUri", ...)]`
5. The MCP client (e.g., ChatGPT) fetches the resource, renders the HTML, and the widget initializes React + connects to the MCP host via `@modelcontextprotocol/ext-apps`

### Current Tight Coupling

| Coupling Point | Location | Impact if Separated |
|---|---|---|
| Widget build outputs to `TravelAgent/Widgets/` | `vite.config.ts` `outDir` | Must change output dir + update deployment |
| `WidgetResource.cs` reads HTML from filesystem | `WidgetResource.cs` | Must fetch from URL instead |
| Widget types mirror C# models | `src/types/hotel.ts` | Must be kept in sync manually |
| No client-side routing | No React Router installed | Must add React Router if multi-widget |

---

## 2. Proposed Separated Architecture

```
┌──────────────────────────────────────────┐
│  Repo A: widget (deployed on Vercel)     │
│                                          │
│  React 19 + Vite + TypeScript            │
│  React Router (optional, see §4)         │
│  Tailwind CSS v4                         │
│  @modelcontextprotocol/ext-apps          │
│                                          │
│  Routes:                                 │
│    /list-hotel   → HotelListWidget       │
│    /hotel-detail → HotelDetailWidget     │
│    /activities   → ActivitiesWidget      │
│    /car-rental   → CarRentalWidget       │
│                                          │
│  Deployed URL:                           │
│  https://widget.travlr.app/              │
└──────────────────────────────────────────┘
              ↕  HTTP GET (fetch widget HTML)
              ↕  MCP protocol (tool results → widget)
┌──────────────────────────────────────────┐
│  Repo B: TravelAgent (MCP Server)        │
│                                          │
│  .NET 8 / ASP.NET Core                   │
│  ModelContextProtocol 1.2.0              │
│                                          │
│  WidgetResource.cs:                      │
│    fetches HTML from Vercel URL          │
│    (or returns redirect to Vercel URL)   │
│                                          │
│  appsettings.json:                       │
│    "WidgetUrl": "https://widget.travlr.app"
└──────────────────────────────────────────┘
```

---

## 3. What Needs to Change

### 3.1 Widget Repo — Build Config

**Current** `vite.config.ts`:
```typescript
build: {
  outDir: "../TravelAgent/Widgets",  // outputs to MCP server directory
  rollupOptions: { input: "list-hotel.html" }
}
```

**Changed** `vite.config.ts` (for separate repo):
```typescript
build: {
  outDir: "dist",                   // standard output
  // Remove viteSingleFile plugin — not needed for URL-served build
  rollupOptions: {
    input: {
      "list-hotel": "list-hotel.html",
      // add more entries as new widgets are added
    }
  }
}
```

> **Note**: If using React Router (SPA), the build becomes a standard Vite SPA build. The single-file plugin is no longer needed because the file is served over HTTP, not embedded in a C# resource.

### 3.2 Widget Repo — Vercel Config

The `hotel-widget/vercel.json` already has the right CORS/CSP headers. Move to the widget repo root:

```json
{
  "headers": [
    {
      "source": "/(.*)",
      "headers": [
        { "key": "X-Frame-Options", "value": "ALLOWALL" },
        { "key": "Content-Security-Policy", "value": "frame-ancestors *;" },
        { "key": "Access-Control-Allow-Origin", "value": "*" }
      ]
    }
  ],
  "rewrites": [
    { "source": "/(.*)", "destination": "/index.html" }
  ]
}
```

The `rewrites` rule enables SPA routing (React Router works correctly on hard refresh/direct URL).

### 3.3 MCP Server — WidgetResource.cs

**Current** (reads from filesystem):
```csharp
[McpServerResource(UriTemplate = "ui://widgets/list-hotel.html")]
public async Task<ResourceContents> GetHotelListWidget() {
    var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                "Widgets", "list-hotel.html");
    var content = await File.ReadAllTextAsync(filePath);
    return new TextResourceContents {
        Uri = "ui://widgets/list-hotel.html",
        MimeType = "text/html;profile=mcp-app",
        Text = content
    };
}
```

**Option A — Fetch and Proxy** (widget HTML still returned inline):
```csharp
[McpServerResource(UriTemplate = "ui://widgets/list-hotel.html")]
public async Task<ResourceContents> GetHotelListWidget() {
    var widgetUrl = configuration["WidgetUrl"]!.TrimEnd('/') + "/list-hotel";
    var content = await httpClient.GetStringAsync(widgetUrl);
    return new TextResourceContents {
        Uri = "ui://widgets/list-hotel.html",
        MimeType = "text/html;profile=mcp-app",
        Text = content
    };
}
```

**Option B — Return Vercel URL directly** (MCP client fetches from Vercel):
```csharp
// Instead of returning the full HTML, return just the URL as the resource
// (depends on whether MCP client supports external resource URIs)
return new TextResourceContents {
    Uri = configuration["WidgetUrl"] + "/list-hotel",
    MimeType = "text/html;profile=mcp-app",
    Text = ""  // client follows the URI
};
```

> **Recommendation**: Use **Option A** (proxy) for maximum compatibility. MCP clients today (ChatGPT, Claude) expect the HTML to be returned in the MCP resource response — not an external redirect. Caching can be added to avoid fetching on every request.

### 3.4 MCP Server — appsettings.json

Already has the placeholder — just update to the real Vercel URL:
```json
{
  "WidgetUrl": "https://your-widget.vercel.app"
}
```

---

## 4. React Router for Multi-Widget Routing

### Why React Router?

If there are multiple widgets (hotel list, hotel detail, activities, car rental), React Router allows:
- A single Vercel deployment serving all widgets at different paths
- The MCP server references the appropriate path per tool
- Shared components and styles across all widgets in one codebase

### Route Structure

```tsx
// src/main.tsx
import { createBrowserRouter, RouterProvider } from "react-router-dom"

const router = createBrowserRouter([
  { path: "/list-hotel",   element: <HotelListWidget /> },
  { path: "/hotel-detail", element: <HotelDetailWidget /> },
  { path: "/activities",   element: <ActivitiesWidget /> },
  { path: "/car-rental",   element: <CarRentalWidget /> },
])

createRoot(document.getElementById("root")!).render(
  <RouterProvider router={router} />
)
```

### MCP Tool → Widget Route Mapping

| MCP Tool | Widget Path | Resource URI |
|---|---|---|
| `search_hotel` | `/list-hotel` | `ui://widgets/list-hotel` |
| `get_hotel_detail` | `/hotel-detail` | `ui://widgets/hotel-detail` |
| `search_activities` | `/activities` | `ui://widgets/activities` |
| `search_car_rental` | `/car-rental` | `ui://widgets/car-rental` |

Each `[McpMeta("ui/resourceUri", "...")]` attribute on the C# tool just points to the corresponding route URL.

### Alternative: Hash-based Routing (Simpler)

If React Router feels heavy, hash routing also works:
- `https://widget.vercel.app/#/list-hotel`
- `https://widget.vercel.app/#/hotel-detail`

No need for Vercel rewrites — hash routing is purely client-side.

---

## 5. Data Contract Synchronization

With separate repos, the TypeScript `Hotel`/`SearchHotelResponse` types and C# models must stay in sync. Three options:

| Option | Pros | Cons |
|---|---|---|
| **Manual sync** (current approach, just in different repos) | Zero tooling overhead | Drift risk; no enforcement |
| **Shared JSON Schema** | One source of truth; both can validate | Extra tooling setup |
| **OpenAPI codegen** | Fully automated from .NET API | C# must expose OpenAPI endpoint |

> **Recommendation**: Start with manual sync (acceptable for small team/rapid iteration). Add a JSON Schema or OpenAPI contract later if drift becomes a problem. The current C# models and TS interfaces are already well-aligned.

---

## 6. Deployment & CI/CD Considerations

### Widget (Vercel)

```yaml
# .github/workflows/widget.yml
on:
  push:
    branches: [main]
    paths: ["widget/**"]

jobs:
  deploy:
    steps:
      - run: npm install && npm run build
        working-directory: widget
      - uses: amondnet/vercel-action@v25
        with:
          vercel-token: ${{ secrets.VERCEL_TOKEN }}
          vercel-org-id: ${{ secrets.VERCEL_ORG_ID }}
          vercel-project-id: ${{ secrets.VERCEL_PROJECT_ID }}
          working-directory: widget
```

### MCP Server

No change from current deploy pipeline. Just ensure `WidgetUrl` is set to the production Vercel URL via environment variable or secrets.

---

## 7. Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Widget URL unavailable when MCP fetches it | Low | High | Add HTTP retry + fallback to cached version in `WidgetResource.cs` |
| Type drift between TS and C# models | Medium | Medium | Document contract, add integration test that validates JSON shape |
| MCP client CORS issues loading Vercel-hosted widget | Low | High | Already handled by `vercel.json` CORS headers |
| Vercel cold start latency on first fetch | Low | Low | Widget HTML is static; Vercel Edge Network serves it fast |
| Breaking widget URL when re-deploying Vercel | Low | High | Use stable production domain (custom domain or Vercel alias), not the auto-generated URL |

---

## 8. Phased Migration Plan

### Phase 1 — No-risk Proof of Concept (1–2 days)
1. Copy `widget/` into a new standalone repo
2. Update `vite.config.ts` to output to `dist/` (remove `viteSingleFile`)
3. Deploy to Vercel
4. Verify the app loads correctly at the Vercel URL

### Phase 2 — Wire MCP to Vercel URL (1 day)
1. Update `WidgetResource.cs` to fetch from `configuration["WidgetUrl"]`
2. Test end-to-end: `search_hotel` → MCP returns HTML from Vercel → widget renders in client

### Phase 3 — Add React Router (1–2 days)
1. Install `react-router-dom`
2. Create route structure matching planned widgets
3. Update `[McpMeta]` attributes in C# tools to point to route paths
4. Test each widget route independently

### Phase 4 — Clean Up (0.5 day)
1. Remove `TravelAgent/Widgets/` directory from MCP repo
2. Remove widget build step from MCP CI/CD
3. Update documentation

---

## 9. Conclusion

| Question | Answer |
|---|---|
| Can widget and MCP be in separate repos? | **Yes** |
| Can widget be deployed on Vercel? | **Yes — trivially, CORS config already exists** |
| Can React Router differentiate widget sources? | **Yes — one Vercel deployment, multiple routes** |
| Can MCP use the Vercel URL as widget source? | **Yes — `appsettings.json` already has the `WidgetUrl` key** |
| How much work is required? | **~3–5 days total (including testing)** |
| What is the main risk? | **Type drift between TS and C# models** |

The architecture is well-suited for this separation. The groundwork (`WidgetUrl` config, CORS headers in `vercel.json`, self-contained widget bundle) was already partially laid. The main implementation effort is updating `WidgetResource.cs` to fetch over HTTP instead of reading from disk, and reconfiguring the Vite build output.
