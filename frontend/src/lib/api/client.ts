import axios from "axios";

/**
 * Base URL of the ASP.NET Core API as seen from the browser.
 *
 * Set `NEXT_PUBLIC_API_URL` in `frontend/.env.local` (see `.env.example` at the
 * repository root). The fallback matches the `http` launch profile in
 * `backend/src/AssignmentHub.Api/Properties/launchSettings.json` so a fresh
 * clone runs without any configuration.
 */
export const API_BASE_URL =
  process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5080";

/**
 * Shared axios instance with automatic bearer-token injection and global
 * 401 handling. Every feature should import `api` rather than calling
 * axios directly.
 */
export const api = axios.create({
  baseURL: API_BASE_URL,
  timeout: 10_000,
  headers: { "Content-Type": "application/json" },
});

// Inject the stored JWT on every outgoing request.
api.interceptors.request.use((config) => {
  const token =
    typeof window !== "undefined" ? localStorage.getItem("token") : null;
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

// On any 401, clear the session and redirect to login.
api.interceptors.response.use(
  (res) => res,
  (err) => {
    if (err.response?.status === 401 && typeof window !== "undefined") {
      localStorage.removeItem("token");
      localStorage.removeItem("user");
      if (!window.location.pathname.startsWith("/login"))
        window.location.href = "/login";
    }
    return Promise.reject(err);
  }
);

// Keep the old name exported so health.ts (which imports `apiClient`) still
// compiles without changes.
export const apiClient = api;
