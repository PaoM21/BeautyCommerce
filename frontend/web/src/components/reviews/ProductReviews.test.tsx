import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import ProductReviews from "./ProductReviews";
import * as reviewService from "../../services/reviewService";
import type { Review } from "../../types/review";

vi.mock("../../services/reviewService");

function renderComponent() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <ProductReviews productId="product-1" />
    </QueryClientProvider>
  );
}

function makeReview(overrides: Partial<Review>): Review {
  return {
    id: "r1",
    userName: "Ana",
    rating: 5,
    comment: "Me encantó",
    createdAt: "2026-01-01T00:00:00Z",
    ...overrides,
  };
}

describe("ProductReviews", () => {
  beforeEach(() => {
    vi.mocked(reviewService.getProductReviews).mockReset();
  });

  it("shows the empty state when there are no reviews", async () => {
    vi.mocked(reviewService.getProductReviews).mockResolvedValue([]);

    renderComponent();

    expect(
      await screen.findByText("Este producto todavía no tiene reseñas.")
    ).toBeInTheDocument();
    expect(screen.getByText("0 reseñas")).toBeInTheDocument();
  });

  it("uses the singular form for exactly one review", async () => {
    vi.mocked(reviewService.getProductReviews).mockResolvedValue([
      makeReview({ id: "r1" }),
    ]);

    renderComponent();

    expect(await screen.findByText("1 reseña")).toBeInTheDocument();
  });

  it("renders each review and computes the average rating", async () => {
    vi.mocked(reviewService.getProductReviews).mockResolvedValue([
      makeReview({ id: "r1", userName: "Ana", rating: 5 }),
      makeReview({ id: "r2", userName: "Beatriz", rating: 3 }),
    ]);

    renderComponent();

    expect(await screen.findByText("Ana")).toBeInTheDocument();
    expect(screen.getByText("Beatriz")).toBeInTheDocument();
    expect(screen.getByText("2 reseñas")).toBeInTheDocument();
    expect(screen.getByText("4.0 (2)")).toBeInTheDocument();
  });
});
