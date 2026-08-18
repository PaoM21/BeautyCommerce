import { api } from "./api";

export async function subscribeNewsletter(email: string): Promise<void> {
  await api.post("/Newsletter/subscribe", { email });
}
