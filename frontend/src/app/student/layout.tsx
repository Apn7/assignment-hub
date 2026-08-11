"use client";

import { useRouter } from "next/navigation";
import { RequireRole } from "@/components/RequireRole";
import { getUser, clearSession } from "@/lib/auth";

export default function StudentLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const router = useRouter();
  const user = getUser();

  const handleLogout = () => {
    clearSession();
    router.push("/login");
  };

  return (
    <RequireRole role="Student">
      <div className="min-h-screen flex flex-col bg-[#FBF9F5]">
        <header className="sticky top-0 z-40 bg-[#FFFFFF]/90 backdrop-blur-sm border-b border-[#E6E2D6] px-6 py-3.5 shadow-xs">
          <div className="max-w-7xl mx-auto flex items-center justify-between">
            <div className="flex items-center gap-3">
              <h1 className="text-xl font-serif font-bold text-[#1F1D1A] tracking-tight">
                Assignment Hub
              </h1>
              <span className="rounded-full bg-[#F0F4F8] border border-[#D3E0EA] px-2.5 py-0.5 text-xs font-semibold text-[#1D4A6E]">
                Student Portal
              </span>
            </div>
            <div className="flex items-center gap-4 text-xs">
              <span className="font-medium text-[#45413C]">
                {user?.fullName}
              </span>
              <button
                onClick={handleLogout}
                className="text-[#8C2A2A] hover:underline font-semibold"
              >
                Logout
              </button>
            </div>
          </div>
        </header>
        <main className="flex-1 max-w-7xl w-full mx-auto p-6">{children}</main>
      </div>
    </RequireRole>
  );
}
