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
  shippingRecipientName: string;
  shippingPhone: string;
  shippingAddressLine: string;
  shippingCity: string;
  shippingDepartment: string;
  carrier?: string | null;
  trackingNumber?: string | null;
  shippedAt?: string | null;
  items: OrderItem[];
}

export interface CheckoutResponse {
  orderId: string;
  orderNumber?: string;
}

export interface CheckoutRequest {
  recipientName: string;
  phone: string;
  addressLine: string;
  city: string;
  department: string;
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

export async function updateOrderShipping(
  orderId: string,
  carrier: string,
  trackingNumber: string
): Promise<void> {
  await api.put("/admin/Orders/shipping", {
    orderId,
    carrier,
    trackingNumber,
  });
}

export async function checkout(
  data: CheckoutRequest
): Promise<CheckoutResponse> {
  const response = await api.post<CheckoutResponse>(
    "/Orders/checkout",
    data
  );

  return response.data;
}

// Bogotá D.C. + los 32 departamentos de Colombia.
export const COLOMBIAN_DEPARTMENTS = [
  "Bogotá D.C.",
  "Amazonas",
  "Antioquia",
  "Arauca",
  "Atlántico",
  "Bolívar",
  "Boyacá",
  "Caldas",
  "Caquetá",
  "Casanare",
  "Cauca",
  "Cesar",
  "Chocó",
  "Córdoba",
  "Cundinamarca",
  "Guainía",
  "Guaviare",
  "Huila",
  "La Guajira",
  "Magdalena",
  "Meta",
  "Nariño",
  "Norte de Santander",
  "Putumayo",
  "Quindío",
  "Risaralda",
  "San Andrés y Providencia",
  "Santander",
  "Sucre",
  "Tolima",
  "Valle del Cauca",
  "Vaupés",
  "Vichada",
];

// Debe mantenerse en sincronía con ShippingCostCalculator (backend) —
// es solo una vista previa para el checkout, el costo real se calcula
// y se persiste en el servidor.
export function estimateShippingCost(city: string): number {
  const normalized = city
    .trim()
    .toLowerCase()
    .replace(/[áàâä]/g, "a")
    .replace(/[éèêë]/g, "e")
    .replace(/[íìîï]/g, "i")
    .replace(/[óòôö]/g, "o")
    .replace(/[úùûü]/g, "u");

  return normalized.startsWith("bogota") ? 8000 : 15000;
}
