import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import Loyalty from "./Loyalty";
import * as loyaltyService from "../../services/loyaltyService";

vi.mock("../../services/loyaltyService");

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <Loyalty />
    </QueryClientProvider>
  );
}

describe("Loyalty", () => {
  beforeEach(() => {
    vi.mocked(loyaltyService.getMyLoyalty).mockReset();
  });

  it("shows an error message when the account fails to load", async () => {
    vi.mocked(loyaltyService.getMyLoyalty).mockRejectedValue(
      new Error("network error")
    );

    renderPage();

    expect(
      await screen.findByText("No fue posible cargar tu programa de puntos.")
    ).toBeInTheDocument();
  });

  it("computes progress toward Silver for a Bronze member", async () => {
    vi.mocked(loyaltyService.getMyLoyalty).mockResolvedValue({
      points: 300,
      level: "Bronze",
    });

    renderPage();

    expect(await screen.findByText("300")).toBeInTheDocument();
    // "Silver" appears both as the progress bar's next-level label and as
    // a benefit card title further down, so there are two matches.
    expect(screen.getAllByText("Silver")).toHaveLength(2);
    expect(
      screen.getByText((_, node) => node?.textContent === "Te faltan 200 puntos para alcanzar el siguiente nivel.")
    ).toBeInTheDocument();
    expect(screen.getByRole("progressbar")).toHaveAttribute(
      "aria-valuenow",
      "60"
    );
  });

  it("computes progress toward Gold for a Silver member", async () => {
    vi.mocked(loyaltyService.getMyLoyalty).mockResolvedValue({
      points: 1000,
      level: "Silver",
    });

    renderPage();

    await screen.findByText("1.000");

    expect(screen.getByRole("progressbar")).toHaveAttribute(
      "aria-valuenow",
      "50"
    );
    expect(
      screen.getByText((_, node) => node?.textContent === "Te faltan 500 puntos para alcanzar el siguiente nivel.")
    ).toBeInTheDocument();
  });

  it("shows no progress bar for a Gold member since it's the top level", async () => {
    vi.mocked(loyaltyService.getMyLoyalty).mockResolvedValue({
      points: 2000,
      level: "Gold",
    });

    renderPage();

    await screen.findByText("2.000");

    expect(screen.queryByRole("progressbar")).not.toBeInTheDocument();
    expect(
      screen.queryByText(/puntos para alcanzar el siguiente nivel/)
    ).not.toBeInTheDocument();
  });
});
