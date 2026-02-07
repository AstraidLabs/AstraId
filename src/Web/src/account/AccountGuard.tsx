import type { PropsWithChildren } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useAuthSession } from "../auth/useAuthSession";

export default function AccountGuard({ children }: PropsWithChildren) {
  const { session, isLoading } = useAuthSession();
  const location = useLocation();

  if (isLoading) {
    return <div className="py-10 text-sm text-slate-400">Loading account...</div>;
  }

  if (!session?.isAuthenticated) {
    const returnUrl = `${location.pathname}${location.search}${location.hash}`;
    return <Navigate to={`/login?returnUrl=${encodeURIComponent(returnUrl)}`} replace />;
  }

  return <>{children}</>;
}
