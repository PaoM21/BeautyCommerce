import { useState, type FormEvent } from "react";
import {
  AppBar,
  Badge,
  Box,
  Container,
  IconButton,
  InputBase,
  Toolbar,
  Typography,
} from "@mui/material";

import SearchIcon from "@mui/icons-material/Search";
import FavoriteBorderIcon from "@mui/icons-material/FavoriteBorder";
import ShoppingBagOutlinedIcon from "@mui/icons-material/ShoppingBagOutlined";
import PersonOutlineIcon from "@mui/icons-material/Person";
import AdminPanelSettingsOutlinedIcon from "@mui/icons-material/AdminPanelSettingsOutlined";

import { Link, useLocation, useNavigate } from "react-router-dom";
import { useCartStore } from "../../store/cartStore";
import { useAuthStore } from "../../store/authStore";
import { palette } from "../../theme/theme";

export default function Header() {
  const itemCount = useCartStore((state) => state.itemCount);
  const token = useAuthStore((state) => state.token);
  const user = useAuthStore((state) => state.user);
  const logout = useAuthStore((state) => state.logout);
  const isAdmin = user?.role === "Admin";

  const navigate = useNavigate();
  const [searchValue, setSearchValue] = useState("");

  const handleSearchSubmit = (event: FormEvent) => {
    event.preventDefault();

    const term = searchValue.trim();
    if (!term) return;

    navigate(`/productos?buscar=${encodeURIComponent(term)}`);
  };

  return (
    <Box sx={{ position: "sticky", top: 0, zIndex: 1100 }}>
      <AppBar
        position="static"
        elevation={0}
        sx={{
          backgroundColor: "#ffffff",
          color: palette.ink,
          borderBottom: `1px solid ${palette.border}`,
        }}
      >
        <Container maxWidth="xl">
          <Toolbar
            disableGutters
            sx={{
              minHeight: 64,
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
                  fontSize: 21,
                  fontWeight: 500,
                  letterSpacing: 1,
                  whiteSpace: "nowrap",
                }}
              >
                HALDY&amp;CO
              </Typography>

              <Typography
                sx={{
                  fontSize: 10,
                  letterSpacing: 5,
                  color: "#999999",
                  mt: -0.5,
                }}
              >
                ECOMMERCE
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

              <NavItem to="/productos?categoria=cabello" label="Cabello" />

              <NavItem to="/productos?categoria=rostro" label="Rostro" />

              <NavItem to="/productos?categoria=piel" label="Piel" />

              <NavItem to="/productos?categoria=labios" label="Labios" />

              <NavItem to="/productos?categoria=unas" label="Uñas" />

              <NavItem to="/productos" label="Novedades" />
            </Box>

            {/* BUSQUEDA + ICONOS */}
            <Box
              sx={{
                display: "flex",
                alignItems: "center",
                gap: 1.5,
                justifyContent: "flex-end",
              }}
            >
              <Box
                component="form"
                onSubmit={handleSearchSubmit}
                sx={{
                  display: { xs: "none", sm: "flex" },
                  alignItems: "center",
                  backgroundColor: "#ffffff",
                  border: "1px solid #eeeeee",
                  borderRadius: "4px",
                  px: 1.2,
                  py: 0.5,
                }}
              >
                <SearchIcon sx={{ fontSize: 15, color: "#999999", mr: 0.8 }} />
                <InputBase
                  placeholder="Buscar"
                  value={searchValue}
                  onChange={(event) => setSearchValue(event.target.value)}
                  sx={{ fontSize: 12.5, width: { sm: 90, md: 130 } }}
                />
              </Box>

              <IconButton component={Link} to="/favoritos">
                <Badge badgeContent={0}>
                  <FavoriteBorderIcon />
                </Badge>
              </IconButton>

              <IconButton component={Link} to="/carrito">
                <Badge badgeContent={itemCount}>
                  <ShoppingBagOutlinedIcon />
                </Badge>
              </IconButton>

              {isAdmin && (
                <IconButton
                  component={Link}
                  to="/admin"
                  title="Panel de administración"
                  sx={{ color: palette.gold }}
                >
                  <AdminPanelSettingsOutlinedIcon />
                </IconButton>
              )}

              <Box
                onClick={() => {
                  if (token) {
                    logout();
                  } else {
                    window.location.href = "/login";
                  }
                }}
                sx={{
                  display: "flex",
                  alignItems: "center",
                  gap: 0.5,
                  cursor: "pointer",
                  color: "inherit",
                  pl: 0.5,
                }}
              >
                <IconButton sx={{ p: 0.5 }}>
                  <PersonOutlineIcon />
                </IconButton>
                <Typography sx={{ fontSize: 13, display: { xs: "none", md: "inline" } }}>
                  Mi cuenta
                </Typography>
              </Box>
            </Box>
          </Toolbar>
        </Container>
      </AppBar>
    </Box>
  );
}

interface NavItemProps {
  to: string;
  label: string;
}

function NavItem({ to, label }: NavItemProps) {
  const location = useLocation();
  const isActive = location.pathname === "/" ? to === "/" : to !== "/" && location.pathname + location.search === to;

  return (
    <Typography
      component={Link}
      to={to}
      sx={{
        textDecoration: "none",
        color: isActive ? palette.ink : "#333333",
        fontSize: 14,
        fontWeight: isActive ? 700 : 400,
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
