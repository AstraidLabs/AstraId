import { useParams } from "react-router-dom";
import OidcScopeForm from "./OidcScopeForm";

export default function OidcScopeEdit() {
  const { id } = useParams<{ id: string }>();
  if (!id) {
    return null;
  }
  return <OidcScopeForm mode="edit" scopeId={id} />;
}
