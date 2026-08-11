import { api } from "./api";
import type { Product } from "../types/product";

// The API returns a PagedResult with an `items` array. Map it to the
// frontend Product[] shape expected by the UI.
export async function getProducts(): Promise<Product[]> {
  const response = await api.get<any>("/products");

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
    brand: p.brand ? { id: "", name: p.brand } : undefined,
    category: p.category ? { id: "", name: p.category } : undefined,
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
  } as Product));
}

export async function getProductById(
  id: string
): Promise<Product> {
  const response = await api.get<any>(`/products/${id}`);

  const p = response.data;

  // Map API product detail shape to frontend Product type
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
        sku: v.name ?? "",
        color: v.color ?? "",
        size: v.size ?? "",
        stock: Number(v.stock ?? 0),
        price: Number(v.price ?? 0),
      }))
    : [];

  // Choose a sensible product price: lowest variant price or 0
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
    brand: p.brand ? { id: "", name: p.brand } : undefined,
    category: p.category ? { id: "", name: p.category } : undefined,
    images: images,
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

export async function updateVariantStock(
  productVariantId: string,
  quantity: number,
  reason: string
): Promise<void> {
  await api.put("/ProductVariants/stock", {
    stock: {
      productVariantId,
      quantity,
      reason,
    },
  });
}
