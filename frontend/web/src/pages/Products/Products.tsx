import { useQuery } from "@tanstack/react-query";
import {
  Box,
  CircularProgress,
  Container,
  Grid,
  Rating,
  Typography,
} from "@mui/material";
import { Link } from "react-router-dom";
import FavoriteBorderIcon from "@mui/icons-material/FavoriteBorder";
import FavoriteIcon from "@mui/icons-material/Favorite";
import IconButton from "@mui/material/IconButton";

import { useWishlist } from "../../hooks/useWishlist";

import { getProducts } from "../../services/productService";
import type { Product } from "../../types/product";

export default function Products() {
  const {
    data: products = [],
    isLoading,
    isError,
  } = useQuery({
    queryKey: ["products"],
    queryFn: getProducts,
  });

  if (isLoading) {
    return (
      <Box
        sx={{
          minHeight: "60vh",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
        }}
      >
        <CircularProgress />
      </Box>
    );
  }

  if (isError) {
    return (
      <Container sx={{ py: 10 }}>
        <Typography>
          No fue posible cargar los productos.
        </Typography>
      </Container>
    );
  }

  return (
    <Container maxWidth="xl" sx={{ py: 8 }}>
      <Typography
        sx={{
          fontSize: 13,
          letterSpacing: 3,
          textTransform: "uppercase",
          color: "#9a8065",
          mb: 2,
        }}
      >
        Beauty Collection
      </Typography>

      <Typography
        component="h1"
        sx={{
          fontSize: { xs: 36, md: 48 },
          fontWeight: 400,
          mb: 6,
        }}
      >
        Todos nuestros productos
      </Typography>

      {products.length === 0 ? (
        <Typography>
          No hay productos disponibles.
        </Typography>
      ) : (
        <Grid container spacing={3}>
          {products.map((product) => (
            <Grid
              key={product.id}
              size={{ xs: 12, sm: 6, md: 4, lg: 3 }}
            >
              <ProductCard product={product} />
            </Grid>
          ))}
        </Grid>
      )}
    </Container>
  );
}

function ProductCard({ product }: { product: Product }) {
  const image =
    product.images?.find((x) => x.isPrimary)?.imageUrl ??
    product.images?.[0]?.imageUrl;

  const { isFavorite, toggleFavorite } = useWishlist();

  return (
    <Box
      component={Link}
      to={`/productos/${product.id}`}
      sx={{
        textDecoration: "none",
        color: "inherit",
        display: "block",
      }}
    >
      <Box
        sx={{
          aspectRatio: "1 / 1",
          backgroundColor: "#f6f4f1",
          overflow: "hidden",
          mb: 2,
          position: "relative",
        }}
      >
        <IconButton
          onClick={(e) => {
            e.preventDefault();
            e.stopPropagation();
            toggleFavorite(product.id);
          }}
          sx={{
            position: "absolute",
            top: 12,
            right: 12,
            backgroundColor: "white",
            zIndex: 2,
            "&:hover": {
              backgroundColor: "white",
            },
          }}
        >
          {isFavorite(product.id) ? (
            <FavoriteIcon />
          ) : (
            <FavoriteBorderIcon />
          )}
        </IconButton>

        {image ? (
          <Box
            component="img"
            src={image}
            alt={product.name}
            sx={{
              width: "100%",
              height: "100%",
              objectFit: "cover",
              transition: "transform .4s ease",
              "&:hover": {
                transform: "scale(1.04)",
              },
            }}
          />
        ) : (
          <Box
            sx={{
              width: "100%",
              height: "100%",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              color: "#aaa",
            }}
          >
            Sin imagen
          </Box>
        )}
      </Box>

      <Typography
        sx={{
          fontSize: 11,
          color: "#999",
          textTransform: "uppercase",
          letterSpacing: 1,
        }}
      >
        {product.brand?.name}
      </Typography>

      <Typography
        sx={{
          fontSize: 16,
          mt: 0.5,
          mb: 1,
        }}
      >
        {product.name}
      </Typography>

      {/* Average rating and count */}
      <Box
        sx={{
          display: "flex",
          alignItems: "center",
          gap: 0.5,
          mb: 1,
        }}
      >
        <Rating
          value={product.averageRating ?? 0}
          precision={0.1}
          readOnly
          size="small"
        />

        <Typography
          sx={{
            fontSize: 12,
            color: "#999",
          }}
        >
          {product.averageRating && product.averageRating > 0
            ? product.averageRating.toFixed(1)
            : "Sin reseñas"}{" "}
          {product.reviewCount && product.reviewCount > 0
            ? `(${product.reviewCount})`
            : ""}
        </Typography>
      </Box>

      <Typography sx={{ fontWeight: 500 }}>
        ${product.price.toLocaleString("es-CO")}
      </Typography>
    </Box>
  );
}
