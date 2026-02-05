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
  retiredUtc?: string | null;
  algorithm: string;
  keyType: string;
  notBeforeUtc?: string | null;
  notAfterUtc?: string | null;
};

export type AdminSigningKeyRingResponse = {
  keys: AdminSigningKeyListItem[];
  nextRotationDueUtc?: string | null;
  retentionDays: number;
  rotationEnabled: boolean;
  rotationIntervalDays: number;
  checkPeriodMinutes: number;
};

export type AdminSigningKeyRotationResponse = {
  newActiveKid: string;
  previousKid?: string | null;
  activatedUtc: string;
};

export type AdminTokenPreset = {
  accessTokenMinutes: number;
  identityTokenMinutes: number;
  refreshTokenAbsoluteDays: number;
  refreshTokenSlidingDays: number;
};

export type AdminRefreshTokenPolicy = {
  rotationEnabled: boolean;
  reuseDetectionEnabled: boolean;
  reuseLeewaySeconds: number;
};

export type AdminTokenPolicyConfig = {
  public: AdminTokenPreset;
  confidential: AdminTokenPreset;
  refreshPolicy: AdminRefreshTokenPolicy;
};

export type AdminTokenPolicyStatus = {
  activeSigningKid?: string | null;
  rotationEnabled: boolean;
  nextRotationCheckUtc?: string | null;
  currentPolicy: AdminTokenPolicyConfig;
};
