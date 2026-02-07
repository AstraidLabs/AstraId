import Alert from "../Alert";

type Props = {
  kind: "success" | "error";
  message: string;
};

export default function InlineAlert({ kind, message }: Props) {
  return <Alert variant={kind === "success" ? "success" : "error"}>{message}</Alert>;
}
