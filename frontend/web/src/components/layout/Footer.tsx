import { Box, Container, Typography } from "@mui/material";
import { Link } from "react-router-dom";
import { palette } from "../../theme/theme";

const EXPLORE_LINKS = [
  { to: "/productos", label: "Todos los productos" },
  { to: "/productos?categoria=cabello", label: "Cabello" },
  { to: "/productos?categoria=rostro", label: "Rostro" },
  { to: "/productos?categoria=piel", label: "Piel" },
  { to: "/productos?categoria=labios", label: "Labios" },
  { to: "/productos?categoria=unas", label: "Uñas" },
];

const ACCOUNT_LINKS = [
  { to: "/mi-cuenta/pedidos", label: "Mis pedidos" },
  { to: "/favoritos", label: "Favoritos" },
  { to: "/mi-cuenta/rewards", label: "Programa de puntos" },
  { to: "/carrito", label: "Carrito" },
];

export default function Footer() {
  return (
    <Box
      component="footer"
      sx={{ backgroundColor: palette.charcoal, color: "rgba(255,255,255,0.85)" }}
    >
      <Container maxWidth="xl" sx={{ py: { xs: 6, md: 8 } }}>
        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: { xs: "1fr", sm: "1.4fr 1fr 1fr" },
            gap: 5,
            pb: 5,
          }}
        >
          <Box>
            <Typography
              sx={{
                fontFamily: '"Fraunces", serif',
                fontSize: 22,
                color: "#fff",
                mb: 1.5,
              }}
            >
              BEAUTY COMMERCE
            </Typography>
            <Typography sx={{ fontSize: 14, lineHeight: 1.8, maxWidth: 340, color: "rgba(255,255,255,0.6)" }}>
              Curaduría de maquillaje y skincare de marcas premium para
              quienes viven la belleza como un ritual, no como un trámite.
            </Typography>
          </Box>

          <Box>
            <Typography sx={{ fontWeight: 600, fontSize: 13, letterSpacing: 1.5, textTransform: "uppercase", color: palette.goldLight, mb: 2 }}>
              Explora
            </Typography>
            <Box sx={{ display: "flex", flexDirection: "column", gap: 1.2 }}>
              {EXPLORE_LINKS.map((link) => (
                <FooterLink key={link.label} {...link} />
              ))}
            </Box>
          </Box>

          <Box>
            <Typography sx={{ fontWeight: 600, fontSize: 13, letterSpacing: 1.5, textTransform: "uppercase", color: palette.goldLight, mb: 2 }}>
              Mi cuenta
            </Typography>
            <Box sx={{ display: "flex", flexDirection: "column", gap: 1.2 }}>
              {ACCOUNT_LINKS.map((link) => (
                <FooterLink key={link.label} {...link} />
              ))}
            </Box>
          </Box>
        </Box>

        <Box
          sx={{
            borderTop: "1px solid rgba(255,255,255,0.12)",
            pt: 3,
            display: "flex",
            justifyContent: "space-between",
            flexWrap: "wrap",
            gap: 1,
          }}
        >
          <Typography sx={{ fontSize: 12, color: "rgba(255,255,255,0.45)" }}>
            © {new Date().getFullYear()} BeautyCommerce. Todos los derechos reservados.
          </Typography>
          <Typography sx={{ fontSize: 12, color: "rgba(255,255,255,0.45)" }}>
            Bogotá · Medellín · Cali
          </Typography>
        </Box>
      </Container>
    </Box>
  );
}

function FooterLink({ to, label }: { to: string; label: string }) {
  return (
    <Typography
      component={Link}
      to={to}
      sx={{
        fontSize: 14,
        color: "rgba(255,255,255,0.75)",
        textDecoration: "none",
        transition: "color 0.2s ease",
        "&:hover": { color: palette.goldLight },
      }}
    >
      {label}
    </Typography>
  );
}
