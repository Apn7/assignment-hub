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
          return null;
        }
        throw err;
      }
    },
  });

  useEffect(() => {
    if (submission) {
      setAnswerText(submission.answerText);
    }
  }, [submission]);

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
      <div className="flex items-center justify-between">
        <Link
          href="/student"
          className="text-xs font-semibold text-[#8C7B6B] hover:text-[#1F1D1A]"
        >
          ← Back to Assignments
        </Link>
        <span className="text-xs text-[#7C766C] font-mono">
          Max Marks: {assignment.maxMarks}
        </span>
      </div>

      {/* Assignment Overview Header */}
      <div className="rounded-2xl border border-[#E6E2D6] bg-[#FFFFFF] p-7 shadow-xs space-y-5">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <span className="inline-flex items-center rounded-md bg-[#F0F4F8] px-2.5 py-1 text-xs font-semibold text-[#1D4A6E] mb-2">
              {assignment.subjectName} — {assignment.classRoomName}
            </span>
            <h2 className="text-3xl font-serif font-bold text-[#1F1D1A]">
              {assignment.title}
            </h2>
          </div>
          {isClosed ? (
            <span className="inline-flex items-center rounded-full bg-[#FDF4F4] border border-[#F2C2C2] px-3 py-1 text-xs font-semibold text-[#8C2A2A]">
              Deadline Passed
            </span>
          ) : (
            <span className="inline-flex items-center rounded-full bg-[#F0F7F4] border border-[#D4E8DF] px-3 py-1 text-xs font-semibold text-[#1E5641]">
              Open for Submission
            </span>
          )}
        </div>

        <div className="text-sm text-[#45413C] bg-[#FBF9F5] p-5 rounded-xl border border-[#E6E2D6] whitespace-pre-wrap font-sans leading-relaxed">
          {assignment.description}
        </div>

        <div className="flex flex-wrap items-center justify-between text-xs text-[#7C766C] pt-2 border-t border-[#F0EDE4]">
          <span>Teacher: {assignment.createdByTeacherName}</span>
          <span>Due: <strong className="text-[#1F1D1A]">{formatDateTime(assignment.deadline)}</strong></span>
        </div>
      </div>

      {/* Inline Alerts */}
      {inlineError && (
        <div className="rounded-xl bg-[#FDF4F4] border border-[#F2C2C2] p-4 text-xs text-[#8C2A2A] font-medium">
          {inlineError}
        </div>
      )}
      {inlineSuccess && (
        <div className="rounded-xl bg-[#F0F7F4] border border-[#D4E8DF] p-4 text-xs text-[#1E5641] font-medium">
          {inlineSuccess}
        </div>
      )}

      {/* STATE 3: Reviewed */}
      {isReviewed && submission && (
        <div className="rounded-2xl border border-[#E6D6EB] bg-[#F7F2F8]/60 p-7 shadow-xs space-y-5">
          <div className="flex items-center justify-between border-b border-[#E6D6EB] pb-3">
            <div className="flex items-center gap-3">
              <h3 className="text-xl font-serif font-bold text-[#5C2B66]">
                Grade & Teacher Feedback
              </h3>
              <StatusBadge status={submission.status} />
            </div>
            <div className="text-right">
              <span className="text-3xl font-extrabold text-[#5C2B66] font-mono">
                {submission.marks} / {submission.maxMarks}
              </span>
              <span className="text-xs text-[#5C2B66] block">Marks Awarded</span>
            </div>
          </div>

          {submission.feedback && (
            <div>
              <label className="block text-xs font-semibold text-[#5C2B66] uppercase tracking-wider mb-1">
                Teacher Feedback
              </label>
              <p className="text-sm text-[#5C2B66] bg-white p-4 rounded-xl border border-[#E6D6EB] italic leading-relaxed">
                "{submission.feedback}"
              </p>
            </div>
          )}

          <div className="text-xs text-[#5C2B66] flex justify-between pt-1">
            <span>Reviewed at: {formatDateTime(submission.reviewedAt)}</span>
            <span>Reviewed work is read-only unless reopened by teacher.</span>
          </div>

          <div>
            <label className="block text-xs font-semibold text-[#45413C] uppercase tracking-wider mb-1">
              Your Submitted Answer
            </label>
            <div className="rounded-xl bg-white p-4 text-xs text-[#1F1D1A] border border-[#E6E2D6] whitespace-pre-wrap font-mono leading-relaxed">
              {submission.answerText}
            </div>
          </div>
        </div>
      )}

      {/* STATE 2: Submitted & Deadline Future */}
      {!isReviewed && isSubmitted && !isClosed && (
        <div className="rounded-2xl border border-[#E6E2D6] bg-[#FFFFFF] p-7 shadow-xs space-y-5">
          {hasPreviousMarks && (
            <div className="rounded-xl bg-[#FFF8EB] p-4 border border-[#F2E3C6] text-[#855B14] text-xs font-medium">
              Previously marked {submission.marks} / {submission.maxMarks} — awaiting re-marking.
            </div>
          )}

          <div className="flex items-center justify-between border-b border-[#F0EDE4] pb-3">
            <h3 className="text-xl font-serif font-bold text-[#1F1D1A]">
              Revise Your Submission
            </h3>
            <StatusBadge status={submission.status} />
          </div>

          <form onSubmit={handleSubmitOrUpdate} className="space-y-4">
            <div>
              <label className="block text-xs font-semibold text-[#45413C] uppercase tracking-wider mb-1">
                Answer Text
              </label>
              <textarea
                rows={6}
                value={answerText}
                onChange={(e) => setAnswerText(e.target.value)}
                className="w-full rounded-xl border border-[#E6E2D6] p-4 text-xs text-[#1F1D1A] font-mono leading-relaxed focus:border-[#8C7B6B] focus:outline-none"
                placeholder="Write your revised answer here..."
              />
            </div>

            <div className="flex items-center justify-between pt-2">
              <span className="text-xs text-[#7C766C]">
                Last updated: {formatDateTime(submission.updatedAt)}
              </span>
              <button
                type="submit"
                disabled={updateMutation.isPending}
                className="rounded-lg bg-[#2D2926] hover:bg-[#1F1D1A] px-5 py-2.5 text-xs font-semibold text-[#FBF9F5] disabled:opacity-50 shadow-xs"
              >
                {updateMutation.isPending ? "Revising..." : "Update Submission"}
              </button>
            </div>
          </form>
        </div>
      )}

      {/* STATE 1: No Submission & Deadline Future */}
      {!isReviewed && !isSubmitted && !isClosed && (
        <div className="rounded-2xl border border-[#E6E2D6] bg-[#FFFFFF] p-7 shadow-xs space-y-5">
          <h3 className="text-xl font-serif font-bold text-[#1F1D1A] border-b border-[#F0EDE4] pb-3">
            Submit Your Answer
          </h3>

          <form onSubmit={handleSubmitOrUpdate} className="space-y-4">
            <div>
              <label className="block text-xs font-semibold text-[#45413C] uppercase tracking-wider mb-1">
                Answer Text
              </label>
              <textarea
                rows={6}
                value={answerText}
                onChange={(e) => setAnswerText(e.target.value)}
                className="w-full rounded-xl border border-[#E6E2D6] p-4 text-xs text-[#1F1D1A] font-mono leading-relaxed focus:border-[#8C7B6B] focus:outline-none"
                placeholder="Type your complete answer here..."
              />
            </div>

            <div className="flex justify-end pt-2">
              <button
                type="submit"
                disabled={submitMutation.isPending}
                className="rounded-lg bg-[#2D2926] hover:bg-[#1F1D1A] px-6 py-2.5 text-xs font-semibold text-[#FBF9F5] disabled:opacity-50 shadow-xs"
              >
                {submitMutation.isPending ? "Submitting..." : "Submit Answer"}
              </button>
            </div>
          </form>
        </div>
      )}

      {/* STATE 4: Deadline Passed & No Submission */}
      {!isReviewed && !isSubmitted && isClosed && (
        <div className="rounded-2xl border border-[#F2C2C2] bg-[#FDF4F4] p-8 text-center space-y-2">
          <h3 className="text-xl font-serif font-bold text-[#8C2A2A]">
            Assignment Closed
          </h3>
          <p className="text-xs text-[#8C2A2A] max-w-md mx-auto">
            The deadline for this assignment has passed and no answer was submitted. Submissions are no longer accepted.
          </p>
        </div>
      )}
    </div>
  );
}
