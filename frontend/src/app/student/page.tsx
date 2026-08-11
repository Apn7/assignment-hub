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

  // Order by deadline nearest first
  const sortedAssignments = data
    ? [...data].sort(
        (a, b) => new Date(a.deadline).getTime() - new Date(b.deadline).getTime()
      )
    : [];

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold tracking-tight text-gray-900">
          Class Assignments
        </h2>
        <p className="mt-1 text-sm text-gray-500">
          View and submit coursework assigned to your class.
        </p>
      </div>

      {!sortedAssignments || sortedAssignments.length === 0 ? (
        <EmptyState
          title="No assignments posted yet"
          description="Your teachers have not published any assignments for your class."
        />
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {sortedAssignments.map((assignment) => {
            const isOverdue = isPastDeadline(assignment.deadline);

            return (
              <div
                key={assignment.id}
                className={`flex flex-col justify-between rounded-xl border bg-white p-5 shadow-sm transition-all hover:shadow-md ${
                  isOverdue ? "border-red-200 bg-red-50/20" : "border-gray-200"
                }`}
              >
                <div className="space-y-3">
                  <div className="flex items-start justify-between gap-2">
                    <span className="inline-flex items-center rounded-md bg-blue-50 px-2 py-1 text-xs font-semibold text-blue-700">
                      {assignment.subjectName}
                    </span>
                    {isOverdue ? (
                      <span className="inline-flex items-center rounded-full bg-red-100 px-2 py-0.5 text-xs font-semibold text-red-800">
                        Closed / Overdue
                      </span>
                    ) : (
                      <span className="inline-flex items-center rounded-full bg-emerald-100 px-2 py-0.5 text-xs font-semibold text-emerald-800">
                        Open
                      </span>
                    )}
                  </div>

                  <div>
                    <h3 className="font-bold text-gray-900 text-lg line-clamp-2">
                      {assignment.title}
                    </h3>
                    <p className="mt-1 text-xs text-gray-500 line-clamp-3">
                      {assignment.description}
                    </p>
                  </div>
                </div>

                <div className="mt-5 pt-4 border-t border-gray-100 flex items-center justify-between text-xs">
                  <div>
                    <span className="text-gray-500 block">Due Date:</span>
                    <span
                      className={`font-semibold ${
                        isOverdue ? "text-red-600" : "text-gray-700"
                      }`}
                    >
                      {formatDateTime(assignment.deadline)}
                    </span>
                  </div>
                  <Link
                    href={`/student/assignments/${assignment.id}`}
                    className="rounded-lg bg-blue-600 px-3 py-1.5 font-semibold text-white hover:bg-blue-700 transition-colors"
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
