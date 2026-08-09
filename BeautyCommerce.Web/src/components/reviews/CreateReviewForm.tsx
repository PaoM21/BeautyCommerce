import {
  Alert,
  Box,
  Button,
  Rating,
  TextField,
  Typography,
} from "@mui/material";
import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";

import { createReview } from "../../services/reviewService";

interface CreateReviewFormProps {
  productId: string;
}

export default function CreateReviewForm({
  productId,
}: CreateReviewFormProps) {
  const queryClient = useQueryClient();

  const [rating, setRating] = useState<number | null>(5);
  const [comment, setComment] = useState("");

  const mutation = useMutation({
    mutationFn: () =>
      createReview({
        review: {
          productId,
          rating: rating ?? 0,
          comment,
        },
      }),

    onSuccess: () => {
      setRating(5);
      setComment("");

      queryClient.invalidateQueries({
        queryKey: ["reviews", productId],
      });
    },
  });

  const handleSubmit = (
    event: React.FormEvent
  ) => {
    event.preventDefault();

    if (!rating) return;

    if (!comment.trim()) return;

    mutation.mutate();
  };

  return (
    <Box
      component="form"
      onSubmit={handleSubmit}
      sx={{
        mt: 8,
        maxWidth: 650,
      }}
    >
      <Typography
        component="h3"
        sx={{
          fontSize: 24,
          fontWeight: 400,
          mb: 3,
        }}
      >
        Comparte tu experiencia
      </Typography>

      <Typography
        sx={{
          fontSize: 13,
          color: "#888",
          mb: 1,
        }}
      >
        Tu calificación
      </Typography>

      <Rating
        value={rating}
        onChange={(_, value) => {
          setRating(value);
        }}
        sx={{ mb: 3 }}
      />

      <TextField
        fullWidth
        multiline
        minRows={4}
        value={comment}
        onChange={(event) =>
          setComment(event.target.value)
        }
        placeholder="Cuéntanos qué te pareció este producto..."
        sx={{
          mb: 3,
          "& .MuiOutlinedInput-root": {
            borderRadius: 0,
          },
        }}
      />

      {mutation.isError && (
        <Alert severity="error" sx={{ mb: 3 }}>
          No fue posible publicar la reseña.
          Verifica que hayas comprado y recibido
          este producto.
        </Alert>
      )}

      {mutation.isSuccess && (
        <Alert severity="success" sx={{ mb: 3 }}>
          Tu reseña fue publicada correctamente.
        </Alert>
      )}

      <Button
        type="submit"
        variant="contained"
        disabled={
          mutation.isPending ||
          !rating ||
          !comment.trim()
        }
        sx={{
          borderRadius: 0,
          px: 5,
          py: 1.5,
          backgroundColor: "#1f1f1f",
          "&:hover": {
            backgroundColor: "#000",
          },
        }}
      >
        {mutation.isPending
          ? "Publicando..."
          : "Publicar reseña"}
      </Button>
    </Box>
  );
}
