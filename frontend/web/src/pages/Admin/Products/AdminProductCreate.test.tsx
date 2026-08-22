import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";

import AdminProductCreate from "./AdminProductCreate";
import * as productService from "../../../services/productService";
import * as catalogService from "../../../services/catalogService";

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

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <AdminProductCreate />
      </MemoryRouter>
    </QueryClientProvider>
  );
}

async function fillValidForm(user: ReturnType<typeof userEvent.setup>) {
  await user.type(
    await screen.findByLabelText(/^nombre/i),
    "Labial Mate Rojo"
  );

  // Marca/Categoría are a raw FormControl+InputLabel+Select without a
  // linked id, so their label isn't programmatically associated to the
  // combobox — select them by DOM order instead (Marca, then Categoría).
  await user.click(screen.getAllByRole("combobox")[0]);
  await user.click(await screen.findByRole("option", { name: "HALDY" }));

  await user.click(screen.getAllByRole("combobox")[1]);
  await user.click(await screen.findByRole("option", { name: "Labios" }));

  await user.type(screen.getByLabelText(/^precio/i), "45000");
  await user.type(screen.getByLabelText(/stock inicial/i), "10");
  await user.type(screen.getByLabelText(/^color/i), "Rojo");
  await user.type(screen.getByLabelText(/^talla/i), "Única");
}

describe("AdminProductCreate", () => {
  beforeEach(() => {
    navigateMock.mockReset();
    vi.mocked(catalogService.getBrands).mockReset();
    vi.mocked(catalogService.getCategories).mockReset();
    vi.mocked(productService.createProduct).mockReset();

    vi.mocked(catalogService.getBrands).mockResolvedValue([
      { id: "brand-1", name: "HALDY" },
    ]);
    vi.mocked(catalogService.getCategories).mockResolvedValue([
      { id: "cat-1", name: "Labios" },
    ]);
  });

  it("shows an error message when brands/categories fail to load", async () => {
    vi.mocked(catalogService.getBrands).mockRejectedValue(
      new Error("network error")
    );

    renderPage();

    expect(
      await screen.findByText("No fue posible cargar las marcas y categorías.")
    ).toBeInTheDocument();
  });

  it("requires the product name", async () => {
    renderPage();
    await screen.findByLabelText(/^nombre/i);

    // The name/brand/color fields carry the HTML `required` attribute, so a
    // real click on the submit button gets blocked by the browser's own
    // constraint validation before it ever reaches our JS validation. Firing
    // the submit event directly bypasses that and exercises our own checks.
    fireEvent.submit(document.querySelector("form")!);

    expect(
      screen.getByText("El nombre del producto es obligatorio.")
    ).toBeInTheDocument();
    expect(productService.createProduct).not.toHaveBeenCalled();
  });

  it("requires selecting a brand", async () => {
    const user = userEvent.setup();

    renderPage();
    await user.type(
      await screen.findByLabelText(/^nombre/i),
      "Labial Mate Rojo"
    );

    fireEvent.submit(document.querySelector("form")!);

    expect(screen.getByText("Selecciona una marca.")).toBeInTheDocument();
  });

  it("requires a positive price", async () => {
    const user = userEvent.setup();

    renderPage();
    await fillValidForm(user);

    const priceField = screen.getByLabelText(/^precio/i);
    await user.clear(priceField);
    await user.type(priceField, "0");

    fireEvent.submit(document.querySelector("form")!);

    expect(
      screen.getByText("El precio debe ser mayor que cero.")
    ).toBeInTheDocument();
    expect(productService.createProduct).not.toHaveBeenCalled();
  });

  it("requires an integer, non-negative stock", async () => {
    const user = userEvent.setup();

    renderPage();
    await fillValidForm(user);

    const stockField = screen.getByLabelText(/stock inicial/i);
    await user.clear(stockField);
    await user.type(stockField, "-5");

    fireEvent.submit(document.querySelector("form")!);

    expect(
      screen.getByText(
        "El stock debe ser un número entero mayor o igual a cero."
      )
    ).toBeInTheDocument();
  });

  it("requires the variant's color and size", async () => {
    const user = userEvent.setup();

    renderPage();
    await fillValidForm(user);

    await user.clear(screen.getByLabelText(/^color/i));

    fireEvent.submit(document.querySelector("form")!);

    expect(
      screen.getByText("El color de la variante es obligatorio.")
    ).toBeInTheDocument();
  });

  it("lets the user add and remove extra image fields", async () => {
    const user = userEvent.setup();

    renderPage();
    await screen.findByLabelText(/^nombre/i);

    expect(screen.getByLabelText(/url de imagen 1/i)).toBeInTheDocument();
    expect(screen.queryByLabelText(/url de imagen 2/i)).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: /agregar imagen/i }));

    expect(screen.getByLabelText(/url de imagen 2/i)).toBeInTheDocument();

    await user.click(
      screen.getAllByRole("button", { name: /^eliminar$/i })[0]
    );

    expect(screen.queryByLabelText(/url de imagen 2/i)).not.toBeInTheDocument();
  });

  it("submits the product with the initial variant and filters blank image URLs", async () => {
    vi.mocked(productService.createProduct).mockResolvedValue("new-product-id");

    const user = userEvent.setup();

    renderPage();
    await fillValidForm(user);

    await user.type(
      screen.getByLabelText(/url de imagen 1/i),
      "https://example.com/img.jpg"
    );

    await user.click(screen.getByRole("button", { name: /crear producto/i }));

    await waitFor(() => {
      expect(productService.createProduct).toHaveBeenCalled();
    });

    // React Query v5 passes a second (mutation context) argument to a
    // mutationFn that isn't wrapped in an arrow function.
    expect(
      vi.mocked(productService.createProduct).mock.calls[0][0]
    ).toEqual({
      name: "Labial Mate Rojo",
      description: "",
      brandId: "brand-1",
      categoryId: "cat-1",
      isFeatured: false,
      variants: [
        {
          price: 45000,
          stock: 10,
          color: "Rojo",
          size: "Única",
        },
      ],
      images: ["https://example.com/img.jpg"],
    });

    expect(navigateMock).toHaveBeenCalledWith(
      "/admin/productos/new-product-id"
    );
  });

  it("shows the backend error message when creation fails", async () => {
    vi.mocked(productService.createProduct).mockRejectedValue({
      response: { data: { detail: "Ya existe un producto con ese SKU." } },
    });

    const user = userEvent.setup();

    renderPage();
    await fillValidForm(user);

    await user.click(screen.getByRole("button", { name: /crear producto/i }));

    expect(
      await screen.findByText("Ya existe un producto con ese SKU.")
    ).toBeInTheDocument();
  });
});
