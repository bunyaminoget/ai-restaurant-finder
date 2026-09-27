import type { NearbyQuery, NearbyResponse, NearbyRestaurant, RestaurantDetail } from "./types";

/** Machine-readable API failure reasons used by the UI states. */
export type ApiErrorKind =
  | "not-found" // HTTP 404 — restaurant does not exist (empty state)
  | "invalid-id" // HTTP 400 — malformed id (error state)
  | "http" // other non-2xx responses
  | "network" // fetch itself failed / aborted (except intentional abort)
  | "invalid-response"; // 2xx but body does not match the contract

export class ApiError extends Error {
  readonly kind: ApiErrorKind;
  readonly status?: number;
  readonly body?: string;

  constructor(kind: ApiErrorKind, message: string, opts?: { status?: number; body?: string }) {
    super(message);
    this.name = "ApiError";
    this.kind = kind;
    this.status = opts?.status;
    this.body = opts?.body;
  }
}

/** API base URL, configurable via env. No secrets are hard-coded. */
export function getApiBaseUrl(): string {
  const raw = import.meta.env.VITE_API_BASE_URL?.trim();
  const base = raw && raw.length > 0 ? raw : "http://localhost:5038";
  return base.replace(/\/+$/, "");
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

export function isRestaurantDetail(value: unknown): value is RestaurantDetail {
  if (!isRecord(value)) return false;
  return (
    typeof value.id === "string" &&
    typeof value.name === "string" &&
    typeof value.latitude === "number" &&
    typeof value.longitude === "number" &&
    typeof value.rating === "number" &&
    typeof value.reviewCount === "number"
  );
}

export function isNearbyRestaurant(value: unknown): value is NearbyRestaurant {
  return (
    isRestaurantDetail(value) &&
    typeof (value as unknown as Record<string, unknown>).distanceKm === "number"
  );
}

async function readBodyText(response: Response): Promise<string> {
  try {
    return await response.text();
  } catch {
    return "";
  }
}

/**
 * Backend returns plain-text bodies for 400/404 but JSON ProblemDetails for
 * 502/503/500 (see Program.cs UseExceptionHandler). Extract a human-readable
 * message so the UI never renders a raw JSON blob.
 */
export function extractErrorMessage(body: string, fallback: string): string {
  const trimmed = body.trim();
  if (trimmed.length === 0) return fallback;
  if (trimmed.startsWith("{")) {
    try {
      const parsed: unknown = JSON.parse(trimmed);
      if (isRecord(parsed)) {
        const title = typeof parsed.title === "string" ? parsed.title.trim() : "";
        const detail = typeof parsed.detail === "string" ? parsed.detail.trim() : "";
        // Çift noktalamayı önlemek için sondaki "."/":" yalnızca detail ile
        // birleştirirken temizlenir; tek başına title aynen korunur.
        const cleanTitle = title.replace(/[.:]+$/, "").trim();
        const combined =
          cleanTitle.length > 0 && detail.length > 0 && detail !== title && detail !== cleanTitle
            ? `${cleanTitle}: ${detail}`
            : title.length > 0
              ? title
              : detail;
        if (combined.length > 0) return combined.slice(0, 500);
      }
    } catch {
      // Not actually JSON — fall through to plain-text handling.
    }
  }
  return trimmed.slice(0, 500);
}

async function throwForStatus(response: Response, fallback: string): Promise<never> {
  const body = await readBodyText(response);
  const message = extractErrorMessage(body, fallback);
  if (response.status === 404) {
    throw new ApiError("not-found", message, { status: 404, body });
  }
  if (response.status === 400) {
    throw new ApiError("invalid-id", message, { status: 400, body });
  }
  throw new ApiError("http", message, { status: response.status, body });
}

export interface RequestOptions {
  signal?: AbortSignal;
  baseUrl?: string;
}

/** GET /api/restaurants/{id} — id is a GUID string. */
export async function getRestaurantDetail(
  id: string,
  options?: RequestOptions,
): Promise<RestaurantDetail> {
  const trimmed = id.trim();
  if (trimmed.length === 0) {
    throw new ApiError("invalid-id", "Restoran id boş olamaz.");
  }
  const base = (options?.baseUrl ?? getApiBaseUrl()).replace(/\/+$/, "");
  let response: Response;
  try {
    response = await fetch(`${base}/api/restaurants/${encodeURIComponent(trimmed)}`, {
      signal: options?.signal,
      headers: { Accept: "application/json" },
    });
  } catch (error) {
    if (error instanceof DOMException && error.name === "AbortError") throw error;
    throw new ApiError("network", "Ağ hatası: sunucuya ulaşılamadı.");
  }
  if (!response.ok) {
    await throwForStatus(response, `Restoran detayı alınamadı (HTTP ${response.status}).`);
  }
  let json: unknown;
  try {
    json = await response.json();
  } catch {
    throw new ApiError("invalid-response", "Sunucudan geçersiz yanıt alındı.");
  }
  if (!isRestaurantDetail(json)) {
    throw new ApiError("invalid-response", "Sunucudan geçersiz yanıt alındı.");
  }
  return json;
}

/** GET /api/restaurants/nearby — used for the simple list -> detail flow. */
export async function getNearbyRestaurants(
  query: NearbyQuery,
  options?: RequestOptions,
): Promise<NearbyResponse> {
  const base = (options?.baseUrl ?? getApiBaseUrl()).replace(/\/+$/, "");
  const params = new URLSearchParams();
  params.set("latitude", String(query.latitude));
  params.set("longitude", String(query.longitude));
  if (query.radiusKm !== undefined) params.set("radiusKm", String(query.radiusKm));
  if (query.minRating !== undefined) params.set("minRating", String(query.minRating));
  if (query.sortBy) params.set("sortBy", query.sortBy);
  if (query.sortDirection) params.set("sortDirection", query.sortDirection);
  if (query.page !== undefined) params.set("page", String(query.page));
  if (query.pageSize !== undefined) params.set("pageSize", String(query.pageSize));

  let response: Response;
  try {
    response = await fetch(`${base}/api/restaurants/nearby?${params.toString()}`, {
      signal: options?.signal,
      headers: { Accept: "application/json" },
    });
  } catch (error) {
    if (error instanceof DOMException && error.name === "AbortError") throw error;
    throw new ApiError("network", "Ağ hatası: sunucuya ulaşılamadı.");
  }
  if (!response.ok) {
    await throwForStatus(response, `Yakındaki restoranlar alınamadı (HTTP ${response.status}).`);
  }
  let json: unknown;
  try {
    json = await response.json();
  } catch {
    throw new ApiError("invalid-response", "Sunucudan geçersiz yanıt alındı.");
  }
  if (!isRecord(json) || !Array.isArray(json.items) || !isRecord(json.pagination)) {
    throw new ApiError("invalid-response", "Sunucudan geçersiz yanıt alındı.");
  }
  // Bozuk öğeleri sessizce ele: tek bir şekilsiz öğe tüm listeyi ve
  // NearbyList render'ını (distanceKm.toFixed) çökertmemeli.
  const items = (json.items as unknown[]).filter(isNearbyRestaurant);
  return { items, pagination: json.pagination } as unknown as NearbyResponse;
}
