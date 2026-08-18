export interface Review {
  id: string;
  userName: string;
  rating: number;
  comment: string;
  createdAt: string;
}

export interface FeaturedReview {
  id: string;
  userName: string;
  rating: number;
  comment: string;
  createdAt: string;
  productId: string;
  productName: string;
}
