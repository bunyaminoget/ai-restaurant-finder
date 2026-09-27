import { describe, expect, it, vi, afterEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { RestaurantDetailPage } from "./RestaurantDetailPage";
import * as client from "../api/client";
import { ApiError } from "../api/client";

afterEach(() => {
  vi.restoreAllMocks();
});

const detail = {
  id: "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  name: "Test Lokanta",
  latitude: 41.0082,
  longitude: 28.9784,
  rating: 4.5,
  reviewCount: 128,
};

describe("RestaurantDetailPage", () => {
  it("loading durumunu gösterir", () => {
    vi.spyOn(client, "getRestaurantDetail").mockImplementation(
      () => new Promise(() => {}), // asla çözülmez -> loading
    );
    render(<RestaurantDetailPage restaurantId={detail.id} />);
    expect(screen.getByRole("status")).toHaveTextContent("yükleniyor");
  });

  it("success durumunda kontrat alanlarını gösterir", async () => {
    vi.spyOn(client, "getRestaurantDetail").mockResolvedValue(detail);
    render(<RestaurantDetailPage restaurantId={detail.id} />);

    await waitFor(() => expect(screen.getByRole("heading", { name: "Test Lokanta" })).toBeInTheDocument());
    expect(screen.getByText(/4\.5/)).toBeInTheDocument();
    expect(screen.getByText(/128 değerlendirme/)).toBeInTheDocument();
    const mapsLink = screen.getByRole("link", { name: "Haritada aç" });
    expect(mapsLink).toHaveAttribute(
      "href",
      `https://www.google.com/maps/search/?api=1&query=${detail.latitude},${detail.longitude}`,
    );
  });

  it("404 durumunda not-found gösterir", async () => {
    vi.spyOn(client, "getRestaurantDetail").mockRejectedValue(
      new ApiError("not-found", "Restaurant was not found.", { status: 404 }),
    );
    render(<RestaurantDetailPage restaurantId={detail.id} />);

    await waitFor(() =>
      expect(screen.getByRole("heading", { name: "Restoran bulunamadı" })).toBeInTheDocument(),
    );
  });

  it("400 durumunda error ve retry gösterir", async () => {
    vi.spyOn(client, "getRestaurantDetail").mockRejectedValue(
      new ApiError("invalid-id", "Id must be a valid non-empty GUID.", { status: 400 }),
    );
    render(<RestaurantDetailPage restaurantId="gecersiz" />);

    await waitFor(() =>
      expect(screen.getByRole("heading", { name: "Detay yüklenemedi" })).toBeInTheDocument(),
    );
    expect(screen.getByRole("button", { name: "Tekrar dene" })).toBeInTheDocument();
  });

  it("502/503/500 durumunda error ve retry gösterir (ProblemDetails)", async () => {
    vi.spyOn(client, "getRestaurantDetail").mockRejectedValue(
      new ApiError("http", "Restaurant data provider is temporarily unavailable.", {
        status: 502,
      }),
    );
    render(<RestaurantDetailPage restaurantId={detail.id} />);

    await waitFor(() =>
      expect(screen.getByRole("heading", { name: "Detay yüklenemedi" })).toBeInTheDocument(),
    );
    expect(
      screen.getByText("Restaurant data provider is temporarily unavailable."),
    ).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Tekrar dene" })).toBeInTheDocument();
  });

  it("şekilsiz 200 yanıtında error ve retry gösterir (invalid-response)", async () => {
    vi.spyOn(client, "getRestaurantDetail").mockRejectedValue(
      new ApiError("invalid-response", "Sunucudan geçersiz yanıt alındı."),
    );
    render(<RestaurantDetailPage restaurantId={detail.id} />);

    await waitFor(() =>
      expect(screen.getByRole("heading", { name: "Detay yüklenemedi" })).toBeInTheDocument(),
    );
    expect(screen.getByRole("button", { name: "Tekrar dene" })).toBeInTheDocument();
  });
});
