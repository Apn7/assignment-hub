import React from "react";

export type StatusType = "Draft" | "Published" | "Submitted" | "Reviewed";

interface StatusBadgeProps {
  status: StatusType | string;
}

export function StatusBadge({ status }: StatusBadgeProps) {
  let badgeStyles = "bg-[#F3EFE6] text-[#45413C] border-[#E2DDD0]";

  switch (status) {
    case "Draft":
      badgeStyles = "bg-[#FFF8EB] text-[#855B14] border-[#F2E3C6]";
      break;
    case "Published":
      badgeStyles = "bg-[#F0F7F4] text-[#1E5641] border-[#D4E8DF]";
      break;
    case "Submitted":
      badgeStyles = "bg-[#F0F4F8] text-[#1D4A6E] border-[#D3E0EA]";
      break;
    case "Reviewed":
      badgeStyles = "bg-[#F7F2F8] text-[#5C2B66] border-[#E6D6EB]";
      break;
  }

  return (
    <span
      className={`inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-semibold tracking-wide ${badgeStyles}`}
    >
      {status}
    </span>
  );
}
