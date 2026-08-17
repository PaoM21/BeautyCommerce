import { Box, Typography } from "@mui/material";
import { Link, NavLink, Outlet, useNavigate } from "react-router-dom";

import DashboardOutlinedIcon from "@mui/icons-material/DashboardOutlined";
import Inventory2OutlinedIcon from "@mui/icons-material/Inventory2Outlined";
import ReceiptLongOutlinedIcon from "@mui/icons-material/ReceiptLongOutlined";
import PeopleOutlinedIcon from "@mui/icons-material/PeopleOutlined";
import WarehouseOutlinedIcon from "@mui/icons-material/WarehouseOutlined";
import StorefrontOutlinedIcon from "@mui/icons-material/StorefrontOutlined";
import LogoutOutlinedIcon from "@mui/icons-material/LogoutOutlined";

import { useAuthStore } from "../../store/authStore";
import { palette } from "../../theme/theme";

const NAV_ITEMS = [
  { to: "/admin/dashboard", label: "Dashboard", icon: DashboardOutlinedIcon },
  { to: "/admin/productos", label: "Productos", icon: Inventory2OutlinedIcon },
  { to: "/admin/pedidos", label: "Pedidos", icon: ReceiptLongOutlinedIcon },
  { to: "/admin/clientes", label: "Clientes", icon: PeopleOutlinedIcon },
  { to: "/admin/inventario", label: "Inventario", icon: WarehouseOutlinedIcon },
];

export default function AdminLayout() {
  const navigate = useNavigate();
  const logout = useAuthStore((state) => state.logout);
  const user = useAuthStore((state) => state.user);

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  return (
    <Box sx={{ display: "flex", minHeight: "100vh", backgroundColor: palette.ivory }}>
      {/* SIDEBAR (desktop) */}
      <Box
        component="nav"
        aria-label="Navegación de administración"
        sx={{
          width: 260,
          flexShrink: 0,
          backgroundColor: palette.charcoal,
          color: "#fff",
          display: { xs: "none", md: "flex" },
          flexDirection: "column",
          position: "sticky",
          top: 0,
          height: "100vh",
        }}
      >
        <Box sx={{ px: 3, py: 4 }}>
          <Typography
            sx={{
              fontFamily: '"Fraunces", serif',
              fontSize: 20,
              fontWeight: 500,
            }}
          >
            BEAUTY COMMERCE
          </Typography>

          <Typography
            sx={{
              fontSize: 11,
              letterSpacing: 2,
              color: palette.goldLight,
              mt: 0.3,
            }}
          >
            PANEL DE ADMINISTRACIÓN
          </Typography>
        </Box>

        <Box component="ul" sx={{ listStyle: "none", m: 0, p: 0, flex: 1 }}>
          {NAV_ITEMS.map(({ to, label, icon: Icon }) => (
            <Box component="li" key={to}>
              <Box
                component={NavLink}
                to={to}
                sx={{
                  display: "flex",
                  alignItems: "center",
                  gap: 1.5,
                  px: 3,
                  py: 1.7,
                  color: "rgba(255,255,255,0.75)",
                  textDecoration: "none",
                  fontSize: 14,
                  borderLeft: "3px solid transparent",
                  transition: "all 0.15s ease",
                  "&:hover": {
                    color: "#fff",
                    backgroundColor: "rgba(255,255,255,0.05)",
                  },
                  "&.active": {
                    color: "#fff",
                    backgroundColor: "rgba(255,255,255,0.08)",
                    borderLeftColor: palette.gold,
                  },
                }}
              >
                <Icon sx={{ fontSize: 20 }} />
                {label}
              </Box>
            </Box>
          ))}
        </Box>

        <Box sx={{ borderTop: "1px solid rgba(255,255,255,0.1)", p: 2 }}>
          {user?.email && (
            <Typography
              sx={{
                fontSize: 12,
                color: "rgba(255,255,255,0.5)",
                px: 1,
                mb: 1.5,
                wordBreak: "break-all",
              }}
            >
              {user.email}
            </Typography>
          )}

          <Box
            component={Link}
            to="/"
            sx={{
              display: "flex",
              alignItems: "center",
              gap: 1.5,
              px: 1,
              py: 1.2,
              color: "rgba(255,255,255,0.75)",
              textDecoration: "none",
              fontSize: 14,
              "&:hover": { color: "#fff" },
            }}
          >
            <StorefrontOutlinedIcon sx={{ fontSize: 20 }} />
            Volver a la tienda
          </Box>

          <Box
            component="button"
            onClick={handleLogout}
            sx={{
              display: "flex",
              alignItems: "center",
              gap: 1.5,
              px: 1,
              py: 1.2,
              width: "100%",
              color: "rgba(255,255,255,0.75)",
              backgroundColor: "transparent",
              border: "none",
              cursor: "pointer",
              fontSize: 14,
              fontFamily: "inherit",
              textAlign: "left",
              "&:hover": { color: "#fff" },
            }}
          >
            <LogoutOutlinedIcon sx={{ fontSize: 20 }} />
            Cerrar sesión
          </Box>
        </Box>
      </Box>

      <Box sx={{ flex: 1, minWidth: 0, display: "flex", flexDirection: "column" }}>
        {/* NAV (mobile) */}
        <Box
          component="nav"
          aria-label="Navegación de administración"
          sx={{
            display: { xs: "flex", md: "none" },
            backgroundColor: palette.charcoal,
            overflowX: "auto",
            gap: 0.5,
            px: 1.5,
            py: 1.2,
            position: "sticky",
            top: 0,
            zIndex: 10,
          }}
        >
          {NAV_ITEMS.map(({ to, label }) => (
            <Box
              key={to}
              component={NavLink}
              to={to}
              sx={{
                whiteSpace: "nowrap",
                px: 2,
                py: 0.8,
                fontSize: 13,
                borderRadius: 4,
                color: "rgba(255,255,255,0.75)",
                textDecoration: "none",
                "&.active": {
                  color: "#fff",
                  backgroundColor: "rgba(255,255,255,0.12)",
                },
              }}
            >
              {label}
            </Box>
          ))}
        </Box>

        <Box component="main" sx={{ flex: 1 }}>
          <Outlet />
        </Box>
      </Box>
    </Box>
  );
}
