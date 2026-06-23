#!/usr/bin/env node
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const scriptDir = path.dirname(fileURLToPath(import.meta.url));
const rootDir = path.resolve(scriptDir, "..");
const modDir = process.env.MOD_DIR
  ? path.resolve(process.env.MOD_DIR)
  : path.join(rootDir, "workspace/mod");
const cardsDir = path.join(modDir, "ModCode/Cards");

const KEYWORD_PREFIXES = {
  eng: {
    Exhaust: "Exhaust.\n",
    Retain: "Retain.\n",
    Innate: "Innate.\n",
    Ethereal: "Ethereal.\n",
  },
  zhs: {
    Exhaust: "消耗，\n",
    Retain: "保留，\n",
    Innate: "固有，\n",
    Ethereal: "虚无，\n",
  },
};

const SPECIAL_FIXES = {
  eng: {
    INNER_POTENTIAL: {
      description: {
        from: "{IfUpgraded:show:Retain.\n|}Exhaust.\n",
        to: "",
      },
      upgradeDescription: {
        from: "Retain.\nExhaust.\n",
        to: "",
      },
    },
    STAR_SHOWER: {
      description: {
        from: "{IfUpgraded:show:Choose 1 card from your Discard pile and add it to your hand.|Exhaust.\nChoose 1 card from your Discard pile and add it to your hand.}",
        to: "Choose 1 card from your Discard pile and add it to your hand.",
      },
    },
  },
  zhs: {
    INNER_POTENTIAL: {
      description: {
        from: "{IfUpgraded:show:保留，\n|}消耗，\n",
        to: "",
      },
      upgradeDescription: {
        from: "保留，\n消耗，\n",
        to: "",
      },
    },
    STAR_SHOWER: {
      description: {
        from: "{IfUpgraded:show:从弃牌堆中选择 1 张牌加入手牌|消耗，\n从弃牌堆中选择 1 张牌加入手牌}。",
        to: "从弃牌堆中选择 1 张牌加入手牌。",
      },
    },
  },
};

function toCardId(className) {
  return className
    .replace(/([A-Z]+)([A-Z][a-z])/g, "$1_$2")
    .replace(/([a-z0-9])([A-Z])/g, "$1_$2")
    .toUpperCase();
}

function extractBlock(source, openingBraceIndex) {
  let depth = 0;
  for (let index = openingBraceIndex; index < source.length; index += 1) {
    const char = source[index];
    if (char === "{") {
      depth += 1;
    } else if (char === "}") {
      depth -= 1;
      if (depth === 0) {
        return source.slice(openingBraceIndex, index + 1);
      }
    }
  }
  throw new Error(`Unclosed block near offset ${openingBraceIndex}`);
}

function parseCanonicalKeywords(body) {
  const match = body.match(/CanonicalKeywords\s*=>\s*\[([^\]]*)\]/s);
  if (!match) {
    return [];
  }
  return [...match[1].matchAll(/CardKeyword\.(\w+)/g)].map((keyword) => keyword[1]);
}

function parseCards() {
  const cards = new Map();
  const classRegex = /public\s+sealed\s+class\s+(\w+)\s*\(\)\s*:\s*(\w+(?:<[^>]+>)?)\s*\(/g;

  for (const fileName of fs.readdirSync(cardsDir).filter((name) => name.endsWith(".cs"))) {
    const source = fs.readFileSync(path.join(cardsDir, fileName), "utf8");
    for (const match of source.matchAll(classRegex)) {
      const className = match[1];
      const baseType = match[2];
      const openBrace = source.indexOf("{", match.index + match[0].length);
      const body = extractBlock(source, openBrace);
      const canonicalKeywords = parseCanonicalKeywords(body);
      if (baseType.startsWith("ChargedModCard")) {
        canonicalKeywords.push("Exhaust", "Retain");
      }
      const upgradeKeywords = [...body.matchAll(/AddKeyword\s*\(\s*CardKeyword\.(\w+)\s*\)/g)].map((keyword) => keyword[1]);
      const removeKeywords = new Set([...body.matchAll(/RemoveKeyword\s*\(\s*CardKeyword\.(\w+)\s*\)/g)].map((keyword) => keyword[1]));
      const baseKeywords = [...new Set(canonicalKeywords)].sort();
      const upgradedKeywords = [...new Set([...baseKeywords, ...upgradeKeywords].filter((keyword) => !removeKeywords.has(keyword)))].sort();
      cards.set(toCardId(className), { baseKeywords, upgradedKeywords });
    }
  }

  return cards;
}

function stripKeywordPrefixes(text, keywords, lang) {
  if (!text) {
    return text;
  }

  let result = text;
  let changed = true;
  while (changed) {
    changed = false;
    for (const keyword of keywords) {
      const prefix = KEYWORD_PREFIXES[lang][keyword];
      if (prefix && result.startsWith(prefix)) {
        result = result.slice(prefix.length);
        changed = true;
      }
    }
  }
  return result;
}

function cardIdFromLocalizationKey(key) {
  const match = key.match(/(?:^|\.)([A-Z][A-Z0-9_]+)\.(description|upgradeDescription)$/);
  return match?.[1] ?? null;
}

function fixLocalizationFile(relativePath, lang, cards) {
  const filePath = path.join(modDir, relativePath);
  const localization = JSON.parse(fs.readFileSync(filePath, "utf8"));
  let changedCount = 0;

  for (const [key, value] of Object.entries(localization)) {
    if (typeof value !== "string") {
      continue;
    }

    const cardId = cardIdFromLocalizationKey(key);
    if (!cardId || !cards.has(cardId)) {
      continue;
    }

    const field = key.endsWith(".upgradeDescription") ? "upgradeDescription" : "description";
    const card = cards.get(cardId);
    const keywords = field === "upgradeDescription" ? card.upgradedKeywords : card.baseKeywords;
    let nextValue = value;

    const special = SPECIAL_FIXES[lang]?.[cardId]?.[field];
    if (special && nextValue.includes(special.from)) {
      nextValue = nextValue.replace(special.from, special.to);
    } else {
      nextValue = stripKeywordPrefixes(nextValue, keywords, lang);
    }

    if (nextValue !== value) {
      localization[key] = nextValue;
      changedCount += 1;
    }
  }

  fs.writeFileSync(filePath, `${JSON.stringify(localization, null, 2)}\n`);
  return changedCount;
}

const cards = parseCards();
const engChanges = fixLocalizationFile("mod/localization/eng/cards.json", "eng", cards);
const zhsChanges = fixLocalizationFile("mod/localization/zhs/cards.json", "zhs", cards);
console.log(`Updated ${engChanges} eng entries and ${zhsChanges} zhs entries.`);
