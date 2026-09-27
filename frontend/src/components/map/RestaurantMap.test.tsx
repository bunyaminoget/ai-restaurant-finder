/**
 * RestaurantMap / MapClickHandler / RestaurantMarkers için odaklı testler.
 * Gerçek Leaflet jsdom'da harita kuramaz, bu yüzden react-leaflet
 * ince bir mock ile değiştirilir; bizim mantığımız (tile attribution,
 * seçim marker'ı, popup içeriği, tıklama -> state akışı) doğrulanır.
 */
import { beforeEach, describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import type { ReactNode } from "react";
import type { NearbyRestaurant } from "../../api/types";

vi.mock("react-leaflet", () => {
  return {
    MapContainer: ({
      children,
      center,
      zoom,
    }: {
      children: ReactNode;
      center: [number, number];
      zoom: number;
    }) => (
      <div
        data-testid="map-container"
        data-center={JSON.stringify(center)}
        data-zoom={String(zoom)}
      >
        {children}
      </div>
    ),
    TileLayer: ({ attribution, url }: { attribution: string; url: string }) => (
      <div data-testid="tile-layer" data-url={url}>
        {attribution}
      </div>
    ),
    Marker: ({
      children,
      position,
      eventHandlers,
    }: {
      children?: ReactNode;
      position: [number, number];
      eventHandlers?: { click?: () => void };
    }) => (
      // Gerçek react-leaflet Marker'ı <button> değildir; mock'ta div
      // kullanarak iç içe button uyarısından kaçınılır.
      <div
        data-testid="map-marker"
        data-position={JSON.stringify(position)}
        onClick={eventHandlers?.click}
      >
        {children}
      </div>
    ),
    Popup: ({ children }: { children: ReactNode }) => (
      <div data-testid="map-popup">{children}</div>
    ),
    useMapEvents: (handlers: Record<string, (event: unknown) => void>) => {
      (globalThis as unknown as { __mapHandlers?: unknown }).__mapHandlers = handlers;
      return null;
    },
    useMap: () => ({ setView: vi.fn(), getZoom: () => 13 }),
  };
});

function getClickHandler(): (event: unknown) => void {
  const handlers = (globalThis as unknown as { __mapHandlers?: { click: (e: unknown) => void } })
    .__mapHandlers;
  if (!handlers) throw new Error("MapClickHandler kaydedilmedi.");
  return handlers.click;
}

const restaurants: NearbyRestaurant[] = [
  {
    id: "11111111-1111-4111-8111-111111111111",
    name: "Test Kebap",
    latitude: 41.009,
    longitude: 28.979,
    rating: 4.5,
    reviewCount: 120,
    distanceKm: 0.34,
  },
  {
    id: "22222222-2222-4222-8222-222222222222",
    name: "Örnek Pide",
    latitude: 41.012,
    longitude: 28.985,
    rating: 3.8,
    reviewCount: 45,
    distanceKm: 1.27,
  },
];

// Mock leaflet ikon fix'i jsdom'da gerçek görsel ister; seçim ikonu yerine
// hafif bir stub yeterli (ikonun kendisi ayrı modülde test dışı).
vi.mock("../leafletIcons", () => ({ selectionIcon: { __stub: "selection-icon" } }));

import { RestaurantMap } from "./RestaurantMap";
import { MapClickHandler } from "./MapClickHandler";

beforeEach(() => {
  vi.clearAllMocks();
});

describe("RestaurantMap", () => {
  const center = { latitude: 41.0082, longitude: 28.9784 };

  function renderMap(selectedId = "") {
    const onMapClick = vi.fn();
    const onSelect = vi.fn();
    render(
      <RestaurantMap
        center={center}
        restaurants={restaurants}
        selectedId={selectedId}
        onMapClick={onMapClick}
        onSelect={onSelect}
      />,
    );
    return { onMapClick, onSelect };
  }

  it("İstanbul merkez ve 13-14 zoom ile başlar", () => {
    renderMap();
    const container = screen.getByTestId("map-container");
    expect(container).toHaveAttribute("data-center", JSON.stringify([41.0082, 28.9784]));
    const zoom = Number(container.getAttribute("data-zoom"));
    expect(zoom).toBeGreaterThanOrEqual(13);
    expect(zoom).toBeLessThanOrEqual(14);
  });

  it("OSM tile layer + görünür attribution kullanır", () => {
    renderMap();
    const tile = screen.getByTestId("tile-layer");
    expect(tile).toHaveAttribute("data-url", expect.stringContaining("tile.openstreetmap.org"));
    expect(tile.textContent).toMatch(/OpenStreetMap/);
  });

  it("seçim noktası için ayrı marker gösterir", () => {
    renderMap();
    const markers = screen.getAllByTestId("map-marker");
    // 1 seçim + 2 restoran marker'ı
    expect(markers).toHaveLength(3);
    expect(markers[0]).toHaveAttribute("data-position", JSON.stringify([41.0082, 28.9784]));
    expect(screen.getByText(/Seçili arama konumu/)).toBeInTheDocument();
  });

  it("her restoran için popup içeriği gösterir (ad, yıldız, yorum, mesafe)", () => {
    renderMap();
    expect(screen.getByText("Test Kebap")).toBeInTheDocument();
    expect(screen.getByText(/4\.5/)).toBeInTheDocument();
    expect(screen.getByText(/120 değerlendirme/)).toBeInTheDocument();
    expect(screen.getByText(/0\.34 km/)).toBeInTheDocument();
    expect(screen.getByText("Örnek Pide")).toBeInTheDocument();
  });

  it("marker tıklaması seçim akışına bağlanır (onSelect)", () => {
    const { onSelect } = renderMap();
    const markers = screen.getAllByTestId("map-marker");
    fireEvent.click(markers[1]);
    expect(onSelect).toHaveBeenCalledWith("11111111-1111-4111-8111-111111111111");
  });

  it("popup içindeki 'Detayı aç' butonu da onSelect çağırır", () => {
    const { onSelect } = renderMap();
    fireEvent.click(screen.getAllByRole("button", { name: "Detayı aç" })[0]);
    expect(onSelect).toHaveBeenCalledWith("11111111-1111-4111-8111-111111111111");
  });

  it("harita tıklaması lat/lng ile onMapClick çağırır", () => {
    const { onMapClick } = renderMap();
    getClickHandler()({ latlng: { lat: 41.02, lng: 28.99 } });
    expect(onMapClick).toHaveBeenCalledWith({ latitude: 41.02, longitude: 28.99 });
  });

  it("harita hareketi (pan/zoom) tek başına API tetiklemez: moveend dinlenmez", () => {
    renderMap();
    const handlers = (globalThis as unknown as { __mapHandlers?: Record<string, unknown> })
      .__mapHandlers;
    expect(handlers).toBeDefined();
    expect(handlers).not.toHaveProperty("moveend");
  });
});

describe("MapClickHandler", () => {
  it("tıklama koordinatını MapCenter olarak iletir", () => {
    const onMapClick = vi.fn();
    render(<MapClickHandler onMapClick={onMapClick} />);
    getClickHandler()({ latlng: { lat: 40.0, lng: 29.5 } });
    expect(onMapClick).toHaveBeenCalledWith({ latitude: 40.0, longitude: 29.5 });
  });
});
