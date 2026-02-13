import { parseErrorResponse } from "./errors";
import { getPreferredLanguageTag } from "../i18n/language";

export const AUTH_SERVER_BASE_URL =
  import.meta.env.VITE_AUTHSERVER_BASE_URL ?? "https://localhost:7001";

const normalizePath = (path: string) =>
  path.startsWith("http")
    ? path
    : `${AUTH_SERVER_BASE_URL}${path.startsWith("/") ? "" : "/"}${path}`;

export type AuthResponse = {
  success: boolean;
  redirectTo?: string | null;
  error?: string | null;
  message?: string | null;
};

export type LoginResponse = AuthResponse & {
  requiresTwoFactor?: boolean;
  mfaToken?: string | null;
};

export type AuthSession = {
  isAuthenticated: boolean;
  userId?: string | null;
  email?: string | null;
  userName?: string | null;
  roles: string[];
  permissions: string[];
};

export type AuthRequestOptions = RequestInit;

export const authFetch = async <T>(
  path: string,
  options: AuthRequestOptions = {}
): Promise<T> => {
  const response = await fetch(normalizePath(path), {
    credentials: "include",
    headers: {
      Accept: "application/json",
      "Content-Type": "application/json",
      "Accept-Language": getPreferredLanguageTag(),
      ...(options.headers ?? {})
    },
    ...options
  });

  if (!response.ok) {
    throw await parseErrorResponse(response);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
};


export type LoginContext = {
  hasOidcContext: boolean;
  clientId?: string | null;
  appName?: string | null;
  logoUrl?: string | null;
  homeUrl?: string | null;
  privacyUrl?: string | null;
  termsUrl?: string | null;
  requestedScopes: string[];
};

export const getLoginContext = (returnUrl?: string | null) => {
  const params = new URLSearchParams();
  if (returnUrl) {
    params.set("returnUrl", returnUrl);
  }
  return authFetch<LoginContext>(`/auth/login-context?${params.toString()}`);
};

export const login = (payload: {
  emailOrUsername: string;
  password: string;
  returnUrl?: string | null;
}) =>
  authFetch<LoginResponse>("/auth/login", {
    method: "POST",
    body: JSON.stringify(payload)
  });

export const loginMfa = (payload: {
  mfaToken: string;
  code: string;
  rememberMachine?: boolean;
  useRecoveryCode?: boolean;
}) =>
  authFetch<AuthResponse>("/auth/login/mfa", {
    method: "POST",
    body: JSON.stringify({
      mfaToken: payload.mfaToken,
      code: payload.code,
      rememberMachine: payload.rememberMachine ?? false,
      useRecoveryCode: payload.useRecoveryCode ?? false
    })
  });

export const register = (payload: {
  email: string;
  password: string;
  confirmPassword: string;
  returnUrl?: string | null;
}) =>
  authFetch<AuthResponse>("/auth/register", {
    method: "POST",
    body: JSON.stringify(payload)
  });

export const forgotPassword = (payload: { email: string }) =>
  authFetch<AuthResponse>("/auth/forgot-password", {
    method: "POST",
    body: JSON.stringify(payload)
  });

export const resetPassword = (payload: {
  email: string;
  token: string;
  newPassword: string;
  confirmPassword: string;
}) =>
  authFetch<AuthResponse>("/auth/reset-password", {
    method: "POST",
    body: JSON.stringify(payload)
  });

export const activateAccount = (payload: { email: string; token: string }) =>
  authFetch<AuthResponse>("/auth/activate", {
    method: "POST",
    body: JSON.stringify(payload)
  });

export const logout = () =>
  authFetch<void>("/auth/logout", {
    method: "POST"
  });

export const getSession = () => authFetch<AuthSession>("/auth/session");

export type MfaStatus = {
  enabled: boolean;
  hasAuthenticatorKey: boolean;
  recoveryCodesLeft: number;
};

export type MfaSetupResponse = {
  sharedKey: string;
  otpAuthUri: string;
  qrCodeSvg: string;
};

export type MfaRecoveryCodesResponse = {
  recoveryCodes: string[];
};

export const getMfaStatus = () =>
  authFetch<MfaStatus>("/auth/mfa/status");

export const startMfaSetup = () =>
  authFetch<MfaSetupResponse>("/auth/mfa/setup/start", {
    method: "POST"
  });

export const confirmMfaSetup = (payload: { code: string }) =>
  authFetch<MfaRecoveryCodesResponse>("/auth/mfa/setup/confirm", {
    method: "POST",
    body: JSON.stringify(payload)
  });

export const regenerateRecoveryCodes = () =>
  authFetch<MfaRecoveryCodesResponse>("/auth/mfa/recovery-codes/regenerate", {
    method: "POST"
  });

export const disableMfa = (payload: { code: string }) =>
  authFetch<AuthResponse>("/auth/mfa/disable", {
    method: "POST",
    body: JSON.stringify(payload)
  });

export const resolveReturnUrl = (returnUrl: string | null) => {
  if (!returnUrl) {
    return null;
  }

  if (returnUrl.startsWith("/")) {
    return `${AUTH_SERVER_BASE_URL}${returnUrl}`;
  }

  return returnUrl;
};


export type SecurityOverviewResponse = {
  emailConfirmed: boolean;
  twoFactorEnabled: boolean;
  recoveryCodesLeft: number;
  hasAuthenticatorKey: boolean;
  email?: string | null;
  userName?: string | null;
};

export const changePassword = (payload: {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
  signOutOtherSessions: boolean;
}) =>
  authFetch<AuthResponse>("/account/password/change", {
    method: "POST",
    body: JSON.stringify(payload)
  });

export const requestEmailChange = (payload: {
  newEmail: string;
  returnUrl?: string | null;
  currentPassword: string;
}) =>
  authFetch<AuthResponse>("/account/email/change-request", {
    method: "POST",
    body: JSON.stringify(payload)
  });

export const confirmEmailChange = (payload: {
  userId: string;
  newEmail: string;
  token: string;
}) =>
  authFetch<AuthResponse>("/account/email/change-confirm", {
    method: "POST",
    body: JSON.stringify(payload)
  });

export const revokeOtherSessions = (payload: { currentPassword: string }) =>
  authFetch<AuthResponse>("/account/sessions/revoke-others", {
    method: "POST",
    body: JSON.stringify(payload)
  });

export const getSecurityOverview = () =>
  authFetch<SecurityOverviewResponse>("/account/security/overview");


export type LanguagePreferenceResponse = {
  preferredLanguage?: string | null;
  effectiveLanguage: string;
};

export const getLanguagePreference = () =>
  authFetch<LanguagePreferenceResponse>("/account/preferences");

export const setLanguagePreference = (language: string) =>
  authFetch<LanguagePreferenceResponse>("/account/preferences/language", {
    method: "PUT",
    body: JSON.stringify({ language })
  });
