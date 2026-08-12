"use client";

import React, { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm, useWatch } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { api } from "@/lib/api/client";
import type {
  AssignmentResponse,
  SubmissionListItem,
  AdminUserResponse,
  AdminClassRoomResponse,
  AdminSubjectResponse,
  AdminTeacherAssignmentResponse,
} from "@/types/api";
import { StatusBadge } from "@/components/StatusBadge";
import { formatDateTime } from "@/lib/date";
import { LoadingState, ErrorState, EmptyState } from "@/components/States";
import { getApiErrorMessage } from "@/lib/api/errors";

// ─── Tab type ─────────────────────────────────────────────────────────────────

type Tab =
  | "assignments"
  | "submissions"
  | "users"
  | "classes"
  | "subjects"
  | "teacher-assignments";

const TABS: { key: Tab; label: string }[] = [
  { key: "assignments", label: "Assignments" },
  { key: "submissions", label: "Submissions" },
  { key: "users", label: "Users" },
  { key: "classes", label: "Classes" },
  { key: "subjects", label: "Subjects" },
  { key: "teacher-assignments", label: "Teacher Assignments" },
];

// ─── Shared Paper Table ───────────────────────────────────────────────────────

function PaperTable({
  headers,
  children,
}: {
  headers: string[];
  children: React.ReactNode;
}) {
  return (
    <div className="overflow-hidden rounded-2xl border border-[#E6E2D6] bg-[#FFFFFF] shadow-xs">
      <table className="w-full text-left text-sm text-[#45413C]">
        <thead className="bg-[#F8F6F0] text-xs uppercase font-semibold text-[#7C766C] border-b border-[#E6E2D6] tracking-wider">
          <tr>
            {headers.map((h, i) => (
              <th
                key={h}
                className={`px-6 py-3.5 ${i === 0 ? "font-serif font-bold text-[#1F1D1A]" : ""}`}
              >
                {h}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-[#F0EDE4]">{children}</tbody>
      </table>
    </div>
  );
}

// ─── Shared Paper Modal ───────────────────────────────────────────────────────

function PaperModal({
  open,
  onClose,
  title,
  children,
}: {
  open: boolean;
  onClose: () => void;
  title: string;
  children: React.ReactNode;
}) {
  if (!open) return null;
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/20 backdrop-blur-sm">
      <div className="bg-white rounded-2xl border border-[#E6E2D6] shadow-lg w-full max-w-lg mx-4 p-6">
        <div className="flex items-center justify-between mb-5">
          <h3 className="text-lg font-serif font-bold text-[#1F1D1A]">{title}</h3>
          <button
            onClick={onClose}
            className="text-[#7C766C] hover:text-[#1F1D1A] transition-colors text-xl leading-none"
          >
            ×
          </button>
        </div>
        {children}
      </div>
    </div>
  );
}

// ─── Form field styling ───────────────────────────────────────────────────────

const inputClass =
  "w-full rounded-lg border border-[#E6E2D6] bg-white px-3 py-2 text-sm text-[#1F1D1A] placeholder-[#B0AB9F] focus:outline-none focus:ring-2 focus:ring-[#2D2926]/20 focus:border-[#2D2926] transition-all";
const labelClass = "block text-xs font-semibold text-[#45413C] mb-1";
const errorTextClass = "text-xs text-[#8C2A2A] mt-1";
const btnPrimaryClass =
  "rounded-lg bg-[#2D2926] text-white px-4 py-2 text-sm font-semibold hover:bg-[#1F1D1A] transition-colors disabled:opacity-50 disabled:cursor-not-allowed";
const btnSecondaryClass =
  "rounded-lg border border-[#E6E2D6] bg-white text-[#45413C] px-4 py-2 text-sm font-semibold hover:bg-[#F8F6F0] transition-colors";

// ─── Zod schemas ──────────────────────────────────────────────────────────────

const createUserSchema = z.object({
  fullName: z.string().min(1, "Full name is required").max(150),
  email: z.string().email("Must be a valid email").max(256),
  password: z.string().min(8, "Password must be at least 8 characters"),
  role: z.enum(["Admin", "Teacher", "Student"]),
  classRoomId: z.string().optional(),
});
type CreateUserForm = z.infer<typeof createUserSchema>;

const createNameSchema = z.object({
  name: z.string().min(1, "Name is required").max(100),
});
type CreateNameForm = z.infer<typeof createNameSchema>;

const createTeacherAssignmentSchema = z.object({
  teacherId: z.string().min(1, "Teacher is required"),
  classRoomId: z.string().min(1, "Class is required"),
  subjectId: z.string().min(1, "Subject is required"),
});
type CreateTeacherAssignmentForm = z.infer<typeof createTeacherAssignmentSchema>;

// ═════════════════════════════════════════════════════════════════════════════
// PAGE
// ═════════════════════════════════════════════════════════════════════════════

export default function AdminDashboardPage() {
  const queryClient = useQueryClient();
  const [activeTab, setActiveTab] = useState<Tab>("assignments");
  const [assignmentStatusFilter, setAssignmentStatusFilter] = useState("");
  const [submissionStatusFilter, setSubmissionStatusFilter] = useState("");
  const [userRoleFilter, setUserRoleFilter] = useState("");
  const [showUserModal, setShowUserModal] = useState(false);
  const [showClassModal, setShowClassModal] = useState(false);
  const [showSubjectModal, setShowSubjectModal] = useState(false);
  const [showTaModal, setShowTaModal] = useState(false);

  // ── Queries ───────────────────────────────────────────────────────────

  const assignments = useQuery<AssignmentResponse[]>({
    queryKey: ["admin", "assignments", assignmentStatusFilter],
    queryFn: async () => {
      const params = assignmentStatusFilter ? { status: assignmentStatusFilter } : {};
      return (await api.get<AssignmentResponse[]>("/api/admin/assignments", { params })).data;
    },
    enabled: activeTab === "assignments",
  });

  const submissions = useQuery<SubmissionListItem[]>({
    queryKey: ["admin", "submissions", submissionStatusFilter],
    queryFn: async () => {
      const params = submissionStatusFilter ? { status: submissionStatusFilter } : {};
      return (await api.get<SubmissionListItem[]>("/api/admin/submissions", { params })).data;
    },
    enabled: activeTab === "submissions",
  });

  const users = useQuery<AdminUserResponse[]>({
    queryKey: ["admin", "users", userRoleFilter],
    queryFn: async () => {
      const params = userRoleFilter ? { role: userRoleFilter } : {};
      return (await api.get<AdminUserResponse[]>("/api/admin/users", { params })).data;
    },
    enabled: activeTab === "users",
  });

  const classRooms = useQuery<AdminClassRoomResponse[]>({
    queryKey: ["admin", "classrooms"],
    queryFn: async () =>
      (await api.get<AdminClassRoomResponse[]>("/api/admin/classrooms")).data,
    enabled: activeTab === "classes" || activeTab === "users" || activeTab === "teacher-assignments",
  });

  const subjects = useQuery<AdminSubjectResponse[]>({
    queryKey: ["admin", "subjects"],
    queryFn: async () =>
      (await api.get<AdminSubjectResponse[]>("/api/admin/subjects")).data,
    enabled: activeTab === "subjects" || activeTab === "teacher-assignments",
  });

  const teacherAssignments = useQuery<AdminTeacherAssignmentResponse[]>({
    queryKey: ["admin", "teacher-assignments"],
    queryFn: async () =>
      (await api.get<AdminTeacherAssignmentResponse[]>("/api/admin/teacher-assignments")).data,
    enabled: activeTab === "teacher-assignments",
  });

  // Teachers list for the teacher-assignment form
  const teachers = useQuery<AdminUserResponse[]>({
    queryKey: ["admin", "users", "Teacher"],
    queryFn: async () =>
      (await api.get<AdminUserResponse[]>("/api/admin/users", { params: { role: "Teacher" } })).data,
    enabled: activeTab === "teacher-assignments",
  });

  // ── Mutations ──────────────────────────────────────────────────────────

  const [userFormError, setUserFormError] = useState("");
  const createUser = useMutation({
    mutationFn: (data: CreateUserForm) =>
      api.post("/api/admin/users", {
        ...data,
        classRoomId: data.classRoomId || null,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["admin", "users"] });
      setShowUserModal(false);
      setUserFormError("");
    },
    onError: (err) => setUserFormError(getApiErrorMessage(err)),
  });

  const [classFormError, setClassFormError] = useState("");
  const createClass = useMutation({
    mutationFn: (data: CreateNameForm) => api.post("/api/admin/classrooms", data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["admin", "classrooms"] });
      setShowClassModal(false);
      setClassFormError("");
    },
    onError: (err) => setClassFormError(getApiErrorMessage(err)),
  });

  const [subjectFormError, setSubjectFormError] = useState("");
  const createSubject = useMutation({
    mutationFn: (data: CreateNameForm) => api.post("/api/admin/subjects", data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["admin", "subjects"] });
      setShowSubjectModal(false);
      setSubjectFormError("");
    },
    onError: (err) => setSubjectFormError(getApiErrorMessage(err)),
  });

  const [taFormError, setTaFormError] = useState("");
  const createTa = useMutation({
    mutationFn: (data: CreateTeacherAssignmentForm) =>
      api.post("/api/admin/teacher-assignments", data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["admin", "teacher-assignments"] });
      setShowTaModal(false);
      setTaFormError("");
    },
    onError: (err) => setTaFormError(getApiErrorMessage(err)),
  });

  // ── Forms ──────────────────────────────────────────────────────────────

  const userForm = useForm<CreateUserForm>({
    resolver: zodResolver(createUserSchema),
    defaultValues: { role: "Student" },
  });
  const watchedRole = useWatch({ control: userForm.control, name: "role" });

  const classForm = useForm<CreateNameForm>({
    resolver: zodResolver(createNameSchema),
  });

  const subjectForm = useForm<CreateNameForm>({
    resolver: zodResolver(createNameSchema),
  });

  const taForm = useForm<CreateTeacherAssignmentForm>({
    resolver: zodResolver(createTeacherAssignmentSchema),
  });

  // ── Render ─────────────────────────────────────────────────────────────

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-3xl font-serif font-bold tracking-tight text-[#1F1D1A]">
          Admin Portal
        </h2>
        <p className="mt-1 text-xs text-[#7C766C]">
          Manage users, classes, subjects, teacher assignments, and audit system data.
        </p>
      </div>

      {/* Tabs */}
      <div className="flex border-b border-[#E6E2D6] overflow-x-auto">
        {TABS.map((tab) => (
          <button
            key={tab.key}
            type="button"
            onClick={() => setActiveTab(tab.key)}
            className={`whitespace-nowrap px-5 py-2.5 text-xs font-semibold border-b-2 transition-colors ${
              activeTab === tab.key
                ? "border-[#2D2926] text-[#1F1D1A]"
                : "border-transparent text-[#7C766C] hover:text-[#1F1D1A]"
            }`}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {/* ═══ TAB: ASSIGNMENTS ══════════════════════════════════════════════ */}
      {activeTab === "assignments" && (
        <TabSection
          title={`System Assignments (${assignments.data?.length ?? 0})`}
          filter={
            <StatusFilter
              value={assignmentStatusFilter}
              onChange={setAssignmentStatusFilter}
              options={["Draft", "Published"]}
            />
          }
          isLoading={assignments.isLoading}
          error={assignments.error}
          refetch={() => assignments.refetch()}
          empty={!assignments.data?.length}
          emptyTitle="No assignments found"
          emptyDesc="No assignments match the selected filter."
        >
          <PaperTable headers={["Title", "Class", "Subject", "Teacher", "Status", "Deadline", "Max Marks"]}>
            {assignments.data?.map((a) => (
              <tr key={a.id} className="hover:bg-[#FBF9F5]">
                <td className="px-6 py-4 font-semibold text-[#1F1D1A]">{a.title}</td>
                <td className="px-6 py-4">{a.classRoomName}</td>
                <td className="px-6 py-4">{a.subjectName}</td>
                <td className="px-6 py-4">{a.createdByTeacherName}</td>
                <td className="px-6 py-4"><StatusBadge status={a.status} /></td>
                <td className="px-6 py-4 text-xs">{formatDateTime(a.deadline)}</td>
                <td className="px-6 py-4 font-mono font-semibold text-[#1F1D1A] text-xs">{a.maxMarks}</td>
              </tr>
            ))}
          </PaperTable>
        </TabSection>
      )}

      {/* ═══ TAB: SUBMISSIONS ═════════════════════════════════════════════ */}
      {activeTab === "submissions" && (
        <TabSection
          title={`System Submissions (${submissions.data?.length ?? 0})`}
          filter={
            <StatusFilter
              value={submissionStatusFilter}
              onChange={setSubmissionStatusFilter}
              options={["Submitted", "Reviewed"]}
            />
          }
          isLoading={submissions.isLoading}
          error={submissions.error}
          refetch={() => submissions.refetch()}
          empty={!submissions.data?.length}
          emptyTitle="No submissions found"
          emptyDesc="No student submissions match the selected filter."
        >
          <PaperTable headers={["Assignment", "Class", "Student", "Status", "Submitted At", "Score"]}>
            {submissions.data?.map((sub) => (
              <tr key={sub.id} className="hover:bg-[#FBF9F5]">
                <td className="px-6 py-4 font-semibold text-[#1F1D1A]">{sub.assignmentTitle}</td>
                <td className="px-6 py-4">{sub.classRoomName}</td>
                <td className="px-6 py-4">{sub.studentName}</td>
                <td className="px-6 py-4"><StatusBadge status={sub.status} /></td>
                <td className="px-6 py-4 text-xs">{formatDateTime(sub.submittedAt)}</td>
                <td className="px-6 py-4 font-mono font-semibold text-[#1F1D1A] text-xs">
                  {sub.marks !== null ? `${sub.marks} / ${sub.maxMarks}` : "Ungraded"}
                </td>
              </tr>
            ))}
          </PaperTable>
        </TabSection>
      )}

      {/* ═══ TAB: USERS ═══════════════════════════════════════════════════ */}
      {activeTab === "users" && (
        <TabSection
          title={`Users (${users.data?.length ?? 0})`}
          filter={
            <div className="flex items-center gap-2">
              <StatusFilter
                value={userRoleFilter}
                onChange={setUserRoleFilter}
                options={["Admin", "Teacher", "Student"]}
                allLabel="All Roles"
                label="Filter Role:"
              />
              <button
                className={btnPrimaryClass}
                onClick={() => {
                  userForm.reset({ role: "Student" });
                  setUserFormError("");
                  setShowUserModal(true);
                }}
              >
                + New User
              </button>
            </div>
          }
          isLoading={users.isLoading}
          error={users.error}
          refetch={() => users.refetch()}
          empty={!users.data?.length}
          emptyTitle="No users found"
          emptyDesc="No users match the selected filter."
        >
          <PaperTable headers={["Name", "Email", "Role", "Class"]}>
            {users.data?.map((u) => (
              <tr key={u.id} className="hover:bg-[#FBF9F5]">
                <td className="px-6 py-4 font-semibold text-[#1F1D1A]">{u.fullName}</td>
                <td className="px-6 py-4">{u.email}</td>
                <td className="px-6 py-4">
                  <span className="inline-block rounded-full bg-[#F5F5F5] border border-[#E0E0E0] px-2.5 py-0.5 text-xs font-semibold text-[#424242]">
                    {u.role}
                  </span>
                </td>
                <td className="px-6 py-4 text-[#7C766C]">{u.classRoomName ?? "—"}</td>
              </tr>
            ))}
          </PaperTable>
        </TabSection>
      )}

      {/* ═══ TAB: CLASSES ═════════════════════════════════════════════════ */}
      {activeTab === "classes" && (
        <TabSection
          title={`Classes (${classRooms.data?.length ?? 0})`}
          filter={
            <button
              className={btnPrimaryClass}
              onClick={() => {
                classForm.reset();
                setClassFormError("");
                setShowClassModal(true);
              }}
            >
              + New Class
            </button>
          }
          isLoading={classRooms.isLoading}
          error={classRooms.error}
          refetch={() => classRooms.refetch()}
          empty={!classRooms.data?.length}
          emptyTitle="No classes found"
          emptyDesc="Create a class to get started."
        >
          <PaperTable headers={["Name", "ID"]}>
            {classRooms.data?.map((c) => (
              <tr key={c.id} className="hover:bg-[#FBF9F5]">
                <td className="px-6 py-4 font-semibold text-[#1F1D1A]">{c.name}</td>
                <td className="px-6 py-4 font-mono text-xs text-[#7C766C]">{c.id}</td>
              </tr>
            ))}
          </PaperTable>
        </TabSection>
      )}

      {/* ═══ TAB: SUBJECTS ════════════════════════════════════════════════ */}
      {activeTab === "subjects" && (
        <TabSection
          title={`Subjects (${subjects.data?.length ?? 0})`}
          filter={
            <button
              className={btnPrimaryClass}
              onClick={() => {
                subjectForm.reset();
                setSubjectFormError("");
                setShowSubjectModal(true);
              }}
            >
              + New Subject
            </button>
          }
          isLoading={subjects.isLoading}
          error={subjects.error}
          refetch={() => subjects.refetch()}
          empty={!subjects.data?.length}
          emptyTitle="No subjects found"
          emptyDesc="Create a subject to get started."
        >
          <PaperTable headers={["Name", "ID"]}>
            {subjects.data?.map((s) => (
              <tr key={s.id} className="hover:bg-[#FBF9F5]">
                <td className="px-6 py-4 font-semibold text-[#1F1D1A]">{s.name}</td>
                <td className="px-6 py-4 font-mono text-xs text-[#7C766C]">{s.id}</td>
              </tr>
            ))}
          </PaperTable>
        </TabSection>
      )}

      {/* ═══ TAB: TEACHER ASSIGNMENTS ═════════════════════════════════════ */}
      {activeTab === "teacher-assignments" && (
        <TabSection
          title={`Teacher Assignments (${teacherAssignments.data?.length ?? 0})`}
          filter={
            <button
              className={btnPrimaryClass}
              onClick={() => {
                taForm.reset();
                setTaFormError("");
                setShowTaModal(true);
              }}
            >
              + New Entitlement
            </button>
          }
          isLoading={teacherAssignments.isLoading}
          error={teacherAssignments.error}
          refetch={() => teacherAssignments.refetch()}
          empty={!teacherAssignments.data?.length}
          emptyTitle="No entitlements found"
          emptyDesc="Assign a teacher to a class/subject pair."
        >
          <PaperTable headers={["Teacher", "Class", "Subject"]}>
            {teacherAssignments.data?.map((ta) => (
              <tr key={ta.id} className="hover:bg-[#FBF9F5]">
                <td className="px-6 py-4 font-semibold text-[#1F1D1A]">{ta.teacherName}</td>
                <td className="px-6 py-4">{ta.classRoomName}</td>
                <td className="px-6 py-4">{ta.subjectName}</td>
              </tr>
            ))}
          </PaperTable>
        </TabSection>
      )}

      {/* ═══ MODALS ═══════════════════════════════════════════════════════ */}

      {/* Create User Modal */}
      <PaperModal open={showUserModal} onClose={() => setShowUserModal(false)} title="Create User">
        <form
          onSubmit={userForm.handleSubmit((data) => createUser.mutate(data))}
          className="space-y-4"
        >
          <div>
            <label className={labelClass}>Full Name</label>
            <input {...userForm.register("fullName")} className={inputClass} placeholder="Karim Uddin" />
            {userForm.formState.errors.fullName && (
              <p className={errorTextClass}>{userForm.formState.errors.fullName.message}</p>
            )}
          </div>
          <div>
            <label className={labelClass}>Email</label>
            <input {...userForm.register("email")} type="email" className={inputClass} placeholder="karim@school.local" />
            {userForm.formState.errors.email && (
              <p className={errorTextClass}>{userForm.formState.errors.email.message}</p>
            )}
          </div>
          <div>
            <label className={labelClass}>Password</label>
            <input {...userForm.register("password")} type="password" className={inputClass} placeholder="Min 8 characters" />
            {userForm.formState.errors.password && (
              <p className={errorTextClass}>{userForm.formState.errors.password.message}</p>
            )}
          </div>
          <div>
            <label className={labelClass}>Role</label>
            <select {...userForm.register("role")} className={inputClass}>
              <option value="Student">Student</option>
              <option value="Teacher">Teacher</option>
              <option value="Admin">Admin</option>
            </select>
          </div>
          {watchedRole === "Student" && (
            <div>
              <label className={labelClass}>Class</label>
              <select {...userForm.register("classRoomId")} className={inputClass}>
                <option value="">Select a class…</option>
                {classRooms.data?.map((c) => (
                  <option key={c.id} value={c.id}>{c.name}</option>
                ))}
              </select>
              {userForm.formState.errors.classRoomId && (
                <p className={errorTextClass}>{userForm.formState.errors.classRoomId.message}</p>
              )}
            </div>
          )}
          {userFormError && (
            <div className="rounded-lg bg-[#FDF2F2] border border-[#F5C6CB] p-3 text-xs text-[#8C2A2A]">
              {userFormError}
            </div>
          )}
          <div className="flex justify-end gap-2 pt-2">
            <button type="button" className={btnSecondaryClass} onClick={() => setShowUserModal(false)}>
              Cancel
            </button>
            <button type="submit" className={btnPrimaryClass} disabled={createUser.isPending}>
              {createUser.isPending ? "Creating…" : "Create User"}
            </button>
          </div>
        </form>
      </PaperModal>

      {/* Create Class Modal */}
      <PaperModal open={showClassModal} onClose={() => setShowClassModal(false)} title="Create Class">
        <form
          onSubmit={classForm.handleSubmit((data) => createClass.mutate(data))}
          className="space-y-4"
        >
          <div>
            <label className={labelClass}>Class Name</label>
            <input {...classForm.register("name")} className={inputClass} placeholder="Class 8 – B" />
            {classForm.formState.errors.name && (
              <p className={errorTextClass}>{classForm.formState.errors.name.message}</p>
            )}
          </div>
          {classFormError && (
            <div className="rounded-lg bg-[#FDF2F2] border border-[#F5C6CB] p-3 text-xs text-[#8C2A2A]">
              {classFormError}
            </div>
          )}
          <div className="flex justify-end gap-2 pt-2">
            <button type="button" className={btnSecondaryClass} onClick={() => setShowClassModal(false)}>
              Cancel
            </button>
            <button type="submit" className={btnPrimaryClass} disabled={createClass.isPending}>
              {createClass.isPending ? "Creating…" : "Create Class"}
            </button>
          </div>
        </form>
      </PaperModal>

      {/* Create Subject Modal */}
      <PaperModal open={showSubjectModal} onClose={() => setShowSubjectModal(false)} title="Create Subject">
        <form
          onSubmit={subjectForm.handleSubmit((data) => createSubject.mutate(data))}
          className="space-y-4"
        >
          <div>
            <label className={labelClass}>Subject Name</label>
            <input {...subjectForm.register("name")} className={inputClass} placeholder="Chemistry" />
            {subjectForm.formState.errors.name && (
              <p className={errorTextClass}>{subjectForm.formState.errors.name.message}</p>
            )}
          </div>
          {subjectFormError && (
            <div className="rounded-lg bg-[#FDF2F2] border border-[#F5C6CB] p-3 text-xs text-[#8C2A2A]">
              {subjectFormError}
            </div>
          )}
          <div className="flex justify-end gap-2 pt-2">
            <button type="button" className={btnSecondaryClass} onClick={() => setShowSubjectModal(false)}>
              Cancel
            </button>
            <button type="submit" className={btnPrimaryClass} disabled={createSubject.isPending}>
              {createSubject.isPending ? "Creating…" : "Create Subject"}
            </button>
          </div>
        </form>
      </PaperModal>

      {/* Create Teacher Assignment Modal */}
      <PaperModal open={showTaModal} onClose={() => setShowTaModal(false)} title="Assign Teacher">
        <form
          onSubmit={taForm.handleSubmit((data) => createTa.mutate(data))}
          className="space-y-4"
        >
          <div>
            <label className={labelClass}>Teacher</label>
            <select {...taForm.register("teacherId")} className={inputClass}>
              <option value="">Select a teacher…</option>
              {teachers.data?.map((t) => (
                <option key={t.id} value={t.id}>{t.fullName} ({t.email})</option>
              ))}
            </select>
            {taForm.formState.errors.teacherId && (
              <p className={errorTextClass}>{taForm.formState.errors.teacherId.message}</p>
            )}
          </div>
          <div>
            <label className={labelClass}>Class</label>
            <select {...taForm.register("classRoomId")} className={inputClass}>
              <option value="">Select a class…</option>
              {classRooms.data?.map((c) => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
            {taForm.formState.errors.classRoomId && (
              <p className={errorTextClass}>{taForm.formState.errors.classRoomId.message}</p>
            )}
          </div>
          <div>
            <label className={labelClass}>Subject</label>
            <select {...taForm.register("subjectId")} className={inputClass}>
              <option value="">Select a subject…</option>
              {subjects.data?.map((s) => (
                <option key={s.id} value={s.id}>{s.name}</option>
              ))}
            </select>
            {taForm.formState.errors.subjectId && (
              <p className={errorTextClass}>{taForm.formState.errors.subjectId.message}</p>
            )}
          </div>
          {taFormError && (
            <div className="rounded-lg bg-[#FDF2F2] border border-[#F5C6CB] p-3 text-xs text-[#8C2A2A]">
              {taFormError}
            </div>
          )}
          <div className="flex justify-end gap-2 pt-2">
            <button type="button" className={btnSecondaryClass} onClick={() => setShowTaModal(false)}>
              Cancel
            </button>
            <button type="submit" className={btnPrimaryClass} disabled={createTa.isPending}>
              {createTa.isPending ? "Assigning…" : "Assign Teacher"}
            </button>
          </div>
        </form>
      </PaperModal>
    </div>
  );
}

// ─── Helper Components ────────────────────────────────────────────────────────

function StatusFilter({
  value,
  onChange,
  options,
  allLabel = "All Statuses",
  label = "Filter Status:",
}: {
  value: string;
  onChange: (v: string) => void;
  options: string[];
  allLabel?: string;
  label?: string;
}) {
  return (
    <div className="flex items-center gap-2">
      <label className="text-xs text-[#7C766C]">{label}</label>
      <select
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className="rounded-lg border border-[#E6E2D6] bg-white px-3 py-1.5 text-xs font-medium text-[#1F1D1A] focus:outline-none"
      >
        <option value="">{allLabel}</option>
        {options.map((o) => (
          <option key={o} value={o}>{o}</option>
        ))}
      </select>
    </div>
  );
}

function TabSection({
  title,
  filter,
  isLoading,
  error,
  refetch,
  empty,
  emptyTitle,
  emptyDesc,
  children,
}: {
  title: string;
  filter?: React.ReactNode;
  isLoading: boolean;
  error: unknown;
  refetch: () => void;
  empty: boolean;
  emptyTitle: string;
  emptyDesc: string;
  children: React.ReactNode;
}) {
  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between flex-wrap gap-2">
        <span className="text-xs font-semibold text-[#7C766C] uppercase tracking-wider">
          {title}
        </span>
        {filter}
      </div>
      {isLoading ? (
        <LoadingState message="Loading…" />
      ) : error ? (
        <ErrorState
          message={getApiErrorMessage(error, "Failed to load data.")}
          onRetry={refetch}
        />
      ) : empty ? (
        <EmptyState title={emptyTitle} description={emptyDesc} />
      ) : (
        children
      )}
    </div>
  );
}
