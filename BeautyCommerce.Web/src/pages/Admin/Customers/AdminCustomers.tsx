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
import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";

import { getAdminCustomers } from "../../../services/customerService";

export default function AdminCustomers() {
  const [search, setSearch] = useState("");

  const {
    data: customers = [],
    isLoading,
    isError,
  } = useQuery({
    queryKey: ["admin-customers"],
    queryFn: getAdminCustomers,
  });

  const filteredCustomers = useMemo(() => {
    const value = search.trim().toLowerCase();

    if (!value) {
      return customers;
    }

    return customers.filter((customer) =>
      [customer.fullName, customer.email].some((field) =>
        field.toLowerCase().includes(value)
      )
    );
  }, [customers, search]);

  const activeCustomers = customers.filter(
    (customer) => customer.isActive
  ).length;

  const inactiveCustomers = customers.filter(
    (customer) => !customer.isActive
  ).length;

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
          No fue posible cargar los clientes.
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
        Clientes
      </Typography>

      <Box
        sx={{
          display: "grid",
          gridTemplateColumns: { xs: "1fr", sm: "repeat(3, 1fr)" },
          gap: 3,
          mb: 6,
        }}
      >
        <SummaryCard label="Total clientes" value={customers.length} />

        <SummaryCard label="Clientes activos" value={activeCustomers} />

        <SummaryCard
          label="Clientes inactivos"
          value={inactiveCustomers}
        />
      </Box>

      <Box
        sx={{
          display: "flex",
          gap: 2,
          mb: 5,
          flexDirection: { xs: "column", sm: "row" },
        }}
      >
        <TextField
          fullWidth
          size="small"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder="Buscar por nombre o correo..."
        />

        {search && (
          <Button
            onClick={() => setSearch("")}
            sx={{ minWidth: 100, color: "#777" }}
          >
            Limpiar
          </Button>
        )}
      </Box>

      <Divider sx={{ mb: 2 }} />

      {filteredCustomers.length === 0 ? (
        <Box sx={{ py: 10, textAlign: "center" }}>
          <Typography sx={{ fontSize: 22, mb: 1 }}>
            {search ? "No se encontraron clientes" : "No hay clientes"}
          </Typography>

          <Typography color="text.secondary">
            {search
              ? "Intenta con otro nombre o correo."
              : "Los clientes registrados aparecerán aquí."}
          </Typography>
        </Box>
      ) : (
        <Box>
          <Box
            sx={{
              display: { xs: "none", md: "grid" },
              gridTemplateColumns: "1.5fr 1.5fr 1fr 1fr",
              gap: 3,
              px: 2,
              pb: 2,
            }}
          >
            <Header text="Cliente" />
            <Header text="Correo electrónico" />
            <Header text="Estado" />
            <Header text="Tipo" />
          </Box>

          <Divider />

          {filteredCustomers.map((customer) => (
            <Box key={customer.id}>
              <Box
                sx={{
                  display: "grid",
                  gridTemplateColumns: {
                    xs: "1fr",
                    md: "1.5fr 1.5fr 1fr 1fr",
                  },
                  gap: { xs: 2, md: 3 },
                  alignItems: "center",
                  px: 2,
                  py: 3,
                  transition: "background-color .2s",
                  "&:hover": {
                    backgroundColor: "#faf9f7",
                  },
                }}
              >
                <Box>
                  <MobileLabel text="CLIENTE" />

                  <Typography sx={{ fontWeight: 500 }}>
                    {customer.fullName}
                  </Typography>
                </Box>

                <Box>
                  <MobileLabel text="CORREO" />

                  <Typography sx={{ color: "#555", wordBreak: "break-word" }}>
                    {customer.email}
                  </Typography>
                </Box>

                <Box>
                  <MobileLabel text="ESTADO" />

                  <Status isActive={customer.isActive} />
                </Box>

                <Box>
                  <MobileLabel text="TIPO" />

                  <Typography sx={{ fontSize: 14, color: "#777" }}>
                    Cliente
                  </Typography>
                </Box>
              </Box>

              <Divider />
            </Box>
          ))}
        </Box>
      )}

      {search && filteredCustomers.length > 0 && (
        <Typography sx={{ mt: 4, fontSize: 13, color: "#999" }}>
          Mostrando {filteredCustomers.length} de {customers.length} clientes
        </Typography>
      )}
    </Container>
  );
}

function SummaryCard({ label, value }: { label: string; value: number }) {
  return (
    <Box
      sx={{
        border: "1px solid #e5e1dc",
        p: { xs: 3, md: 4 },
        minHeight: 140,
      }}
    >
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

      <Typography sx={{ fontSize: { xs: 30, md: 36 }, fontWeight: 400 }}>
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

function Status({ isActive }: { isActive: boolean }) {
  return (
    <Box sx={{ display: "inline-flex", alignItems: "center", gap: 1 }}>
      <Box
        sx={{
          width: 7,
          height: 7,
          borderRadius: "50%",
          backgroundColor: isActive ? "#6b8068" : "#b8b8b8",
        }}
      />

      <Typography
        sx={{
          fontSize: 14,
          color: isActive ? "#5f735d" : "#888",
        }}
      >
        {isActive ? "Activo" : "Inactivo"}
      </Typography>
    </Box>
  );
}
