import {
  Box,
  Button,
  Container,
  Typography,
} from "@mui/material";

import { Link } from "react-router-dom";

export default function Home() {
  return (
    <Box>
      {/* HERO */}
      <Box
        sx={{
          minHeight: {
            xs: "70vh",
            md: "calc(100vh - 76px)",
          },
          display: "flex",
          alignItems: "center",
          backgroundColor: "#f7f5f2",
        }}
      >
        <Container maxWidth="xl">
          <Box
            sx={{
              maxWidth: 620,
              py: 10,
            }}
          >
            <Typography
              sx={{
                fontSize: 13,
                letterSpacing: 3,
                textTransform: "uppercase",
                color: "#9a8065",
                mb: 3,
              }}
            >
              Beauty Collection
            </Typography>

            <Typography
              component="h1"
              sx={{
                fontSize: {
                  xs: 42,
                  md: 68,
                },
                lineHeight: 1.05,
                fontWeight: 400,
                letterSpacing: -2,
                color: "#202020",
                mb: 3,
              }}
            >
              Descubre la belleza que resalta tu esencia.
            </Typography>

            <Typography
              sx={{
                fontSize: 17,
                lineHeight: 1.7,
                color: "#666666",
                maxWidth: 500,
                mb: 4,
              }}
            >
              Productos seleccionados para crear una rutina
              de belleza que se adapte a ti.
            </Typography>

            <Button
              component={Link}
              to="/productos"
              variant="contained"
              sx={{
                backgroundColor: "#1f1f1f",
                px: 5,
                py: 1.7,
                borderRadius: 0,
                "&:hover": {
                  backgroundColor: "#333333",
                },
              }}
            >
              Explorar colección
            </Button>
          </Box>
        </Container>
      </Box>

      {/* CATEGORÍAS */}
      <Container maxWidth="xl" sx={{ py: 12 }}>
        <Typography
          sx={{
            fontSize: 13,
            letterSpacing: 3,
            textTransform: "uppercase",
            color: "#9a8065",
            mb: 2,
          }}
        >
          Explora
        </Typography>

        <Typography
          component="h2"
          sx={{
            fontSize: {
              xs: 32,
              md: 44,
            },
            fontWeight: 400,
            mb: 6,
          }}
        >
          Encuentra tu ritual de belleza
        </Typography>

        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: {
              xs: "1fr",
              sm: "1fr 1fr",
              lg: "repeat(4, 1fr)",
            },
            gap: 2,
          }}
        >
          <CategoryCard
            title="Maquillaje"
            subtitle="Looks que hablan de ti"
          />

          <CategoryCard
            title="Skincare"
            subtitle="Cuida tu piel"
          />

          <CategoryCard
            title="Accesorios"
            subtitle="Detalles que transforman"
          />

          <CategoryCard
            title="Fragancias"
            subtitle="Tu esencia"
          />
        </Box>
      </Container>
    </Box>
  );
}

interface CategoryCardProps {
  title: string;
  subtitle: string;
}

function CategoryCard({
  title,
  subtitle,
}: CategoryCardProps) {
  return (
    <Box
      component={Link}
      to="/productos"
      sx={{
        textDecoration: "none",
        color: "inherit",
        minHeight: 320,
        backgroundColor: "#f3f1ee",
        display: "flex",
        flexDirection: "column",
        justifyContent: "flex-end",
        p: 4,
        transition: "transform 0.3s ease",
        "&:hover": {
          transform: "translateY(-5px)",
        },
      }}
    >
      <Typography
        sx={{
          fontSize: 25,
          fontWeight: 500,
          mb: 1,
        }}
      >
        {title}
      </Typography>

      <Typography
        sx={{
          color: "#777777",
          fontSize: 14,
        }}
      >
        {subtitle}
      </Typography>
    </Box>
  );
}
