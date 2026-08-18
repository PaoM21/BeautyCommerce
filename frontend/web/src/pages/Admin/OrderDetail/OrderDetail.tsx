import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Container,
  Divider,
  MenuItem,
  Select,
  TextField,
  Typography,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useNavigate, useParams } from "react-router-dom";
import { useEffect, useState } from "react";

import {
  getAdminOrderById,
  getValidNextStatuses,
  updateOrderShipping,
  updateOrderStatus,
} from "../../../services/orderService";
import { getApiErrorMessage } from "../../../services/apiError";

const CARRIER_OPTIONS = ["Envía", "Interrapidísimo", "Otra"];

export default function AdminOrderDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [status, setStatus] = useState("");
  const [carrier, setCarrier] = useState("");
  const [trackingNumber, setTrackingNumber] = useState("");

  const {
    data: order,
    isLoading,
    isError,
    error,
  } = useQuery({
    queryKey: ["admin-order", id],
    queryFn: () => getAdminOrderById(id!),
    enabled: Boolean(id),
  });

  useEffect(() => {
    if (order) {
      setCarrier(order.carrier ?? "");
      setTrackingNumber(order.trackingNumber ?? "");
    }
  }, [order]);

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

  const shippingMutation = useMutation({
    mutationFn: () =>
      updateOrderShipping(id!, carrier, trackingNumber),

    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ["admin-order", id],
      });
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
          {getApiErrorMessage(
            error,
            "No fue posible encontrar el pedido."
          )}
        </Typography>
      </Container>
    );
  }

  const validNextStatuses = getValidNextStatuses(order.status);
  const mutationErrorMessage =
    (mutation.error as { response?: { data?: { detail?: string } } } | null)
      ?.response?.data?.detail ?? "No fue posible actualizar el estado.";

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
        Dirección de envío
      </Typography>

      <Box
        sx={{
          display: "grid",
          gridTemplateColumns: {
            xs: "1fr",
            md: "repeat(3, 1fr)",
          },
          gap: 4,
          mb: 5,
        }}
      >
        <Info label="Destinatario" value={order.shippingRecipientName || "—"} />
        <Info label="Teléfono" value={order.shippingPhone || "—"} />
        <Info
          label="Dirección"
          value={
            order.shippingAddressLine
              ? `${order.shippingAddressLine}, ${order.shippingCity} (${order.shippingDepartment})`
              : "—"
          }
        />
      </Box>

      <Typography
        sx={{
          fontSize: 22,
          mb: 3,
        }}
      >
        Transportadora y guía
      </Typography>

      <Box
        sx={{
          display: "flex",
          gap: 2,
          flexDirection: {
            xs: "column",
            sm: "row",
          },
          mb: 3,
        }}
      >
        <Select
          value={carrier}
          onChange={(event) => setCarrier(event.target.value)}
          displayEmpty
          sx={{ minWidth: 220 }}
        >
          <MenuItem value="" disabled>
            Selecciona la transportadora
          </MenuItem>

          {CARRIER_OPTIONS.map((option) => (
            <MenuItem key={option} value={option}>
              {option}
            </MenuItem>
          ))}
        </Select>

        <TextField
          label="Número de guía"
          value={trackingNumber}
          onChange={(event) => setTrackingNumber(event.target.value)}
          sx={{ minWidth: 220 }}
        />

        <Button
          variant="contained"
          disabled={
            shippingMutation.isPending || !carrier || !trackingNumber
          }
          onClick={() => shippingMutation.mutate()}
          sx={{
            borderRadius: 0,
            px: 4,
            backgroundColor: "#1f1f1f",
            "&:hover": { backgroundColor: "#000" },
          }}
        >
          {shippingMutation.isPending ? "Guardando..." : "Guardar envío"}
        </Button>
      </Box>

      {order.shippedAt && (
        <Typography sx={{ fontSize: 13, color: "#777", mb: 2 }}>
          Despachado el{" "}
          {new Date(order.shippedAt).toLocaleDateString("es-CO")}
        </Typography>
      )}

      {shippingMutation.isError && (
        <Alert severity="error" sx={{ mb: 4 }}>
          No fue posible guardar la información de envío.
        </Alert>
      )}

      {shippingMutation.isSuccess && (
        <Alert severity="success" sx={{ mb: 4 }}>
          Información de envío guardada correctamente.
        </Alert>
      )}

      <Divider sx={{ mb: 5 }} />

      <Typography
        sx={{
          fontSize: 22,
          mb: 3,
        }}
      >
        Actualizar estado
      </Typography>

      {validNextStatuses.length > 0 ? (
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
            value={status}
            onChange={(event) => setStatus(event.target.value)}
            displayEmpty
            sx={{
              minWidth: 220,
            }}
          >
            <MenuItem value="" disabled>
              Selecciona un nuevo estado
            </MenuItem>

            {validNextStatuses.map((nextStatus) => (
              <MenuItem key={nextStatus} value={nextStatus}>
                {getStatusLabel(nextStatus)}
              </MenuItem>
            ))}
          </Select>

          <Button
            variant="contained"
            disabled={mutation.isPending || !status}
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
      ) : (
        <Typography color="text.secondary" sx={{ mb: 5 }}>
          Este pedido está en un estado final y no admite más cambios.
        </Typography>
      )}

      {mutation.isError && (
        <Alert severity="error" sx={{ mb: 4 }}>
          {mutationErrorMessage}
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
        Resumen económico
      </Typography>

      <Box
        sx={{
          display: "grid",
          gridTemplateColumns: {
            xs: "1fr",
            sm: "repeat(4, 1fr)",
          },
          gap: 4,
          mb: 6,
        }}
      >
        <Info
          label="Subtotal"
          value={`$${order.subTotal.toLocaleString("es-CO")}`}
        />

        <Info
          label="Envío"
          value={`$${order.shippingCost.toLocaleString("es-CO")}`}
        />

        <Info
          label="Impuestos"
          value={`$${order.tax.toLocaleString("es-CO")}`}
        />

        <Info
          label="Total"
          value={`$${order.total.toLocaleString("es-CO")}`}
        />
      </Box>

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
