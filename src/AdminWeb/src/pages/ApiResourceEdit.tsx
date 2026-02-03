import { useParams } from "react-router-dom";
import ApiResourceForm from "./ApiResourceForm";

export default function ApiResourceEdit() {
  const { id } = useParams<{ id: string }>();
  if (!id) {
    return null;
  }
  return <ApiResourceForm mode="edit" resourceId={id} />;
}
