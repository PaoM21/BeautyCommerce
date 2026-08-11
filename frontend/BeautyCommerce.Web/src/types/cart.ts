export interface CartItem {
  productId: string;
  productVariantId: string;
  productName: string;
  color: string;
  size: string;
  quantity: number;
  unitPrice: number;
  subtotal: number;
  imageUrl?: string;
}

export interface ShoppingCart {
  items: CartItem[];
  total: number;
}

export interface AddCartItem {
  productVariantId: string;
  quantity: number;
}
