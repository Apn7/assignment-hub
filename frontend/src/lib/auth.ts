import type { User } from "@/types";

export const saveSession = (token: string, user: User) => {
  localStorage.setItem("token", token);
  localStorage.setItem("user", JSON.stringify(user));
};

export const getUser = (): User | null => {
  try {
    return JSON.parse(localStorage.getItem("user") ?? "null");
  } catch {
    return null;
  }
};

export const clearSession = () => {
  localStorage.removeItem("token");
  localStorage.removeItem("user");
};

export const roleHome: Record<string, string> = {
  Admin: "/admin",
  Teacher: "/teacher",
  Student: "/student",
};
