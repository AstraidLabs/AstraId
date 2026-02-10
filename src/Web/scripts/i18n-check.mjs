import { readdirSync, readFileSync, writeFileSync } from "node:fs";
import { resolve, join } from "node:path";
import { execSync } from "node:child_process";

const root = resolve(process.cwd());
const srcRoot = resolve(root, "src");
const providerPath = resolve(srcRoot, "i18n/LanguageProvider.tsx");
const provider = readFileSync(providerPath, "utf8");

const getCanonicalKeys = (text) => [...new Set([...text.matchAll(/\|\s*"([^"]+)"/g)].map((match) => match[1]))];

const parseLocaleMap = (text, locale) => {
  const blockRegex = locale === "en"
    ? /const en: TranslationSet = \{([\s\S]*?)\n\};/
    : new RegExp(`${locale}: \\{([\\s\\S]*?)\\n  \\},`);
  const match = text.match(blockRegex);
  if (!match) return {};

  const entries = [...match[1].matchAll(/"([^"]+)":\s*"((?:\\.|[^"\\])*)"/g)];
  const map = {};
  for (const [, key, value] of entries) {
    map[key] = value.replace(/\\"/g, '"').replace(/\\n/g, "\n");
  }
  return map;
};

const evaluateCoverage = (text) => {
  const canonicalKeys = getCanonicalKeys(text);
  const locales = ["en", "cs", "de", "pl", "sk"];
  const localeMaps = Object.fromEntries(locales.map((locale) => [locale, parseLocaleMap(text, locale)]));
  const en = localeMaps.en;
  const coverage = locales.map((locale) => {
    const map = localeMaps[locale];
    const missing = canonicalKeys.filter((key) => map[key] === undefined);
    const empty = canonicalKeys.filter((key) => (map[key] ?? "").trim() === "");
    const untranslated = locale === "en"
      ? []
      : canonicalKeys.filter((key) => map[key] !== undefined && map[key] === en[key]);
    return { locale, missing, empty, untranslated };
  });

  return { canonicalKeys, coverage };
};

const listFiles = (dir) => {
  const items = readdirSync(dir, { withFileTypes: true });
  const files = [];
  for (const item of items) {
    const full = join(dir, item.name);
    if (item.isDirectory()) files.push(...listFiles(full));
    else if (full.endsWith(".ts") || full.endsWith(".tsx")) files.push(full);
  }
  return files;
};

const usedKeys = new Set();
const usagePatterns = [/\bt\(\s*["'`]([^"'`]+)["'`]\s*[),]/g, /i18nKey\s*=\s*["'`]([^"'`]+)["'`]/g];
for (const file of listFiles(srcRoot)) {
  const content = readFileSync(file, "utf8");
  for (const pattern of usagePatterns) {
    for (const match of content.matchAll(pattern)) {
      if (match[1]?.includes(".")) usedKeys.add(match[1]);
    }
  }
}

const beforeText = (() => {
  try {
    return execSync("git show HEAD:src/Web/src/i18n/LanguageProvider.tsx", { cwd: resolve(root, "..") }).toString("utf8");
  } catch {
    return null;
  }
})();

const before = beforeText ? evaluateCoverage(beforeText) : null;
const after = evaluateCoverage(provider);

for (const item of after.coverage) {
  console.log(`${item.locale}: missing=${item.missing.length}, empty=${item.empty.length}, untranslated=${item.untranslated.length}`);
}

if (process.argv.includes("--report")) {
  const lines = [
    "# Web i18n Coverage Report",
    "",
    "## Discovery",
    "- i18n initialization: custom `LanguageProvider` + `useLanguage` hook in `src/i18n/LanguageProvider.tsx`.",
    "- Fallback behavior: locale normalization defaults to English (`DEFAULT_LOCALE`), and translation lookup also falls back to English.",
    "- Supported locales: en, cs, de, pl, sk.",
    "",
    "## Key Coverage",
    `- Canonical translation keys (from typed \`TranslationKey\`): **${after.canonicalKeys.length}**.`,
    `- Keys referenced by UI calls (\`t(...)\`, \`i18nKey\`): **${usedKeys.size}**.`,
    "",
    "## Before vs After",
    "| Locale | Missing (before) | Untranslated (before) | Missing (after) | Untranslated (after) |",
    "| --- | ---: | ---: | ---: | ---: |",
    ...after.coverage.map((item) => {
      const b = before?.coverage.find((entry) => entry.locale === item.locale);
      return `| ${item.locale} | ${b?.missing.length ?? "n/a"} | ${b?.untranslated.length ?? "n/a"} | ${item.missing.length} | ${item.untranslated.length} |`;
    }),
    "",
    "## Top Areas",
    `- login: ${after.canonicalKeys.filter((key) => key.startsWith("login.")).length} keys`,
    `- register: ${after.canonicalKeys.filter((key) => key.startsWith("register.")).length} keys`,
    `- auth/common: ${after.canonicalKeys.filter((key) => key.startsWith("auth.") || key.startsWith("language.")).length} keys`,
    "",
    "## Runtime Notes",
    "- Missing keys will not crash runtime due to English fallback.",
    "- `i18n:check` fails when any locale has missing, empty, or untranslated values versus English."
  ];

  writeFileSync(resolve(root, "docs/i18n-coverage.md"), `${lines.join("\n")}\n`);
}

const hasIssues = after.coverage.some((item) => item.missing.length > 0 || item.empty.length > 0 || item.untranslated.length > 0);
if (hasIssues) process.exit(1);
