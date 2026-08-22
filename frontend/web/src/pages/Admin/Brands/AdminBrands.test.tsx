import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import AdminBrands from "./AdminBrands";
import * as catalogService from "../../../services/catalogService";
import type { BrandDetail } from "../../../services/catalogService";

vi.mock("../../../services/catalogService");

function makeBrand(overrides: Partial<BrandDetail>): BrandDetail {
  return {
    id: "b1",
    name: "Brand 1",
    description: "",
    logoUrl: "",
    isActive: true,
    isDeleted: false,
    deletedAt: null,
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
        <AdminBrands />
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe("AdminBrands", () => {
  beforeEach(() => {
    vi.mocked(catalogService.getBrandDetails).mockReset();
    vi.mocked(catalogService.deleteBrand).mockReset();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("shows an error message when brands fail to load", async () => {
    vi.mocked(catalogService.getBrandDetails).mockRejectedValue(
      new Error("network error")
    );

    renderPage();

    expect(
      await screen.findByText("No fue posible cargar las marcas.")
    ).toBeInTheDocument();
  });

  it("shows the empty state when there are no brands", async () => {
    vi.mocked(catalogService.getBrandDetails).mockResolvedValue([]);

    renderPage();

    expect(
      await screen.findByText("Todavía no hay marcas.")
    ).toBeInTheDocument();
  });

  it("does not delete when the confirmation dialog is dismissed", async () => {
    vi.mocked(catalogService.getBrandDetails).mockResolvedValue([
      makeBrand({ id: "b1", name: "Labiales HALDY" }),
    ]);
    vi.spyOn(window, "confirm").mockReturnValue(false);

    const user = userEvent.setup();

    renderPage();

    await screen.findByText("Labiales HALDY");
    await user.click(screen.getByRole("button", { name: /eliminar/i }));

    expect(window.confirm).toHaveBeenCalledWith(
      '¿Eliminar la marca "Labiales HALDY"?'
    );
    expect(catalogService.deleteBrand).not.toHaveBeenCalled();
  });

  it("deletes the brand and shows a success message when confirmed", async () => {
    vi.mocked(catalogService.getBrandDetails).mockResolvedValue([
      makeBrand({ id: "b1", name: "Labiales HALDY" }),
    ]);
    vi.mocked(catalogService.deleteBrand).mockResolvedValue(undefined);
    vi.spyOn(window, "confirm").mockReturnValue(true);

    const user = userEvent.setup();

    renderPage();

    await screen.findByText("Labiales HALDY");
    await user.click(screen.getByRole("button", { name: /eliminar/i }));

    await waitFor(() => {
      expect(catalogService.deleteBrand).toHaveBeenCalledWith("b1");
    });

    expect(await screen.findByText("Marca eliminada.")).toBeInTheDocument();
  });

  it("shows the backend error message when deletion fails", async () => {
    vi.mocked(catalogService.getBrandDetails).mockResolvedValue([
      makeBrand({ id: "b1", name: "Labiales HALDY" }),
    ]);
    vi.mocked(catalogService.deleteBrand).mockRejectedValue({
      response: { data: { detail: "La marca tiene productos asociados." } },
    });
    vi.spyOn(window, "confirm").mockReturnValue(true);

    const user = userEvent.setup();

    renderPage();

    await screen.findByText("Labiales HALDY");
    await user.click(screen.getByRole("button", { name: /eliminar/i }));

    expect(
      await screen.findByText("La marca tiene productos asociados.")
    ).toBeInTheDocument();
  });
});
