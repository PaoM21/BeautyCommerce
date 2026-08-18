import { useState } from "react";
import {
  Box,
  Button,
  Checkbox,
  Container,
  FormControlLabel,
  TextField,
  Typography,
} from "@mui/material";

import { Link, useNavigate } from "react-router-dom";

import { register } from "../../services/authService";

export default function Register() {
  const navigate = useNavigate();

  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [acceptedTerms, setAcceptedTerms] = useState(false);

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const handleSubmit = async (
    event: React.FormEvent
  ) => {
    event.preventDefault();

    setError("");

    if (
      !firstName ||
      !lastName ||
      !email ||
      !password
    ) {
      setError(
        "Completa todos los campos."
      );

      return;
    }

    if (!acceptedTerms) {
      setError(
        "Debes aceptar los Términos y Condiciones y la Política de Tratamiento de Datos Personales."
      );

      return;
    }

    try {
      setLoading(true);

      await register({
        firstName,
        lastName,
        email,
        password,
      });

      navigate("/login");
    } catch (error) {
      console.error(error);

      setError(
        "No fue posible crear la cuenta."
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <Container maxWidth="sm">
      <Box
        sx={{
          py: 10,
          maxWidth: 480,
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
          Crear cuenta
        </Typography>

        <Typography
          sx={{
            color: "#777",
            mb: 5,
          }}
        >
          Forma parte de HALDY&CO ECOMMERCE.
        </Typography>

        <Box
          component="form"
          onSubmit={handleSubmit}
        >
          <Box
            sx={{
              display: "grid",
              gridTemplateColumns: "1fr 1fr",
              gap: 2,
              mb: 3,
            }}
          >
            <TextField
              label="Nombre"
              value={firstName}
              onChange={(event) =>
                setFirstName(event.target.value)
              }
            />

            <TextField
              label="Apellido"
              value={lastName}
              onChange={(event) =>
                setLastName(event.target.value)
              }
            />
          </Box>

          <TextField
            fullWidth
            label="Correo electrónico"
            type="email"
            value={email}
            onChange={(event) =>
              setEmail(event.target.value)
            }
            sx={{ mb: 3 }}
          />

          <TextField
            fullWidth
            label="Contraseña"
            type="password"
            value={password}
            onChange={(event) =>
              setPassword(event.target.value)
            }
            sx={{ mb: 2 }}
            helperText="Mínimo 8 caracteres, incluyendo mayúscula, minúscula, número y carácter especial."
          />

          <FormControlLabel
            sx={{ mb: 2, alignItems: "flex-start" }}
            control={
              <Checkbox
                checked={acceptedTerms}
                onChange={(event) =>
                  setAcceptedTerms(event.target.checked)
                }
                sx={{ pt: 0 }}
              />
            }
            label={
              <Typography sx={{ fontSize: 13, color: "#666", mt: 1 }}>
                He leído y acepto los{" "}
                <Box
                  component={Link}
                  to="/terminos-y-condiciones"
                  target="_blank"
                  sx={{ color: "#1f1f1f", fontWeight: 500 }}
                >
                  Términos y Condiciones
                </Box>{" "}
                y la{" "}
                <Box
                  component={Link}
                  to="/tratamiento-de-datos-personales"
                  target="_blank"
                  sx={{ color: "#1f1f1f", fontWeight: 500 }}
                >
                  Política de Tratamiento de Datos Personales
                </Box>
                .
              </Typography>
            }
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
            type="submit"
            fullWidth
            variant="contained"
            disabled={loading || !acceptedTerms}
            sx={{
              backgroundColor: "#1f1f1f",
              borderRadius: 0,
              py: 1.7,
            }}
          >
            {loading
              ? "Creando cuenta..."
              : "Crear cuenta"}
          </Button>
        </Box>

        <Typography
          sx={{
            mt: 4,
            textAlign: "center",
            color: "#777",
          }}
        >
          ¿Ya tienes una cuenta?{" "}
          <Box
            component={Link}
            to="/login"
            sx={{
              color: "#1f1f1f",
              textDecoration: "none",
              fontWeight: 500,
            }}
          >
            Iniciar sesión
          </Box>
        </Typography>
      </Box>
    </Container>
  );
}
