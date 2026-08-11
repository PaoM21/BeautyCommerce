import {
  Box,
  CircularProgress,
  Container,
  Typography,
} from "@mui/material";
import { useQuery } from "@tanstack/react-query";

import { getDashboard } from "../../../services/dashboardService";

export default function Dashboard() {
  const {
    data: dashboard,
    isLoading,
    isError,
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
          No fue posible cargar el dashboard.
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
    </Container>
  );
}
