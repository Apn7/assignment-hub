"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { getUser, roleHome } from "@/lib/auth";
import { LoadingState } from "@/components/States";

export default function Home() {
  const router = useRouter();
  useEffect(() => {
    const user = getUser();
    router.replace(user ? roleHome[user.role] : "/login");
  }, [router]);

  // The session lives in localStorage, so the role is only readable after mount and
  // this route always renders once before redirecting. Returning null showed a blank
  // white page as the very first thing a visitor sees; a spinner reads as loading
  // rather than as a broken deploy.
  return <LoadingState message="Loading Assignment Hub…" />;
}
