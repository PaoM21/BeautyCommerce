import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";

import Cart from "./Cart";
import { useCartStore } from "../../store/cartStore";
import * as cartService from "../../services/cartService";
import type { ShoppingCart } from "../../types/cart";

vi.mock("../../services/cartService");

function renderCart() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <Cart />
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe("Cart", () => {
  beforeEach(() => {
    vi.mocked(cartService.getShoppingCart).mockReset();
    useCartStore.setState({ itemCount: 0 });
  });

  it("shows the empty state when there are no items", async () => {
    vi.mocked(cartService.getShoppingCart).mockResolvedValue({
      items: [],
      total: 0,
    });

    renderCart();

    expect(
      await screen.findByText("Tu carrito está vacío")
    ).toBeInTheDocument();
  });

  it("shows an error message when the cart fails to load", async () => {
    vi.mocked(cartService.getShoppingCart).mockRejectedValue(
      new Error("network error")
    );

    renderCart();

    expect(
      await screen.findByText("No fue posible cargar el carrito.")
    ).toBeInTheDocument();
  });

  it("renders the items and total, and syncs the cart badge count", async () => {
    const cart: ShoppingCart = {
      items: [
        {
          productId: "p1",
          productVariantId: "v1",
          productName: "Labial Mate Rojo",
          color: "Rojo",
          size: "",
          quantity: 2,
          unitPrice: 45000,
          subtotal: 90000,
        },
      ],
      total: 90000,
    };

    vi.mocked(cartService.getShoppingCart).mockResolvedValue(cart);

    renderCart();

    expect(await screen.findByText("Labial Mate Rojo")).toBeInTheDocument();
    // The item subtotal and the cart total both render "$90.000" here,
    // since there's a single line item.
    expect(screen.getAllByText("$90.000")).toHaveLength(2);
    expect(useCartStore.getState().itemCount).toBe(2);
  });
});
