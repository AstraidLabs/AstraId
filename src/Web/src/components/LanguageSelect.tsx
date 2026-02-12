import { Languages } from "lucide-react";
import { useId } from "react";
import { setLanguagePreference } from "../api/authServer";
import { useLanguage } from "../i18n/LanguageProvider";
import { SUPPORTED_LOCALES } from "../i18n/language";

const localeLabels: Record<(typeof SUPPORTED_LOCALES)[number], string> = {
  en: "English",
  cs: "Čeština",
  de: "Deutsch",
  pl: "Polski",
  sk: "Slovenčina"
};

type Props = {
  authenticated?: boolean;
  compact?: boolean;
};

export default function LanguageSelect({ authenticated = false, compact = false }: Props) {
  const { locale, setLocale, t } = useLanguage();
  const selectId = useId();

  const onChange = async (next: string) => {
    setLocale(next);

    if (authenticated) {
      try {
        await setLanguagePreference(next);
      } catch {
        // preference persistence is best-effort for UX only
      }
    }
  };

  return (
    <div className={`flex flex-wrap items-center gap-2 ${compact ? "" : "mt-2"}`}>
      <label htmlFor={selectId} className="inline-flex items-center gap-2 whitespace-nowrap text-xs text-slate-300">
        <Languages className="h-3.5 w-3.5" aria-hidden="true" />
        <span>{t("language.label")}</span>
      </label>
      <select
        id={selectId}
        className="min-w-28 rounded border border-slate-700 bg-slate-950 px-2 py-1 text-xs text-slate-100 outline-none transition focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/40"
        aria-label={t("language.label")}
        value={locale}
        onChange={(event) => void onChange(event.target.value)}
      >
        {SUPPORTED_LOCALES.map((localeValue) => (
          <option key={localeValue} value={localeValue}>
            {localeLabels[localeValue]}
          </option>
        ))}
      </select>
    </div>
  );
}
