import axios from "axios";

export const api = axios.create({
  baseURL: "https://localhost:44353/api",
  headers: {
    "Content-Type": "application/json",
  },
});

api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem("beauty_token");

    if (token) {
      // eslint-disable-next-line @typescript-eslint/ban-ts-comment
      // @ts-ignore
      config.headers.Authorization = `Bearer ${token}`;
    }

    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Global response interceptor: handle auth errors and provide better dev logging
api.interceptors.response.use(
  (response) => response,
  (error) => {
    const status = error?.response?.status;
    const config = error?.config;

    // If unauthorized
    if (status === 401) {
      // If there is no token in storage, this was an anonymous request
      // (e.g., public product list). Do not force a redirect to login
      // for anonymous requests — let callers handle 401 if needed.
      const token = localStorage.getItem("beauty_token");

      // Do not redirect for auth endpoints
      if (!token || (config?.url && config.url.includes("/auth"))) {
        return Promise.reject(error);
      }

      // Otherwise token existed and was invalid/expired: clear it and redirect
      try {
        localStorage.removeItem("beauty_token");
        if (typeof window !== "undefined") {
          window.location.href = "/login";
        }
      } catch {}
    }

    return Promise.reject(error);
  }
);
