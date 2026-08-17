import { Box, Button, Container, Rating, Typography } from "@mui/material";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";

import LocalShippingOutlinedIcon from "@mui/icons-material/LocalShippingOutlined";
import VerifiedOutlinedIcon from "@mui/icons-material/VerifiedOutlined";
import CardGiftcardOutlinedIcon from "@mui/icons-material/CardGiftcardOutlined";
import SupportAgentOutlinedIcon from "@mui/icons-material/SupportAgentOutlined";
import ContentCutOutlinedIcon from "@mui/icons-material/ContentCutOutlined";
import FaceRetouchingNaturalOutlinedIcon from "@mui/icons-material/FaceRetouchingNaturalOutlined";
import WaterDropOutlinedIcon from "@mui/icons-material/WaterDropOutlined";
import ColorLensOutlinedIcon from "@mui/icons-material/ColorLensOutlined";
import BackHandOutlinedIcon from "@mui/icons-material/BackHandOutlined";

import Seo from "../../components/seo/Seo";
import PhotoPlaceholder from "../../components/media/PhotoPlaceholder";
import { palette } from "../../theme/theme";
import { getProducts } from "../../services/productService";
import type { Product } from "../../types/product";

const CATEGORIES = [
  { title: "Cabello", subtitle: "Rituales capilares de alta gama", tone: "gold", slug: "cabello" },
  { title: "Rostro", subtitle: "Maquillaje de precisión", tone: "rose", slug: "rostro" },
  { title: "Piel", subtitle: "Skincare de autor", tone: "ivory", slug: "piel" },
  { title: "Labios", subtitle: "Color con acabado editorial", tone: "rose", slug: "labios" },
  { title: "Uñas", subtitle: "Manicure de salón, en casa", tone: "gold", slug: "unas" },
] as const;

const NEEDS = [
  { icon: FaceRetouchingNaturalOutlinedIcon, label: "Maquillaje de rostro", slug: "rostro" },
  { icon: WaterDropOutlinedIcon, label: "Hidratación profunda", slug: "piel" },
  { icon: ColorLensOutlinedIcon, label: "Labios statement", slug: "labios" },
  { icon: ContentCutOutlinedIcon, label: "Rituales capilares", slug: "cabello" },
  { icon: BackHandOutlinedIcon, label: "Manicure de salón", slug: "unas" },
];

