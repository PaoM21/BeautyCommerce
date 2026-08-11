import {
  Box,
  CircularProgress,
  Container,
  Grid,
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
        <Typography>No fue posible cargar el dashboard.</Typography>
      </Container>
    );
  }

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

      <Grid container spacing={3}>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <DashboardCard
            label="Productos"
            value={dashboard.totalProducts.toLocaleString("es-CO")}
          />
        </Grid>

        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <DashboardCard
            label="Pedidos"
            value={dashboard.totalOrders.toLocaleString("es-CO")}
          />
        </Grid>

        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <DashboardCard
            label="Clientes"
            value={dashboard.totalCustomers.toLocaleString("es-CO")}
          />
        </Grid>

        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <DashboardCard
            label="Pedidos pendientes"
            value={dashboard.pendingOrders.toLocaleString("es-CO")}
          />
        </Grid>
      </Grid>

      <Box
        sx={{
          display: "grid",
          gridTemplateColumns: {
            xs: "1fr",
            md: "1fr 1fr",
          },
          gap: 3,
          mt: 3,
        }}
      >
        <SalesCard label="Ventas totales" value={dashboard.totalSales} />

        <SalesCard label="Ventas de este mes" value={dashboard.salesThisMonth} />
      </Box>
    </Container>
  );
}

function DashboardCard({
  label,
  value,
}: {
  label: string;
  value: string;
}) {
  return (
    <Box
      sx={{
        border: "1px solid #e5e1dc",
        p: 4,
        minHeight: 150,
        display: "flex",
        flexDirection: "column",
        justifyContent: "space-between",
      }}
    >
      <Typography
        sx={{
          fontSize: 11,
          letterSpacing: 1.5,
          textTransform: "uppercase",
          color: "#999",
        }}
      >
        {label}
      </Typography>

      <Typography
        sx={{
          fontSize: 34,
          fontWeight: 400,
        }}
      >
        {value}
      </Typography>
    </Box>
  );
}

function SalesCard({
  label,
  value,
}: {
  label: string;
  value: number;
}) {
  return (
    <Box
      sx={{
        border: "1px solid #e5e1dc",
        p: 4,
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
        {label}
      </Typography>

      <Typography
        sx={{
          fontSize: {
            xs: 32,
            md: 40,
          },
          fontWeight: 400,
        }}
      >
        ${value.toLocaleString("es-CO")}
      </Typography>
    </Box>
  );
}
