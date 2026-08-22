import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

import CreateReviewForm from "./CreateReviewForm";
import * as reviewService from "../../services/reviewService";

vi.mock("../../services/reviewService");

function renderForm() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <CreateReviewForm productId="product-1" />
    </QueryClientProvider>
  );
}

describe("CreateReviewForm", () => {
  beforeEach(() => {
    vi.mocked(reviewService.createReview).mockReset();
  });

  it("does not submit when the comment is empty", () => {
    renderForm();

    fireEvent.submit(document.querySelector("form")!);

    expect(reviewService.createReview).not.toHaveBeenCalled();
  });

  it("keeps the submit button disabled until a comment is entered", async () => {
    const user = userEvent.setup();

    renderForm();

    const submitButton = screen.getByRole("button", {
      name: /publicar reseña/i,
    });
    expect(submitButton).toBeDisabled();

    await user.type(
      screen.getByPlaceholderText(/cuéntanos qué te pareció/i),
      "Excelente producto"
    );

    expect(submitButton).toBeEnabled();
  });

  it("submits the rating and comment, then clears the form", async () => {
    vi.mocked(reviewService.createReview).mockResolvedValue({});

    const user = userEvent.setup();

    renderForm();

    await user.type(
      screen.getByPlaceholderText(/cuéntanos qué te pareció/i),
      "Excelente producto"
    );
    await user.click(
      screen.getByRole("button", { name: /publicar reseña/i })
    );

    await waitFor(() => {
      expect(reviewService.createReview).toHaveBeenCalledWith({
        review: {
          productId: "product-1",
          rating: 5,
          comment: "Excelente producto",
        },
      });
    });

    expect(
      await screen.findByText("Tu reseña fue publicada correctamente.")
    ).toBeInTheDocument();
    expect(
      screen.getByPlaceholderText(/cuéntanos qué te pareció/i)
    ).toHaveValue("");
  });

  it("shows an error message when publishing fails", async () => {
    vi.mocked(reviewService.createReview).mockRejectedValue(
      new Error("not verified purchase")
    );

    const user = userEvent.setup();

    renderForm();

    await user.type(
      screen.getByPlaceholderText(/cuéntanos qué te pareció/i),
      "Excelente producto"
    );
    await user.click(
      screen.getByRole("button", { name: /publicar reseña/i })
    );

    expect(
      await screen.findByText(
        "No fue posible publicar la reseña. Verifica que hayas comprado y recibido este producto."
      )
    ).toBeInTheDocument();
  });
});
