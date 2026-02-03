import { useParams } from "react-router-dom";
import OidcResourceForm from "./OidcResourceForm";

export default function OidcResourceEdit() {
  const { id } = useParams<{ id: string }>();
  if (!id) {
    return null;
  }
  return <OidcResourceForm mode="edit" resourceId={id} />;
}
