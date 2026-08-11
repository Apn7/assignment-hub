"use client";

import React from "react";

interface ConfirmDialogProps {
  isOpen: boolean;
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  isPending?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

export function ConfirmDialog({
  isOpen,
  title,
  message,
  confirmLabel = "Confirm",
  cancelLabel = "Cancel",
  isPending = false,
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-[#1F1D1A]/35 backdrop-blur-[2px] p-4">
      <div className="w-full max-w-md rounded-2xl bg-[#FFFFFF] p-6 border border-[#E6E2D6] shadow-xl space-y-4">
        <h3 className="text-xl font-serif font-bold text-[#1F1D1A]">{title}</h3>
        <p className="text-sm text-[#45413C] leading-relaxed">{message}</p>

        <div className="flex justify-end gap-3 pt-2">
          <button
            type="button"
            onClick={onCancel}
            disabled={isPending}
            className="rounded-lg border border-[#E6E2D6] bg-[#FBF9F5] px-4 py-2 text-sm font-medium text-[#45413C] hover:bg-[#F3EFE6] disabled:opacity-50 transition-colors"
          >
            {cancelLabel}
          </button>
          <button
            type="button"
            onClick={onConfirm}
            disabled={isPending}
            className="rounded-lg bg-[#2D2926] px-4 py-2 text-sm font-semibold text-[#FBF9F5] hover:bg-[#1F1D1A] disabled:opacity-50 transition-colors shadow-sm"
          >
            {isPending ? "Processing…" : confirmLabel}
          </button>
        </div>
      </div>
    </div>
  );
}
