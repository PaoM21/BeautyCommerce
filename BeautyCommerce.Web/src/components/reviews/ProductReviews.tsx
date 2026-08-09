import {
    Box,
    Divider,
    Rating,
    Typography,
} from "@mui/material";

import { useQuery } from "@tanstack/react-query";

import { getProductReviews } from "../../services/reviewService";

interface ProductReviewsProps {
    productId: string;
}

export default function ProductReviews({
    productId,
}: ProductReviewsProps) {
    const {
        data: reviews = [],
        isLoading,
    } = useQuery({
        queryKey: ["reviews", productId],
        queryFn: () => getProductReviews(productId),
    });

    if (isLoading) {
        return (
            <Typography sx={{ color: "#888" }}>
                Cargando reseñas...
            </Typography>
        );
    }

    return (
        <Box>
            <Box
                sx={{
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "space-between",
                    mb: 4,
                }}
            >
                <Typography
                    component="h2"
                    sx={{
                        fontSize: 28,
                        fontWeight: 400,
                    }}
                >
                    Opiniones
                </Typography>

                <Typography
                    sx={{
                        fontSize: 14,
                        color: "#888",
                    }}
                >
                    {reviews.length}{" "}
                    {reviews.length === 1
                        ? "reseña"
                        : "reseñas"}
                </Typography>
            </Box>

            {/* Average rating computed from fetched reviews */}
            {reviews.length > 0 && (
                (() => {
                    const averageRating =
                        reviews.reduce((sum, review) => sum + review.rating, 0) / reviews.length;

                    return (
                        <Box
                            sx={{
                                display: "flex",
                                alignItems: "center",
                                gap: 1,
                                mb: 3,
                            }}
                        >
                            <Rating
                                value={averageRating}
                                precision={0.1}
                                readOnly
                                size="small"
                            />

                            <Typography
                                sx={{
                                    fontSize: 13,
                                    color: "#777",
                                }}
                            >
                                {averageRating.toFixed(1)} ({reviews.length})
                            </Typography>
                        </Box>
                    );
                })()
            )}

            {reviews.length === 0 ? (
                <Typography
                    sx={{
                        color: "#888",
                        py: 4,
                    }}
                >
                    Este producto todavía no tiene reseñas.
                </Typography>
            ) : (
                <Box>
                    {reviews.map((review) => (
                        <Box key={review.id} sx={{ py: 3 }}>
                            <Box
                                sx={{
                                    display: "flex",
                                    justifyContent: "space-between",
                                    gap: 2,
                                    mb: 1,
                                }}
                            >
                                <Typography fontWeight={500}>
                                    {review.userName}
                                </Typography>

                                <Typography
                                    sx={{
                                        fontSize: 13,
                                        color: "#999",
                                    }}
                                >
                                    {new Date(
                                        review.createdAt
                                    ).toLocaleDateString("es-CO")}
                                </Typography>
                            </Box>

                            <Rating
                                value={review.rating}
                                readOnly
                                size="small"
                            />

                            <Typography
                                sx={{
                                    mt: 1.5,
                                    color: "#666",
                                    lineHeight: 1.7,
                                }}
                            >
                                {review.comment}
                            </Typography>

                            <Divider sx={{ mt: 3 }} />
                        </Box>
                    ))}
                </Box>
            )}
        </Box>
    );
}