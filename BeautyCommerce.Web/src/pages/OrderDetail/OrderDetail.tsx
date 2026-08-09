import { useQuery } from "@tanstack/react-query";
import {
  Box,
  CircularProgress,
  Container,
  Divider,
  Typography,
} from "@mui/material";

import { useParams } from "react-router-dom";

import { getOrderById } from "../../services/orderService";

export default function OrderDetail() {
  const { id } = useParams<{
    id: string;
  }>();

  const {
    data: order,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ["order", id],
    queryFn: () => getOrderById(id!),
    enabled: !!id,
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

function OrderTimeline({
  status,
}: {
  status: string;
}) {
  const steps = [
    ["pending", "Pedido recibido"],
    ["paid", "Pago confirmado"],
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
          currentIndex >= 0 && index <= currentIndex;

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
                flexShrink: 0,
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

function SummaryRow({
  label,
  value,
}: {
  label: string;
  value: number;
}) {
  return (
    <Box
      sx={{
        display: "flex",
        justifyContent: "space-between",
      }}
    >
      <Typography
        sx={{
          color: "#777",
          fontSize: 14,
        }}
      >
        {label}
      </Typography>

      <Typography fontSize={14}>
        ${value.toLocaleString("es-CO")}
      </Typography>
    </Box>
  );
}

  if (isError || !order) {
    return (
      <Container sx={{ py: 10 }}>
        <Typography>
          No fue posible encontrar el pedido.
        </Typography>
      </Container>
    );
  }

  return (
    <Container
      maxWidth="md"
      sx={{ py: 8 }}
    >
      <Typography
        component="h1"
        sx={{
          fontSize: 42,
          fontWeight: 400,
          mb: 2,
        }}
      >
        {order.orderNumber ??
          `Pedido ${order.id}`}
      </Typography>

      {order.status && (
        <Typography
          color="text.secondary"
          sx={{ mb: 2 }}
        >
          Estado: {order.status}
        </Typography>
      )}

      <OrderTimeline status={order.status} />

      <Divider sx={{ my: 6 }} />

      <Typography
        sx={{
          fontSize: 22,
          mb: 3,
        }}
      >
        Resumen
      </Typography>

      <Box
        sx={{
          display: "flex",
          flexDirection: "column",
          gap: 2,
        }}
      >
        <SummaryRow
          label="Subtotal"
          value={order.subTotal}
        />

        <SummaryRow
          label="Envío"
          value={order.shippingCost}
        />

        <SummaryRow
          label="Impuestos"
          value={order.tax}
        />

        <Divider />

        <Box
          sx={{
            display: "flex",
            justifyContent: "space-between",
            alignItems: "center",
          }}
        >
          <Typography fontWeight={500}>
            Total
          </Typography>

          <Typography
            fontWeight={600}
            fontSize={18}
          >
            ${order.total.toLocaleString("es-CO")}
          </Typography>
        </Box>
      </Box>

      {order.items.map((item) => (
        <Box
          key={item.productVariantId}
          sx={{
            py: 3,
            display: "flex",
            justifyContent:
              "space-between",
            gap: 3,
          }}
        >
          <Box>
            <Typography>
              {item.productName ??
                "Producto"}
            </Typography>

            <Typography
              variant="body2"
              color="text.secondary"
            >
              Cantidad: {item.quantity}
            </Typography>
          </Box>

          <Typography>
            $
            {item.unitPrice.toLocaleString(
              "es-CO"
            )}
          </Typography>
        </Box>
      ))}

      <Divider />

      <Box
        sx={{
          display: "flex",
          justifyContent:
            "space-between",
          py: 4,
        }}
      >
        <Typography fontWeight={600}>
          Total
        </Typography>

        <Typography fontWeight={600}>
          $
          {order.total.toLocaleString(
            "es-CO"
          )}
        </Typography>
      </Box>
    </Container>
  );
}
