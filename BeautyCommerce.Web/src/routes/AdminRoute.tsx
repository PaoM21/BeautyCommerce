import { Navigate, Outlet } from "react-router-dom";

import { useAuthStore } from "../store/authStore";

export default function AdminRoute() {
  const token = useAuthStore(
    (state) => state.token
  );

  const isAdmin = useAuthStore(
    (state) => state.isAdmin()
  );

  if (!token) {
    return (
      <Navigate
        to="/login"
        replace
      />
    );
  }

  if (!isAdmin) {
    return (
      <Navigate
        to="/"
        replace
      />
    );
  }

  return <Outlet />;
}
