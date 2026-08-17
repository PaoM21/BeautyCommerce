import { createTheme } from "@mui/material/styles";

// Paleta editorial de lujo: base beige + vinotinto,
// pensada para transmitir exclusividad (skincare/makeup premium)
// sin perder la densidad comercial de un ecommerce real.
export const palette = {
  ivory: "#f6efe4", // beige
  ivoryDeep: "#e9dcc4", // beige profundo
  charcoal: "#3d0f18", // vinotinto casi negro (fondos oscuros, CTAs)
  charcoalSoft: "#5c1a28", // vinotinto oscuro (hover de fondos oscuros)
  gold: "#7d2438", // vinotinto principal (acentos, texto de marca)
  goldDeep: "#5c1a28", // vinotinto oscuro (hover de acentos)
  goldLight: "#c68a9a", // vinotinto claro (acentos sobre fondo oscuro)
  rose: "#e4c9ae", // beige rosado
  roseDeep: "#c9a37e", // beige rosado profundo
  textSecondary: "#75695a",
  border: "#e3d2ae",
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
      main: palette.charcoal,
      light: palette.charcoalSoft,
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
          backgroundColor: palette.charcoal,
          boxShadow: "none",
          "&:hover": {
            backgroundColor: palette.charcoalSoft,
            boxShadow: "none",
          },
        },
        outlined: {
          borderRadius: 2,
          borderColor: palette.charcoal,
          padding: "11px 31px",
        },
      },
    },
  },
});
