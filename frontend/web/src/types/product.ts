export interface Product {
  id: string;
  sku: string;
  barcode: string;

  name: string;
  slug: string;

  shortDescription?: string;
  description?: string;

  price: number;
  oldPrice?: number;

  brandId: string;
  categoryId: string;

  brand?: {
    id: string;
    name: string;
  };

  category?: {
    id: string;
    name: string;
  };

  images?: ProductImage[];

  isFeatured?: boolean;

  variants?: ProductVariant[];
  averageRating?: number;
  reviewCount?: number;
  defaultVariantId?: string;
}

export interface ProductImage {
  id: string;
  imageUrl: string;
  isPrimary?: boolean;
  displayOrder?: number;
}

export interface ProductVariant {
  id: string;
  productId: string;
  sku: string;
  color?: string;
  size?: string;
  stock: number;
  price: number;
}
