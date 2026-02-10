export const SUPPORTED_LOCALES = ["en", "cs", "de", "pl", "sk"] as const;
export type SupportedLocale = (typeof SUPPORTED_LOCALES)[number];

export const LOCALE_STORAGE_KEY = "astraid.locale";
const LEGACY_LANGUAGE_STORAGE_KEY = "astraid.lang";

const localeToTagMap: Record<SupportedLocale, string> = {
  en: "en-US",
  cs: "cs-CZ",
  de: "de-DE",
  pl: "pl-PL",
  sk: "sk-SK"
};

const neutralMap: Record<string, SupportedLocale> = {
  en: "en",
  cs: "cs",
  de: "de",
  pl: "pl",
  sk: "sk"
};

export const normalizeLocale = (input?: string | null): SupportedLocale => {
  if (!input) return "en";
  const normalized = input.trim().toLowerCase();

  const direct = SUPPORTED_LOCALES.find((locale) => locale === normalized);
  if (direct) return direct;

  const neutral = normalized.split("-")[0];
  return neutralMap[neutral] ?? "en";
};

export const toLanguageTag = (locale: SupportedLocale): string => localeToTagMap[locale];

export const getStoredLocale = (): SupportedLocale | null => {
  try {
    const value = localStorage.getItem(LOCALE_STORAGE_KEY) ?? localStorage.getItem(LEGACY_LANGUAGE_STORAGE_KEY);
    return value ? normalizeLocale(value) : null;
  } catch {
    return null;
  }
};

export const setPreferredLocale = (locale: string): SupportedLocale => {
  const normalized = normalizeLocale(locale);
  try {
    localStorage.setItem(LOCALE_STORAGE_KEY, normalized);
    localStorage.removeItem(LEGACY_LANGUAGE_STORAGE_KEY);
  } catch {
    // ignore
  }
  return normalized;
};

export const getPreferredLocale = (): SupportedLocale => {
  const stored = getStoredLocale();
  if (stored) return stored;

  if (typeof navigator !== "undefined") {
    const fromLanguages = (navigator.languages ?? []).map((entry) => normalizeLocale(entry));
    if (fromLanguages.length > 0) return fromLanguages[0];
    return normalizeLocale(navigator.language);
  }

  return "en";
};

export const getPreferredLanguageTag = (): string => toLanguageTag(getPreferredLocale());
