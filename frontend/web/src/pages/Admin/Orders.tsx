import {
  Alert,
  Box,
  CircularProgress,
  Container,
  Divider,
  MenuItem,
  Select,
  Typography,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

import {
  getAdminOrders,
  updateOrderStatus,
} from "../../services/orderService";

const statuses = [
  {
    value: "Pending",
    label: "Pendiente",
  },
  {
    value: "Processing",
    label: "Preparando pedido",
  },
  {
    value: "Shipped",
    label: "Enviado",
  },
  {
    value: "Delivered",
    label: "Entregado",
  },
  {
    value: "Cancelled",
    label: "Cancelado",
  },
];

export default function AdminOrders() {
  const queryClient = useQueryClient();

  const {
    data: orders = [],
    isLoading,
    isError,
  } = useQuery({
    queryKey: ["admin-orders"],
    queryFn: getAdminOrders,
  });

  const mutation = useMutation({
    mutationFn: ({ orderId, status }: { orderId: string; status: string }) =>
      updateOrderStatus(orderId, status),

    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: ["admin-orders"],
      });
    },
  });

  if (isLoading) {
    return (
      <Box
        sx={{
          minHeight: "60vh",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
        }}
      >
        <CircularProgress />
      </Box>
    );
  }

  if (isError) {
    return (
      <Container sx={{ py: 10 }}>
        <Alert severity="error">No fue posible cargar los pedidos.</Alert>
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
            xs: 36,
            md: 48,
          },
          fontWeight: 400,
          mb: 6,
        }}
      >
        Pedidos
      </Typography>

      {orders.length === 0 ? (
        <Typography>No hay pedidos registrados.</Typography>
      ) : (
        <Box>
          {orders.map((order) => (
            <Box key={order.id}>
              <Box
                sx={{
                  display: "grid",
                  gridTemplateColumns: {
                    xs: "1fr",
                    md: "1fr 1fr 1fr 1fr auto",
                  },
                  gap: 3,
                  alignItems: "center",
                  py: 3,
                }}
              >
                <Box>
                  <Typography
                    sx={{
                      fontSize: 11,
                      color: "#999",
                      letterSpacing: 1,
                    }}
                  >
                    PEDIDO
                  </Typography>

                  <Typography>{order.orderNumber}</Typography>

                  {order.customerName && (
                    <Typography
                      sx={{
                        fontSize: 13,
                        color: "#777",
                        mt: 0.5,
                      }}
                    >
                      {order.customerName}
                    </Typography>
                  )}

                  {order.customerEmail && (
                    <Typography
                      sx={{
                        fontSize: 12,
                        color: "#aaa",
                      }}
                    >
                      {order.customerEmail}
                    </Typography>
                  )}
                </Box>

                <Box>
                  <Typography
                    sx={{
                      fontSize: 11,
                      color: "#999",
                      letterSpacing: 1,
                    }}
                  >
                    FECHA
                  </Typography>

                  <Typography>
                    {new Date(order.orderDate).toLocaleDateString("es-CO")}
                  </Typography>
                </Box>

                <Box>
                  <Typography
                    sx={{
                      fontSize: 11,
                      color: "#999",
                      letterSpacing: 1,
                    }}
                  >
                    TOTAL
                  </Typography>

                  <Typography sx={{ fontWeight: 500 }}>
                    ${order.total.toLocaleString("es-CO")}
                  </Typography>
                </Box>

                <Box>
                  <Typography
                    sx={{
                      fontSize: 11,
                      color: "#999",
                      letterSpacing: 1,
                      mb: 1,
                    }}
                  >
                    ESTADO
                  </Typography>

                  <Select
                    size="small"
                    value={order.status}
                    onChange={(event) => {
                      mutation.mutate({
                        orderId: order.id,
                        status: event.target.value,
                      });
                    }}
                    disabled={mutation.isPending}
                    sx={{
                      minWidth: 180,
                    }}
                  >
                    {statuses.map((status) => (
                      <MenuItem key={status.value} value={status.value}>
                        {status.label}
                      </MenuItem>
                    ))}
                  </Select>
                </Box>
              </Box>

              <Divider />
            </Box>
          ))}
        </Box>
      )}

      {mutation.isError && (
        <Alert severity="error" sx={{ mt: 4 }}>
          No fue posible actualizar el estado.
        </Alert>
      )}
    </Container>
  );
}
