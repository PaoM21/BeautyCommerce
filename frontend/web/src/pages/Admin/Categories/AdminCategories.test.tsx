import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import AdminCategories from "./AdminCategories";
import * as catalogService from "../../../services/catalogService";
import type { CategoryDetail } from "../../../services/catalogService";

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

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <AdminCategories />
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe("AdminCategories", () => {
  beforeEach(() => {
    vi.mocked(catalogService.getCategoryDetails).mockReset();
    vi.mocked(catalogService.deleteCategory).mockReset();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("shows an error message when categories fail to load", async () => {
    vi.mocked(catalogService.getCategoryDetails).mockRejectedValue(
      new Error("network error")
    );

    renderPage();

    expect(
      await screen.findByText("No fue posible cargar las categorías.")
    ).toBeInTheDocument();
  });

  it("shows the empty state when there are no categories", async () => {
    vi.mocked(catalogService.getCategoryDetails).mockResolvedValue([]);

    renderPage();

    expect(
      await screen.findByText("Todavía no hay categorías.")
    ).toBeInTheDocument();
  });

  it("renders each category with its slug", async () => {
    vi.mocked(catalogService.getCategoryDetails).mockResolvedValue([
      makeCategory({ name: "Rostro", slug: "rostro" }),
    ]);

    renderPage();

    expect(await screen.findByText("Rostro")).toBeInTheDocument();
    expect(screen.getByText("/rostro")).toBeInTheDocument();
  });

  it("does not delete when the confirmation dialog is dismissed", async () => {
    vi.mocked(catalogService.getCategoryDetails).mockResolvedValue([
      makeCategory({ id: "c1", name: "Rostro" }),
    ]);
    vi.spyOn(window, "confirm").mockReturnValue(false);

    const user = userEvent.setup();

    renderPage();

    await screen.findByText("Rostro");
    await user.click(screen.getByRole("button", { name: /eliminar/i }));

    expect(catalogService.deleteCategory).not.toHaveBeenCalled();
  });

  it("deletes the category and shows a success message when confirmed", async () => {
    vi.mocked(catalogService.getCategoryDetails).mockResolvedValue([
      makeCategory({ id: "c1", name: "Rostro" }),
    ]);
    vi.mocked(catalogService.deleteCategory).mockResolvedValue(undefined);
    vi.spyOn(window, "confirm").mockReturnValue(true);

    const user = userEvent.setup();

    renderPage();

    await screen.findByText("Rostro");
    await user.click(screen.getByRole("button", { name: /eliminar/i }));

    await waitFor(() => {
      expect(catalogService.deleteCategory).toHaveBeenCalledWith("c1");
    });

    expect(
      await screen.findByText("Categoría eliminada.")
    ).toBeInTheDocument();
  });
});
