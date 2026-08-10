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

  login: (token: string, user?: User | null) => void;
  logout: () => void;
}

function getRoleFromToken(token: string): string | undefined {
  try {
    const payload = JSON.parse(
      atob(token.split(".")[1])
    );

    return (
      payload.role ??
      payload[
        "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
      ]
    );
  } catch {
    return undefined;
  }
}

const storedToken = localStorage.getItem("beauty_token");

export const useAuthStore = create<AuthStore>((set) => ({
  token: storedToken,

  user: storedToken
    ? {
        id: "",
        email: "",
        role: getRoleFromToken(storedToken),
      }
    : null,

  login: (token, user = null) => {
    localStorage.setItem("beauty_token", token);

    const role = getRoleFromToken(token);

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
}));
