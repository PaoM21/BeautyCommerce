import { useQuery } from "@tanstack/react-query";
import {
  Box,
  Button,
  CircularProgress,
  Container,
  Divider,
  Typography,
} from "@mui/material";
import { Link } from "react-router-dom";
import { useEffect } from "react";

import { getShoppingCart } from "../../services/cartService";
import { useCartStore } from "../../store/cartStore";

export default function Cart() {
  const setItemCount = useCartStore(
    (state) => state.setItemCount
  );

  const {
    data: cart,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ["shopping-cart"],
    queryFn: getShoppingCart,
  });

  useEffect(() => {
    if (!cart) return;
    setItemCount(
      cart.items.reduce((total, item) => total + item.quantity, 0)
    );
  }, [cart, setItemCount]);

  if (isLoading) {
    return (
      <Box
        sx={{
          minHeight: "60vh",
          display: "flex",
          justifyContent: "center",
          alignItems: "center",
        }}
      >
        <CircularProgress />
      </Box>
    );
  }

  if (isError || !cart) {
    return (
      <Container sx={{ py: 10 }}>
        <Typography>
          No fue posible cargar el carrito.
        </Typography>
      </Container>
    );
  }

  if (cart.items.length === 0) {
    return (
      <Container
        maxWidth="md"
        sx={{
          py: 15,
          textAlign: "center",
        }}
      >
        <Typography
          component="h1"
          sx={{
            fontSize: 40,
            fontWeight: 400,
            mb: 2,
          }}
        >
          Tu carrito está vacío
        </Typography>

        <Typography
          sx={{
            color: "#777",
            mb: 4,
          }}
        >
          Descubre nuestros productos y encuentra
          tus favoritos.
        </Typography>

        <Button
          component={Link}
          to="/productos"
          variant="contained"
          sx={{
            backgroundColor: "#1f1f1f",
            borderRadius: 0,
            px: 5,
            py: 1.5,
          }}
        >
          Explorar productos
        </Button>
      </Container>
    );
  }

  return (
    <Container maxWidth="lg" sx={{ py: 8 }}>
      <Typography
        component="h1"
        sx={{
          fontSize: 42,
          fontWeight: 400,
          mb: 6,
        }}
      >
        Tu carrito
      </Typography>

      {cart.items.map((item) => (
        <Box key={item.productVariantId}>
          <Box
            sx={{
              display: "flex",
              gap: 3,
              py: 3,
              alignItems: "center",
            }}
          >
            <Box
              sx={{
                width: 110,
                height: 110,
                backgroundColor: "#f6f4f1",
                flexShrink: 0,
              }}
            >
              {item.imageUrl && (
                <Box
                  component="img"
                  src={item.imageUrl}
                  alt={item.productName}
                  sx={{
                    width: "100%",
                    height: "100%",
                    objectFit: "cover",
                  }}
                />
              )}
            </Box>

            <Box sx={{ flexGrow: 1 }}>
              <Typography
                sx={{
                  fontWeight: 500,
                  mb: 1,
                }}
              >
                {item.productName}
              </Typography>

              {item.color && (
                <Typography
                  variant="body2"
                  color="text.secondary"
                >
                  Color: {item.color}
                </Typography>
              )}

              {item.size && (
                <Typography
                  variant="body2"
                  color="text.secondary"
                >
                  Tamaño: {item.size}
                </Typography>
              )}

              <Typography
                variant="body2"
                sx={{ mt: 1 }}
              >
                Cantidad: {item.quantity}
              </Typography>
            </Box>

            <Typography fontWeight={500}>
              $
              {item.subtotal.toLocaleString(
                "es-CO"
              )}
            </Typography>
          </Box>

          <Divider />
        </Box>
      ))}

      <Box
        sx={{
          display: "flex",
          justifyContent: "flex-end",
          mt: 5,
        }}
      >
        <Box sx={{ width: 320 }}>
          <Box
            sx={{
              display: "flex",
              justifyContent: "space-between",
              mb: 3,
            }}
          >
            <Typography>
              Total
            </Typography>

            <Typography fontWeight={600}>
              $
              {cart.total.toLocaleString(
                "es-CO"
              )}
            </Typography>
          </Box>

          <Button
            component={Link}
            to="/checkout"
            fullWidth
            variant="contained"
            sx={{
              backgroundColor: "#1f1f1f",
              borderRadius: 0,
              py: 1.7,
            }}
          >
            Continuar al checkout
          </Button>
        </Box>
      </Box>
    </Container>
  );
}
