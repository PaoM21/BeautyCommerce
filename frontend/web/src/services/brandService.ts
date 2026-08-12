import { api } from "./api";

export interface Brand {
  id: string;
  name: string;
  description?: string;
  logoUrl?: string;
}

interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
}

export async function getBrands(): Promise<Brand[]> {
  const response = await api.get<ApiResponse<Brand[]>>("/Brands");

  return response.data.data ?? [];
}
