import {
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";

import {
  getWishlist,
  addToWishlist,
  removeFromWishlist,
} from "../services/wishlistService";

export function useWishlist() {
  const queryClient = useQueryClient();

  const wishlistQuery = useQuery({
    queryKey: ["wishlist"],
    queryFn: getWishlist,
  });

  const addMutation = useMutation({
    mutationFn: addToWishlist,

    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: ["wishlist"],
      });
    },
  });

  const removeMutation = useMutation({
    mutationFn: removeFromWishlist,

    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: ["wishlist"],
      });
    },
  });

  const isFavorite = (productId: string) => {
    return (
      wishlistQuery.data?.some(
        (item) => item.productId === productId
      ) ?? false
    );
  };

  const toggleFavorite = (productId: string) => {
    if (
      addMutation.isPending ||
      removeMutation.isPending
    ) {
      return;
    }

    if (isFavorite(productId)) {
      removeMutation.mutate(productId);
    } else {
      addMutation.mutate(productId);
    }
  };

  return {
    wishlist: wishlistQuery.data ?? [],
    isFavorite,
    toggleFavorite,
    isLoading: wishlistQuery.isLoading,
    isUpdating:
      addMutation.isPending ||
      removeMutation.isPending,
  };
}
