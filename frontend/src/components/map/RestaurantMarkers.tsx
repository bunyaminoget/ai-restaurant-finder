import { Marker, Popup } from "react-leaflet";
import type { NearbyRestaurant } from "../../api/types";

interface Props {
  restaurants: NearbyRestaurant[];
  selectedId?: string;
  onSelect: (id: string) => void;
}

/**
 * Restoran marker'ları. Her marker'da popup içinde kontrat alanları
 * (ad, puan yıldızı, yorum sayısı, mesafe) gösterilir; marker'a
 * tıklamak mevcut seçim/detail akışına bağlanır (onSelect -> selectedId).
 */
export function RestaurantMarkers({ restaurants, selectedId, onSelect }: Props) {
  return (
    <>
      {restaurants.map((restaurant) => (
        <Marker
          key={restaurant.id}
          position={[restaurant.latitude, restaurant.longitude]}
          eventHandlers={{ click: () => onSelect(restaurant.id) }}
        >
          <Popup>
            <div>
              <strong>{restaurant.name}</strong>
              <br />⭐ {restaurant.rating.toFixed(1)} · {restaurant.reviewCount} değerlendirme
              <br />
              {restaurant.distanceKm.toFixed(2)} km
              <br />
              <button
                type="button"
                onClick={() => onSelect(restaurant.id)}
                aria-pressed={restaurant.id === selectedId}
              >
                Detayı aç
              </button>
            </div>
          </Popup>
        </Marker>
      ))}
    </>
  );
}
