import { useRef, useState, useEffect, useCallback } from "react";
import HotelCard from "./HotelCard";
import type { Hotel } from "../types/hotel";

interface Props {
  hotels: Hotel[];
  nights: number;
}

const GAP = 16;
const MAX_CARD = 290;

/**
 * Given the scroll container's visible width, compute a card width so that
 * only complete cards are shown — no partial/cut-off cards.
 *
 * Steps:
 *   1. Count how many MAX_CARD-wide cards fit: n = floor((available + gap) / (MAX_CARD + gap))
 *   2. Stretch all n cards to fill the full available width evenly
 */
function calcCardWidth(available: number): number {
  const n = Math.max(1, Math.floor((available + GAP) / (MAX_CARD + GAP)));
  return (available - GAP * (n - 1)) / n;
}

export default function HotelCarousel({ hotels, nights }: Props) {
  const scrollRef = useRef<HTMLDivElement>(null);
  const [canPrev, setCanPrev] = useState(false);
  const [canNext, setCanNext] = useState(hotels.length > 1);
  const [cardWidth, setCardWidth] = useState(290);

  // Recompute card width whenever the scroll container resizes
  useEffect(() => {
    const el = scrollRef.current;
    if (!el) return;

    const ro = new ResizeObserver(() => {
      setCardWidth(calcCardWidth(el.clientWidth));
    });

    ro.observe(el);
    return () => ro.disconnect();
  }, []);

  const updateButtons = useCallback(() => {
    const el = scrollRef.current;
    if (!el) return;
    setCanPrev(el.scrollLeft > 2);
    setCanNext(el.scrollLeft + el.clientWidth < el.scrollWidth - 2);
  }, []);

  useEffect(() => {
    const el = scrollRef.current;
    if (!el) return;
    updateButtons();
    el.addEventListener("scroll", updateButtons, { passive: true });
    window.addEventListener("resize", updateButtons);
    return () => {
      el.removeEventListener("scroll", updateButtons);
      window.removeEventListener("resize", updateButtons);
    };
  }, [updateButtons]);

  const scroll = (dir: "prev" | "next") => {
    scrollRef.current?.scrollBy({
      left: dir === "next" ? cardWidth + GAP : -(cardWidth + GAP),
      behavior: "smooth",
    });
  };

  return (
    <div className="relative flex items-start gap-2 px-1">
      {/* Left arrow */}
      <button
        onClick={() => scroll("prev")}
        disabled={!canPrev}
        aria-label="Previous hotels"
        className="flex-shrink-0 my-auto size-12 rounded-full
                   bg-muted text-muted-foreground text-xl
                   flex items-center justify-center
                   hover:bg-border
                   disabled:opacity-25 disabled:cursor-not-allowed
                   transition-colors duration-150 cursor-pointer"
      >
        ‹
      </button>

      {/* Scrollable cards */}
      <div
        ref={scrollRef}
        className="hide-scrollbar flex gap-4 overflow-x-auto snap-x snap-mandatory flex-1 pb-1"
      >
        {hotels.map((hotel) => (
          <HotelCard
            key={hotel.id}
            hotel={hotel}
            nights={nights}
            width={cardWidth}
          />
        ))}
      </div>

      {/* Right arrow */}
      <button
        onClick={() => scroll("next")}
        disabled={!canNext}
        aria-label="Next hotels"
        className="flex-shrink-0 my-auto size-12 rounded-full
                   bg-muted text-muted-foreground text-xl
                   flex items-center justify-center
                   hover:bg-border
                   disabled:opacity-25 disabled:cursor-not-allowed
                   transition-colors duration-150 cursor-pointer"
      >
        ›
      </button>
    </div>
  );
}
