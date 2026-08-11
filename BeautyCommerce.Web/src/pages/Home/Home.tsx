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
      {/* HERO: split layout with image on the right */}
      <Box
        sx={{
          minHeight: {
            xs: "70vh",
            md: "calc(100vh - 76px)",
          },
          display: "flex",
          alignItems: "center",
          background: "linear-gradient(180deg, rgba(247,245,242,1) 0%, rgba(247,245,242,1) 50%)",
          position: "relative",
          overflow: "hidden",
        }}
      >
        <Container maxWidth="xl">
          <Box
            sx={{
              display: "grid",
              gridTemplateColumns: { xs: "1fr", md: "1fr 1fr" },
              alignItems: "center",
              gap: 4,
              py: { xs: 6, md: 10 },
            }}
          >
            <Box sx={{ maxWidth: 620 }}>
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
                  fontSize: { xs: 34, md: 56 },
                  lineHeight: 1.05,
                  fontWeight: 400,
                  letterSpacing: -1.5,
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
                  maxWidth: 520,
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
                  color: "#fff",
                  px: 5,
                  py: 1.6,
                  borderRadius: 6,
                  boxShadow: "0 6px 18px rgba(31,31,31,0.18)",
                }}
              >
                Explorar colección
              </Button>
            </Box>

            {/* Right column: hero image box (place hero.jpg in public/) */}
            <Box
              sx={{
                display: { xs: "none", md: "block" },
                width: "100%",
                height: 420,
                borderRadius: 12,
                overflow: "hidden",
                boxShadow: "0 10px 30px rgba(0,0,0,0.12)",
                backgroundImage: "url('/hero.svg')",
                backgroundSize: "cover",
                backgroundPosition: "center right",
              }}
              aria-hidden
            />
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
          Categorías principales
        </Typography>

        <Typography
          component="h2"
          sx={{
            fontSize: { xs: 28, md: 40 },
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
            gap: 3,
          }}
        >
          <CategoryCard
            title="Maquillaje"
            subtitle="Looks que hablan de ti"
            imageUrl="/maquillaje.svg"
          />

          <CategoryCard
            title="Skincare"
            subtitle="Cuida tu piel"
            imageUrl="/skincare.svg"
          />

          <CategoryCard
            title="Accesorios"
            subtitle="Detalles que transforman"
            imageUrl="/accesorios.svg"
          />

          <CategoryCard
            title="Fragancias"
            subtitle="Tu esencia"
            imageUrl="/fragancias.svg"
          />
        </Box>
      </Container>
    </Box>
  );
}

interface CategoryCardProps {
  title: string;
  subtitle: string;
  imageUrl?: string;
}

function CategoryCard({
  title,
  subtitle,
  imageUrl,
}: CategoryCardProps & { imageUrl?: string }) {
  return (
    <Box
      component={Link}
      to="/productos"
      sx={{
        textDecoration: "none",
        color: "inherit",
        minHeight: 320,
        backgroundColor: "#fff",
        display: "flex",
        flexDirection: "column",
        justifyContent: "flex-end",
        p: 4,
        transition: "transform 0.25s ease, box-shadow 0.25s ease",
        borderRadius: 12,
        overflow: "hidden",
        boxShadow: "0 8px 24px rgba(16,16,16,0.08)",
        backgroundImage: imageUrl ? `url(${imageUrl})` : undefined,
        backgroundSize: "cover",
        backgroundPosition: "center",
        "&:hover": {
          transform: "translateY(-6px)",
          boxShadow: "0 18px 40px rgba(16,16,16,0.12)",
        },
      }}
    >
      <Box
        sx={{
          background: "linear-gradient(180deg, rgba(255,255,255,0) 0%, rgba(255,255,255,0.9) 70%)",
          py: 2,
          px: 1,
        }}
      >
        <Typography
          sx={{
            fontSize: 22,
            fontWeight: 600,
            mb: 0.5,
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
    </Box>
  );
}
