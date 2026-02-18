import { UserManager } from 'oidc-client-ts'

const authority = import.meta.env.VITE_AUTH_AUTHORITY ?? 'https://localhost:7001'
const clientId = import.meta.env.VITE_AUTH_CLIENT_ID ?? 'astraid-web'
const redirectUri = import.meta.env.VITE_AUTH_REDIRECT_URI ?? 'http://localhost:5174/auth/callback'

export const userManager = new UserManager({
  authority,
  client_id: clientId,
  redirect_uri: redirectUri,
  response_type: 'code',
  scope: 'openid profile api.read api.write',
  post_logout_redirect_uri: 'http://localhost:5174/login',
  automaticSilentRenew: false,
  userStore: window.sessionStorage
})
