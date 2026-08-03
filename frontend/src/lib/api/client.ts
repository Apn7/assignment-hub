import axios from 'axios';

/**
 * Base URL of the ASP.NET Core API as seen from the browser.
 *
 * Set `NEXT_PUBLIC_API_URL` in `frontend/.env.local` (see `.env.example` at the
 * repository root). The fallback matches the `http` launch profile in
 * `backend/src/AssignmentHub.Api/Properties/launchSettings.json` so a fresh
 * clone runs without any configuration.
 */
export const API_BASE_URL =
  process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5080';

/**
 * Shared axios instance. Every feature should import this rather than calling
 * axios directly, so that auth headers, error normalisation and timeouts are
 * configured in exactly one place.
 */
export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  timeout: 10_000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request/response interceptors (bearer token injection, 401 handling) are
// added here once authentication lands.
