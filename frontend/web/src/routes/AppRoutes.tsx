import { Routes, Route, Navigate } from "react-router-dom";

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
import AdminOrders from "../pages/Admin/Orders/AdminOrders";
import AdminOrderDetail from "../pages/Admin/OrderDetail/OrderDetail";
import AdminProducts from "../pages/Admin/Products/AdminProducts";
import AdminProductCreate from "../pages/Admin/Products/AdminProductCreate";
import AdminProductDetail from "../pages/Admin/Products/AdminProductDetail";
import AdminProductEdit from "../pages/Admin/Products/AdminProductEdit";
import Dashboard from "../pages/Admin/Dashboard/Dashboard";
import AdminCustomers from "../pages/Admin/Customers/AdminCustomers";
import AdminInventory from "../pages/Admin/Inventory/AdminInventory";
import AdminCategories from "../pages/Admin/Categories/AdminCategories";
import AdminCategoryForm from "../pages/Admin/Categories/AdminCategoryForm";
import AdminBrands from "../pages/Admin/Brands/AdminBrands";
import AdminBrandForm from "../pages/Admin/Brands/AdminBrandForm";
import AccountOrders from "../pages/Account/Orders";
import AccountOrderDetail from "../pages/Account/OrderDetail";
import Wishlist from "../pages/Wishlist/Wishlist";
import Loyalty from "../pages/Account/Loyalty";
import TermsAndConditions from "../pages/Legal/TermsAndConditions";
import DataProcessingPolicy from "../pages/Legal/DataProcessingPolicy";

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
    </Routes>
  );
}
