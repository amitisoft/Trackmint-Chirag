import axios from "axios";
import type { AuthResponse } from "../../types/models";
import { getAuthSession, useAuthStore } from "../../store/auth-store";

const baseURL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5151/api";

export const api = axios.create({
  baseURL,
});

api.interceptors.request.use((config) => {
  const session = getAuthSession();

  if (session?.accessToken) {
    config.headers.Authorization = `Bearer ${session.accessToken}`;
  }

  return config;
});

let refreshPromise: Promise<AuthResponse> | null = null;

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config as typeof error.config & { _retry?: boolean };
    const session = getAuthSession();

    if (error.response?.status !== 401 || !session?.refreshToken || originalRequest?._retry) {
      return Promise.reject(error);
    }

    originalRequest._retry = true;

    if (!refreshPromise) {
      refreshPromise = api
        .post<AuthResponse>("/auth/refresh", { refreshToken: session.refreshToken })
        .then((response) => response.data)
        .finally(() => {
          refreshPromise = null;
        });
    }

    try {
      const refreshedSession = await refreshPromise;
      useAuthStore.getState().setSession(refreshedSession);
      originalRequest.headers = originalRequest.headers ?? {};
      originalRequest.headers.Authorization = `Bearer ${refreshedSession.accessToken}`;
      return api(originalRequest);
    } catch (refreshError) {
      useAuthStore.getState().clearSession();
      return Promise.reject(refreshError);
    }
  },
);
