import { beforeEach, describe, expect, it, vi } from "vitest";

import { api } from "./api";
import { deleteProduct } from "./productService";

vi.mock("./api", () => ({
  api: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}));

describe("deleteProduct", () => {
  beforeEach(() => {
    vi.mocked(api.delete).mockReset();
  });

  it("returns the success result from the API", async () => {
    vi.mocked(api.delete).mockResolvedValue({
      data: { success: true, message: "Producto eliminado." },
    });

    await expect(deleteProduct("p1")).resolves.toEqual({
      success: true,
      message: "Producto eliminado.",
    });
  });

  it("returns a failure result with the backend message on a 404 instead of throwing", async () => {
    vi.mocked(api.delete).mockRejectedValue({
      response: { status: 404, data: { message: "El producto ya no existe." } },
    });

    await expect(deleteProduct("p1")).resolves.toEqual({
      success: false,
      message: "El producto ya no existe.",
    });
  });

  it("falls back to a generic message on a 404 without a backend message", async () => {
    vi.mocked(api.delete).mockRejectedValue({
      response: { status: 404, data: {} },
    });

    await expect(deleteProduct("p1")).resolves.toEqual({
      success: false,
      message: "Producto no encontrado.",
    });
  });

  it("re-throws any other error", async () => {
    vi.mocked(api.delete).mockRejectedValue({
      response: { status: 500 },
    });

    await expect(deleteProduct("p1")).rejects.toBeTruthy();
  });
});
