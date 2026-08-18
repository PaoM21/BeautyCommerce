import { useState, type MouseEvent } from "react";
import { Box, Button, Container, IconButton, Rating, Typography } from "@mui/material";
import { Link } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";

import LocalShippingOutlinedIcon from "@mui/icons-material/LocalShippingOutlined";
import LockOutlinedIcon from "@mui/icons-material/LockOutlined";
import CardGiftcardOutlinedIcon from "@mui/icons-material/CardGiftcardOutlined";
import ContentCutOutlinedIcon from "@mui/icons-material/ContentCutOutlined";
import FaceRetouchingNaturalOutlinedIcon from "@mui/icons-material/FaceRetouchingNaturalOutlined";
import WaterDropOutlinedIcon from "@mui/icons-material/WaterDropOutlined";
import ColorLensOutlinedIcon from "@mui/icons-material/ColorLensOutlined";
import BackHandOutlinedIcon from "@mui/icons-material/BackHandOutlined";
import WbSunnyOutlinedIcon from "@mui/icons-material/WbSunnyOutlined";
import VisibilityOutlinedIcon from "@mui/icons-material/VisibilityOutlined";
import AddShoppingCartOutlinedIcon from "@mui/icons-material/AddShoppingCartOutlined";
import FormatQuoteOutlinedIcon from "@mui/icons-material/FormatQuoteOutlined";
import FavoriteBorderIcon from "@mui/icons-material/FavoriteBorder";
import FavoriteIcon from "@mui/icons-material/Favorite";

import Seo from "../../components/seo/Seo";
import PhotoPlaceholder from "../../components/media/PhotoPlaceholder";
import { palette } from "../../theme/theme";
import { getProducts, getBestSellers } from "../../services/productService";
import { getBrands } from "../../services/catalogService";
import { getFeaturedReviews } from "../../services/reviewService";
import { addCartItem } from "../../services/cartService";
import { useCartStore } from "../../store/cartStore";
import { useWishlist } from "../../hooks/useWishlist";
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
  { icon: WbSunnyOutlinedIcon, label: "Protección solar", slug: "piel" },
  { icon: ColorLensOutlinedIcon, label: "Labios statement", slug: "labios" },
  { icon: VisibilityOutlinedIcon, label: "Mirada intensa", slug: "rostro" },
  { icon: ContentCutOutlinedIcon, label: "Rituales capilares", slug: "cabello" },
  { icon: BackHandOutlinedIcon, label: "Manicure de salón", slug: "unas" },
];

const TRUST_POINTS = [
  { icon: LocalShippingOutlinedIcon, title: "Envíos nacionales", text: "A todo el país, con tiempos rápidos." },
  { icon: LockOutlinedIcon, title: "Pago seguro", text: "Tus pagos están protegidos." },
  { icon: CardGiftcardOutlinedIcon, title: "Empaque hermoso", text: "Cuidamos cada detalle para ti." },
];

const INSPIRATION_TONES = ["gold", "rose", "charcoal", "ivory", "rose", "gold"] as const;

const HOME_JSON_LD = {
  "@context": "https://schema.org",
  "@type": "Organization",
  name: "HALDY&CO ECOMMERCE",
  url: "https://www.haldyco.com",
  logo: "https://www.haldyco.com/favicon.svg",
  description:
    "Ecommerce de belleza de lujo en Colombia: maquillaje, skincare, cabello, uñas y labios de marcas premium.",
  areaServed: "CO",
};

