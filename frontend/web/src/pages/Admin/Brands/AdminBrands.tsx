import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Container,
  Divider,
  Typography,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { useNavigate } from "react-router-dom";

import {
  deleteBrand,
  getBrandDetails,
} from "../../../services/catalogService";
import { getApiErrorMessage } from "../../../services/apiError";

export default function AdminBrands() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [feedback, setFeedback] = useState<
    { type: "success" | "error"; message: string } | null
  >(null);

  const {
    data: brands = [],
    isLoading,
    isError,
    error,
  } = useQuery({
    queryKey: ["admin-brands"],
    queryFn: getBrandDetails,
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteBrand(id),

    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["admin-brands"] });
      setFeedback({ type: "success", message: "Marca eliminada." });
    },

    onError: (err) => {
      setFeedback({
        type: "error",
        message: getApiErrorMessage(err, "No fue posible eliminar la marca."),
      });
    },
  });

  const handleDelete = (event: React.MouseEvent, id: string, name: string) => {
    event.stopPropagation();

    if (!window.confirm(`¿Eliminar la marca "${name}"?`)) {
      return;
    }

    setFeedback(null);
    deleteMutation.mutate(id);
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

  if (isError) {
    return (
      <Container sx={{ py: 10 }}>
        <Alert severity="error">
          {getApiErrorMessage(error, "No fue posible cargar las marcas.")}
        </Alert>
      </Container>
    );
  }

  return (
    <Container maxWidth="lg" sx={{ py: 8 }}>
      <Typography
        sx={{ fontSize: 12, letterSpacing: 3, textTransform: "uppercase", color: "#9a8065", mb: 2 }}
      >
        Administración
      </Typography>

      <Box
        sx={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: { xs: "flex-start", md: "center" },
          gap: 3,
          mb: 5,
          flexDirection: { xs: "column", md: "row" },
        }}
      >
        <Typography component="h1" sx={{ fontSize: { xs: 36, md: 48 }, fontWeight: 400 }}>
          Marcas
        </Typography>

        <Button
          variant="contained"
          onClick={() => navigate("/admin/marcas/nueva")}
          sx={{
            borderRadius: 0,
            px: 4,
            py: 1.5,
            backgroundColor: "#1f1f1f",
            "&:hover": { backgroundColor: "#000" },
          }}
        >
          Nueva marca
        </Button>
      </Box>

      {feedback && (
        <Alert severity={feedback.type} sx={{ mb: 3 }} onClose={() => setFeedback(null)}>
          {feedback.message}
        </Alert>
      )}

      <Divider sx={{ mb: 2 }} />

      {brands.length === 0 ? (
        <Box sx={{ py: 10, textAlign: "center" }}>
          <Typography sx={{ color: "#777" }}>Todavía no hay marcas.</Typography>
        </Box>
      ) : (
        <Box>
          {brands.map((brand) => (
            <Box
              key={brand.id}
              onClick={() => navigate(`/admin/marcas/${brand.id}/editar`)}
              sx={{
                display: "flex",
                alignItems: "center",
                gap: 3,
                py: 3,
                cursor: "pointer",
                borderBottom: "1px solid #e5e5e5",
                transition: "background-color 0.2s",
                "&:hover": { backgroundColor: "#faf9f7" },
              }}
            >
              <Box sx={{ flex: 1, minWidth: 0 }}>
                <Typography sx={{ fontSize: 16, mb: 0.5 }}>{brand.name}</Typography>
                {brand.description && (
                  <Typography sx={{ fontSize: 13, color: "#999" }}>
                    {brand.description}
                  </Typography>
                )}
              </Box>

              <Button
                onClick={(event) => handleDelete(event, brand.id, brand.name)}
                disabled={deleteMutation.isPending && deleteMutation.variables === brand.id}
                sx={{ color: "#c62828", minWidth: "auto" }}
              >
                Eliminar
              </Button>
            </Box>
          ))}
        </Box>
      )}
    </Container>
  );
}
