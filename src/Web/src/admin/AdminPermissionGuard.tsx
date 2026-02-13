import type { PropsWithChildren } from "react";
import { Navigate } from "react-router-dom";
import { hasAnyPermission } from "../auth/adminAccess";
import { useAuthSession } from "../auth/useAuthSession";
import { toAdminRoute } from "../routing";

type Props = PropsWithChildren<{
  required: readonly string[];
}>;

export default function AdminPermissionGuard({ required, children }: Props) {
  const { session } = useAuthSession();

  if (!hasAnyPermission(required, session?.permissions)) {
    return <Navigate to={toAdminRoute("/")} replace />;
  }

  return <>{children}</>;
}
