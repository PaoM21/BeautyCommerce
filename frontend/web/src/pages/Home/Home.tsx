import {
  Box,
  Button,
  Container,
  Typography,
} from "@mui/material";

import { Link } from "react-router-dom";
import LocalShippingOutlinedIcon from "@mui/icons-material/LocalShippingOutlined";
import VerifiedOutlinedIcon from "@mui/icons-material/VerifiedOutlined";
import CardGiftcardOutlinedIcon from "@mui/icons-material/CardGiftcardOutlined";
import SupportAgentOutlinedIcon from "@mui/icons-material/SupportAgentOutlined";

import Seo from "../../components/seo/Seo";
import { palette } from "../../theme/theme";

const CATEGORIES = [
  {
    title: "Cabello",
    subtitle: "Rituales capilares de alta gama",
    imageUrl: "/categoria-cabello.svg",
    slug: "cabello",
  },
  {
    title: "Rostro",
    subtitle: "Maquillaje de precisión",
    imageUrl: "/categoria-rostro.svg",
    slug: "rostro",
  },
  {
    title: "Piel",
    subtitle: "Skincare de autor",
    imageUrl: "/categoria-piel.svg",
    slug: "piel",
  },
  {
    title: "Labios",
    subtitle: "Color con acabado editorial",
    imageUrl: "/categoria-labios.svg",
    slug: "labios",
  },
  {
    title: "Uñas",
    subtitle: "Manicure de salón, en casa",
    imageUrl: "/categoria-unas.svg",
    slug: "unas",
  },
];

const TRUST_POINTS = [
  {
    icon: VerifiedOutlinedIcon,
    title: "100% originales",
    text: "Marcas premium con garantía de autenticidad.",
  },
  {
    icon: LocalShippingOutlinedIcon,
    title: "Envío a toda Colombia",
    text: "Entrega asegurada y monitoreada puerta a puerta.",
  },
  {
    icon: CardGiftcardOutlinedIcon,
    title: "Empaque de lujo",
    text: "Cada pedido llega listo para regalar.",
  },
  {
    icon: SupportAgentOutlinedIcon,
    title: "Asesoría personalizada",
    text: "Especialistas en belleza a tu disposición.",
  },
];

const HOME_JSON_LD = {
  "@context": "https://schema.org",
  "@type": "Organization",
  name: "BeautyCommerce",
  url: "https://www.beautycommerce.co",
  logo: "https://www.beautycommerce.co/favicon.svg",
  description:
    "Ecommerce de belleza de lujo en Colombia: maquillaje, skincare, cabello, uñas y labios de marcas premium.",
  areaServed: "CO",
};

