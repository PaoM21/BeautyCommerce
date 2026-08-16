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
import {
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { useMemo, useState } from "react";

import {
  getInventory,
  updateInventoryStock,
  type InventoryItem,
} from "../../../services/inventoryService";
import { getApiErrorMessage } from "../../../services/apiError";

export default function AdminInventory() {
  const queryClient = useQueryClient();

  const [search, setSearch] = useState("");
  const [filter, setFilter] = useState("all");

  const [selectedItem, setSelectedItem] = useState<InventoryItem | null>(
    null
  );

  const [quantity, setQuantity] = useState("");
  const [reason, setReason] = useState("");
  const [isEntry, setIsEntry] = useState(true);

  const {
    data: inventory = [],
    isLoading,
    isError,
    error,
  } = useQuery({
    queryKey: ["admin-inventory"],
    queryFn: getInventory,
  });

  const mutation = useMutation({
    mutationFn: updateInventoryStock,

    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ["admin-inventory"],
      });

      if (selectedItem) {
        await queryClient.invalidateQueries({
          queryKey: ["admin-product", selectedItem.productId],
        });
        await queryClient.invalidateQueries({
          queryKey: ["admin-product-edit", selectedItem.productId],
        });
      }

      await queryClient.invalidateQueries({ queryKey: ["admin-products"] });

      setSelectedItem(null);
      setQuantity("");
      setReason("");
    },
  });

  const filteredInventory = useMemo(() => {
    const value = search.trim().toLowerCase();

    return inventory.filter((item) => {
      const matchesSearch =
        !value ||
        item.productName.toLowerCase().includes(value) ||
        item.sku.toLowerCase().includes(value);

      const matchesFilter =
        filter === "all" ||
        (filter === "low" && item.stockStatus === "LowStock") ||
        (filter === "out" && item.stockStatus === "OutOfStock");

      return matchesSearch && matchesFilter;
    });
  }, [inventory, search, filter]);

  const totalVariants = inventory.length;

  const lowStock = inventory.filter(
    (item) => item.stockStatus === "LowStock"
  ).length;

  const outOfStock = inventory.filter(
    (item) => item.stockStatus === "OutOfStock"
  ).length;

  const handleSubmit = () => {
    if (!selectedItem) {
      return;
    }

    const parsedQuantity = Number(quantity);

    if (!Number.isInteger(parsedQuantity) || parsedQuantity <= 0) {
      return;
    }

    if (!reason.trim()) {
      return;
    }

    mutation.mutate({
      productVariantId: selectedItem.productVariantId,
      quantity: parsedQuantity,
      isEntry,
      reason: reason.trim(),
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

  if (isError) {
    return (
      <Container sx={{ py: 10 }}>
        <Alert severity="error">
          {getApiErrorMessage(
            error,
            "No fue posible cargar el inventario."
          )}
        </Alert>
      </Container>
    );
  }

  return (
    <Container maxWidth="xl" sx={{ py: 8 }}>
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
          fontSize: { xs: 38, md: 48 },
          fontWeight: 400,
          mb: 6,
        }}
      >
        Inventario
      </Typography>

      <Box
        sx={{
          display: "grid",
          gridTemplateColumns: { xs: "1fr", sm: "repeat(3, 1fr)" },
          gap: 3,
          mb: 6,
        }}
      >
        <SummaryCard label="Variantes" value={totalVariants} />
        <SummaryCard label="Stock bajo" value={lowStock} />
        <SummaryCard label="Agotados" value={outOfStock} />
      </Box>

      <Box
        sx={{
          display: "flex",
          gap: 2,
          mb: 5,
          flexDirection: { xs: "column", md: "row" },
        }}
      >
        <TextField
          fullWidth
          size="small"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder="Buscar producto o SKU..."
        />

        <Select
          size="small"
          value={filter}
          onChange={(event) => setFilter(event.target.value)}
          sx={{ minWidth: 180 }}
        >
          <MenuItem value="all">Todos</MenuItem>
          <MenuItem value="low">Stock bajo</MenuItem>
          <MenuItem value="out">Agotados</MenuItem>
        </Select>
      </Box>

      <Divider sx={{ mb: 2 }} />

      {filteredInventory.length === 0 ? (
        <Box sx={{ py: 10, textAlign: "center" }}>
          <Typography sx={{ fontSize: 22, mb: 1 }}>
            No se encontraron variantes
          </Typography>

          <Typography color="text.secondary">
            Intenta cambiar la búsqueda o los filtros.
          </Typography>
        </Box>
      ) : (
        <Box>
          <Box
            sx={{
              display: { xs: "none", md: "grid" },
              gridTemplateColumns: "2fr 1fr 1fr 1fr 1fr auto",
              gap: 3,
              px: 2,
              pb: 2,
            }}
          >
            <Header text="Producto" />
            <Header text="SKU" />
            <Header text="Variante" />
            <Header text="Precio" />
            <Header text="Stock" />
            <Box />
          </Box>

          <Divider />

          {filteredInventory.map((item) => (
            <Box key={item.productVariantId}>
              <Box
                sx={{
                  display: "grid",
                  gridTemplateColumns: {
                    xs: "1fr",
                    md: "2fr 1fr 1fr 1fr 1fr auto",
                  },
                  gap: { xs: 2, md: 3 },
                  alignItems: "center",
                  px: 2,
                  py: 3,
                }}
              >
                <Box>
                  <MobileLabel text="PRODUCTO" />

                  <Typography sx={{ fontWeight: 500 }}>
                    {item.productName}
                  </Typography>
                </Box>

                <Box>
                  <MobileLabel text="SKU" />

                  <Typography sx={{ fontSize: 13, color: "#777" }}>
                    {item.sku || "—"}
                  </Typography>
                </Box>

                <Box>
                  <MobileLabel text="VARIANTE" />

                  <Typography sx={{ fontSize: 13 }}>
                    {[item.color, item.size].filter(Boolean).join(" · ") ||
                      "Única"}
                  </Typography>
                </Box>

                <Box>
                  <MobileLabel text="PRECIO" />

                  <Typography>
                    ${item.price.toLocaleString("es-CO")}
                  </Typography>
                </Box>

                <Box>
                  <MobileLabel text="STOCK" />

                  <StockStatus item={item} />
                </Box>

                <Button
                  variant="outlined"
                  onClick={() => {
                    setSelectedItem(item);
                    setQuantity("");
                    setReason("");
                    setIsEntry(true);
                  }}
                  sx={{
                    borderRadius: 0,
                    color: "#1f1f1f",
                    borderColor: "#1f1f1f",
                  }}
                >
                  Ajustar
                </Button>
              </Box>

              <Divider />
            </Box>
          ))}
        </Box>
      )}

      {selectedItem && (
        <Box
          sx={{
            position: "fixed",
            inset: 0,
            zIndex: 1300,
            backgroundColor: "rgba(0,0,0,.35)",
            display: "flex",
            justifyContent: "center",
            alignItems: "center",
            p: 2,
          }}
          onClick={() => setSelectedItem(null)}
        >
          <Box
            onClick={(event) => event.stopPropagation()}
            sx={{
              width: "100%",
              maxWidth: 500,
              backgroundColor: "#fff",
              p: { xs: 3, md: 5 },
            }}
          >
            <Typography sx={{ fontSize: 24, mb: 1 }}>
              Ajustar inventario
            </Typography>

            <Typography sx={{ color: "#777", mb: 4 }}>
              {selectedItem.productName}
            </Typography>

            <Box sx={{ mb: 3 }}>
              <Typography
                sx={{
                  fontSize: 11,
                  letterSpacing: 1.5,
                  textTransform: "uppercase",
                  color: "#999",
                  mb: 1,
                }}
              >
                Stock actual
              </Typography>

              <Typography sx={{ fontSize: 28 }}>
                {selectedItem.stock}
              </Typography>
            </Box>

            <Select
              fullWidth
              value={isEntry ? "entry" : "exit"}
              onChange={(event) =>
                setIsEntry(event.target.value === "entry")
              }
              sx={{ mb: 3 }}
            >
              <MenuItem value="entry">Entrada de mercancía</MenuItem>
              <MenuItem value="exit">Salida de mercancía</MenuItem>
            </Select>

            <TextField
              fullWidth
              type="number"
              label="Cantidad"
              value={quantity}
              onChange={(event) => setQuantity(event.target.value)}
              sx={{ mb: 3 }}
              slotProps={{
                htmlInput: { min: 1 },
              }}
            />

            <TextField
              fullWidth
              multiline
              minRows={3}
              label="Motivo"
              value={reason}
              onChange={(event) => setReason(event.target.value)}
              placeholder="Ej: Compra a proveedor"
              sx={{ mb: 4 }}
            />

            {mutation.isError && (
              <Alert severity="error" sx={{ mb: 3 }}>
                {getApiErrorMessage(
                  mutation.error,
                  "No fue posible actualizar el inventario."
                )}
              </Alert>
            )}

            <Box sx={{ display: "flex", gap: 2, justifyContent: "flex-end" }}>
              <Button
                onClick={() => setSelectedItem(null)}
                sx={{ color: "#777" }}
              >
                Cancelar
              </Button>

              <Button
                variant="contained"
                onClick={handleSubmit}
                disabled={
                  mutation.isPending || !quantity || !reason.trim()
                }
                sx={{
                  borderRadius: 0,
                  backgroundColor: "#1f1f1f",
                  "&:hover": { backgroundColor: "#000" },
                }}
              >
                {mutation.isPending ? "Guardando..." : "Guardar ajuste"}
              </Button>
            </Box>
          </Box>
        </Box>
      )}
    </Container>
  );
}

