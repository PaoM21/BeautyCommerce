import { api } from "./api";
import type { WishlistItem } from "../types/wishlist";

export async function getWishlist(): Promise<WishlistItem[]> {
  try {
    const response = await api.get<WishlistItem[]>("/Wishlist");

    return response.data ?? [];
  } catch (err: any) {
    // If anonymous user or token invalid, return empty wishlist instead of throwing
    const status = err?.response?.status;

    if (status === 401 || status === 403) return [];

    throw err;
  }
}

export async function addToWishlist(
  productId: string
) {
  const response = await api.post(
    "/Wishlist",
    {
      productId,
    }
  );

  return response.data;
}

export async function removeFromWishlist(
  productId: string
) {
  const response = await api.delete(
    `/Wishlist/${productId}`
  );

  return response.data;
}
