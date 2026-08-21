import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";

import Register from "./Register";
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

describe("Register", () => {
  beforeEach(() => {
    navigateMock.mockReset();
    vi.mocked(authService.register).mockReset();
  });

  it("shows a validation error and does not call the API when fields are empty", async () => {
    render(
      <MemoryRouter>
        <Register />
      </MemoryRouter>
    );

    fireEvent.submit(document.querySelector("form")!);

    expect(
      await screen.findByText("Completa todos los campos.")
    ).toBeInTheDocument();
    expect(authService.register).not.toHaveBeenCalled();
  });

  it("blocks submission until the terms checkbox is accepted", async () => {
    const user = userEvent.setup();

    render(
      <MemoryRouter>
        <Register />
      </MemoryRouter>
    );

    await user.type(screen.getByLabelText(/^nombre$/i), "Ana");
    await user.type(screen.getByLabelText(/^apellido$/i), "Pérez");
    await user.type(
      screen.getByLabelText(/correo electrónico/i),
      "ana@test.com"
    );
    await user.type(screen.getByLabelText(/^contraseña/i), "Password123!");

    expect(
      screen.getByRole("button", { name: /crear cuenta/i })
    ).toBeDisabled();

    fireEvent.submit(document.querySelector("form")!);

    expect(
      await screen.findByText(/debes aceptar los términos/i)
    ).toBeInTheDocument();
    expect(authService.register).not.toHaveBeenCalled();
  });

  it("registers and redirects to login on success", async () => {
    vi.mocked(authService.register).mockResolvedValue(undefined);

    const user = userEvent.setup();

    render(
      <MemoryRouter>
        <Register />
      </MemoryRouter>
    );

    await user.type(screen.getByLabelText(/^nombre$/i), "Ana");
    await user.type(screen.getByLabelText(/^apellido$/i), "Pérez");
    await user.type(
      screen.getByLabelText(/correo electrónico/i),
      "ana@test.com"
    );
    await user.type(screen.getByLabelText(/^contraseña/i), "Password123!");
    await user.click(screen.getByRole("checkbox"));
    await user.click(screen.getByRole("button", { name: /crear cuenta/i }));

    await waitFor(() => {
      expect(navigateMock).toHaveBeenCalledWith("/login");
    });

    expect(authService.register).toHaveBeenCalledWith({
      firstName: "Ana",
      lastName: "Pérez",
      email: "ana@test.com",
      password: "Password123!",
    });
  });

  it("shows a generic error when registration fails", async () => {
    vi.mocked(authService.register).mockRejectedValue(
      new Error("Email already exists")
    );

    const user = userEvent.setup();

    render(
      <MemoryRouter>
        <Register />
      </MemoryRouter>
    );

    await user.type(screen.getByLabelText(/^nombre$/i), "Ana");
    await user.type(screen.getByLabelText(/^apellido$/i), "Pérez");
    await user.type(
      screen.getByLabelText(/correo electrónico/i),
      "ana@test.com"
    );
    await user.type(screen.getByLabelText(/^contraseña/i), "Password123!");
    await user.click(screen.getByRole("checkbox"));
    await user.click(screen.getByRole("button", { name: /crear cuenta/i }));

    expect(
      await screen.findByText("No fue posible crear la cuenta.")
    ).toBeInTheDocument();
    expect(navigateMock).not.toHaveBeenCalled();
  });
});
