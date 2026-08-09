import { Routes, Route } from "react-router-dom";

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
import OrderConfirmation from "../pages/OrderConfirmation/OrderConfirmation";
import Orders from "../features/orders/pages/OrdersPage";
import OrderDetail from "../pages/OrderDetail/OrderDetail";
import AdminOrders from "../pages/Admin/Orders/AdminOrders";
import AdminOrderDetail from "../pages/Admin/Orders/AdminOrderDetail";
import AdminDashboard from "../pages/Admin/Dashboard/AdminDashboard";
import AdminRoute from "./AdminRoute";
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
          <Route path="/pedido-confirmado/:id" element={<OrderConfirmation />} />
          <Route path="/pedidos" element={<Orders />} />
          <Route path="/pedidos/:id" element={<OrderDetail />} />
          <Route path="/mi-cuenta/pedidos" element={<AccountOrders />} />
          <Route path="/mi-cuenta/pedidos/:id" element={<AccountOrderDetail />} />
          <Route element={<AdminRoute />}>
            <Route path="/admin" element={<AdminDashboard />} />
            <Route path="/admin/pedidos" element={<AdminOrders />} />
            <Route path="/admin/pedidos/:id" element={<AdminOrderDetail />} />
          </Route>
          <Route path="/favoritos" element={<Wishlist />} />
          <Route path="/mi-cuenta/rewards" element={<Loyalty />} />
        </Route>
      </Route>
    </Routes>
  );
}
