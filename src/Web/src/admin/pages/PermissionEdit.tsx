import { useParams } from "react-router-dom";
import PermissionForm from "./PermissionForm";

export default function PermissionEdit() {
  const { id } = useParams<{ id: string }>();
  if (!id) {
    return null;
  }
  return <PermissionForm mode="edit" permissionId={id} />;
}
