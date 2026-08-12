# syntax=docker/dockerfile:1

# ---------------------------------------------------------------------------
# Dependencies
# ---------------------------------------------------------------------------
FROM node:22-alpine AS deps
WORKDIR /app

# Lockfile-only layer so `npm ci` is reused whenever dependencies have not changed.
COPY package.json package-lock.json ./
RUN npm ci

# ---------------------------------------------------------------------------
# Build
# ---------------------------------------------------------------------------
FROM node:22-alpine AS build
WORKDIR /app

COPY --from=deps /app/node_modules ./node_modules
COPY . .

# NEXT_PUBLIC_* values are inlined into the client bundle by `next build`, so the API
# URL must be present *here*. Setting it at runtime has no effect — the string is
# already compiled into the JavaScript the browser downloads.
ARG NEXT_PUBLIC_API_URL
ENV NEXT_PUBLIC_API_URL=$NEXT_PUBLIC_API_URL

RUN test -n "$NEXT_PUBLIC_API_URL" || \
    (echo "ERROR: NEXT_PUBLIC_API_URL build-arg is required; the bundle would otherwise fall back to localhost." && exit 1)

RUN npm run build

# ---------------------------------------------------------------------------
# Runtime
# ---------------------------------------------------------------------------
FROM node:22-alpine AS runtime
WORKDIR /app

ENV NODE_ENV=production

# `output: "standalone"` emits a self-contained server plus only the traced subset of
# node_modules. It deliberately leaves out `public` and `.next/static` — those are
# expected to sit behind a CDN — so they are copied in explicitly, after which
# server.js serves them itself.
COPY --from=build /app/.next/standalone ./
COPY --from=build /app/.next/static ./.next/static
COPY --from=build /app/public ./public

# server.js honours both of these; 0.0.0.0 is required for ingress to reach it.
ENV PORT=3000
ENV HOSTNAME=0.0.0.0
EXPOSE 3000

CMD ["node", "server.js"]
