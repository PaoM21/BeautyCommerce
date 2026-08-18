import { useState, type FormEvent } from "react";
import { Box, Container, IconButton, InputBase, Typography } from "@mui/material";
import { Link } from "react-router-dom";

import InstagramIcon from "@mui/icons-material/Instagram";
import FacebookIcon from "@mui/icons-material/Facebook";
import MusicNoteIcon from "@mui/icons-material/MusicNote";
import YouTubeIcon from "@mui/icons-material/YouTube";
import PinterestIcon from "@mui/icons-material/Pinterest";
import PlaceOutlinedIcon from "@mui/icons-material/PlaceOutlined";

import { palette } from "../../theme/theme";
import { subscribeNewsletter } from "../../services/newsletterService";

const INFORMATION_LINKS = [
  { label: "Nosotros", to: null },
  { label: "Términos y condiciones", to: "/terminos-y-condiciones" },
  { label: "Política de privacidad", to: "/tratamiento-de-datos-personales" },
  { label: "Política de cambios", to: null },
  { label: "Envíos", to: null },
  { label: "Preguntas frecuentes", to: null },
];

const ACCOUNT_LINKS = [
  { label: "Mis pedidos", to: "/mi-cuenta/pedidos" },
  { label: "Favoritos", to: "/favoritos" },
  { label: "Mis puntos", to: "/mi-cuenta/rewards" },
  { label: "Direcciones", to: null },
  { label: "Cupones", to: null },
];

const HELP_LINKS = [
  { label: "Contacto", to: null },
  { label: "WhatsApp", to: null },
  { label: "Método de pago", to: null },
  { label: "Garantías", to: null },
];

const SOCIAL_ICONS = [InstagramIcon, FacebookIcon, MusicNoteIcon, YouTubeIcon, PinterestIcon];

