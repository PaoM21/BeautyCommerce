import {
  Box,
  CircularProgress,
  Container,
  LinearProgress,
  Typography,
} from "@mui/material";

import { useQuery } from "@tanstack/react-query";

import { getMyLoyalty } from "../../services/loyaltyService";

export default function Loyalty() {
  const {
    data: loyalty,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ["loyalty"],
    queryFn: getMyLoyalty,
  });

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

  if (isError || !loyalty) {
    return (
      <Container sx={{ py: 10 }}>
        <Typography>
          No fue posible cargar tu programa de puntos.
        </Typography>
      </Container>
    );
  }

  const points = loyalty.points;

  const nextLevel =
    loyalty.level === "Bronze"
      ? 500
      : loyalty.level === "Silver"
        ? 1500
        : null;

  const currentLevelStart =
    loyalty.level === "Bronze"
      ? 0
      : loyalty.level === "Silver"
        ? 500
        : 1500;

  const progress = nextLevel
    ? Math.min(
        100,
        ((points - currentLevelStart) /
          (nextLevel - currentLevelStart)) *
          100
      )
    : 100;

  const remaining = nextLevel
    ? Math.max(0, nextLevel - points)
    : 0;

  return (
    <Container maxWidth="lg" sx={{ py: 8 }}>
      {/* HEADER */}

      <Box sx={{ mb: 6 }}>
        <Typography
          sx={{
            fontSize: 12,
            letterSpacing: 3,
            textTransform: "uppercase",
            color: "#9a8065",
            mb: 2,
          }}
        >
          Beauty Rewards
        </Typography>

        <Typography
          component="h1"
          sx={{
            fontSize: {
              xs: 38,
              md: 52,
            },
            fontWeight: 400,
          }}
        >
          Tu belleza tiene recompensas.
        </Typography>

        <Typography
          sx={{
            mt: 2,
            color: "#777",
            maxWidth: 600,
            lineHeight: 1.8,
          }}
        >
          Acumula puntos con tus compras y disfruta
          de beneficios exclusivos.
        </Typography>
      </Box>

      {/* CARD PRINCIPAL */}

      <Box
        sx={{
          backgroundColor: "#f7f4ef",
          p: {
            xs: 4,
            md: 7,
          },
          mb: 5,
        }}
      >
        <Box
          sx={{
            display: "flex",
            flexDirection: {
              xs: "column",
              md: "row",
            },
            justifyContent: "space-between",
            gap: 5,
          }}
        >
          <Box>
            <Typography
              sx={{
                fontSize: 12,
                letterSpacing: 2,
                textTransform: "uppercase",
                color: "#888",
              }}
            >
              Tus puntos
            </Typography>

            <Typography
              sx={{
                fontSize: {
                  xs: 64,
                  md: 86,
                },
                fontWeight: 300,
                lineHeight: 1,
                mt: 2,
              }}
            >
              {points.toLocaleString("es-CO")}
            </Typography>

            <Typography
              sx={{
                mt: 1,
                color: "#888",
              }}
            >
              puntos disponibles
            </Typography>
          </Box>

          <Box
            sx={{
              minWidth: {
                xs: "100%",
                md: 220,
              },
              display: "flex",
              flexDirection: "column",
              justifyContent: "center",
            }}
          >
            <Typography
              sx={{
                fontSize: 12,
                letterSpacing: 2,
                textTransform: "uppercase",
                color: "#888",
              }}
            >
              Tu nivel
            </Typography>

            <Typography
              sx={{
                fontSize: 34,
                fontWeight: 400,
                mt: 1,
              }}
            >
              {loyalty.level}
            </Typography>
          </Box>
        </Box>
      </Box>

      {/* PROGRESO */}

      {nextLevel && (
        <Box sx={{ mb: 7 }}>
          <Box
            sx={{
              display: "flex",
              justifyContent: "space-between",
              mb: 1,
            }}
          >
            <Typography variant="body2">
              {loyalty.level}
            </Typography>

            <Typography variant="body2">
              {loyalty.level === "Bronze"
                ? "Silver"
                : "Gold"}
            </Typography>
          </Box>

          <LinearProgress
            variant="determinate"
            value={progress}
            sx={{
              height: 8,
              borderRadius: 0,
              backgroundColor: "#e7e2db",
              "& .MuiLinearProgress-bar": {
                backgroundColor: "#9a8065",
              },
            }}
          />

          <Typography
            sx={{
              mt: 2,
              color: "#777",
              fontSize: 14,
            }}
          >
            Te faltan{" "}
            <strong>
              {remaining.toLocaleString("es-CO")}
            </strong>{" "}
            puntos para alcanzar el siguiente nivel.
          </Typography>
        </Box>
      )}

      {/* BENEFICIOS */}

      <Box>
        <Typography
          component="h2"
          sx={{
            fontSize: 28,
            fontWeight: 400,
            mb: 4,
          }}
        >
          Tus beneficios
        </Typography>

        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: {
              xs: "1fr",
              md: "repeat(3, 1fr)",
            },
            gap: 3,
          }}
        >
          <Benefit
            title="Bronze"
            description="Comienza a acumular puntos con cada compra."
          />

          <Benefit
            title="Silver"
            description="Obtén beneficios exclusivos al alcanzar 500 puntos."
          />

          <Benefit
            title="Gold"
            description="Disfruta de beneficios especiales desde 1.500 puntos."
          />
        </Box>
      </Box>
    </Container>
  );
}

function Benefit({
  title,
  description,
}: {
  title: string;
  description: string;
}) {
  return (
    <Box
      sx={{
        border: "1px solid #e5e1dc",
        p: 4,
        minHeight: 160,
      }}
    >
      <Typography
        sx={{
          fontSize: 12,
          letterSpacing: 2,
          textTransform: "uppercase",
          color: "#9a8065",
          mb: 2,
        }}
      >
        {title}
      </Typography>

      <Typography
        sx={{
          color: "#666",
          lineHeight: 1.7,
        }}
      >
        {description}
      </Typography>
    </Box>
  );
}
