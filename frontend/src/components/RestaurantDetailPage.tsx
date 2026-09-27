import { useEffect, useState } from "react";
import { ApiError, getRestaurantDetail } from "../api/client";
import type { RestaurantDetail } from "../api/types";

type Status =
  | { kind: "loading" }
  | { kind: "not-found"; message: string }
  | { kind: "error"; message: string }
  | { kind: "success"; detail: RestaurantDetail };

interface Props {
  restaurantId: string;
  onBack?: () => void;
}

function statusFor(error: unknown): Status {
  if (error instanceof ApiError) {
    if (error.kind === "not-found") return { kind: "not-found", message: error.message };
    return { kind: "error", message: error.message };
  }
  return { kind: "error", message: "Beklenmeyen bir hata oluştu." };
}

/**
 * Restaurant detail screen. All text is rendered via React (no injected HTML,
 * so no XSS surface). Covers loading / not-found (404) / error (400, 502/503/500
 * ProblemDetails, network, invalid-response) / success states of
 * GET /api/restaurants/{id}. Retries and id changes abort the in-flight
 * request and ignore its stale response.
 */
export function RestaurantDetailPage({ restaurantId, onBack }: Props) {
  const [status, setStatus] = useState<Status>({ kind: "loading" });
  const [retryKey, setRetryKey] = useState(0);

  useEffect(() => {
    const controller = new AbortController();
    setStatus({ kind: "loading" });
    getRestaurantDetail(restaurantId, { signal: controller.signal })
      .then((detail) => {
        if (!controller.signal.aborted) setStatus({ kind: "success", detail });
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") return;
        if (!controller.signal.aborted) setStatus(statusFor(error));
      });
    return () => controller.abort();
  }, [restaurantId, retryKey]);

  const retry = () => setRetryKey((key) => key + 1);

  return (
    <section aria-label="Restoran detayı" className="card">
      {onBack && (
        <button type="button" className="link-button" onClick={onBack}>
          ← Listeye dön
        </button>
      )}

      {status.kind === "loading" && (
        <p role="status" aria-live="polite">
          Restoran detayı yükleniyor…
        </p>
      )}

      {status.kind === "not-found" && (
        <div role="status" aria-live="polite">
          <h2>Restoran bulunamadı</h2>
          <p>{status.message}</p>
          <button type="button" onClick={retry}>
            Tekrar dene
          </button>
          {onBack && (
            <button type="button" onClick={onBack}>
              Listeye dön
            </button>
          )}
        </div>
      )}

      {status.kind === "error" && (
        <div role="alert">
          <h2>Detay yüklenemedi</h2>
          <p>{status.message}</p>
          <button type="button" onClick={retry}>
            Tekrar dene
          </button>
        </div>
      )}

      {status.kind === "success" && <DetailView detail={status.detail} />}
    </section>
  );
}

function DetailView({ detail }: { detail: RestaurantDetail }) {
  const mapsUrl = `https://www.google.com/maps/search/?api=1&query=${detail.latitude},${detail.longitude}`;
  return (
    <div>
      <h2>{detail.name}</h2>
      <dl className="detail-list">
        <div>
          <dt>Puan</dt>
          <dd>
            {detail.rating.toFixed(1)} ({detail.reviewCount} değerlendirme)
          </dd>
        </div>
        <div>
          <dt>Konum</dt>
          <dd>
            {detail.latitude.toFixed(5)}, {detail.longitude.toFixed(5)}
          </dd>
        </div>
        <div>
          <dt>Restoran ID</dt>
          <dd>
            <code>{detail.id}</code>
          </dd>
        </div>
      </dl>
      <p>
        <a href={mapsUrl} target="_blank" rel="noreferrer">
          Haritada aç
        </a>
      </p>
    </div>
  );
}
