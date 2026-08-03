'use client';

import { useQuery } from '@tanstack/react-query';
import { API_BASE_URL, getHealth, healthQueryKey } from '@/lib/api';

/**
 * Placeholder landing page. Its only job is to prove the frontend can reach the
 * backend: it calls `GET /api/health` through the shared axios client and
 * renders the result. Real role-based pages replace this.
 */
export default function Home() {
  const { data, error, isPending, isFetching, refetch } = useQuery({
    queryKey: healthQueryKey,
    queryFn: getHealth,
  });

  return (
    <div className="flex flex-1 items-center justify-center bg-zinc-50 px-4 py-12 font-sans dark:bg-black">
      <main className="w-full max-w-xl">
        <h1 className="text-2xl font-semibold tracking-tight text-black sm:text-3xl dark:text-zinc-50">
          Assignment Hub
        </h1>
        <p className="mt-2 text-sm text-zinc-600 sm:text-base dark:text-zinc-400">
          Scaffold only — no features implemented yet.
        </p>

        <section
          aria-labelledby="api-status-heading"
          className="mt-8 rounded-xl border border-black/[.08] bg-white p-5 sm:p-6 dark:border-white/[.145] dark:bg-zinc-950"
        >
          <div className="flex flex-wrap items-center justify-between gap-3">
            <h2
              id="api-status-heading"
              className="text-sm font-medium text-zinc-900 dark:text-zinc-100"
            >
              Backend health check
            </h2>
            <button
              type="button"
              onClick={() => refetch()}
              disabled={isFetching}
              className="rounded-full border border-black/[.08] px-3 py-1 text-xs font-medium transition-colors hover:bg-black/[.04] disabled:opacity-50 dark:border-white/[.145] dark:hover:bg-white/[.06]"
            >
              {isFetching ? 'Checking…' : 'Re-check'}
            </button>
          </div>

          <dl className="mt-4 space-y-3 text-sm">
            <div className="flex flex-wrap items-baseline gap-x-2">
              <dt className="text-zinc-500 dark:text-zinc-400">Endpoint</dt>
              <dd className="font-mono text-xs break-all text-zinc-700 dark:text-zinc-300">
                {API_BASE_URL}/api/health
              </dd>
            </div>

            {isPending && (
              <p className="text-zinc-500 dark:text-zinc-400">
                Contacting the API…
              </p>
            )}

            {error && (
              <div className="rounded-lg bg-red-50 p-3 text-red-800 dark:bg-red-950/40 dark:text-red-300">
                <p className="font-medium">Could not reach the API.</p>
                <p className="mt-1 text-xs break-words">{error.message}</p>
                <p className="mt-2 text-xs">
                  Start the backend with{' '}
                  <code className="font-mono">dotnet run</code> from{' '}
                  <code className="font-mono">
                    backend/src/AssignmentHub.Api
                  </code>
                  , and confirm{' '}
                  <code className="font-mono">NEXT_PUBLIC_API_URL</code> points
                  at it.
                </p>
              </div>
            )}

            {data && (
              <>
                <div className="flex flex-wrap items-baseline gap-x-2">
                  <dt className="text-zinc-500 dark:text-zinc-400">Status</dt>
                  <dd className="font-medium text-green-700 dark:text-green-400">
                    {data.status}
                  </dd>
                </div>
                <div className="flex flex-wrap items-baseline gap-x-2">
                  <dt className="text-zinc-500 dark:text-zinc-400">
                    Environment
                  </dt>
                  <dd className="text-zinc-700 dark:text-zinc-300">
                    {data.environment}
                  </dd>
                </div>
                <div className="flex flex-wrap items-baseline gap-x-2">
                  <dt className="text-zinc-500 dark:text-zinc-400">
                    Server time
                  </dt>
                  <dd className="font-mono text-xs text-zinc-700 dark:text-zinc-300">
                    {data.timestampUtc}
                  </dd>
                </div>
              </>
            )}
          </dl>
        </section>
      </main>
    </div>
  );
}
