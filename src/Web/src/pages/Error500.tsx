import { useLanguage } from "../i18n/LanguageProvider";
import ErrorPage from "./ErrorPage";

export default function Error500() {
  const { t } = useLanguage();
  return (
    <ErrorPage
      title="500"
      description={t("error.500.description")}
      hint={t("error.500.hint")}
    />
  );
}
