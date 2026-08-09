import { api } from "./api";
import type { Review } from "../types/review";

export async function getProductReviews(
    productId: string
): Promise<Review[]> {
    const response = await api.get<Review[]>(
        `/Reviews/product/${productId}`
    );

    return response.data;
}

export interface CreateReviewRequest {
    review: {
        productId: string;
        rating: number;
        comment: string;
    };
}

export async function createReview(
    data: CreateReviewRequest
): Promise<any> {
    const response = await api.post(
        "/Reviews",
        data
    );

    return response.data;
}
