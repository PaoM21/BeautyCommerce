import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Container,
  Divider,
  TextField,
  Typography,
} from "@mui/material";
import { useMutation, useQuery } from "@tanstack/react-query";
import { useParams, useNavigate } from "react-router-dom";
import { useState } from "react";

import {
  getProductById,
  updateVariantStock,
} from "../../../services/productService";

export default function AdminProductDetail() {
  const { id } = useParams();
  const [quantities, setQuantities] = useState<Record<string, string>>({});
  const [reasons, setReasons] = useState<Record<string, string>>({});
  const [successMessage, setSuccessMessage] = useState("");
  const [errorMessage, setErrorMessage] = useState("");

  const {
    data: product,
    isLoading,
    isError,
    refetch,
  } = useQuery({
    queryKey: ["admin-product", id],
    queryFn: () => getProductById(id!),
    enabled: Boolean(id),
  });

  const navigate = useNavigate();

  const mutation = useMutation({
    mutationFn: ({
      productVariantId,
      quantity,
      reason,
    }: {
      productVariantId: string;
      quantity: number;
      reason: string;
    }) =>
      updateVariantStock(
        productVariantId,
        quantity,
        reason
      ),

    onSuccess: async () => {
      await refetch();

      setQuantities({});
      setReasons({});

      setSuccessMessage("Stock actualizado correctamente.");
      setErrorMessage("");
    },

    onError: () => {
      setErrorMessage(
        "No fue posible actualizar el stock."
      );
      setSuccessMessage("");
    },
  });

  const handleQuantityChange = (
    variantId: string,
    value: string
  ) => {
    setQuantities((current) => ({
      ...current,
      [variantId]: value,
    }));
  };

  const handleReasonChange = (
    variantId: string,
    value: string
  ) => {
    setReasons((current) => ({
      ...current,
      [variantId]: value,
    }));
  };

  const handleUpdateStock = (
    variantId: string
  ) => {
    const rawQuantity = quantities[variantId];

    if (!rawQuantity) {
      setErrorMessage(
        "Ingresa una cantidad para actualizar el stock."
      );
      setSuccessMessage("");
      return;
    }

    const quantity = Number(rawQuantity);

    if (!Number.isInteger(quantity) || quantity === 0) {
      setErrorMessage(
        "La cantidad debe ser un número entero diferente de cero."
      );
      setSuccessMessage("");
      return;
    }

    mutation.mutate({
      productVariantId: variantId,
      quantity,
      reason:
        reasons[variantId]?.trim() ||
        "Ajuste manual de inventario",
    });
  };

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

  if (isError || !product) {
    return (
      <Container sx={{ py: 10 }}>
        <Alert severity="error">
          No fue posible encontrar el producto.
        </Alert>
      </Container>
    );
  }

  const images = product.images ?? [];
  const variants = product.variants ?? [];

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

      <Box
        sx={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: {
            xs: "flex-start",
            md: "center",
          },
          gap: 3,
          mb: 6,
          flexDirection: {
            xs: "column",
            md: "row",
          },
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
          {product.name}
        </Typography>

        <Button
          variant="contained"
          onClick={() =>
            navigate(`/admin/productos/${product.id}/editar`)
          }
          sx={{
            borderRadius: 0,
            px: 4,
            py: 1.5,
            backgroundColor: "#1f1f1f",
            "&:hover": {
              backgroundColor: "#000",
            },
          }}
        >
          Editar producto
        </Button>
      </Box>

      <Box
        sx={{
          display: "grid",
          gridTemplateColumns: {
            xs: "1fr",
            md: "1fr 1fr",
          },
          gap: 4,
          mb: 6,
        }}
      >
        <Info
          label="Marca"
          value={product.brand?.name ?? "Sin marca"}
        />

        <Info
          label="Categoría"
          value={product.category?.name ?? "Sin categoría"}
        />
      </Box>

      <Divider sx={{ mb: 5 }} />

      <Typography
        sx={{
          fontSize: 22,
          mb: 2,
        }}
      >
        Descripción
      </Typography>

      <Typography
        sx={{
          color: "#666",
          lineHeight: 1.8,
          mb: 6,
        }}
      >
        {product.description || "Sin descripción."}
      </Typography>

      <Divider sx={{ mb: 5 }} />

      <Typography
        sx={{
          fontSize: 22,
          mb: 3,
        }}
      >
        Imágenes
      </Typography>

      {images.length === 0 ? (
        <Typography
          sx={{
            color: "#999",
            mb: 6,
          }}
        >
          Este producto no tiene imágenes.
        </Typography>
      ) : (
        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: {
              xs: "1fr",
              sm: "repeat(2, 1fr)",
              md: "repeat(3, 1fr)",
            },
            gap: 3,
            mb: 6,
          }}
        >
          {images.map((image) => (
            <Box
              key={image.id}
              sx={{
                height: 240,
                backgroundColor: "#f6f4f1",
                overflow: "hidden",
              }}
            >
              <Box
                component="img"
                src={image.imageUrl ?? ""}
                alt={product.name}
                sx={{
                  width: "100%",
                  height: "100%",
                  objectFit: "cover",
                }}
              />
            </Box>
          ))}
        </Box>
      )}

      <Divider sx={{ mb: 5 }} />

      <Typography
        sx={{
          fontSize: 22,
          mb: 3,
        }}
      >
        Variantes
      </Typography>

      {successMessage && (
        <Alert
          severity="success"
          sx={{ mb: 3 }}
          onClose={() => setSuccessMessage("")}
        >
          {successMessage}
        </Alert>
      )}

      {errorMessage && (
        <Alert
          severity="error"
          sx={{ mb: 3 }}
          onClose={() => setErrorMessage("")}
        >
          {errorMessage}
        </Alert>
      )}

      {variants.length === 0 ? (
        <Typography sx={{ color: "#999" }}>
          Este producto no tiene variantes.
        </Typography>
      ) : (
        <Box>
          {variants.map((variant) => (
            <Box key={variant.id}>
              <Box
                sx={{
                  display: "grid",
                  gridTemplateColumns: {
                    xs: "1fr",
                    sm: "repeat(3, 1fr)",
                  },
                  gap: 3,
                  py: 3,
                }}
              >
                <Info
                  label="SKU"
                  value={variant.sku || "Sin SKU"}
                />

                <Info
                  label="Precio"
                  value={`$${variant.price.toLocaleString(
                    "es-CO"
                  )}`}
                />

                <Info
                  label="Stock actual"
                  value={String(variant.stock)}
                />
              </Box>

              <Box
                sx={{
                  display: "grid",
                  gridTemplateColumns: {
                    xs: "1fr",
                    sm: "1fr 2fr auto",
                  },
                  gap: 2,
                  alignItems: "center",
                  pb: 3,
                }}
              >
                <TextField
                  label="Cantidad"
                  type="number"
                  value={quantities[variant.id] ?? ""}
                  onChange={(event) =>
                    handleQuantityChange(
                      variant.id,
                      event.target.value
                    )
                  }
                  helperText="Usa + para agregar y - para descontar"
                  size="small"
                  slotProps={{
                    htmlInput: {
                      step: 1,
                    },
                  }}
                />

                <TextField
                  label="Motivo"
                  value={reasons[variant.id] ?? ""}
                  onChange={(event) =>
                    handleReasonChange(
                      variant.id,
                      event.target.value
                    )
                  }
                  placeholder="Ej. Reposición de inventario"
                  size="small"
                />

                <Button
                  variant="contained"
                  onClick={() =>
                    handleUpdateStock(variant.id)
                  }
                  disabled={mutation.isPending}
                  sx={{
                    borderRadius: 0,
                    minHeight: 40,
                    px: 3,
                    backgroundColor: "#1f1f1f",
                    "&:hover": {
                      backgroundColor: "#000",
                    },
                  }}
                >
                  {mutation.isPending
                    ? "Actualizando..."
                    : "Actualizar stock"}
                </Button>
              </Box>

              <Divider />
            </Box>
          ))}
        </Box>
      )}
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

      <Typography>
        {value}
      </Typography>
    </Box>
  );
}
