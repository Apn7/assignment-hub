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
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold tracking-tight text-gray-900">
            My Assignments
          </h2>
          <p className="mt-1 text-sm text-gray-500">
            Manage your draft and published assignments across your classes.
          </p>
        </div>
        <Link
          href="/teacher/assignments/new"
          className="inline-flex items-center rounded-lg bg-blue-600 px-4 py-2 text-sm font-semibold text-white shadow-sm hover:bg-blue-700 transition-colors"
        >
          + New Assignment
        </Link>
      </div>

      {!data || data.length === 0 ? (
        <EmptyState
          title="No assignments created yet"
          description="Get started by creating your first assignment draft."
          action={
            <Link
              href="/teacher/assignments/new"
              className="inline-flex items-center rounded-lg bg-blue-600 px-4 py-2 text-sm font-semibold text-white hover:bg-blue-700"
            >
              Create Assignment
            </Link>
          }
        />
      ) : (
        <div className="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm">
          <table className="w-full text-left text-sm text-gray-600">
            <thead className="bg-gray-50 text-xs uppercase font-semibold text-gray-700 border-b border-gray-200">
              <tr>
                <th className="px-6 py-3.5">Title</th>
                <th className="px-6 py-3.5">Class</th>
                <th className="px-6 py-3.5">Subject</th>
                <th className="px-6 py-3.5">Status</th>
                <th className="px-6 py-3.5">Deadline</th>
                <th className="px-6 py-3.5">Max Marks</th>
                <th className="px-6 py-3.5 text-right">Action</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200">
              {data.map((assignment) => (
                <tr
                  key={assignment.id}
                  className="hover:bg-gray-50/80 transition-colors"
                >
                  <td className="px-6 py-4 font-medium text-gray-900">
                    <Link
                      href={`/teacher/assignments/${assignment.id}`}
                      className="hover:text-blue-600 hover:underline"
                    >
                      {assignment.title}
                    </Link>
                  </td>
                  <td className="px-6 py-4 text-gray-700">
                    {assignment.classRoomName}
                  </td>
                  <td className="px-6 py-4 text-gray-700">
                    {assignment.subjectName}
                  </td>
                  <td className="px-6 py-4">
                    <StatusBadge status={assignment.status} />
                  </td>
                  <td className="px-6 py-4 text-gray-700">
                    {formatDateTime(assignment.deadline)}
                  </td>
                  <td className="px-6 py-4 text-gray-700 font-mono">
                    {assignment.maxMarks}
                  </td>
                  <td className="px-6 py-4 text-right">
                    <Link
                      href={`/teacher/assignments/${assignment.id}`}
                      className="text-xs font-semibold text-blue-600 hover:text-blue-800 hover:underline"
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
