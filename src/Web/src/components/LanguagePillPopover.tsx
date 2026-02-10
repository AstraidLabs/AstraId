import { Check, ChevronDown, Languages, Search } from "lucide-react";
import { useEffect, useId, useMemo, useRef, useState, type KeyboardEvent } from "react";
import { useLanguage } from "../i18n/LanguageProvider";
import { SUPPORTED_LOCALES, type SupportedLocale } from "../i18n/language";

const localeMetadata: Record<SupportedLocale, { nativeName: string; code: string; flag: string }> = {
  en: { nativeName: "English", code: "en-US", flag: "🇺🇸" },
  cs: { nativeName: "Čeština", code: "cs-CZ", flag: "🇨🇿" },
  sk: { nativeName: "Slovenčina", code: "sk-SK", flag: "🇸🇰" },
  de: { nativeName: "Deutsch", code: "de-DE", flag: "🇩🇪" },
  pl: { nativeName: "Polski", code: "pl-PL", flag: "🇵🇱" }
};

type Placement = "bottom-start" | "bottom-end";

type LanguagePillPopoverProps = {
  value: SupportedLocale;
  onChange: (locale: SupportedLocale) => void;
  compact?: boolean;
  placement?: Placement;
};

export default function LanguagePillPopover({
  value,
  onChange,
  compact = false,
  placement = "bottom-start"
}: LanguagePillPopoverProps) {
  const { t } = useLanguage();
  const [isOpen, setIsOpen] = useState(false);
  const [search, setSearch] = useState("");
  const [activeIndex, setActiveIndex] = useState(0);

  const containerRef = useRef<HTMLDivElement>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const searchRef = useRef<HTMLInputElement>(null);

  const triggerId = useId();
  const popoverId = useId();
  const listboxId = useId();

  const locales = useMemo(() => SUPPORTED_LOCALES.map((locale) => ({ locale, ...localeMetadata[locale] })), []);

  const filteredLocales = useMemo(() => {
    const term = search.trim().toLowerCase();
    if (!term) return locales;

    return locales.filter(({ nativeName, code }) =>
      nativeName.toLowerCase().includes(term) || code.toLowerCase().includes(term)
    );
  }, [locales, search]);

  useEffect(() => {
    if (!isOpen) {
      setSearch("");
      setActiveIndex(0);
      return;
    }

    const selectedIndex = filteredLocales.findIndex((entry) => entry.locale === value);
    setActiveIndex(selectedIndex >= 0 ? selectedIndex : 0);
  }, [filteredLocales, isOpen, value]);

  useEffect(() => {
    if (!isOpen) return;

    const onPointerDown = (event: MouseEvent | TouchEvent) => {
      const target = event.target as Node;
      if (!containerRef.current?.contains(target)) {
        setIsOpen(false);
      }
    };

    const onEscape = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        event.preventDefault();
        setIsOpen(false);
      }
    };

    document.addEventListener("mousedown", onPointerDown);
    document.addEventListener("touchstart", onPointerDown);
    document.addEventListener("keydown", onEscape);

    return () => {
      document.removeEventListener("mousedown", onPointerDown);
      document.removeEventListener("touchstart", onPointerDown);
      document.removeEventListener("keydown", onEscape);
    };
  }, [isOpen]);

  useEffect(() => {
    if (isOpen) {
      searchRef.current?.focus();
      return;
    }

    triggerRef.current?.focus();
  }, [isOpen]);

  const selectLocale = (locale: SupportedLocale) => {
    onChange(locale);
    setIsOpen(false);
  };

  const onListKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    if (!filteredLocales.length) return;

    if (event.key === "ArrowDown") {
      event.preventDefault();
      setActiveIndex((prev) => (prev + 1) % filteredLocales.length);
      return;
    }

    if (event.key === "ArrowUp") {
      event.preventDefault();
      setActiveIndex((prev) => (prev - 1 + filteredLocales.length) % filteredLocales.length);
      return;
    }

    if (event.key === "Enter") {
      event.preventDefault();
      const active = filteredLocales[Math.max(activeIndex, 0)];
      if (active) {
        selectLocale(active.locale);
      }
    }
  };

  const activeDescendantId = filteredLocales[activeIndex]
    ? `${listboxId}-${filteredLocales[activeIndex].locale}`
    : undefined;

  const alignmentClass = placement === "bottom-end" ? "right-0" : "left-0";

  return (
    <div ref={containerRef} className={`relative flex flex-col gap-1.5 ${compact ? "" : "mt-2"}`}>
      <label htmlFor={triggerId} className="inline-flex items-center gap-2 text-xs text-slate-300">
        <Languages className="h-3.5 w-3.5" aria-hidden="true" />
        <span>{t("language.label")}</span>
      </label>

      <button
        id={triggerId}
        ref={triggerRef}
        type="button"
        aria-haspopup="dialog"
        aria-expanded={isOpen}
        aria-controls={popoverId}
        className="inline-flex min-w-[11rem] items-center justify-between gap-2 rounded-full border border-slate-700/80 bg-slate-900/70 px-3 py-1.5 text-sm text-slate-100 shadow-lg shadow-slate-950/30 backdrop-blur-md transition hover:border-slate-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/70"
        onClick={() => setIsOpen((prev) => !prev)}
      >
        <span className="inline-flex items-center gap-2">
          <span aria-hidden="true">{localeMetadata[value].flag}</span>
          <span>{localeMetadata[value].nativeName}</span>
        </span>
        <ChevronDown
          className={`h-4 w-4 text-slate-300 transition-transform motion-reduce:transition-none ${isOpen ? "rotate-180" : ""}`}
          aria-hidden="true"
        />
      </button>

      <div
        id={popoverId}
        role="dialog"
        aria-label={t("language.popoverAria")}
        className={`absolute ${alignmentClass} top-full z-30 mt-2 w-72 rounded-2xl border border-slate-700/80 bg-slate-950/90 p-2 shadow-2xl shadow-slate-950/60 backdrop-blur-md transition duration-150 motion-reduce:transition-none ${
          isOpen
            ? "pointer-events-auto scale-100 opacity-100"
            : "pointer-events-none scale-95 opacity-0"
        }`}
        onKeyDown={onListKeyDown}
      >
        <div className="relative mb-2">
          <Search className="pointer-events-none absolute left-3 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-slate-500" aria-hidden="true" />
          <input
            ref={searchRef}
            type="text"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            className="w-full rounded-xl border border-slate-700 bg-slate-900/80 py-1.5 pl-8 pr-3 text-xs text-slate-100 outline-none transition focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/40"
            placeholder={t("language.searchPlaceholder")}
            aria-label={t("language.searchPlaceholder")}
          />
        </div>

        <ul
          id={listboxId}
          role="listbox"
          aria-label={t("language.listAria")}
          aria-activedescendant={activeDescendantId}
          className="max-h-56 overflow-y-auto rounded-xl"
          tabIndex={0}
        >
          {filteredLocales.length === 0 ? (
            <li className="px-3 py-2 text-xs text-slate-400">{t("language.noResults")}</li>
          ) : (
            filteredLocales.map((entry, index) => {
              const isSelected = entry.locale === value;
              const isActive = index === activeIndex;

              return (
                <li
                  id={`${listboxId}-${entry.locale}`}
                  key={entry.locale}
                  role="option"
                  aria-selected={isSelected}
                >
                  <button
                    type="button"
                    className={`flex w-full items-center justify-between rounded-lg px-2.5 py-2 text-left text-sm transition focus:outline-none ${
                      isActive ? "bg-indigo-500/20 text-indigo-100" : "text-slate-200 hover:bg-slate-800/80"
                    }`}
                    onMouseEnter={() => setActiveIndex(index)}
                    onClick={() => selectLocale(entry.locale)}
                  >
                    <span className="inline-flex items-center gap-2">
                      <span aria-hidden="true">{entry.flag}</span>
                      <span>{entry.nativeName}</span>
                    </span>
                    <span className="inline-flex items-center gap-2 text-xs text-slate-400">
                      {entry.code}
                      {isSelected ? <Check className="h-3.5 w-3.5 text-emerald-300" aria-hidden="true" /> : null}
                    </span>
                  </button>
                </li>
              );
            })
          )}
        </ul>
      </div>
    </div>
  );
}
