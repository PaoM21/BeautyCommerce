import { beforeEach, describe, expect, it, vi } from "vitest";

import { api } from "./api";
import { getBrands, getCategories } from "./catalogService";

vi.mock("./api", () => ({
  api: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}));

describe("getBrands", () => {
  beforeEach(() => {
    vi.mocked(api.get).mockReset();
  });

  it("returns the array directly when the API responds with a bare array", async () => {
    vi.mocked(api.get).mockResolvedValue({
      data: [{ id: "b1", name: "Brand 1" }],
    });

    await expect(getBrands()).resolves.toEqual([{ id: "b1", name: "Brand 1" }]);
  });

  it("unwraps the array from an ApiResponse envelope", async () => {
    vi.mocked(api.get).mockResolvedValue({
      data: { data: [{ id: "b1", name: "Brand 1" }] },
    });

    await expect(getBrands()).resolves.toEqual([{ id: "b1", name: "Brand 1" }]);
  });

  it("falls back to an empty array for an unexpected shape", async () => {
    vi.mocked(api.get).mockResolvedValue({ data: null });

    await expect(getBrands()).resolves.toEqual([]);
  });
});

describe("getCategories", () => {
  beforeEach(() => {
    vi.mocked(api.get).mockReset();
  });

  it("maps each item and defaults a missing slug to an empty string", async () => {
    vi.mocked(api.get).mockResolvedValue({
      data: [{ id: "c1", name: "Rostro" }],
    });

    await expect(getCategories()).resolves.toEqual([
      { id: "c1", name: "Rostro", slug: "" },
    ]);
  });

  it("keeps an existing slug and unwraps an ApiResponse envelope", async () => {
    vi.mocked(api.get).mockResolvedValue({
      data: { data: [{ id: "c1", name: "Rostro", slug: "rostro" }] },
    });

    await expect(getCategories()).resolves.toEqual([
      { id: "c1", name: "Rostro", slug: "rostro" },
    ]);
  });
});
