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