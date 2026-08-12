import { useState } from "react";
import {
  Box,
  Button,
  Container,
  TextField,
  Typography,
} from "@mui/material";
import { Link, useNavigate } from "react-router-dom";

import { login } from "../../services/authService";
import { useAuthStore } from "../../store/authStore";

export default function Login() {
  const navigate = useNavigate();

  const loginStore = useAuthStore((state) => state.login);

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const handleSubmit = async (
    event: React.FormEvent
  ) => {
    event.preventDefault();

    setError("");

    if (!email || !password) {
      setError("Ingresa tu correo y contraseña.");
      return;
    }

    try {
      setLoading(true);

      const response = await login({
        email,
        password,
      });

      loginStore(response.token, response.user ?? null);

      navigate("/");
    } catch (error) {
      console.error(error);

      setError("Correo o contraseña incorrectos.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <Container maxWidth="sm">
      <Box
        sx={{
          py: 12,
          maxWidth: 450,
          mx: "auto",
        }}
      >
        <Typography
          component="h1"
          sx={{
            fontSize: 42,
            fontWeight: 400,
            mb: 2,
          }}
        >
          Bienvenida
        </Typography>

        <Typography
          sx={{
            color: "#777",
            mb: 5,
          }}
        >
          Inicia sesión en Beauty Commerce.
        </Typography>

        <Box component="form" onSubmit={handleSubmit}>
          <TextField
            fullWidth
            label="Correo electrónico"
            type="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            sx={{ mb: 3 }}
          />

          <TextField
            fullWidth
            label="Contraseña"
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            sx={{ mb: 2 }}
          />

          {error && (
            <Typography
              sx={{
                color: "#c62828",
                fontSize: 14,
                mb: 3,
              }}
            >
              {error}
            </Typography>
          )}

          <Button
            fullWidth
            type="submit"
            variant="contained"
            disabled={loading}
            sx={{
              backgroundColor: "#1f1f1f",
              borderRadius: 0,
              py: 1.7,
            }}
          >
            {loading ? "Ingresando..." : "Iniciar sesión"}
          </Button>
        </Box>

        <Typography
          sx={{
            mt: 4,
            textAlign: "center",
            color: "#777",
          }}
        >
          ¿No tienes una cuenta? {" "}
          <Box
            component={Link}
            to="/registro"
            sx={{
              color: "#1f1f1f",
              textDecoration: "none",
              fontWeight: 500,
            }}
          >
            Crear cuenta
          </Box>
        </Typography>
      </Box>
    </Container>
  );
}
