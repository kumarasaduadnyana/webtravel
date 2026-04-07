import './index.css'
import HotelCarousel from './components/HotelCarousel'
import { useMcpApp } from './hooks/useMcpApp'

function calcNights(checkIn: string, checkOut: string): number {
  const diff = new Date(checkOut).getTime() - new Date(checkIn).getTime()
  return Math.max(1, Math.round(diff / 86_400_000))
}

function App() {
  const { hotels, meta, subtitle, loading, error } = useMcpApp()
  const nights = meta ? calcNights(meta.checkIn, meta.checkOut) : 1

  if (loading) {
    return (
      <div className="flex items-center justify-center h-40 text-gray-400 text-sm">
        Loading hotels…
      </div>
    )
  }

  if (error && !hotels.length) {
    return (
      <div className="flex items-center justify-center h-40 text-gray-400 text-sm">
        Could not connect to host.
      </div>
    )
  }

  if (!hotels.length) {
    return (
      <div className="flex items-center justify-center h-40 text-gray-400 text-sm">
        No hotels found for this search.
      </div>
    )
  }

  return (
    <div className="bg-white p-4">
      {subtitle && (
        <p className="text-sm text-gray-500 mb-3">{subtitle}</p>
      )}
      <HotelCarousel hotels={hotels} nights={nights} />
    </div>
  )
}

export default App
