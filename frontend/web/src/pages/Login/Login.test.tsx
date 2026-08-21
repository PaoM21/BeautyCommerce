import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";

import Login from "./Login";
import { useAuthStore } from "../../store/authStore";
import * as authService from "../../services/authService";

const navigateMock = vi.fn();

vi.mock("react-router-dom", async (importOriginal) => {
  const actual = await importOriginal<typeof import("react-router-dom")>();

  return {
    ...actual,
    useNavigate: () => navigateMock,
  };
});

vi.mock("../../services/authService");

describe("Login", () => {
  beforeEach(() => {
    navigateMock.mockReset();
    vi.mocked(authService.login).mockReset();
    localStorage.clear();
    useAuthStore.setState({ token: null, user: null });
  });

  it("shows a validation error and does not call the API when fields are empty", async () => {
    const user = userEvent.setup();

    render(
      <MemoryRouter>
        <Login />
      </MemoryRouter>
    );

    await user.click(
      screen.getByRole("button", { name: /iniciar sesión/i })
    );

    expect(
      screen.getByText("Ingresa tu correo y contraseña.")
    ).toBeInTheDocument();
    expect(authService.login).not.toHaveBeenCalled();
  });

  it("logs in and redirects home on success", async () => {
    vi.mocked(authService.login).mockResolvedValue({
      token: "token-123",
      user: { id: "1", email: "cliente@test.com" },
    });

    const user = userEvent.setup();

    render(
      <MemoryRouter>
        <Login />
      </MemoryRouter>
    );

    await user.type(
      screen.getByLabelText(/correo electrónico/i),
      "cliente@test.com"
    );
    await user.type(screen.getByLabelText(/contraseña/i), "Password123!");
    await user.click(
      screen.getByRole("button", { name: /iniciar sesión/i })
    );

    await waitFor(() => {
      expect(navigateMock).toHaveBeenCalledWith("/");
    });

    expect(useAuthStore.getState().token).toBe("token-123");
  });

  it("shows a generic error when the credentials are rejected", async () => {
    vi.mocked(authService.login).mockRejectedValue(
      new Error("Unauthorized")
    );

    const user = userEvent.setup();

    render(
      <MemoryRouter>
        <Login />
      </MemoryRouter>
    );

    await user.type(
      screen.getByLabelText(/correo electrónico/i),
      "cliente@test.com"
    );
    await user.type(screen.getByLabelText(/contraseña/i), "wrong-password");
    await user.click(
      screen.getByRole("button", { name: /iniciar sesión/i })
    );

    expect(
      await screen.findByText("Correo o contraseña incorrectos.")
    ).toBeInTheDocument();
    expect(navigateMock).not.toHaveBeenCalled();
  });
});
