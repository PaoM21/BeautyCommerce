import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";

import {
  Box,
  CircularProgress,
  Container,
  Divider,
  Typography,
} from "@mui/material";

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
        <Typography>No fue posible cargar tus pedidos.</Typography>
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
        Mis pedidos
      </Typography>

      {orders.length === 0 ? (
        <Typography color="text.secondary">Todavía no tienes pedidos.</Typography>
      ) : (
        <Box>
          {orders.map((order) => {
            const date = order.orderDate;

            return (
              <Box key={order.id}>
                <Box
                  sx={{
                    py: 4,
                    display: "flex",
                    justifyContent: "space-between",
                    alignItems: "flex-start",
                    gap: 3,
                    flexWrap: "wrap",
                  }}
                >
                  <Box>
                    <Typography
                      component={Link}
                      to={`/pedidos/${order.id}`}
                      sx={{
                        fontWeight: 500,
                        mb: 1,
                        color: "#1f1f1f",
                        textDecoration: "none",
                        display: "inline-block",
                      }}
                    >
                      {order.orderNumber ?? `Pedido ${order.id}`}
                    </Typography>

                    {date && (
                      <Typography variant="body2" color="text.secondary">
                        {new Date(date).toLocaleDateString("es-CO")}
                      </Typography>
                    )}

                    {order.status && (
                      <Typography variant="body2" sx={{ mt: 1 }}>
                        Estado: {order.status}
                      </Typography>
                    )}
                  </Box>

                  <Typography sx={{ fontWeight: 600 }}>${order.total.toLocaleString("es-CO")}</Typography>
                </Box>

                <Divider />
              </Box>
            );
          })}
        </Box>
      )}
    </Container>
  );
}
