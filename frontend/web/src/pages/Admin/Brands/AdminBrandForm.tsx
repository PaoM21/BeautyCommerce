import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Container,
  TextField,
  Typography,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { useNavigate, useParams } from "react-router-dom";

import {
  createBrand,
  getBrandById,
  updateBrand,
  type BrandDetail,
  type BrandInput,
} from "../../../services/catalogService";
import { getApiErrorMessage } from "../../../services/apiError";

const EMPTY_FORM: BrandInput = {
  name: "",
  description: "",
  logoUrl: "",
};

export default function AdminBrandForm() {
  const { id } = useParams();
  const isEdit = Boolean(id);
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [form, setForm] = useState<BrandInput>(EMPTY_FORM);
  const [existing, setExisting] = useState<BrandDetail | null>(null);
  const [error, setError] = useState("");

  const { isLoading } = useQuery({
    queryKey: ["admin-brand", id],
    queryFn: async () => {
      const brand = await getBrandById(id!);
      setExisting(brand);
      setForm({
        name: brand.name,
        description: brand.description,
        logoUrl: brand.logoUrl,
      });
      return brand;
    },
    enabled: isEdit,
  });

  const mutation = useMutation({
    mutationFn: async () => {
      if (isEdit) {
        await updateBrand(id!, existing!, form);
      } else {
        await createBrand(form);
      }
    },

    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["admin-brands"] });
      navigate("/admin/marcas");
    },

    onError: (err) => {
      setError(getApiErrorMessage(err, "No fue posible guardar la marca."));
    },
  });

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();

    if (!form.name) {
      setError("El nombre es obligatorio.");
      return;
    }

    setError("");
    mutation.mutate();
  };

  if (isEdit && isLoading) {
    return (
      <Box sx={{ minHeight: "60vh", display: "flex", justifyContent: "center", alignItems: "center" }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Container maxWidth="sm" sx={{ py: 8 }}>
      <Typography
        sx={{ fontSize: 12, letterSpacing: 3, textTransform: "uppercase", color: "#9a8065", mb: 2 }}
      >
        Administración
      </Typography>

      <Typography component="h1" sx={{ fontSize: { xs: 32, md: 40 }, fontWeight: 400, mb: 6 }}>
        {isEdit ? "Editar marca" : "Nueva marca"}
      </Typography>

      <Box component="form" onSubmit={handleSubmit}>
        <TextField
          fullWidth
          label="Nombre"
          value={form.name}
          onChange={(event) => setForm((f) => ({ ...f, name: event.target.value }))}
          sx={{ mb: 3 }}
        />

        <TextField
          fullWidth
          multiline
          minRows={2}
          label="Descripción"
          value={form.description}
          onChange={(event) => setForm((f) => ({ ...f, description: event.target.value }))}
          sx={{ mb: 3 }}
        />

        <TextField
          fullWidth
          label="URL del logo (opcional)"
          value={form.logoUrl}
          onChange={(event) => setForm((f) => ({ ...f, logoUrl: event.target.value }))}
          sx={{ mb: 4 }}
        />

        {error && (
          <Alert severity="error" sx={{ mb: 3 }}>
            {error}
          </Alert>
        )}

        <Box sx={{ display: "flex", gap: 2 }}>
          <Button
            type="submit"
            variant="contained"
            disabled={mutation.isPending}
            sx={{
              borderRadius: 0,
              px: 4,
              py: 1.5,
              backgroundColor: "#1f1f1f",
              "&:hover": { backgroundColor: "#000" },
            }}
          >
            {mutation.isPending ? "Guardando..." : "Guardar"}
          </Button>

          <Button onClick={() => navigate("/admin/marcas")} sx={{ color: "#777" }}>
            Cancelar
          </Button>
        </Box>
      </Box>
    </Container>
  );
}
