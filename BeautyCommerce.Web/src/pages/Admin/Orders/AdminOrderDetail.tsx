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

function getAvailableStatuses(status: string) {
  switch (status.toLowerCase()) {
    case "pending":
      return [
        { value: "Paid", label: "Pagado" },
        { value: "Cancelled", label: "Cancelado" },
      ];

    case "paid":
      return [
        { value: "Processing", label: "Preparando pedido" },
        { value: "Cancelled", label: "Cancelado" },
      ];

    case "processing":
      return [{ value: "Shipped", label: "Enviado" }];

    case "shipped":
      return [{ value: "Delivered", label: "Entregado" }];

    default:
      return [];
  }
}

export default function AdminOrderDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [newStatus, setNewStatus] = useState("");

  const {
    data: order,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ["order", id],
    queryFn: () => getAdminOrderById(id!),
    enabled: Boolean(id),
  });

  const mutation = useMutation({
    mutationFn: () =>
      updateOrderStatus(order!.id, newStatus),

    onSuccess: async () => {
      // Update the cached order immediately so the UI reflects the new status
      queryClient.setQueryData(["order", id], (old: any) => {
        if (!old) return old;
        return { ...old, status: newStatus };
      });

      await queryClient.invalidateQueries({
        queryKey: ["order", id],
      });

      await queryClient.invalidateQueries({
        queryKey: ["admin-orders"],
      });

      setNewStatus("");
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
      <Button
        onClick={() => navigate("/admin/pedidos")}
        sx={{
          color: "#777",
          mb: 4,
          px: 0,
          "&:hover": {
            backgroundColor: "transparent",
            color: "#222",
          },
        }}
      >
        ← Volver a pedidos
      </Button>

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

      <Box
        sx={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: {
            xs: "flex-start",
            md: "center",
          },
          flexDirection: {
            xs: "column",
            md: "row",
          },
          gap: 3,
          mb: 6,
        }}
      >
        <Typography
          component="h1"
          sx={{
            fontSize: {
              xs: 36,
              md: 48,
            },
            fontWeight: 400,
          }}
        >
          {order.orderNumber}
        </Typography>

        <StatusBadge status={order.status} />
      </Box>

      <Box
        sx={{
          display: "grid",
          gridTemplateColumns: {
            xs: "1fr",
            md: "1fr 1fr 1fr",
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
          label="Total"
          value={`$${order.total.toLocaleString(
            "es-CO"
          )}`}
        />

        <Info
          label="Transacción"
          value={order.transactionId ?? "Sin transacción"}
        />
      </Box>

      <Divider sx={{ mb: 5 }} />

      {/* CAMBIO DE ESTADO */}

      <Box
        sx={{
          border: "1px solid #e5e1dc",
          p: {
            xs: 3,
            md: 4,
          },
          mb: 6,
        }}
      >
        <Typography
          sx={{
            fontSize: 22,
            mb: 1,
          }}
        >
          Actualizar estado
        </Typography>

        <Typography
          sx={{
            fontSize: 14,
            color: "#777",
            mb: 3,
          }}
        >
          El sistema solo permitirá cambios de estado
          válidos.
        </Typography>

        <Box
          sx={{
            display: "flex",
            gap: 2,
            flexDirection: {
              xs: "column",
              sm: "row",
            },
          }}
        >
          <Select
            value={newStatus}
            onChange={(event) =>
              setNewStatus(event.target.value)
            }
            displayEmpty
            size="small"
            sx={{
              minWidth: {
                sm: 240,
              },
            }}
          >
            <MenuItem value="">Seleccionar nuevo estado</MenuItem>

            {getAvailableStatuses(order.status).map((option) => (
              <MenuItem key={option.value} value={option.value}>
                {option.label}
              </MenuItem>
            ))}
          </Select>

          <Button
            variant="contained"
            disabled={
              !newStatus ||
              newStatus.toLowerCase() ===
                order.status.toLowerCase() ||
              mutation.isPending
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
            {mutation.isPending ? "Actualizando..." : "Actualizar estado"}
          </Button>
        </Box>

        {mutation.isError && (
          <Alert severity="error" sx={{ mt: 3 }}>
            No fue posible actualizar el estado. El cambio puede no estar permitido.
          </Alert>
        )}

        {mutation.isSuccess && (
          <Alert severity="success" sx={{ mt: 3 }}>
            Estado actualizado correctamente.
          </Alert>
        )}
      </Box>

      {/* PRODUCTOS */}

      <Typography
        sx={{
          fontSize: 24,
          mb: 3,
        }}
      >
        Productos
      </Typography>

      <Box>
        {order.items.map((item) => (
          <Box key={item.id}>
            <Box
              sx={{
                display: "flex",
                gap: 3,
                py: 3,
                alignItems: "center",
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
                    }}
                  >
                    {item.color && `Color: ${item.color}`}

                    {item.color && item.size && " · "}

                    {item.size && `Talla: ${item.size}`}
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

              <Box sx={{ textAlign: "right" }}>
                <Typography sx={{ fontWeight: 500 }}>
                  ${item.subtotal.toLocaleString("es-CO")}
                </Typography>

                <Typography
                  sx={{
                    fontSize: 12,
                    color: "#999",
                    mt: 0.5,
                  }}
                >
                  ${item.unitPrice.toLocaleString("es-CO")} c/u
                </Typography>
              </Box>
            </Box>

            <Divider />
          </Box>
        ))}
      </Box>

      {/* TOTAL */}

      <Box
        sx={{
          display: "flex",
          justifyContent: "flex-end",
          mt: 5,
        }}
      >
        <Box
          sx={{
            width: {
              xs: "100%",
              sm: 300,
            },
          }}
        >
          <Box
            sx={{
              display: "flex",
              justifyContent: "space-between",
            }}
          >
            <Typography sx={{ fontWeight: 500 }}>Total</Typography>

            <Typography sx={{ fontSize: 22, fontWeight: 500 }}>
              ${order.total.toLocaleString("es-CO")}
            </Typography>
          </Box>
        </Box>
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

function StatusBadge({
  status,
}: {
  status: string;
}) {
  return (
    <Box
      sx={{
        border: "1px solid #e5e1dc",
        px: 2,
        py: 1,
      }}
    >
      <Typography
        sx={{
          fontSize: 13,
        }}
      >
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
