import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";

import ForgotPassword from "./ForgotPassword";
import * as authService from "../../services/authService";

vi.mock("../../services/authService");

describe("ForgotPassword", () => {
  beforeEach(() => {
    vi.mocked(authService.forgotPassword).mockReset();
  });

  it("shows a validation error and does not call the API when the email is empty", async () => {
    const user = userEvent.setup();

    render(
      <MemoryRouter>
        <ForgotPassword />
      </MemoryRouter>
    );

    await user.click(screen.getByRole("button", { name: /enviar enlace/i }));

    expect(
      screen.getByText("Ingresa tu correo electrónico.")
    ).toBeInTheDocument();
    expect(authService.forgotPassword).not.toHaveBeenCalled();
  });

  it("shows the confirmation message after a successful submit", async () => {
    vi.mocked(authService.forgotPassword).mockResolvedValue(undefined);

    const user = userEvent.setup();

    render(
      <MemoryRouter>
        <ForgotPassword />
      </MemoryRouter>
    );

    await user.type(
      screen.getByLabelText(/correo electrónico/i),
      "cliente@test.com"
    );
    await user.click(screen.getByRole("button", { name: /enviar enlace/i }));

    expect(
      await screen.findByText(/te enviamos un enlace/i)
    ).toBeInTheDocument();
    expect(authService.forgotPassword).toHaveBeenCalledWith(
      "cliente@test.com"
    );
  });

  it("does not reveal whether the email exists when the request fails", async () => {
    vi.mocked(authService.forgotPassword).mockRejectedValue(
      new Error("network error")
    );

    const user = userEvent.setup();

    render(
      <MemoryRouter>
        <ForgotPassword />
      </MemoryRouter>
    );

    await user.type(
      screen.getByLabelText(/correo electrónico/i),
      "cliente@test.com"
    );
    await user.click(screen.getByRole("button", { name: /enviar enlace/i }));

    expect(
      await screen.findByText(
        "No fue posible procesar la solicitud. Intenta de nuevo."
      )
    ).toBeInTheDocument();
  });
});
