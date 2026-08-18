import { createTheme } from "@mui/material/styles";

// Paleta editorial blush: crema + rosa empolvado + acentos en negro
// y maroon oscuro, inspirada en la referencia visual de la marca.
export const palette = {
  ivory: "#faf3ee", // crema (fondo general)
  ivoryDeep: "#f0e3d8", // crema profundo (tarjetas, inputs)
  blush: "#f8e1e3", // rosa empolvado (fondos de sección)
  blushDeep: "#f0c7cc", // rosa empolvado profundo (hover, bandas)
  charcoal: "#4a1420", // maroon oscuro (barra de anuncio, footer)
  charcoalSoft: "#661d2c", // maroon (hover de fondos oscuros)
  ink: "#18130f", // negro casi puro (botones sólidos tipo CTA)
  inkSoft: "#332a24", // negro suavizado (hover de botones)
  gold: "#c9707f", // rosa acento (texto de marca, eyebrow, hover)
  goldDeep: "#a8505f", // rosa acento oscuro (hover de acentos)
  goldLight: "#e3aeb8", // rosa acento claro (sobre fondo oscuro)
  rose: "#e4c9ae", // beige rosado (compat)
  roseDeep: "#c9a37e", // beige rosado profundo (compat)
  textSecondary: "#7a6a63",
  border: "#ecdcd6",
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
