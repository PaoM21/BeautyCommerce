import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";

import Dashboard from "./Dashboard";
import * as dashboardService from "../../../services/dashboardService";
import type { Dashboard as DashboardData } from "../../../types/dashboard";

vi.mock("../../../services/dashboardService");

function makeDashboard(overrides: Partial<DashboardData>): DashboardData {
  return {
    totalProducts: 10,
    totalOrders: 20,
    totalCustomers: 5,
    totalSales: 1000000,
    salesThisMonth: 250000,
    pendingOrders: 2,
    lowStockProducts: 1,
    outOfStockProducts: 0,
    lastOrders: [],
    salesByMonth: [],
    ...overrides,
  };
}

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <Dashboard />
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe("Dashboard", () => {
  beforeEach(() => {
    vi.mocked(dashboardService.getDashboard).mockReset();
  });

  it("shows an error message when the dashboard fails to load", async () => {
    vi.mocked(dashboardService.getDashboard).mockRejectedValue(
      new Error("network error")
    );

    renderPage();

    expect(
      await screen.findByText("No fue posible cargar el dashboard.")
    ).toBeInTheDocument();
  });

  it("renders the summary cards with formatted values", async () => {
    vi.mocked(dashboardService.getDashboard).mockResolvedValue(
      makeDashboard({ totalProducts: 42, totalSales: 1500000 })
    );

    renderPage();

    expect(await screen.findByText("42")).toBeInTheDocument();
    expect(screen.getByText("$1.500.000")).toBeInTheDocument();
  });

  it("shows the no-orders placeholder when there are no recent orders", async () => {
    vi.mocked(dashboardService.getDashboard).mockResolvedValue(
      makeDashboard({ lastOrders: [] })
    );

    renderPage();

    expect(
      await screen.findByText("Todavía no hay pedidos.")
    ).toBeInTheDocument();
  });

  it("renders recent orders linking to their detail page", async () => {
    vi.mocked(dashboardService.getDashboard).mockResolvedValue(
      makeDashboard({
        lastOrders: [
          {
            id: "order-1",
            orderNumber: "ORD-1",
            customer: "Ana Pérez",
            total: 53000,
            createdAt: "2026-01-01T00:00:00Z",
          },
        ],
      })
    );

    renderPage();

    expect(await screen.findByText("ORD-1")).toBeInTheDocument();
    expect(screen.getByText("Cliente: Ana Pérez")).toBeInTheDocument();
    expect(screen.getByText("ORD-1").closest("a")).toHaveAttribute(
      "href",
      "/admin/pedidos/order-1"
    );
  });

  it("shows monthly sales only when there is data for it", async () => {
    vi.mocked(dashboardService.getDashboard).mockResolvedValue(
      makeDashboard({ salesByMonth: [] })
    );

    renderPage();

    await screen.findByText("Últimos pedidos");

    expect(screen.queryByText("Ventas por mes")).not.toBeInTheDocument();
  });

  it("renders monthly sales rows when present", async () => {
    vi.mocked(dashboardService.getDashboard).mockResolvedValue(
      makeDashboard({
        salesByMonth: [{ month: "Enero 2026", total: 300000 }],
      })
    );

    renderPage();

    expect(await screen.findByText("Ventas por mes")).toBeInTheDocument();
    expect(screen.getByText("Enero 2026")).toBeInTheDocument();
    expect(screen.getByText("$300.000")).toBeInTheDocument();
  });
});
