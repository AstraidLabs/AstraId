import { Outlet } from "react-router-dom";
import Container from "../components/Container";
import useDocumentMeta from "../hooks/useDocumentMeta";

const AuthLayout = () => {
  useDocumentMeta({
    title: "AstraId | Sign in",
    description: "Sign in or register for AstraId with secure authentication and account protections."
  });

  return (
    <div className="auth-space-bg relative min-h-screen overflow-hidden bg-slate-950 text-slate-100">
      <div className="auth-space-stars" aria-hidden="true" />
      <div className="auth-space-stars auth-space-stars--slow" aria-hidden="true" />
      <div className="auth-space-glow" aria-hidden="true" />
      <Container>
        <main className="relative z-10 flex min-h-screen items-center justify-center py-10">
          <div className="w-full rounded-3xl border border-slate-700/40 bg-slate-950/55 p-1 backdrop-blur-sm">
            <Outlet />
          </div>
        </main>
      </Container>
    </div>
  );
};

export default AuthLayout;
