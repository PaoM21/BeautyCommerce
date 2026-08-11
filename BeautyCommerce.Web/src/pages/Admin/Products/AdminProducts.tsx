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
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { useNavigate } from "react-router-dom";

import {
  getAdminProducts,
} from "../../../services/productService";

export default function AdminProducts() {
  const navigate = useNavigate();

  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [searchValue, setSearchValue] = useState("");

  const {
    data,
    isLoading,
    isError,
  } = useQuery({
    queryKey: [
      "admin-products",
      page,
      search,
    ],
    queryFn: () =>
      getAdminProducts(
        page,
        12,
        search
      ),
  });

  const handleSearch = () => {
    setPage(1);
    setSearch(searchValue.trim());
  };

  const handleClearSearch = () => {
    setSearchValue("");
    setSearch("");
    setPage(1);
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

  if (isError || !data) {
    return (
      <Container sx={{ py: 10 }}>
        <Alert severity="error">
          No fue posible cargar los productos.
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

      <Box
        sx={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: {
            xs: "flex-start",
            md: "center",
          },
          gap: 3,
          mb: 5,
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
          Productos
        </Typography>

        <Button
          variant="contained"
          onClick={() =>
            navigate("/admin/productos/nuevo")
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
          Nuevo producto
        </Button>
      </Box>

      <Box
        sx={{
          display: "flex",
          gap: 2,
          mb: 5,
          flexDirection: {
            xs: "column",
            sm: "row",
          },
        }}
      >
        <TextField
          fullWidth
          value={searchValue}
          onChange={(event) =>
            setSearchValue(event.target.value)
          }
          onKeyDown={(event) => {
            if (event.key === "Enter") {
              handleSearch();
            }
          }}
          placeholder="Buscar producto..."
          size="small"
        />

        <Button
          variant="outlined"
          onClick={handleSearch}
          sx={{
            minWidth: 120,
            borderRadius: 0,
            color: "#1f1f1f",
            borderColor: "#1f1f1f",
          }}
        >
          Buscar
        </Button>

        {search && (
          <Button
            onClick={handleClearSearch}
            sx={{
              minWidth: 100,
              color: "#777",
            }}
          >
            Limpiar
          </Button>
        )}
      </Box>

      <Divider sx={{ mb: 2 }} />

      {data.items.length === 0 ? (
        <Box
          sx={{
            py: 10,
            textAlign: "center",
          }}
        >
          <Typography
            sx={{
              color: "#777",
              mb: 2,
            }}
          >
            No se encontraron productos.
          </Typography>

          {search && (
            <Button
              onClick={handleClearSearch}
              sx={{
                color: "#1f1f1f",
              }}
            >
              Limpiar búsqueda
            </Button>
          )}
        </Box>
      ) : (
        <Box>
          {data.items.map((product) => (
            <Box
              key={product.id}
              onClick={() => {
                navigate(`/admin/productos/${product.id}`);
              }}
              sx={{
                display: "flex",
                alignItems: "center",
                gap: 3,
                py: 3,
                cursor: "pointer",
                borderBottom:
                  "1px solid #e5e5e5",
                transition:
                  "background-color 0.2s",
                "&:hover": {
                  backgroundColor: "#faf9f7",
                },
              }}
            >
              <Box
                sx={{
                  width: 90,
                  height: 90,
                  backgroundColor: "#f6f4f1",
                  flexShrink: 0,
                  overflow: "hidden",
                }}
              >
                {product.image && (
                  <Box
                    component="img"
                    src={product.image}
                    alt={product.name}
                    sx={{
                      width: "100%",
                      height: "100%",
                      objectFit: "cover",
                    }}
                  />
                )}
              </Box>

              <Box
                sx={{
                  flex: 1,
                  minWidth: 0,
                }}
              >
                <Typography
                  sx={{
                    fontSize: 16,
                    mb: 0.5,
                  }}
                >
                  {product.name}
                </Typography>

                <Typography
                  sx={{
                    fontSize: 13,
                    color: "#888",
                    mb: 0.5,
                  }}
                >
                  {product.brand}
                </Typography>

                <Typography
                  sx={{
                    fontSize: 13,
                    color: "#999",
                  }}
                >
                  {product.category}
                </Typography>
              </Box>

              <Box
                sx={{
                  textAlign: "right",
                  minWidth: 110,
                }}
              >
                <Typography
                  sx={{
                    fontWeight: 500,
                  }}
                >
                  $
                  {product.price.toLocaleString(
                    "es-CO"
                  )}
                </Typography>

                <Typography
                  sx={{
                    fontSize: 12,
                    color:
                      product.stock > 0
                        ? "#777"
                        : "#c62828",
                    mt: 0.5,
                  }}
                >
                  Stock: {product.stock}
                </Typography>
              </Box>
            </Box>
          ))}
        </Box>
      )}

      {data.totalPages > 1 && (
        <Box
          sx={{
            display: "flex",
            justifyContent: "center",
            alignItems: "center",
            gap: 3,
            mt: 5,
          }}
        >
          <Button
            disabled={page <= 1}
            onClick={() =>
              setPage((current) =>
                Math.max(1, current - 1)
              )
            }
            sx={{
              color: "#1f1f1f",
            }}
          >
            ← Anterior
          </Button>

          <Typography
            sx={{
              fontSize: 13,
              color: "#777",
            }}
          >
            Página {data.page} de{" "}
            {data.totalPages}
          </Typography>

          <Button
            disabled={
              page >= data.totalPages
            }
            onClick={() =>
              setPage((current) =>
                Math.min(
                  data.totalPages,
                  current + 1
                )
              )
            }
            sx={{
              color: "#1f1f1f",
            }}
          >
            Siguiente →
          </Button>
        </Box>
      )}
    </Container>
  );
}
