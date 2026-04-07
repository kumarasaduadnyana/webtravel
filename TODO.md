# Hackathon TODO — Hotel Search Widget (ChatGPT App)

## Stack
- **Widget**: React + TypeScript + Tailwind v4 (Vite, built to single HTML)
- **Map**: react-leaflet + Leaflet.js
- **MCP Server**: .NET 8 (existing)

---

## How React + Tailwind fits into ChatGPT Apps

ChatGPT renders your widget from a **single HTML file** served via MCP resource.
The trick: Vite builds your React app → `vite-plugin-singlefile` inlines all JS/CSS into one `index.html` → .NET reads that file and serves it as `ui://widgets/list-hotel.html`.

```
widget/src/  →  vite build  →  dist/index.html  →  copied to TravelAgent/Widgets/list-hotel.html
```

---

## Phase 1 — Widget Project Setup

- [x] Create `widget/` folder at repo root
- [x] Init Vite project: `npm create vite@latest . -- --template react-ts`
- [x] Install dependencies:
  ```bash
  npm install
  npm install -D tailwindcss@next @tailwindcss/vite
  npm install leaflet react-leaflet
  npm install -D @types/leaflet
  npm install -D vite-plugin-singlefile
  ```
- [x] Configure Tailwind v4 in `vite.config.ts`:
  ```ts
  import tailwindcss from '@tailwindcss/vite'
  import { viteSingleFile } from 'vite-plugin-singlefile'

  export default defineConfig({
    plugins: [react(), tailwindcss(), viteSingleFile()],
    build: { outDir: '../TravelAgent/Widgets', rollupOptions: { input: 'index.html' } }
  })
  ```
- [x] Add `@import "tailwindcss"` to `src/index.css`
- [x] Add npm build script in `widget/package.json`:
  ```json
  "scripts": {
    "dev": "vite",
    "build": "vite build --emptyOutDir false"
  }
  ```

---

## Phase 2 — Read ChatGPT App Data

ChatGPT injects tool result data into `window.__APP_DATA__` before rendering.

- [x] Create `src/types/hotel.ts` — TypeScript types matching `SearchHotelResponse` from the MCP tool:
  ```ts
  export interface Hotel {
    id: string
    name: string
    location: string
    rating: number
    starRating: number
    guestRating: number
    guestRatingCount: number
    price: number
    currency: string
    imageUrl: string
    amenities: string[]
    images: string[]
    latitude?: number   // needed for map — backend must add these
    longitude?: number
  }

  export interface SearchHotelResponse {
    hotels: Hotel[]
    meta: {
      destination: string
      checkIn: string
      checkOut: string
      currency: string
    }
  }
  ```
- [x] Create `src/hooks/useAppData.ts` to read `window.__APP_DATA__`:
  ```ts
  declare global {
    interface Window { __APP_DATA__: SearchHotelResponse }
  }
  export const useAppData = () => window.__APP_DATA__ ?? null
  ```

---

## Phase 3 — Hotel List View (Card Carousel)

Port the existing `Templates/hotel-widget.html` carousel to React + Tailwind.

- [x] Create `src/components/HotelCard.tsx` — single hotel card with image, name, price, rating stars
- [x] Create `src/components/HotelCarousel.tsx` — horizontal scroll carousel with prev/next arrows and dot indicators
- [x] Style with Tailwind v4 utility classes (replace existing hand-written CSS)
- [x] Handle missing images gracefully (emoji fallback 🏨)

---

## Phase 4 — Map View (Leaflet)

- [ ] Create `src/components/HotelMap.tsx`:
  - Full-height Leaflet map centered on destination
  - One `<Marker>` per hotel with lat/lng
  - `<Popup>` on each marker: hotel name, price/night, star rating, "View" link
  - Cluster markers if hotels are close together (optional: `react-leaflet-cluster`)
- [ ] Import Leaflet CSS in `src/index.css`:
  ```css
  @import 'leaflet/dist/leaflet.css';
  ```
- [ ] Fix Leaflet default icon path issue (common Vite/webpack problem):
  ```ts
  import L from 'leaflet'
  import markerIcon from 'leaflet/dist/images/marker-icon.png'
  import markerShadow from 'leaflet/dist/images/marker-shadow.png'
  delete (L.Icon.Default.prototype as any)._getIconUrl
  L.Icon.Default.mergeOptions({ iconUrl: markerIcon, shadowUrl: markerShadow })
  ```
- [ ] Add custom pin color/style for selected hotel (highlight on hover from card)

---

## Phase 5 — List / Map Toggle

- [ ] Create `src/components/ViewToggle.tsx` — "List" / "Map" tab buttons (styled with Tailwind)
- [ ] Wire toggle state in `src/App.tsx`:
  ```tsx
  const [view, setView] = useState<'list' | 'map'>('list')
  return (
    <>
      <ViewToggle value={view} onChange={setView} />
      {view === 'list' ? <HotelCarousel hotels={hotels} /> : <HotelMap hotels={hotels} />}
    </>
  )
  ```
- [ ] Sync selection: clicking a card in list highlights its pin on map (optional for hackathon)

---

## Phase 6 — Backend: Add Coordinates to Hotel Model

> This is backend work — coordinate with the MCP server developer.

- [ ] Add `Latitude` and `Longitude` to `TravelAgent/Models/Hotel.cs`
- [ ] Populate from search API response in `TravelAgent/Services/HotelService.cs`
- [ ] If API doesn't return coordinates: add a geocoding fallback (e.g. call Nominatim/OpenStreetMap with hotel name + location string)

---

## Phase 7 — Build Integration

- [ ] Add build step to `TravelAgent/TravelAgent.csproj` so `dotnet build` also builds the widget:
  ```xml
  <Target Name="BuildWidget" BeforeTargets="Build">
    <Exec Command="npm run build" WorkingDirectory="$(MSBuildProjectDirectory)/../widget" />
  </Target>
  ```
- [ ] Verify `Widgets/list-hotel.html` is auto-copied to output dir (already configured in `.csproj`)
- [ ] Test end-to-end: prompt ChatGPT → tool called → widget renders with map toggle

---

## Phase 8 — Polish (time permitting)

- [ ] Loading skeleton while data renders
- [ ] "No results" empty state
- [ ] Responsive layout (mobile-friendly within ChatGPT panel)
- [ ] Dark mode support (ChatGPT has dark mode)
- [ ] Animated map pin bounce on first load

---

## Key Files

| File | Purpose |
|------|---------|
| `widget/src/App.tsx` | Root component, view toggle logic |
| `widget/src/types/hotel.ts` | TypeScript types for MCP tool response |
| `widget/src/components/HotelCard.tsx` | Single hotel card |
| `widget/src/components/HotelCarousel.tsx` | Horizontal scrolling list |
| `widget/src/components/HotelMap.tsx` | Leaflet map with pins |
| `widget/src/components/ViewToggle.tsx` | List/Map tab switch |
| `widget/vite.config.ts` | Vite + singlefile + Tailwind v4 config |
| `TravelAgent/Widgets/list-hotel.html` | Build output (served by MCP resource) |
| `TravelAgent/MCP/Resources/WidgetResource.cs` | MCP resource serving the HTML |
| `TravelAgent/MCP/Tools/HotelAssistantTool.cs` | MCP tool with `ui` annotation |
| `TravelAgent/Models/Hotel.cs` | Add Latitude/Longitude here |
