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
        <h2 className="text-3xl font-serif font-bold tracking-tight text-[#1F1D1A]">
          Admin Read-Only Overview
        </h2>
        <p className="mt-1 text-xs text-[#7C766C]">
          System-wide audit view across all classes, subjects, teachers, and students.
        </p>
      </div>

      {/* Tabs */}
      <div className="flex border-b border-[#E6E2D6]">
        <button
          type="button"
          onClick={() => setActiveTab("assignments")}
          className={`px-5 py-2.5 text-xs font-semibold border-b-2 transition-colors ${
            activeTab === "assignments"
              ? "border-[#2D2926] text-[#1F1D1A]"
              : "border-transparent text-[#7C766C] hover:text-[#1F1D1A]"
          }`}
        >
          All Assignments
        </button>
        <button
          type="button"
          onClick={() => setActiveTab("submissions")}
          className={`px-5 py-2.5 text-xs font-semibold border-b-2 transition-colors ${
            activeTab === "submissions"
              ? "border-[#2D2926] text-[#1F1D1A]"
              : "border-transparent text-[#7C766C] hover:text-[#1F1D1A]"
          }`}
        >
          All Submissions
        </button>
      </div>

      {/* TAB 1: ASSIGNMENTS */}
      {activeTab === "assignments" && (
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold text-[#7C766C] uppercase tracking-wider">
              System Assignments ({assignments?.length ?? 0})
            </span>
            <div className="flex items-center gap-2">
              <label className="text-xs text-[#7C766C]">Filter Status:</label>
              <select
                value={assignmentStatusFilter}
                onChange={(e) => setAssignmentStatusFilter(e.target.value)}
                className="rounded-lg border border-[#E6E2D6] bg-white px-3 py-1.5 text-xs font-medium text-[#1F1D1A] focus:outline-none"
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
            <div className="overflow-hidden rounded-2xl border border-[#E6E2D6] bg-[#FFFFFF] shadow-xs">
              <table className="w-full text-left text-sm text-[#45413C]">
                <thead className="bg-[#F8F6F0] text-xs uppercase font-semibold text-[#7C766C] border-b border-[#E6E2D6] tracking-wider">
                  <tr>
                    <th className="px-6 py-3.5 font-serif font-bold text-[#1F1D1A]">Title</th>
                    <th className="px-6 py-3.5">Class</th>
                    <th className="px-6 py-3.5">Subject</th>
                    <th className="px-6 py-3.5">Teacher</th>
                    <th className="px-6 py-3.5">Status</th>
                    <th className="px-6 py-3.5">Deadline</th>
                    <th className="px-6 py-3.5">Max Marks</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-[#F0EDE4]">
                  {assignments.map((a) => (
                    <tr key={a.id} className="hover:bg-[#FBF9F5]">
                      <td className="px-6 py-4 font-semibold text-[#1F1D1A]">
                        {a.title}
                      </td>
                      <td className="px-6 py-4 text-[#45413C]">{a.classRoomName}</td>
                      <td className="px-6 py-4 text-[#45413C]">{a.subjectName}</td>
                      <td className="px-6 py-4 text-[#45413C]">{a.createdByTeacherName}</td>
                      <td className="px-6 py-4">
                        <StatusBadge status={a.status} />
                      </td>
                      <td className="px-6 py-4 text-[#45413C] text-xs">
                        {formatDateTime(a.deadline)}
                      </td>
                      <td className="px-6 py-4 font-mono font-semibold text-[#1F1D1A] text-xs">
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
            <span className="text-xs font-semibold text-[#7C766C] uppercase tracking-wider">
              System Submissions ({submissions?.length ?? 0})
            </span>
            <div className="flex items-center gap-2">
              <label className="text-xs text-[#7C766C]">Filter Status:</label>
              <select
                value={submissionStatusFilter}
                onChange={(e) => setSubmissionStatusFilter(e.target.value)}
                className="rounded-lg border border-[#E6E2D6] bg-white px-3 py-1.5 text-xs font-medium text-[#1F1D1A] focus:outline-none"
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
            <div className="overflow-hidden rounded-2xl border border-[#E6E2D6] bg-[#FFFFFF] shadow-xs">
              <table className="w-full text-left text-sm text-[#45413C]">
                <thead className="bg-[#F8F6F0] text-xs uppercase font-semibold text-[#7C766C] border-b border-[#E6E2D6] tracking-wider">
                  <tr>
                    <th className="px-6 py-3.5 font-serif font-bold text-[#1F1D1A]">Assignment</th>
                    <th className="px-6 py-3.5">Class</th>
                    <th className="px-6 py-3.5">Student</th>
                    <th className="px-6 py-3.5">Status</th>
                    <th className="px-6 py-3.5">Submitted At</th>
                    <th className="px-6 py-3.5">Score</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-[#F0EDE4]">
                  {submissions.map((sub) => (
                    <tr key={sub.id} className="hover:bg-[#FBF9F5]">
                      <td className="px-6 py-4 font-semibold text-[#1F1D1A]">
                        {sub.assignmentTitle}
                      </td>
                      <td className="px-6 py-4 text-[#45413C]">{sub.classRoomName}</td>
                      <td className="px-6 py-4 text-[#45413C]">{sub.studentName}</td>
                      <td className="px-6 py-4">
                        <StatusBadge status={sub.status} />
                      </td>
                      <td className="px-6 py-4 text-[#45413C] text-xs">
                        {formatDateTime(sub.submittedAt)}
                      </td>
                      <td className="px-6 py-4 font-mono font-semibold text-[#1F1D1A] text-xs">
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
