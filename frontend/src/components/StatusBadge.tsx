import React from "react";

export type StatusType = "Draft" | "Published" | "Submitted" | "Reviewed";

interface StatusBadgeProps {
  status: StatusType | string;
}

export function StatusBadge({ status }: StatusBadgeProps) {
  let badgeStyles = "bg-gray-100 text-gray-800 border-gray-200";

  switch (status) {
    case "Draft":
      badgeStyles = "bg-amber-50 text-amber-800 border-amber-200/80";
      break;
    case "Published":
      badgeStyles = "bg-emerald-50 text-emerald-800 border-emerald-200/80";
      break;
    case "Submitted":
      badgeStyles = "bg-blue-50 text-blue-800 border-blue-200/80";
      break;
    case "Reviewed":
      badgeStyles = "bg-purple-50 text-purple-800 border-purple-200/80";
      break;
  }

  return (
    <span
      className={`inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-semibold ${badgeStyles}`}
    >
      {status}
    </span>
  );
}
