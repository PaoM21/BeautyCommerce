import { Box, Container, Typography } from "@mui/material";
import type { ReactNode } from "react";

import { palette } from "../../theme/theme";

interface LegalPageLayoutProps {
  eyebrow: string;
  title: string;
  lastUpdated: string;
  children: ReactNode;
}

export default function LegalPageLayout({
  eyebrow,
  title,
  lastUpdated,
  children,
}: LegalPageLayoutProps) {
  return (
    <Container maxWidth="md" sx={{ py: { xs: 8, md: 10 } }}>
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
        {eyebrow}
      </Typography>

      <Typography
        component="h1"
        variant="h1"
        sx={{ fontSize: { xs: 32, md: 44 }, color: palette.charcoal, mb: 1.5 }}
      >
        {title}
      </Typography>

      <Typography sx={{ fontSize: 13, color: palette.textSecondary, mb: 6 }}>
        Última actualización: {lastUpdated}
      </Typography>

      <Box
        sx={{
          "& h2": {
            fontFamily: '"Fraunces", serif',
            fontWeight: 500,
            fontSize: 22,
            color: palette.charcoal,
            mt: 5,
            mb: 2,
          },
          "& p": {
            fontSize: 15,
            lineHeight: 1.8,
            color: "#3a342c",
            mb: 2,
          },
          "& ul": {
            pl: 3,
            mb: 2,
          },
          "& li": {
            fontSize: 15,
            lineHeight: 1.8,
            color: "#3a342c",
            mb: 1,
          },
          "& strong": {
            color: palette.charcoal,
          },
          "& .placeholder": {
            backgroundColor: "#fdf0d5",
            color: "#8a5a00",
            padding: "1px 6px",
            fontWeight: 600,
          },
        }}
      >
        {children}
      </Box>
    </Container>
  );
}
