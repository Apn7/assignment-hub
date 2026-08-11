"use client";

import { useRouter } from "next/navigation";
import { RequireRole } from "@/components/RequireRole";
import { getUser, clearSession } from "@/lib/auth";

export default function AdminLayout({
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
    <RequireRole role="Admin">
      <header className="flex items-center justify-between px-6 py-3 bg-white border-b shadow-sm">
        <h1 className="text-lg font-semibold">Assignment Hub</h1>
        <div className="flex items-center gap-4">
          <span className="text-sm text-gray-600">{user?.fullName}</span>
          <button
            onClick={handleLogout}
            className="text-sm text-red-600 hover:underline"
          >
            Logout
          </button>
        </div>
      </header>
      <main className="p-6">{children}</main>
    </RequireRole>
  );
}
