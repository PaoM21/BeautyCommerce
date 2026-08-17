import { api } from "./api";

export interface CatalogItem {
  id: string;
  name: string;
  slug?: string;
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

  const items = Array.isArray(data)
    ? data
    : Array.isArray(data?.data)
    ? data.data
    : [];

  return items.map((c: any) => ({
    id: c.id,
    name: c.name,
    slug: c.slug ?? "",
  }));
}
