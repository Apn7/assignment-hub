/**
 * Date formatting and timezone conversion utilities.
 */

/**
 * Formats an ISO UTC date string into a readable local date and time.
 * Example: "Aug 15, 2026, 3:30 PM"
 */
export function formatDateTime(isoString: string | null | undefined): string {
  if (!isoString) return "N/A";
  const date = new Date(isoString);
  if (isNaN(date.getTime())) return "Invalid date";

  return new Intl.DateTimeFormat("en-US", {
    month: "short",
    day: "numeric",
    year: "numeric",
    hour: "numeric",
    minute: "2-digit",
    hour12: true,
  }).format(date);
}

/**
 * Checks if a given ISO UTC deadline string has passed relative to local time.
 */
export function isPastDeadline(isoString: string | null | undefined): boolean {
  if (!isoString) return false;
  return new Date(isoString).getTime() < Date.now();
}

/**
 * Converts an ISO UTC date string to `YYYY-MM-DDTHH:mm` format suitable for `<input type="datetime-local">`.
 */
export function toDatetimeLocal(isoString?: string): string {
  if (!isoString) return "";
  const d = new Date(isoString);
  if (isNaN(d.getTime())) return "";

  const pad = (n: number) => String(n).padStart(2, "0");
  const year = d.getFullYear();
  const month = pad(d.getMonth() + 1);
  const day = pad(d.getDate());
  const hours = pad(d.getHours());
  const minutes = pad(d.getMinutes());

  return `${year}-${month}-${day}T${hours}:${minutes}`;
}

/**
 * Converts a `<input type="datetime-local">` value (`YYYY-MM-DDTHH:mm`) to a UTC ISO string.
 */
export function toUtcIso(datetimeLocalString: string): string {
  if (!datetimeLocalString) return "";
  const d = new Date(datetimeLocalString);
  return d.toISOString();
}
