import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";

import AdminCategoryForm from "./AdminCategoryForm";
import * as catalogService from "../../../services/catalogService";
import type { CategoryDetail } from "../../../services/catalogService";

const navigateMock = vi.fn();

vi.mock("react-router-dom", async (importOriginal) => {
  const actual = await importOriginal<typeof import("react-router-dom")>();

  return {
    ...actual,
    useNavigate: () => navigateMock,
  };
});

vi.mock("../../../services/catalogService");

function makeCategory(overrides: Partial<CategoryDetail>): CategoryDetail {
  return {
    id: "c1",
    name: "Rostro",
    slug: "rostro",
    description: "",
    imageUrl: "",
    parentCategoryId: null,
    ...overrides,
  };
}

function renderCreate() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={["/admin/categorias/nueva"]}>
        <Routes>
          <Route
            path="/admin/categorias/nueva"
            element={<AdminCategoryForm />}
          />
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
      <MemoryRouter initialEntries={[`/admin/categorias/${id}/editar`]}>
        <Routes>
          <Route
            path="/admin/categorias/:id/editar"
            element={<AdminCategoryForm />}
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe("AdminCategoryForm", () => {
  beforeEach(() => {
    navigateMock.mockReset();
    vi.mocked(catalogService.getCategoryDetails).mockReset();
    vi.mocked(catalogService.getCategoryById).mockReset();
    vi.mocked(catalogService.createCategory).mockReset();
    vi.mocked(catalogService.updateCategory).mockReset();
    vi.mocked(catalogService.getCategoryDetails).mockResolvedValue([]);
  });

  it("requires both name and slug before submitting", () => {
    renderCreate();

    fireEvent.submit(document.querySelector("form")!);

    expect(
      screen.getByText("Nombre y slug son obligatorios.")
    ).toBeInTheDocument();
    expect(catalogService.createCategory).not.toHaveBeenCalled();
  });

  it("creates a category and redirects to the list", async () => {
    vi.mocked(catalogService.createCategory).mockResolvedValue("new-id");

    const user = userEvent.setup();

    renderCreate();

    await user.type(screen.getByLabelText(/^nombre$/i), "Labios");
    await user.type(screen.getByLabelText(/^slug$/i), "labios");
    await user.click(screen.getByRole("button", { name: /^guardar$/i }));

    await waitFor(() => {
      expect(catalogService.createCategory).toHaveBeenCalledWith({
        name: "Labios",
        slug: "labios",
        description: "",
        imageUrl: "",
        parentCategoryId: null,
      });
    });

    expect(navigateMock).toHaveBeenCalledWith("/admin/categorias");
  });

  it("excludes the category being edited from its own parent options", async () => {
    vi.mocked(catalogService.getCategoryDetails).mockResolvedValue([
      makeCategory({ id: "c1", name: "Rostro" }),
      makeCategory({ id: "c2", name: "Labios" }),
    ]);
    vi.mocked(catalogService.getCategoryById).mockResolvedValue(
      makeCategory({ id: "c1", name: "Rostro" })
    );

    const user = userEvent.setup();

    renderEdit("c1");

    await screen.findByDisplayValue("Rostro");

    await user.click(screen.getByLabelText(/categoría padre/i));

    expect(screen.getByRole("option", { name: "Labios" })).toBeInTheDocument();
    expect(
      screen.queryByRole("option", { name: "Rostro" })
    ).not.toBeInTheDocument();
  });

  it("updates an existing category with the edited fields", async () => {
    vi.mocked(catalogService.getCategoryById).mockResolvedValue(
      makeCategory({ id: "c1", name: "Rostro", slug: "rostro" })
    );
    vi.mocked(catalogService.updateCategory).mockResolvedValue(undefined);

    const user = userEvent.setup();

    renderEdit("c1");

    await screen.findByDisplayValue("Rostro");

    const nameField = screen.getByLabelText(/^nombre$/i);
    await user.clear(nameField);
    await user.type(nameField, "Cuidado facial");

    await user.click(screen.getByRole("button", { name: /^guardar$/i }));

    await waitFor(() => {
      expect(catalogService.updateCategory).toHaveBeenCalledWith("c1", {
        name: "Cuidado facial",
        slug: "rostro",
        description: "",
        imageUrl: "",
        parentCategoryId: null,
      });
    });

    expect(navigateMock).toHaveBeenCalledWith("/admin/categorias");
  });

  it("shows the backend error message when saving fails", async () => {
    vi.mocked(catalogService.createCategory).mockRejectedValue({
      response: { data: { detail: "Ya existe una categoría con ese slug." } },
    });

    const user = userEvent.setup();

    renderCreate();

    await user.type(screen.getByLabelText(/^nombre$/i), "Labios");
    await user.type(screen.getByLabelText(/^slug$/i), "labios");
    await user.click(screen.getByRole("button", { name: /^guardar$/i }));

    expect(
      await screen.findByText("Ya existe una categoría con ese slug.")
    ).toBeInTheDocument();
  });
});
