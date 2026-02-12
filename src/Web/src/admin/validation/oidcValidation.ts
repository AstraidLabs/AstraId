const scopeNameRegex = /^[a-z0-9][a-z0-9:_.-]*$/;
const resourceNameRegex = /^[a-z0-9][a-z0-9:_.-]*$/;
const clientIdRegex = /^[a-zA-Z0-9][a-zA-Z0-9_.-]*$/;

const nameMinLength = 3;
const nameMaxLength = 100;
const clientIdMinLength = 3;
const clientIdMaxLength = 100;

const supportedGrantTypes = ["authorization_code", "refresh_token", "client_credentials", "password"] as const;

export type RedirectUriError = {
  line: number;
  message: string;
};

export function validateScopeName(value: string) {
  const trimmed = value.trim().toLowerCase();
  if (!trimmed) {
    return { value: trimmed, error: "Scope name is required." };
  }
  if (trimmed.length < nameMinLength || trimmed.length > nameMaxLength) {
    return {
      value: trimmed,
      error: `Scope name must be between ${nameMinLength} and ${nameMaxLength} characters.`,
    };
  }
  if (!scopeNameRegex.test(trimmed)) {
    return { value: trimmed, error: "Scope name may use lowercase letters, numbers, :, ., _, -." };
  }
  return { value: trimmed, error: null };
}

export function validateResourceName(value: string) {
  const trimmed = value.trim().toLowerCase();
  if (!trimmed) {
    return { value: trimmed, error: "Resource name is required." };
  }
  if (trimmed.length < nameMinLength || trimmed.length > nameMaxLength) {
    return {
      value: trimmed,
      error: `Resource name must be between ${nameMinLength} and ${nameMaxLength} characters.`,
    };
  }
  if (!resourceNameRegex.test(trimmed)) {
    return { value: trimmed, error: "Resource name may use lowercase letters, numbers, :, ., _, -." };
  }
  return { value: trimmed, error: null };
}

export function validateClientId(value: string) {
  const trimmed = value.trim();
  if (!trimmed) {
    return { value: trimmed, error: "Client ID is required." };
  }
  if (trimmed.length < clientIdMinLength || trimmed.length > clientIdMaxLength) {
    return {
      value: trimmed,
      error: `Client ID must be between ${clientIdMinLength} and ${clientIdMaxLength} characters.`,
    };
  }
  if (!clientIdRegex.test(trimmed)) {
    return { value: trimmed, error: "Client ID may use letters, numbers, underscores, dots, and dashes." };
  }
  return { value: trimmed, error: null };
}

export function validateGrantTypes(grantTypes: string[]) {
  if (!grantTypes || grantTypes.length === 0) {
    return "Select at least one grant type.";
  }
  const invalid = grantTypes.filter((grantType) => !supportedGrantTypes.includes(grantType as never));
  if (invalid.length > 0) {
    return `Unsupported grant types: ${invalid.join(", ")}.`;
  }
  return null;
}

export function validatePkceRules(
  clientType: "public" | "confidential",
  grantTypes: string[],
  pkceRequired: boolean
) {
  if (clientType === "public" && grantTypes.includes("client_credentials")) {
    return "Public clients cannot use client_credentials.";
  }
  if (clientType === "confidential" && pkceRequired) {
    return "PKCE can only be required for public clients.";
  }
  if (pkceRequired && !grantTypes.includes("authorization_code")) {
    return "PKCE requires authorization_code grant type.";
  }
  return null;
}

export function parseRedirectUris(value: string) {
  return value
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter(Boolean);
}

export function validateRedirectUris(value: string, requireAtLeastOne: boolean) {
  const lines = value.split(/\r?\n/);
  const errors: RedirectUriError[] = [];

  const normalized = lines
    .map((line) => line.trim())
    .filter((line) => line.length > 0);

  normalized.forEach((line, index) => {
    if (/\s/.test(line)) {
      errors.push({ line: index + 1, message: "URI must not contain whitespace." });
      return;
    }
    try {
      const uri = new URL(line);
      if (uri.hash) {
        errors.push({ line: index + 1, message: "URI must not include a fragment (#)." });
        return;
      }
      const isHttps = uri.protocol === "https:";
      const isLoopback =
        uri.hostname === "localhost" ||
        uri.hostname === "127.0.0.1" ||
        uri.hostname === "[::1]";
      if (!isHttps && !(isLoopback && uri.protocol === "http:")) {
        errors.push({
          line: index + 1,
          message: "Use HTTPS unless it is a localhost callback.",
        });
      }
    } catch {
      errors.push({ line: index + 1, message: "Must be an absolute URL." });
    }
  });

  if (requireAtLeastOne && normalized.length === 0) {
    errors.push({ line: 0, message: "At least one redirect URI is required." });
  }

  return { normalized, errors };
}

export function getRedirectErrorMessage(errors: RedirectUriError[]) {
  if (errors.length === 0) {
    return null;
  }
  if (errors.some((error) => error.line === 0)) {
    return errors.find((error) => error.line === 0)?.message ?? null;
  }
  return "One or more redirect URIs are invalid. Review the list below.";
}
