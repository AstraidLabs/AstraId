import { authFetch, type AuthResponse, type MfaRecoveryCodesResponse, type MfaSetupResponse, type MfaStatus } from "../api/authServer";

export type MeSummary = {
  userId: string;
  email?: string | null;
  userName?: string | null;
  emailConfirmed: boolean;
  twoFactorEnabled: boolean;
  roles: string[];
  createdUtc?: string | null;
  lastLoginUtc?: string | null;
};

export type SecurityEvent = {
  id: string;
  timestampUtc: string;
  eventType: string;
  ipAddress?: string | null;
  userAgent?: string | null;
  clientId?: string | null;
  traceId?: string | null;
};

export const getOverview = () => authFetch<MeSummary>("/auth/me");

export const changePasswordAccount = (payload: { currentPassword: string; newPassword: string; confirmPassword: string; signOutOtherSessions: boolean; }) =>
  authFetch<AuthResponse>("/auth/me/change-password", { method: "POST", body: JSON.stringify({ currentPassword: payload.currentPassword, newPassword: payload.newPassword, confirmNewPassword: payload.confirmPassword }) });

export const requestEmailChangeAccount = (payload: { newEmail: string; currentPassword: string; returnUrl?: string | null }) =>
  authFetch<AuthResponse>("/auth/me/change-email/start", { method: "POST", body: JSON.stringify({ newEmail: payload.newEmail, password: payload.currentPassword }) });

export const confirmEmailChangeAccount = (payload: { userId: string; newEmail: string; token: string }) =>
  authFetch<AuthResponse>("/auth/me/change-email/confirm", { method: "POST", body: JSON.stringify(payload) });

export const revokeOtherSessionsAccount = () => authFetch<{ success: boolean; message: string }>("/auth/me/signout-all", { method: "POST" });

export const getSecurityEvents = (take = 20) => authFetch<SecurityEvent[]>(`/auth/me/security-events?take=${take}`);

export const getMfaStatusAccount = () => authFetch<MfaStatus>("/auth/mfa/status");
export const startMfaSetupAccount = () => authFetch<MfaSetupResponse>("/auth/mfa/setup/start", { method: "POST" });
export const confirmMfaSetupAccount = (payload: { code: string }) => authFetch<MfaRecoveryCodesResponse>("/auth/mfa/setup/confirm", { method: "POST", body: JSON.stringify(payload) });
export const regenerateRecoveryCodesAccount = () => authFetch<MfaRecoveryCodesResponse>("/auth/mfa/recovery-codes/regenerate", { method: "POST" });
export const disableMfaAccount = (payload: { code: string }) => authFetch<AuthResponse>("/auth/mfa/disable", { method: "POST", body: JSON.stringify(payload) });
