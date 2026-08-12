import type { Metadata } from "next";
import localFont from "next/font/local";
import "./globals.css";
import { Providers } from "./providers";

const lora = localFont({
  src: [
    {
      path: "../../public/fonts/lora-regular.ttf",
      weight: "400",
      style: "normal",
    },
    {
      path: "../../public/fonts/lora-bold.ttf",
      weight: "700",
      style: "normal",
    },
  ],
  variable: "--font-serif",
  display: "swap",
});

const plusJakartaSans = localFont({
  src: [
    {
      path: "../../public/fonts/plus-jakarta-sans-regular.ttf",
      weight: "400",
      style: "normal",
    },
    {
      path: "../../public/fonts/plus-jakarta-sans-semibold.ttf",
      weight: "600",
      style: "normal",
    },
    {
      path: "../../public/fonts/plus-jakarta-sans-bold.ttf",
      weight: "700",
      style: "normal",
    },
  ],
  variable: "--font-sans",
  display: "swap",
});

export const metadata: Metadata = {
  title: "Assignment Hub",
  description:
    "Role-based assignment and submission management system with paper aesthetic.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html
      lang="en"
      className={`${lora.variable} ${plusJakartaSans.variable} h-full antialiased`}
    >
      <body className="min-h-full flex flex-col bg-[#FBF9F5] text-[#1F1D1A] font-sans selection:bg-[#EFE8D8] selection:text-[#1F1D1A]">
        <Providers>{children}</Providers>
      </body>
    </html>
  );
}
