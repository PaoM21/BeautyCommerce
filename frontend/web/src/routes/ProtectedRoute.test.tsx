import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it } from "vitest";

import ProtectedRoute from "./ProtectedRoute";
import { useAuthStore } from "../store/authStore";

function renderWithGuard(requiredRole?: string) {
  return render(
    <MemoryRouter initialEntries={["/admin"]}>
      <Routes>
        <Route
          path="/admin"
          element={<ProtectedRoute requiredRole={requiredRole} />}
        >
          <Route index element={<div>Contenido protegido</div>} />
        </Route>
        <Route path="/login" element={<div>Página de login</div>} />
        <Route path="/" element={<div>Página de inicio</div>} />
      </Routes>
    </MemoryRouter>
  );
}

describe("ProtectedRoute", () => {
  beforeEach(() => {
    localStorage.clear();
    useAuthStore.setState({ token: null, user: null });
  });

  it("redirects to /login when there is no token", () => {
    renderWithGuard();

    expect(screen.getByText("Página de login")).toBeInTheDocument();
    expect(screen.queryByText("Contenido protegido")).not.toBeInTheDocument();
  });

  it("renders the nested route when authenticated and no role is required", () => {
    useAuthStore.setState({
      token: "token-123",
      user: { id: "1", email: "cliente@test.com" },
    });

    renderWithGuard();

    expect(screen.getByText("Contenido protegido")).toBeInTheDocument();
  });

  it("redirects home when the user lacks the required role", () => {
    useAuthStore.setState({
      token: "token-123",
      user: { id: "1", email: "cliente@test.com", role: "Customer" },
    });

    renderWithGuard("Admin");

    expect(screen.getByText("Página de inicio")).toBeInTheDocument();
    expect(screen.queryByText("Contenido protegido")).not.toBeInTheDocument();
  });

  it("renders the nested route when the user has the required role", () => {
    useAuthStore.setState({
      token: "token-123",
      user: { id: "1", email: "admin@test.com", role: "Admin" },
    });

    renderWithGuard("Admin");

    expect(screen.getByText("Contenido protegido")).toBeInTheDocument();
  });
});
