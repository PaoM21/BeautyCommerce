import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { renderHook, waitFor } from "@testing-library/react";
import type { ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { useWishlist } from "./useWishlist";
import * as wishlistService from "../services/wishlistService";

vi.mock("../services/wishlistService");

function wrapper({ children }: { children: ReactNode }) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  return (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
}

describe("useWishlist", () => {
  beforeEach(() => {
    vi.mocked(wishlistService.getWishlist).mockReset();
    vi.mocked(wishlistService.addToWishlist).mockReset();
    vi.mocked(wishlistService.removeFromWishlist).mockReset();
  });

  it("reports a product as favorite only when it's in the wishlist", async () => {
    vi.mocked(wishlistService.getWishlist).mockResolvedValue([
      { productId: "p1" },
    ]);

    const { result } = renderHook(() => useWishlist(), { wrapper });

    await waitFor(() => expect(result.current.isLoading).toBe(false));

    expect(result.current.isFavorite("p1")).toBe(true);
    expect(result.current.isFavorite("p2")).toBe(false);
  });

  it("adds a product that isn't in the wishlist yet", async () => {
    vi.mocked(wishlistService.getWishlist).mockResolvedValue([]);
    vi.mocked(wishlistService.addToWishlist).mockResolvedValue({});

    const { result } = renderHook(() => useWishlist(), { wrapper });

    await waitFor(() => expect(result.current.isLoading).toBe(false));

    result.current.toggleFavorite("p1");

    await waitFor(() => {
      expect(wishlistService.addToWishlist).toHaveBeenCalled();
    });
    // React Query v5 passes a second (mutation context) argument to a
    // mutationFn that isn't wrapped in an arrow function.
    expect(
      vi.mocked(wishlistService.addToWishlist).mock.calls[0][0]
    ).toBe("p1");
    expect(wishlistService.removeFromWishlist).not.toHaveBeenCalled();
  });

  it("removes a product that's already in the wishlist", async () => {
    vi.mocked(wishlistService.getWishlist).mockResolvedValue([
      { productId: "p1" },
    ]);
    vi.mocked(wishlistService.removeFromWishlist).mockResolvedValue({});

    const { result } = renderHook(() => useWishlist(), { wrapper });

    await waitFor(() => expect(result.current.isLoading).toBe(false));

    result.current.toggleFavorite("p1");

    await waitFor(() => {
      expect(wishlistService.removeFromWishlist).toHaveBeenCalled();
    });
    expect(
      vi.mocked(wishlistService.removeFromWishlist).mock.calls[0][0]
    ).toBe("p1");
    expect(wishlistService.addToWishlist).not.toHaveBeenCalled();
  });

  it("ignores toggles while an add/remove mutation is already pending", async () => {
    vi.mocked(wishlistService.getWishlist).mockResolvedValue([]);

    let resolveAdd: (() => void) | undefined;
    vi.mocked(wishlistService.addToWishlist).mockReturnValue(
      new Promise((resolve) => {
        resolveAdd = () => resolve({});
      })
    );

    const { result } = renderHook(() => useWishlist(), { wrapper });

    await waitFor(() => expect(result.current.isLoading).toBe(false));

    result.current.toggleFavorite("p1");

    await waitFor(() => expect(result.current.isUpdating).toBe(true));

    result.current.toggleFavorite("p1");

    expect(wishlistService.addToWishlist).toHaveBeenCalledTimes(1);

    resolveAdd?.();
  });
});
