import test from "node:test";
import assert from "node:assert/strict";

import { parseLocaleMap } from "./i18n-parser.mjs";

const mockProvider = `
const en: TranslationSet = {
  "hello": "Hello"
};

const translations: Record<SupportedLocale, TranslationSet> = {
  en,
  sk: {
    ...en,
    "hello": "Ahoj"
  }
};

const unrelated = {
    nested: {
      "hello": "SHOULD_NOT_BE_PARSED"
    }
  },
next = true;
`;

const legacyRegexParse = (text, locale) => {
  const blockRegex = locale === "en"
    ? /const en: TranslationSet = \{([\s\S]*?)\n\};/
    : new RegExp(`${locale}: \\{([\\s\\S]*?)\\n  \\},`);
  const match = text.match(blockRegex);
  if (!match) return {};

  const entries = [...match[1].matchAll(/"([^"]+)":\s*"((?:\\.|[^"\\])*)"/g)];
  return Object.fromEntries(entries.map(([, key, value]) => [key, value]));
};

test("legacy regex spills last locale into unrelated code without trailing comma", () => {
  const leaked = legacyRegexParse(mockProvider, "sk");
  assert.equal(leaked.hello, "SHOULD_NOT_BE_PARSED");
});

test("parseLocaleMap stops at end of sk locale object", () => {
  const parsed = parseLocaleMap(mockProvider, "sk");
  assert.deepEqual(parsed, { hello: "Ahoj" });
});
