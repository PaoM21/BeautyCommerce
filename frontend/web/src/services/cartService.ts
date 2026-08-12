import { api } from "./api";
import type {
  AddCartItem,
  ShoppingCart,
} from "../types/cart";

export async function getShoppingCart(): Promise<ShoppingCart> {
  const response = await api.get<{ data: ShoppingCart }>(
    "/cart"
  );

  // API returns ApiResponse<ShoppingCartDto> with Data property
  return response.data?.data ?? { items: [], total: 0 };
}

export async function addCartItem(
  item: AddCartItem
): Promise<void> {
  await api.post("/cart/items", item);
}

export async function removeCartItem(
  productVariantId: string
): Promise<void> {
  await api.delete(`/cart/items/${productVariantId}`);
}
