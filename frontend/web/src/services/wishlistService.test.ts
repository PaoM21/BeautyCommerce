import { beforeEach, describe, expect, it, vi } from "vitest";

import { api } from "./api";
import {
  addToWishlist,
  getWishlist,
  removeFromWishlist,
} from "./wishlistService";

vi.mock("./api", () => ({
  api: {
    get: vi.fn(),
    post: vi.fn(),
    delete: vi.fn(),
  },
}));

describe("getWishlist", () => {
  beforeEach(() => {
    vi.mocked(api.get).mockReset();
  });

  it("returns the wishlist items on success", async () => {
    vi.mocked(api.get).mockResolvedValue({
      data: [{ productId: "p1" }],
    });

    await expect(getWishlist()).resolves.toEqual([{ productId: "p1" }]);
  });

  it("returns an empty array for an anonymous (401) request instead of throwing", async () => {
    vi.mocked(api.get).mockRejectedValue({
      response: { status: 401 },
    });

    await expect(getWishlist()).resolves.toEqual([]);
  });

  it("returns an empty array for a forbidden (403) request instead of throwing", async () => {
    vi.mocked(api.get).mockRejectedValue({
      response: { status: 403 },
    });

    await expect(getWishlist()).resolves.toEqual([]);
  });

  it("re-throws any other error", async () => {
    vi.mocked(api.get).mockRejectedValue({
      response: { status: 500 },
    });

    await expect(getWishlist()).rejects.toBeTruthy();
  });
});

describe("addToWishlist / removeFromWishlist", () => {
  beforeEach(() => {
    vi.mocked(api.post).mockReset();
    vi.mocked(api.delete).mockReset();
  });

  it("posts the productId to add an item", async () => {
    vi.mocked(api.post).mockResolvedValue({ data: { id: "w1" } });

    await addToWishlist("p1");

    expect(api.post).toHaveBeenCalledWith("/Wishlist", { productId: "p1" });
  });

  it("deletes by productId to remove an item", async () => {
    vi.mocked(api.delete).mockResolvedValue({ data: undefined });

    await removeFromWishlist("p1");

    expect(api.delete).toHaveBeenCalledWith("/Wishlist/p1");
  });
});
