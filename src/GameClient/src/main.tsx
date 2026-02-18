import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { userManager } from './auth/oidc'
import { GamePage } from './pages/GamePage'
import './styles.css'

function Login() {
  return <div className="flex h-full items-center justify-center">
    <button className="rounded bg-indigo-600 px-4 py-2" onClick={() => userManager.signinRedirect()}>Login with AstraID</button>
  </div>
}

function Callback() {
  userManager.signinRedirectCallback().then(() => window.location.assign('/game'))
  return <div className="p-6">Completing sign-in...</div>
}

function ProtectedGame() {
  userManager.getUser().then((u) => { if (!u) window.location.assign('/login') })
  return <GamePage />
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Navigate to="/game" />} />
        <Route path="/login" element={<Login />} />
        <Route path="/auth/callback" element={<Callback />} />
        <Route path="/game" element={<ProtectedGame />} />
      </Routes>
    </BrowserRouter>
  </StrictMode>
)
