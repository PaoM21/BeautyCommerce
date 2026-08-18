import { createTheme } from "@mui/material/styles";

// Paleta boutique: blanco + crema + rosa empolvado + negro casi puro,
// calibrada contra la referencia visual de la marca (hex exactos).
export const palette = {
  ivory: "#f5efea", // crema/beige (fondo general)
  ivoryDeep: "#efe4da", // crema profundo (tarjetas, inputs)
  blush: "#f7e8e7", // rosa empolvado (fondos de sección)
  blushDeep: "#f4dfde", // rosa empolvado profundo (hover, bandas)
  charcoal: "#171717", // negro (footer, texto fuerte) — sin vinotinto
  charcoalSoft: "#333333", // negro suavizado
  ink: "#171717", // negro casi puro (botones sólidos, texto principal)
  inkSoft: "#333333", // negro suavizado (hover de botones)
  gold: "#d99aa3", // rosa empolvado (texto de marca, eyebrow, hover)
  goldDeep: "#c17b85", // rosa empolvado oscuro (hover de acentos)
  goldLight: "#e6bcc2", // rosa empolvado claro (sobre fondo oscuro)
  rose: "#e4c9ae", // beige rosado (compat)
  roseDeep: "#c9a37e", // beige rosado profundo (compat)
  textSecondary: "#777777",
  border: "#e7e7e7",
};

declare module "@mui/material/styles" {
  interface Palette {
    accent: Palette["primary"];
  }
  interface PaletteOptions {
    accent?: PaletteOptions["primary"];
  }
}

export const theme = createTheme({
  palette: {
    background: {
      default: palette.ivory,
      paper: "#ffffff",
    },
    primary: {
      main: palette.ink,
      light: palette.inkSoft,
      contrastText: "#ffffff",
    },
    secondary: {
      main: palette.rose,
      contrastText: palette.charcoal,
    },
    accent: {
      main: palette.gold,
      dark: palette.goldDeep,
      light: palette.goldLight,
      contrastText: "#ffffff",
    },
    text: {
      primary: "#211d19",
      secondary: palette.textSecondary,
    },
    divider: palette.border,
  },

  typography: {
    fontFamily: '"Inter", "Helvetica Neue", sans-serif',

    h1: {
      fontFamily: '"Fraunces", "Georgia", serif',
      fontWeight: 500,
      letterSpacing: -1,
    },

    h2: {
      fontFamily: '"Fraunces", "Georgia", serif',
      fontWeight: 500,
      letterSpacing: -0.5,
    },

    h3: {
      fontFamily: '"Fraunces", "Georgia", serif',
      fontWeight: 500,
    },

    button: {
      textTransform: "none",
      fontWeight: 500,
      letterSpacing: 0.3,
    },
  },

  shape: {
    borderRadius: 4,
  },

  components: {
    MuiButton: {
      styleOverrides: {
        root: {
          borderRadius: 2,
        },
        contained: {
          borderRadius: 2,
          padding: "12px 32px",
          backgroundColor: palette.ink,
          boxShadow: "none",
          "&:hover": {
            backgroundColor: palette.inkSoft,
            boxShadow: "none",
          },
        },
        outlined: {
          borderRadius: 2,
          borderColor: palette.ink,
          padding: "11px 31px",
        },
      },
    },
  },
});
