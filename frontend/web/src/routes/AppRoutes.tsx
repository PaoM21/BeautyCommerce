import { Suspense, lazy } from "react";
import { Routes, Route, Navigate, Outlet } from "react-router-dom";
import { Box, CircularProgress } from "@mui/material";

import MainLayout from "../components/layout/MainLayout";
import AdminLayout from "../components/layout/AdminLayout";
import Home from "../pages/Home/Home";
import Products from "../pages/Products/Products";
import ProductDetail from "../pages/ProductDetail/ProductDetail";
import Cart from "../pages/Cart/Cart";
import Login from "../pages/Login/Login";
import Register from "../pages/Register/Register";
import ForgotPassword from "../pages/ForgotPassword/ForgotPassword";
import ResetPassword from "../pages/ResetPassword/ResetPassword";
import ProtectedRoute from "./ProtectedRoute";
import Checkout from "../pages/Checkout/Checkout";
import CheckoutSuccess from "../pages/Checkout/CheckoutSuccess";
import AccountOrders from "../pages/Account/Orders";
import AccountOrderDetail from "../pages/Account/OrderDetail";
import Wishlist from "../pages/Wishlist/Wishlist";
import Loyalty from "../pages/Account/Loyalty";
import TermsAndConditions from "../pages/Legal/TermsAndConditions";
import DataProcessingPolicy from "../pages/Legal/DataProcessingPolicy";

// Cargadas bajo demanda: el panel admin nunca lo visita la mayoría del
// tráfico de la tienda, así que no debería ir en el bundle inicial.
const AdminOrders = lazy(() => import("../pages/Admin/Orders/AdminOrders"));
const AdminOrderDetail = lazy(() => import("../pages/Admin/OrderDetail/OrderDetail"));
const AdminProducts = lazy(() => import("../pages/Admin/Products/AdminProducts"));
const AdminProductCreate = lazy(() => import("../pages/Admin/Products/AdminProductCreate"));
const AdminProductDetail = lazy(() => import("../pages/Admin/Products/AdminProductDetail"));
const AdminProductEdit = lazy(() => import("../pages/Admin/Products/AdminProductEdit"));
const Dashboard = lazy(() => import("../pages/Admin/Dashboard/Dashboard"));
const AdminCustomers = lazy(() => import("../pages/Admin/Customers/AdminCustomers"));
const AdminInventory = lazy(() => import("../pages/Admin/Inventory/AdminInventory"));
const AdminCategories = lazy(() => import("../pages/Admin/Categories/AdminCategories"));
const AdminCategoryForm = lazy(() => import("../pages/Admin/Categories/AdminCategoryForm"));
const AdminBrands = lazy(() => import("../pages/Admin/Brands/AdminBrands"));
const AdminBrandForm = lazy(() => import("../pages/Admin/Brands/AdminBrandForm"));

function AdminFallback() {
  return (
    <Box sx={{ display: "flex", justifyContent: "center", py: 10 }}>
      <CircularProgress />
    </Box>
  );
}

export default function AppRoutes() {
  return (
    <Routes>
      <Route element={<MainLayout />}>
        <Route path="/" element={<Home />} />

        <Route path="/productos" element={<Products />} />

        <Route path="/productos/:id" element={<ProductDetail />} />

        <Route path="/login" element={<Login />} />

        <Route path="/registro" element={<Register />} />

        <Route path="/olvide-password" element={<ForgotPassword />} />

        <Route path="/restablecer-password" element={<ResetPassword />} />

        <Route path="/terminos-y-condiciones" element={<TermsAndConditions />} />

        <Route path="/tratamiento-de-datos-personales" element={<DataProcessingPolicy />} />

        <Route element={<ProtectedRoute />}>
          <Route path="/carrito" element={<Cart />} />
          <Route path="/checkout" element={<Checkout />} />
          <Route path="/checkout/success" element={<CheckoutSuccess />} />
          <Route path="/mi-cuenta/pedidos" element={<AccountOrders />} />
          <Route path="/mi-cuenta/pedidos/:id" element={<AccountOrderDetail />} />
          <Route path="/favoritos" element={<Wishlist />} />
          <Route path="/mi-cuenta/rewards" element={<Loyalty />} />
        </Route>
      </Route>

      <Route element={<ProtectedRoute requiredRole="Admin" />}>
        <Route element={<AdminLayout />}>
          <Route
            element={
              <Suspense fallback={<AdminFallback />}>
                <Outlet />
              </Suspense>
            }
          >
            <Route path="/admin" element={<Navigate to="/admin/dashboard" replace />} />
            <Route path="/admin/dashboard" element={<Dashboard />} />

            <Route path="/admin/productos" element={<AdminProducts />} />
            <Route path="/admin/productos/nuevo" element={<AdminProductCreate />} />
            <Route path="/admin/productos/:id" element={<AdminProductDetail />} />
            <Route path="/admin/productos/:id/editar" element={<AdminProductEdit />} />

            <Route path="/admin/pedidos" element={<AdminOrders />} />
            <Route path="/admin/pedidos/:id" element={<AdminOrderDetail />} />

            <Route path="/admin/clientes" element={<AdminCustomers />} />

            <Route path="/admin/inventario" element={<AdminInventory />} />

            <Route path="/admin/categorias" element={<AdminCategories />} />
            <Route path="/admin/categorias/nueva" element={<AdminCategoryForm />} />
            <Route path="/admin/categorias/:id/editar" element={<AdminCategoryForm />} />

            <Route path="/admin/marcas" element={<AdminBrands />} />
            <Route path="/admin/marcas/nueva" element={<AdminBrandForm />} />
            <Route path="/admin/marcas/:id/editar" element={<AdminBrandForm />} />
          </Route>
        </Route>
      </Route>
    </Routes>
  );
}
