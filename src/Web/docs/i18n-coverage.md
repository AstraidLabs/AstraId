# Web i18n Coverage Report

## Discovery
- i18n initialization: custom `LanguageProvider` + `useLanguage` hook in `src/i18n/LanguageProvider.tsx`.
- Fallback behavior: locale normalization defaults to English (`DEFAULT_LOCALE`), and translation lookup also falls back to English.
- Supported locales: en, cs, de, pl, sk.

## Key Coverage
- Canonical translation keys (from typed `TranslationKey`): **32**.
- Keys referenced by UI calls (`t(...)`, `i18nKey`): **32**.

## Before vs After
| Locale | Missing (before) | Untranslated (before) | Missing (after) | Untranslated (after) |
| --- | ---: | ---: | ---: | ---: |
| en | 0 | 0 | 0 | 0 |
| cs | 25 | 0 | 0 | 0 |
| de | 25 | 0 | 0 | 0 |
| pl | 25 | 0 | 0 | 0 |
| sk | 25 | 0 | 0 | 0 |

## Top Areas
- login: 13 keys
- register: 16 keys
- auth/common: 3 keys

## Runtime Notes
- Missing keys will not crash runtime due to English fallback.
- `i18n:check` fails when any locale has missing, empty, or untranslated values versus English.
