import React from "react";

export function LoadingState({ message = "Loading data…" }: { message?: string }) {
  return (
    <div className="flex items-center justify-center p-12 text-gray-500 text-sm">
      <svg
        className="mr-3 h-5 w-5 animate-spin text-blue-600"
        xmlns="http://www.w3.org/2000/svg"
        fill="none"
        viewBox="0 0 24 24"
      >
        <circle
          className="opacity-25"
          cx="12"
          cy="12"
          r="10"
          stroke="currentColor"
          strokeWidth="4"
        ></circle>
        <path
          className="opacity-75"
          fill="currentColor"
          d="M4 12a8 8 0 018-8v8H4z"
        ></path>
      </svg>
      {message}
    </div>
  );
}

export function ErrorState({
  message = "Failed to load data.",
  onRetry,
}: {
  message?: string;
  onRetry?: () => void;
}) {
  return (
    <div className="rounded-xl border border-red-200 bg-red-50 p-6 text-center text-red-700">
      <p className="text-sm font-medium">{message}</p>
      {onRetry && (
        <button
          type="button"
          onClick={onRetry}
          className="mt-3 inline-flex items-center rounded-lg bg-red-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-red-700"
        >
          Try again
        </button>
      )}
    </div>
  );
}

export function EmptyState({
  title = "No data found",
  description = "There are no items to display at this time.",
  action,
}: {
  title?: string;
  description?: string;
  action?: React.ReactNode;
}) {
  return (
    <div className="rounded-xl border border-dashed border-gray-300 p-12 text-center">
      <h3 className="text-sm font-semibold text-gray-900">{title}</h3>
      <p className="mt-1 text-xs text-gray-500">{description}</p>
      {action && <div className="mt-4">{action}</div>}
    </div>
  );
}
