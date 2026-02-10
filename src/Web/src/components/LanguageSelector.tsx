import { useState } from "react";
import { setLanguagePreference } from "../api/authServer";
import { SUPPORTED_LANGUAGE_TAGS, getPreferredLanguageTag, setPreferredLanguageTag } from "../i18n/language";

type Props = {
  authenticated?: boolean;
  compact?: boolean;
};

const getLanguageIcon = (tag: string) => {
  const language = tag.toLowerCase();

  if (language.startsWith("cs")) return "🇨🇿";
  if (language.startsWith("sk")) return "🇸🇰";
  if (language.startsWith("en")) return "🇬🇧";
  if (language.startsWith("de")) return "🇩🇪";
  if (language.startsWith("fr")) return "🇫🇷";

  return "🌐";
};

export default function LanguageSelector({ authenticated = false, compact = false }: Props) {
  const [value, setValue] = useState(getPreferredLanguageTag());

  const onChange = async (next: string) => {
    const normalized = setPreferredLanguageTag(next);
    setValue(normalized);

    if (authenticated) {
      try {
        await setLanguagePreference(normalized);
      } catch {
        // preference persistence is best-effort for UX only
      }
    }
  };

  return (
    <label className={`flex items-center gap-2 text-xs text-slate-300 ${compact ? "" : "mt-2"}`}>
      <span>Language</span>
      <select
        className="rounded border border-slate-700 bg-slate-950 px-2 py-1 text-xs text-slate-100"
        value={value}
        onChange={(event) => void onChange(event.target.value)}
      >
        {SUPPORTED_LANGUAGE_TAGS.map((tag) => (
          <option key={tag} value={tag}>{`${getLanguageIcon(tag)} ${tag}`}</option>
        ))}
      </select>
    </label>
  );
}
