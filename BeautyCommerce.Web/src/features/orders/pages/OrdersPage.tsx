import { useQuery } from "@tanstack/react-query";
import {
  Box,
  CircularProgress,
  Container,
  Typography,
} from "@mui/material";
import { Link } from "react-router-dom";

import { getMyOrders } from "../../../services/orderService";

export default function OrdersPage() {
  const {
    data: orders = [],
    isLoading,
    isError,
  } = useQuery({
    queryKey: ["orders"],
    queryFn: getMyOrders,
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
          No fue posible cargar tus pedidos.
        </Typography>
      </Container>
    );
  }

  return (
    <Container maxWidth="lg" sx={{ py: 8 }}>
      <Typography
        sx={{
          fontSize: 13,
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
          fontSize: { xs: 36, md: 48 },
          fontWeight: 400,
          mb: 6,
        }}
      >
        Mis pedidos
      </Typography>

      {orders.length === 0 ? (
        <Box sx={{ py: 8 }}>
          <Typography
            sx={{
              fontSize: 18,
              mb: 3,
            }}
          >
            Todavía no tienes pedidos.
          </Typography>

          <Box
            component={Link}
            to="/productos"
            sx={{
              display: "inline-block",
              px: 4,
              py: 1.5,
              backgroundColor: "#111",
              color: "#fff",
              textDecoration: "none",
              fontSize: 13,
              letterSpacing: 1,
              textTransform: "uppercase",
            }}
          >
            Explorar productos
          </Box>
        </Box>
      ) : (
        <Box>
          {orders.map((order) => (
            <Box
              key={order.id}
              component={Link}
              to={`/pedidos/${order.id}`}
              sx={{
                display: "block",
                textDecoration: "none",
                color: "inherit",
                borderTop: "1px solid #e5e5e5",
                py: 3,
                transition: "opacity .2s ease",
                "&:hover": {
                  opacity: 0.7,
                },
              }}
            >
              <Box
                sx={{
                  display: "flex",
                  justifyContent: "space-between",
                  alignItems: {
                    xs: "flex-start",
                    md: "center",
                  },
                  gap: 3,
                  flexDirection: {
                    xs: "column",
                    md: "row",
                  },
                }}
              >
                <Box>
                  <Typography
                    sx={{
                      fontSize: 12,
                      color: "#999",
                      letterSpacing: 1,
                      textTransform: "uppercase",
                      mb: 0.5,
                    }}
                  >
                    Pedido
                  </Typography>

                  <Typography sx={{ fontSize: 18 }}>
                    {order.orderNumber}
                  </Typography>

                  <Typography
                    sx={{
                      fontSize: 13,
                      color: "#777",
                      mt: 0.5,
                    }}
                  >
                    {new Date(
                      order.orderDate
                    ).toLocaleDateString("es-CO")}
                  </Typography>
                </Box>

                <Box
                  sx={{
                    textAlign: {
                      xs: "left",
                      md: "right",
                    },
                  }}
                >
                  <Typography
                    sx={{
                      fontSize: 12,
                      textTransform: "uppercase",
                      letterSpacing: 1,
                      color: "#9a8065",
                    }}
                  >
                    {order.status}
                  </Typography>

                  <Typography
                    sx={{
                      fontSize: 18,
                      mt: 0.5,
                      fontWeight: 500,
                    }}
                  >
                    $
                    {order.total.toLocaleString(
                      "es-CO"
                    )}
                  </Typography>
                </Box>
              </Box>
            </Box>
          ))}
        </Box>
      )}
    </Container>
  );
}
