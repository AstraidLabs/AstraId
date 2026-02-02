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

export type AdminClientScopeItem = {
  name: string;
  displayName?: string | null;
};

export type AdminClientSecretResponse = {
  client: AdminClientDetail;
  clientSecret?: string | null;
};
