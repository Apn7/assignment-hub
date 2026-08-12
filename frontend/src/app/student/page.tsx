"use client";

import React from "react";
import Link from "next/link";
import axios from "axios";
import { useQuery, useQueries } from "@tanstack/react-query";
import { api } from "@/lib/api/client";
import type { AssignmentResponse, SubmissionResponse } from "@/types/api";
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

  const sortedAssignments = data
    ? [...data].sort(
        (a, b) => new Date(a.deadline).getTime() - new Date(b.deadline).getTime()
      )
    : [];

  // One lookup per assignment for *this* student's own submission. The list endpoint
  // carries no submission data, and without it every card looked identical — a student
  // who had submitted on time still read as "Closed / Overdue" once the deadline
  // passed, which is worse than saying nothing.
  //
  // A 404 here is the documented answer for "you have not submitted to this one", not a
  // failure, so it maps to null and retries are off. useQueries takes a dynamic-length
  // array by design, so this stays a single hook call and must sit above the early
  // returns below.
  const submissionQueries = useQueries({
    queries: sortedAssignments.map((assignment) => ({
      queryKey: ["student", "submission", assignment.id],
      retry: false,
      queryFn: async (): Promise<SubmissionResponse | null> => {
        try {
          const res = await api.get<SubmissionResponse>(
            `/api/assignments/${assignment.id}/submissions/mine`
          );
          return res.data;
        } catch (err) {
          if (axios.isAxiosError(err) && err.response?.status === 404) {
            return null;
          }
          throw err;
        }
      },
    })),
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
          {sortedAssignments.map((assignment, index) => {
            const isOverdue = isPastDeadline(assignment.deadline);
            const submissionQuery = submissionQueries[index];
            const submission = submissionQuery?.data ?? null;
            const submissionPending = submissionQuery?.isLoading ?? false;

            // Deadline alone is not the story: "missed" and "handed in, now closed" are
            // very different outcomes and used to render identically.
            const isReviewed = submission?.status === "Reviewed";
            const hasSubmitted = submission !== null;
            const isMissed = isOverdue && !hasSubmitted;

            const badge = isReviewed
              ? {
                  label: `Graded · ${submission!.marks} / ${submission!.maxMarks}`,
                  className:
                    "bg-[#F6F0F8] border-[#E2D0E8] text-[#5C2B66]",
                }
              : hasSubmitted
                ? {
                    label: isOverdue ? "Submitted · Closed" : "Submitted",
                    className: "bg-[#F0F7F4] border-[#D4E8DF] text-[#1E5641]",
                  }
                : isOverdue
                  ? {
                      label: "Missed",
                      className: "bg-[#FDF4F4] border-[#F2C2C2] text-[#8C2A2A]",
                    }
                  : {
                      label: "Not submitted",
                      className: "bg-[#FBF7EF] border-[#E8DCC2] text-[#7A5C1E]",
                    };

            return (
              <div
                key={assignment.id}
                className={`flex flex-col justify-between rounded-2xl border bg-[#FFFFFF] p-6 shadow-xs transition-all hover:shadow-md ${
                  // Red only when the work was actually missed. A closed assignment the
                  // student did hand in is not a problem and should not look like one.
                  isMissed ? "border-[#F2C2C2] bg-[#FDF9F9]" : "border-[#E6E2D6]"
                }`}
              >
                <div className="space-y-3">
                  <div className="flex items-start justify-between gap-2">
                    <span className="inline-flex items-center rounded-md bg-[#F0F4F8] px-2.5 py-1 text-xs font-semibold text-[#1D4A6E]">
                      {assignment.subjectName}
                    </span>
                    {submissionPending ? (
                      <span className="inline-flex items-center rounded-full bg-[#F5F3EE] border border-[#E6E2D6] px-2.5 py-0.5 text-xs font-semibold text-[#7C766C]">
                        …
                      </span>
                    ) : (
                      <span
                        className={`inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-semibold ${badge.className}`}
                      >
                        {badge.label}
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
                        isMissed ? "text-[#8C2A2A]" : "text-[#1F1D1A]"
                      }`}
                    >
                      {formatDateTime(assignment.deadline)}
                    </span>
                  </div>
                  <Link
                    href={`/student/assignments/${assignment.id}`}
                    className="rounded-lg bg-[#2D2926] hover:bg-[#1F1D1A] px-3.5 py-1.5 font-semibold text-[#FBF9F5] transition-colors shadow-xs"
                  >
                    {/* Names the action available in this state rather than always
                        saying "View Task", so the next step is obvious from the list. */}
                    {isReviewed
                      ? "View Grade →"
                      : hasSubmitted
                        ? "View Submission →"
                        : isOverdue
                          ? "View Task →"
                          : "Submit Answer →"}
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
