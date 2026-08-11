import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Container,
  Divider,
  MenuItem,
  Select,
  Typography,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useNavigate, useParams } from "react-router-dom";
import { useState } from "react";

import {
  getAdminOrderById,
  updateOrderStatus,
} from "../../../services/orderService";

export default function AdminOrderDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [status, setStatus] = useState("");

  const {
    data: order,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ["admin-order", id],
    queryFn: () => getAdminOrderById(id!),
    enabled: Boolean(id),
  });

  const mutation = useMutation({
    mutationFn: () =>
      updateOrderStatus(id!, status),

    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ["admin-order", id],
      });

      await queryClient.invalidateQueries({
        queryKey: ["admin-orders"],
      });

      setStatus("");
    },
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
        <Typography>
          No fue posible encontrar el pedido.
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
        Administración
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
            md: "repeat(4, 1fr)",
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
          value={`$${order.total.toLocaleString("es-CO")}`}
        />

        <Info
          label="Transacción"
          value={order.transactionId ?? "Pendiente"}
        />
      </Box>

      <Divider sx={{ mb: 5 }} />

      <Typography
        sx={{
          fontSize: 22,
          mb: 3,
        }}
      >
        Actualizar estado
      </Typography>

      <Box
        sx={{
          display: "flex",
          gap: 2,
          flexDirection: {
            xs: "column",
            sm: "row",
          },
          mb: 5,
        }}
      >
        <Select
          value={status || order.status}
          onChange={(event) => setStatus(event.target.value)}
          sx={{
            minWidth: 220,
          }}
        >
          <MenuItem value="Pending">
            Pendiente
          </MenuItem>

          <MenuItem value="Paid">
            Pagado
          </MenuItem>

          <MenuItem value="Processing">
            Preparando pedido
          </MenuItem>

          <MenuItem value="Shipped">
            Enviado
          </MenuItem>

          <MenuItem value="Delivered">
            Entregado
          </MenuItem>

          <MenuItem value="Cancelled">
            Cancelado
          </MenuItem>
        </Select>

        <Button
          variant="contained"
          disabled={
            mutation.isPending ||
            !status ||
            status.toLowerCase() ===
              order.status.toLowerCase()
          }
          onClick={() => mutation.mutate()}
          sx={{
            borderRadius: 0,
            px: 4,
            backgroundColor: "#1f1f1f",
            "&:hover": {
              backgroundColor: "#000",
            },
          }}
        >
          {mutation.isPending
            ? "Actualizando..."
            : "Actualizar estado"}
        </Button>
      </Box>

      {mutation.isError && (
        <Alert severity="error" sx={{ mb: 4 }}>
          No fue posible actualizar el estado.
        </Alert>
      )}

      {mutation.isSuccess && (
        <Alert severity="success" sx={{ mb: 4 }}>
          Estado actualizado correctamente.
        </Alert>
      )}

      <Divider sx={{ mb: 5 }} />

      <Typography
        sx={{
          fontSize: 22,
          mb: 3,
        }}
      >
        Productos
      </Typography>

      {order.items.map((item) => (
        <Box key={item.id}>
          <Box
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
              <Typography>
                {item.productName}
              </Typography>

              {(item.color || item.size) && (
                <Typography
                  sx={{
                    fontSize: 13,
                    color: "#888",
                    mt: 0.5,
                  }}
                >
                  {item.color &&
                    `Color: ${item.color}`}
                  {item.color &&
                    item.size &&
                    " · "}
                  {item.size &&
                    `Talla: ${item.size}`}
                </Typography>
              )}

              <Typography
                sx={{
                  fontSize: 13,
                  color: "#888",
                  mt: 1,
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
              <Typography fontWeight={500}>
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
                $
                {item.unitPrice.toLocaleString(
                  "es-CO"
                )}{" "}
                c/u
              </Typography>
            </Box>
          </Box>

          <Divider />
        </Box>
      ))}

      <Button
        onClick={() => navigate("/admin/pedidos")}
        sx={{
          mt: 5,
          color: "#1f1f1f",
        }}
      >
        ← Volver a pedidos
      </Button>
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
