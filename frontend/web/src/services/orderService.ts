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
  subTotal: number;
  shippingCost: number;
  tax: number;
  total: number;
  transactionId?: string | null;
  items: OrderItem[];
}

export interface CheckoutResponse {
  orderId: string;
  orderNumber?: string;
}

// Mirrors OrderStatusValidator.IsValidTransition on the backend — kept in
// sync manually, not fetched, since the transition rules are static.
const VALID_STATUS_TRANSITIONS: Record<string, string[]> = {
  Pending: ["Paid", "Cancelled"],
  Paid: ["Processing", "Cancelled"],
  Processing: ["Shipped"],
  Shipped: ["Delivered"],
  Delivered: [],
  Cancelled: [],
};

export function getValidNextStatuses(currentStatus: string): string[] {
  return VALID_STATUS_TRANSITIONS[currentStatus] ?? [];
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
