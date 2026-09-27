import L from "leaflet";
import markerIcon2xUrl from "leaflet/dist/images/marker-icon-2x.png";
import markerIconUrl from "leaflet/dist/images/marker-icon.png";
import markerShadowUrl from "leaflet/dist/images/marker-shadow.png";

/**
 * Vite, Leaflet'in varsayılan marker görsellerini çözümleyemez
 * (`_getIconUrl` çalışma anında hatalı yol üretir), bu yüzden ikon
 * yollarını derleme anında çözümlenen import'larla sabitliyoruz.
 * Detay: https://leafletjs.com + Vite static asset handling.
 */
L.Icon.Default.mergeOptions({
  iconRetinaUrl: markerIcon2xUrl,
  iconUrl: markerIconUrl,
  shadowUrl: markerShadowUrl,
});

/**
 * Kullanıcının haritada seçtiği arama noktası için varsayılan restoran
 * marker'larından görsel olarak ayırt edilebilir ikon (mavi nokta).
 */
export const selectionIcon = L.divIcon({
  className: "selection-marker",
  html: `<span aria-hidden="true" style="
    display:block;width:18px;height:18px;border-radius:50%;
    background:#1a56db;border:3px solid #fff;
    box-shadow:0 0 0 2px #1a56db,0 1px 4px rgba(0,0,0,.4);"></span>`,
  iconSize: [18, 18],
  iconAnchor: [9, 9],
  popupAnchor: [0, -9],
});
