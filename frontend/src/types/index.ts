export type Role = "Admin" | "Teacher" | "Student";
export type User = { id: string; fullName: string; email: string; role: Role };
