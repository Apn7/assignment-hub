"use client";

import React, { useState, useEffect } from "react";
import { useParams } from "next/navigation";
import Link from "next/link";
import axios from "axios";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/api/client";
import type {
  AssignmentResponse,
  SubmissionResponse,
  SubmitAnswerRequest,
  UpdateSubmissionRequest,
} from "@/types/api";
import { StatusBadge } from "@/components/StatusBadge";
import { formatDateTime, isPastDeadline } from "@/lib/date";
import { LoadingState, ErrorState } from "@/components/States";
import { getApiErrorMessage } from "@/lib/api/errors";

export default function StudentAssignmentDetailPage() {
  const params = useParams();
  const queryClient = useQueryClient();
  const assignmentId = params.id as string;

  const [answerText, setAnswerText] = useState("");
  const [inlineError, setInlineError] = useState<string | null>(null);
  const [inlineSuccess, setInlineSuccess] = useState<string | null>(null);

  // Fetch assignment detail
  const {
    data: assignment,
    isLoading: assignmentLoading,
    error: assignmentError,
    refetch: refetchAssignment,
  } = useQuery<AssignmentResponse>({
    queryKey: ["student", "assignment", assignmentId],
    queryFn: async () => {
      const res = await api.get<AssignmentResponse>(
        `/api/assignments/${assignmentId}`
      );
      return res.data;
    },
  });

  // Fetch student's submission (404 = not submitted yet)
  const {
    data: submission,
    isLoading: submissionLoading,
    error: submissionError,
    refetch: refetchSubmission,
  } = useQuery<SubmissionResponse | null>({
    queryKey: ["student", "submission", assignmentId],
    queryFn: async () => {
      try {
        const res = await api.get<SubmissionResponse>(
          `/api/assignments/${assignmentId}/submissions/mine`
        );
        return res.data;
      } catch (err: unknown) {
        if (axios.isAxiosError(err) && err.response?.status === 404) {
          return null; // Not submitted yet
        }
        throw err;
      }
    },
  });

  // Pre-fill answer input when existing submission loads
  useEffect(() => {
    if (submission) {
      setAnswerText(submission.answerText);
    }
  }, [submission]);

  // Mutations
  const submitMutation = useMutation<
    SubmissionResponse,
    unknown,
    SubmitAnswerRequest
  >({
    mutationFn: async (payload) => {
      const res = await api.post<SubmissionResponse>(
        `/api/assignments/${assignmentId}/submissions`,
        payload
      );
      return res.data;
    },
    onSuccess: () => {
      setInlineError(null);
      setInlineSuccess("Answer submitted successfully!");
      queryClient.invalidateQueries({ queryKey: ["student", "submission", assignmentId] });
    },
    onError: (err) => {
      setInlineSuccess(null);
      setInlineError(getApiErrorMessage(err, "Failed to submit answer."));
    },
  });

  const updateMutation = useMutation<
    SubmissionResponse,
    unknown,
    UpdateSubmissionRequest
  >({
    mutationFn: async (payload) => {
      const res = await api.put<SubmissionResponse>(
        `/api/assignments/${assignmentId}/submissions/mine`,
        payload
      );
      return res.data;
    },
    onSuccess: () => {
      setInlineError(null);
      setInlineSuccess("Submission revised successfully!");
      queryClient.invalidateQueries({ queryKey: ["student", "submission", assignmentId] });
    },
    onError: (err) => {
      setInlineSuccess(null);
      setInlineError(getApiErrorMessage(err, "Failed to revise submission."));
    },
  });

  const handleSubmitOrUpdate = (e: React.FormEvent) => {
    e.preventDefault();
    if (!answerText.trim()) {
      setInlineError("Please enter your answer text before submitting.");
      return;
    }

    setInlineError(null);
    setInlineSuccess(null);

    if (!submission) {
      submitMutation.mutate({ answerText });
    } else {
      updateMutation.mutate({ answerText });
    }
  };

  if (assignmentLoading || submissionLoading)
    return <LoadingState message="Loading assignment details…" />;

  if (assignmentError || !assignment)
    return (
      <ErrorState
        message={getApiErrorMessage(
          assignmentError,
          "Assignment not found or not visible to your class."
        )}
        onRetry={() => {
          refetchAssignment();
          refetchSubmission();
        }}
      />
    );

  const isClosed = isPastDeadline(assignment.deadline);
  const isReviewed = submission?.status === "Reviewed";
  const isSubmitted = submission?.status === "Submitted";
  const hasPreviousMarks = submission?.marks !== null && submission?.marks !== undefined;

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      {/* Top Navigation */}
      <div className="flex items-center justify-between">
        <Link
          href="/student"
          className="text-sm font-semibold text-gray-600 hover:text-gray-900"
        >
          ← Back to Assignments
        </Link>
        <span className="text-xs text-gray-500 font-mono">
          Max Marks: {assignment.maxMarks}
        </span>
      </div>

      {/* Assignment Overview Header */}
      <div className="rounded-xl border border-gray-200 bg-white p-6 shadow-sm space-y-4">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <span className="inline-flex items-center rounded-md bg-blue-50 px-2.5 py-1 text-xs font-semibold text-blue-700 mb-2">
              {assignment.subjectName} — {assignment.classRoomName}
            </span>
            <h2 className="text-2xl font-bold text-gray-900">
              {assignment.title}
            </h2>
          </div>
          {isClosed ? (
            <span className="inline-flex items-center rounded-full bg-red-100 px-3 py-1 text-xs font-semibold text-red-800">
              Deadline Passed
            </span>
          ) : (
            <span className="inline-flex items-center rounded-full bg-emerald-100 px-3 py-1 text-xs font-semibold text-emerald-800">
              Open for Submission
            </span>
          )}
        </div>

        <div className="prose prose-sm text-gray-700 bg-gray-50 p-4 rounded-lg border border-gray-100 whitespace-pre-wrap font-sans">
          {assignment.description}
        </div>

        <div className="flex flex-wrap items-center justify-between text-xs text-gray-500 pt-2 border-t border-gray-100">
          <span>Teacher: {assignment.createdByTeacherName}</span>
          <span>Due: <strong className="text-gray-700">{formatDateTime(assignment.deadline)}</strong></span>
        </div>
      </div>

      {/* Inline Response Alerts */}
      {inlineError && (
        <div className="rounded-xl bg-red-50 p-4 text-sm text-red-700 font-medium border border-red-200">
          {inlineError}
        </div>
      )}
      {inlineSuccess && (
        <div className="rounded-xl bg-emerald-50 p-4 text-sm text-emerald-700 font-medium border border-emerald-200">
          {inlineSuccess}
        </div>
      )}

      {/* RENDER BY STATE */}

      {/* STATE 3: Reviewed */}
      {isReviewed && submission && (
        <div className="rounded-xl border border-purple-200 bg-purple-50/40 p-6 shadow-sm space-y-5">
          <div className="flex items-center justify-between border-b border-purple-100 pb-3">
            <div className="flex items-center gap-3">
              <h3 className="text-lg font-semibold text-purple-950">
                Grade & Teacher Feedback
              </h3>
              <StatusBadge status={submission.status} />
            </div>
            <div className="text-right">
              <span className="text-2xl font-extrabold text-purple-900 font-mono">
                {submission.marks} / {submission.maxMarks}
              </span>
              <span className="text-xs text-purple-600 block">Marks Awarded</span>
            </div>
          </div>

          {submission.feedback && (
            <div>
              <label className="block text-xs font-semibold text-purple-900 uppercase tracking-wider mb-1">
                Teacher Feedback
              </label>
              <p className="text-sm text-purple-900 bg-white p-3 rounded-lg border border-purple-200 italic">
                "{submission.feedback}"
              </p>
            </div>
          )}

          <div className="text-xs text-purple-700 flex justify-between pt-1">
            <span>Reviewed at: {formatDateTime(submission.reviewedAt)}</span>
            <span>Reviewed work is read-only unless reopened by teacher.</span>
          </div>

          <div>
            <label className="block text-xs font-semibold text-gray-700 uppercase tracking-wider mb-1">
              Your Submitted Answer
            </label>
            <div className="rounded-lg bg-white p-4 text-sm text-gray-800 border border-gray-200 whitespace-pre-wrap font-mono">
              {submission.answerText}
            </div>
          </div>
        </div>
      )}

      {/* STATE 2: Submitted & Deadline Future (Open for revision) */}
      {!isReviewed && isSubmitted && !isClosed && (
        <div className="rounded-xl border border-gray-200 bg-white p-6 shadow-sm space-y-5">
          {hasPreviousMarks && (
            <div className="rounded-lg bg-amber-50 p-4 border border-amber-200 text-amber-800 text-sm font-medium">
              Previously marked {submission.marks} / {submission.maxMarks} — awaiting re-marking.
            </div>
          )}

          <div className="flex items-center justify-between border-b border-gray-100 pb-3">
            <h3 className="text-lg font-semibold text-gray-900">
              Revise Your Submission
            </h3>
            <StatusBadge status={submission.status} />
          </div>

          <form onSubmit={handleSubmitOrUpdate} className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Answer Text
              </label>
              <textarea
                rows={6}
                value={answerText}
                onChange={(e) => setAnswerText(e.target.value)}
                className="w-full rounded-lg border border-gray-300 p-3 text-sm font-mono focus:border-blue-500 focus:outline-none"
                placeholder="Write your revised answer here..."
              />
            </div>

            <div className="flex items-center justify-between pt-2">
              <span className="text-xs text-gray-500">
                Last updated: {formatDateTime(submission.updatedAt)}
              </span>
              <button
                type="submit"
                disabled={updateMutation.isPending}
                className="rounded-lg bg-blue-600 px-5 py-2 text-sm font-semibold text-white hover:bg-blue-700 disabled:opacity-50"
              >
                {updateMutation.isPending ? "Revising..." : "Update Submission"}
              </button>
            </div>
          </form>
        </div>
      )}

      {/* STATE 1: No Submission & Deadline Future (Open to submit) */}
      {!isReviewed && !isSubmitted && !isClosed && (
        <div className="rounded-xl border border-gray-200 bg-white p-6 shadow-sm space-y-5">
          <h3 className="text-lg font-semibold text-gray-900 border-b border-gray-100 pb-3">
            Submit Your Answer
          </h3>

          <form onSubmit={handleSubmitOrUpdate} className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Answer Text
              </label>
              <textarea
                rows={6}
                value={answerText}
                onChange={(e) => setAnswerText(e.target.value)}
                className="w-full rounded-lg border border-gray-300 p-3 text-sm font-mono focus:border-blue-500 focus:outline-none"
                placeholder="Type your complete answer here..."
              />
            </div>

            <div className="flex justify-end pt-2">
              <button
                type="submit"
                disabled={submitMutation.isPending}
                className="rounded-lg bg-blue-600 px-6 py-2.5 text-sm font-semibold text-white hover:bg-blue-700 disabled:opacity-50"
              >
                {submitMutation.isPending ? "Submitting..." : "Submit Answer"}
              </button>
            </div>
          </form>
        </div>
      )}

      {/* STATE 4: Deadline Passed & No Submission */}
      {!isReviewed && !isSubmitted && isClosed && (
        <div className="rounded-xl border border-red-200 bg-red-50/50 p-8 text-center space-y-3">
          <h3 className="text-lg font-bold text-red-900">
            Assignment Closed
          </h3>
          <p className="text-sm text-red-700 max-w-md mx-auto">
            The deadline for this assignment has passed and you did not submit an answer. No further submissions are accepted.
          </p>
        </div>
      )}
    </div>
  );
}
