import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";

import AdminProductEdit from "./AdminProductEdit";
import * as productService from "../../../services/productService";
import * as catalogService from "../../../services/catalogService";
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
vi.mock("../../../services/catalogService");

function makeProduct(overrides: Partial<Product>): Product {
  return {
    id: "p1",
    sku: "SKU-1",
    barcode: "",
    name: "Labial Mate Rojo",
    slug: "labial-mate-rojo",
    description: "Un labial mate de larga duración.",
    price: 45000,
    brandId: "brand-1",
    categoryId: "cat-1",
    isFeatured: false,
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
      <MemoryRouter initialEntries={["/admin/productos/p1/editar"]}>
        <Routes>
          <Route
            path="/admin/productos/:id/editar"
            element={<AdminProductEdit />}
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe("AdminProductEdit", () => {
  beforeEach(() => {
    navigateMock.mockReset();
    vi.mocked(productService.getProductById).mockReset();
    vi.mocked(productService.updateProduct).mockReset();
    vi.mocked(catalogService.getBrands).mockReset();
    vi.mocked(catalogService.getCategories).mockReset();

    vi.mocked(catalogService.getBrands).mockResolvedValue([
      { id: "brand-1", name: "HALDY" },
      { id: "brand-2", name: "Otra Marca" },
    ]);
    vi.mocked(catalogService.getCategories).mockResolvedValue([
      { id: "cat-1", name: "Labios" },
      { id: "cat-2", name: "Rostro" },
    ]);
  });

  it("shows an error message when the product fails to load", async () => {
    vi.mocked(productService.getProductById).mockRejectedValue(
      new Error("not found")
    );

    renderPage();

    expect(
      await screen.findByText("No fue posible cargar el producto.")
    ).toBeInTheDocument();
  });

  it("loads the existing product's name and description into the form", async () => {
    vi.mocked(productService.getProductById).mockResolvedValue(
      makeProduct({})
    );

    renderPage();

    expect(
      await screen.findByDisplayValue("Labial Mate Rojo")
    ).toBeInTheDocument();
    expect(
      screen.getByDisplayValue("Un labial mate de larga duración.")
    ).toBeInTheDocument();
  });

  it("requires the product name", async () => {
    vi.mocked(productService.getProductById).mockResolvedValue(
      makeProduct({})
    );

    const user = userEvent.setup();

    renderPage();
    await screen.findByDisplayValue("Labial Mate Rojo");

    const nameField = screen.getByLabelText(/^nombre$/i);
    await user.clear(nameField);

    await user.click(screen.getByRole("button", { name: /guardar cambios/i }));

    expect(
      screen.getByText("El nombre del producto es obligatorio.")
    ).toBeInTheDocument();
    expect(productService.updateProduct).not.toHaveBeenCalled();
  });

  it("updates the product and navigates to its detail page after showing a success message", async () => {
    vi.mocked(productService.getProductById).mockResolvedValue(
      makeProduct({})
    );
    vi.mocked(productService.updateProduct).mockResolvedValue(undefined);

    const user = userEvent.setup();

    renderPage();
    await screen.findByDisplayValue("Labial Mate Rojo");

    const nameField = screen.getByLabelText(/^nombre$/i);
    await user.clear(nameField);
    await user.type(nameField, "Labial Mate Rojo Intenso");

    await user.click(screen.getByRole("button", { name: /guardar cambios/i }));

    await waitFor(() => {
      expect(productService.updateProduct).toHaveBeenCalledWith("p1", {
        name: "Labial Mate Rojo Intenso",
        description: "Un labial mate de larga duración.",
        brandId: "brand-1",
        categoryId: "cat-1",
        isFeatured: false,
      });
    });

    expect(
      await screen.findByText("Producto actualizado correctamente.")
    ).toBeInTheDocument();

    await waitFor(
      () => {
        expect(navigateMock).toHaveBeenCalledWith("/admin/productos/p1");
      },
      { timeout: 2000 }
    );
  });

  it("shows the backend error message when saving fails", async () => {
    vi.mocked(productService.getProductById).mockResolvedValue(
      makeProduct({})
    );
    vi.mocked(productService.updateProduct).mockRejectedValue({
      response: { data: { detail: "No se pudo actualizar: SKU duplicado." } },
    });

    const user = userEvent.setup();

    renderPage();
    await screen.findByDisplayValue("Labial Mate Rojo");

    await user.click(screen.getByRole("button", { name: /guardar cambios/i }));

    expect(
      await screen.findByText("No se pudo actualizar: SKU duplicado.")
    ).toBeInTheDocument();
  });
});
