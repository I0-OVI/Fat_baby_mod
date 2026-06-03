#!/usr/bin/env node
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const scriptDir = path.dirname(fileURLToPath(import.meta.url));
const rootDir = path.resolve(scriptDir, "..");
const cardsDir = path.join(rootDir, "workspace/mod/ModCode/Cards");
const characterDir = path.join(rootDir, "workspace/mod/ModCode/Character");
const localizationDir = path.join(rootDir, "workspace/mod/mod/localization");
const expectationsPath = path.join(rootDir, "tests/card_expectations.json");

const expectation = JSON.parse(fs.readFileSync(expectationsPath, "utf8"));
const errors = [];

function fail(message) {
  errors.push(message);
}

function readText(relativePath) {
  return fs.readFileSync(path.join(rootDir, relativePath), "utf8");
}

function listFiles(dir, suffix) {
  return fs.readdirSync(dir)
    .filter((name) => name.endsWith(suffix))
    .map((name) => path.join(dir, name));
}

function splitArgs(args) {
  return args.split(",").map((arg) => arg.trim());
}

function enumValue(text) {
  return text.split(".").at(-1).trim();
}

function toCardId(className) {
  return className
    .replace(/([A-Z]+)([A-Z][a-z])/g, "$1_$2")
    .replace(/([a-z0-9])([A-Z])/g, "$1_$2")
    .toUpperCase();
}

function numberValue(text) {
  const match = String(text).match(/-?\d+(?:\.\d+)?/);
  return match ? Number(match[0]) : NaN;
}

function sorted(value) {
  return [...value].sort();
}

function sameJson(left, right) {
  return JSON.stringify(left) === JSON.stringify(right);
}

