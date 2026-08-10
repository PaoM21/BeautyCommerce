import {
  Alert,
  Box,
  CircularProgress,
  Container,
  Divider,
  Typography,
} from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { useParams } from "react-router-dom";

import { getOrderById } from "../../services/orderService";

export default function OrderDetail() {
  const { id } = useParams();

  const {
    data: order,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ["order", id],
    queryFn: () => getOrderById(id!),
    enabled: Boolean(id),
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

  if (isError || !order) {
    return (
      <Container sx={{ py: 10 }}>
        <Alert severity="error">
          No fue posible encontrar el pedido.
        </Alert>
      </Container>
    );
  }

  return (
    <Container
      maxWidth="md"
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
        Mi pedido
      </Typography>

      <Typography
        component="h1"
        sx={{
          fontSize: {
            xs: 36,
            md: 48,
          },
          fontWeight: 400,
          mb: 6,
        }}
      >
        {order.orderNumber}
      </Typography>

      <Box
        sx={{
          display: "grid",
          gridTemplateColumns: {
            xs: "1fr",
            sm: "1fr 1fr",
          },
          gap: 4,
          mb: 6,
        }}
      >
        <Info
          label="Fecha"
          value={new Date(
            order.orderDate
          ).toLocaleDateString("es-CO")}
        />

        <Info
          label="Estado"
          value={getStatusLabel(order.status)}
        />

        <Info
          label="Total"
          value={`$${order.total.toLocaleString(
            "es-CO"
          )}`}
        />

        <Info
          label="Transacción"
          value={
            order.transactionId ?? "Pendiente"
          }
        />
      </Box>

      <Divider sx={{ mb: 5 }} />

      <Typography
        sx={{
          fontSize: 22,
          mb: 3,
        }}
      >
        Estado del pedido
      </Typography>

      <OrderTimeline status={order.status} />

      <Divider sx={{ my: 6 }} />

      <Typography
        sx={{
          fontSize: 22,
          mb: 3,
        }}
      >
        Productos
      </Typography>

      <Box>
        {order.items.map((item) => (
          <Box
            key={item.id}
            sx={{
              display: "flex",
              gap: 3,
              py: 3,
            }}
          >
            <Box
              sx={{
                width: 100,
                height: 100,
                backgroundColor: "#f6f4f1",
                flexShrink: 0,
                overflow: "hidden",
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

            <Box sx={{ flex: 1 }}>
              <Typography
                sx={{
                  fontSize: 16,
                  mb: 0.5,
                }}
              >
                {item.productName}
              </Typography>

              {(item.color || item.size) && (
                <Typography
                  sx={{
                    fontSize: 13,
                    color: "#888",
                    mb: 1,
                  }}
                >
                  {item.color &&
                    `Color: ${item.color}`}

                  {item.color && item.size && " · "}

                  {item.size &&
                    `Talla: ${item.size}`}
                </Typography>
              )}

              <Typography
                sx={{
                  fontSize: 13,
                  color: "#888",
                }}
              >
                Cantidad: {item.quantity}
              </Typography>
            </Box>

            <Box
              sx={{
                textAlign: "right",
              }}
            >
              <Typography sx={{ fontWeight: 500 }}>
                $
                {item.subtotal.toLocaleString(
                  "es-CO"
                )}
              </Typography>

              <Typography
                sx={{
                  fontSize: 12,
                  color: "#999",
                  mt: 0.5,
                }}
              >
                ${item.unitPrice.toLocaleString(
                  "es-CO"
                )} c/u
              </Typography>
            </Box>
          </Box>
        ))}
      </Box>
    </Container>
  );
}

function Info({
  label,
  value,
}: {
  label: string;
  value: string;
}) {
  return (
    <Box>
      <Typography
        sx={{
          fontSize: 11,
          letterSpacing: 1.5,
          color: "#999",
          textTransform: "uppercase",
          mb: 1,
        }}
      >
        {label}
      </Typography>

      <Typography>{value}</Typography>
    </Box>
  );
}

function OrderTimeline({
  status,
}: {
  status: string;
}) {
  const steps = [
    ["pending", "Pedido recibido"],
    ["processing", "Preparando pedido"],
    ["shipped", "Pedido enviado"],
    ["delivered", "Pedido entregado"],
  ];

  const currentIndex = steps.findIndex(
    ([key]) => key === status.toLowerCase()
  );

  return (
    <Box>
      {steps.map(([key, label], index) => {
        const completed =
          index <= currentIndex;

        return (
          <Box
            key={key}
            sx={{
              display: "flex",
              gap: 2,
              mb: 3,
            }}
          >
            <Box
              sx={{
                width: 12,
                height: 12,
                borderRadius: "50%",
                backgroundColor: completed
                  ? "#1f1f1f"
                  : "#ddd",
                mt: 0.5,
              }}
            />

            <Typography
              sx={{
                color: completed
                  ? "#222"
                  : "#aaa",
              }}
            >
              {label}
            </Typography>
          </Box>
        );
      })}
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
