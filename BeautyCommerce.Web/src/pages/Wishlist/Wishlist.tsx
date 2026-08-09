import { useQuery } from "@tanstack/react-query";
import {
  Box,
  Button,
  Container,
  Grid,
  Typography,
} from "@mui/material";

import {
  getWishlist,
  removeFromWishlist,
} from "../../services/wishlistService";

export default function Wishlist() {
  const {
    data: wishlist = [],
    isLoading,
    isError,
    refetch,
  } = useQuery({
    queryKey: ["wishlist"],
    queryFn: getWishlist,
  });

  const handleRemove = async (
    productId: string
  ) => {
    try {
      await removeFromWishlist(productId);

      await refetch();
    } catch (error) {
      console.error(error);
    }
  };

  if (isLoading) {
    return (
      <Container sx={{ py: 10 }}>
        <Typography>Cargando favoritos...</Typography>
      </Container>
    );
  }

  if (isError) {
    return (
      <Container sx={{ py: 10 }}>
        <Typography>No fue posible cargar tus favoritos.</Typography>
      </Container>
    );
  }

  return (
    <Container maxWidth="lg" sx={{ py: 8 }}>
      <Typography
        component="h1"
        sx={{
          fontSize: {
            xs: 34,
            md: 44,
          },
          fontWeight: 400,
          mb: 6,
        }}
      >
        Mis favoritos
      </Typography>

      {wishlist.length === 0 ? (
        <Box sx={{ textAlign: "center", py: 10 }}>
          <Typography sx={{ fontSize: 22, mb: 2 }}>
            Tu lista está vacía
          </Typography>

          <Typography color="text.secondary">
            Guarda tus productos favoritos para encontrarlos fácilmente.
          </Typography>
        </Box>
      ) : (
        <Grid container spacing={3}>
          {wishlist.map((item) => (
            <Grid
              key={item.productId}
              size={{ xs: 12, sm: 6, md: 3 }}
            >
              <Box>
                {item.imageUrl && (
                  <Box
                    component="img"
                    src={item.imageUrl}
                    alt={item.productName ?? "Producto"}
                    sx={{
                      width: "100%",
                      aspectRatio: "1 / 1",
                      objectFit: "cover",
                    }}
                  />
                )}

                <Typography sx={{ mt: 2, fontWeight: 500 }}>
                  {item.productName ?? "Producto"}
                </Typography>

                {item.price !== undefined && (
                  <Typography sx={{ mt: 1 }}>
                    ${item.price?.toLocaleString("es-CO")}
                  </Typography>
                )}

                <Button
                  onClick={() => handleRemove(item.productId)}
                  sx={{ mt: 2, px: 0, color: "#777" }}
                >
                  Eliminar
                </Button>
              </Box>
            </Grid>
          ))}
        </Grid>
      )}
    </Container>
  );
}
