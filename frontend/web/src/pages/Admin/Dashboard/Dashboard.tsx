import {
  Box,
  CircularProgress,
  Container,
  Divider,
  Typography,
} from "@mui/material";
import { useQuery } from "@tanstack/react-query";

import { getDashboard } from "../../../services/dashboardService";
import { getApiErrorMessage } from "../../../services/apiError";

export default function Dashboard() {
  const {
    data: dashboard,
    isLoading,
    isError,
    error,
  } = useQuery({
    queryKey: ["dashboard"],
    queryFn: getDashboard,
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

  if (isError || !dashboard) {
    return (
      <Container sx={{ py: 10 }}>
        <Typography>
          {getApiErrorMessage(
            error,
            "No fue posible cargar el dashboard."
          )}
        </Typography>
      </Container>
    );
  }

  const cards = [
    {
      label: "Productos",
      value: dashboard.totalProducts,
    },
    {
      label: "Pedidos",
      value: dashboard.totalOrders,
    },
    {
      label: "Clientes",
      value: dashboard.totalCustomers,
    },
    {
      label: "Pedidos pendientes",
      value: dashboard.pendingOrders,
    },
    {
      label: "Ventas totales",
      value: `$${dashboard.totalSales.toLocaleString("es-CO")}`,
    },
    {
      label: "Ventas del mes",
      value: `$${dashboard.salesThisMonth.toLocaleString("es-CO")}`,
    },
    {
      label: "Stock bajo",
      value: dashboard.lowStockProducts,
    },
    {
      label: "Agotados",
      value: dashboard.outOfStockProducts,
    },
  ];

  return (
    <Container
      maxWidth="xl"
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
        Administración
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
        Dashboard
      </Typography>

      <Box
        sx={{
          display: "grid",
          gridTemplateColumns: {
            xs: "1fr",
            sm: "1fr 1fr",
            lg: "repeat(3, 1fr)",
          },
          gap: 3,
        }}
      >
        {cards.map((card) => (
          <Box
            key={card.label}
            sx={{
              border: "1px solid #e5e1dc",
              p: {
                xs: 3,
                md: 4,
              },
              minHeight: 150,
            }}
          >
            <Typography
              sx={{
                fontSize: 11,
                letterSpacing: 1.5,
                textTransform: "uppercase",
                color: "#999",
                mb: 2,
              }}
            >
              {card.label}
            </Typography>

            <Typography
              sx={{
                fontSize: {
                  xs: 28,
                  md: 34,
                },
                fontWeight: 400,
              }}
            >
              {card.value}
            </Typography>
          </Box>
        ))}
      </Box>

      <Divider sx={{ my: 6 }} />

      <Typography sx={{ fontSize: 22, mb: 3 }}>
        Últimos pedidos
      </Typography>

      {dashboard.lastOrders.length === 0 ? (
        <Box sx={{ border: "1px solid #e5e1dc", p: 5, textAlign: "center" }}>
          <Typography color="text.secondary">
            Todavía no hay pedidos.
          </Typography>
        </Box>
      ) : (
        <Box>
          {dashboard.lastOrders.map((order) => (
            <Box
              key={order.id}
              sx={{
                display: "grid",
                gridTemplateColumns: {
                  xs: "1fr",
                  md: "1.5fr 1fr 1fr",
                },
                gap: 2,
                alignItems: "center",
                py: 3,
                px: 2,
                borderBottom: "1px solid #e5e1dc",
              }}
            >
              <Box>
                <Typography sx={{ fontWeight: 500 }}>
                  {order.orderNumber}
                </Typography>

                <Typography sx={{ fontSize: 13, color: "#999", mt: 0.5 }}>
                  {new Date(order.createdAt).toLocaleDateString("es-CO")}
                </Typography>
              </Box>

              <Typography sx={{ fontSize: 13, color: "#777" }}>
                Cliente: {order.customer}
              </Typography>

              <Typography
                sx={{
                  fontWeight: 500,
                  textAlign: { xs: "left", md: "right" },
                }}
              >
                ${order.total.toLocaleString("es-CO")}
              </Typography>
            </Box>
          ))}
        </Box>
      )}

      {dashboard.salesByMonth.length > 0 && (
        <>
          <Divider sx={{ my: 6 }} />

          <Typography sx={{ fontSize: 22, mb: 3 }}>
            Ventas por mes
          </Typography>

          <Box>
            {dashboard.salesByMonth.map((month) => (
              <Box
                key={month.month}
                sx={{
                  display: "flex",
                  justifyContent: "space-between",
                  alignItems: "center",
                  py: 2,
                  borderBottom: "1px solid #e5e1dc",
                }}
              >
                <Typography>{month.month}</Typography>

                <Typography sx={{ fontWeight: 500 }}>
                  ${month.total.toLocaleString("es-CO")}
                </Typography>
              </Box>
            ))}
          </Box>
        </>
      )}
    </Container>
  );
}
