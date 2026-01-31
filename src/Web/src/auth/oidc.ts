export const oidcConfig = {
  authority: "https://localhost:7001",
  client_id: "web-spa",
  redirect_uri: "http://localhost:5173/auth/callback",
  post_logout_redirect_uri: "http://localhost:5173/",
  response_type: "code",
  scope: "openid profile email offline_access api"
};
