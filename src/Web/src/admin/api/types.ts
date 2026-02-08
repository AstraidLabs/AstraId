export type PagedResult<T> = {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export type AdminClientListItem = {
  id: string;
  clientId: string;
  displayName?: string | null;
  clientType: string;
  enabled: boolean;
};

export type AdminClientDetail = {
  id: string;
  clientId: string;
  displayName?: string | null;
  clientType: string;
  enabled: boolean;
  grantTypes: string[];
  pkceRequired: boolean;
  scopes: string[];
  redirectUris: string[];
  postLogoutRedirectUris: string[];
  profile?: string | null;
  presetId?: string | null;
  presetVersion?: number | null;
  systemManaged: boolean;
};

export type AdminClientProfileRule = {
  profile: string;
  summary: string;
  allowedGrantTypes: string[];
  requiresPkceForAuthorizationCode: boolean;
  requiresClientSecret: boolean;
  allowsRedirectUris: boolean;
  allowOfflineAccess: boolean;
  redirectPolicy: string;
  ruleCodes: string[];
};

export type AdminClientProfileRulesResponse = {
  version: number;
  profiles: AdminClientProfileRule[];
};

export type AdminClientPresetListItem = {
  id: string;
  name: string;
  profile: string;
  summary: string;
  version: number;
};

export type AdminClientPresetDetail = {
  id: string;
  name: string;
  profile: string;
  summary: string;
  version: number;
  defaults: {
    clientType: string;
    pkceRequired: boolean;
    grantTypes: string[];
    redirectUris: string[];
    postLogoutRedirectUris: string[];
    scopes: string[];
  };
  lockedFields: string[];
  allowedOverrides: string[];
};

export type AdminOidcScopeListItem = {
  id: string;
  name: string;
  displayName?: string | null;
  description?: string | null;
  resources: string[];
  claims: string[];
};

export type AdminOidcScopeDetail = AdminOidcScopeListItem;

export type AdminOidcScopeUsage = {
  clientCount: number;
};

export type AdminOidcResourceListItem = {
  id: string;
  name: string;
  displayName?: string | null;
  description?: string | null;
  isActive: boolean;
  createdUtc: string;
  updatedUtc: string;
};

export type AdminOidcResourceDetail = AdminOidcResourceListItem;

export type AdminOidcResourceUsage = {
  scopeCount: number;
};

export type AdminClientSecretResponse = {
  client: AdminClientDetail;
  clientSecret?: string | null;
};

export type AdminUserListItem = {
  id: string;
  email?: string | null;
  userName?: string | null;
  emailConfirmed: boolean;
  isLockedOut: boolean;
  isActive: boolean;
  roles: string[];
};

export type AdminUserDetail = {
  id: string;
  email?: string | null;
  userName?: string | null;
  phoneNumber?: string | null;
  emailConfirmed: boolean;
  twoFactorEnabled: boolean;
  recoveryCodesLeft: number;
  isLockedOut: boolean;
  isActive: boolean;
  roles: string[];
};

export type AdminRoleListItem = {
  id: string;
  name: string;
  isSystem: boolean;
};

export type AdminRoleDetail = {
  id: string;
  name: string;
  isSystem: boolean;
  permissionIds: string[];
};

export type AdminRoleUsage = {
  userCount: number;
};

export type AdminPermissionItem = {
  id: string;
  key: string;
  description: string;
  group: string;
  isSystem: boolean;
};

export type AdminPermissionUsage = {
  roleCount: number;
  endpointCount: number;
};

export type AdminApiResourceListItem = {
  id: string;
  name: string;
  displayName: string;
  baseUrl?: string | null;
  isActive: boolean;
};

export type AdminApiResourceDetail = AdminApiResourceListItem & {
  apiKey?: string | null;
};

export type AdminApiEndpointListItem = {
  id: string;
  method: string;
  path: string;
  displayName?: string | null;
  isDeprecated: boolean;
  isActive: boolean;
  permissionIds: string[];
  permissionKeys: string[];
};

export type AdminAuditListItem = {
  id: string;
  timestampUtc: string;
  action: string;
  targetType: string;
  targetId?: string | null;
  actorUserId?: string | null;
  actorEmail?: string | null;
  dataJson?: string | null;
};

export type AdminErrorLogListItem = {
  id: string;
  timestampUtc: string;
  traceId: string;
  path: string;
  method: string;
  statusCode: number;
  title: string;
  detail: string;
  actorUserId?: string | null;
  actorEmail?: string | null;
};

export type AdminErrorLogDetail = AdminErrorLogListItem & {
  exceptionType?: string | null;
  stackTrace?: string | null;
  innerException?: string | null;
  dataJson?: string | null;
  userAgent?: string | null;
  remoteIp?: string | null;
};

export type AdminSessionInfo = {
  userId: string;
  email?: string | null;
  userName?: string | null;
  roles: string[];
  permissions: string[];
};

export type AdminSigningKeyListItem = {
  kid: string;
  status: string;
  createdUtc: string;
  activatedUtc?: string | null;
  retireAfterUtc?: string | null;
  retiredUtc?: string | null;
  revokedUtc?: string | null;
  algorithm: string;
  keyType: string;
  notBeforeUtc?: string | null;
  notAfterUtc?: string | null;
  isPublished: boolean;
};

export type AdminSigningKeyRingResponse = {
  keys: AdminSigningKeyListItem[];
  nextRotationDueUtc?: string | null;
  nextRotationCheckUtc?: string | null;
  lastRotationUtc?: string | null;
  gracePeriodDays: number;
  rotationEnabled: boolean;
  rotationIntervalDays: number;
  checkPeriodMinutes: number;
};

export type AdminSigningKeyRotationResponse = {
  newActiveKid: string;
  previousKid?: string | null;
  activatedUtc: string;
};

export type AdminTokenPolicyValues = {
  accessTokenMinutes: number;
  identityTokenMinutes: number;
  authorizationCodeMinutes: number;
  refreshTokenDays: number;
  refreshRotationEnabled: boolean;
  refreshReuseDetectionEnabled: boolean;
  refreshReuseLeewaySeconds: number;
  clockSkewSeconds: number;
};

export type AdminTokenPolicyGuardrails = {
  minAccessTokenMinutes: number;
  maxAccessTokenMinutes: number;
  minIdentityTokenMinutes: number;
  maxIdentityTokenMinutes: number;
  minAuthorizationCodeMinutes: number;
  maxAuthorizationCodeMinutes: number;
  minRefreshTokenDays: number;
  maxRefreshTokenDays: number;
  minClockSkewSeconds: number;
  maxClockSkewSeconds: number;
};

export type AdminTokenPolicyConfig = {
  policy: AdminTokenPolicyValues;
  guardrails: AdminTokenPolicyGuardrails;
};

export type AdminTokenPolicyStatus = {
  activeSigningKid?: string | null;
  rotationEnabled: boolean;
  nextRotationCheckUtc?: string | null;
  currentPolicy: AdminTokenPolicyConfig;
};

export type AdminKeyRotationPolicyValues = {
  enabled: boolean;
  rotationIntervalDays: number;
  gracePeriodDays: number;
  jwksCacheMarginMinutes: number;
  nextRotationUtc?: string | null;
  lastRotationUtc?: string | null;
};

export type AdminKeyRotationPolicyGuardrails = {
  minRotationIntervalDays: number;
  maxRotationIntervalDays: number;
  minGracePeriodDays: number;
  maxGracePeriodDays: number;
  minJwksCacheMarginMinutes: number;
  maxJwksCacheMarginMinutes: number;
  preventDisableRotationInProduction: boolean;
};

export type AdminKeyRotationPolicyResponse = {
  policy: AdminKeyRotationPolicyValues;
  guardrails: AdminKeyRotationPolicyGuardrails;
};

export type AdminKeyRotationPolicyRequest = {
  enabled: boolean;
  rotationIntervalDays: number;
  gracePeriodDays: number;
  jwksCacheMarginMinutes: number;
  breakGlass: boolean;
  reason?: string | null;
};

export type AdminSigningKeyJwksResponse = {
  jwksJson: string;
};

export type AdminTokenIncidentListItem = {
  id: string;
  timestampUtc: string;
  type: string;
  severity: string;
  userId?: string | null;
  clientId?: string | null;
  traceId?: string | null;
  detailJson?: string | null;
};

export type AdminTokenIncidentDetail = AdminTokenIncidentListItem & {
  actorUserId?: string | null;
};

export type AdminDataProtectionStatus = {
  keysPersisted: boolean;
  keyPath?: string | null;
  readOnly: boolean;
  keyCount: number;
  latestKeyActivationUtc?: string | null;
  latestKeyExpirationUtc?: string | null;
};

export type AdminEncryptionKeyStatus = {
  enabled: boolean;
  source: string;
  thumbprint?: string | null;
  subject?: string | null;
  notBeforeUtc?: string | null;
  notAfterUtc?: string | null;
};

export type AdminRevocationResult = {
  tokensRevoked: number;
  authorizationsRevoked: number;
};

export type AdminUserLifecyclePolicy = {
  id: string;
  enabled: boolean;
  deactivateAfterDays: number;
  deleteAfterDays: number;
  hardDeleteAfterDays?: number | null;
  hardDeleteEnabled: boolean;
  warnBeforeLogoutMinutes: number;
  idleLogoutMinutes: number;
  updatedUtc: string;
  updatedByUserId?: string | null;
};

export type AdminUserLifecyclePreview = {
  wouldDeactivate: number;
  wouldAnonymize: number;
  wouldHardDelete: number;
};
