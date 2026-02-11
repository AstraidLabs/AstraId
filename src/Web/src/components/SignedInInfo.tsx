import { Link } from "react-router-dom";
import Card from "./Card";
import { useLanguage } from "../i18n/LanguageProvider";

type Props = {
  title: string;
  message: string;
};

export default function SignedInInfo({ title, message }: Props) {
  const { t } = useLanguage();
  return (
    <div className="mx-auto max-w-md">
      <Card title={title}>
        <div className="space-y-4 text-sm text-slate-300">
          <p>{message}</p>
          <Link
            to="/account/security"
            className="inline-flex rounded-lg bg-indigo-500 px-4 py-2 font-semibold text-white transition hover:bg-indigo-400"
          >
            {t("signedIn.goSecurity")}
          </Link>
        </div>
      </Card>
    </div>
  );
}
