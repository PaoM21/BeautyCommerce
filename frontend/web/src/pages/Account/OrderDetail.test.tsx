import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";

import OrderDetail from "./OrderDetail";
import * as orderService from "../../services/orderService";
import type { Order } from "../../services/orderService";

vi.mock("../../services/orderService", async (importOriginal) => {
  const actual =
    await importOriginal<typeof import("../../services/orderService")>();

  return {
    ...actual,
    getOrderById: vi.fn(),
  };
});

function makeOrder(overrides: Partial<Order>): Order {
  return {
    id: "order-1",
    userId: "user-1",
    orderNumber: "ORD-1",
    orderDate: "2026-01-01T00:00:00Z",
    status: "Processing",
    subTotal: 45000,
    shippingCost: 8000,
    tax: 0,
    total: 53000,
    shippingRecipientName: "Ana Pérez",
    shippingPhone: "3000000000",
    shippingAddressLine: "Calle 1",
    shippingCity: "Bogotá",
    shippingDepartment: "Cundinamarca",
    items: [],
    ...overrides,
  };
}

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={["/mi-cuenta/pedidos/order-1"]}>
        <Routes>
          <Route
            path="/mi-cuenta/pedidos/:id"
            element={<OrderDetail />}
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe("OrderDetail (customer)", () => {
  beforeEach(() => {
    vi.mocked(orderService.getOrderById).mockReset();
  });

  it("shows an error message when the order fails to load", async () => {
    vi.mocked(orderService.getOrderById).mockRejectedValue(
      new Error("not found")
    );

    renderPage();

    expect(
      await screen.findByText("No fue posible encontrar el pedido.")
    ).toBeInTheDocument();
  });

  it("marks steps up to the current status as completed in the timeline", async () => {
    vi.mocked(orderService.getOrderById).mockResolvedValue(
      makeOrder({ status: "Shipped" })
    );

    renderPage();

    await screen.findByText("ORD-1");

    const completedColor = "rgb(31, 31, 31)";
    const pendingColor = "rgb(221, 221, 221)";

    expect(
      screen.getByText("Pedido recibido").previousSibling
    ).toHaveStyle({ backgroundColor: completedColor });
    expect(
      screen.getByText("Preparando pedido").previousSibling
    ).toHaveStyle({ backgroundColor: completedColor });
    expect(
      screen.getByText("Pedido enviado").previousSibling
    ).toHaveStyle({ backgroundColor: completedColor });
    expect(
      screen.getByText("Pedido entregado").previousSibling
    ).toHaveStyle({ backgroundColor: pendingColor });
  });

  it("shows the carrier and tracking number only when both are set", async () => {
    vi.mocked(orderService.getOrderById).mockResolvedValue(
      makeOrder({ status: "Shipped", carrier: "Envía", trackingNumber: "TRK-1" })
    );

    renderPage();

    expect(await screen.findByText("Envía")).toBeInTheDocument();
    expect(screen.getByText("TRK-1")).toBeInTheDocument();
  });

  it("does not show shipping details when tracking hasn't been set yet", async () => {
    vi.mocked(orderService.getOrderById).mockResolvedValue(
      makeOrder({ status: "Processing", carrier: null, trackingNumber: null })
    );

    renderPage();

    await screen.findByText("ORD-1");

    expect(screen.queryByText(/transportadora/i)).not.toBeInTheDocument();
  });

  it("renders order items with their quantity and subtotal", async () => {
    vi.mocked(orderService.getOrderById).mockResolvedValue(
      makeOrder({
        items: [
          {
            id: "item-1",
            productVariantId: "v1",
            productName: "Labial Mate Rojo",
            color: "Rojo",
            size: "",
            quantity: 2,
            unitPrice: 22500,
            subtotal: 45000,
          },
        ],
      })
    );

    renderPage();

    expect(await screen.findByText("Labial Mate Rojo")).toBeInTheDocument();
    expect(screen.getByText("Cantidad: 2")).toBeInTheDocument();
  });
});
