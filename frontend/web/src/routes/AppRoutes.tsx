import { Routes, Route, Navigate } from "react-router-dom";

import MainLayout from "../components/layout/MainLayout";
import Home from "../pages/Home/Home";
import Products from "../pages/Products/Products";
import ProductDetail from "../pages/ProductDetail/ProductDetail";
import Cart from "../pages/Cart/Cart";
import Login from "../pages/Login/Login";
import Register from "../pages/Register/Register";
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
import AccountOrders from "../pages/Account/Orders";
import AccountOrderDetail from "../pages/Account/OrderDetail";
import Wishlist from "../pages/Wishlist/Wishlist";
import Loyalty from "../pages/Account/Loyalty";

export default function AppRoutes() {
  return (
    <Routes>
      <Route element={<MainLayout />}>
        <Route path="/" element={<Home />} />

        <Route path="/productos" element={<Products />} />

        <Route path="/productos/:id" element={<ProductDetail />} />

        <Route path="/login" element={<Login />} />

        <Route path="/registro" element={<Register />} />

        <Route element={<ProtectedRoute />}>
          <Route path="/carrito" element={<Cart />} />
          <Route path="/checkout" element={<Checkout />} />
          <Route path="/checkout/success" element={<CheckoutSuccess />} />
          <Route path="/mi-cuenta/pedidos" element={<AccountOrders />} />
          <Route path="/mi-cuenta/pedidos/:id" element={<AccountOrderDetail />} />
          <Route path="/favoritos" element={<Wishlist />} />
          <Route path="/mi-cuenta/rewards" element={<Loyalty />} />
        </Route>

        <Route element={<ProtectedRoute requiredRole="Admin" />}>
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
        </Route>
      </Route>
    </Routes>
  );
}
