import { useState } from "react";
import {
  Box,
  Button,
  Container,
  TextField,
  Typography,
} from "@mui/material";
import { Link } from "react-router-dom";

import { forgotPassword } from "../../services/authService";

export default function ForgotPassword() {
  const [email, setEmail] = useState("");
  const [loading, setLoading] = useState(false);
  const [submitted, setSubmitted] = useState(false);
  const [error, setError] = useState("");

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();

    if (!email) {
      setError("Ingresa tu correo electrónico.");
      return;
    }

    setError("");

    try {
      setLoading(true);

      await forgotPassword(email);

      setSubmitted(true);
    } catch (error) {
      console.error(error);

      setError("No fue posible procesar la solicitud. Intenta de nuevo.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <Container maxWidth="sm">
      <Box sx={{ py: 12, maxWidth: 450, mx: "auto" }}>
        <Typography
          component="h1"
          sx={{ fontSize: 42, fontWeight: 400, mb: 2 }}
        >
          Recuperar contraseña
        </Typography>

        {submitted ? (
          <>
            <Typography sx={{ color: "#666", mb: 5, lineHeight: 1.7 }}>
              Si el correo <strong>{email}</strong> está registrado, te
              enviamos un enlace para restablecer tu contraseña. Revisa tu
              bandeja de entrada (y la carpeta de spam, por si acaso).
            </Typography>

            <Box
              component={Link}
              to="/login"
              sx={{
                color: "#1f1f1f",
                textDecoration: "none",
                fontWeight: 500,
              }}
            >
              ← Volver a iniciar sesión
            </Box>
          </>
        ) : (
          <>
            <Typography sx={{ color: "#777", mb: 5 }}>
              Ingresa tu correo y te enviaremos un enlace para restablecer
              tu contraseña.
            </Typography>

            <Box component="form" onSubmit={handleSubmit}>
              <TextField
                fullWidth
                label="Correo electrónico"
                type="email"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                sx={{ mb: 2 }}
              />

              {error && (
                <Typography
                  sx={{ color: "#c62828", fontSize: 14, mb: 3 }}
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
                {loading ? "Enviando..." : "Enviar enlace"}
              </Button>
            </Box>

            <Typography sx={{ mt: 4, textAlign: "center", color: "#777" }}>
              <Box
                component={Link}
                to="/login"
                sx={{
                  color: "#1f1f1f",
                  textDecoration: "none",
                  fontWeight: 500,
                }}
              >
                ← Volver a iniciar sesión
              </Box>
            </Typography>
          </>
        )}
      </Box>
    </Container>
  );
}
