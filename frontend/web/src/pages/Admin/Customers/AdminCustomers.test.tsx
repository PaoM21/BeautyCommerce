import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

import AdminCustomers from "./AdminCustomers";
import * as customerService from "../../../services/customerService";
import type { AdminCustomer } from "../../../services/customerService";

vi.mock("../../../services/customerService");

function makeCustomer(overrides: Partial<AdminCustomer>): AdminCustomer {
  return {
    id: "u1",
    fullName: "Ana Pérez",
    email: "ana@test.com",
    role: "Customer",
    isActive: true,
    ...overrides,
  };
}

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <AdminCustomers />
    </QueryClientProvider>
  );
}

describe("AdminCustomers", () => {
  beforeEach(() => {
    vi.mocked(customerService.getAdminCustomers).mockReset();
  });

  it("shows an error message when customers fail to load", async () => {
    vi.mocked(customerService.getAdminCustomers).mockRejectedValue(
      new Error("network error")
    );

    renderPage();

    expect(
      await screen.findByText("No fue posible cargar los clientes.")
    ).toBeInTheDocument();
  });

  it("summarizes total, active and inactive customers", async () => {
    vi.mocked(customerService.getAdminCustomers).mockResolvedValue([
      makeCustomer({ id: "u1", fullName: "Ana Pérez", isActive: true }),
      makeCustomer({ id: "u2", fullName: "Beatriz Gómez", isActive: true }),
      makeCustomer({ id: "u3", fullName: "Carla Ruiz", isActive: false }),
    ]);

    renderPage();

    await screen.findByText("Ana Pérez");

    expect(screen.getByText("Total clientes").nextSibling).toHaveTextContent(
      "3"
    );
    expect(
      screen.getByText("Clientes activos").nextSibling
    ).toHaveTextContent("2");
    expect(
      screen.getByText("Clientes inactivos").nextSibling
    ).toHaveTextContent("1");
  });

  it("filters customers by name or email", async () => {
    vi.mocked(customerService.getAdminCustomers).mockResolvedValue([
      makeCustomer({ id: "u1", fullName: "Ana Pérez", email: "ana@test.com" }),
      makeCustomer({
        id: "u2",
        fullName: "Beatriz Gómez",
        email: "beatriz@test.com",
      }),
    ]);

    const user = userEvent.setup();

    renderPage();

    await screen.findByText("Ana Pérez");
    expect(screen.getByText("Beatriz Gómez")).toBeInTheDocument();

    await user.type(
      screen.getByPlaceholderText(/buscar por nombre o correo/i),
      "beatriz@test.com"
    );

    expect(screen.queryByText("Ana Pérez")).not.toBeInTheDocument();
    expect(screen.getByText("Beatriz Gómez")).toBeInTheDocument();
    expect(
      screen.getByText("Mostrando 1 de 2 clientes")
    ).toBeInTheDocument();
  });

  it("shows the no-results message and lets the user clear the search", async () => {
    vi.mocked(customerService.getAdminCustomers).mockResolvedValue([
      makeCustomer({ id: "u1", fullName: "Ana Pérez" }),
    ]);

    const user = userEvent.setup();

    renderPage();

    await screen.findByText("Ana Pérez");

    await user.type(
      screen.getByPlaceholderText(/buscar por nombre o correo/i),
      "no existe"
    );

    expect(
      screen.getByText("No se encontraron clientes")
    ).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: /limpiar/i }));

    expect(screen.getByText("Ana Pérez")).toBeInTheDocument();
  });
});
