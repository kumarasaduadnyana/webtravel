import { useState, useCallback } from 'react'
import { useApp } from '@modelcontextprotocol/ext-apps/react'
import type { App } from '@modelcontextprotocol/ext-apps'
import type { Hotel, SearchHotelResponse } from '../types/hotel'

// Mock data only used when running locally (npm run dev), replaced by real
// tool results in production (ChatGPT / any MCP host)
const DEV_MOCK: SearchHotelResponse | null = import.meta.env.DEV
  ? {
      hotels: [
        {
          id: '1',
          name: 'Sofitel Bali Nusa Dua Beach Resort',
          location: 'Nusa Dua',
          rating: 5,
          starRating: 5,
          guestRating: 8.6,
          guestRatingCount: 985,
          price: 5113240,
          currency: 'AUD',
          imageUrl:
            'https://images.unsplash.com/photo-1602002418082-a4443e081dd1?w=600&q=80',
          amenities: ['Pool', 'Spa', 'Beach'],
          images: [],
        },
        {
          id: '2',
          name: 'The ONE Legian',
          location: 'Kuta',
          rating: 4,
          starRating: 4,
          guestRating: 7.6,
          guestRatingCount: 993,
          price: 497233,
          currency: 'AUD',
          imageUrl:
            'https://images.unsplash.com/photo-1520250497591-112f2f40a3f4?w=600&q=80',
          amenities: ['Pool', 'Gym'],
          images: [],
        },
        {
          id: '3',
          name: 'Grand Mirage Resort & Thalasso Bali',
          location: 'Nusa Dua',
          rating: 4,
          starRating: 4,
          guestRating: 8.0,
          guestRatingCount: 1002,
          price: 3352493,
          currency: 'AUD',
          imageUrl:
            'https://images.unsplash.com/photo-1571896349842-33c89424de2d?w=600&q=80',
          amenities: ['Pool', 'Beach', 'Spa'],
          images: [],
        },
        {
          id: '4',
          name: 'COMO Uma Canggu',
          location: 'Canggu',
          rating: 5,
          starRating: 5,
          guestRating: 9.1,
          guestRatingCount: 412,
          price: 4200000,
          currency: 'AUD',
          imageUrl:
            'https://images.unsplash.com/photo-1455587734955-081b22074882?w=600&q=80',
          amenities: ['Pool', 'Yoga', 'Spa'],
          images: [],
        },
      ],
      meta: {
        destination: 'Bali',
        checkIn: '2026-04-10',
        checkOut: '2026-04-13',
        currency: 'AUD',
      },
    }
  : null

// Mirror of Alpine.js setResults() logic: supports both the full
// SearchHotelResponse shape and bare hotel arrays
function extractHotels(data: unknown): { hotels: Hotel[]; meta: SearchHotelResponse['meta'] | null; subtitle: string } {
  let hotels: Hotel[] = []
  let meta: SearchHotelResponse['meta'] | null = null
  let subtitle = ''

  if (Array.isArray(data)) {
    hotels = data as Hotel[]
  } else if (data && typeof data === 'object') {
    const d = data as Record<string, unknown>
    hotels = (d.hotels ?? d.results ?? d.items ?? []) as Hotel[]
    meta = (d.meta as SearchHotelResponse['meta']) ?? null
    const dest =
      (d.meta as Record<string, unknown>)?.destination ??
      d.destination ??
      d.location
    if (dest) {
      subtitle = `${dest} · ${hotels.length} propert${hotels.length === 1 ? 'y' : 'ies'}`
    }
  }

  return { hotels, meta, subtitle }
}

// Mirror of Alpine.js ontoolresult parser:
// prefer structuredContent, fall back to parsing the first text content block
function parseResult(result: unknown): unknown | null {
  const r = result as Record<string, unknown>
  if (r?.structuredContent != null) return r.structuredContent

  try {
    const blocks = r?.content as Array<{ type: string; text: string }> | undefined
    const text = blocks?.find((c) => c.type === 'text')?.text
    return text ? JSON.parse(text) : null
  } catch {
    return null
  }
}

export interface McpAppState {
  app: App | null
  hotels: Hotel[]
  meta: SearchHotelResponse['meta'] | null
  subtitle: string
  /** true while waiting for the first tool result */
  loading: boolean
  isConnected: boolean
  error: Error | null
}

export function useMcpApp(): McpAppState {
  // Dev: pre-populate with mock so the UI is visible immediately.
  // Prod: empty; populated when ontoolresult fires.
  const [hotels, setHotels] = useState<Hotel[]>(DEV_MOCK?.hotels ?? [])
  const [meta, setMeta] = useState<SearchHotelResponse['meta'] | null>(
    DEV_MOCK?.meta ?? null,
  )
  const [subtitle, setSubtitle] = useState('')
  const [loading, setLoading] = useState(DEV_MOCK === null)

  const onAppCreated = useCallback((app: App) => {
    app.ontoolresult = (result) => {
      const raw = parseResult(result)
      if (!raw) return

      const { hotels, meta, subtitle } = extractHotels(raw)
      setHotels(hotels)
      setMeta(meta)
      setSubtitle(subtitle)
      setLoading(false)
    }
  }, [])

  const { app, isConnected, error } = useApp({
    appInfo: { name: 'Travlr Booking Assistant', version: '1.0.0' },
    capabilities: {},
    onAppCreated,
  })

  return { app, hotels, meta, subtitle, loading, isConnected, error }
}
