import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

import AdminInventory from "./AdminInventory";
import * as inventoryService from "../../../services/inventoryService";
import type { InventoryItem } from "../../../services/inventoryService";

vi.mock("../../../services/inventoryService");

function makeItem(overrides: Partial<InventoryItem>): InventoryItem {
  return {
    productVariantId: "v1",
    productId: "p1",
    productName: "Labial Mate Rojo",
    sku: "SKU-1",
    barcode: "BC-1",
    color: "Rojo",
    size: "",
    price: 45000,
    stock: 20,
    minimumStock: 5,
    stockStatus: "InStock",
    ...overrides,
  };
}

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <AdminInventory />
    </QueryClientProvider>
  );
}

describe("AdminInventory", () => {
  beforeEach(() => {
    vi.mocked(inventoryService.getInventory).mockReset();
    vi.mocked(inventoryService.updateInventoryStock).mockReset();
  });

  it("shows an error message when the inventory fails to load", async () => {
    vi.mocked(inventoryService.getInventory).mockRejectedValue(
      new Error("network error")
    );

    renderPage();

    expect(
      await screen.findByText("No fue posible cargar el inventario.")
    ).toBeInTheDocument();
  });

  it("summarizes total variants, low stock and out of stock counts", async () => {
    vi.mocked(inventoryService.getInventory).mockResolvedValue([
      makeItem({
        productVariantId: "v1",
        productName: "Labial Mate Rojo",
        stockStatus: "InStock",
      }),
      makeItem({
        productVariantId: "v2",
        productName: "Base Líquida",
        stockStatus: "LowStock",
      }),
      makeItem({
        productVariantId: "v3",
        productName: "Sombra Bronze",
        stockStatus: "OutOfStock",
      }),
    ]);

    renderPage();

    await screen.findByText("Labial Mate Rojo");

    expect(screen.getByText("Variantes").nextSibling).toHaveTextContent("3");
    expect(screen.getByText("Stock bajo").nextSibling).toHaveTextContent("1");
    expect(screen.getByText("Agotados").nextSibling).toHaveTextContent("1");
  });

  it("filters by product name or SKU", async () => {
    vi.mocked(inventoryService.getInventory).mockResolvedValue([
      makeItem({
        productVariantId: "v1",
        productName: "Labial Mate Rojo",
        sku: "LIP-01",
      }),
      makeItem({
        productVariantId: "v2",
        productName: "Base Líquida",
        sku: "BASE-02",
      }),
    ]);

    const user = userEvent.setup();

    renderPage();

    await screen.findByText("Labial Mate Rojo");
    expect(screen.getByText("Base Líquida")).toBeInTheDocument();

    await user.type(
      screen.getByPlaceholderText(/buscar producto o sku/i),
      "base-02"
    );

    expect(screen.queryByText("Labial Mate Rojo")).not.toBeInTheDocument();
    expect(screen.getByText("Base Líquida")).toBeInTheDocument();
  });

  it("filters by stock status", async () => {
    vi.mocked(inventoryService.getInventory).mockResolvedValue([
      makeItem({
        productVariantId: "v1",
        productName: "Labial Mate Rojo",
        stockStatus: "InStock",
      }),
      makeItem({
        productVariantId: "v2",
        productName: "Base Líquida",
        stockStatus: "OutOfStock",
      }),
    ]);

    const user = userEvent.setup();

    renderPage();

    await screen.findByText("Labial Mate Rojo");

    // The filter Select has no accessible label, so it exposes an empty
    // combobox name; click its visible display text instead.
    await user.click(screen.getByText("Todos"));
    await user.click(screen.getByRole("option", { name: "Agotados" }));

    expect(screen.queryByText("Labial Mate Rojo")).not.toBeInTheDocument();
    expect(screen.getByText("Base Líquida")).toBeInTheDocument();
  });

  it("shows the empty-results message when no item matches the filters", async () => {
    vi.mocked(inventoryService.getInventory).mockResolvedValue([
      makeItem({ productVariantId: "v1", productName: "Labial Mate Rojo" }),
    ]);

    const user = userEvent.setup();

    renderPage();

    await screen.findByText("Labial Mate Rojo");

    await user.type(
      screen.getByPlaceholderText(/buscar producto o sku/i),
      "no existe"
    );

    expect(
      screen.getByText("No se encontraron variantes")
    ).toBeInTheDocument();
  });

  it("keeps the save button disabled until quantity and reason are filled", async () => {
    vi.mocked(inventoryService.getInventory).mockResolvedValue([
      makeItem({ productVariantId: "v1", productName: "Labial Mate Rojo" }),
    ]);

    const user = userEvent.setup();

    renderPage();

    await screen.findByText("Labial Mate Rojo");
    await user.click(screen.getByRole("button", { name: /ajustar/i }));

    const saveButton = screen.getByRole("button", { name: /guardar ajuste/i });
    expect(saveButton).toBeDisabled();

    await user.type(screen.getByLabelText(/^cantidad$/i), "5");
    expect(saveButton).toBeDisabled();

    await user.type(screen.getByLabelText(/^motivo$/i), "Compra a proveedor");
    expect(saveButton).toBeEnabled();
  });

  it("does not submit when the quantity is not a positive integer", async () => {
    vi.mocked(inventoryService.getInventory).mockResolvedValue([
      makeItem({ productVariantId: "v1", productName: "Labial Mate Rojo" }),
    ]);

    const user = userEvent.setup();

    renderPage();

    await screen.findByText("Labial Mate Rojo");
    await user.click(screen.getByRole("button", { name: /ajustar/i }));

    await user.type(screen.getByLabelText(/^cantidad$/i), "0");
    await user.type(screen.getByLabelText(/^motivo$/i), "Ajuste inválido");
    await user.click(screen.getByRole("button", { name: /guardar ajuste/i }));

    expect(inventoryService.updateInventoryStock).not.toHaveBeenCalled();
  });

  it("submits an entry adjustment and closes the modal on success", async () => {
    vi.mocked(inventoryService.getInventory).mockResolvedValue([
      makeItem({ productVariantId: "v1", productName: "Labial Mate Rojo" }),
    ]);
    vi.mocked(inventoryService.updateInventoryStock).mockResolvedValue(
      undefined
    );

    const user = userEvent.setup();

    renderPage();

    await screen.findByText("Labial Mate Rojo");
    await user.click(screen.getByRole("button", { name: /ajustar/i }));

    await user.type(screen.getByLabelText(/^cantidad$/i), "10");
    await user.type(screen.getByLabelText(/^motivo$/i), "Compra a proveedor");
    await user.click(screen.getByRole("button", { name: /guardar ajuste/i }));

    await waitFor(() => {
      expect(inventoryService.updateInventoryStock).toHaveBeenCalled();
    });

    // React Query v5 passes a second (mutation context) argument to a
    // mutationFn that isn't wrapped in an arrow function; only the first
    // argument is the actual payload we care about here.
    expect(
      vi.mocked(inventoryService.updateInventoryStock).mock.calls[0][0]
    ).toEqual({
      productVariantId: "v1",
      quantity: 10,
      isEntry: true,
      reason: "Compra a proveedor",
    });

    await waitFor(() => {
      expect(screen.queryByText("Ajustar inventario")).not.toBeInTheDocument();
    });
  });
});
