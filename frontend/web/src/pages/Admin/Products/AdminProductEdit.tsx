import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Container,
  FormControl,
  FormControlLabel,
  InputLabel,
  MenuItem,
  Select,
  Switch,
  TextField,
  Typography,
} from "@mui/material";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useNavigate, useParams } from "react-router-dom";
import { useEffect, useState } from "react";

import {
  getProductById,
  updateProduct,
} from "../../../services/productService";

import { getBrands, getCategories } from "../../../services/catalogService";
import { getApiErrorMessage } from "../../../services/apiError";

export default function AdminProductEdit() {
  const { id } = useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [brandId, setBrandId] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [isFeatured, setIsFeatured] = useState(false);

  const [successMessage, setSuccessMessage] =
    useState("");

  const [errorMessage, setErrorMessage] =
    useState("");

  /*
   * PRODUCT
   */

  const {
    data: product,
    isLoading: isLoadingProduct,
    isError: isProductError,
    error: productError,
  } = useQuery({
    queryKey: ["admin-product-edit", id],
    queryFn: () => getProductById(id!),
    enabled: Boolean(id),
  });

  /*
   * BRANDS
   */

  const {
    data: brands = [],
    isLoading: isLoadingBrands,
  } = useQuery({
    queryKey: ["brands"],
    queryFn: getBrands,
  });

  /*
   * CATEGORIES
   */

  const {
    data: categories = [],
    isLoading: isLoadingCategories,
  } = useQuery({
    queryKey: ["categories"],
    queryFn: getCategories,
  });

  /*
   * LOAD PRODUCT INTO FORM
   */

  useEffect(() => {
    if (!product) {
      return;
    }

    setName(product.name ?? "");

    setDescription(
      product.description ??
      ""
    );

    setBrandId(
      product.brandId ?? ""
    );

    setCategoryId(
      product.categoryId ?? ""
    );

    setIsFeatured(
      product.isFeatured ?? false
    );
  }, [product]);

  /*
   * UPDATE
   */

  const mutation = useMutation({
    mutationFn: () =>
      updateProduct(id!, {
        name,
        description,
        brandId,
        categoryId,
        isFeatured,
      }),

    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["admin-products"] });
      await queryClient.invalidateQueries({
        queryKey: ["admin-product", id],
      });
      await queryClient.invalidateQueries({
        queryKey: ["admin-product-edit", id],
      });

      setSuccessMessage(
        "Producto actualizado correctamente."
      );

      setErrorMessage("");

      setTimeout(() => {
        navigate(
          `/admin/productos/${id}`
        );
      }, 800);
    },

    onError: (err) => {
      setErrorMessage(
        getApiErrorMessage(
          err,
          "No fue posible actualizar el producto."
        )
      );

      setSuccessMessage("");
    },
  });

  const handleSubmit = (
    event: React.FormEvent<HTMLFormElement>
  ) => {
    event.preventDefault();

    setSuccessMessage("");
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

    mutation.mutate();
  };

  if (
    isLoadingProduct ||
    isLoadingBrands ||
    isLoadingCategories
  ) {
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

  if (
    isProductError ||
    !product
  ) {
    return (
      <Container sx={{ py: 10 }}>
        <Alert severity="error">
          {getApiErrorMessage(
            productError,
            "No fue posible cargar el producto."
          )}
        </Alert>
      </Container>
    );
  }

  return (
    <Container
      maxWidth="md"
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
          mb: 1,
        }}
      >
        Editar producto
      </Typography>

      <Typography
        sx={{
          color: "#777",
          mb: 6,
        }}
      >
        {product.name}
      </Typography>

      {successMessage && (
        <Alert
          severity="success"
          sx={{ mb: 3 }}
        >
          {successMessage}
        </Alert>
      )}

      {errorMessage && (
        <Alert
          severity="error"
          sx={{ mb: 3 }}
        >
          {errorMessage}
        </Alert>
      )}

      <Box
        component="form"
        onSubmit={handleSubmit}
      >
        <TextField
          fullWidth
          label="Nombre"
          value={name}
          onChange={(event) =>
            setName(event.target.value)
          }
          sx={{ mb: 3 }}
        />

        <TextField
          fullWidth
          multiline
          minRows={5}
          label="Descripción"
          value={description}
          onChange={(event) =>
            setDescription(
              event.target.value
            )
          }
          sx={{ mb: 3 }}
        />

        <FormControl
          fullWidth
          sx={{ mb: 3 }}
        >
          <InputLabel>
            Marca
          </InputLabel>

          <Select
            value={brandId}
            label="Marca"
            onChange={(event) =>
              setBrandId(
                event.target.value
              )
            }
          >
            {brands.map((brand) => (
              <MenuItem
                key={brand.id}
                value={brand.id}
              >
                {brand.name}
              </MenuItem>
            ))}
          </Select>
        </FormControl>

        <FormControl
          fullWidth
          sx={{ mb: 3 }}
        >
          <InputLabel>
            Categoría
          </InputLabel>

          <Select
            value={categoryId}
            label="Categoría"
            onChange={(event) =>
              setCategoryId(
                event.target.value
              )
            }
          >
            {categories.map(
              (category) => (
                <MenuItem
                  key={category.id}
                  value={category.id}
                >
                  {category.name}
                </MenuItem>
              )
            )}
          </Select>
        </FormControl>

        <FormControlLabel
          control={
            <Switch
              checked={isFeatured}
              onChange={(event) =>
                setIsFeatured(
                  event.target.checked
                )
              }
            />
          }
          label="Producto destacado"
          sx={{ mb: 4 }}
        />

        <Box
          sx={{
            display: "flex",
            gap: 2,
            justifyContent: "flex-end",
          }}
        >
          <Button
            type="button"
            variant="outlined"
            onClick={() =>
              navigate(
                `/admin/productos/${id}`
              )
            }
            disabled={
              mutation.isPending
            }
            sx={{
              borderRadius: 0,
              color: "#1f1f1f",
              borderColor: "#1f1f1f",
            }}
          >
            Cancelar
          </Button>

          <Button
            type="submit"
            variant="contained"
            disabled={
              mutation.isPending
            }
            sx={{
              borderRadius: 0,
              px: 4,
              backgroundColor:
                "#1f1f1f",
              "&:hover": {
                backgroundColor:
                  "#000",
              },
            }}
          >
            {mutation.isPending
              ? "Guardando..."
              : "Guardar cambios"}
          </Button>
        </Box>
      </Box>
    </Container>
  );
}
