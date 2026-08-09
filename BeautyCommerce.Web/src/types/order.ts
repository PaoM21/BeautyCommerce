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
