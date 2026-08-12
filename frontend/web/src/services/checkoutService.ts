import { api } from "./api";

export interface CheckoutResponse {
  orderId: string;
}

export async function checkout(): Promise<CheckoutResponse> {
  try {
    const response = await api.post<CheckoutResponse>(
      "/Orders/checkout"
    );

    return response.data;
  } catch (err: any) {
    const message =
      err?.response?.data?.message ?? err?.response?.data ?? err?.message ?? "Checkout failed";
    throw new Error(message);
  }
}
