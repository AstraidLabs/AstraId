import type { PropsWithChildren } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { hasAdminAccess } from "../auth/adminAccess";
import { useAuthSession } from "../auth/useAuthSession";
import { toAdminReturnUrl } from "../routing";


export default function AdminGuard({ children }: PropsWithChildren) {
  const { session, isLoading } = useAuthSession();
  const location = useLocation();

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-950 text-slate-100">
        Načítám...
      </div>
    );
  }

  if (!session?.isAuthenticated) {
    const returnUrl = toAdminReturnUrl(
      `${location.pathname}${location.search}${location.hash}`
    );
    const target = `/login?returnUrl=${encodeURIComponent(returnUrl)}`;
    return <Navigate to={target} replace />;
  }

  if (!hasAdminAccess(session.permissions)) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-950 text-slate-100">
        <div className="rounded-lg border border-slate-800 bg-slate-900/40 p-8 text-center">
          <h1 className="text-xl font-semibold text-white">Access denied</h1>
          <p className="mt-2 text-sm text-slate-400">
            Nemáte oprávnění pro administraci systému.
          </p>
        </div>
      </div>
    );
  }

  return <>{children}</>;
}
