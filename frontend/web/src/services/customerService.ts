import { api } from "./api";

export interface AdminCustomer {
  id: string;
  fullName: string;
  email: string;
  role: string;
  isActive: boolean;
}

export async function getAdminCustomers(): Promise<AdminCustomer[]> {
  const response = await api.get<AdminCustomer[]>("/Users/customers");

  return response.data;
}
