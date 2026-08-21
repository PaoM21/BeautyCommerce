import { beforeEach, describe, expect, it } from "vitest";

import { useCartStore } from "./cartStore";

describe("cartStore", () => {
  beforeEach(() => {
    useCartStore.setState({ itemCount: 0 });
  });

  it("starts at zero", () => {
    expect(useCartStore.getState().itemCount).toBe(0);
  });

  it("setItemCount replaces the count", () => {
    useCartStore.getState().setItemCount(5);

    expect(useCartStore.getState().itemCount).toBe(5);
  });

  it("increment adds one by default", () => {
    useCartStore.getState().increment();
    useCartStore.getState().increment();

    expect(useCartStore.getState().itemCount).toBe(2);
  });

  it("increment accepts a custom quantity", () => {
    useCartStore.getState().increment(3);

    expect(useCartStore.getState().itemCount).toBe(3);
  });

  it("decrement subtracts one by default", () => {
    useCartStore.setState({ itemCount: 5 });
    useCartStore.getState().decrement();

    expect(useCartStore.getState().itemCount).toBe(4);
  });

  it("decrement never goes below zero", () => {
    useCartStore.setState({ itemCount: 1 });
    useCartStore.getState().decrement(10);

    expect(useCartStore.getState().itemCount).toBe(0);
  });

  it("clear resets the count to zero", () => {
    useCartStore.setState({ itemCount: 7 });
    useCartStore.getState().clear();

    expect(useCartStore.getState().itemCount).toBe(0);
  });
});
