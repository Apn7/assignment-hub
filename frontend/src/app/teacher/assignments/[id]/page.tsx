"use client";

import React, { useState, useEffect } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/api/client";
import type {
  AssignmentResponse,
  UpdateAssignmentRequest,
  TeacherAssignmentResponse,
  SubmissionListItem,
  SubmissionResponse,
  GradeSubmissionRequest,
} from "@/types/api";
import { StatusBadge } from "@/components/StatusBadge";
import { formatDateTime, toDatetimeLocal, toUtcIso } from "@/lib/date";
import { LoadingState, ErrorState, EmptyState } from "@/components/States";
import { ConfirmDialog } from "@/components/ConfirmDialog";
import { getApiErrorMessage } from "@/lib/api/errors";

const editSchema = z.object({
  title: z.string().min(1, "Title is required"),
  description: z.string().min(1, "Description is required"),
  pair: z.string().min(1, "Pair is required"),
  deadlineLocal: z.string().min(1, "Deadline is required"),
  maxMarks: z.number().min(1, "Max marks must be at least 1"),
});

type EditFormData = z.infer<typeof editSchema>;

export default function TeacherAssignmentDetailPage() {
  const params = useParams();
  const router = useRouter();
  const queryClient = useQueryClient();
  const assignmentId = params.id as string;

  const [serverError, setServerError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  // Modals state
  const [showPublishDialog, setShowPublishDialog] = useState(false);
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);
  const [selectedSubmissionId, setSelectedSubmissionId] = useState<string | null>(null);
  const [showReopenDialog, setShowReopenDialog] = useState(false);

  // Grade Form state
  const [gradeMarks, setGradeMarks] = useState<number | "">("");
  const [gradeFeedback, setGradeFeedback] = useState<string>("");
  const [gradeError, setGradeError] = useState<string | null>(null);

  // Queries
  const {
    data: assignment,
    isLoading,
    error,
    refetch,
  } = useQuery<AssignmentResponse>({
    queryKey: ["teacher", "assignment", assignmentId],
    queryFn: async () => {
      // Mine list endpoint carries teacher's assignments
      const res = await api.get<AssignmentResponse[]>("/api/assignments/mine");
      const found = res.data.find((a) => a.id === assignmentId);
      if (!found) throw new Error("Assignment not found or unauthorized.");
      return found;
    },
  });

  const { data: pairs } = useQuery<TeacherAssignmentResponse[]>({
    queryKey: ["teacher", "assignments", "pairs"],
    queryFn: async () => {
      const res = await api.get<TeacherAssignmentResponse[]>(
        "/api/teacher-assignments/mine"
      );
      return res.data;
    },
  });

  const { data: submissions, isLoading: submissionsLoading } = useQuery<
    SubmissionListItem[]
  >({
    queryKey: ["teacher", "submissions", assignmentId],
    queryFn: async () => {
      const res = await api.get<SubmissionListItem[]>(
        `/api/assignments/${assignmentId}/submissions`
      );
      return res.data;
    },
    enabled: assignment?.status === "Published",
  });

  const { data: selectedSubmission, isLoading: selectedSubmissionLoading } =
    useQuery<SubmissionResponse>({
      queryKey: ["teacher", "submission", selectedSubmissionId],
      queryFn: async () => {
        const res = await api.get<SubmissionResponse>(
          `/api/submissions/${selectedSubmissionId}`
        );
        return res.data;
      },
      enabled: !!selectedSubmissionId,
    });

  // Populate grade form when selected submission details load
  useEffect(() => {
    if (selectedSubmission) {
      setGradeMarks(selectedSubmission.marks ?? "");
      setGradeFeedback(selectedSubmission.feedback ?? "");
      setGradeError(null);
    }
  }, [selectedSubmission]);

  // Form setup
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<EditFormData>({
    resolver: zodResolver(editSchema),
  });

  useEffect(() => {
    if (assignment) {
      reset({
        title: assignment.title,
        description: assignment.description,
        pair: `${assignment.classRoomId}:${assignment.subjectId}`,
        deadlineLocal: toDatetimeLocal(assignment.deadline),
        maxMarks: assignment.maxMarks,
      });
    }
  }, [assignment, reset]);

  // Mutations
  const updateMutation = useMutation<
    AssignmentResponse,
    unknown,
    UpdateAssignmentRequest
  >({
    mutationFn: async (payload) => {
      const res = await api.put<AssignmentResponse>(
        `/api/assignments/${assignmentId}`,
        payload
      );
      return res.data;
    },
    onSuccess: () => {
      setSuccessMessage("Assignment updated successfully.");
      queryClient.invalidateQueries({ queryKey: ["teacher", "assignment", assignmentId] });
      queryClient.invalidateQueries({ queryKey: ["teacher", "assignments", "mine"] });
    },
    onError: (err) => {
      setServerError(getApiErrorMessage(err, "Failed to update assignment."));
    },
  });

  const publishMutation = useMutation({
    mutationFn: async () => {
      const res = await api.post(`/api/assignments/${assignmentId}/publish`);
      return res.data;
    },
    onSuccess: () => {
      setShowPublishDialog(false);
      setSuccessMessage("Assignment published successfully! Students in this class can now view it.");
      queryClient.invalidateQueries({ queryKey: ["teacher", "assignment", assignmentId] });
      queryClient.invalidateQueries({ queryKey: ["teacher", "assignments", "mine"] });
    },
    onError: (err) => {
      setShowPublishDialog(false);
      setServerError(getApiErrorMessage(err, "Failed to publish assignment."));
    },
  });

  const deleteMutation = useMutation({
    mutationFn: async () => {
      await api.delete(`/api/assignments/${assignmentId}`);
    },
    onSuccess: () => {
      setShowDeleteDialog(false);
      router.push("/teacher");
    },
    onError: (err) => {
      setShowDeleteDialog(false);
      setServerError(getApiErrorMessage(err, "Failed to delete assignment."));
    },
  });

  const gradeMutation = useMutation<
    SubmissionResponse,
    unknown,
    GradeSubmissionRequest
  >({
    mutationFn: async (payload) => {
      const res = await api.post<SubmissionResponse>(
        `/api/submissions/${selectedSubmissionId}/grade`,
        payload
      );
      return res.data;
    },
    onSuccess: () => {
      setGradeError(null);
      queryClient.invalidateQueries({ queryKey: ["teacher", "submissions", assignmentId] });
      queryClient.invalidateQueries({
        queryKey: ["teacher", "submission", selectedSubmissionId],
      });
    },
    onError: (err) => {
      setGradeError(getApiErrorMessage(err, "Failed to record grade."));
    },
  });

  const reopenMutation = useMutation({
    mutationFn: async () => {
      const res = await api.post(
        `/api/submissions/${selectedSubmissionId}/status`,
        { status: "Submitted" }
      );
      return res.data;
    },
    onSuccess: () => {
      setShowReopenDialog(false);
      queryClient.invalidateQueries({ queryKey: ["teacher", "submissions", assignmentId] });
      queryClient.invalidateQueries({
        queryKey: ["teacher", "submission", selectedSubmissionId],
      });
    },
    onError: (err) => {
      setShowReopenDialog(false);
      setGradeError(getApiErrorMessage(err, "Failed to reopen submission."));
    },
  });

  const onSubmitUpdate = (formData: EditFormData) => {
    setServerError(null);
    setSuccessMessage(null);
    const [classRoomId, subjectId] = formData.pair.split(":");
    const payload: UpdateAssignmentRequest = {
      title: formData.title,
      description: formData.description,
      classRoomId,
      subjectId,
      deadline: toUtcIso(formData.deadlineLocal),
      maxMarks: formData.maxMarks,
    };
    updateMutation.mutate(payload);
  };

  const handleGradeSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (gradeMarks === "" || isNaN(Number(gradeMarks))) {
      setGradeError("Please enter valid numerical marks.");
      return;
    }
    setGradeError(null);
    gradeMutation.mutate({
      marks: Number(gradeMarks),
      feedback: gradeFeedback,
    });
  };

  if (isLoading) return <LoadingState message="Loading assignment details…" />;
  if (error || !assignment)
    return (
      <ErrorState
        message={getApiErrorMessage(error, "Assignment not found or inaccessible.")}
        onRetry={() => refetch()}
      />
    );

  const isPublished = assignment.status === "Published";

  return (
    <div className="max-w-5xl mx-auto space-y-8">
      {/* Header */}
      <div className="flex flex-wrap items-center justify-between gap-4 border-b border-gray-200 pb-4">
        <div>
          <div className="flex items-center gap-3">
            <h2 className="text-2xl font-bold text-gray-900">
              {assignment.title}
            </h2>
            <StatusBadge status={assignment.status} />
          </div>
          <p className="mt-1 text-sm text-gray-500">
            {assignment.classRoomName} — {assignment.subjectName}
          </p>
        </div>
        <Link
          href="/teacher"
          className="text-sm font-semibold text-gray-600 hover:text-gray-900"
        >
          ← Back to Dashboard
        </Link>
      </div>

      {serverError && (
        <div className="rounded-xl bg-red-50 p-4 text-sm text-red-700 font-medium border border-red-200">
          {serverError}
        </div>
      )}
      {successMessage && (
        <div className="rounded-xl bg-emerald-50 p-4 text-sm text-emerald-700 font-medium border border-emerald-200">
          {successMessage}
        </div>
      )}

      {/* Assignment Edit Form */}
      <div className="rounded-xl border border-gray-200 bg-white p-6 shadow-sm space-y-6">
        <div className="flex items-center justify-between border-b border-gray-100 pb-3">
          <h3 className="text-lg font-semibold text-gray-900">
            Assignment Details
          </h3>
          {!isPublished && (
            <div className="flex items-center gap-3">
              <button
                type="button"
                onClick={() => setShowDeleteDialog(true)}
                className="rounded-lg border border-red-200 px-3 py-1.5 text-xs font-semibold text-red-600 hover:bg-red-50"
              >
                Delete Draft
              </button>
              <button
                type="button"
                onClick={() => setShowPublishDialog(true)}
                className="rounded-lg bg-emerald-600 px-4 py-1.5 text-xs font-semibold text-white hover:bg-emerald-700"
              >
                Publish Assignment
              </button>
            </div>
          )}
        </div>

        <form onSubmit={handleSubmit(onSubmitUpdate)} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Title
            </label>
            <input
              {...register("title")}
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
            />
            {errors.title && (
              <p className="mt-1 text-xs text-red-600">{errors.title.message}</p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Class & Subject
            </label>
            <select
              {...register("pair")}
              disabled={isPublished}
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm bg-white focus:border-blue-500 focus:outline-none disabled:bg-gray-100 disabled:text-gray-500"
            >
              {pairs?.map((p) => (
                <option
                  key={`${p.classRoomId}:${p.subjectId}`}
                  value={`${p.classRoomId}:${p.subjectId}`}
                >
                  {p.classRoomName} — {p.subjectName}
                </option>
              ))}
            </select>
            {isPublished && (
              <p className="mt-1 text-xs text-amber-700">
                Hint: Class and subject are frozen once an assignment is published.
              </p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Description
            </label>
            <textarea
              {...register("description")}
              rows={4}
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
            />
            {errors.description && (
              <p className="mt-1 text-xs text-red-600">
                {errors.description.message}
              </p>
            )}
          </div>

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Deadline
              </label>
              <input
                type="datetime-local"
                {...register("deadlineLocal")}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
              />
              {isPublished && (
                <p className="mt-1 text-xs text-amber-700">
                  Hint: The deadline of a published assignment can only move later.
                </p>
              )}
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Max Marks
              </label>
              <input
                type="number"
                {...register("maxMarks", { valueAsNumber: true })}
                disabled={isPublished}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none disabled:bg-gray-100 disabled:text-gray-500 font-mono"
              />
              {isPublished && (
                <p className="mt-1 text-xs text-amber-700">
                  Hint: Maximum marks are frozen once published.
                </p>
              )}
            </div>
          </div>

          <div className="flex justify-end pt-3">
            <button
              type="submit"
              disabled={isSubmitting || updateMutation.isPending}
              className="rounded-lg bg-blue-600 px-5 py-2 text-sm font-semibold text-white hover:bg-blue-700 disabled:opacity-50"
            >
              {isSubmitting || updateMutation.isPending
                ? "Saving..."
                : "Save Changes"}
            </button>
          </div>
        </form>
      </div>

      {/* Submissions Section (Published Only) */}
      {isPublished && (
        <div className="space-y-6 pt-4 border-t border-gray-200">
          <div className="flex items-center justify-between">
            <h3 className="text-xl font-bold text-gray-900">
              Student Submissions
            </h3>
            <span className="text-xs text-gray-500">
              Select a submission to grade or review.
            </span>
          </div>

          {submissionsLoading ? (
            <LoadingState message="Loading submissions…" />
          ) : !submissions || submissions.length === 0 ? (
            <EmptyState
              title="No submissions yet"
              description="Students in this class have not handed in any answers so far."
            />
          ) : (
            <div className="grid grid-cols-1 gap-6 lg:grid-cols-12">
              {/* Submissions List */}
              <div className="lg:col-span-6 overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm">
                <ul className="divide-y divide-gray-200">
                  {submissions.map((sub) => {
                    const isSelected = selectedSubmissionId === sub.id;
                    return (
                      <li
                        key={sub.id}
                        onClick={() => setSelectedSubmissionId(sub.id)}
                        className={`p-4 cursor-pointer transition-colors ${
                          isSelected
                            ? "bg-blue-50/80 border-l-4 border-blue-600"
                            : "hover:bg-gray-50"
                        }`}
                      >
                        <div className="flex items-center justify-between">
                          <p className="font-semibold text-sm text-gray-900">
                            {sub.studentName}
                          </p>
                          <StatusBadge status={sub.status} />
                        </div>
                        <div className="mt-2 flex items-center justify-between text-xs text-gray-500">
                          <span>Submitted: {formatDateTime(sub.submittedAt)}</span>
                          <span className="font-medium text-gray-700">
                            {sub.marks !== null ? `${sub.marks} / ${sub.maxMarks}` : "Ungraded"}
                          </span>
                        </div>
                      </li>
                    );
                  })}
                </ul>
              </div>

              {/* Detail & Marking Drawer / Panel */}
              <div className="lg:col-span-6 rounded-xl border border-gray-200 bg-white p-6 shadow-sm space-y-5">
                {!selectedSubmissionId ? (
                  <div className="p-8 text-center text-sm text-gray-500">
                    Select a student submission from the list to view their answer and grade.
                  </div>
                ) : selectedSubmissionLoading ? (
                  <LoadingState message="Loading submission details…" />
                ) : selectedSubmission ? (
                  <div className="space-y-5">
                    <div className="flex items-center justify-between border-b border-gray-100 pb-3">
                      <div>
                        <h4 className="font-semibold text-gray-900">
                          {selectedSubmission.studentName}
                        </h4>
                        <p className="text-xs text-gray-500">
                          Submitted: {formatDateTime(selectedSubmission.submittedAt)}
                        </p>
                      </div>
                      <StatusBadge status={selectedSubmission.status} />
                    </div>

                    <div>
                      <label className="block text-xs font-semibold text-gray-700 uppercase tracking-wider mb-1">
                        Answer Text
                      </label>
                      <div className="rounded-lg bg-gray-50 p-3 text-sm text-gray-800 border border-gray-200 whitespace-pre-wrap max-h-60 overflow-y-auto font-mono">
                        {selectedSubmission.answerText}
                      </div>
                    </div>

                    {/* Grade Form */}
                    <form onSubmit={handleGradeSubmit} className="space-y-4 pt-2 border-t border-gray-100">
                      <h5 className="text-sm font-semibold text-gray-900">
                        {selectedSubmission.status === "Reviewed"
                          ? "Current Grade & Feedback"
                          : "Grade Submission"}
                      </h5>

                      {gradeError && (
                        <div className="rounded-lg bg-red-50 p-3 text-xs text-red-700 font-medium">
                          {gradeError}
                        </div>
                      )}

                      <div>
                        <label className="block text-xs font-medium text-gray-700 mb-1">
                          Marks (Max: {selectedSubmission.maxMarks})
                        </label>
                        <input
                          type="number"
                          value={gradeMarks}
                          onChange={(e) =>
                            setGradeMarks(e.target.value === "" ? "" : Number(e.target.value))
                          }
                          className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm font-mono focus:border-blue-500 focus:outline-none"
                          placeholder={`0 to ${selectedSubmission.maxMarks}`}
                        />
                      </div>

                      <div>
                        <label className="block text-xs font-medium text-gray-700 mb-1">
                          Feedback (Optional)
                        </label>
                        <textarea
                          rows={3}
                          value={gradeFeedback}
                          onChange={(e) => setGradeFeedback(e.target.value)}
                          className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
                          placeholder="Provide constructive feedback for the student..."
                        />
                      </div>

                      <div className="flex flex-wrap items-center justify-between gap-2 pt-2">
                        {selectedSubmission.status === "Reviewed" && (
                          <button
                            type="button"
                            onClick={() => setShowReopenDialog(true)}
                            className="rounded-lg border border-purple-300 bg-purple-50 px-3 py-1.5 text-xs font-semibold text-purple-700 hover:bg-purple-100"
                          >
                            Reopen for Revision
                          </button>
                        )}
                        <button
                          type="submit"
                          disabled={gradeMutation.isPending}
                          className="ml-auto rounded-lg bg-blue-600 px-4 py-1.5 text-xs font-semibold text-white hover:bg-blue-700 disabled:opacity-50"
                        >
                          {gradeMutation.isPending
                            ? "Saving Grade..."
                            : selectedSubmission.status === "Reviewed"
                            ? "Update Grade"
                            : "Record Grade"}
                        </button>
                      </div>
                    </form>
                  </div>
                ) : null}
              </div>
            </div>
          )}
        </div>
      )}

      {/* Confirmation Dialogs */}
      <ConfirmDialog
        isOpen={showPublishDialog}
        title="Publish Assignment"
        message="Are you sure you want to publish this assignment? Students in this class will see it immediately."
        confirmLabel="Publish Now"
        isPending={publishMutation.isPending}
        onConfirm={() => publishMutation.mutate()}
        onCancel={() => setShowPublishDialog(false)}
      />

      <ConfirmDialog
        isOpen={showDeleteDialog}
        title="Delete Draft Assignment"
        message="Are you sure you want to delete this draft assignment? This action cannot be undone."
        confirmLabel="Delete Draft"
        isPending={deleteMutation.isPending}
        onConfirm={() => deleteMutation.mutate()}
        onCancel={() => setShowDeleteDialog(false)}
      />

      <ConfirmDialog
        isOpen={showReopenDialog}
        title="Reopen Submission for Revision"
        message="Reopening moves this submission back to 'Submitted' state so the student can revise their answer. Recorded marks and feedback will be preserved."
        confirmLabel="Reopen Submission"
        isPending={reopenMutation.isPending}
        onConfirm={() => reopenMutation.mutate()}
        onCancel={() => setShowReopenDialog(false)}
      />
    </div>
  );
}
