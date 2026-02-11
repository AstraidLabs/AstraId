import { useLanguage } from "../i18n/LanguageProvider";
import ErrorPage from "./ErrorPage";

export default function Error404() {
  const { t } = useLanguage();
  return (
    <ErrorPage
      title="404"
      description={t("error.404.description")}
      hint={t("error.404.hint")}
    />
  );
}