export default function Home() {
  return (
    <Box>
      <Seo
        title="Maquillaje y skincare de lujo en Colombia"
        description="Descubre una selección curada de maquillaje, skincare, cuidado del cabello, uñas y labios de marcas premium. Envíos a toda Colombia, empaque de lujo y asesoría personalizada."
        path="/"
        jsonLd={HOME_JSON_LD}
      />

      {/* HERO */}
      <Box
        sx={{
          minHeight: {
            xs: "78vh",
            md: "calc(100vh - 76px)",
          },
          display: "flex",
          alignItems: "center",
          background: `linear-gradient(180deg, ${palette.ivory} 0%, ${palette.ivoryDeep} 100%)`,
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
                  color: palette.gold,
                  mb: 3,
                  fontWeight: 500,
                }}
              >
                Curaduría de belleza premium
              </Typography>

              <Typography
                component="h1"
                variant="h1"
                sx={{
                  fontSize: { xs: 36, md: 60 },
                  lineHeight: 1.08,
                  color: palette.charcoal,
                  mb: 3,
                }}
              >
                El ritual de belleza que mereces, en cada detalle.
              </Typography>

              <Typography
                sx={{
                  fontSize: 17,
                  lineHeight: 1.75,
                  color: palette.textSecondary,
                  maxWidth: 520,
                  mb: 5,
                }}
              >
                Maquillaje, skincare, cabello, uñas y labios de las
                firmas más exclusivas del mundo, seleccionadas para
                quienes no negocian la calidad. Envío asegurado a
                toda Colombia y empaque digno de regalo.
              </Typography>

              <Box sx={{ display: "flex", gap: 2, flexWrap: "wrap" }}>
                <Button
                  component={Link}
                  to="/productos"
                  variant="contained"
                  size="large"
                  sx={{
                    px: 5,
                    py: 1.7,
                    boxShadow: "0 10px 24px rgba(26,23,20,0.18)",
                  }}
                >
                  Explorar colección
                </Button>

                <Button
                  component={Link}
                  to="/productos?categoria=piel"
                  variant="outlined"
                  size="large"
                  sx={{
                    px: 5,
                    py: 1.7,
                    color: palette.charcoal,
                  }}
                >
                  Ver skincare
                </Button>
              </Box>
            </Box>

            <Box
              sx={{
                display: { xs: "none", md: "block" },
                width: "100%",
                height: 460,
                overflow: "hidden",
                boxShadow: "0 20px 50px rgba(26,23,20,0.14)",
                backgroundImage: "url('/hero.svg')",
                backgroundSize: "cover",
                backgroundPosition: "center right",
              }}
              role="img"
              aria-label="Selección de maquillaje y skincare de lujo BeautyCommerce"
            />
          </Box>
        </Container>
      </Box>

      {/* TRUST BAR */}
      <Box sx={{ backgroundColor: "#ffffff", borderTop: `1px solid ${palette.border}`, borderBottom: `1px solid ${palette.border}` }}>
        <Container maxWidth="xl">
          <Box
            sx={{
              display: "grid",
              gridTemplateColumns: { xs: "1fr 1fr", md: "repeat(4, 1fr)" },
              gap: { xs: 3, md: 2 },
              py: { xs: 5, md: 6 },
            }}
          >
            {TRUST_POINTS.map(({ icon: Icon, title, text }) => (
              <Box key={title} sx={{ display: "flex", gap: 1.5, alignItems: "flex-start" }}>
                <Icon sx={{ color: palette.gold, fontSize: 28, mt: 0.3 }} />
                <Box>
                  <Typography sx={{ fontWeight: 600, fontSize: 14, mb: 0.3 }}>
                    {title}
                  </Typography>
                  <Typography sx={{ fontSize: 13, color: palette.textSecondary, lineHeight: 1.5 }}>
                    {text}
                  </Typography>
                </Box>
              </Box>
            ))}
          </Box>
        </Container>
      </Box>

      {/* CATEGORÍAS */}
      <Container maxWidth="xl" sx={{ py: { xs: 8, md: 12 } }}>
        <Typography
          sx={{
            fontSize: 13,
            letterSpacing: 3,
            textTransform: "uppercase",
            color: palette.gold,
            mb: 2,
            fontWeight: 500,
          }}
        >
          Categorías principales
        </Typography>

        <Typography
          component="h2"
          variant="h2"
          sx={{
            fontSize: { xs: 28, md: 42 },
            mb: 6,
            color: palette.charcoal,
            maxWidth: 640,
          }}
        >
          Tu ritual de belleza, de la raíz al último detalle
        </Typography>

        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: {
              xs: "1fr 1fr",
              sm: "repeat(3, 1fr)",
              lg: "repeat(5, 1fr)",
            },
            gap: 3,
          }}
        >
          {CATEGORIES.map((category) => (
            <CategoryCard key={category.slug} {...category} />
          ))}
        </Box>
      </Container>

      {/* PROMESA DE MARCA */}
      <Box sx={{ backgroundColor: palette.charcoal, color: "#fff" }}>
        <Container maxWidth="xl" sx={{ py: { xs: 8, md: 10 } }}>
          <Box
            sx={{
              display: "grid",
              gridTemplateColumns: { xs: "1fr", md: "1fr 1fr 1fr" },
              gap: 5,
            }}
          >
            <Box>
              <Typography
                sx={{
                  fontSize: 13,
                  letterSpacing: 3,
                  textTransform: "uppercase",
                  color: palette.goldLight,
                  mb: 2,
                }}
              >
                Nuestra promesa
              </Typography>

              <Typography
                component="h2"
                variant="h2"
                sx={{ fontSize: { xs: 26, md: 34 }, mb: 2 }}
              >
                Belleza sin concesiones
              </Typography>

              <Typography sx={{ color: "rgba(255,255,255,0.7)", lineHeight: 1.8 }}>
                Trabajamos solo con distribuidores autorizados para
                garantizar la autenticidad de cada producto que llega
                a tus manos.
              </Typography>
            </Box>

            <Box>
              <Typography sx={{ fontWeight: 600, mb: 1.5 }}>
                Experiencia de compra
              </Typography>
              <Typography sx={{ color: "rgba(255,255,255,0.7)", lineHeight: 1.8 }}>
                Empaque premium, seguimiento en tiempo real y una
                línea de asesoría dedicada a resolver tus dudas sobre
                cada rutina.
              </Typography>
            </Box>

            <Box>
              <Typography sx={{ fontWeight: 600, mb: 1.5 }}>
                Cobertura nacional
              </Typography>
              <Typography sx={{ color: "rgba(255,255,255,0.7)", lineHeight: 1.8 }}>
                Envíos asegurados a Bogotá, Medellín, Cali,
                Barranquilla y el resto del país.
              </Typography>
            </Box>
          </Box>
        </Container>
      </Box>
    </Box>
  );
}

interface CategoryCardProps {
  title: string;
  subtitle: string;
  imageUrl: string;
  slug: string;
}

function CategoryCard({ title, subtitle, imageUrl, slug }: CategoryCardProps) {
  return (
    <Box
      component={Link}
      to={`/productos?categoria=${slug}`}
      sx={{
        textDecoration: "none",
        color: "inherit",
        minHeight: 340,
        backgroundColor: "#fff",
        display: "flex",
        flexDirection: "column",
        justifyContent: "flex-end",
        p: 3,
        transition: "transform 0.25s ease, box-shadow 0.25s ease",
        overflow: "hidden",
        boxShadow: "0 8px 24px rgba(26,16,16,0.08)",
        backgroundImage: `url(${imageUrl})`,
        backgroundSize: "cover",
        backgroundPosition: "center",
        "&:hover": {
          transform: "translateY(-6px)",
          boxShadow: "0 18px 40px rgba(26,16,16,0.14)",
        },
      }}
    >
      <Box
        sx={{
          background: "linear-gradient(180deg, rgba(255,255,255,0) 0%, rgba(255,255,255,0.92) 65%)",
          py: 2,
          px: 1,
        }}
      >
        <Typography
          sx={{
            fontFamily: '"Fraunces", serif',
            fontSize: 21,
            fontWeight: 500,
            mb: 0.5,
            color: palette.charcoal,
          }}
        >
          {title}
        </Typography>

        <Typography
          sx={{
            color: palette.textSecondary,
            fontSize: 13,
          }}
        >
          {subtitle}
        </Typography>
      </Box>
    </Box>
  );
}
