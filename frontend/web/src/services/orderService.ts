import { api } from "./api";

export interface OrderItem {
  id: string;
  productVariantId: string;
  productName: string;
  color: string;
  size: string;
  quantity: number;
  unitPrice: number;
  subtotal: number;
  imageUrl?: string | null;
}

export interface Order {
  id: string;
  userId: string;
  customerName?: string | null;
  customerEmail?: string | null;
  orderNumber: string;
  orderDate: string;
  status: string;
  total: number;
  transactionId?: string | null;
  items: OrderItem[];
}

export interface CheckoutResponse {
  orderId: string;
  orderNumber?: string;
}

export async function getOrders(): Promise<Order[]> {
  const response = await api.get<Order[]>("/Orders");

  return response.data;
}

export async function getMyOrders(): Promise<Order[]> {
  return getOrders();
}

export async function getOrderById(
  id: string
): Promise<Order> {
  const response = await api.get<Order>(
    `/Orders/${id}`
  );

  return response.data;
}

export async function getAdminOrders(): Promise<Order[]> {
  const response = await api.get<Order[]>("/admin/Orders");

  return response.data;
}

export async function getAdminOrderById(
  id: string
): Promise<Order> {
  const response = await api.get<Order>(
    `/admin/Orders/${id}`
  );

  return response.data;
}

export async function updateOrderStatus(
  orderId: string,
  status: string
): Promise<void> {
  await api.put("/admin/Orders/status", {
    orderId,
    status,
  });
}

export async function checkout(): Promise<CheckoutResponse> {
  const response = await api.post<CheckoutResponse>(
    "/Orders/checkout"
  );

  return response.data;
}