export default function Footer() {
  const [email, setEmail] = useState("");
  const [status, setStatus] = useState<"idle" | "sending" | "sent" | "error">("idle");

  const handleSubscribe = async (event: FormEvent) => {
    event.preventDefault();
    if (!email.trim() || status === "sending") return;

    try {
      setStatus("sending");
      await subscribeNewsletter(email.trim());
      setStatus("sent");
      setEmail("");
    } catch (error) {
      console.error(error);
      setStatus("error");
    }
  };

  return (
    <Box component="footer" sx={{ backgroundColor: "#ffffff", color: palette.ink, borderTop: `1px solid ${palette.border}` }}>
      <Container maxWidth="xl" sx={{ py: { xs: 6, md: 8 } }}>
        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: { xs: "1fr", sm: "1fr 1fr", md: "1.3fr 1fr 1fr 1fr 1.1fr" },
            gap: 5,
            pb: 5,
          }}
        >
          {/* SUSCRÍBETE + REDES */}
          <Box>
            <Typography sx={{ fontWeight: 700, fontSize: 13, letterSpacing: 1.5, textTransform: "uppercase", color: palette.ink, mb: 1 }}>
              Suscríbete y recibe
            </Typography>
            <Typography sx={{ fontSize: 13, color: palette.textSecondary, mb: 2 }}>
              Novedades y promociones exclusivas
            </Typography>

            <Box
              component="form"
              onSubmit={handleSubscribe}
              sx={{ display: "flex", backgroundColor: palette.ivory, border: `1px solid ${palette.border}`, borderRadius: 1, mb: 1 }}
            >
              <InputBase
                type="email"
                required
                placeholder="Tu correo electrónico"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                sx={{ flex: 1, color: palette.ink, fontSize: 13, px: 1.5 }}
              />
              <Box
                component="button"
                type="submit"
                disabled={status === "sending"}
                sx={{
                  border: "none",
                  backgroundColor: palette.gold,
                  color: "#fff",
                  fontSize: 11,
                  fontWeight: 600,
                  letterSpacing: 1,
                  px: 2,
                  cursor: "pointer",
                  "&:hover": { backgroundColor: palette.goldDeep },
                }}
              >
                SUSCRIBIRSE
              </Box>
            </Box>

            {status === "sent" && (
              <Typography sx={{ fontSize: 12, color: palette.goldDeep }}>
                ¡Gracias! Ya quedaste suscrita.
              </Typography>
            )}
            {status === "error" && (
              <Typography sx={{ fontSize: 12, color: "#c62828" }}>
                No fue posible suscribirte. Intenta de nuevo.
              </Typography>
            )}

            <Typography sx={{ fontSize: 12, letterSpacing: 1, textTransform: "uppercase", color: palette.textSecondary, mt: 3, mb: 1.2 }}>
              Síguenos
            </Typography>
            <Box sx={{ display: "flex", gap: 1 }}>
              {SOCIAL_ICONS.map((Icon, index) => (
                <IconButton
                  key={index}
                  title="Pendiente: agregar cuenta real"
                  sx={{
                    color: palette.textSecondary,
                    border: `1px dashed ${palette.border}`,
                    width: 34,
                    height: 34,
                    cursor: "default",
                  }}
                >
                  <Icon sx={{ fontSize: 17 }} />
                </IconButton>
              ))}
            </Box>
          </Box>

          <FooterColumn title="Información" links={INFORMATION_LINKS} />
          <FooterColumn title="Mi cuenta" links={ACCOUNT_LINKS} />
          <FooterColumn title="Ayuda" links={HELP_LINKS} />

          {/* CONTÁCTANOS */}
          <Box>
            <Typography sx={{ fontWeight: 700, fontSize: 13, letterSpacing: 1.5, textTransform: "uppercase", color: palette.ink, mb: 2 }}>
              Contáctanos
            </Typography>
            <Typography sx={placeholderSx}>[Teléfono pendiente]</Typography>
            <Typography sx={{ ...placeholderSx, mt: 0.8 }}>[correo@dominio.com]</Typography>
            <Typography sx={{ ...placeholderSx, mt: 0.8, mb: 2 }}>[Dirección pendiente], Colombia</Typography>

            <Box
              sx={{
                height: 90,
                border: `1px dashed ${palette.border}`,
                borderRadius: 1,
                display: "flex",
                flexDirection: "column",
                alignItems: "center",
                justifyContent: "center",
                gap: 0.5,
                backgroundColor: palette.ivory,
              }}
            >
              <PlaceOutlinedIcon sx={{ color: palette.textSecondary, fontSize: 20 }} />
              <Typography sx={{ fontSize: 10.5, color: palette.textSecondary, textAlign: "center", px: 1 }}>
                Mapa disponible cuando tengamos la dirección real
              </Typography>
            </Box>
          </Box>
        </Box>

        <Box
          sx={{
            borderTop: `1px solid ${palette.border}`,
            pt: 3,
            display: "flex",
            justifyContent: "space-between",
            flexWrap: "wrap",
            gap: 1,
          }}
        >
          <Typography sx={{ fontSize: 12, color: palette.textSecondary }}>
            © {new Date().getFullYear()} HALDY&amp;CO ECOMMERCE. Todos los derechos reservados.
          </Typography>
          <Typography sx={{ fontSize: 12, color: palette.textSecondary }}>
            Bogotá · Medellín · Cali
          </Typography>
        </Box>
      </Container>
    </Box>
  );
}

const placeholderSx = {
  fontSize: 13,
  color: palette.goldDeep,
  borderBottom: `1px dashed ${palette.goldDeep}`,
  display: "inline-block",
  pb: 0.2,
};

interface FooterLinkItem {
  label: string;
  to: string | null;
}

function FooterColumn({ title, links }: { title: string; links: FooterLinkItem[] }) {
  return (
    <Box>
      <Typography sx={{ fontWeight: 700, fontSize: 13, letterSpacing: 1.5, textTransform: "uppercase", color: palette.ink, mb: 2 }}>
        {title}
      </Typography>
      <Box sx={{ display: "flex", flexDirection: "column", gap: 1.2 }}>
        {links.map((link) =>
          link.to ? (
            <Typography
              key={link.label}
              component={Link}
              to={link.to}
              sx={{
                fontSize: 14,
                color: palette.textSecondary,
                textDecoration: "none",
                transition: "color 0.2s ease",
                "&:hover": { color: palette.gold },
              }}
            >
              {link.label}
            </Typography>
          ) : (
            <Typography key={link.label} sx={{ fontSize: 14, color: "#bbbbbb" }}>
              {link.label} <Box component="span" sx={{ fontSize: 10.5 }}>(próximamente)</Box>
            </Typography>
          )
        )}
      </Box>
    </Box>
  );
}
