export const SUPPORTED_LANGUAGE_TAGS = ["en-US", "cs-CZ", "sk-SK", "pl-PL", "de-DE"] as const;
export type SupportedLanguageTag = (typeof SUPPORTED_LANGUAGE_TAGS)[number];

export const LANGUAGE_STORAGE_KEY = "astraid.lang";

const neutralMap: Record<string, SupportedLanguageTag> = {
  en: "en-US",
  cs: "cs-CZ",
  sk: "sk-SK",
  pl: "pl-PL",
  de: "de-DE"
};

export const normalizeLanguageTag = (input?: string | null): SupportedLanguageTag => {
  if (!input) return "en-US";
  const normalized = input.trim();
  const direct = SUPPORTED_LANGUAGE_TAGS.find((tag) => tag.toLowerCase() === normalized.toLowerCase());
  if (direct) return direct;

  const neutral = normalized.split("-")[0]?.toLowerCase();
  return neutralMap[neutral] ?? "en-US";
};

export const getStoredLanguageTag = (): SupportedLanguageTag | null => {
  try {
    const value = localStorage.getItem(LANGUAGE_STORAGE_KEY);
    return value ? normalizeLanguageTag(value) : null;
  } catch {
    return null;
  }
};

export const setPreferredLanguageTag = (tag: string) => {
  const normalized = normalizeLanguageTag(tag);
  try {
    localStorage.setItem(LANGUAGE_STORAGE_KEY, normalized);
  } catch {
    // ignore
  }
  return normalized;
};

export const getPreferredLanguageTag = (): SupportedLanguageTag => {
  const stored = getStoredLanguageTag();
  if (stored) return stored;

  if (typeof navigator !== "undefined") {
    const fromLanguages = (navigator.languages ?? []).map((entry) => normalizeLanguageTag(entry));
    if (fromLanguages.length > 0) return fromLanguages[0];
    return normalizeLanguageTag(navigator.language);
  }

  return "en-US";
};
