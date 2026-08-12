import {
  Box,
  Button,
  Container,
  Typography,
} from "@mui/material";
import { useSearchParams } from "react-router-dom";
import { Link } from "react-router-dom";

export default function CheckoutSuccess() {
  const [searchParams] = useSearchParams();

  const orderId = searchParams.get("orderId");

  return (
    <Container
      maxWidth="md"
      sx={{
        py: 14,
        textAlign: "center",
      }}
    >
      <Typography
        sx={{
          fontSize: 12,
          letterSpacing: 3,
          textTransform: "uppercase",
          color: "#9a8065",
          mb: 3,
        }}
      >
        Compra realizada
      </Typography>

      <Typography
        component="h1"
        sx={{
          fontSize: {
            xs: 38,
            md: 52,
          },
          fontWeight: 400,
          mb: 3,
        }}
      >
        Gracias por tu compra
      </Typography>

      <Typography
        sx={{
          color: "#777",
          maxWidth: 500,
          mx: "auto",
          lineHeight: 1.8,
          mb: 2,
        }}
      >
        Hemos recibido tu pedido correctamente.
        Estamos preparando todo para ti.
      </Typography>

      {orderId && (
        <Typography
          sx={{
            fontSize: 13,
            color: "#999",
            mb: 5,
          }}
        >
          Número de pedido: {orderId}
        </Typography>
      )}

      <Box
        sx={{
          display: "flex",
          justifyContent: "center",
          gap: 2,
          flexWrap: "wrap",
        }}
      >
        {orderId && (
          <Button
            component={Link}
            to={`/mi-cuenta/pedidos/${orderId}`}
            variant="contained"
            sx={{
              borderRadius: 0,
              px: 4,
              py: 1.5,
              backgroundColor: "#1f1f1f",
              "&:hover": {
                backgroundColor: "#000",
              },
            }}
          >
            Ver mi pedido
          </Button>
        )}

        <Button
          component={Link}
          to="/productos"
          variant="outlined"
          sx={{
            borderRadius: 0,
            px: 4,
            py: 1.5,
            borderColor: "#1f1f1f",
            color: "#1f1f1f",
          }}
        >
          Seguir comprando
        </Button>
      </Box>
    </Container>
  );
}
