"use client";

import React, { useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useQuery, useMutation } from "@tanstack/react-query";
import { api } from "@/lib/api/client";
import type {
  TeacherAssignmentResponse,
  AssignmentResponse,
  CreateAssignmentRequest,
} from "@/types/api";
import { toUtcIso } from "@/lib/date";
import { LoadingState, ErrorState } from "@/components/States";
import { getApiErrorMessage } from "@/lib/api/errors";

const schema = z.object({
  title: z.string().min(1, "Title is required"),
  description: z.string().min(1, "Description is required"),
  pair: z.string().min(1, "Select a class and subject pair"),
  deadlineLocal: z.string().min(1, "Deadline is required"),
  maxMarks: z
    .number()
    .int("Max marks must be an integer")
    .min(1, "Max marks must be at least 1")
    .max(1000, "Max marks cannot exceed 1000"),
});

type FormData = z.infer<typeof schema>;

export default function NewAssignmentPage() {
  const router = useRouter();
  const [serverError, setServerError] = useState<string | null>(null);

  const { data: pairs, isLoading, error: pairsError } = useQuery<
    TeacherAssignmentResponse[]
  >({
    queryKey: ["teacher", "assignments", "pairs"],
    queryFn: async () => {
      const res = await api.get<TeacherAssignmentResponse[]>(
        "/api/teacher-assignments/mine"
      );
      return res.data;
    },
  });

  const createMutation = useMutation<
    AssignmentResponse,
    unknown,
    CreateAssignmentRequest
  >({
    mutationFn: async (payload) => {
      const res = await api.post<AssignmentResponse>(
        "/api/assignments",
        payload
      );
      return res.data;
    },
    onSuccess: (created) => {
      router.push(`/teacher/assignments/${created.id}`);
    },
    onError: (err) => {
      setServerError(getApiErrorMessage(err, "Failed to create assignment."));
    },
  });

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: {
      title: "",
      description: "",
      pair: "",
      deadlineLocal: "",
      maxMarks: 20,
    },
  });

  const onSubmit = (formData: FormData) => {
    setServerError(null);
    const [classRoomId, subjectId] = formData.pair.split(":");
    const payload: CreateAssignmentRequest = {
      title: formData.title,
      description: formData.description,
      classRoomId,
      subjectId,
      deadline: toUtcIso(formData.deadlineLocal),
      maxMarks: formData.maxMarks,
    };

    createMutation.mutate(payload);
  };

  if (isLoading) return <LoadingState message="Loading your teaching pairs…" />;
  if (pairsError)
    return (
      <ErrorState
        message={getApiErrorMessage(
          pairsError,
          "Failed to load teaching assignments."
        )}
      />
    );

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-3xl font-serif font-bold text-[#1F1D1A]">
            Create Assignment Draft
          </h2>
          <p className="mt-1 text-xs text-[#7C766C]">
            Assignments start as drafts. Publishing makes work visible to your class.
          </p>
        </div>
        <Link
          href="/teacher"
          className="text-xs font-semibold text-[#8C7B6B] hover:text-[#1F1D1A]"
        >
          ← Back to Dashboard
        </Link>
      </div>

      <form
        onSubmit={handleSubmit(onSubmit)}
        className="rounded-2xl border border-[#E6E2D6] bg-[#FFFFFF] p-7 shadow-xs space-y-5"
      >
        {serverError && (
          <div className="rounded-lg bg-[#FDF4F4] border border-[#F2C2C2] p-4 text-xs text-[#8C2A2A] font-medium">
            {serverError}
          </div>
        )}

        <div>
          <label className="block text-xs font-semibold text-[#45413C] uppercase tracking-wider mb-1.5">
            Assignment Title
          </label>
          <input
            {...register("title")}
            className="w-full rounded-lg border border-[#E6E2D6] px-3.5 py-2 text-sm text-[#1F1D1A] placeholder:text-[#A59F93]"
            placeholder="e.g. Kinematics Problem Set 1"
          />
          {errors.title && (
            <p className="mt-1 text-xs text-[#8C2A2A] font-medium">{errors.title.message}</p>
          )}
        </div>

        <div>
          <label className="block text-xs font-semibold text-[#45413C] uppercase tracking-wider mb-1.5">
            Class & Subject Entitlement
          </label>
          <select
            {...register("pair")}
            className="w-full rounded-lg border border-[#E6E2D6] px-3.5 py-2 text-sm text-[#1F1D1A] bg-white"
          >
            <option value="">-- Select Class & Subject --</option>
            {pairs?.map((p) => (
              <option
                key={`${p.classRoomId}:${p.subjectId}`}
                value={`${p.classRoomId}:${p.subjectId}`}
              >
                {p.classRoomName} — {p.subjectName}
              </option>
            ))}
          </select>
          {errors.pair && (
            <p className="mt-1 text-xs text-[#8C2A2A] font-medium">{errors.pair.message}</p>
          )}
        </div>

        <div>
          <label className="block text-xs font-semibold text-[#45413C] uppercase tracking-wider mb-1.5">
            Description / Problems
          </label>
          <textarea
            {...register("description")}
            rows={4}
            className="w-full rounded-lg border border-[#E6E2D6] p-3.5 text-sm text-[#1F1D1A] placeholder:text-[#A59F93]"
            placeholder="Provide task details or questions..."
          />
          {errors.description && (
            <p className="mt-1 text-xs text-[#8C2A2A] font-medium">
              {errors.description.message}
            </p>
          )}
        </div>

        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div>
            <label className="block text-xs font-semibold text-[#45413C] uppercase tracking-wider mb-1.5">
              Submission Deadline
            </label>
            <input
              type="datetime-local"
              {...register("deadlineLocal")}
              className="w-full rounded-lg border border-[#E6E2D6] px-3.5 py-2 text-sm text-[#1F1D1A]"
            />
            {errors.deadlineLocal && (
              <p className="mt-1 text-xs text-[#8C2A2A] font-medium">
                {errors.deadlineLocal.message}
              </p>
            )}
          </div>

          <div>
            <label className="block text-xs font-semibold text-[#45413C] uppercase tracking-wider mb-1.5">
              Maximum Marks
            </label>
            <input
              type="number"
              {...register("maxMarks", { valueAsNumber: true })}
              className="w-full rounded-lg border border-[#E6E2D6] px-3.5 py-2 text-sm text-[#1F1D1A] font-mono"
            />
            {errors.maxMarks && (
              <p className="mt-1 text-xs text-[#8C2A2A] font-medium">
                {errors.maxMarks.message}
              </p>
            )}
          </div>
        </div>

        <div className="flex justify-end gap-3 pt-4 border-t border-[#F0EDE4]">
          <Link
            href="/teacher"
            className="rounded-lg border border-[#E6E2D6] bg-[#FBF9F5] px-4 py-2 text-xs font-semibold text-[#45413C] hover:bg-[#F3EFE6]"
          >
            Cancel
          </Link>
          <button
            type="submit"
            disabled={isSubmitting || createMutation.isPending}
            className="rounded-lg bg-[#2D2926] hover:bg-[#1F1D1A] px-5 py-2 text-xs font-semibold text-[#FBF9F5] disabled:opacity-50 shadow-xs"
          >
            {isSubmitting || createMutation.isPending
              ? "Creating..."
              : "Save Draft"}
          </button>
        </div>
      </form>
    </div>
  );
}
