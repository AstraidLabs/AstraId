const TRANSLATION_ENTRY_REGEX = /"([^"]+)":\s*"((?:\\.|[^"\\])*)"/g;

const isEscaped = (text, index) => {
  let slashCount = 0;
  for (let cursor = index - 1; cursor >= 0 && text[cursor] === "\\"; cursor -= 1) {
    slashCount += 1;
  }
  return slashCount % 2 === 1;
};

const findMatchingBrace = (text, openIndex) => {
  let depth = 0;
  let quote = null;
  for (let index = openIndex; index < text.length; index += 1) {
    const char = text[index];

    if (quote) {
      if (char === quote && !isEscaped(text, index)) quote = null;
      continue;
    }

    if (char === '"' || char === "'" || char === "`") {
      quote = char;
      continue;
    }

    if (char === "{") depth += 1;
    else if (char === "}") {
      depth -= 1;
      if (depth === 0) return index;
    }
  }

  return -1;
};

const extractConstObjectBlock = (text, constName) => {
  const declaration = new RegExp(`const\\s+${constName}\\b[^=]*=`, "m");
  const declarationMatch = declaration.exec(text);
  if (!declarationMatch) return "";

  const objectStart = text.indexOf("{", declarationMatch.index + declarationMatch[0].length);
  if (objectStart < 0) return "";

  const objectEnd = findMatchingBrace(text, objectStart);
  if (objectEnd < 0) return "";

  return text.slice(objectStart + 1, objectEnd);
};

const extractLocaleObjectBlock = (translationsBlock, locale) => {
  const localePattern = new RegExp(`(^|\\n)\\s*${locale}\\s*:\\s*\\{`, "m");
  const localeMatch = localePattern.exec(translationsBlock);
  if (!localeMatch) return "";

  const objectStart = translationsBlock.indexOf("{", localeMatch.index + localeMatch[0].length - 1);
  if (objectStart < 0) return "";

  const objectEnd = findMatchingBrace(translationsBlock, objectStart);
  if (objectEnd < 0) return "";

  return translationsBlock.slice(objectStart + 1, objectEnd);
};

const parseEntries = (block) => {
  const map = {};
  for (const [, key, value] of block.matchAll(TRANSLATION_ENTRY_REGEX)) {
    map[key] = value.replace(/\\"/g, '"').replace(/\\n/g, "\n");
  }
  return map;
};

export const getCanonicalKeys = (text) => [...new Set([...text.matchAll(/\|\s*"([^"]+)"/g)].map((match) => match[1]))];

export const parseLocaleMap = (text, locale) => {
  if (locale === "en") {
    return parseEntries(extractConstObjectBlock(text, "en"));
  }

  // Regex-only parsing previously relied on a trailing `},` terminator and could
  // overrun the final locale block (for example `sk`) into unrelated code.
  const translationsBlock = extractConstObjectBlock(text, "translations");
  const localeBlock = extractLocaleObjectBlock(translationsBlock, locale);

  if (/\b(import|export)\b/.test(localeBlock)) {
    throw new Error(`Unexpected token while parsing locale ${locale}`);
  }

  return parseEntries(localeBlock);
};

export const evaluateCoverage = (text, locales = ["en", "cs", "de", "pl", "sk"]) => {
  const canonicalKeys = getCanonicalKeys(text);
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
