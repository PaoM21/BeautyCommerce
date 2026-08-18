import { api } from "./api";
import type { FeaturedReview, Review } from "../types/review";

export async function getProductReviews(
    productId: string
): Promise<Review[]> {
    const response = await api.get<Review[]>(
        `/Reviews/product/${productId}`
    );

    return response.data;
}

export async function getFeaturedReviews(
    count = 6
): Promise<FeaturedReview[]> {
    const response = await api.get<FeaturedReview[]>(
        `/Reviews/featured?count=${count}`
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
