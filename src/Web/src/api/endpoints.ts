import { apiFetch } from "./http";

export type PublicMessage = { message: string };
export type MeResponse = {
  sub: string;
  name?: string | null;
  email?: string | null;
  permissions: string[];
};

export type PingResponse = { status: string } | Record<string, unknown>;

export const getPublicMessage = () => apiFetch<PublicMessage>("/api/public");

export const getMe = (token: string) =>
  apiFetch<MeResponse>("/api/me", { token });

export const getAdminPing = (token: string) =>
  apiFetch<PingResponse>("/api/admin/ping", { token });

export const getAuthServerIntegrationPing = (token: string) =>
  apiFetch<PingResponse>("/api/integrations/authserver/ping", { token });

export const getCmsIntegrationPing = (token: string) =>
  apiFetch<PingResponse>("/api/integrations/cms/ping", { token });
