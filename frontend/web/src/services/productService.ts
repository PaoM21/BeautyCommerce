import { api } from "./api";
import type { Product } from "../types/product";

export interface ProductFilter {
  search?: string;
  categoryId?: string;
}

export async function getProducts(
  filter?: ProductFilter
): Promise<Product[]> {
  const response = await api.get<any>("/products", {
    params: {
      Search: filter?.search || undefined,
      CategoryId: filter?.categoryId || undefined,
    },
  });

  const data = response.data;

  const items = Array.isArray(data?.items) ? data.items : [];

  return items.map((p: any) => ({
    id: p.id,
    sku: p.sku ?? "",
    barcode: p.barcode ?? "",
    name: p.name,
    slug: p.slug ?? "",
    shortDescription: p.shortDescription,
    description: p.description,
    price: p.price ?? 0,
    oldPrice: p.oldPrice ?? undefined,
    brandId: p.brandId ?? "",
    categoryId: p.categoryId ?? "",
    brand: p.brand
      ? {
          id: p.brandId ?? "",
          name: p.brand,
        }
      : undefined,
    category: p.category
      ? {
          id: p.categoryId ?? "",
          name: p.category,
        }
      : undefined,
    images: p.image
      ? [
          {
            id: "",
            imageUrl: p.image,
            isPrimary: true,
          },
        ]
      : [],
    variants: [],
    averageRating: p.averageRating ?? undefined,
    reviewCount: p.reviewCount ?? undefined,
    defaultVariantId: p.defaultVariantId ?? undefined,
  } as Product));
}

export async function getBestSellers(count = 8): Promise<Product[]> {
  const response = await api.get<any[]>("/products/best-sellers", {
    params: { count },
  });

  const items = Array.isArray(response.data) ? response.data : [];

  return items.map((p: any) => ({
    id: p.id,
    sku: p.sku ?? "",
    barcode: p.barcode ?? "",
    name: p.name,
    slug: p.slug ?? "",
    shortDescription: p.shortDescription,
    description: p.description,
    price: p.price ?? 0,
    oldPrice: p.oldPrice ?? undefined,
    brandId: p.brandId ?? "",
    categoryId: p.categoryId ?? "",
    brand: p.brand ? { id: p.brandId ?? "", name: p.brand } : undefined,
    category: p.category ? { id: p.categoryId ?? "", name: p.category } : undefined,
    images: p.image
      ? [{ id: "", imageUrl: p.image, isPrimary: true }]
      : [],
    variants: [],
    averageRating: p.averageRating ?? undefined,
    reviewCount: p.reviewCount ?? undefined,
    defaultVariantId: p.defaultVariantId ?? undefined,
  } as Product));
}

export async function getProductById(
  id: string
): Promise<Product> {
  const response = await api.get<any>(`/products/${id}`);

  const p = response.data;

  const images = Array.isArray(p?.images)
    ? p.images.map((img: any) => ({
        id: img.id ?? "",
        imageUrl: img.url ?? img.imageUrl ?? null,
        isPrimary: img.isPrimary ?? false,
        displayOrder: img.displayOrder ?? undefined,
      }))
    : [];

  const variants = Array.isArray(p?.variants)
    ? p.variants.map((v: any) => ({
        id: v.id,
        productId: p.id,
        sku: v.sku ?? "",
        color: v.color ?? "",
        size: v.size ?? "",
        stock: Number(v.stock ?? 0),
        price: Number(v.price ?? 0),
      }))
    : [];

  const price = variants.length
    ? Math.min(...variants.map((v: any) => Number(v.price ?? 0)))
    : p.price ?? 0;

  const product: Product = {
    id: p.id,
    sku: p.sku ?? "",
    barcode: p.barcode ?? "",
    name: p.name,
    slug: p.slug ?? "",
    shortDescription: p.shortDescription ?? p.short_description ?? undefined,
    description: p.description ?? undefined,
    price: price,
    oldPrice: p.oldPrice ?? undefined,
    brandId: p.brandId ?? "",
    categoryId: p.categoryId ?? "",
    brand: p.brand
      ? {
          id: p.brandId ?? "",
          name: p.brand,
        }
      : undefined,
    category: p.category
      ? {
          id: p.categoryId ?? "",
          name: p.category,
        }
      : undefined,
    images: images,
    isFeatured: p.isFeatured ?? false,
    variants: variants,
    averageRating: p.averageRating ?? undefined,
    reviewCount: p.reviewCount ?? undefined,
  };

  return product;
}

export interface AdminProductListItem {
  id: string;
  name: string;
  brand: string;
  category: string;
  price: number;
  stock: number;
  image: string | null;
}

export interface AdminProductsResult {
  items: AdminProductListItem[];
  page: number;
  pageSize: number;
  totalRecords: number;
  totalPages: number;
}

export async function getAdminProducts(
  page = 1,
  pageSize = 12,
  search?: string
): Promise<AdminProductsResult> {
  const response = await api.get<AdminProductsResult>(
    "/products",
    {
      params: {
        Page: page,
        PageSize: pageSize,
        Search: search || undefined,
      },
    }
  );

  return response.data;
}


export interface UpdateProductInput {
  name: string;
  description: string;
  brandId: string;
  categoryId: string;
  isFeatured: boolean;
}

export async function updateProduct(
  id: string,
  data: UpdateProductInput
): Promise<void> {
  await api.put(`/products/${id}`, {
    name: data.name,
    description: data.description,
    brandId: data.brandId,
    categoryId: data.categoryId,
    isFeatured: data.isFeatured,
  });
}

export interface CreateProductVariant {
  price: number;
  stock: number;
  color: string;
  size: string;
}

export interface CreateProductRequest {
  name: string;
  description: string;
  brandId: string;
  categoryId: string;
  isFeatured: boolean;
  variants: CreateProductVariant[];
  images: string[];
}

export async function createProduct(
  product: CreateProductRequest
): Promise<string> {
  const response = await api.post<string>(
    "/products",
    product
  );

  return response.data;
}

export interface DeleteProductResult {
  success: boolean;
  message: string;
}

export async function deleteProduct(
  id: string
): Promise<DeleteProductResult> {
  try {
    const response = await api.delete<DeleteProductResult>(
      `/products/${id}`
    );

    return {
      success: response.data.success,
      message: response.data.message,
    };
  } catch (err: any) {
    const status = err?.response?.status;

    if (status === 404) {
      return {
        success: false,
        message:
          err?.response?.data?.message ?? "Producto no encontrado.",
      };
    }

    throw err;
  }
}