function SummaryCard({ label, value }: { label: string; value: number }) {
  return (
    <Box sx={{ border: "1px solid #e5e1dc", p: 4, minHeight: 140 }}>
      <Typography
        sx={{
          fontSize: 11,
          letterSpacing: 1.5,
          textTransform: "uppercase",
          color: "#999",
          mb: 3,
        }}
      >
        {label}
      </Typography>

      <Typography sx={{ fontSize: 34 }}>
        {value.toLocaleString("es-CO")}
      </Typography>
    </Box>
  );
}

function Header({ text }: { text: string }) {
  return (
    <Typography
      sx={{
        fontSize: 11,
        letterSpacing: 1.5,
        textTransform: "uppercase",
        color: "#999",
      }}
    >
      {text}
    </Typography>
  );
}

function MobileLabel({ text }: { text: string }) {
  return (
    <Typography
      sx={{
        display: { xs: "block", md: "none" },
        fontSize: 10,
        letterSpacing: 1.5,
        color: "#999",
        mb: 0.5,
      }}
    >
      {text}
    </Typography>
  );
}

function StockStatus({ item }: { item: InventoryItem }) {
  const isOut = item.stockStatus === "OutOfStock";
  const isLow = item.stockStatus === "LowStock";

  return (
    <Box sx={{ display: "inline-flex", alignItems: "center", gap: 1 }}>
      <Box
        sx={{
          width: 7,
          height: 7,
          borderRadius: "50%",
          backgroundColor: isOut ? "#c62828" : isLow ? "#b28745" : "#6b8068",
        }}
      />

      <Typography
        sx={{
          fontSize: 14,
          color: isOut ? "#c62828" : isLow ? "#8a6b35" : "#5f735d",
        }}
      >
        {item.stock}

        <Box component="span" sx={{ ml: 0.5, color: "#999" }}>
          {isOut ? "Agotado" : isLow ? "Bajo" : ""}
        </Box>
      </Typography>
    </Box>
  );
}
