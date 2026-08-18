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

import { Link, useNavigate } from "react-router-dom";
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
  const [searchOpen, setSearchOpen] = useState(false);
  const [searchValue, setSearchValue] = useState("");

  const handleSearchSubmit = (event: FormEvent) => {
    event.preventDefault();

    const term = searchValue.trim();
    if (!term) return;

    navigate(`/productos?buscar=${encodeURIComponent(term)}`);
    setSearchOpen(false);
  };

  return (
    <Box sx={{ position: "sticky", top: 0, zIndex: 1100 }}>
      {/* BARRA DE ANUNCIO */}
      <Box
        sx={{
          backgroundColor: palette.charcoal,
          color: "#fff",
          textAlign: "center",
          py: 0.7,
        }}
      >
        <Typography
          sx={{
            fontSize: 12,
            letterSpacing: 1.5,
            textTransform: "uppercase",
          }}
        >
          Envío asegurado a toda Colombia · Empaque de regalo en cada pedido
        </Typography>
      </Box>

      <AppBar
        position="static"
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
                  color: palette.gold,
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
              {searchOpen ? (
                <Box
                  component="form"
                  onSubmit={handleSearchSubmit}
                  sx={{
                    display: "flex",
                    alignItems: "center",
                    backgroundColor: palette.ivory,
                    borderRadius: 1,
                    px: 1.5,
                    mr: 1,
                  }}
                >
                  <InputBase
                    autoFocus
                    placeholder="Buscar productos..."
                    value={searchValue}
                    onChange={(event) => setSearchValue(event.target.value)}
                    onBlur={() => !searchValue && setSearchOpen(false)}
                    sx={{ fontSize: 14, width: { xs: 140, sm: 220 } }}
                  />
                  <IconButton type="submit" size="small">
                    <SearchIcon fontSize="small" />
                  </IconButton>
                </Box>
              ) : (
                <IconButton onClick={() => setSearchOpen(true)} aria-label="Buscar">
                  <SearchIcon />
                </IconButton>
              )}

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
    </Box>
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
