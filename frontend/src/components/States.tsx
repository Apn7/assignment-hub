import React from "react";

export function LoadingState({ message = "Loading data…" }: { message?: string }) {
  return (
    <div className="flex items-center justify-center p-12 text-[#7C766C] text-sm font-medium">
      <svg
        className="mr-3 h-5 w-5 animate-spin text-[#8C7B6B]"
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
    <div className="rounded-2xl border border-[#F2C2C2] bg-[#FDF4F4] p-6 text-center text-[#8C2A2A]">
      <p className="text-sm font-medium">{message}</p>
      {onRetry && (
        <button
          type="button"
          onClick={onRetry}
          className="mt-3 inline-flex items-center rounded-lg bg-[#8C2A2A] px-3.5 py-1.5 text-xs font-semibold text-white hover:bg-[#6E2020] transition-colors"
        >
          Try again
        </button>
      )}
    </div>
  );
}

export function EmptyState({
  title = "No items found",
  description = "There are no items to display at this time.",
  action,
}: {
  title?: string;
  description?: string;
  action?: React.ReactNode;
}) {
  return (
    <div className="rounded-2xl border border-dashed border-[#E6E2D6] bg-[#FFFFFF] p-12 text-center shadow-xs">
      <h3 className="text-base font-serif font-bold text-[#1F1D1A]">{title}</h3>
      <p className="mt-1 text-xs text-[#7C766C] max-w-sm mx-auto">{description}</p>
      {action && <div className="mt-5">{action}</div>}
    </div>
  );
}
