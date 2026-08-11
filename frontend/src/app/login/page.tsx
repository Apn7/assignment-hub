"use client";

import axios from "axios";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { api } from "@/lib/api/client";
import { saveSession, roleHome } from "@/lib/auth";

const schema = z.object({
  email: z.string().email("Enter a valid email address"),
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
    <main className="min-h-screen flex flex-col items-center justify-center bg-[#FBF9F5] p-4 text-[#1F1D1A]">
      <div className="w-full max-w-sm space-y-6">
        <div className="text-center space-y-1">
          <h1 className="text-3xl font-serif font-bold text-[#1F1D1A] tracking-tight">
            Assignment Hub
          </h1>
          <p className="text-xs text-[#7C766C]">
            Academic Portal • Admin, Teacher & Student Login
          </p>
        </div>

        <form
          onSubmit={handleSubmit(onSubmit)}
          className="w-full bg-[#FFFFFF] rounded-2xl border border-[#E6E2D6] shadow-sm p-7 space-y-5"
        >
          <div>
            <label className="block text-xs font-semibold text-[#45413C] uppercase tracking-wider mb-1.5">
              Email Address
            </label>
            <input
              {...register("email")}
              className="w-full rounded-lg border border-[#E6E2D6] px-3.5 py-2.5 text-sm text-[#1F1D1A] bg-[#FFFFFF] placeholder:text-[#A59F93] focus:border-[#8C7B6B] focus:outline-none"
              placeholder="teacher1@assignmenthub.local"
            />
            {errors.email && (
              <p className="text-xs text-[#8C2A2A] mt-1 font-medium">
                {errors.email.message}
              </p>
            )}
          </div>

          <div>
            <label className="block text-xs font-semibold text-[#45413C] uppercase tracking-wider mb-1.5">
              Password
            </label>
            <input
              type="password"
              {...register("password")}
              className="w-full rounded-lg border border-[#E6E2D6] px-3.5 py-2.5 text-sm text-[#1F1D1A] bg-[#FFFFFF] focus:border-[#8C7B6B] focus:outline-none"
            />
            {errors.password && (
              <p className="text-xs text-[#8C2A2A] mt-1 font-medium">
                {errors.password.message}
              </p>
            )}
          </div>

          {serverError && (
            <div className="rounded-lg bg-[#FDF4F4] border border-[#F2C2C2] p-3 text-xs text-[#8C2A2A] font-medium text-center">
              {serverError}
            </div>
          )}

          <button
            disabled={isSubmitting}
            className="w-full bg-[#2D2926] hover:bg-[#1F1D1A] text-[#FBF9F5] rounded-lg py-2.5 text-sm font-semibold transition-colors disabled:opacity-50 shadow-xs"
          >
            {isSubmitting ? "Signing in…" : "Sign in to Account"}
          </button>
        </form>

        <div className="text-center text-xs text-[#7C766C]">
          Demo credentials available in project documentation.
        </div>
      </div>
    </main>
  );
}
