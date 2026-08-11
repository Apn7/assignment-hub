"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { getUser, roleHome } from "@/lib/auth";
import type { Role } from "@/types";

export function RequireRole({
  role,
  children,
}: {
  role: Role;
  children: React.ReactNode;
}) {
  const router = useRouter();
  const [ready, setReady] = useState(false);

  useEffect(() => {
    const user = getUser();
    if (!user) router.replace("/login");
    else if (user.role !== role) router.replace(roleHome[user.role]);
    else setReady(true);
  }, [role, router]);

  if (!ready) return <p className="p-8 text-gray-500">Loading…</p>;
  return <>{children}</>;
}
