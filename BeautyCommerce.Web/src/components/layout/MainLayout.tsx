import { Box } from "@mui/material";
import { Outlet } from "react-router-dom";

import Header from "./Header";

export default function MainLayout() {
  return (
    <Box
      sx={{
        minHeight: "100vh",
        backgroundColor: "#ffffff",
      }}
    >
      <Header />

      <Box component="main">
        <Outlet />
      </Box>
    </Box>
  );
}
