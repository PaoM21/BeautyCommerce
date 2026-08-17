import { createTheme } from "@mui/material/styles";

// Paleta editorial de lujo: base marfil/carbón + acento oro-bronce,
// pensada para transmitir exclusividad (skincare/makeup premium).
export const palette = {
  ivory: "#f7f3ee",
  ivoryDeep: "#efe8df",
  charcoal: "#1a1714",
  charcoalSoft: "#2b2620",
  gold: "#a9805a",
  goldDeep: "#8a6540",
  goldLight: "#d9c2a3",
  rose: "#e7cfc6",
  textSecondary: "#6f6a63",
  border: "#e6ddd1",
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
