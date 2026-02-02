import { useParams } from "react-router-dom";
import ClientForm from "./ClientForm";

export default function ClientEdit() {
  const { id } = useParams();
  if (!id) {
    return null;
  }
  return <ClientForm mode="edit" clientId={id} />;
}
