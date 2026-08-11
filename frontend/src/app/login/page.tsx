"use client";

import axios from "axios";
import { useForm } from "react-hook-form";
import { z } from "zod/v4";
import { zodResolver } from "@hookform/resolvers/zod";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { api } from "@/lib/api/client";
import { saveSession, roleHome } from "@/lib/auth";

const schema = z.object({
  email: z.email("Enter a valid email"),
  password: z.string().min(1, "Password is required"),
});
type FormData = z.infer<typeof schema>;

export default function LoginPage() {
  const router = useRouter();
  const [serverError, setServerError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormData>({ resolver: zodResolver(schema) });

  const onSubmit = async (data: FormData) => {
    setServerError(null);
    try {
      const res = await api.post("/api/auth/login", data);
      saveSession(res.data.accessToken, res.data.user);
      router.replace(roleHome[res.data.user.role] ?? "/login");
    } catch (err: unknown) {
      const status = axios.isAxiosError(err)
        ? err.response?.status
        : undefined;
      setServerError(
        status === 401
          ? "Invalid email or password."
          : "Something went wrong. Is the API running?"
      );
    }
  };

  return (
    <main className="min-h-screen flex items-center justify-center bg-gray-50 p-4">
      <form
        onSubmit={handleSubmit(onSubmit)}
        className="w-full max-w-sm bg-white rounded-xl shadow p-6 space-y-4"
      >
        <h1 className="text-xl font-semibold">Assignment Hub</h1>
        <div>
          <label className="block text-sm mb-1">Email</label>
          <input
            {...register("email")}
            className="w-full border rounded px-3 py-2"
            placeholder="teacher1@assignmenthub.local"
          />
          {errors.email && (
            <p className="text-sm text-red-600 mt-1">{errors.email.message}</p>
          )}
        </div>
        <div>
          <label className="block text-sm mb-1">Password</label>
          <input
            type="password"
            {...register("password")}
            className="w-full border rounded px-3 py-2"
          />
          {errors.password && (
            <p className="text-sm text-red-600 mt-1">
              {errors.password.message}
            </p>
          )}
        </div>
        {serverError && <p className="text-sm text-red-600">{serverError}</p>}
        <button
          disabled={isSubmitting}
          className="w-full bg-blue-600 text-white rounded py-2 disabled:opacity-50"
        >
          {isSubmitting ? "Signing in…" : "Sign in"}
        </button>
      </form>
    </main>
  );
}
