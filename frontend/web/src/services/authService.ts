import { api } from "./api";
import type {
  LoginRequest,
  LoginResponse,
  RegisterRequest,
} from "../types/auth";

export async function login(
  credentials: LoginRequest
): Promise<LoginResponse> {
  const response = await api.post<any>(
    "/auth/login",
    { login: credentials }
  );

  // Backend returns LoginResponseDto with Token, Expiration, UserId, Email, FullName
  const data = response.data;

  return {
    token: data?.token ?? data?.Token ?? "",
    expiration: data?.expiration ?? data?.Expiration,
    user: {
      id: data?.user?.id ?? data?.userId ?? data?.UserId ?? "",
      email: data?.user?.email ?? data?.email ?? data?.Email ?? "",
      firstName: data?.user?.firstName ?? undefined,
      lastName: data?.user?.lastName ?? undefined,
    },
  };
}

export async function register(
  data: RegisterRequest
): Promise<void> {
  await api.post("/auth/register", { user: data });
}
