import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Emits `.next/standalone` with a minimal `server.js` and only the traced subset of
  // node_modules, so the container image ships ~150MB instead of ~500MB. Required by
  // frontend/Dockerfile; `npm run dev` and `npm run start` are unaffected.
  output: "standalone",
};

export default nextConfig;
