import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

import Wishlist from "./Wishlist";
import * as wishlistService from "../../services/wishlistService";
import type { WishlistItem } from "../../types/wishlist";

vi.mock("../../services/wishlistService");

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <Wishlist />
    </QueryClientProvider>
  );
}

function makeItem(overrides: Partial<WishlistItem>): WishlistItem {
  return {
    productId: "p1",
    productName: "Labial Mate Rojo",
    price: 45000,
    ...overrides,
  };
}

describe("Wishlist", () => {
  beforeEach(() => {
    vi.mocked(wishlistService.getWishlist).mockReset();
    vi.mocked(wishlistService.removeFromWishlist).mockReset();
  });

  it("shows an error message when the wishlist fails to load", async () => {
    vi.mocked(wishlistService.getWishlist).mockRejectedValue(
      new Error("network error")
    );

    renderPage();

    expect(
      await screen.findByText("No fue posible cargar tus favoritos.")
    ).toBeInTheDocument();
  });

  it("shows the empty state when the wishlist has no items", async () => {
    vi.mocked(wishlistService.getWishlist).mockResolvedValue([]);

    renderPage();

    expect(
      await screen.findByText("Tu lista está vacía")
    ).toBeInTheDocument();
  });

  it("renders each item with its name and price", async () => {
    vi.mocked(wishlistService.getWishlist).mockResolvedValue([
      makeItem({ productId: "p1", productName: "Labial Mate Rojo", price: 45000 }),
    ]);

    renderPage();

    expect(await screen.findByText("Labial Mate Rojo")).toBeInTheDocument();
    expect(screen.getByText("$45.000")).toBeInTheDocument();
  });

  it("removes an item and refetches the list", async () => {
    vi.mocked(wishlistService.getWishlist)
      .mockResolvedValueOnce([makeItem({ productId: "p1" })])
      .mockResolvedValueOnce([]);
    vi.mocked(wishlistService.removeFromWishlist).mockResolvedValue({});

    const user = userEvent.setup();

    renderPage();

    await screen.findByText("Labial Mate Rojo");

    await user.click(screen.getByRole("button", { name: /eliminar/i }));

    await waitFor(() => {
      expect(wishlistService.removeFromWishlist).toHaveBeenCalledWith("p1");
    });

    expect(
      await screen.findByText("Tu lista está vacía")
    ).toBeInTheDocument();
  });
});
