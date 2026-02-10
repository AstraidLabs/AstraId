import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { getPreferredLocale, normalizeLocale, setPreferredLocale, toLanguageTag, type SupportedLocale } from "./language";

type TranslationKey =
  | "language.label"
  | "auth.backToHome"
  | "auth.backToHomeAria"
  | "login.metaTitle"
  | "login.metaDescription"
  | "login.cardTitle"
  | "login.cardDescription"
  | "login.secureAccess"
  | "login.emailOrUsername"
  | "login.password"
  | "login.forgotPassword"
  | "login.hidePassword"
  | "login.showPassword"
  | "login.submit"
  | "login.submitting"
  | "login.createAccount"
  | "register.metaTitle"
  | "register.metaDescription"
  | "register.cardTitle"
  | "register.cardDescription"
  | "register.enterpriseOnboarding"
  | "register.email"
  | "register.password"
  | "register.confirmPassword"
  | "register.passwordHint"
  | "register.hidePassword"
  | "register.showPassword"
  | "register.hideConfirmedPassword"
  | "register.showConfirmedPassword"
  | "register.submit"
  | "register.submitting"
  | "register.signIn";

type TranslationSet = Record<TranslationKey, string>;

const en: TranslationSet = {
  "language.label": "Language",
  "auth.backToHome": "Back to home",
  "auth.backToHomeAria": "Back to home",
  "login.metaTitle": "AstraId | Sign in",
  "login.metaDescription": "Sign in to AstraId to securely access your account and enterprise identity features.",
  "login.cardTitle": "Sign in",
  "login.cardDescription": "Access your AstraId account securely.",
  "login.secureAccess": "Secure access",
  "login.emailOrUsername": "Email or username",
  "login.password": "Password",
  "login.forgotPassword": "Forgot password?",
  "login.hidePassword": "Hide password",
  "login.showPassword": "Show password",
  "login.submit": "Sign in",
  "login.submitting": "Signing in...",
  "login.createAccount": "Create account",
  "register.metaTitle": "AstraId | Create account",
  "register.metaDescription": "Create a new AstraId account with secure registration and enterprise-grade protection.",
  "register.cardTitle": "Create account",
  "register.cardDescription": "Set up your AstraId account.",
  "register.enterpriseOnboarding": "Enterprise onboarding",
  "register.email": "Email",
  "register.password": "Password",
  "register.confirmPassword": "Confirm password",
  "register.passwordHint": "Use at least 8 characters with a mix of letters and numbers.",
  "register.hidePassword": "Hide password",
  "register.showPassword": "Show password",
  "register.hideConfirmedPassword": "Hide confirmed password",
  "register.showConfirmedPassword": "Show confirmed password",
  "register.submit": "Create account",
  "register.submitting": "Creating account...",
  "register.signIn": "Already have an account? Sign in"
};

const translations: Record<SupportedLocale, TranslationSet> = {
  en,
  cs: {
    ...en,
    "language.label": "Jazyk",
    "auth.backToHome": "Zpět na hlavní stránku",
    "auth.backToHomeAria": "Zpět na hlavní stránku",
    "login.cardTitle": "Přihlášení",
    "login.submit": "Přihlásit se",
    "register.cardTitle": "Vytvořit účet",
    "register.submit": "Vytvořit účet"
  },
  de: {
    ...en,
    "language.label": "Sprache",
    "auth.backToHome": "Zur Startseite",
    "auth.backToHomeAria": "Zur Startseite",
    "login.cardTitle": "Anmelden",
    "login.submit": "Anmelden",
    "register.cardTitle": "Konto erstellen",
    "register.submit": "Konto erstellen"
  },
  pl: {
    ...en,
    "language.label": "Język",
    "auth.backToHome": "Powrót do strony głównej",
    "auth.backToHomeAria": "Powrót do strony głównej",
    "login.cardTitle": "Zaloguj się",
    "login.submit": "Zaloguj się",
    "register.cardTitle": "Utwórz konto",
    "register.submit": "Utwórz konto"
  },
  sk: {
    ...en,
    "language.label": "Jazyk",
    "auth.backToHome": "Späť na hlavnú stránku",
    "auth.backToHomeAria": "Späť na hlavnú stránku",
    "login.cardTitle": "Prihlásenie",
    "login.submit": "Prihlásiť sa",
    "register.cardTitle": "Vytvoriť účet",
    "register.submit": "Vytvoriť účet"
  }
};

type LanguageContextValue = {
  locale: SupportedLocale;
  setLocale: (locale: string) => void;
  t: (key: TranslationKey) => string;
};

const LanguageContext = createContext<LanguageContextValue | null>(null);

export const LanguageProvider = ({ children }: { children: ReactNode }) => {
  const [locale, setLocaleState] = useState<SupportedLocale>(() => getPreferredLocale());

  const setLocale = useCallback((nextLocale: string) => {
    const normalized = setPreferredLocale(nextLocale);
    setLocaleState(normalized);
  }, []);

  useEffect(() => {
    document.documentElement.lang = toLanguageTag(locale);
  }, [locale]);

  useEffect(() => {
    const onStorage = (event: StorageEvent) => {
      if (event.key !== null && event.key !== "astraid.locale" && event.key !== "astraid.lang") {
        return;
      }
      setLocaleState(normalizeLocale(event.newValue));
    };

    window.addEventListener("storage", onStorage);
    return () => window.removeEventListener("storage", onStorage);
  }, []);

  const value = useMemo<LanguageContextValue>(() => ({
    locale,
    setLocale,
    t: (key) => translations[locale][key] ?? en[key]
  }), [locale, setLocale]);

  return <LanguageContext.Provider value={value}>{children}</LanguageContext.Provider>;
};

export const useLanguage = () => {
  const context = useContext(LanguageContext);
  if (!context) {
    throw new Error("useLanguage must be used within LanguageProvider");
  }
  return context;
};
