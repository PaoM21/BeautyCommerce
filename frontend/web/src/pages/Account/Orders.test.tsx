import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";

import Orders from "./Orders";
import * as orderService from "../../services/orderService";
import type { Order } from "../../services/orderService";

vi.mock("../../services/orderService", async (importOriginal) => {
  const actual =
    await importOriginal<typeof import("../../services/orderService")>();

  return {
    ...actual,
    getOrders: vi.fn(),
  };
});

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <Orders />
      </MemoryRouter>
    </QueryClientProvider>
  );
}

function makeOrder(overrides: Partial<Order>): Order {
  return {
    id: "order-1",
    userId: "user-1",
    orderNumber: "ORD-1",
    orderDate: "2026-01-01T00:00:00Z",
    status: "Pending",
    subTotal: 100,
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

describe("Orders (customer)", () => {
  beforeEach(() => {
    vi.mocked(orderService.getOrders).mockReset();
  });

  it("shows an error message when orders fail to load", async () => {
    vi.mocked(orderService.getOrders).mockRejectedValue(
      new Error("network error")
    );

    renderPage();

    expect(
      await screen.findByText("No fue posible cargar tus pedidos.")
    ).toBeInTheDocument();
  });

  it("shows the empty state when there are no orders", async () => {
    vi.mocked(orderService.getOrders).mockResolvedValue([]);

    renderPage();

    expect(
      await screen.findByText("Todavía no tienes pedidos")
    ).toBeInTheDocument();
  });

  it("renders each order with its translated status and links to its detail", async () => {
    vi.mocked(orderService.getOrders).mockResolvedValue([
      makeOrder({ id: "order-1", orderNumber: "ORD-1", status: "Shipped" }),
    ]);

    renderPage();

    expect(await screen.findByText("ORD-1")).toBeInTheDocument();
    expect(screen.getByText("Enviado")).toBeInTheDocument();
    expect(screen.getByRole("link")).toHaveAttribute(
      "href",
      "/mi-cuenta/pedidos/order-1"
    );
  });
});
