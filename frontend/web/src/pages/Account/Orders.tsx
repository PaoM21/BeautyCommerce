import {
  Box,
  CircularProgress,
  Container,
  Divider,
  Typography,
} from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";

import { getOrders } from "../../services/orderService";

export default function Orders() {
  const {
    data: orders = [],
    isLoading,
    isError,
  } = useQuery({
    queryKey: ["orders"],
    queryFn: getOrders,
  });

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

  if (isError) {
    return (
      <Container sx={{ py: 10 }}>
        <Typography>
          No fue posible cargar tus pedidos.
        </Typography>
      </Container>
    );
  }

  return (
    <Container
      maxWidth="lg"
      sx={{
        py: 8,
      }}
    >
      <Typography
        sx={{
          fontSize: 12,
          letterSpacing: 3,
          textTransform: "uppercase",
          color: "#9a8065",
          mb: 2,
        }}
      >
        Mi cuenta
      </Typography>

      <Typography
        component="h1"
        sx={{
          fontSize: {
            xs: 38,
            md: 48,
          },
          fontWeight: 400,
          mb: 6,
        }}
      >
        Mis pedidos
      </Typography>

      {orders.length === 0 ? (
        <Box
          sx={{
            py: 8,
            textAlign: "center",
          }}
        >
          <Typography
            sx={{
              fontSize: 24,
              mb: 2,
            }}
          >
            Todavía no tienes pedidos
          </Typography>

          <Typography color="text.secondary">
            Cuando realices tu primera compra,
            aparecerá aquí.
          </Typography>
        </Box>
      ) : (
        <Box>
          {orders.map((order) => (
            <Box key={order.id}>
              <Box
                component={Link}
                to={`/mi-cuenta/pedidos/${order.id}`}
                sx={{
                  display: "grid",
                  gridTemplateColumns: {
                    xs: "1fr",
                    md: "1fr 1fr 1fr auto",
                  },
                  gap: 3,
                  alignItems: "center",
                  py: 4,
                  textDecoration: "none",
                  color: "inherit",
                  transition: "opacity .2s",
                  "&:hover": {
                    opacity: 0.7,
                  },
                }}
              >
                <Box>
                  <Typography
                    sx={{
                      fontSize: 12,
                      color: "#999",
                      mb: 0.5,
                    }}
                  >
                    PEDIDO
                  </Typography>

                  <Typography>
                    {order.orderNumber}
                  </Typography>
                </Box>

                <Box>
                  <Typography
                    sx={{
                      fontSize: 12,
                      color: "#999",
                      mb: 0.5,
                    }}
                  >
                    FECHA
                  </Typography>

                  <Typography>
                    {new Date(
                      order.orderDate
                    ).toLocaleDateString("es-CO")}
                  </Typography>
                </Box>

                <Box>
                  <Typography
                    sx={{
                      fontSize: 12,
                      color: "#999",
                      mb: 0.5,
                    }}
                  >
                    ESTADO
                  </Typography>

                  <Typography>
                    {getStatusLabel(order.status)}
                  </Typography>
                </Box>

                <Typography sx={{ fontWeight: 500 }}>
                  $
                  {order.total.toLocaleString(
                    "es-CO"
                  )}
                </Typography>
              </Box>

              <Divider />
            </Box>
          ))}
        </Box>
      )}
    </Container>
  );
}

function getStatusLabel(status: string) {
  switch (status.toLowerCase()) {
    case "pending":
      return "Pendiente";

    case "paid":
      return "Pagado";

    case "processing":
      return "Preparando pedido";

    case "shipped":
      return "Enviado";

    case "delivered":
      return "Entregado";

    case "cancelled":
      return "Cancelado";

    default:
      return status;
  }
}