export default function Home() {
  const { data: newArrivals = [] } = useQuery({
    queryKey: ["products", "home-new-arrivals"],
    queryFn: () => getProducts(),
  });

  const { data: bestSellers = [] } = useQuery({
    queryKey: ["products", "home-best-sellers"],
    queryFn: () => getBestSellers(8),
  });

  const { data: brands = [] } = useQuery({
    queryKey: ["brands", "home-brand-strip"],
    queryFn: () => getBrands(),
  });

  const { data: testimonials = [] } = useQuery({
    queryKey: ["reviews", "home-featured"],
    queryFn: () => getFeaturedReviews(5),
  });

  return (
    <Box>
      <Seo
        title="Maquillaje y skincare de lujo en Colombia"
        description="Descubre una selección curada de maquillaje, skincare, cuidado del cabello, uñas y labios de marcas premium. Envíos a toda Colombia, empaque de lujo y asesoría personalizada."
        path="/"
        jsonLd={HOME_JSON_LD}
      />

      {/* HERO — split: texto + foto de campaña (reemplazar PhotoPlaceholder por foto real) */}
      <Box
        sx={{
          display: "grid",
          gridTemplateColumns: { xs: "1fr", md: "1fr 1.2fr" },
          minHeight: { xs: "auto", md: 560 },
          backgroundColor: palette.ivory,
        }}
      >
        <Box
          sx={{
            display: "flex",
            flexDirection: "column",
            justifyContent: "center",
            px: { xs: 4, md: 8 },
            py: { xs: 7, md: 0 },
          }}
        >
          <Typography
            component="h1"
            variant="h1"
            sx={{ fontSize: { xs: 34, md: 52 }, lineHeight: 1.15, color: palette.charcoal, mb: 3 }}
          >
            Descubre la{" "}
            <Box component="span" sx={{ color: palette.gold }}>
              belleza
            </Box>{" "}
            que resalta tu esencia.
          </Typography>

          <Button
            component={Link}
            to="/productos"
            variant="contained"
            size="large"
            sx={{ alignSelf: "flex-start", px: 5, py: 1.7, letterSpacing: 1 }}
          >
            Explorar colección
          </Button>
        </Box>

        <Box sx={{ position: "relative", minHeight: { xs: 320, md: "auto" } }}>
          <PhotoPlaceholder
            alt="Campaña HALDY&CO ECOMMERCE"
            tone="rose"
            sx={{ width: "100%", height: "100%" }}
          />
        </Box>
      </Box>

      {/* CATEGORÍAS */}
      <Container maxWidth="xl" sx={{ py: { xs: 8, md: 10 } }}>
        <Typography
          sx={{ fontSize: 13, letterSpacing: 3, textTransform: "uppercase", color: palette.charcoal, mb: 5, textAlign: "center", fontWeight: 500 }}
        >
          Categorías principales
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

      {/* MÁS VENDIDOS — ranking real por unidades vendidas, se oculta si aún no hay ventas */}
      {bestSellers.length > 0 && (
        <Container maxWidth="xl" sx={{ py: { xs: 6, md: 8 } }}>
          <SectionHeader eyebrow={null} title="Más vendidos" to="/productos" />

          <Box
            sx={{
              display: "grid",
              gridTemplateColumns: { xs: "1fr 1fr", sm: "repeat(3, 1fr)", lg: "repeat(4, 1fr)" },
              gap: 3,
            }}
          >
            {bestSellers.map((product) => (
              <BestSellerCard key={product.id} product={product} />
            ))}
          </Box>
        </Container>
      )}

      {/* NOVEDADES — catálogo real (se oculta si aún no hay productos publicados) */}
      {newArrivals.length > 0 && (
        <Box sx={{ backgroundColor: palette.blush }}>
          <Container maxWidth="xl" sx={{ py: { xs: 6, md: 8 } }}>
            <SectionHeader eyebrow={null} title="Novedades" to="/productos" />

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
        </Box>
      )}

      {/* MARCAS QUE AMAMOS — marcas reales del catálogo */}
      {brands.length > 0 && (
        <Box sx={{ backgroundColor: "#ffffff", borderTop: `1px solid ${palette.border}`, borderBottom: `1px solid ${palette.border}` }}>
          <Container maxWidth="xl" sx={{ py: { xs: 5, md: 6 } }}>
            <Typography
              sx={{ fontSize: 13, letterSpacing: 3, textTransform: "uppercase", color: palette.textSecondary, mb: 3.5, textAlign: "center" }}
            >
              Marcas que amamos
            </Typography>

            <Box sx={{ display: "flex", flexWrap: "wrap", justifyContent: "center", gap: { xs: 3, md: 6 } }}>
              {brands.map((brand) => (
                <Typography
                  key={brand.id}
                  sx={{
                    fontFamily: '"Fraunces", serif',
                    fontSize: { xs: 16, md: 19 },
                    color: palette.charcoal,
                    opacity: 0.55,
                    transition: "opacity 0.2s ease",
                    "&:hover": { opacity: 1 },
                  }}
                >
                  {brand.name}
                </Typography>
              ))}
            </Box>
          </Container>
        </Box>
      )}

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
              gridTemplateColumns: { xs: "repeat(2, 1fr)", sm: "repeat(4, 1fr)", lg: "repeat(7, 1fr)" },
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

      {/* INSPIRACIÓN — galería editorial (placeholders hasta tener fotografía real) */}
      <Container maxWidth="xl" sx={{ py: { xs: 7, md: 9 } }}>
        <SectionHeader eyebrow={null} title="Inspiración" to="/productos" ctaLabel="Ver más" />

        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: { xs: "1fr 1fr", sm: "repeat(3, 1fr)", lg: "repeat(6, 1fr)" },
            gap: 2,
          }}
        >
          {INSPIRATION_TONES.map((tone, index) => (
            <PhotoPlaceholder
              key={index}
              alt={`Inspiración de belleza ${index + 1}`}
              tone={tone}
              sx={{ width: "100%", aspectRatio: "3 / 4" }}
            />
          ))}
        </Box>
      </Container>

      {/* LO QUE DICEN NUESTRAS CLIENTAS — reseñas reales, se oculta mientras no existan */}
      {testimonials.length > 0 && (
        <Box sx={{ backgroundColor: palette.blush }}>
          <Container maxWidth="xl" sx={{ py: { xs: 8, md: 10 } }}>
            <Typography
              component="h2"
              variant="h2"
              sx={{ fontSize: { xs: 24, md: 30 }, mb: 6, color: palette.charcoal, textAlign: "center" }}
            >
              Lo que dicen nuestras clientas
            </Typography>

            <Box
              sx={{
                display: "grid",
                gridTemplateColumns: { xs: "1fr", sm: "repeat(2, 1fr)", lg: "repeat(3, 1fr)" },
                gap: 3,
              }}
            >
              {testimonials.map((review) => (
                <Box key={review.id} sx={{ backgroundColor: "#fff", p: 4 }}>
                  <FormatQuoteOutlinedIcon sx={{ color: palette.goldLight, fontSize: 28, mb: 1 }} />
                  <Rating value={review.rating} readOnly size="small" sx={{ mb: 1.5, display: "block" }} />
                  <Typography sx={{ fontSize: 14.5, lineHeight: 1.75, color: palette.charcoal, mb: 2.5 }}>
                    "{review.comment}"
                  </Typography>
                  <Typography sx={{ fontSize: 13.5, fontWeight: 600, color: palette.charcoal }}>
                    {review.userName}
                  </Typography>
                  <Typography sx={{ fontSize: 12.5, color: palette.textSecondary }}>
                    Sobre {review.productName}
                  </Typography>
                </Box>
              ))}
            </Box>
          </Container>
        </Box>
      )}

      {/* TRUST BAR */}
      <Box sx={{ backgroundColor: palette.blushDeep }}>
        <Container maxWidth="xl">
          <Box
            sx={{
              display: "grid",
              gridTemplateColumns: { xs: "1fr", sm: "repeat(3, 1fr)" },
              gap: { xs: 3, sm: 0 },
              py: { xs: 5, md: 5 },
            }}
          >
            {TRUST_POINTS.map(({ icon: Icon, title, text }, index) => (
              <Box
                key={title}
                sx={{
                  display: "flex",
                  gap: 1.5,
                  alignItems: "center",
                  justifyContent: { xs: "flex-start", sm: "center" },
                  borderLeft: index > 0 ? { xs: "none", sm: `1px solid ${palette.charcoalSoft}` } : "none",
                  opacity: 0.85,
                }}
              >
                <Icon sx={{ color: palette.charcoal, fontSize: 26 }} />
                <Box>
                  <Typography sx={{ fontWeight: 600, fontSize: 14, mb: 0.2, color: palette.charcoal }}>{title}</Typography>
                  <Typography sx={{ fontSize: 12.5, color: palette.charcoal }}>{text}</Typography>
                </Box>
              </Box>
            ))}
          </Box>
        </Container>
      </Box>
    </Box>
  );
}

