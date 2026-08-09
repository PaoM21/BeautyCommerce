import { api } from "./api";
import type { LoyaltyAccount } from "../types/loyalty";

export async function getMyLoyalty(): Promise<LoyaltyAccount> {
  const response = await api.get<LoyaltyAccount>("/Loyalty");

  return response.data;
}