const TRUST_POINTS = [
  { icon: VerifiedOutlinedIcon, title: "100% originales", text: "Marcas premium con garantía de autenticidad." },
  { icon: LocalShippingOutlinedIcon, title: "Envío a toda Colombia", text: "Entrega asegurada y monitoreada puerta a puerta." },
  { icon: CardGiftcardOutlinedIcon, title: "Empaque de lujo", text: "Cada pedido llega listo para regalar." },
  { icon: SupportAgentOutlinedIcon, title: "Asesoría personalizada", text: "Especialistas en belleza a tu disposición." },
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
  const { data: newArrivals = [] } = useQuery({
    queryKey: ["products", "home-new-arrivals"],
    queryFn: () => getProducts(),
  });

  return (
    <Box>
      <Seo
        title="Maquillaje y skincare de lujo en Colombia"
        description="Descubre una selección curada de maquillaje, skincare, cuidado del cabello, uñas y labios de marcas premium. Envíos a toda Colombia, empaque de lujo y asesoría personalizada."
        path="/"
        jsonLd={HOME_JSON_LD}
      />

      {/* HERO — foto de campaña a página completa (reemplazar con PhotoPlaceholder src="/images/hero.jpg") */}
      <Box
        sx={{
          position: "relative",
          minHeight: { xs: "82vh", md: "calc(100vh - 116px)" },
          display: "flex",
          alignItems: "flex-end",
          overflow: "hidden",
        }}
      >
        <PhotoPlaceholder
          alt="Campaña BeautyCommerce"
          tone="charcoal"
          sx={{ position: "absolute", inset: 0, width: "100%", height: "100%" }}
        />

        <Box
          sx={{
            position: "absolute",
            inset: 0,
            background:
              "linear-gradient(0deg, rgba(26,23,20,0.88) 0%, rgba(26,23,20,0.35) 45%, rgba(26,23,20,0.05) 70%)",
          }}
        />

        <Container maxWidth="xl" sx={{ position: "relative", pb: { xs: 6, md: 9 }, pt: 12 }}>
          <Box sx={{ maxWidth: 640 }}>
            <Typography
              sx={{
                fontSize: 13,
                letterSpacing: 3,
                textTransform: "uppercase",
                color: palette.goldLight,
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
                color: "#fff",
                mb: 3,
              }}
            >
              El ritual de belleza que mereces, en cada detalle.
            </Typography>

            <Typography
              sx={{
                fontSize: 17,
                lineHeight: 1.75,
                color: "rgba(255,255,255,0.8)",
                maxWidth: 520,
                mb: 5,
              }}
            >
              Maquillaje, skincare, cabello, uñas y labios de las firmas
              más exclusivas, seleccionadas para quienes no negocian la
              calidad.
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
                  backgroundColor: "#fff",
                  color: palette.charcoal,
                  "&:hover": { backgroundColor: palette.ivoryDeep },
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
                  color: "#fff",
                  borderColor: "rgba(255,255,255,0.6)",
                  "&:hover": { borderColor: "#fff", backgroundColor: "rgba(255,255,255,0.08)" },
                }}
              >
                Ver skincare
              </Button>
            </Box>
          </Box>
        </Container>
      </Box>

      {/* TRUST BAR */}
      <Box sx={{ backgroundColor: "#ffffff", borderBottom: `1px solid ${palette.border}` }}>
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
                  <Typography sx={{ fontWeight: 600, fontSize: 14, mb: 0.3 }}>{title}</Typography>
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
          sx={{ fontSize: { xs: 28, md: 42 }, mb: 6, color: palette.charcoal, maxWidth: 640 }}
        >
          Tu ritual de belleza, de la raíz al último detalle
        </Typography>

        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: { xs: "1fr 1fr", sm: "repeat(3, 1fr)", lg: "repeat(5, 1fr)" },
            gap: 3,
          }}
        >
          {CATEGORIES.map((category) => (
            <CategoryCard key={category.slug} {...category} />
          ))}
        </Box>
      </Container>

      {/* COMPRA POR NECESIDAD */}
      <Box sx={{ backgroundColor: palette.ivoryDeep }}>
        <Container maxWidth="xl" sx={{ py: { xs: 7, md: 9 } }}>
          <Typography
            component="h2"
            variant="h2"
            sx={{ fontSize: { xs: 24, md: 30 }, mb: 5, color: palette.charcoal, textAlign: "center" }}
          >
            Compra por necesidad
          </Typography>

          <Box
            sx={{
              display: "grid",
              gridTemplateColumns: { xs: "repeat(2, 1fr)", sm: "repeat(3, 1fr)", md: "repeat(5, 1fr)" },
              gap: 3,
            }}
          >
            {NEEDS.map(({ icon: Icon, label, slug }) => (
              <Box
                key={label}
                component={Link}
                to={`/productos?categoria=${slug}`}
                sx={{
                  textDecoration: "none",
                  color: "inherit",
                  display: "flex",
                  flexDirection: "column",
                  alignItems: "center",
                  gap: 1.5,
                  p: 3,
                  textAlign: "center",
                  transition: "transform 0.2s ease",
                  "&:hover": { transform: "translateY(-3px)" },
                }}
              >
                <Box
                  sx={{
                    width: 64,
                    height: 64,
                    borderRadius: "50%",
                    backgroundColor: "#fff",
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                    boxShadow: "0 6px 16px rgba(26,23,20,0.08)",
                  }}
                >
                  <Icon sx={{ color: palette.gold, fontSize: 26 }} />
                </Box>
                <Typography sx={{ fontSize: 13.5, fontWeight: 500, color: palette.charcoal }}>
                  {label}
                </Typography>
              </Box>
            ))}
          </Box>
        </Container>
      </Box>

      {/* NOVEDADES — catálogo real (se oculta si aún no hay productos publicados) */}
      {newArrivals.length > 0 && (
        <Container maxWidth="xl" sx={{ py: { xs: 8, md: 12 } }}>
          <Box sx={{ display: "flex", justifyContent: "space-between", alignItems: "flex-end", mb: 5 }}>
            <Box>
              <Typography
                sx={{ fontSize: 13, letterSpacing: 3, textTransform: "uppercase", color: palette.gold, mb: 2, fontWeight: 500 }}
              >
                Recién llegados
              </Typography>
              <Typography component="h2" variant="h2" sx={{ fontSize: { xs: 26, md: 36 }, color: palette.charcoal }}>
                Novedades de la semana
              </Typography>
            </Box>

            <Button component={Link} to="/productos" variant="outlined" sx={{ display: { xs: "none", sm: "inline-flex" } }}>
              Ver todo
            </Button>
          </Box>

          <Box
            sx={{
              display: "grid",
              gridTemplateColumns: { xs: "1fr 1fr", sm: "repeat(3, 1fr)", lg: "repeat(4, 1fr)" },
              gap: 3,
            }}
          >
            {newArrivals.slice(0, 8).map((product) => (
              <NewArrivalCard key={product.id} product={product} />
            ))}
          </Box>
        </Container>
      )}

      {/* PROMESA DE MARCA */}
      <Box sx={{ backgroundColor: palette.charcoal, color: "#fff" }}>
        <Container maxWidth="xl" sx={{ py: { xs: 8, md: 10 } }}>
          <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", md: "1fr 1fr 1fr" }, gap: 5 }}>
            <Box>
              <Typography sx={{ fontSize: 13, letterSpacing: 3, textTransform: "uppercase", color: palette.goldLight, mb: 2 }}>
                Nuestra promesa
              </Typography>
              <Typography component="h2" variant="h2" sx={{ fontSize: { xs: 26, md: 34 }, mb: 2 }}>
                Belleza sin concesiones
              </Typography>
              <Typography sx={{ color: "rgba(255,255,255,0.7)", lineHeight: 1.8 }}>
                Trabajamos solo con distribuidores autorizados para garantizar
                la autenticidad de cada producto que llega a tus manos.
              </Typography>
            </Box>

            <Box>
              <Typography sx={{ fontWeight: 600, mb: 1.5 }}>Experiencia de compra</Typography>
              <Typography sx={{ color: "rgba(255,255,255,0.7)", lineHeight: 1.8 }}>
                Empaque premium, seguimiento de tu pedido y una línea de
                asesoría dedicada a resolver tus dudas sobre cada rutina.
              </Typography>
            </Box>

            <Box>
              <Typography sx={{ fontWeight: 600, mb: 1.5 }}>Cobertura nacional</Typography>
              <Typography sx={{ color: "rgba(255,255,255,0.7)", lineHeight: 1.8 }}>
                Envíos asegurados a Bogotá, Medellín, Cali, Barranquilla y el
                resto del país.
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
  tone: "charcoal" | "gold" | "rose" | "ivory";
  slug: string;
}

