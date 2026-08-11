import axios from "axios";
import type { ApiErrorResponse } from "@/types/api";

/**
 * Extracts a user-friendly error message from an Axios error response,
 * inspecting ApiErrorResponse title, detail, and validation errors dictionary.
 */
export function getApiErrorMessage(
  err: unknown,
  fallbackMessage = "An unexpected error occurred. Please try again."
): string {
  if (!axios.isAxiosError(err) || !err.response) {
    return fallbackMessage;
  }

  const data = err.response.data as ApiErrorResponse | undefined;

  if (!data) {
    return err.message || fallbackMessage;
  }

  // 1. If there are field-level validation errors, combine them into a single string.
  if (data.errors && Object.keys(data.errors).length > 0) {
    const messages = Object.values(data.errors).flat();
    if (messages.length > 0) {
      return messages.join(" ");
    }
  }

  // 2. Use title or detail (e.g. 409/422 messages like "Marks must be between 0 and 10 for this assignment").
  if (data.title) {
    return data.title;
  }

  if (data.detail) {
    return data.detail;
  }

  return fallbackMessage;
}
