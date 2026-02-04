const permissionKeyRegex = /^[a-z0-9]+(\.[a-z0-9]+)*$/;
const roleNameRegex = /^[A-Za-z0-9][A-Za-z0-9 _\.-]*$/;
const httpMethods = ["GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS"] as const;

export function validatePermissionKey(value: string) {
  const trimmed = value.trim();
  if (!trimmed) {
    return { value: trimmed, error: "Permission key is required." };
  }
  if (trimmed.length < 3 || trimmed.length > 100) {
    return { value: trimmed, error: "Permission key must be between 3 and 100 characters." };
  }
  if (!permissionKeyRegex.test(trimmed)) {
    return {
      value: trimmed,
      error: "Use lowercase letters, numbers, and dots only (e.g. system.admin).",
    };
  }
  return { value: trimmed, error: null };
}

export function validatePermissionDescription(value: string) {
  const trimmed = value.trim();
  if (!trimmed) {
    return { value: trimmed, error: "Description is required." };
  }
  if (trimmed.length > 250) {
    return { value: trimmed, error: "Description must be 250 characters or fewer." };
  }
  if (hasControlChars(trimmed)) {
    return { value: trimmed, error: "Description must not contain control characters." };
  }
  return { value: trimmed, error: null };
}

export function validatePermissionGroup(value: string) {
  const trimmed = value.trim();
  if (!trimmed) {
    return { value: trimmed, error: null };
  }
  if (trimmed.length > 100) {
    return { value: trimmed, error: "Group must be 100 characters or fewer." };
  }
  if (hasControlChars(trimmed)) {
    return { value: trimmed, error: "Group must not contain control characters." };
  }
  return { value: trimmed, error: null };
}

export function validateRoleName(value: string) {
  const trimmed = value.trim();
  if (!trimmed) {
    return { value: trimmed, error: "Role name is required." };
  }
  if (trimmed.length < 2 || trimmed.length > 64) {
    return { value: trimmed, error: "Role name must be between 2 and 64 characters." };
  }
  if (!roleNameRegex.test(trimmed)) {
    return {
      value: trimmed,
      error: "Use letters, numbers, spaces, dots, underscores, or dashes only.",
    };
  }
  return { value: trimmed, error: null };
}

export function validateAuditDateRange(fromUtc: string, toUtc: string) {
  if (!fromUtc || !toUtc) {
    return null;
  }
  const fromDate = new Date(fromUtc);
  const toDate = new Date(toUtc);
  if (Number.isNaN(fromDate.getTime()) || Number.isNaN(toDate.getTime())) {
    return "Date range is invalid.";
  }
  if (fromDate > toDate) {
    return "From date must be earlier than to date.";
  }
  return null;
}

export function validateEmail(value: string) {
  const trimmed = value.trim();
  if (!trimmed) {
    return { value: trimmed, error: "Email is required." };
  }
  const isValid = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(trimmed);
  if (!isValid) {
    return { value: trimmed, error: "Enter a valid email address." };
  }
  return { value: trimmed, error: null };
}

export function validateHttpMethod(value: string) {
  const trimmed = value.trim().toUpperCase();
  if (!trimmed) {
    return { value: trimmed, error: "HTTP method is required." };
  }
  if (!httpMethods.includes(trimmed as (typeof httpMethods)[number])) {
    return { value: trimmed, error: "Use a standard HTTP method (GET, POST, PUT, PATCH, DELETE, HEAD, OPTIONS)." };
  }
  return { value: trimmed, error: null };
}

export function validateEndpointPath(value: string) {
  const trimmed = value.trim();
  if (!trimmed) {
    return { value: trimmed, error: "Path is required." };
  }
  if (!trimmed.startsWith("/")) {
    return { value: trimmed, error: "Path must start with '/'." };
  }
  if (trimmed.length > 256) {
    return { value: trimmed, error: "Path must be 256 characters or fewer." };
  }
  if (/\s/.test(trimmed)) {
    return { value: trimmed, error: "Path must not contain whitespace." };
  }
  return { value: trimmed, error: null };
}

function hasControlChars(value: string) {
  return Array.from(value).some((char) => char <= "\u001F" || char === "\u007F");
}
