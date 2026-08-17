import { Box, type SxProps, type Theme } from "@mui/material";
import { palette } from "../../theme/theme";

type Tone = "charcoal" | "gold" | "rose" | "ivory";

const GRADIENTS: Record<Tone, string> = {
  charcoal: `linear-gradient(135deg, ${palette.charcoalSoft} 0%, ${palette.charcoal} 100%)`,
  gold: `linear-gradient(135deg, ${palette.goldLight} 0%, ${palette.gold} 100%)`,
  rose: `linear-gradient(135deg, ${palette.rose} 0%, ${palette.roseDeep} 100%)`,
  ivory: `linear-gradient(135deg, ${palette.ivory} 0%, ${palette.ivoryDeep} 100%)`,
};

interface PhotoPlaceholderProps {
  /**
   * URL de la fotografía real. Mientras no exista, se muestra un
   * degradado editorial de marca en su lugar — basta con pasar
   * `src` (p. ej. "/images/hero-campana.jpg") para reemplazarlo.
   */
  src?: string;
  alt: string;
  tone?: Tone;
  sx?: SxProps<Theme>;
}

export default function PhotoPlaceholder({
  src,
  alt,
  tone = "charcoal",
  sx,
}: PhotoPlaceholderProps) {
  if (src) {
    return (
      <Box
        component="img"
        src={src}
        alt={alt}
        sx={{ objectFit: "cover", ...sx }}
      />
    );
  }

  return (
    <Box
      role="img"
      aria-label={alt}
      sx={{
        position: "relative",
        background: GRADIENTS[tone],
        overflow: "hidden",
        "&::after": {
          content: '""',
          position: "absolute",
          inset: 0,
          background:
            "radial-gradient(circle at 30% 20%, rgba(255,255,255,0.14), transparent 55%)",
        },
        ...sx,
      }}
    />
  );
}
