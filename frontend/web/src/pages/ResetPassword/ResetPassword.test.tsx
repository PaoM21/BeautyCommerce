import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";

import ResetPassword from "./ResetPassword";
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

function renderAt(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route path="/restablecer-password" element={<ResetPassword />} />
      </Routes>
    </MemoryRouter>
  );
}

describe("ResetPassword", () => {
  beforeEach(() => {
    navigateMock.mockReset();
    vi.mocked(authService.resetPassword).mockReset();
  });

  it("shows an invalid link message when email or token are missing", () => {
    renderAt("/restablecer-password");

    expect(
      screen.getByText("Este enlace no es válido. Solicita uno nuevo.")
    ).toBeInTheDocument();
  });

  it("requires both password fields to be filled", async () => {
    const user = userEvent.setup();

    renderAt("/restablecer-password?email=cliente@test.com&token=abc");

    await user.click(
      screen.getByRole("button", { name: /restablecer contraseña/i })
    );

    expect(screen.getByText("Completa ambos campos.")).toBeInTheDocument();
    expect(authService.resetPassword).not.toHaveBeenCalled();
  });

  it("rejects mismatched passwords", async () => {
    const user = userEvent.setup();

    renderAt("/restablecer-password?email=cliente@test.com&token=abc");

    await user.type(screen.getByLabelText(/^nueva contraseña$/i), "Abc12345!");
    await user.type(
      screen.getByLabelText(/confirmar nueva contraseña/i),
      "Different1!"
    );
    await user.click(
      screen.getByRole("button", { name: /restablecer contraseña/i })
    );

    expect(
      screen.getByText("Las contraseñas no coinciden.")
    ).toBeInTheDocument();
    expect(authService.resetPassword).not.toHaveBeenCalled();
  });

  it("resets the password and redirects to login on success", async () => {
    vi.mocked(authService.resetPassword).mockResolvedValue(undefined);

    const user = userEvent.setup();

    renderAt("/restablecer-password?email=cliente@test.com&token=abc");

    await user.type(screen.getByLabelText(/^nueva contraseña$/i), "Abc12345!");
    await user.type(
      screen.getByLabelText(/confirmar nueva contraseña/i),
      "Abc12345!"
    );
    await user.click(
      screen.getByRole("button", { name: /restablecer contraseña/i })
    );

    await waitFor(() => {
      expect(authService.resetPassword).toHaveBeenCalledWith(
        "cliente@test.com",
        "abc",
        "Abc12345!"
      );
    });

    expect(navigateMock).toHaveBeenCalledWith("/login", {
      state: { message: "Tu contraseña fue actualizada. Inicia sesión." },
    });
  });

  it("shows the backend error message when the link expired", async () => {
    vi.mocked(authService.resetPassword).mockRejectedValue({
      response: { data: { detail: "El enlace expiró." } },
    });

    const user = userEvent.setup();

    renderAt("/restablecer-password?email=cliente@test.com&token=abc");

    await user.type(screen.getByLabelText(/^nueva contraseña$/i), "Abc12345!");
    await user.type(
      screen.getByLabelText(/confirmar nueva contraseña/i),
      "Abc12345!"
    );
    await user.click(
      screen.getByRole("button", { name: /restablecer contraseña/i })
    );

    expect(await screen.findByText("El enlace expiró.")).toBeInTheDocument();
  });
});