function compareValue(cardName, label, actual, expected) {
  if (!sameJson(actual, expected)) {
    fail(`${cardName}: ${label} expected ${JSON.stringify(expected)}, got ${JSON.stringify(actual)}`);
  }
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

function addVar(vars, name, value, cardName) {
  if (Object.hasOwn(vars, name)) {
    fail(`${cardName}: duplicate dynamic var ${name}`);
  }
  vars[name] = value;
}

function parseVars(body, cardName) {
  const vars = {};

  const typedVarNames = {
    DamageVar: "Damage",
    BlockVar: "Block",
    CardsVar: "Cards",
    HealVar: "Heal"
  };

  for (const match of body.matchAll(/new\s+(DamageVar|BlockVar|CardsVar|HealVar)\s*\(\s*(-?\d+(?:\.\d+)?)m?/g)) {
    addVar(vars, typedVarNames[match[1]], numberValue(match[2]), cardName);
  }

  for (const match of body.matchAll(/new\s+DynamicVar\s*\(\s*"([^"]+)"\s*,\s*(-?\d+(?:\.\d+)?)m?/g)) {
    addVar(vars, match[1], numberValue(match[2]), cardName);
  }

  for (const match of body.matchAll(/new\s+PowerVar<(\w+)>\s*\(\s*(-?\d+(?:\.\d+)?)m?/g)) {
    addVar(vars, match[1], numberValue(match[2]), cardName);
  }

  return vars;
}

function parseCanonicalKeywords(body) {
  const match = body.match(/CanonicalKeywords\s*=>\s*\[([^\]]*)\]/s);
  if (!match) {
    return [];
  }

  return sorted([...match[1].matchAll(/CardKeyword\.(\w+)/g)].map((keyword) => keyword[1]));
}

function parseResultPileType(body) {
  if (/AfterCardPlayedLate[\s\S]*CardPileCmd\.Add\s*\(\s*this\s*,\s*PileType\.Hand/.test(body)) {
    return "Hand";
  }

  const methodIndex = body.indexOf("GetResultPileTypeForCardPlay");
  if (methodIndex < 0) {
    return "Default";
  }

  const openBrace = body.indexOf("{", methodIndex);
  if (openBrace < 0) {
    return "Custom";
  }

  const methodBody = extractBlock(body, openBrace);
  const returnPileTypes = new Set([...methodBody.matchAll(/return\s+PileType\.(\w+)/g)].map((match) => match[1]));
  if (returnPileTypes.size === 1) {
    return [...returnPileTypes][0];
  }

  if (returnPileTypes.has("Hand")) {
    return "Hand";
  }

  return returnPileTypes.size > 0 ? sorted(returnPileTypes).join("|") : "Custom";
}

function parseGeneratesSelfCopy(body, className) {
  const escapedClassName = className.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
  const createsSelfByModel = new RegExp(`ModelDb\\.Card<${escapedClassName}>\\s*\\(`).test(body);
  const createsSelfByGeneric = new RegExp(`CreateCard<${escapedClassName}>\\s*\\(`).test(body);
  return (createsSelfByModel || createsSelfByGeneric) && /AddGeneratedCards?ToCombat/.test(body);
}

function parseCards() {
  const cards = new Map();
  const classRegex = /public\s+sealed\s+class\s+(\w+)\s*\(\)\s*:\s*(ModCard|ChargedModCard<[^>]+>)\s*\(([^)]*)\)/g;

  for (const file of listFiles(cardsDir, ".cs")) {
    const source = fs.readFileSync(file, "utf8");
    for (const match of source.matchAll(classRegex)) {
      const className = match[1];
      const baseType = match[2];
      const args = splitArgs(match[3]);
      const openBrace = source.indexOf("{", match.index + match[0].length);
      const body = extractBlock(source, openBrace);
      const vars = parseVars(body, className);
      const cost = numberValue(args[0]);
      let upgradedCost = cost;
      const upgradedVars = { ...vars };

      for (const upgrade of body.matchAll(/DynamicVars\.(Damage|Block|Cards|Heal|Weak)\.UpgradeValueBy\s*\(\s*(-?\d+(?:\.\d+)?)m?\s*\)/g)) {
        const propertyAliases = {
          Damage: "Damage",
          Block: "Block",
          Cards: "Cards",
          Heal: "Heal",
          Weak: "WeakPower"
        };
        const varName = propertyAliases[upgrade[1]];
        upgradedVars[varName] = (upgradedVars[varName] ?? 0) + numberValue(upgrade[2]);
      }

      for (const upgrade of body.matchAll(/DynamicVars\["([^"]+)"\]\.UpgradeValueBy\s*\(\s*(-?\d+(?:\.\d+)?)m?\s*\)/g)) {
        upgradedVars[upgrade[1]] = (upgradedVars[upgrade[1]] ?? 0) + numberValue(upgrade[2]);
      }

      for (const upgrade of body.matchAll(/EnergyCost\.UpgradeBy\s*\(\s*(-?\d+)\s*\)/g)) {
        upgradedCost += numberValue(upgrade[1]);
      }

      const canonicalKeywords = parseCanonicalKeywords(body);
      if (baseType.startsWith("ChargedModCard")) {
        canonicalKeywords.push("Exhaust");
        canonicalKeywords.push("Retain");
      }
      canonicalKeywords.sort();
      const upgradeKeywords = sorted([...body.matchAll(/AddKeyword\s*\(\s*CardKeyword\.(\w+)\s*\)/g)].map((keyword) => keyword[1]));
      const upgradedKeywords = sorted(new Set([...canonicalKeywords, ...upgradeKeywords]));

      cards.set(className, {
        className,
        id: toCardId(className),
        cost,
        type: enumValue(args[1]),
        rarity: enumValue(args[2]),
        target: enumValue(args[3]),
        xCost: /HasEnergyCostX\s*=>\s*true/.test(body),
        gainsBlock: /GainsBlock\s*=>\s*true/.test(body),
        keywords: canonicalKeywords,
        vars,
        maxUpgradeLevel: numberValue(body.match(/MaxUpgradeLevel\s*=>\s*(-?\d+)/)?.[1] ?? "1"),
        upgradedCost,
        upgradedVars,
        upgradedKeywords,
        resultPile: parseResultPileType(body),
        generatesSelfCopy: parseGeneratesSelfCopy(body, className),
        hasImbalanceTip: /HoverTipFactory\.FromPower<ImbalancePower>\s*\(/.test(body),
        file: path.relative(rootDir, file)
      });
    }
  }

  return cards;
}

function parseClassListFromModelDbCalls(source) {
  return [...source.matchAll(/ModelDb\.Card<(\w+)>\s*\(/g)].map((match) => match[1]);
}

function compareSets(label, actual, expected) {
  const actualSorted = sorted(actual);
  const expectedSorted = sorted(expected);
  if (!sameJson(actualSorted, expectedSorted)) {
    const missing = expectedSorted.filter((item) => !actualSorted.includes(item));
    const extra = actualSorted.filter((item) => !expectedSorted.includes(item));
    if (missing.length > 0) {
      fail(`${label}: missing ${missing.join(", ")}`);
    }
    if (extra.length > 0) {
      fail(`${label}: unexpected ${extra.join(", ")}`);
    }
  }
}

function compareVars(cardName, label, actual, expected) {
  const actualOrdered = Object.fromEntries(Object.entries(actual).sort(([left], [right]) => left.localeCompare(right)));
  const expectedOrdered = Object.fromEntries(Object.entries(expected).sort(([left], [right]) => left.localeCompare(right)));
  compareValue(cardName, label, actualOrdered, expectedOrdered);
}

function checkCards() {
  const cards = parseCards();
  const expectedClasses = expectation.cards.map((card) => card.class);
  compareSets("ModCard classes", [...cards.keys()], expectedClasses);

  for (const expected of expectation.cards) {
    const actual = cards.get(expected.class);
    if (!actual) {
      continue;
    }

    const expectedKeywords = sorted(expected.keywords ?? []);
    const expectedUpgrade = expected.upgrade ?? {};
    const expectedUpgradeVars = { ...(expected.vars ?? {}), ...(expectedUpgrade.vars ?? {}) };
    const expectedUpgradedKeywords = sorted(new Set([...expectedKeywords, ...(expectedUpgrade.keywords ?? [])]));

    compareValue(expected.class, "id", actual.id, expected.id);
    compareValue(expected.class, "cost", actual.cost, expected.cost);
    compareValue(expected.class, "type", actual.type, expected.type);
    compareValue(expected.class, "rarity", actual.rarity, expected.rarity);
    compareValue(expected.class, "target", actual.target, expected.target);
    compareValue(expected.class, "xCost", actual.xCost, expected.xCost ?? false);
    compareValue(expected.class, "gainsBlock", actual.gainsBlock, expected.gainsBlock ?? false);
    if (Object.hasOwn(expected.vars ?? {}, "Imbalance") && !actual.hasImbalanceTip) {
      fail(`${expected.class}: cards with Imbalance must show the Imbalance/poise damage hover tip`);
    }
    compareValue(expected.class, "result pile", actual.resultPile, expected.resultPile ?? "Default");
    if (Object.hasOwn(expected, "generatesSelfCopy")) {
      compareValue(expected.class, "generates self copy", actual.generatesSelfCopy, expected.generatesSelfCopy);
    }
    if ((expected.upgradeable ?? true) && actual.maxUpgradeLevel <= 0) {
      fail(`${expected.class}: MaxUpgradeLevel is ${actual.maxUpgradeLevel}, so the card cannot be smith-upgraded`);
    }
    compareValue(expected.class, "keywords", actual.keywords, expectedKeywords);
    compareVars(expected.class, "vars", actual.vars, expected.vars ?? {});
    compareValue(expected.class, "upgraded cost", actual.upgradedCost, expectedUpgrade.cost ?? expected.cost);
    compareVars(expected.class, "upgraded vars", actual.upgradedVars, expectedUpgradeVars);
    compareValue(expected.class, "upgraded keywords", actual.upgradedKeywords, expectedUpgradedKeywords);
  }

  const poolSource = fs.readFileSync(path.join(characterDir, "ScarletCardPool.cs"), "utf8");
  const poolClasses = parseClassListFromModelDbCalls(poolSource);
  compareSets("ScarletCardPool", poolClasses, expectedClasses);

  const duplicatePoolEntries = poolClasses.filter((className, index) => poolClasses.indexOf(className) !== index);
  if (duplicatePoolEntries.length > 0) {
    fail(`ScarletCardPool: duplicate entries ${sorted(new Set(duplicatePoolEntries)).join(", ")}`);
  }

  const acolyteSource = fs.readFileSync(path.join(characterDir, "ScarletAcolyte.cs"), "utf8");
  const startingDeckClasses = parseClassListFromModelDbCalls(acolyteSource);
  const actualStartingDeck = {};
  for (const className of startingDeckClasses) {
    actualStartingDeck[className] = (actualStartingDeck[className] ?? 0) + 1;
  }
  compareVars("ScarletAcolyte", "starting deck", actualStartingDeck, expectation.startingDeck);
}

function checkChargeLifecycle() {
  const source = readText("workspace/mod/ModCode/Cards/ChargedModCard.cs");
  const relicSource = readText("workspace/mod/ModCode/Relics/ElementFlaskRelic.cs");

  if (!/InitializeChargeAtCombatStart[\s\S]*!IsInCombat[\s\S]*CardPileCmd\.Add\s*\(\s*this\s*,\s*PileType\.Exhaust/.test(source)) {
    fail("ChargedModCard: combat start must move charged cards into the exhaust pile");
  }

  if (!/InitializeChargeAtCombatStart[\s\S]*!IsInCombat/.test(source)) {
    fail("ChargedModCard: charge setup must ignore permanent deck cards and only initialize combat card instances");
  }

  if (!/AfterCardPlayed[\s\S]*_remainingCharge--[\s\S]*ReturnToHandFromExhaust\s*\(/.test(source)) {
    fail("ChargedModCard: charge countdown reaching 0 must return the card from exhaust to hand immediately");
  }

  if (!/ReturnToHandFromExhaust[\s\S]*Pile\?\.Type\s*!=\s*PileType\.Exhaust[\s\S]*CardPileCmd\.Add\s*\(\s*this\s*,\s*PileType\.Hand/.test(source)) {
    fail("ChargedModCard: return-to-hand must move the same card from exhaust to hand");
  }

  if (!/AfterCardChangedPilesLate[\s\S]*oldPileType\s*==\s*PileType\.Play[\s\S]*Pile\?\.Type\s*==\s*PileType\.Exhaust[\s\S]*StartChargeCycle\s*\(/.test(source)) {
    fail("ChargedModCard: playing the charged card must reset charge after it enters exhaust");
  }

  if (!/BeforeCombatStart[\s\S]*PlayerCombatState\.AllCards\.OfType<IChargedCard>\(\)[\s\S]*InitializeChargeAtCombatStart\s*\(/.test(relicSource)) {
    fail("ElementFlaskRelic: combat start must initialize charged cards from actual combat piles before the first draw");
  }
}

function checkMagicDamageFormula() {
  const magicCardsSource = readText("workspace/mod/ModCode/Cards/MagicCards.cs");
  const utilityPowersSource = readText("workspace/mod/ModCode/Powers/UtilityPowers.cs");

  if (!/MagicPower[\s\S]*ModifyDamageAdditive[\s\S]*props\.IsPoweredAttack\(\)/.test(utilityPowersSource)) {
    fail("MagicPower: magic damage bonus must only apply to powered attacks");
  }

  if (!/Splash[\s\S]*Hook\.ModifyDamage[\s\S]*ModifyDamageHookType\.All[\s\S]*decimal\s+splashDamage/.test(magicCardsSource)) {
    fail("MagicCardActions.Splash: splash base damage must use Hook.ModifyDamage so Corrupted magic cards calculate base * 1.5 + Magic");
  }

  if (!/Splash[\s\S]*DamageCmd\.Attack\s*\(\s*splashDamage\s*\)[\s\S]*\.Unpowered\(\)/.test(magicCardsSource)) {
    fail("MagicCardActions.Splash: splash hit must stay Unpowered so Magic is not added a second time");
  }
}

function readLocalization(locale) {
  const file = path.join(localizationDir, locale, "cards.json");
  return JSON.parse(fs.readFileSync(file, "utf8"));
}

function readLocalizationFile(locale, name) {
  const file = path.join(localizationDir, locale, name);
  return JSON.parse(fs.readFileSync(file, "utf8"));
}

function placeholders(text) {
  return [...String(text).matchAll(/\{([A-Za-z][A-Za-z0-9_]*):diff\(\)\}/g)].map((match) => match[1]);
}

function checkZhsCardStyle(cardId, suffix, text) {
  const value = String(text ?? "");
  const lines = value.split("\n");
  if (!value.endsWith("。")) {
    fail(`zhs: ${cardId}.${suffix} must end with 。`);
  }

  for (const [index, line] of lines.entries()) {
    if (index < lines.length - 1 && !line.endsWith("，")) {
      fail(`zhs: ${cardId}.${suffix} line ${index + 1} must end with ，`);
    }
  }

  if (value.includes("每段削韧")) {
    fail(`zhs: ${cardId}.${suffix} should use 削韧 n and leave per-hit rules to the hover tip`);
  }
}

function checkLocalization() {
  const expectedById = new Map(expectation.cards.map((card) => [card.id, card]));
  const expectedIds = [...expectedById.keys()];

  for (const locale of ["eng", "zhs"]) {
    const cardsJson = readLocalization(locale);
    const localizedTitleIds = Object.keys(cardsJson)
      .filter((key) => key.endsWith(".title"))
      .map((key) => key.replace(/\.title$/, ""));
    compareSets(`${locale} card titles`, localizedTitleIds, expectedIds);

    for (const expected of expectation.cards) {
      for (const suffix of ["title", "description", "upgradeDescription"]) {
        const key = `${expected.id}.${suffix}`;
        if (!Object.hasOwn(cardsJson, key)) {
          fail(`${locale}: missing ${key}`);
        }
      }

      const baseVarNames = new Set(Object.keys(expected.vars ?? {}));
      const upgradedVarNames = new Set(Object.keys({ ...(expected.vars ?? {}), ...((expected.upgrade ?? {}).vars ?? {}) }));
      for (const varName of placeholders(cardsJson[`${expected.id}.description`] ?? "")) {
        if (!baseVarNames.has(varName)) {
          fail(`${locale}: ${expected.id}.description uses unknown dynamic var ${varName}`);
        }
      }
      for (const varName of placeholders(cardsJson[`${expected.id}.upgradeDescription`] ?? "")) {
        if (!upgradedVarNames.has(varName)) {
          fail(`${locale}: ${expected.id}.upgradeDescription uses unknown dynamic var ${varName}`);
        }
      }

      if (locale === "zhs") {
        checkZhsCardStyle(expected.id, "description", cardsJson[`${expected.id}.description`]);
        checkZhsCardStyle(expected.id, "upgradeDescription", cardsJson[`${expected.id}.upgradeDescription`]);
      }
    }
  }

  const zhsPowers = readLocalizationFile("zhs", "powers.json");
  for (const prefix of ["mod", "MOD"]) {
    const description = zhsPowers[`${prefix}-IMBALANCE_POWER.description`] ?? "";
    for (const requiredText of ["削韧 n", "每段攻击", "失衡值为 0", "眩晕状态"]) {
      if (!description.includes(requiredText)) {
        fail(`zhs powers: ${prefix}-IMBALANCE_POWER.description must explain ${requiredText}`);
      }
    }
  }
}

function checkJsonFiles() {
  for (const locale of ["eng", "zhs"]) {
    for (const name of fs.readdirSync(path.join(localizationDir, locale))) {
      if (name.endsWith(".json")) {
        JSON.parse(fs.readFileSync(path.join(localizationDir, locale, name), "utf8"));
      }
    }
  }
}

checkJsonFiles();
checkCards();
checkChargeLifecycle();
checkMagicDamageFormula();
checkLocalization();

if (errors.length > 0) {
  console.error(`card tests failed with ${errors.length} issue(s):`);
  for (const error of errors) {
    console.error(`- ${error}`);
  }
  process.exitCode = 1;
} else {
  console.log(`card tests passed (${expectation.cards.length} cards, 2 locales)`);
}
