import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import AdminProducts from "./AdminProducts";
import * as productService from "../../../services/productService";
import * as mediaService from "../../../services/mediaService";
import type {
  AdminProductListItem,
  AdminProductsResult,
} from "../../../services/productService";

vi.mock("../../../services/productService");
vi.mock("../../../services/mediaService");

function makeResult(
  overrides: Partial<AdminProductsResult>
): AdminProductsResult {
  return {
    items: [],
    page: 1,
    pageSize: 12,
    totalRecords: 0,
    totalPages: 1,
    ...overrides,
  };
}

function makeProduct(
  overrides: Partial<AdminProductListItem>
): AdminProductListItem {
  return {
    id: "p1",
    name: "Labial Mate Rojo",
    brand: "HALDY",
    category: "Labios",
    price: 45000,
    stock: 10,
    image: null,
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
        <AdminProducts />
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe("AdminProducts", () => {
  beforeEach(() => {
    vi.mocked(productService.getAdminProducts).mockReset();
    vi.mocked(productService.deleteProduct).mockReset();
    vi.mocked(mediaService.syncImagesFromDrive).mockReset();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("shows an error message when products fail to load", async () => {
    vi.mocked(productService.getAdminProducts).mockRejectedValue(
      new Error("network error")
    );

    renderPage();

    expect(
      await screen.findByText("No fue posible cargar los productos.")
    ).toBeInTheDocument();
  });

  it("shows the empty state when there are no products", async () => {
    vi.mocked(productService.getAdminProducts).mockResolvedValue(
      makeResult({ items: [] })
    );

    renderPage();

    expect(
      await screen.findByText("No se encontraron productos.")
    ).toBeInTheDocument();
  });

  it("searches on Enter and resets to page 1", async () => {
    vi.mocked(productService.getAdminProducts).mockResolvedValue(
      makeResult({ items: [makeProduct({})] })
    );

    const user = userEvent.setup();

    renderPage();

    await screen.findByText("Labial Mate Rojo");

    await user.type(
      screen.getByPlaceholderText(/buscar producto/i),
      "labial{Enter}"
    );

    await waitFor(() => {
      expect(productService.getAdminProducts).toHaveBeenLastCalledWith(
        1,
        12,
        "labial"
      );
    });
  });

  it("paginates using the totalPages returned by the API", async () => {
    vi.mocked(productService.getAdminProducts).mockResolvedValue(
      makeResult({
        items: [makeProduct({})],
        page: 1,
        totalPages: 3,
      })
    );

    const user = userEvent.setup();

    renderPage();

    await screen.findByText("Labial Mate Rojo");

    expect(screen.getByText("Página 1 de 3")).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /anterior/i })
    ).toBeDisabled();

    await user.click(screen.getByRole("button", { name: /siguiente/i }));

    await waitFor(() => {
      expect(productService.getAdminProducts).toHaveBeenLastCalledWith(
        2,
        12,
        ""
      );
    });
  });

  it("does not delete when the confirmation dialog is dismissed", async () => {
    vi.mocked(productService.getAdminProducts).mockResolvedValue(
      makeResult({ items: [makeProduct({ id: "p1", name: "Labial Mate Rojo" })] })
    );
    vi.spyOn(window, "confirm").mockReturnValue(false);

    const user = userEvent.setup();

    renderPage();

    await screen.findByText("Labial Mate Rojo");
    await user.click(screen.getByRole("button", { name: /eliminar/i }));

    expect(productService.deleteProduct).not.toHaveBeenCalled();
  });

  it("shows the backend failure message when deletion is rejected by the server", async () => {
    vi.mocked(productService.getAdminProducts).mockResolvedValue(
      makeResult({ items: [makeProduct({ id: "p1", name: "Labial Mate Rojo" })] })
    );
    vi.mocked(productService.deleteProduct).mockResolvedValue({
      success: false,
      message: "El producto tiene pedidos asociados.",
    });
    vi.spyOn(window, "confirm").mockReturnValue(true);

    const user = userEvent.setup();

    renderPage();

    await screen.findByText("Labial Mate Rojo");
    await user.click(screen.getByRole("button", { name: /eliminar/i }));

    expect(
      await screen.findByText("El producto tiene pedidos asociados.")
    ).toBeInTheDocument();
  });

  it("syncs images from Drive and shows the result summary", async () => {
    vi.mocked(productService.getAdminProducts).mockResolvedValue(
      makeResult({ items: [] })
    );
    vi.mocked(mediaService.syncImagesFromDrive).mockResolvedValue({
      updatedProducts: [
        {
          productId: "p1",
          productName: "Labial Mate Rojo",
          sourceFile: "SKU-1.jpg",
          imageUrl: "https://drive/img.jpg",
          isPrimary: true,
        },
      ],
      unmatchedFiles: ["SKU-999.jpg"],
      errors: [],
      filesProcessed: 2,
    });

    const user = userEvent.setup();

    renderPage();

    await screen.findByText("No se encontraron productos.");

    await user.click(
      screen.getByRole("button", { name: /sincronizar imágenes desde drive/i })
    );

    expect(
      await screen.findByText(
        "1 imagen(es) sincronizada(s) de 2 archivo(s) procesado(s)."
      )
    ).toBeInTheDocument();
    expect(screen.getByText(/SKU-999\.jpg/)).toBeInTheDocument();
  });
});
