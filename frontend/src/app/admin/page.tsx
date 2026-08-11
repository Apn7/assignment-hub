"use client";

import React, { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api/client";
import type { AssignmentResponse, SubmissionListItem } from "@/types/api";
import { StatusBadge } from "@/components/StatusBadge";
import { formatDateTime } from "@/lib/date";
import { LoadingState, ErrorState, EmptyState } from "@/components/States";
import { getApiErrorMessage } from "@/lib/api/errors";

export default function AdminDashboardPage() {
  const [activeTab, setActiveTab] = useState<"assignments" | "submissions">(
    "assignments"
  );
  const [assignmentStatusFilter, setAssignmentStatusFilter] = useState<string>("");
  const [submissionStatusFilter, setSubmissionStatusFilter] = useState<string>("");

  // Fetch all assignments for admin
  const {
    data: assignments,
    isLoading: assignmentsLoading,
    error: assignmentsError,
    refetch: refetchAssignments,
  } = useQuery<AssignmentResponse[]>({
    queryKey: ["admin", "assignments", assignmentStatusFilter],
    queryFn: async () => {
      const params = assignmentStatusFilter ? { status: assignmentStatusFilter } : {};
      const res = await api.get<AssignmentResponse[]>("/api/admin/assignments", { params });
      return res.data;
    },
    enabled: activeTab === "assignments",
  });

  // Fetch all submissions for admin
  const {
    data: submissions,
    isLoading: submissionsLoading,
    error: submissionsError,
    refetch: refetchSubmissions,
  } = useQuery<SubmissionListItem[]>({
    queryKey: ["admin", "submissions", submissionStatusFilter],
    queryFn: async () => {
      const params = submissionStatusFilter ? { status: submissionStatusFilter } : {};
      const res = await api.get<SubmissionListItem[]>("/api/admin/submissions", { params });
      return res.data;
    },
    enabled: activeTab === "submissions",
  });

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold tracking-tight text-gray-900">
          Admin Read-Only Overview
        </h2>
        <p className="mt-1 text-sm text-gray-500">
          System-wide audit view across all classes, subjects, teachers, and students.
        </p>
      </div>

      {/* Tabs */}
      <div className="flex border-b border-gray-200">
        <button
          type="button"
          onClick={() => setActiveTab("assignments")}
          className={`px-4 py-2.5 text-sm font-medium border-b-2 transition-colors ${
            activeTab === "assignments"
              ? "border-blue-600 text-blue-600"
              : "border-transparent text-gray-500 hover:text-gray-700"
          }`}
        >
          All Assignments
        </button>
        <button
          type="button"
          onClick={() => setActiveTab("submissions")}
          className={`px-4 py-2.5 text-sm font-medium border-b-2 transition-colors ${
            activeTab === "submissions"
              ? "border-blue-600 text-blue-600"
              : "border-transparent text-gray-500 hover:text-gray-700"
          }`}
        >
          All Submissions
        </button>
      </div>

      {/* TAB 1: ASSIGNMENTS */}
      {activeTab === "assignments" && (
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold text-gray-500 uppercase tracking-wider">
              System Assignments ({assignments?.length ?? 0})
            </span>
            <div className="flex items-center gap-2">
              <label className="text-xs text-gray-600">Filter Status:</label>
              <select
                value={assignmentStatusFilter}
                onChange={(e) => setAssignmentStatusFilter(e.target.value)}
                className="rounded-lg border border-gray-300 bg-white px-3 py-1.5 text-xs font-medium text-gray-700 focus:border-blue-500 focus:outline-none"
              >
                <option value="">All Statuses</option>
                <option value="Draft">Draft</option>
                <option value="Published">Published</option>
              </select>
            </div>
          </div>

          {assignmentsLoading ? (
            <LoadingState message="Loading system assignments…" />
          ) : assignmentsError ? (
            <ErrorState
              message={getApiErrorMessage(assignmentsError, "Failed to load assignments.")}
              onRetry={() => refetchAssignments()}
            />
          ) : !assignments || assignments.length === 0 ? (
            <EmptyState
              title="No assignments found"
              description="No assignments match the selected filter."
            />
          ) : (
            <div className="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm">
              <table className="w-full text-left text-sm text-gray-600">
                <thead className="bg-gray-50 text-xs uppercase font-semibold text-gray-700 border-b border-gray-200">
                  <tr>
                    <th className="px-6 py-3.5">Title</th>
                    <th className="px-6 py-3.5">Class</th>
                    <th className="px-6 py-3.5">Subject</th>
                    <th className="px-6 py-3.5">Teacher</th>
                    <th className="px-6 py-3.5">Status</th>
                    <th className="px-6 py-3.5">Deadline</th>
                    <th className="px-6 py-3.5">Max Marks</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-200">
                  {assignments.map((a) => (
                    <tr key={a.id} className="hover:bg-gray-50/80">
                      <td className="px-6 py-4 font-medium text-gray-900">
                        {a.title}
                      </td>
                      <td className="px-6 py-4 text-gray-700">{a.classRoomName}</td>
                      <td className="px-6 py-4 text-gray-700">{a.subjectName}</td>
                      <td className="px-6 py-4 text-gray-700">{a.createdByTeacherName}</td>
                      <td className="px-6 py-4">
                        <StatusBadge status={a.status} />
                      </td>
                      <td className="px-6 py-4 text-gray-700">
                        {formatDateTime(a.deadline)}
                      </td>
                      <td className="px-6 py-4 font-mono text-gray-700">
                        {a.maxMarks}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {/* TAB 2: SUBMISSIONS */}
      {activeTab === "submissions" && (
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold text-gray-500 uppercase tracking-wider">
              System Submissions ({submissions?.length ?? 0})
            </span>
            <div className="flex items-center gap-2">
              <label className="text-xs text-gray-600">Filter Status:</label>
              <select
                value={submissionStatusFilter}
                onChange={(e) => setSubmissionStatusFilter(e.target.value)}
                className="rounded-lg border border-gray-300 bg-white px-3 py-1.5 text-xs font-medium text-gray-700 focus:border-blue-500 focus:outline-none"
              >
                <option value="">All Statuses</option>
                <option value="Submitted">Submitted</option>
                <option value="Reviewed">Reviewed</option>
              </select>
            </div>
          </div>

          {submissionsLoading ? (
            <LoadingState message="Loading system submissions…" />
          ) : submissionsError ? (
            <ErrorState
              message={getApiErrorMessage(submissionsError, "Failed to load submissions.")}
              onRetry={() => refetchSubmissions()}
            />
          ) : !submissions || submissions.length === 0 ? (
            <EmptyState
              title="No submissions found"
              description="No student submissions match the selected filter."
            />
          ) : (
            <div className="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm">
              <table className="w-full text-left text-sm text-gray-600">
                <thead className="bg-gray-50 text-xs uppercase font-semibold text-gray-700 border-b border-gray-200">
                  <tr>
                    <th className="px-6 py-3.5">Assignment</th>
                    <th className="px-6 py-3.5">Class</th>
                    <th className="px-6 py-3.5">Student</th>
                    <th className="px-6 py-3.5">Status</th>
                    <th className="px-6 py-3.5">Submitted At</th>
                    <th className="px-6 py-3.5">Score</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-200">
                  {submissions.map((sub) => (
                    <tr key={sub.id} className="hover:bg-gray-50/80">
                      <td className="px-6 py-4 font-medium text-gray-900">
                        {sub.assignmentTitle}
                      </td>
                      <td className="px-6 py-4 text-gray-700">{sub.classRoomName}</td>
                      <td className="px-6 py-4 text-gray-700">{sub.studentName}</td>
                      <td className="px-6 py-4">
                        <StatusBadge status={sub.status} />
                      </td>
                      <td className="px-6 py-4 text-gray-700">
                        {formatDateTime(sub.submittedAt)}
                      </td>
                      <td className="px-6 py-4 font-mono font-medium text-gray-900">
                        {sub.marks !== null ? `${sub.marks} / ${sub.maxMarks}` : "Ungraded"}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
