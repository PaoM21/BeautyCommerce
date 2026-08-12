import {
  Box,
  CircularProgress,
  Container,
  Divider,
  Typography,
} from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";

import { getAdminOrders } from "../../../services/orderService";

export default function AdminOrders() {
  const {
    data: orders = [],
    isLoading,
    isError,
  } = useQuery({
    queryKey: ["admin-orders"],
    queryFn: getAdminOrders,
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
          No fue posible cargar los pedidos.
        </Typography>
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
        Pedidos
      </Typography>

      {orders.length === 0 ? (
        <Box
          sx={{
            py: 10,
            textAlign: "center",
          }}
        >
          <Typography
            sx={{
              fontSize: 24,
              mb: 1,
            }}
          >
            No hay pedidos
          </Typography>

          <Typography color="text.secondary">
            Los pedidos realizados por los clientes
            aparecerán aquí.
          </Typography>
        </Box>
      ) : (
        <Box>
          {/* ENCABEZADO */}

          <Box
            sx={{
              display: {
                xs: "none",
                md: "grid",
              },
              gridTemplateColumns:
                "1.2fr 1fr 1fr 1fr auto",
              gap: 3,
              px: 2,
              pb: 2,
            }}
          >
            <Header text="Pedido" />
            <Header text="Fecha" />
            <Header text="Estado" />
            <Header text="Total" />
            <Box />
          </Box>

          <Divider />

          {orders.map((order) => (
            <Box key={order.id}>
              <Box
                component={Link}
                to={`/admin/pedidos/${order.id}`}
                sx={{
                  display: "grid",
                  gridTemplateColumns: {
                    xs: "1fr",
                    md: "1.2fr 1fr 1fr 1fr auto",
                  },
                  gap: {
                    xs: 2,
                    md: 3,
                  },
                  alignItems: "center",
                  px: 2,
                  py: 3,
                  textDecoration: "none",
                  color: "inherit",
                  transition: "background-color .2s",
                  "&:hover": {
                    backgroundColor: "#faf9f7",
                  },
                }}
              >
                <Box>
                  <MobileLabel text="PEDIDO" />

                  <Typography sx={{ fontWeight: 500 }}>
                    {order.orderNumber}
                  </Typography>
                </Box>

                <Box>
                  <MobileLabel text="FECHA" />

                  <Typography>
                    {new Date(
                      order.orderDate
                    ).toLocaleDateString("es-CO")}
                  </Typography>
                </Box>

                <Box>
                  <MobileLabel text="ESTADO" />

                  <Status status={order.status} />
                </Box>

                <Box>
                  <MobileLabel text="TOTAL" />

                  <Typography sx={{ fontWeight: 500 }}>
                    $
                    {order.total.toLocaleString(
                      "es-CO"
                    )}
                  </Typography>
                </Box>

                <Typography
                  sx={{
                    fontSize: 13,
                    color: "#777",
                  }}
                >
                  Ver pedido →
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

function Header({
  text,
}: {
  text: string;
}) {
  return (
    <Typography
      sx={{
        fontSize: 11,
        letterSpacing: 1.5,
        textTransform: "uppercase",
        color: "#999",
      }}
    >
      {text}
    </Typography>
  );
}

function MobileLabel({
  text,
}: {
  text: string;
}) {
  return (
    <Typography
      sx={{
        display: {
          xs: "block",
          md: "none",
        },
        fontSize: 10,
        letterSpacing: 1.5,
        color: "#999",
        mb: 0.5,
      }}
    >
      {text}
    </Typography>
  );
}

function Status({
  status,
}: {
  status: string;
}) {
  return (
    <Box
      sx={{
        display: "inline-flex",
        alignItems: "center",
        gap: 1,
      }}
    >
      <Box
        sx={{
          width: 7,
          height: 7,
          borderRadius: "50%",
          backgroundColor:
            status.toLowerCase() === "delivered"
              ? "#6b8068"
              : "#9a8065",
        }}
      />

      <Typography>
        {getStatusLabel(status)}
      </Typography>
    </Box>
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
