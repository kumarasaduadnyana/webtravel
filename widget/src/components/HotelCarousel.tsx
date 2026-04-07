import { useRef, useState, useEffect, useCallback } from 'react'
import HotelCard from './HotelCard'
import type { Hotel } from '../types/hotel'

interface Props {
  hotels: Hotel[]
  nights: number
}

const CARD_WIDTH = 290
const GAP = 16
const SCROLL_STEP = CARD_WIDTH + GAP

export default function HotelCarousel({ hotels, nights }: Props) {
  const scrollRef = useRef<HTMLDivElement>(null)
  const [canPrev, setCanPrev] = useState(false)
  const [canNext, setCanNext] = useState(hotels.length > 1)

  const updateButtons = useCallback(() => {
    const el = scrollRef.current
    if (!el) return
    setCanPrev(el.scrollLeft > 2)
    setCanNext(el.scrollLeft + el.clientWidth < el.scrollWidth - 2)
  }, [])

  useEffect(() => {
    const el = scrollRef.current
    if (!el) return
    updateButtons()
    el.addEventListener('scroll', updateButtons, { passive: true })
    window.addEventListener('resize', updateButtons)
    return () => {
      el.removeEventListener('scroll', updateButtons)
      window.removeEventListener('resize', updateButtons)
    }
  }, [updateButtons])

  const scroll = (dir: 'prev' | 'next') => {
    scrollRef.current?.scrollBy({
      left: dir === 'next' ? SCROLL_STEP : -SCROLL_STEP,
      behavior: 'smooth',
    })
  }

  return (
    <div className="relative flex items-start gap-2 px-1">
      {/* Left arrow */}
      <button
        onClick={() => scroll('prev')}
        disabled={!canPrev}
        aria-label="Previous hotels"
        className="flex-shrink-0 mt-[105px] w-8 h-8 rounded bg-gray-200 text-gray-600 text-xl
                   flex items-center justify-center
                   hover:bg-gray-300 disabled:opacity-25 disabled:cursor-not-allowed
                   transition-all duration-150 cursor-pointer"
      >
        ‹
      </button>

      {/* Scrollable cards */}
      <div
        ref={scrollRef}
        className="hide-scrollbar flex gap-4 overflow-x-auto snap-x snap-mandatory flex-1 pb-1"
      >
        {hotels.map((hotel) => (
          <HotelCard key={hotel.id} hotel={hotel} nights={nights} />
        ))}
      </div>

      {/* Right arrow */}
      <button
        onClick={() => scroll('next')}
        disabled={!canNext}
        aria-label="Next hotels"
        className="flex-shrink-0 mt-[105px] w-8 h-8 rounded bg-gray-200 text-gray-600 text-xl
                   flex items-center justify-center
                   hover:bg-gray-300 disabled:opacity-25 disabled:cursor-not-allowed
                   transition-all duration-150 cursor-pointer"
      >
        ›
      </button>
    </div>
  )
}
