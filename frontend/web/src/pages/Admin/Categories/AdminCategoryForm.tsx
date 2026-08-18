import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Container,
  MenuItem,
  TextField,
  Typography,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";

import {
  createCategory,
  getCategoryById,
  getCategoryDetails,
  updateCategory,
  type CategoryInput,
} from "../../../services/catalogService";
import { getApiErrorMessage } from "../../../services/apiError";

const EMPTY_FORM: CategoryInput = {
  name: "",
  slug: "",
  description: "",
  imageUrl: "",
  parentCategoryId: null,
};

export default function AdminCategoryForm() {
  const { id } = useParams();
  const isEdit = Boolean(id);
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [form, setForm] = useState<CategoryInput>(EMPTY_FORM);
  const [error, setError] = useState("");

  const { data: allCategories = [] } = useQuery({
    queryKey: ["admin-categories"],
    queryFn: getCategoryDetails,
  });

  const { data: existing, isLoading } = useQuery({
    queryKey: ["admin-category", id],
    queryFn: () => getCategoryById(id!),
    enabled: isEdit,
  });

  useEffect(() => {
    if (existing) {
      setForm({
        name: existing.name,
        slug: existing.slug,
        description: existing.description,
        imageUrl: existing.imageUrl,
        parentCategoryId: existing.parentCategoryId,
      });
    }
  }, [existing]);

  const mutation = useMutation({
    mutationFn: async () => {
      if (isEdit) {
        await updateCategory(id!, form);
      } else {
        await createCategory(form);
      }
    },

    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["admin-categories"] });
      navigate("/admin/categorias");
    },

    onError: (err) => {
      setError(getApiErrorMessage(err, "No fue posible guardar la categoría."));
    },
  });

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();

    if (!form.name || !form.slug) {
      setError("Nombre y slug son obligatorios.");
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
        {isEdit ? "Editar categoría" : "Nueva categoría"}
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
          label="Slug"
          helperText="Usado en la URL, ej. /productos?categoria=rostro"
          value={form.slug}
          onChange={(event) => setForm((f) => ({ ...f, slug: event.target.value }))}
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
          label="URL de imagen (opcional)"
          value={form.imageUrl}
          onChange={(event) => setForm((f) => ({ ...f, imageUrl: event.target.value }))}
          sx={{ mb: 3 }}
        />

        <TextField
          select
          fullWidth
          label="Categoría padre (opcional)"
          value={form.parentCategoryId ?? ""}
          onChange={(event) =>
            setForm((f) => ({
              ...f,
              parentCategoryId: event.target.value || null,
            }))
          }
          sx={{ mb: 4 }}
        >
          <MenuItem value="">Ninguna</MenuItem>
          {allCategories
            .filter((c) => c.id !== id)
            .map((c) => (
              <MenuItem key={c.id} value={c.id}>
                {c.name}
              </MenuItem>
            ))}
        </TextField>

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

          <Button onClick={() => navigate("/admin/categorias")} sx={{ color: "#777" }}>
            Cancelar
          </Button>
        </Box>
      </Box>
    </Container>
  );
}
