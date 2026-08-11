import { api } from "./api";

export interface InventoryItem {
  productVariantId: string;
  productId: string;
  productName: string;
  sku: string;
  barcode: string;
  color: string;
  size: string;
  price: number;
  stock: number;
  minimumStock: number;
  imageUrl?: string | null;
  stockStatus: string;
}

export interface InventoryMovement {
  id: string;
  productVariantId: string;
  productName: string;
  sku: string;
  color: string;
  size: string;
  quantity: number;
  isEntry: boolean;
  reason: string;
  userId?: string | null;
  createdAt: string;
}

export interface UpdateStockRequest {
  productVariantId: string;
  quantity: number;
  isEntry: boolean;
  reason: string;
}

export async function getInventory(): Promise<InventoryItem[]> {
  const response = await api.get<InventoryItem[]>("/inventory");

  return response.data;
}

export async function getInventoryMovements(): Promise<InventoryMovement[]> {
  const response = await api.get<InventoryMovement[]>("/inventory/movements");

  return response.data;
}

export async function updateInventoryStock(
  data: UpdateStockRequest
): Promise<void> {
  await api.put("/inventory/stock", data);
}
