import { createTheme } from "@mui/material/styles";

export const theme = createTheme({
  palette: {
    background: {
      default: "#ffffff",
    },
    primary: {
      main: "#1f1f1f",
    },
    secondary: {
      main: "#c8a97e",
    },
  },

  typography: {
    fontFamily: '"Inter", sans-serif',

    h1: {
      fontWeight: 500,
    },

    h2: {
      fontWeight: 500,
    },

    button: {
      textTransform: "none",
    },
  },

  shape: {
    borderRadius: 4,
  },
});
