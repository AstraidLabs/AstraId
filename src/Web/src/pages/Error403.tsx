import { useLanguage } from "../i18n/LanguageProvider";
import ErrorPage from "./ErrorPage";

export default function Error403() {
  const { t } = useLanguage();
  return (
    <ErrorPage
      title="403"
      description={t("error.403.description")}
      hint={t("error.403.hint")}
    />
  );
}
