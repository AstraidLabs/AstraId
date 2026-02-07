import { Link } from "react-router-dom";
import Card from "./Card";

type Props = {
  title: string;
  message: string;
};

export default function SignedInInfo({ title, message }: Props) {
  return (
    <div className="mx-auto max-w-md">
      <Card title={title}>
        <div className="space-y-4 text-sm text-slate-300">
          <p>{message}</p>
          <Link
            to="/account/security"
            className="inline-flex rounded-lg bg-indigo-500 px-4 py-2 font-semibold text-white transition hover:bg-indigo-400"
          >
            Go to account security
          </Link>
        </div>
      </Card>
    </div>
  );
}
