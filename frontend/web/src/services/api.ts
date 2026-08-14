import axios from "axios";

const baseURL = import.meta.env.VITE_API_URL;

if (!baseURL) {
  throw new Error(
    "VITE_API_URL no está configurada. Define esta variable de entorno antes de compilar o ejecutar la aplicación."
  );
}

export const api = axios.create({
  baseURL,
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

api.interceptors.response.use(
  (response) => response,
  (error) => {
    const status = error?.response?.status;
    const config = error?.config;

    // If unauthorized
    if (status === 401) {
      const token = localStorage.getItem("beauty_token");

      if (!token || (config?.url && config.url.includes("/auth"))) {
        return Promise.reject(error);
      }

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
