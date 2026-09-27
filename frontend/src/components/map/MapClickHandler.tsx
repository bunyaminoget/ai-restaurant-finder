import { useMapEvents } from "react-leaflet";
import type { MapCenter } from "./RestaurantMap";

interface Props {
  onMapClick: (center: MapCenter) => void;
}

/**
 * Harita tıklamalarını yakalar. Pan/zoom bilerek dinlenmez:
 * harita hareketi tek başına nearby API çağrısı tetiklemez,
 * yalnızca tıklama arama merkezini günceller.
 */
export function MapClickHandler({ onMapClick }: Props) {
  useMapEvents({
    click(event) {
      onMapClick({ latitude: event.latlng.lat, longitude: event.latlng.lng });
    },
  });
  return null;
}
