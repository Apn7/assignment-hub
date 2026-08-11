"use client";

import React from "react";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api/client";
import type { AssignmentResponse } from "@/types/api";
import { formatDateTime, isPastDeadline } from "@/lib/date";
import { LoadingState, ErrorState, EmptyState } from "@/components/States";
import { getApiErrorMessage } from "@/lib/api/errors";

export default function StudentDashboardPage() {
  const { data, error, isLoading, refetch } = useQuery<AssignmentResponse[]>({
    queryKey: ["student", "assignments"],
    queryFn: async () => {
      const res = await api.get<AssignmentResponse[]>("/api/assignments");
      return res.data;
    },
  });

  if (isLoading)
    return <LoadingState message="Loading assignments for your class…" />;
  if (error)
    return (
      <ErrorState
        message={getApiErrorMessage(error, "Could not load class assignments.")}
        onRetry={() => refetch()}
      />
    );

  const sortedAssignments = data
    ? [...data].sort(
        (a, b) => new Date(a.deadline).getTime() - new Date(b.deadline).getTime()
      )
    : [];

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-3xl font-serif font-bold tracking-tight text-[#1F1D1A]">
          Class Assignments
        </h2>
        <p className="mt-1 text-xs text-[#7C766C]">
          Coursework assigned to your class, ordered by due date.
        </p>
      </div>

      {!sortedAssignments || sortedAssignments.length === 0 ? (
        <EmptyState
          title="No assignments posted yet"
          description="Your teachers have not published any assignments for your class."
        />
      ) : (
        <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3">
          {sortedAssignments.map((assignment) => {
            const isOverdue = isPastDeadline(assignment.deadline);

            return (
              <div
                key={assignment.id}
                className={`flex flex-col justify-between rounded-2xl border bg-[#FFFFFF] p-6 shadow-xs transition-all hover:shadow-md ${
                  isOverdue
                    ? "border-[#F2C2C2] bg-[#FDF9F9]"
                    : "border-[#E6E2D6]"
                }`}
              >
                <div className="space-y-3">
                  <div className="flex items-start justify-between gap-2">
                    <span className="inline-flex items-center rounded-md bg-[#F0F4F8] px-2.5 py-1 text-xs font-semibold text-[#1D4A6E]">
                      {assignment.subjectName}
                    </span>
                    {isOverdue ? (
                      <span className="inline-flex items-center rounded-full bg-[#FDF4F4] border border-[#F2C2C2] px-2.5 py-0.5 text-xs font-semibold text-[#8C2A2A]">
                        Closed / Overdue
                      </span>
                    ) : (
                      <span className="inline-flex items-center rounded-full bg-[#F0F7F4] border border-[#D4E8DF] px-2.5 py-0.5 text-xs font-semibold text-[#1E5641]">
                        Open
                      </span>
                    )}
                  </div>

                  <div>
                    <h3 className="font-serif font-bold text-[#1F1D1A] text-xl line-clamp-2">
                      {assignment.title}
                    </h3>
                    <p className="mt-2 text-xs text-[#7C766C] line-clamp-3 leading-relaxed">
                      {assignment.description}
                    </p>
                  </div>
                </div>

                <div className="mt-6 pt-4 border-t border-[#F0EDE4] flex items-center justify-between text-xs">
                  <div>
                    <span className="text-[#7C766C] block">Due Date:</span>
                    <span
                      className={`font-semibold ${
                        isOverdue ? "text-[#8C2A2A]" : "text-[#1F1D1A]"
                      }`}
                    >
                      {formatDateTime(assignment.deadline)}
                    </span>
                  </div>
                  <Link
                    href={`/student/assignments/${assignment.id}`}
                    className="rounded-lg bg-[#2D2926] hover:bg-[#1F1D1A] px-3.5 py-1.5 font-semibold text-[#FBF9F5] transition-colors shadow-xs"
                  >
                    View Task →
                  </Link>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