function CategoryCard({ title, subtitle, tone, slug }: CategoryCardProps) {
  return (
    <Box
      component={Link}
      to={`/productos?categoria=${slug}`}
      sx={{
        textDecoration: "none",
        color: "inherit",
        display: "block",
        transition: "transform 0.25s ease, box-shadow 0.25s ease",
        boxShadow: "0 8px 24px rgba(26,16,16,0.08)",
        "&:hover": { transform: "translateY(-6px)", boxShadow: "0 18px 40px rgba(26,16,16,0.14)" },
      }}
    >
      <PhotoPlaceholder alt={title} tone={tone} sx={{ width: "100%", height: 300 }} />

      <Box sx={{ backgroundColor: "#fff", p: 2.5 }}>
        <Typography sx={{ fontFamily: '"Fraunces", serif', fontSize: 20, fontWeight: 500, mb: 0.5, color: palette.charcoal }}>
          {title}
        </Typography>
        <Typography sx={{ color: palette.textSecondary, fontSize: 13 }}>{subtitle}</Typography>
      </Box>
    </Box>
  );
}

function NewArrivalCard({ product }: { product: Product }) {
  const image = product.images?.find((x) => x.isPrimary)?.imageUrl ?? product.images?.[0]?.imageUrl;

  return (
    <Box component={Link} to={`/productos/${product.id}`} sx={{ textDecoration: "none", color: "inherit", display: "block" }}>
      <Box sx={{ aspectRatio: "1 / 1", backgroundColor: palette.ivoryDeep, overflow: "hidden", mb: 1.5 }}>
        {image && (
          <Box
            component="img"
            src={image}
            alt={product.name}
            sx={{ width: "100%", height: "100%", objectFit: "cover", transition: "transform .4s ease", "&:hover": { transform: "scale(1.04)" } }}
          />
        )}
      </Box>

      <Typography sx={{ fontSize: 11, color: palette.textSecondary, textTransform: "uppercase", letterSpacing: 1 }}>
        {product.brand?.name}
      </Typography>
      <Typography sx={{ fontSize: 15, mt: 0.5, mb: 0.5, color: palette.charcoal }}>{product.name}</Typography>

      <Box sx={{ display: "flex", alignItems: "center", gap: 0.5, mb: 0.5 }}>
        <Rating value={product.averageRating ?? 0} precision={0.1} readOnly size="small" />
      </Box>

      <Typography sx={{ fontWeight: 600, color: palette.charcoal }}>
        ${product.price.toLocaleString("es-CO")}
      </Typography>
    </Box>
  );
}