function SectionHeader({
  title,
  to,
  ctaLabel = "Ver todos",
}: {
  eyebrow: string | null;
  title: string;
  to: string;
  ctaLabel?: string;
}) {
  return (
    <Box sx={{ display: "flex", justifyContent: "space-between", alignItems: "flex-end", mb: 4 }}>
      <Typography component="h2" variant="h2" sx={{ fontSize: { xs: 24, md: 30 }, color: palette.charcoal }}>
        {title}
      </Typography>

      <Button component={Link} to={to} sx={{ display: { xs: "none", sm: "inline-flex" }, color: palette.gold, fontSize: 13 }}>
        {ctaLabel}
      </Button>
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

function useQuickAdd(defaultVariantId?: string) {
  const [adding, setAdding] = useState(false);
  const queryClient = useQueryClient();
  const increment = useCartStore((state) => state.increment);

  const handleQuickAdd = async (event: MouseEvent) => {
    event.preventDefault();
    event.stopPropagation();

    if (!defaultVariantId || adding) return;

    try {
      setAdding(true);

      await addCartItem({ productVariantId: defaultVariantId, quantity: 1 });

      await queryClient.invalidateQueries({ queryKey: ["shopping-cart"] });
      increment(1);
    } catch (error) {
      console.error(error);
      alert("No fue posible agregar el producto al carrito.");
    } finally {
      setAdding(false);
    }
  };

  return { adding, handleQuickAdd };
}

function BestSellerCard({ product }: { product: Product }) {
  const image = product.images?.find((x) => x.isPrimary)?.imageUrl ?? product.images?.[0]?.imageUrl;
  const { adding, handleQuickAdd } = useQuickAdd(product.defaultVariantId);
  const { isFavorite, toggleFavorite } = useWishlist();

  return (
    <Box component={Link} to={`/productos/${product.id}`} sx={{ textDecoration: "none", color: "inherit", display: "block" }}>
      <Box sx={{ position: "relative", aspectRatio: "1 / 1", backgroundColor: palette.ivoryDeep, overflow: "hidden", mb: 1.5 }}>
        {image && (
          <Box
            component="img"
            src={image}
            alt={product.name}
            sx={{ width: "100%", height: "100%", objectFit: "cover" }}
          />
        )}

        <IconButton
          onClick={(event) => {
            event.preventDefault();
            event.stopPropagation();
            toggleFavorite(product.id);
          }}
          aria-label="Agregar a favoritos"
          size="small"
          sx={{ position: "absolute", top: 8, right: 8, backgroundColor: "#fff", "&:hover": { backgroundColor: "#fff" } }}
        >
          {isFavorite(product.id) ? <FavoriteIcon fontSize="small" sx={{ color: palette.gold }} /> : <FavoriteBorderIcon fontSize="small" />}
        </IconButton>
      </Box>

      <Typography sx={{ fontSize: 11, color: palette.textSecondary, textTransform: "uppercase", letterSpacing: 1 }}>
        {product.brand?.name}
      </Typography>
      <Typography sx={{ fontSize: 15, mt: 0.5, mb: 0.5, color: palette.charcoal }}>{product.name}</Typography>

      <Box sx={{ display: "flex", alignItems: "center", gap: 0.5, mb: 0.5 }}>
        <Rating value={product.averageRating ?? 0} precision={0.1} readOnly size="small" />
        {(product.reviewCount ?? 0) > 0 && (
          <Typography sx={{ fontSize: 12, color: palette.textSecondary }}>({product.reviewCount})</Typography>
        )}
      </Box>

      <Typography sx={{ fontWeight: 600, color: palette.charcoal, mb: 1.5 }}>
        ${product.price.toLocaleString("es-CO")}
      </Typography>

      {product.defaultVariantId && (
        <Button
          onClick={handleQuickAdd}
          disabled={adding}
          fullWidth
          variant="contained"
          startIcon={<AddShoppingCartOutlinedIcon fontSize="small" />}
          sx={{ fontSize: 12, py: 1.1 }}
        >
          Agregar al carrito
        </Button>
      )}
    </Box>
  );
}

function NewArrivalCard({ product }: { product: Product }) {
  const image = product.images?.find((x) => x.isPrimary)?.imageUrl ?? product.images?.[0]?.imageUrl;

  return (
    <Box component={Link} to={`/productos/${product.id}`} sx={{ textDecoration: "none", color: "inherit", display: "block" }}>
      <Box sx={{ position: "relative", aspectRatio: "1 / 1", backgroundColor: "#fff", overflow: "hidden", mb: 1.5 }}>
        <Box
          sx={{
            position: "absolute",
            top: 10,
            left: 10,
            zIndex: 1,
            backgroundColor: palette.gold,
            color: "#fff",
            fontSize: 10,
            fontWeight: 600,
            letterSpacing: 1,
            px: 1.2,
            py: 0.4,
            borderRadius: 4,
          }}
        >
          NUEVO
        </Box>

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

      <Typography sx={{ fontWeight: 600, color: palette.charcoal }}>
        ${product.price.toLocaleString("es-CO")}
      </Typography>
    </Box>
  );
}
