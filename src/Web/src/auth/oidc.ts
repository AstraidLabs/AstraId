import { WebStorageStateStore, type UserManagerSettings } from "oidc-client-ts";

const authority = import.meta.env.VITE_AUTH_AUTHORITY ?? "https://localhost:7001";
const clientId = import.meta.env.VITE_AUTH_CLIENT_ID ?? "web-spa";
const redirectUri =
  import.meta.env.VITE_AUTH_REDIRECT_URI ??
  "http://localhost:5173/auth/callback";
const postLogoutRedirectUri =
  import.meta.env.VITE_AUTH_POST_LOGOUT_REDIRECT_URI ?? "http://localhost:5173/";
const scope =
  import.meta.env.VITE_AUTH_SCOPE ??
  "openid profile email offline_access api";

export const oidcConfig: UserManagerSettings = {
  authority,
  client_id: clientId,
  redirect_uri: redirectUri,
  post_logout_redirect_uri: postLogoutRedirectUri,
  response_type: "code",
  scope,
  userStore: new WebStorageStateStore({ store: window.sessionStorage })
};
