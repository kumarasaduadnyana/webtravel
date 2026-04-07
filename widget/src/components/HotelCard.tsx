import type { Hotel } from "../types/hotel";

interface Props {
  hotel: Hotel;
  nights: number;
  width?: number;
}

function StarRating({ count }: { count: number }) {
  const rounded = Math.round(count);
  return (
    <div className="flex gap-0.5">
      {Array.from({ length: 5 }, (_, i) => (
        <span
          key={i}
          className={`text-lg leading-none ${i < rounded ? "text-amber-400" : "text-border"}`}
        >
          ★
        </span>
      ))}
    </div>
  );
}

function getRatingLabel(score: number): string {
  if (score >= 9) return "Exceptional";
  if (score >= 8) return "Excellent";
  if (score >= 7) return "Very Good";
  if (score >= 6) return "Good";
  return "Fair";
}

function formatPrice(price: number): string {
  return Math.round(price).toLocaleString();
}

export default function HotelCard({ hotel, nights, width = 290 }: Props) {
  const currency = hotel.currency || "AUD";
  const vipPrice = hotel.price ? hotel.price * 0.882 : null;

  return (
    <div
      className="flex flex-col flex-shrink-0 snap-start select-none rounded-xl bg-card ring-1 ring-border overflow-hidden"
      style={{ width: `${width}px` }}
    >
      {/* Image */}
      <div className="w-full h-[210px] overflow-hidden bg-muted flex items-center justify-center flex-shrink-0">
        {hotel.imageUrl ? (
          <img
            src={hotel.imageUrl}
            alt={hotel.name}
            className="w-full h-full object-cover transition-transform duration-300 hover:scale-105"
            onError={(e) => {
              const el = e.currentTarget;
              el.style.display = "none";
              el.parentElement!.innerHTML =
                '<span style="font-size:3rem">🏨</span>';
            }}
          />
        ) : (
          <span className="text-5xl">🏨</span>
        )}
      </div>

      {/* Card content */}
      <div className="flex flex-col flex-1 p-3">
        {/* Name */}
        <h3 className="font-bold text-card-foreground text-[15px] leading-snug mb-1 line-clamp-2">
          {hotel.name}
        </h3>

        {/* Location */}
        <p className="text-sm text-muted-foreground mb-2">{hotel.location}</p>

        {/* Stars */}
        <StarRating count={hotel.starRating} />

        {/* Guest Rating */}
        <div className="flex items-center gap-2 mt-1.5 mb-3">
          <span className="border border-border rounded text-xs font-semibold text-card-foreground px-1.5 py-0.5 leading-none">
            {hotel.guestRating?.toFixed(1)}
          </span>
          <span className="text-xs text-muted-foreground">
            {getRatingLabel(hotel.guestRating)} ({hotel.guestRatingCount?.toLocaleString()})
          </span>
        </div>

        {/* Pricing */}
        <div className="flex-1 flex flex-col gap-0.5">
          <p className="text-xs text-muted-foreground">From</p>
          <p className="text-sm">
            <span className="font-bold text-card-foreground">
              {currency} {formatPrice(hotel.price)}
            </span>
            <span className="text-muted-foreground ml-1">/ {nights} nights</span>
          </p>

          {vipPrice !== null && (
            <p className="text-sm">
              <span className="font-bold text-accent">
                {currency} {formatPrice(vipPrice)}
              </span>
              <span className="text-muted-foreground text-xs ml-1">with </span>
              <span className="font-bold text-xs text-primary">VIP</span>
            </p>
          )}

          <p className="text-xs text-muted-foreground mt-0.5">
            ✓ Taxes and fees included
          </p>
        </div>

        {/* CTA */}
        <button className="mt-3 w-full bg-primary hover:bg-primary-hover text-primary-foreground font-medium text-sm py-2.5 rounded-full transition-colors duration-200 cursor-pointer">
          View Details
        </button>
      </div>
    </div>
  );
}
