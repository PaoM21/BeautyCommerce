import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";

import AdminOrders from "./AdminOrders";
import * as orderService from "../../../services/orderService";
import type { Order } from "../../../services/orderService";

vi.mock("../../../services/orderService", async (importOriginal) => {
  const actual =
    await importOriginal<typeof import("../../../services/orderService")>();

  return {
    ...actual,
    getAdminOrders: vi.fn(),
  };
});

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <AdminOrders />
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
    total: 108000,
    shippingRecipientName: "Ana Pérez",
    shippingPhone: "3000000000",
    shippingAddressLine: "Calle 1",
    shippingCity: "Bogotá",
    shippingDepartment: "Cundinamarca",
    items: [],
    ...overrides,
  };
}

describe("AdminOrders", () => {
  beforeEach(() => {
    vi.mocked(orderService.getAdminOrders).mockReset();
  });

  it("shows the empty state when there are no orders", async () => {
    vi.mocked(orderService.getAdminOrders).mockResolvedValue([]);

    renderPage();

    expect(await screen.findByText("No hay pedidos")).toBeInTheDocument();
  });

  it("shows an error message when the orders fail to load", async () => {
    vi.mocked(orderService.getAdminOrders).mockRejectedValue(
      new Error("network error")
    );

    renderPage();

    expect(
      await screen.findByText("No fue posible cargar los pedidos.")
    ).toBeInTheDocument();
  });

  it("renders each order with its number, translated status and total", async () => {
    vi.mocked(orderService.getAdminOrders).mockResolvedValue([
      makeOrder({
        id: "order-1",
        orderNumber: "ORD-1",
        status: "Processing",
        total: 53000,
      }),
      makeOrder({
        id: "order-2",
        orderNumber: "ORD-2",
        status: "Delivered",
        total: 90000,
      }),
    ]);

    renderPage();

    expect(await screen.findByText("ORD-1")).toBeInTheDocument();
    expect(screen.getByText("Preparando pedido")).toBeInTheDocument();
    expect(screen.getByText("$53.000")).toBeInTheDocument();

    expect(screen.getByText("ORD-2")).toBeInTheDocument();
    expect(screen.getByText("Entregado")).toBeInTheDocument();
    expect(screen.getByText("$90.000")).toBeInTheDocument();

    expect(
      screen.getByRole("link", { name: /ORD-1.*Preparando pedido/s })
    ).toHaveAttribute("href", "/admin/pedidos/order-1");
  });
});
