import { create } from "zustand";

interface CartStore {
  itemCount: number;

  setItemCount: (count: number) => void;

  increment: (quantity?: number) => void;

  decrement: (quantity?: number) => void;

  clear: () => void;
}

export const useCartStore = create<CartStore>((set) => ({
  itemCount: 0,

  setItemCount: (count) =>
    set({
      itemCount: count,
    }),

  increment: (quantity = 1) =>
    set((state) => ({
      itemCount: state.itemCount + quantity,
    })),

  decrement: (quantity = 1) =>
    set((state) => ({
      itemCount: Math.max(0, state.itemCount - quantity),
    })),

  clear: () =>
    set({
      itemCount: 0,
    }),
}));
