"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { getUser, roleHome } from "@/lib/auth";

export default function Home() {
  const router = useRouter();
  useEffect(() => {
    const user = getUser();
    router.replace(user ? roleHome[user.role] : "/login");
  }, [router]);
  return null;
}
