import { useEffect } from "react";
import { MapContainer, Marker, Popup, TileLayer, useMap } from "react-leaflet";
import "leaflet/dist/leaflet.css";
import "./leafletIcons";
import { selectionIcon } from "./leafletIcons";
import { MapClickHandler } from "./MapClickHandler";
import { RestaurantMarkers } from "./RestaurantMarkers";
import type { NearbyRestaurant } from "../../api/types";

/** Haritada seçilen arama merkezi (uygulanan nearby sorgusuyla birebir). */
export interface MapCenter {
  latitude: number;
  longitude: number;
}

export const DEFAULT_MAP_ZOOM = 13;

interface Props {
  center: MapCenter;
  restaurants: NearbyRestaurant[];
  selectedId?: string;
  onMapClick: (center: MapCenter) => void;
  onSelect: (id: string) => void;
}

/**
 * Form/manual-submit ile uygulanan merkez değiştiğinde harita görünümünü
 * senkron tutar. Pan/zoom'un kendisi API çağrısı tetiklemez; görünüm
 * senkronu yalnızca `center` prop'u değiştiğinde çalışır.
 */
function MapViewSync({ center }: { center: MapCenter }) {
  const map = useMap();
  useEffect(() => {
    map.setView([center.latitude, center.longitude], map.getZoom());
  }, [map, center.latitude, center.longitude]);
  return null;
}

/**
 * Gerçek harita entegrasyonu (Leaflet + OpenStreetMap).
 * Tıklama -> onMapClick (ayrı seçim marker'ı + nearby akışı),
 * restoran marker'ları -> onSelect (mevcut detail akışı).
 */
export function RestaurantMap({ center, restaurants, selectedId, onMapClick, onSelect }: Props) {
  return (
    <MapContainer
      center={[center.latitude, center.longitude]}
      zoom={DEFAULT_MAP_ZOOM}
      scrollWheelZoom
      className="map-container"
      aria-label="Restoran haritası"
    >
      <TileLayer
        attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
        url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
      />
      <MapViewSync center={center} />
      <MapClickHandler onMapClick={onMapClick} />
      <Marker position={[center.latitude, center.longitude]} icon={selectionIcon}>
        <Popup>
          Seçili arama konumu
          <br />
          {center.latitude.toFixed(5)}, {center.longitude.toFixed(5)}
        </Popup>
      </Marker>
      <RestaurantMarkers restaurants={restaurants} selectedId={selectedId} onSelect={onSelect} />
    </MapContainer>
  );
}
