import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";

import AdminBrandForm from "./AdminBrandForm";
import * as catalogService from "../../../services/catalogService";
import type { BrandDetail } from "../../../services/catalogService";

const navigateMock = vi.fn();

vi.mock("react-router-dom", async (importOriginal) => {
  const actual = await importOriginal<typeof import("react-router-dom")>();

  return {
    ...actual,
    useNavigate: () => navigateMock,
  };
});

vi.mock("../../../services/catalogService");

function makeBrand(overrides: Partial<BrandDetail>): BrandDetail {
  return {
    id: "b1",
    name: "Brand 1",
    description: "Desc",
    logoUrl: "https://example.com/logo.png",
    isActive: true,
    isDeleted: false,
    deletedAt: null,
    ...overrides,
  };
}

function renderCreate() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={["/admin/marcas/nueva"]}>
        <Routes>
          <Route path="/admin/marcas/nueva" element={<AdminBrandForm />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

function renderEdit(id: string) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/admin/marcas/${id}/editar`]}>
        <Routes>
          <Route
            path="/admin/marcas/:id/editar"
            element={<AdminBrandForm />}
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe("AdminBrandForm", () => {
  beforeEach(() => {
    navigateMock.mockReset();
    vi.mocked(catalogService.getBrandById).mockReset();
    vi.mocked(catalogService.createBrand).mockReset();
    vi.mocked(catalogService.updateBrand).mockReset();
  });

  it("requires a name before submitting a new brand", () => {
    renderCreate();

    fireEvent.submit(document.querySelector("form")!);

    expect(
      screen.getByText("El nombre es obligatorio.")
    ).toBeInTheDocument();
    expect(catalogService.createBrand).not.toHaveBeenCalled();
  });

  it("creates a brand and redirects to the list", async () => {
    vi.mocked(catalogService.createBrand).mockResolvedValue("new-id");

    const user = userEvent.setup();

    renderCreate();

    await user.type(screen.getByLabelText(/^nombre$/i), "Nueva Marca");
    await user.click(screen.getByRole("button", { name: /^guardar$/i }));

    await waitFor(() => {
      expect(catalogService.createBrand).toHaveBeenCalledWith({
        name: "Nueva Marca",
        description: "",
        logoUrl: "",
      });
    });

    expect(navigateMock).toHaveBeenCalledWith("/admin/marcas");
  });

  it("shows the backend error message when saving fails", async () => {
    vi.mocked(catalogService.createBrand).mockRejectedValue({
      response: { data: { detail: "Ya existe una marca con ese nombre." } },
    });

    const user = userEvent.setup();

    renderCreate();

    await user.type(screen.getByLabelText(/^nombre$/i), "Nueva Marca");
    await user.click(screen.getByRole("button", { name: /^guardar$/i }));

    expect(
      await screen.findByText("Ya existe una marca con ese nombre.")
    ).toBeInTheDocument();
  });

  it("loads the existing brand into the form in edit mode", async () => {
    vi.mocked(catalogService.getBrandById).mockResolvedValue(
      makeBrand({ id: "b1", name: "Labiales HALDY", description: "Línea de labiales" })
    );

    renderEdit("b1");

    expect(await screen.findByDisplayValue("Labiales HALDY")).toBeInTheDocument();
    expect(screen.getByDisplayValue("Línea de labiales")).toBeInTheDocument();
    expect(screen.getByText("Editar marca")).toBeInTheDocument();
  });

  it("updates the brand merging the current record with the edited fields", async () => {
    const existing = makeBrand({
      id: "b1",
      name: "Labiales HALDY",
      description: "Línea de labiales",
    });
    vi.mocked(catalogService.getBrandById).mockResolvedValue(existing);
    vi.mocked(catalogService.updateBrand).mockResolvedValue(undefined);

    const user = userEvent.setup();

    renderEdit("b1");

    await screen.findByDisplayValue("Labiales HALDY");

    const nameField = screen.getByLabelText(/^nombre$/i);
    await user.clear(nameField);
    await user.type(nameField, "Labiales HALDY Pro");

    await user.click(screen.getByRole("button", { name: /^guardar$/i }));

    await waitFor(() => {
      expect(catalogService.updateBrand).toHaveBeenCalledWith(
        "b1",
        existing,
        {
          name: "Labiales HALDY Pro",
          description: "Línea de labiales",
          logoUrl: "https://example.com/logo.png",
        }
      );
    });

    expect(navigateMock).toHaveBeenCalledWith("/admin/marcas");
  });
});
