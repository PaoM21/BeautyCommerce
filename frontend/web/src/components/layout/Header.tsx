import {
  AppBar,
  Badge,
  Box,
  Container,
  IconButton,
  Toolbar,
  Typography,
} from "@mui/material";

import SearchIcon from "@mui/icons-material/Search";
import FavoriteBorderIcon from "@mui/icons-material/FavoriteBorder";
import ShoppingBagOutlinedIcon from "@mui/icons-material/ShoppingBagOutlined";
import PersonOutlineIcon from "@mui/icons-material/Person";

import { Link } from "react-router-dom";
import { useCartStore } from "../../store/cartStore";
import { useAuthStore } from "../../store/authStore";
import { palette } from "../../theme/theme";

export default function Header() {
  const itemCount = useCartStore((state) => state.itemCount);
  const token = useAuthStore((state) => state.token);
  const logout = useAuthStore((state) => state.logout);
  return (
    <AppBar
      position="sticky"
      elevation={0}
      sx={{
        backgroundColor: "#ffffff",
        color: palette.charcoal,
        borderBottom: `1px solid ${palette.border}`,
      }}
    >
      <Container maxWidth="xl">
        <Toolbar
          disableGutters
          sx={{
            minHeight: 76,
            display: "flex",
            justifyContent: "space-between",
          }}
        >
          {/* LOGO */}
          <Box
            component={Link}
            to="/"
            sx={{
              textDecoration: "none",
              color: "inherit",
              minWidth: 180,
            }}
          >
            <Typography
              sx={{
                fontFamily: '"Fraunces", serif',
                fontSize: 23,
                fontWeight: 500,
                letterSpacing: 2,
              }}
            >
              BEAUTY
            </Typography>

            <Typography
              sx={{
                fontSize: 10,
                letterSpacing: 5,
                color: palette.gold,
                mt: -0.5,
              }}
            >
              COMMERCE
            </Typography>
          </Box>

          {/* MENU */}
          <Box
            sx={{
              display: { xs: "none", lg: "flex" },
              alignItems: "center",
              gap: 3,
            }}
          >
            <NavItem to="/" label="Inicio" />

            <NavItem
              to="/productos?categoria=cabello"
              label="Cabello"
            />

            <NavItem
              to="/productos?categoria=rostro"
              label="Rostro"
            />

            <NavItem
              to="/productos?categoria=piel"
              label="Piel"
            />

            <NavItem
              to="/productos?categoria=labios"
              label="Labios"
            />

            <NavItem
              to="/productos?categoria=unas"
              label="Uñas"
            />

            <NavItem
              to="/productos?novedades=true"
              label="Novedades"
            />
          </Box>

          {/* ICONOS */}
          <Box
            sx={{
              display: "flex",
              alignItems: "center",
              gap: 0.5,
              minWidth: 180,
              justifyContent: "flex-end",
            }}
          >
            <IconButton>
              <SearchIcon />
            </IconButton>

            <IconButton>
              <Badge badgeContent={0}>
                <FavoriteBorderIcon />
              </Badge>
            </IconButton>

            <IconButton component={Link} to="/carrito">
              <Badge badgeContent={itemCount}>
                <ShoppingBagOutlinedIcon />
              </Badge>
            </IconButton>

            <IconButton
              onClick={() => {
                if (token) {
                  logout();
                } else {
                  window.location.href = "/login";
                }
              }}
            >
              <PersonOutlineIcon />
            </IconButton>
          </Box>
        </Toolbar>
      </Container>
    </AppBar>
  );
}

interface NavItemProps {
  to: string;
  label: string;
}

function NavItem({ to, label }: NavItemProps) {
  return (
    <Typography
      component={Link}
      to={to}
      sx={{
        textDecoration: "none",
        color: "#333333",
        fontSize: 14,
        fontWeight: 400,
        whiteSpace: "nowrap",
        transition: "color 0.2s ease",
        "&:hover": {
          color: palette.gold,
        },
      }}
    >
      {label}
    </Typography>
  );
}
