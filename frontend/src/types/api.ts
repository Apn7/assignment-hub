/**
 * Shapes returned by the ASP.NET Core API.
 *
 * The backend serialises with the default camelCase policy, so these mirror the
 * C# contracts in `backend/src/AssignmentHub.Application/DTOs` field for field.
 */

/** Payload of `GET /api/health`. */
export interface HealthResponse {
  status: string;
  environment: string;
  /** ISO-8601 timestamp. */
  timestampUtc: string;
}

/** The single error shape every failing endpoint returns. */
export interface ApiErrorResponse {
  status: number;
  title: string;
  detail?: string | null;
  /** Correlates the response with the server log entry. */
  traceId?: string | null;
  /** Field-level validation failures, keyed by property name. */
  errors?: Record<string, string[]> | null;
}

/** Payload of `GET /api/teacher-assignments/mine`. */
export interface TeacherAssignmentResponse {
  classRoomId: string;
  classRoomName: string;
  subjectId: string;
  subjectName: string;
}

/** Payload of assignment endpoints. */
export interface AssignmentResponse {
  id: string;
  title: string;
  description: string;
  classRoomId: string;
  classRoomName: string;
  subjectId: string;
  subjectName: string;
  createdByTeacherId: string;
  createdByTeacherName: string;
  /** ISO UTC string. */
  deadline: string;
  maxMarks: number;
  /** "Draft" | "Published". */
  status: "Draft" | "Published";
  createdAt: string;
  updatedAt: string;
}

/** Request body for `POST /api/assignments`. */
export interface CreateAssignmentRequest {
  title: string;
  description: string;
  classRoomId: string;
  subjectId: string;
  /** ISO UTC string. */
  deadline: string;
  maxMarks: number;
}

/** Request body for `PUT /api/assignments/{id}`. */
export interface UpdateAssignmentRequest {
  title: string;
  description: string;
  classRoomId: string;
  subjectId: string;
  /** ISO UTC string. */
  deadline: string;
  maxMarks: number;
}

/** Detailed submission response shape. */
export interface SubmissionResponse {
  id: string;
  assignmentId: string;
  assignmentTitle: string;
  classRoomName: string;
  subjectName: string;
  maxMarks: number;
  deadline: string;
  studentId: string;
  studentName: string;
  answerText: string;
  submittedAt: string;
  updatedAt: string;
  /** "Submitted" | "Reviewed". */
  status: "Submitted" | "Reviewed";
  marks: number | null;
  feedback: string | null;
  reviewedAt: string | null;
}

/** Submission summary item shape in list views. */
export interface SubmissionListItem {
  id: string;
  assignmentId: string;
  assignmentTitle: string;
  classRoomName: string;
  studentId: string;
  studentName: string;
  submittedAt: string;
  updatedAt: string;
  status: "Submitted" | "Reviewed";
  marks: number | null;
  maxMarks: number;
}

/** Body for `POST /api/assignments/{id}/submissions`. */
export interface SubmitAnswerRequest {
  answerText: string;
}

/** Body for `PUT /api/assignments/{id}/submissions/mine`. */
export interface UpdateSubmissionRequest {
  answerText: string;
}

/** Body for `POST /api/submissions/{id}/grade`. */
export interface GradeSubmissionRequest {
  marks: number;
  feedback?: string | null;
}

/** Body for `POST /api/submissions/{id}/status`. */
export interface ChangeSubmissionStatusRequest {
  status: "Submitted" | "Reviewed";
}

// ─── Admin Management Types ──────────────────────────────────────────────────

/** Body for `POST /api/admin/users`. */
export interface CreateUserRequest {
  fullName: string;
  email: string;
  password: string;
  role: "Admin" | "Teacher" | "Student";
  classRoomId?: string | null;
}

/** Response from admin user endpoints. */
export interface AdminUserResponse {
  id: string;
  fullName: string;
  email: string;
  role: string;
  classRoomId: string | null;
  classRoomName: string | null;
}

/** Body for `POST /api/admin/classrooms`. */
export interface CreateClassRoomRequest {
  name: string;
}

/** Response from admin classroom endpoints. */
export interface AdminClassRoomResponse {
  id: string;
  name: string;
}

/** Body for `POST /api/admin/subjects`. */
export interface CreateSubjectRequest {
  name: string;
}

/** Response from admin subject endpoints. */
export interface AdminSubjectResponse {
  id: string;
  name: string;
}

/** Body for `POST /api/admin/teacher-assignments`. */
export interface CreateTeacherAssignmentAdminRequest {
  teacherId: string;
  classRoomId: string;
  subjectId: string;
}

/** Response from admin teacher-assignment endpoints. */
export interface AdminTeacherAssignmentResponse {
  id: string;
  teacherId: string;
  teacherName: string;
  classRoomId: string;
  classRoomName: string;
  subjectId: string;
  subjectName: string;
}

