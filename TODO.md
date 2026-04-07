# Hackathon TODO — Hotel Search Widget (ChatGPT App)

## Stack
- **Widget**: React + TypeScript + Tailwind v4 (Vite, built to single HTML)
- **Map**: react-leaflet + Leaflet.js
- **MCP Server**: .NET 8 (existing)

---

## How React + Tailwind fits into ChatGPT Apps

ChatGPT renders your widget from a **single HTML file** served via MCP resource.
The trick: Vite builds your React app → `vite-plugin-singlefile` inlines all JS/CSS into one `list-hotel.html` → .NET reads that file and serves it as `ui://widgets/list-hotel.html`.

```
widget/src/  →  vite build  →  TravelAgent/Widgets/list-hotel.html
```

---

## Phase 1 — Widget Project Setup ✅

- [x] Create `widget/` folder at repo root
- [x] Init Vite + React + TypeScript project
- [x] Install dependencies (Tailwind v4, react-leaflet, vite-plugin-singlefile, @modelcontextprotocol/ext-apps)
- [x] Configure `vite.config.ts` — output to `TravelAgent/Widgets/list-hotel.html`
- [x] Tailwind v4 setup with `@import "tailwindcss"` in `index.css`

---

## Phase 2 — MCP App Data Integration ✅

> Replaced `window.__APP_DATA__` approach with official `@modelcontextprotocol/ext-apps` SDK.

- [x] Create `src/types/hotel.ts` — TypeScript types matching `SearchHotelResponse`
- [x] ~~`useAppData.ts`~~ → replaced by `useMcpApp.ts`
- [x] `src/hooks/useMcpApp.ts` — connects via `useApp()`, listens to `ontoolresult`, parses `structuredContent` with text fallback (mirrors Alpine.js logic)
- [x] Dev mock data pre-loaded when `import.meta.env.DEV` — no mock in production build
- [x] `App.tsx` calls `useHostStyles(app)` to sync MCP host theme to document

---

## Phase 3 — Hotel List View (Card Carousel) ✅

- [x] `src/components/HotelCard.tsx` — image, name, location, stars, guest rating, price, VIP price, CTA button
- [x] `src/components/HotelCarousel.tsx` — horizontal snap scroll, prev/next arrows
- [x] `ResizeObserver` on scroll container — cards fill available width with zero cut-off or partial cards
- [x] Scroll step synced to dynamic card width
- [x] Missing image fallback (🏨 emoji)
- [x] Tailwind v4 utility classes throughout

---

## Phase 3b — Theming ✅

- [x] CSS variable design token system (`--card`, `--primary`, `--muted`, `--border`, etc.)
- [x] `@theme inline` block maps tokens to Tailwind utilities (`bg-card`, `text-foreground`, etc.)
- [x] `@custom-variant dark ([data-theme="dark"] &)` — dark mode triggered by MCP SDK
- [x] Dark mode fully wired: `useHostStyles` → `applyDocumentTheme("dark")` → CSS vars switch → Tailwind `dark:` classes activate
- [x] Transparent outer background — widget blends with any host (ChatGPT, MCP Inspector)

---

## Phase 4 — Map View (Leaflet)

- [ ] Create `src/components/HotelMap.tsx`:
  - Full-height Leaflet map centered on destination
  - One `<Marker>` per hotel with lat/lng
  - `<Popup>` on each marker: hotel name, price/night, star rating
  - Cluster markers if hotels are close together (optional: `react-leaflet-cluster`)
- [ ] Import Leaflet CSS in `src/index.css`:
  ```css
  @import 'leaflet/dist/leaflet.css';
  ```
- [ ] Fix Leaflet default icon path issue (common Vite problem):
  ```ts
  import L from 'leaflet'
  import markerIcon from 'leaflet/dist/images/marker-icon.png'
  import markerShadow from 'leaflet/dist/images/marker-shadow.png'
  delete (L.Icon.Default.prototype as any)._getIconUrl
  L.Icon.Default.mergeOptions({ iconUrl: markerIcon, shadowUrl: markerShadow })
  ```

---

## Phase 5 — List / Map Toggle

- [ ] Create `src/components/ViewToggle.tsx` — "List" / "Map" tab buttons
- [ ] Wire toggle state in `src/App.tsx`
- [ ] Sync: clicking a card highlights its pin on map (optional)

---

## Phase 6 — Backend: Add Coordinates to Hotel Model

> Backend work — coordinate with MCP server developer.

- [ ] Add `Latitude` and `Longitude` to `TravelAgent/Models/Hotel.cs`
- [ ] Populate from search API response in `TravelAgent/Services/HotelService.cs`
- [ ] If API doesn't return coordinates: geocoding fallback via Nominatim

---

## Phase 7 — Build Integration

- [ ] Add build step to `TravelAgent/TravelAgent.csproj` so `dotnet build` also builds the widget:
  ```xml
  <Target Name="BuildWidget" BeforeTargets="Build">
    <Exec Command="npm run build" WorkingDirectory="$(MSBuildProjectDirectory)/../widget" />
  </Target>
  ```
- [ ] Test end-to-end: ChatGPT prompt → tool called → widget renders with map toggle

---

## Phase 8 — Polish (time permitting)

- [ ] Loading skeleton while waiting for tool result
- [ ] Animated map pin bounce on first load
- [ ] Cross-highlight: hover card ↔ highlight map pin

---

## Key Files

| File | Purpose |
|------|---------|
| `widget/src/App.tsx` | Root — wires `useMcpApp`, `useHostStyles`, view toggle |
| `widget/src/types/hotel.ts` | TypeScript types for MCP tool response |
| `widget/src/hooks/useMcpApp.ts` | MCP ext-apps connection + tool result parsing |
| `widget/src/index.css` | Tailwind v4 + CSS design tokens + dark variant |
| `widget/src/components/HotelCard.tsx` | Single hotel card |
| `widget/src/components/HotelCarousel.tsx` | Responsive carousel with ResizeObserver |
| `widget/src/components/HotelMap.tsx` | _(next)_ Leaflet map with pins |
| `widget/src/components/ViewToggle.tsx` | _(next)_ List/Map tab switch |
| `widget/vite.config.ts` | Vite + singlefile + Tailwind v4 config |
| `TravelAgent/Widgets/list-hotel.html` | Build output served by MCP resource |
| `TravelAgent/MCP/Resources/WidgetResource.cs` | MCP resource serving the HTML |
| `TravelAgent/MCP/Tools/HotelAssistantTool.cs` | MCP tool with `ui` annotation |
| `TravelAgent/Models/Hotel.cs` | Add `Latitude`/`Longitude` here (Phase 6) |
