import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
    Box,
    Button,
    CircularProgress,
    Container,
    Grid,
    Typography,
} from "@mui/material";
import { useParams } from "react-router-dom";
import { useState } from "react";
import { getProductById } from "../../services/productService";
import { addCartItem } from "../../services/cartService";
import { useCartStore } from "../../store/cartStore";
import ProductReviews from "../../components/reviews/ProductReviews";
import CreateReviewForm from "../../components/reviews/CreateReviewForm";
import Seo from "../../components/seo/Seo";

export default function ProductDetail() {
    const [selectedVariant, setSelectedVariant] =
        useState<string | null>(null);

    const [addingToCart, setAddingToCart] =
        useState(false);
    const queryClient = useQueryClient();
    const increment = useCartStore((state) => state.increment);
    const { id } = useParams<{ id: string }>();

    const {
        data: product,
        isLoading,
        isError,
    } = useQuery({
        queryKey: ["product", id],
        queryFn: () => getProductById(id!),
        enabled: Boolean(id),
    });

    const handleAddToCart = async () => {
        if (!product) return;

        let variantId = selectedVariant;

        if (!variantId && product.variants?.length === 1) {
            variantId = product.variants[0].id;
        }

        if (!variantId) {
            alert("Selecciona una variante.");
            return;
        }

        try {
            setAddingToCart(true);

            await addCartItem({
                productVariantId: variantId,
                quantity: 1,
            });

            await queryClient.invalidateQueries({
                queryKey: ["shopping-cart"],
            });

            increment(1);

            alert("Producto agregado al carrito.");
        } catch (error) {
            console.error(error);

            alert(
                "No fue posible agregar el producto al carrito."
            );
        } finally {
            setAddingToCart(false);
        }
    };

    if (isLoading) {
        return (
            <Box
                sx={{
                    minHeight: "70vh",
                    display: "flex",
                    justifyContent: "center",
                    alignItems: "center",
                }}
            >
                <CircularProgress />
            </Box>
        );
    }

    if (isError || !product) {
        return (
            <Container sx={{ py: 10 }}>
                <Typography>
                    Producto no encontrado.
                </Typography>
            </Container>
        );
    }

    const image =
        product.images?.find((x) => x.isPrimary)?.imageUrl ??
            product.images?.[0]?.imageUrl;

    const productJsonLd = {
        "@context": "https://schema.org",
        "@type": "Product",
        name: product.name,
        description: product.shortDescription ?? product.description ?? product.name,
        image: image ? [image] : undefined,
        brand: product.brand?.name
            ? { "@type": "Brand", name: product.brand.name }
            : undefined,
        offers: {
            "@type": "Offer",
            priceCurrency: "COP",
            price: product.price,
            availability: "https://schema.org/InStock",
        },
        ...(product.averageRating && product.reviewCount
            ? {
                  aggregateRating: {
                      "@type": "AggregateRating",
                      ratingValue: product.averageRating,
                      reviewCount: product.reviewCount,
                  },
              }
            : {}),
    };

    return (
        <Container maxWidth="xl" sx={{ py: 8 }}>
            <Seo
                title={product.name}
                description={
                    product.shortDescription ??
                    `Compra ${product.name} ${product.brand?.name ? `de ${product.brand.name}` : ""} en HALDY&CO ECOMMERCE. Envío a toda Colombia.`
                }
                path={`/productos/${product.id}`}
                image={image}
                type="product"
                jsonLd={productJsonLd}
            />

            <Grid container spacing={6}>
                {/* IMAGEN */}
                <Grid size={{ xs: 12, md: 6 }}>
                    <Box
                        sx={{
                            backgroundColor: "#f6f4f1",
                            aspectRatio: "1 / 1",
                            overflow: "hidden",
                        }}
                    >
                        {image && (
                            <Box
                                component="img"
                                src={image}
                                alt={product.name}
                                sx={{
                                    width: "100%",
                                    height: "100%",
                                    objectFit: "cover",
                                }}
                            />
                        )}
                    </Box>
                </Grid>

                {/* INFORMACIÓN */}
                <Grid size={{ xs: 12, md: 6 }}>
                    <Typography
                        sx={{
                            fontSize: 12,
                            textTransform: "uppercase",
                            letterSpacing: 2,
                            color: "#9a8065",
                            mb: 2,
                        }}
                    >
                        {product.brand?.name}
                    </Typography>

                    <Typography
                        component="h1"
                        sx={{
                            fontSize: { xs: 34, md: 48 },
                            fontWeight: 400,
                            lineHeight: 1.1,
                            mb: 3,
                        }}
                    >
                        {product.name}
                    </Typography>

                    <Typography
                        sx={{
                            fontSize: 22,
                            fontWeight: 500,
                            mb: 3,
                        }}
                    >
                        ${Number(product.price ?? 0).toLocaleString("es-CO")}
                    </Typography>

                    {product.shortDescription && (
                        <Typography
                            sx={{
                                color: "#666",
                                lineHeight: 1.8,
                                mb: 4,
                            }}
                        >
                            {product.shortDescription}
                        </Typography>
                    )}

                    {/* VARIANTES */}
                    {product.variants &&
                        product.variants.length > 0 && (
                            <Box sx={{ mb: 4 }}>
                                <Typography
                                    sx={{
                                        fontWeight: 500,
                                        mb: 2,
                                    }}
                                >
                                    Selecciona una variante
                                </Typography>

                                <Box
                                    sx={{
                                        display: "flex",
                                        gap: 1,
                                        flexWrap: "wrap",
                                    }}
                                >
                                    {product.variants.map((variant) => (
                                        <Button
                                            key={variant.id}
                                            variant={
                                                selectedVariant === variant.id
                                                    ? "contained"
                                                    : "outlined"
                                            }
                                            disabled={variant.stock <= 0}
                                            onClick={() =>
                                                setSelectedVariant(variant.id)
                                            }
                                            sx={{
                                                borderRadius: 0,
                                            }}
                                        >
                                            {variant.color ||
                                                variant.size ||
                                                "Variante"}
                                        </Button>
                                    ))}
                                </Box>
                            </Box>
                        )}

                    <Button
                        variant="contained"
                        fullWidth
                        disabled={addingToCart}
                        onClick={handleAddToCart}
                        sx={{
                            backgroundColor: "#1f1f1f",
                            py: 1.8,
                            borderRadius: 0,
                            "&:hover": {
                                backgroundColor: "#333",
                            },
                        }}
                    >
                        {addingToCart
                            ? "Agregando..."
                            : "Agregar al carrito"}
                    </Button>
                </Grid>
            </Grid>
            <Box sx={{ mt: 10 }}>
                <ProductReviews productId={product.id} />

                <CreateReviewForm productId={product.id} />
            </Box>
        </Container>
    );
}

