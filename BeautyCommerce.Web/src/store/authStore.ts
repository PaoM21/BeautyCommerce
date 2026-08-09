import { create } from "zustand";

interface User {
  id: string;
  email: string;
  firstName?: string;
  lastName?: string;
  role?: string;
}

interface AuthStore {
  token: string | null;
  user: User | null;

  login: (token: string, user: User | null) => void;
  logout: () => void;

  isAdmin: () => boolean;
}

function getRoleFromToken(token: string): string | null {
  try {
    const payload = token.split(".")[1];

    if (!payload) return null;

    const decoded = JSON.parse(
      atob(
        payload
          .replace(/-/g, "+")
          .replace(/_/g, "/")
      )
    );

    return (
      decoded.role ??
      decoded[
        "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
      ] ??
      null
    );
  } catch {
    return null;
  }
}

export const useAuthStore = create<AuthStore>(
  (set, get) => ({
    token: localStorage.getItem("beauty_token"),

    user: null,

    login: (token, user) => {
      localStorage.setItem("beauty_token", token);

      const role =
        user?.role ??
        getRoleFromToken(token) ??
        undefined;

      set({
        token,
        user: user
          ? {
              ...user,
              role,
            }
          : null,
      });
    },

    logout: () => {
      localStorage.removeItem("beauty_token");

      set({
        token: null,
        user: null,
      });
    },

    isAdmin: () =>
      get().user?.role?.toLowerCase() === "admin",
  })
);
