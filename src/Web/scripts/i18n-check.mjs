import { readdirSync, readFileSync, writeFileSync } from "node:fs";
import { resolve, join } from "node:path";
import { execSync } from "node:child_process";
import { evaluateCoverage } from "./i18n-parser.mjs";

const root = resolve(process.cwd());
const srcRoot = resolve(root, "src");
const providerPath = resolve(srcRoot, "i18n/LanguageProvider.tsx");
const provider = readFileSync(providerPath, "utf8");

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
