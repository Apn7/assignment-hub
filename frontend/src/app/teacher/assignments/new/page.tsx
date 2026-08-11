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
          <h2 className="text-2xl font-bold tracking-tight text-gray-900">
            Create Assignment Draft
          </h2>
          <p className="mt-1 text-sm text-gray-500">
            Assignments start as drafts and must be published before students can view them.
          </p>
        </div>
        <Link
          href="/teacher"
          className="text-sm font-semibold text-gray-600 hover:text-gray-900"
        >
          ← Back to Dashboard
        </Link>
      </div>

      <form
        onSubmit={handleSubmit(onSubmit)}
        className="rounded-xl border border-gray-200 bg-white p-6 shadow-sm space-y-5"
      >
        {serverError && (
          <div className="rounded-lg bg-red-50 p-4 text-sm text-red-700 font-medium">
            {serverError}
          </div>
        )}

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Title
          </label>
          <input
            {...register("title")}
            className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
            placeholder="e.g. Kinematics Worksheet 1"
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
            className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm bg-white focus:border-blue-500 focus:outline-none"
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
            <p className="mt-1 text-xs text-red-600">{errors.pair.message}</p>
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
            placeholder="Provide instructions or problem statements..."
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
            {errors.deadlineLocal && (
              <p className="mt-1 text-xs text-red-600">
                {errors.deadlineLocal.message}
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
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none font-mono"
            />
            {errors.maxMarks && (
              <p className="mt-1 text-xs text-red-600">
                {errors.maxMarks.message}
              </p>
            )}
          </div>
        </div>

        <div className="flex justify-end gap-3 pt-4 border-t border-gray-100">
          <Link
            href="/teacher"
            className="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
          >
            Cancel
          </Link>
          <button
            type="submit"
            disabled={isSubmitting || createMutation.isPending}
            className="rounded-lg bg-blue-600 px-5 py-2 text-sm font-semibold text-white hover:bg-blue-700 disabled:opacity-50"
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
