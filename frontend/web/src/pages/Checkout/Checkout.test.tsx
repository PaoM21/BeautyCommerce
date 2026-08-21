import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";

import Checkout from "./Checkout";
import { useCartStore } from "../../store/cartStore";
import * as cartService from "../../services/cartService";
import * as orderService from "../../services/orderService";
import type { ShoppingCart } from "../../types/cart";

const navigateMock = vi.fn();

vi.mock("react-router-dom", async (importOriginal) => {
  const actual = await importOriginal<typeof import("react-router-dom")>();

  return {
    ...actual,
    useNavigate: () => navigateMock,
  };
});

vi.mock("../../services/cartService");
vi.mock("../../services/orderService", async (importOriginal) => {
  const actual = await importOriginal<typeof import("../../services/orderService")>();

  return {
    ...actual,
    checkout: vi.fn(),
  };
});

const CART_WITH_ITEMS: ShoppingCart = {
  items: [
    {
      productId: "p1",
      productVariantId: "v1",
      productName: "Labial Mate Rojo",
      color: "Rojo",
      size: "",
      quantity: 1,
      unitPrice: 45000,
      subtotal: 45000,
    },
  ],
  total: 45000,
};

function renderCheckout() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <Checkout />
      </MemoryRouter>
    </QueryClientProvider>
  );
}

async function fillShippingForm(user: ReturnType<typeof userEvent.setup>) {
  await user.type(
    screen.getByLabelText(/nombre de quien recibe/i),
    "Ana Pérez"
  );
  await user.type(screen.getByLabelText(/teléfono de contacto/i), "3001234567");
  await user.type(
    screen.getByLabelText(/^dirección$/i),
    "Calle 123 #45-67"
  );
  await user.type(screen.getByLabelText(/^ciudad$/i), "Bogotá");
  await user.click(screen.getByRole("combobox", { name: /departamento/i }));
  await user.click(
    await screen.findByRole("option", { name: "Bogotá D.C." })
  );
}

describe("Checkout", () => {
  beforeEach(() => {
    navigateMock.mockReset();
    vi.mocked(cartService.getShoppingCart).mockReset();
    vi.mocked(orderService.checkout).mockReset();
    useCartStore.setState({ itemCount: 0 });
  });

  it("shows the empty-cart state and does not render the form", async () => {
    vi.mocked(cartService.getShoppingCart).mockResolvedValue({
      items: [],
      total: 0,
    });

    renderCheckout();

    expect(
      await screen.findByText("Tu carrito está vacío")
    ).toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: /confirmar y pagar/i })
    ).not.toBeInTheDocument();
  });

  it("keeps the submit button disabled until the shipping address is complete", async () => {
    vi.mocked(cartService.getShoppingCart).mockResolvedValue(CART_WITH_ITEMS);

    const user = userEvent.setup();

    renderCheckout();

    const submitButton = await screen.findByRole("button", {
      name: /confirmar y pagar/i,
    });

    expect(submitButton).toBeDisabled();
    expect(
      screen.getByText("Completa la dirección de envío para continuar.")
    ).toBeInTheDocument();

    await fillShippingForm(user);

    expect(submitButton).toBeEnabled();
  });

  it("shows the Bogotá shipping cost once a city is entered", async () => {
    vi.mocked(cartService.getShoppingCart).mockResolvedValue(CART_WITH_ITEMS);

    const user = userEvent.setup();

    renderCheckout();

    await screen.findByRole("button", { name: /confirmar y pagar/i });

    expect(screen.getByText("Ingresa tu ciudad")).toBeInTheDocument();

    await user.type(screen.getByLabelText(/^ciudad$/i), "Bogotá");

    expect(screen.getByText("$8.000")).toBeInTheDocument();
  });

  it("submits the order and redirects to the success page", async () => {
    vi.mocked(cartService.getShoppingCart).mockResolvedValue(CART_WITH_ITEMS);
    vi.mocked(orderService.checkout).mockResolvedValue({
      orderId: "order-1",
    });

    const user = userEvent.setup();

    renderCheckout();

    await screen.findByRole("button", { name: /confirmar y pagar/i });
    await fillShippingForm(user);

    await user.click(
      screen.getByRole("button", { name: /confirmar y pagar/i })
    );

    await waitFor(() => {
      expect(navigateMock).toHaveBeenCalledWith(
        "/checkout/success?orderId=order-1"
      );
    });

    expect(useCartStore.getState().itemCount).toBe(0);
  });

  it("shows an error alert when the checkout request fails", async () => {
    vi.mocked(cartService.getShoppingCart).mockResolvedValue(CART_WITH_ITEMS);
    vi.mocked(orderService.checkout).mockRejectedValue(
      new Error("out of stock")
    );

    const user = userEvent.setup();

    renderCheckout();

    await screen.findByRole("button", { name: /confirmar y pagar/i });
    await fillShippingForm(user);

    await user.click(
      screen.getByRole("button", { name: /confirmar y pagar/i })
    );

    expect(
      await screen.findByText(
        "No fue posible procesar tu pedido. Verifica el stock y vuelve a intentarlo."
      )
    ).toBeInTheDocument();
    expect(navigateMock).not.toHaveBeenCalled();
  });
});
