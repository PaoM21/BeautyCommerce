import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";

import AdminProductDetail from "./AdminProductDetail";
import * as productService from "../../../services/productService";
import * as inventoryService from "../../../services/inventoryService";
import type { Product } from "../../../types/product";

const navigateMock = vi.fn();

vi.mock("react-router-dom", async (importOriginal) => {
  const actual = await importOriginal<typeof import("react-router-dom")>();

  return {
    ...actual,
    useNavigate: () => navigateMock,
  };
});

vi.mock("../../../services/productService");
vi.mock("../../../services/inventoryService");

function makeProduct(overrides: Partial<Product>): Product {
  return {
    id: "p1",
    sku: "SKU-1",
    barcode: "",
    name: "Labial Mate Rojo",
    slug: "labial-mate-rojo",
    price: 45000,
    brandId: "brand-1",
    categoryId: "cat-1",
    images: [],
    variants: [],
    ...overrides,
  };
}

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={["/admin/productos/p1"]}>
        <Routes>
          <Route path="/admin/productos/:id" element={<AdminProductDetail />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe("AdminProductDetail", () => {
  beforeEach(() => {
    navigateMock.mockReset();
    vi.mocked(productService.getProductById).mockReset();
    vi.mocked(inventoryService.updateInventoryStock).mockReset();
  });

  it("shows an error message when the product fails to load", async () => {
    vi.mocked(productService.getProductById).mockRejectedValue(
      new Error("not found")
    );

    renderPage();

    expect(
      await screen.findByText("No fue posible encontrar el producto.")
    ).toBeInTheDocument();
  });

  it("shows fallback text for missing brand, category and description", async () => {
    vi.mocked(productService.getProductById).mockResolvedValue(
      makeProduct({ brand: undefined, category: undefined, description: undefined })
    );

    renderPage();

    await screen.findByText("Labial Mate Rojo");

    expect(screen.getByText("Sin marca")).toBeInTheDocument();
    expect(screen.getByText("Sin categoría")).toBeInTheDocument();
    expect(screen.getByText("Sin descripción.")).toBeInTheDocument();
  });

  it("shows placeholders when there are no images or variants", async () => {
    vi.mocked(productService.getProductById).mockResolvedValue(
      makeProduct({ images: [], variants: [] })
    );

    renderPage();

    expect(
      await screen.findByText("Este producto no tiene imágenes.")
    ).toBeInTheDocument();
    expect(
      screen.getByText("Este producto no tiene variantes.")
    ).toBeInTheDocument();
  });

  it("renders variants with SKU, price and stock", async () => {
    vi.mocked(productService.getProductById).mockResolvedValue(
      makeProduct({
        variants: [
          {
            id: "v1",
            productId: "p1",
            sku: "SKU-VAR-1",
            color: "Rojo",
            size: "Única",
            stock: 15,
            price: 45000,
          },
        ],
      })
    );

    renderPage();

    expect(await screen.findByText("SKU-VAR-1")).toBeInTheDocument();
    expect(screen.getByText("$45.000")).toBeInTheDocument();
    expect(screen.getByText("15")).toBeInTheDocument();
  });

  it("navigates to the edit page", async () => {
    vi.mocked(productService.getProductById).mockResolvedValue(makeProduct({}));

    const user = userEvent.setup();

    renderPage();

    await screen.findByText("Labial Mate Rojo");
    await user.click(screen.getByRole("button", { name: /editar producto/i }));

    expect(navigateMock).toHaveBeenCalledWith("/admin/productos/p1/editar");
  });

  describe("stock adjustment", () => {
    function variantProduct() {
      return makeProduct({
        variants: [
          {
            id: "v1",
            productId: "p1",
            sku: "SKU-VAR-1",
            color: "Rojo",
            size: "Única",
            stock: 15,
            price: 45000,
          },
        ],
      });
    }

    it("requires a quantity before updating stock", async () => {
      vi.mocked(productService.getProductById).mockResolvedValue(
        variantProduct()
      );

      const user = userEvent.setup();

      renderPage();
      await screen.findByText("SKU-VAR-1");

      await user.click(
        screen.getByRole("button", { name: /actualizar stock/i })
      );

      expect(
        screen.getByText("Ingresa una cantidad para actualizar el stock.")
      ).toBeInTheDocument();
      expect(inventoryService.updateInventoryStock).not.toHaveBeenCalled();
    });

    it("rejects a non-integer or zero quantity", async () => {
      vi.mocked(productService.getProductById).mockResolvedValue(
        variantProduct()
      );

      const user = userEvent.setup();

      renderPage();
      await screen.findByText("SKU-VAR-1");

      await user.type(screen.getByLabelText(/^cantidad$/i), "0");
      await user.click(
        screen.getByRole("button", { name: /actualizar stock/i })
      );

      expect(
        screen.getByText(
          "La cantidad debe ser un número entero diferente de cero."
        )
      ).toBeInTheDocument();
    });

    it("registers a positive quantity as a stock entry with the default reason", async () => {
      vi.mocked(productService.getProductById).mockResolvedValue(
        variantProduct()
      );
      vi.mocked(inventoryService.updateInventoryStock).mockResolvedValue(
        undefined
      );

      const user = userEvent.setup();

      renderPage();
      await screen.findByText("SKU-VAR-1");

      await user.type(screen.getByLabelText(/^cantidad$/i), "5");
      await user.click(
        screen.getByRole("button", { name: /actualizar stock/i })
      );

      await waitFor(() => {
        expect(inventoryService.updateInventoryStock).toHaveBeenCalled();
      });

      expect(
        vi.mocked(inventoryService.updateInventoryStock).mock.calls[0][0]
      ).toEqual({
        productVariantId: "v1",
        quantity: 5,
        isEntry: true,
        reason: "Ajuste manual de inventario",
      });

      expect(
        await screen.findByText("Stock actualizado correctamente.")
      ).toBeInTheDocument();
    });

    it("registers a negative quantity as a stock exit with the given reason", async () => {
      vi.mocked(productService.getProductById).mockResolvedValue(
        variantProduct()
      );
      vi.mocked(inventoryService.updateInventoryStock).mockResolvedValue(
        undefined
      );

      const user = userEvent.setup();

      renderPage();
      await screen.findByText("SKU-VAR-1");

      await user.type(screen.getByLabelText(/^cantidad$/i), "-3");
      await user.type(screen.getByLabelText(/^motivo$/i), "Producto dañado");
      await user.click(
        screen.getByRole("button", { name: /actualizar stock/i })
      );

      await waitFor(() => {
        expect(inventoryService.updateInventoryStock).toHaveBeenCalled();
      });

      expect(
        vi.mocked(inventoryService.updateInventoryStock).mock.calls[0][0]
      ).toEqual({
        productVariantId: "v1",
        quantity: 3,
        isEntry: false,
        reason: "Producto dañado",
      });
    });

    it("shows the backend error message when the update fails", async () => {
      vi.mocked(productService.getProductById).mockResolvedValue(
        variantProduct()
      );
      vi.mocked(inventoryService.updateInventoryStock).mockRejectedValue({
        response: { data: { detail: "Stock insuficiente." } },
      });

      const user = userEvent.setup();

      renderPage();
      await screen.findByText("SKU-VAR-1");

      await user.type(screen.getByLabelText(/^cantidad$/i), "-3");
      await user.click(
        screen.getByRole("button", { name: /actualizar stock/i })
      );

      expect(
        await screen.findByText("Stock insuficiente.")
      ).toBeInTheDocument();
    });
  });
});
