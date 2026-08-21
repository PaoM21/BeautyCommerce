import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";

import AdminOrderDetail from "./OrderDetail";
import * as orderService from "../../../services/orderService";
import type { Order } from "../../../services/orderService";

const navigateMock = vi.fn();

vi.mock("react-router-dom", async (importOriginal) => {
  const actual = await importOriginal<typeof import("react-router-dom")>();

  return {
    ...actual,
    useNavigate: () => navigateMock,
  };
});

vi.mock("../../../services/orderService", async (importOriginal) => {
  const actual =
    await importOriginal<typeof import("../../../services/orderService")>();

  return {
    ...actual,
    getAdminOrderById: vi.fn(),
    updateOrderStatus: vi.fn(),
    updateOrderShipping: vi.fn(),
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
      <MemoryRouter initialEntries={["/admin/pedidos/order-1"]}>
        <Routes>
          <Route
            path="/admin/pedidos/:id"
            element={<AdminOrderDetail />}
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe("AdminOrderDetail", () => {
  beforeEach(() => {
    navigateMock.mockReset();
    vi.mocked(orderService.getAdminOrderById).mockReset();
    vi.mocked(orderService.updateOrderStatus).mockReset();
    vi.mocked(orderService.updateOrderShipping).mockReset();
  });

  it("shows an error message when the order fails to load", async () => {
    vi.mocked(orderService.getAdminOrderById).mockRejectedValue(
      new Error("not found")
    );

    renderPage();

    expect(
      await screen.findByText("No fue posible encontrar el pedido.")
    ).toBeInTheDocument();
  });

  it("only offers the valid next statuses for the current status", async () => {
    vi.mocked(orderService.getAdminOrderById).mockResolvedValue(
      makeOrder({ status: "Processing" })
    );

    const user = userEvent.setup();

    renderPage();

    await screen.findByText("ORD-1");

    await user.click(
      screen.getByText("Selecciona un nuevo estado")
    );

    expect(screen.getByRole("option", { name: "Enviado" })).toBeInTheDocument();
    expect(
      screen.queryByRole("option", { name: "Entregado" })
    ).not.toBeInTheDocument();
  });

  it("shows a terminal message and no status selector for a Delivered order", async () => {
    vi.mocked(orderService.getAdminOrderById).mockResolvedValue(
      makeOrder({ status: "Delivered" })
    );

    renderPage();

    expect(
      await screen.findByText(
        "Este pedido está en un estado final y no admite más cambios."
      )
    ).toBeInTheDocument();
    expect(
      screen.queryByText("Selecciona un nuevo estado")
    ).not.toBeInTheDocument();
  });

  it("updates the status and shows a success message", async () => {
    vi.mocked(orderService.getAdminOrderById).mockResolvedValue(
      makeOrder({ status: "Processing" })
    );
    vi.mocked(orderService.updateOrderStatus).mockResolvedValue(undefined);

    const user = userEvent.setup();

    renderPage();

    await screen.findByText("ORD-1");

    const updateButton = screen.getByRole("button", {
      name: /actualizar estado/i,
    });
    expect(updateButton).toBeDisabled();

    await user.click(
      screen.getByText("Selecciona un nuevo estado")
    );
    await user.click(screen.getByRole("option", { name: "Enviado" }));

    expect(updateButton).toBeEnabled();

    await user.click(updateButton);

    await waitFor(() => {
      expect(orderService.updateOrderStatus).toHaveBeenCalledWith(
        "order-1",
        "Shipped"
      );
    });

    expect(
      await screen.findByText("Estado actualizado correctamente.")
    ).toBeInTheDocument();
  });

  it("shows the backend error detail when updating the status fails", async () => {
    vi.mocked(orderService.getAdminOrderById).mockResolvedValue(
      makeOrder({ status: "Processing" })
    );
    vi.mocked(orderService.updateOrderStatus).mockRejectedValue({
      response: { data: { detail: "Transición de estado no permitida." } },
    });

    const user = userEvent.setup();

    renderPage();

    await screen.findByText("ORD-1");

    await user.click(
      screen.getByText("Selecciona un nuevo estado")
    );
    await user.click(screen.getByRole("option", { name: "Enviado" }));
    await user.click(
      screen.getByRole("button", { name: /actualizar estado/i })
    );

    expect(
      await screen.findByText("Transición de estado no permitida.")
    ).toBeInTheDocument();
  });

  it("keeps the shipping save button disabled until carrier and tracking are set", async () => {
    vi.mocked(orderService.getAdminOrderById).mockResolvedValue(
      makeOrder({ status: "Processing", carrier: null, trackingNumber: null })
    );

    const user = userEvent.setup();

    renderPage();

    await screen.findByText("ORD-1");

    const saveButton = screen.getByRole("button", { name: /guardar envío/i });
    expect(saveButton).toBeDisabled();

    await user.click(
      screen.getByText("Selecciona la transportadora")
    );
    await user.click(screen.getByRole("option", { name: "Envía" }));

    expect(saveButton).toBeDisabled();

    await user.type(screen.getByLabelText(/número de guía/i), "TRK-1");

    expect(saveButton).toBeEnabled();
  });

  it("saves the shipping info and shows a success message", async () => {
    vi.mocked(orderService.getAdminOrderById).mockResolvedValue(
      makeOrder({ status: "Processing", carrier: null, trackingNumber: null })
    );
    vi.mocked(orderService.updateOrderShipping).mockResolvedValue(undefined);

    const user = userEvent.setup();

    renderPage();

    await screen.findByText("ORD-1");

    await user.click(
      screen.getByText("Selecciona la transportadora")
    );
    await user.click(screen.getByRole("option", { name: "Envía" }));
    await user.type(screen.getByLabelText(/número de guía/i), "TRK-1");
    await user.click(screen.getByRole("button", { name: /guardar envío/i }));

    await waitFor(() => {
      expect(orderService.updateOrderShipping).toHaveBeenCalledWith(
        "order-1",
        "Envía",
        "TRK-1"
      );
    });

    expect(
      await screen.findByText("Información de envío guardada correctamente.")
    ).toBeInTheDocument();
  });
});
