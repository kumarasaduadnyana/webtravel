import './index.css'
import HotelCarousel from './components/HotelCarousel'
import { useMcpApp } from './hooks/useMcpApp'
import { useHostStyles } from '@modelcontextprotocol/ext-apps/react'

function calcNights(checkIn: string, checkOut: string): number {
  const diff = new Date(checkOut).getTime() - new Date(checkIn).getTime()
  return Math.max(1, Math.round(diff / 86_400_000))
}

function App() {
  const { app, hotels, meta, subtitle, loading, error } = useMcpApp()

  // Syncs host theme (light/dark) + CSS variables to document.
  // This sets data-theme="dark" on <html> which activates Tailwind dark: classes.
  useHostStyles(app, app?.getHostContext())

  const nights = meta ? calcNights(meta.checkIn, meta.checkOut) : 1

  if (loading) {
    return (
      <div className="flex items-center justify-center h-40 text-sm text-muted-foreground">
        Loading hotels…
      </div>
    )
  }

  if (error && !hotels.length) {
    return (
      <div className="flex items-center justify-center h-40 text-sm text-muted-foreground">
        Could not connect to host.
      </div>
    )
  }

  if (!hotels.length) {
    return (
      <div className="flex items-center justify-center h-40 text-sm text-muted-foreground">
        No hotels found for this search.
      </div>
    )
  }

  return (
    <div className="p-4 bg-transparent">
      {subtitle && (
        <p className="text-sm text-muted-foreground mb-3">{subtitle}</p>
      )}
      <HotelCarousel hotels={hotels} nights={nights} />
    </div>
  )
}

export default App
