import { useState } from "react";
import {
  Box,
  Button,
  Container,
  TextField,
  Typography,
} from "@mui/material";
import { Link, useNavigate, useSearchParams } from "react-router-dom";

import { resetPassword } from "../../services/authService";
import { getApiErrorMessage } from "../../services/apiError";

export default function ResetPassword() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const email = searchParams.get("email") ?? "";
  const token = searchParams.get("token") ?? "";

  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const linkIsValid = Boolean(email && token);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();

    if (!newPassword || !confirmPassword) {
      setError("Completa ambos campos.");
      return;
    }

    if (newPassword !== confirmPassword) {
      setError("Las contraseñas no coinciden.");
      return;
    }

    setError("");

    try {
      setLoading(true);

      await resetPassword(email, token, newPassword);

      navigate("/login", {
        state: { message: "Tu contraseña fue actualizada. Inicia sesión." },
      });
    } catch (err) {
      setError(
        getApiErrorMessage(
          err,
          "No fue posible restablecer tu contraseña. El enlace pudo haber expirado."
        )
      );
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
          Restablecer contraseña
        </Typography>

        {!linkIsValid ? (
          <>
            <Typography sx={{ color: "#c62828", mb: 4 }}>
              Este enlace no es válido. Solicita uno nuevo.
            </Typography>

            <Box
              component={Link}
              to="/olvide-password"
              sx={{ color: "#1f1f1f", textDecoration: "none", fontWeight: 500 }}
            >
              Solicitar nuevo enlace
            </Box>
          </>
        ) : (
          <>
            <Typography sx={{ color: "#777", mb: 5 }}>
              Elige tu nueva contraseña para <strong>{email}</strong>.
            </Typography>

            <Box component="form" onSubmit={handleSubmit}>
              <TextField
                fullWidth
                label="Nueva contraseña"
                type="password"
                value={newPassword}
                onChange={(event) => setNewPassword(event.target.value)}
                sx={{ mb: 3 }}
              />

              <TextField
                fullWidth
                label="Confirmar nueva contraseña"
                type="password"
                value={confirmPassword}
                onChange={(event) => setConfirmPassword(event.target.value)}
                sx={{ mb: 2 }}
              />

              {error && (
                <Typography sx={{ color: "#c62828", fontSize: 14, mb: 3 }}>
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
                {loading ? "Guardando..." : "Restablecer contraseña"}
              </Button>
            </Box>
          </>
        )}
      </Box>
    </Container>
  );
}
