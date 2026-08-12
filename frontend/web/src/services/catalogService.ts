import { api } from "./api";

export interface CatalogItem {
  id: string;
  name: string;
}

export async function getBrands(): Promise<CatalogItem[]> {
  const response = await api.get<any>("/Brands");

  const data = response.data;

  return Array.isArray(data)
    ? data
    : Array.isArray(data?.data)
    ? data.data
    : [];
}

export async function getCategories(): Promise<CatalogItem[]> {
  const response = await api.get<any>("/Categories");

  const data = response.data;

  return Array.isArray(data)
    ? data
    : Array.isArray(data?.data)
    ? data.data
    : [];
}
