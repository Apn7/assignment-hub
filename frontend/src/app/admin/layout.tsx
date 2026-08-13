"use client";

import { useRouter } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import { RequireRole } from "@/components/RequireRole";
import { getUser, clearSession } from "@/lib/auth";

export default function AdminLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const router = useRouter();
  const queryClient = useQueryClient();
  const user = getUser();

  const handleLogout = () => {
    clearSession();
    // The QueryClient is created once in Providers and survives this navigation,
    // because logging out routes client-side rather than reloading the page. Without
    // clearing it, the next user to sign in reads the previous user's cached entries —
    // query keys carry no user identity, and staleTime keeps them "fresh" for 30s, so
    // their dashboard paints someone else's data before the first refetch lands.
    queryClient.clear();
    // replace, not push: Back must not return to a screen belonging to the old session.
    router.replace("/login");
  };

  return (
    <RequireRole role="Admin">
      <div className="min-h-screen flex flex-col bg-[#FBF9F5]">
        <header className="sticky top-0 z-40 bg-[#FFFFFF]/90 backdrop-blur-sm border-b border-[#E6E2D6] px-6 py-3.5 shadow-xs">
          <div className="max-w-7xl mx-auto flex items-center justify-between">
            <div className="flex items-center gap-3">
              <h1 className="text-xl font-serif font-bold text-[#1F1D1A] tracking-tight">
                Assignment Hub
              </h1>
              <span className="rounded-full bg-[#F5F5F5] border border-[#E0E0E0] px-2.5 py-0.5 text-xs font-semibold text-[#424242]">
                Admin Portal
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
