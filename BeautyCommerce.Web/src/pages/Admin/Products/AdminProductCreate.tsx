import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Container,
  Divider,
  FormControl,
  FormControlLabel,
  InputLabel,
  MenuItem,
  Select,
  TextField,
  Typography,
  Checkbox,
} from "@mui/material";

import { useMutation, useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { useNavigate } from "react-router-dom";

import {
  createProduct,
} from "../../../services/productService";

import {
  getBrands,
} from "../../../services/brandService";

import {
  getCategories,
} from "../../../services/categoryService";

export default function AdminProductCreate() {
  const navigate = useNavigate();

  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [brandId, setBrandId] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [isFeatured, setIsFeatured] = useState(false);

  const [price, setPrice] = useState("");
  const [stock, setStock] = useState("");

  const [images, setImages] = useState<string[]>([""]);

  const [errorMessage, setErrorMessage] = useState("");

  const brandsQuery = useQuery({
    queryKey: ["brands"],
    queryFn: getBrands,
  });

  const categoriesQuery = useQuery({
    queryKey: ["categories"],
    queryFn: getCategories,
  });

  const mutation = useMutation({
    mutationFn: createProduct,

    onSuccess: (id) => {
      navigate(`/admin/productos/${id}`);
    },

    onError: () => {
      setErrorMessage(
        "No fue posible crear el producto."
      );
    },
  });

  const handleImageChange = (
    index: number,
    value: string
  ) => {
    setImages((current) =>
      current.map((image, currentIndex) =>
        currentIndex === index ? value : image
      )
    );
  };

  const addImage = () => {
    setImages((current) => [...current, ""]);
  };

  const removeImage = (index: number) => {
    setImages((current) =>
      current.filter((_, currentIndex) => currentIndex !== index)
    );
  };

  const handleSubmit = (
    event: React.FormEvent<HTMLFormElement>
  ) => {
    event.preventDefault();

    setErrorMessage("");

    if (!name.trim()) {
      setErrorMessage(
        "El nombre del producto es obligatorio."
      );
      return;
    }

    if (!brandId) {
      setErrorMessage(
        "Selecciona una marca."
      );
      return;
    }

    if (!categoryId) {
      setErrorMessage(
        "Selecciona una categoría."
      );
      return;
    }

    const parsedPrice = Number(price);
    const parsedStock = Number(stock);

    if (!Number.isFinite(parsedPrice) || parsedPrice <= 0) {
      setErrorMessage("El precio debe ser mayor que cero.");
      return;
    }

    if (!Number.isInteger(parsedStock) || parsedStock < 0) {
      setErrorMessage(
        "El stock debe ser un número entero mayor o igual a cero."
      );
      return;
    }

    mutation.mutate({
      name: name.trim(),
      description: description.trim(),
      brandId,
      categoryId,
      isFeatured,
      variants: [
        {
          price: parsedPrice,
          stock: parsedStock,
        },
      ],
      images: images.map((image) => image.trim()).filter(Boolean),
    });
  };

  const loadingOptions = brandsQuery.isLoading || categoriesQuery.isLoading;

  if (loadingOptions) {
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

  if (brandsQuery.isError || categoriesQuery.isError) {
    return (
      <Container sx={{ py: 10 }}>
        <Alert severity="error">
          No fue posible cargar las marcas y categorías.
        </Alert>
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
        Nuevo producto
      </Typography>

      {errorMessage && (
        <Alert severity="error" sx={{ mb: 4 }} onClose={() => setErrorMessage("")}> 
          {errorMessage}
        </Alert>
      )}

      <Box component="form" onSubmit={handleSubmit}>
        <Typography sx={{ fontSize: 22, mb: 3 }}>Información del producto</Typography>

        <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", md: "1fr 1fr" }, gap: 3, mb: 5 }}>
          <TextField label="Nombre" value={name} onChange={(event) => setName(event.target.value)} fullWidth required />

          <FormControl fullWidth required>
            <InputLabel>Marca</InputLabel>
            <Select value={brandId} label="Marca" onChange={(event) => setBrandId(event.target.value)}>
              {brandsQuery.data?.map((brand) => (
                <MenuItem key={brand.id} value={brand.id}>{brand.name}</MenuItem>
              ))}
            </Select>
          </FormControl>

          <FormControl fullWidth required>
            <InputLabel>Categoría</InputLabel>
            <Select value={categoryId} label="Categoría" onChange={(event) => setCategoryId(event.target.value)}>
              {categoriesQuery.data?.map((category) => (
                <MenuItem key={category.id} value={category.id}>{category.name}</MenuItem>
              ))}
            </Select>
          </FormControl>

          <FormControlLabel control={<Checkbox checked={isFeatured} onChange={(event) => setIsFeatured(event.target.checked)} />} label="Producto destacado" />
        </Box>

        <TextField label="Descripción" value={description} onChange={(event) => setDescription(event.target.value)} fullWidth multiline minRows={5} sx={{ mb: 6 }} />

        <Divider sx={{ mb: 5 }} />

        <Typography sx={{ fontSize: 22, mb: 3 }}>Variante inicial</Typography>

        <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", sm: "1fr 1fr" }, gap: 3, mb: 6 }}>
          <TextField label="Precio" type="number" value={price} onChange={(event) => setPrice(event.target.value)} inputProps={{ min: 0, step: "0.01" }} required />

          <TextField label="Stock inicial" type="number" value={stock} onChange={(event) => setStock(event.target.value)} inputProps={{ min: 0, step: 1 }} required />
        </Box>

        <Divider sx={{ mb: 5 }} />

        <Typography sx={{ fontSize: 22, mb: 3 }}>Imágenes</Typography>

        <Box sx={{ mb: 5 }}>
          {images.map((image, index) => (
            <Box key={index} sx={{ display: "flex", gap: 2, mb: 2 }}>
              <TextField fullWidth label={`URL de imagen ${index + 1}`} value={image} onChange={(event) => handleImageChange(index, event.target.value)} />

              {images.length > 1 && (
                <Button type="button" onClick={() => removeImage(index)} sx={{ color: "#777" }}>Eliminar</Button>
              )}
            </Box>
          ))}

          <Button type="button" variant="outlined" onClick={addImage} sx={{ borderRadius: 0, color: "#1f1f1f", borderColor: "#1f1f1f" }}>
            + Agregar imagen
          </Button>
        </Box>

        <Divider sx={{ mb: 5 }} />

        <Box sx={{ display: "flex", gap: 2, justifyContent: "flex-end" }}>
          <Button type="button" onClick={() => navigate("/admin/productos")} sx={{ color: "#777" }}>Cancelar</Button>

          <Button type="submit" variant="contained" disabled={mutation.isPending} sx={{ borderRadius: 0, px: 5, py: 1.5, backgroundColor: "#1f1f1f", "&:hover": { backgroundColor: "#000" } }}>
            {mutation.isPending ? "Creando..." : "Crear producto"}
          </Button>
        </Box>
      </Box>
    </Container>
  );
}
