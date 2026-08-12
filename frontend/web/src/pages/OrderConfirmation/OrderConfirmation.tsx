import {
  Box,
  Button,
  Container,
  Typography,
} from "@mui/material";

import {
  Link,
  useParams,
} from "react-router-dom";

export default function OrderConfirmation() {
  const { id } = useParams<{ id: string }>();

  return (
    <Container maxWidth="md">
      <Box
        sx={{
          py: 15,
          textAlign: "center",
        }}
      >
        <Typography
          sx={{
            fontSize: 13,
            letterSpacing: 3,
            textTransform: "uppercase",
            color: "#9a8065",
            mb: 3,
          }}
        >
          Beauty Commerce
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
          ¡Gracias por tu compra!
        </Typography>

        <Typography
          sx={{
            color: "#666",
            lineHeight: 1.8,
            mb: 2,
          }}
        >
          Tu pedido ha sido creado
          correctamente.
        </Typography>

        {id && (
          <Typography
            sx={{
              fontSize: 14,
              color: "#999",
              mb: 5,
              wordBreak: "break-all",
            }}
          >
            Pedido: {id}
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
          <Button
            component={Link}
            to="/productos"
            variant="outlined"
            sx={{
              borderRadius: 0,
              px: 4,
              py: 1.5,
            }}
          >
            Seguir comprando
          </Button>

          <Button
            component={Link}
            to="/pedidos"
            variant="contained"
            sx={{
              backgroundColor: "#1f1f1f",
              borderRadius: 0,
              px: 4,
              py: 1.5,
            }}
          >
            Ver mis pedidos
          </Button>
        </Box>
      </Box>
    </Container>
  );
}
