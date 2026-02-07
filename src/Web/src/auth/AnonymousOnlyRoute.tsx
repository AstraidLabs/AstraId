import type { PropsWithChildren } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useAuthSession } from "./useAuthSession";

const defaultTarget = "/account";

const isSafeReturnUrl = (value: string | null) =>
  !!value && value.startsWith("/") && !value.startsWith("//");

export default function AnonymousOnlyRoute({ children }: PropsWithChildren) {
  const { status } = useAuthSession();
  const location = useLocation();

  if (status === "loading") {
    return <div className="py-10 text-sm text-slate-400">Loading…</div>;
  }

  if (status === "authenticated") {
    const params = new URLSearchParams(location.search);
    const returnUrl = params.get("returnUrl");
    const target = isSafeReturnUrl(returnUrl) ? returnUrl : defaultTarget;
    return <Navigate to={target} replace />;
  }

  return <>{children}</>;
}
