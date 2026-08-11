"use client";

import React from "react";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api/client";
import type { AssignmentResponse } from "@/types/api";
import { StatusBadge } from "@/components/StatusBadge";
import { formatDateTime } from "@/lib/date";
import { LoadingState, ErrorState, EmptyState } from "@/components/States";
import { getApiErrorMessage } from "@/lib/api/errors";

export default function TeacherDashboardPage() {
  const { data, error, isLoading, refetch } = useQuery<AssignmentResponse[]>({
    queryKey: ["teacher", "assignments", "mine"],
    queryFn: async () => {
      const res = await api.get<AssignmentResponse[]>("/api/assignments/mine");
      return res.data;
    },
  });

  if (isLoading) return <LoadingState message="Loading your assignments…" />;
  if (error)
    return (
      <ErrorState
        message={getApiErrorMessage(error, "Could not load assignments.")}
        onRetry={() => refetch()}
      />
    );

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h2 className="text-3xl font-serif font-bold tracking-tight text-[#1F1D1A]">
            My Assignments
          </h2>
          <p className="mt-1 text-xs text-[#7C766C]">
            Manage your coursework drafts, publish tasks to classes, and review student work.
          </p>
        </div>
        <Link
          href="/teacher/assignments/new"
          className="inline-flex items-center rounded-lg bg-[#2D2926] hover:bg-[#1F1D1A] px-4 py-2.5 text-xs font-semibold text-[#FBF9F5] shadow-xs transition-colors"
        >
          + Create New Assignment
        </Link>
      </div>

      {!data || data.length === 0 ? (
        <EmptyState
          title="No assignments created yet"
          description="Get started by creating your first coursework draft."
          action={
            <Link
              href="/teacher/assignments/new"
              className="inline-flex items-center rounded-lg bg-[#2D2926] px-4 py-2 text-xs font-semibold text-[#FBF9F5] hover:bg-[#1F1D1A]"
            >
              Create Assignment Draft
            </Link>
          }
        />
      ) : (
        <div className="overflow-hidden rounded-2xl border border-[#E6E2D6] bg-[#FFFFFF] shadow-xs">
          <table className="w-full text-left text-sm text-[#45413C]">
            <thead className="bg-[#F8F6F0] text-xs uppercase font-semibold text-[#7C766C] border-b border-[#E6E2D6] tracking-wider">
              <tr>
                <th className="px-6 py-3.5 font-serif font-bold text-[#1F1D1A]">Title</th>
                <th className="px-6 py-3.5">Class</th>
                <th className="px-6 py-3.5">Subject</th>
                <th className="px-6 py-3.5">Status</th>
                <th className="px-6 py-3.5">Deadline</th>
                <th className="px-6 py-3.5">Max Marks</th>
                <th className="px-6 py-3.5 text-right">Action</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[#F0EDE4]">
              {data.map((assignment) => (
                <tr
                  key={assignment.id}
                  className="hover:bg-[#FBF9F5] transition-colors"
                >
                  <td className="px-6 py-4 font-semibold text-[#1F1D1A]">
                    <Link
                      href={`/teacher/assignments/${assignment.id}`}
                      className="hover:text-[#8C7B6B] hover:underline"
                    >
                      {assignment.title}
                    </Link>
                  </td>
                  <td className="px-6 py-4 text-[#45413C]">
                    {assignment.classRoomName}
                  </td>
                  <td className="px-6 py-4 text-[#45413C]">
                    {assignment.subjectName}
                  </td>
                  <td className="px-6 py-4">
                    <StatusBadge status={assignment.status} />
                  </td>
                  <td className="px-6 py-4 text-[#45413C] text-xs">
                    {formatDateTime(assignment.deadline)}
                  </td>
                  <td className="px-6 py-4 text-[#1F1D1A] font-mono text-xs font-semibold">
                    {assignment.maxMarks}
                  </td>
                  <td className="px-6 py-4 text-right">
                    <Link
                      href={`/teacher/assignments/${assignment.id}`}
                      className="text-xs font-semibold text-[#8C7B6B] hover:text-[#1F1D1A] hover:underline"
                    >
                      View / Edit →
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
