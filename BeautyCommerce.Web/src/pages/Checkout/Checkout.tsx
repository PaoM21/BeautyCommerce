import {
    Alert,
    Box,
    Button,
    CircularProgress,
    Container,
    Divider,
    Typography,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { useCartStore } from "../../store/cartStore";
import { getShoppingCart } from "../../services/cartService";
import { checkout } from "../../services/orderService";

export default function Checkout() {
    const navigate = useNavigate();

    const queryClient = useQueryClient();

    const setItemCount = useCartStore(
        (state) => state.setItemCount
    );

    const {
        data: cart,
        isLoading,
        isError,
    } = useQuery({
        queryKey: ["shopping-cart"],
        queryFn: getShoppingCart,
    });

    const mutation = useMutation({
        mutationFn: checkout,

        onSuccess: (data) => {
            queryClient.setQueryData(["shopping-cart"], {
                items: [],
                total: 0,
            });

            setItemCount(0);

            queryClient.invalidateQueries({
                queryKey: ["orders"],
            });

            navigate(`/checkout/success?orderId=${data.orderId}`);
        },
    });

    if (isLoading) {
        return (
            <Box
                sx={{
                    minHeight: "60vh",
                    display: "flex",
                    justifyContent: "center",
                    alignItems: "center",
                }}
            >
                <CircularProgress />
            </Box>
        );
    }

    if (isError || !cart) {
        return (
            <Container sx={{ py: 10 }}>
                <Alert severity="error">
                    No fue posible cargar tu carrito.
                </Alert>
            </Container>
        );
    }

    if (cart.items.length === 0) {
        return (
            <Container
                maxWidth="md"
                sx={{
                    py: 12,
                    textAlign: "center",
                }}
            >
                <Typography
                    sx={{
                        fontSize: 32,
                        fontWeight: 400,
                        mb: 2,
                    }}
                >
                    Tu carrito está vacío
                </Typography>

                <Typography
                    sx={{
                        color: "#777",
                        mb: 4,
                    }}
                >
                    Agrega productos antes de continuar.
                </Typography>

                <Button
                    variant="contained"
                    onClick={() => navigate("/productos")}
                    sx={{
                        borderRadius: 0,
                        px: 5,
                        py: 1.5,
                    }}
                >
                    Explorar productos
                </Button>
            </Container>
        );
    }

    return (
        <Container maxWidth="lg" sx={{ py: 8 }}>
            <Typography
                sx={{
                    fontSize: 12,
                    letterSpacing: 3,
                    textTransform: "uppercase",
                    color: "#9a8065",
                    mb: 2,
                }}
            >
                Checkout
            </Typography>

            <Typography
                component="h1"
                sx={{
                    fontSize: {
                        xs: 38,
                        md: 50,
                    },
                    fontWeight: 400,
                    mb: 6,
                }}
            >
                Finalizar compra
            </Typography>

            <Box
                sx={{
                    display: "grid",
                    gridTemplateColumns: {
                        xs: "1fr",
                        md: "1.5fr 1fr",
                    },
                    gap: 6,
                }}
            >
                {/* PRODUCTOS */}

                <Box>
                    <Typography
                        sx={{
                            fontSize: 22,
                            mb: 3,
                        }}
                    >
                        Tu pedido
                    </Typography>

                    {cart.items.map((item) => (
                        <Box key={item.productVariantId}>
                            <Box
                                sx={{
                                    display: "flex",
                                    gap: 3,
                                    py: 3,
                                }}
                            >
                                <Box
                                    sx={{
                                        width: 90,
                                        height: 90,
                                        backgroundColor: "#f6f4f1",
                                        flexShrink: 0,
                                    }}
                                >
                                    {item.imageUrl && (
                                        <Box
                                            component="img"
                                            src={item.imageUrl}
                                            alt={item.productName}
                                            sx={{
                                                width: "100%",
                                                height: "100%",
                                                objectFit: "cover",
                                            }}
                                        />
                                    )}
                                </Box>

                                <Box sx={{ flex: 1 }}>
                                    <Typography>
                                        {item.productName}
                                    </Typography>

                                    <Typography
                                        sx={{
                                            fontSize: 13,
                                            color: "#888",
                                            mt: 0.5,
                                        }}
                                    >
                                        {item.color && `Color: ${item.color}`}
                                        {item.color && item.size && " · "}
                                        {item.size && `Talla: ${item.size}`}
                                    </Typography>

                                    <Typography
                                        sx={{
                                            fontSize: 13,
                                            color: "#888",
                                            mt: 1,
                                        }}
                                    >
                                        Cantidad: {item.quantity}
                                    </Typography>
                                </Box>

                                <Typography fontWeight={500}>
                                    $
                                    {item.subtotal.toLocaleString(
                                        "es-CO"
                                    )}
                                </Typography>
                            </Box>

                            <Divider />
                        </Box>
                    ))}
                </Box>

                {/* RESUMEN */}

                <Box
                    sx={{
                        border: "1px solid #e5e1dc",
                        p: 4,
                        height: "fit-content",
                    }}
                >
                    <Typography
                        sx={{
                            fontSize: 22,
                            mb: 4,
                        }}
                    >
                        Resumen
                    </Typography>

                    <Box
                        sx={{
                            display: "flex",
                            justifyContent: "space-between",
                            mb: 2,
                        }}
                    >
                        <Typography color="text.secondary">
                            Productos
                        </Typography>

                        <Typography>
                            $
                            {cart.total.toLocaleString("es-CO")}
                        </Typography>
                    </Box>

                    <Box
                        sx={{
                            display: "flex",
                            justifyContent: "space-between",
                            mb: 3,
                        }}
                    >
                        <Typography color="text.secondary">
                            Envío
                        </Typography>

                        <Typography>
                            Gratis
                        </Typography>
                    </Box>

                    <Divider sx={{ mb: 3 }} />

                    <Box
                        sx={{
                            display: "flex",
                            justifyContent: "space-between",
                            mb: 4,
                        }}
                    >
                        <Typography fontWeight={500}>
                            Total
                        </Typography>

                        <Typography
                            fontSize={20}
                            fontWeight={500}
                        >
                            $
                            {cart.total.toLocaleString("es-CO")}
                        </Typography>
                    </Box>

                    {mutation.isError && (
                        <Alert severity="error" sx={{ mb: 3 }}>
                            No fue posible procesar tu pedido.
                            Verifica el stock y vuelve a intentarlo.
                        </Alert>
                    )}

                    <Button
                        fullWidth
                        variant="contained"
                        disabled={mutation.isPending}
                        onClick={() => mutation.mutate()}
                        sx={{
                            borderRadius: 0,
                            py: 1.7,
                            backgroundColor: "#1f1f1f",
                            "&:hover": {
                                backgroundColor: "#000",
                            },
                        }}
                    >
                        {mutation.isPending
                            ? "Procesando..."
                            : "Confirmar y pagar"}
                    </Button>
                </Box>
            </Box>
        </Container>
    );
}
