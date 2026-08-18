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

// --- Administración de categorías ---

export interface CategoryDetail {
  id: string;
  name: string;
  slug: string;
  description: string;
  imageUrl: string;
  parentCategoryId: string | null;
}

export interface CategoryInput {
  name: string;
  slug: string;
  description: string;
  imageUrl: string;
  parentCategoryId: string | null;
}

export async function getCategoryDetails(): Promise<CategoryDetail[]> {
  const response = await api.get<any>("/Categories");

  return response.data?.data ?? [];
}

export async function getCategoryById(id: string): Promise<CategoryDetail> {
  const response = await api.get<any>(`/Categories/${id}`);

  return response.data.data;
}

export async function createCategory(input: CategoryInput): Promise<string> {
  const response = await api.post<any>("/Categories", { category: input });

  return response.data.data;
}

export async function updateCategory(
  id: string,
  input: CategoryInput
): Promise<void> {
  await api.put(`/Categories/${id}`, input);
}

export async function deleteCategory(id: string): Promise<void> {
  await api.delete(`/Categories/${id}`);
}

// --- Administración de marcas ---

export interface BrandDetail {
  id: string;
  name: string;
  description: string;
  logoUrl: string;
  isActive: boolean;
  isDeleted: boolean;
  deletedAt: string | null;
}

export interface BrandInput {
  name: string;
  description: string;
  logoUrl: string;
}

export async function getBrandDetails(): Promise<BrandDetail[]> {
  const response = await api.get<any>("/Brands");

  return response.data?.data ?? [];
}

export async function getBrandById(id: string): Promise<BrandDetail> {
  const response = await api.get<any>(`/Brands/${id}`);

  return response.data.data;
}

export async function createBrand(input: BrandInput): Promise<string> {
  const response = await api.post<any>("/Brands", { brand: input });

  return response.data.data;
}

export async function updateBrand(
  id: string,
  current: BrandDetail,
  input: BrandInput
): Promise<void> {
  await api.put(`/Brands/${id}`, {
    brand: {
      ...current,
      ...input,
      id,
    },
  });
}

export async function deleteBrand(id: string): Promise<void> {
  await api.delete(`/Brands/${id}`);
}
