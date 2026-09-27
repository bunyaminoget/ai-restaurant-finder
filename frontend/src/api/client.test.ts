import { describe, expect, it, vi, afterEach } from "vitest";
import { ApiError, extractErrorMessage, getNearbyRestaurants, getRestaurantDetail } from "./client";

const detail = {
  id: "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  name: "Test Lokanta",
  latitude: 41.0082,
  longitude: 28.9784,
  rating: 4.5,
  reviewCount: 128,
};

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

function textResponse(body: string, status: number) {
  return new Response(body, {
    status,
    headers: { "Content-Type": "text/plain" },
  });
}

afterEach(() => {
  vi.unstubAllGlobals();
});

describe("getRestaurantDetail", () => {
  it("200 yanıtındaki camelCase kontrat alanlarını döndürür", async () => {
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse(detail));
    vi.stubGlobal("fetch", fetchMock);

    const result = await getRestaurantDetail(detail.id, { baseUrl: "http://localhost:5038" });

    expect(result).toEqual(detail);
    expect(fetchMock).toHaveBeenCalledWith(
      `http://localhost:5038/api/restaurants/${detail.id}`,
      expect.objectContaining({ headers: { Accept: "application/json" } }),
    );
  });

  it("404 yanıtını not-found olarak eşler", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(textResponse("Restaurant was not found.", 404)));

    const error = await getRestaurantDetail(detail.id).catch((e: unknown) => e);
    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).kind).toBe("not-found");
  });

  it("400 yanıtını invalid-id olarak eşler", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(textResponse("Id must be a valid non-empty GUID.", 400)),
    );

    const error = await getRestaurantDetail("gecersiz").catch((e: unknown) => e);
    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).kind).toBe("invalid-id");
  });

  it("şekli bozuk 200 yanıtını invalid-response olarak işaretler", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(jsonResponse({ wrong: "shape" })));

    const error = await getRestaurantDetail(detail.id).catch((e: unknown) => e);
    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).kind).toBe("invalid-response");
  });

  it("boş id için istek atmadan invalid-id fırlatır", async () => {
    const fetchMock = vi.fn();
    vi.stubGlobal("fetch", fetchMock);

    const error = await getRestaurantDetail("   ").catch((e: unknown) => e);
    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).kind).toBe("invalid-id");
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("502 ProblemDetails gövdesini http hatası olarak eşler ve title gösterir", async () => {
    vi.stubGlobal(
      "fetch",
      vi
        .fn()
        .mockResolvedValue(
          jsonResponse(
            { title: "Restaurant data provider is temporarily unavailable.", status: 502 },
            502,
          ),
        ),
    );

    const error = await getRestaurantDetail(detail.id).catch((e: unknown) => e);
    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).kind).toBe("http");
    expect((error as ApiError).status).toBe(502);
    expect((error as ApiError).message).toBe(
      "Restaurant data provider is temporarily unavailable.",
    );
  });

  it("503 ProblemDetails gövdesindeki ham JSON yerine title + detail gösterir", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(
        jsonResponse(
          {
            title: "Restaurant data provider is not configured.",
            detail: "Missing API key.",
            status: 503,
          },
          503,
        ),
      ),
    );

    const error = await getRestaurantDetail(detail.id).catch((e: unknown) => e);
    expect((error as ApiError).kind).toBe("http");
    expect((error as ApiError).message).toBe(
      "Restaurant data provider is not configured: Missing API key.",
    );
  });

  it("boş gövdeli 500 yanıtında fallback mesajı kullanır", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(textResponse("", 500)));

    const error = await getRestaurantDetail(detail.id).catch((e: unknown) => e);
    expect((error as ApiError).kind).toBe("http");
    expect((error as ApiError).status).toBe(500);
    expect((error as ApiError).message).toBe("Restoran detayı alınamadı (HTTP 500).");
  });
});

describe("extractErrorMessage", () => {
  it("title sonundaki noktayı temizleyip ': ' ile birleştirir (çift noktalama olmaz)", () => {
    const body = JSON.stringify({ title: "Service unavailable.", detail: "Try again." });
    expect(extractErrorMessage(body, "fallback")).toBe("Service unavailable: Try again.");
    expect(extractErrorMessage(body, "fallback")).not.toContain(".:");
  });

  it("title sonundaki iki noktayı temizler", () => {
    const body = JSON.stringify({ title: "Hata:", detail: "Detay." });
    expect(extractErrorMessage(body, "fallback")).toBe("Hata: Detay.");
  });

  it("yalnızca title varsa aynen korunur", () => {
    const body = JSON.stringify({ title: "Restaurant data provider is temporarily unavailable." });
    expect(extractErrorMessage(body, "fallback")).toBe(
      "Restaurant data provider is temporarily unavailable.",
    );
  });
});

describe("getNearbyRestaurants", () => {
  const pagination = { page: 1, pageSize: 20, totalCount: 1, totalPages: 1, hasNextPage: false };
  const item = { ...detail, distanceKm: 0.42 };

  it("bozuk öğeleri filtreler, geçerlileri döndürür (NearbyList çökmez)", async () => {
    vi.stubGlobal(
      "fetch",
      vi
        .fn()
        .mockResolvedValue(
          jsonResponse({ items: [item, { id: "bozuk", name: "Bozuk" }], pagination }),
        ),
    );

    const result = await getNearbyRestaurants({ latitude: 41.0, longitude: 29.0 });
    expect(result.items).toHaveLength(1);
    expect(result.items[0]).toEqual(item);
  });

  it("şekilsiz gövdeyi invalid-response olarak işaretler", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(jsonResponse({ wrong: "shape" })));

    const error = await getNearbyRestaurants({ latitude: 41.0, longitude: 29.0 }).catch(
      (e: unknown) => e,
    );
    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).kind).toBe("invalid-response");
  });
});
