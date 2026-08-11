import { api } from "./api";

export interface Category {
  id: string;
  name: string;
  description?: string;
}

interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
}

export async function getCategories(): Promise<Category[]> {
  const response = await api.get<ApiResponse<Category[]>>("/Categories");

  return response.data.data ?? [];
}
