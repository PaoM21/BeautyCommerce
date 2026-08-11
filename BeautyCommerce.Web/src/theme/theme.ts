import { createTheme } from "@mui/material/styles";

export const theme = createTheme({
  palette: {
    background: {
      default: "#f7f5f2", // soft beige
    },
    primary: {
      main: "#1f1f1f", // dark for CTAs
      contrastText: "#ffffff",
    },
    secondary: {
      main: "#e7cfc6", // soft rose/beige
      contrastText: "#1f1f1f",
    },
    text: {
      primary: "#202020",
      secondary: "#777777",
    },
  },

  typography: {
    fontFamily: '"Inter", sans-serif',

    h1: {
      fontWeight: 400,
    },

    h2: {
      fontWeight: 400,
    },

    button: {
      textTransform: "none",
    },
  },

  shape: {
    borderRadius: 8,
  },

  components: {
    MuiButton: {
      styleOverrides: {
        contained: {
          borderRadius: 6,
          padding: "10px 26px",
          backgroundColor: "#1f1f1f",
        },
      },
    },
  },
});
