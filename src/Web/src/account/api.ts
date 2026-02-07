import { AUTH_SERVER_BASE_URL, type AuthResponse, type MfaRecoveryCodesResponse, type MfaSetupResponse, type MfaStatus, type SecurityOverviewResponse } from "../api/authServer";
import { parseProblemDetails } from "../api/problemDetails";

const toUrl = (path: string) => `${AUTH_SERVER_BASE_URL}${path.startsWith("/") ? path : `/${path}`}`;

const accountFetch = async <T>(path: string, options: RequestInit = {}): Promise<T> => {
  const response = await fetch(toUrl(path), {
    credentials: "include",
    headers: {
      Accept: "application/json",
      "Content-Type": "application/json",
      ...(options.headers ?? {})
    },
    ...options
  });

  if (!response.ok) {
    throw await parseProblemDetails(response);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
};

export const getOverview = () => accountFetch<SecurityOverviewResponse>("/account/security/overview");

export const changePasswordAccount = (payload: {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
  signOutOtherSessions: boolean;
}) => accountFetch<AuthResponse>("/account/password/change", { method: "POST", body: JSON.stringify(payload) });

export const requestEmailChangeAccount = (payload: { newEmail: string; currentPassword: string; returnUrl?: string | null }) =>
  accountFetch<AuthResponse>("/account/email/change-request", { method: "POST", body: JSON.stringify(payload) });

export const confirmEmailChangeAccount = (payload: { userId: string; newEmail: string; token: string }) =>
  accountFetch<AuthResponse>("/account/email/change-confirm", { method: "POST", body: JSON.stringify(payload) });

export const revokeOtherSessionsAccount = (payload: { currentPassword: string }) =>
  accountFetch<AuthResponse>("/account/sessions/revoke-others", { method: "POST", body: JSON.stringify(payload) });

export const getMfaStatusAccount = () => accountFetch<MfaStatus>("/auth/mfa/status");
export const startMfaSetupAccount = () => accountFetch<MfaSetupResponse>("/auth/mfa/setup/start", { method: "POST" });
export const confirmMfaSetupAccount = (payload: { code: string }) =>
  accountFetch<MfaRecoveryCodesResponse>("/auth/mfa/setup/confirm", { method: "POST", body: JSON.stringify(payload) });
export const regenerateRecoveryCodesAccount = () =>
  accountFetch<MfaRecoveryCodesResponse>("/auth/mfa/recovery-codes/regenerate", { method: "POST" });
export const disableMfaAccount = (payload: { code: string }) =>
  accountFetch<AuthResponse>("/auth/mfa/disable", { method: "POST", body: JSON.stringify(payload) });
