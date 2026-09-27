// Backend contract (verified against Program.cs + response records):
// - ASP.NET minimal APIs serialize with the default web JSON options, i.e.
//   camelCase property names.
// - GET /api/restaurants/{id} -> 200 RestaurantDetailResponse
//   { id, name, latitude, longitude, rating, reviewCount }
//   400 plain text ("Id must be a valid non-empty GUID."),
//   404 plain text ("Restaurant with id '...' was not found.").
// - GET /api/restaurants/nearby -> 200 NearbyRestaurantsResponse
//   { items: [{ id, name, latitude, longitude, rating, reviewCount, distanceKm }],
//     pagination: { page, pageSize, totalCount, totalPages, hasNextPage } }

export interface RestaurantDetail {
  id: string;
  name: string;
  latitude: number;
  longitude: number;
  rating: number;
  reviewCount: number;
}

export interface NearbyRestaurant extends RestaurantDetail {
  distanceKm: number;
}

export interface NearbyPagination {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
}

export interface NearbyResponse {
  items: NearbyRestaurant[];
  pagination: NearbyPagination;
}

export interface NearbyQuery {
  latitude: number;
  longitude: number;
  radiusKm?: number;
  minRating?: number;
  sortBy?: string;
  sortDirection?: string;
  page?: number;
  pageSize?: number;
}
