import { useEffect, useState } from "react";
import { ApiError, getNearbyRestaurants } from "../api/client";
import type { NearbyRestaurant } from "../api/types";

interface Props {
  latitude: number;
  longitude: number;
  onSelect: (id: string) => void;
  selectedId?: string;
}

/** Minimal nearby list used only as an entry point into the detail screen. */
export function NearbyList({ latitude, longitude, onSelect, selectedId }: Props) {
  const [items, setItems] = useState<NearbyRestaurant[]>([]);
  const [state, setState] = useState<"loading" | "error" | "empty" | "ready">("loading");
  const [message, setMessage] = useState("");

  useEffect(() => {
    const controller = new AbortController();
    setState("loading");
    getNearbyRestaurants({ latitude, longitude }, { signal: controller.signal })
      .then((response) => {
        if (controller.signal.aborted) return;
        setItems(response.items);
        setState(response.items.length === 0 ? "empty" : "ready");
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") return;
        if (controller.signal.aborted) return;
        setState("error");
        setMessage(error instanceof ApiError ? error.message : "Beklenmeyen bir hata oluştu.");
      });
    return () => controller.abort();
  }, [latitude, longitude]);

  if (state === "loading") return <p role="status">Yakındaki restoranlar yükleniyor…</p>;
  if (state === "error") return <p role="alert">Liste yüklenemedi: {message}</p>;
  if (state === "empty") return <p role="status">Bu konum çevresinde restoran bulunamadı.</p>;

  return (
    <ul className="nearby-list">
      {items.map((item) => (
        <li key={item.id} className={item.id === selectedId ? "selected" : undefined}>
          <button type="button" onClick={() => onSelect(item.id)} aria-pressed={item.id === selectedId}>
            <strong>{item.name}</strong>
            <span>
              {item.rating.toFixed(1)} ★ · {item.distanceKm.toFixed(2)} km
            </span>
          </button>
        </li>
      ))}
    </ul>
  );
}
