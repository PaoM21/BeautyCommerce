import { beforeEach, describe, expect, it } from "vitest";

import { useAuthStore } from "./authStore";

function fakeJwt(payload: Record<string, unknown>): string {
  const header = btoa(JSON.stringify({ alg: "none", typ: "JWT" }));
  const body = btoa(JSON.stringify(payload));

  return `${header}.${body}.signature`;
}

describe("authStore", () => {
  beforeEach(() => {
    localStorage.clear();
    useAuthStore.setState({ token: null, user: null });
  });

  it("starts logged out", () => {
    expect(useAuthStore.getState().token).toBeNull();
    expect(useAuthStore.getState().user).toBeNull();
  });

  it("login stores the token in localStorage and state", () => {
    const token = fakeJwt({ role: "Customer" });

    useAuthStore.getState().login(token, {
      id: "1",
      email: "cliente@test.com",
    });

    expect(useAuthStore.getState().token).toBe(token);
    expect(localStorage.getItem("beauty_token")).toBe(token);
  });

  it("login decodes the role claim from the JWT payload", () => {
    const token = fakeJwt({ role: "Admin" });

    useAuthStore.getState().login(token, {
      id: "1",
      email: "admin@test.com",
    });

    expect(useAuthStore.getState().user?.role).toBe("Admin");
  });

  it("login falls back to the ASP.NET claims URI for the role", () => {
    const token = fakeJwt({
      "http://schemas.microsoft.com/ws/2008/06/identity/claims/role":
        "Admin",
    });

    useAuthStore.getState().login(token, {
      id: "1",
      email: "admin@test.com",
    });

    expect(useAuthStore.getState().user?.role).toBe("Admin");
  });

  it("login does not throw and leaves role undefined for a malformed token", () => {
    expect(() =>
      useAuthStore.getState().login("not-a-real-jwt", {
        id: "1",
        email: "cliente@test.com",
      })
    ).not.toThrow();

    expect(useAuthStore.getState().user?.role).toBeUndefined();
  });

  it("login without a user leaves user null even with a valid token", () => {
    useAuthStore.getState().login(fakeJwt({ role: "Customer" }));

    expect(useAuthStore.getState().user).toBeNull();
  });

  it("logout clears the token from state and localStorage", () => {
    useAuthStore.getState().login(fakeJwt({ role: "Customer" }), {
      id: "1",
      email: "cliente@test.com",
    });

    useAuthStore.getState().logout();

    expect(useAuthStore.getState().token).toBeNull();
    expect(useAuthStore.getState().user).toBeNull();
    expect(localStorage.getItem("beauty_token")).toBeNull();
  });
});
