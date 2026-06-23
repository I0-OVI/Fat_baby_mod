#!/usr/bin/env node
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const scriptDir = path.dirname(fileURLToPath(import.meta.url));
const rootDir = path.resolve(scriptDir, "..");
const modDir = process.env.MOD_DIR
  ? path.resolve(process.env.MOD_DIR)
  : path.join(rootDir, "workspace/mod");
const modCodeDir = process.env.MOD_CODE_DIR
  ? path.resolve(process.env.MOD_CODE_DIR)
  : path.join(modDir, "ModCode");
const cardsDir = path.join(modCodeDir, "Cards");
const characterDir = path.join(modCodeDir, "Character");
const localizationDir = path.join(modDir, "mod/localization");
const expectationsPath = path.join(rootDir, "tests/card_expectations.json");

const expectation = JSON.parse(fs.readFileSync(expectationsPath, "utf8"));
const errors = [];

function fail(message) {
  errors.push(message);
}

function readModCodeText(relativePath) {
  return fs.readFileSync(path.join(modCodeDir, relativePath), "utf8");
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
    if (source.includes(": ModCard") || source.includes(": ChargedModCard")) {
      const expectedNamespace = "namespace FatBaby.ModCode.Cards;";
      if (!source.includes(expectedNamespace)) {
        fail(`${path.relative(rootDir, file)}: ModCard classes must use ${expectedNamespace}`);
      }
    }

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
      const removeKeywords = new Set([...body.matchAll(/RemoveKeyword\s*\(\s*CardKeyword\.(\w+)\s*\)/g)].map((keyword) => keyword[1]));
      const upgradedKeywords = sorted(new Set([...canonicalKeywords, ...upgradeKeywords].filter((keyword) => !removeKeywords.has(keyword))));

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
    const expectedUpgradedKeywords = sorted(new Set(
      [...expectedKeywords, ...(expectedUpgrade.keywords ?? [])].filter(
        (keyword) => !(expectedUpgrade.removeKeywords ?? []).includes(keyword)
      )
    ));

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
  const source = readModCodeText("Cards/ChargedModCard.cs");
  const relicSource = readModCodeText("Relics/ElementFlaskRelic.cs");

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

  if (!/ReturnToHandFromExhaust[\s\S]*UtilityCardActions\.MaxHandSize[\s\S]*StartChargeCycle\s*\(\s*showInSidebar:\s*true\s*\)[\s\S]*CardPileCmd\.Add/.test(source)) {
    fail("ChargedModCard: full hand must restart charge instead of moving to another pile");
  }

  if (!/AfterCardChangedPilesLate[\s\S]*OnEnteredExhaustFromOtherPileAsync/.test(source)) {
    fail("ChargedModCard: entering exhaust (played or forced) must reset charge");
  }

  if (!/OnEnteredExhaustFromOtherPileAsync/.test(source)) {
    fail("ChargedModCard: must expose OnEnteredExhaustFromOtherPileAsync for forced exhaust");
  }

  const patchSource = readModCodeText("Patches/ChargedCardExhaustPatch.cs");
  if (!/CardCmd\.Exhaust[\s\S]*IChargedCard[\s\S]*OnEnteredExhaustFromOtherPileAsync/.test(patchSource)) {
    fail("ChargedCardExhaustPatch: CardCmd.Exhaust must restart charge on the exhausted card instance");
  }

  if (!/BeforeCombatStart[\s\S]*ChargesRemaining\s*<=\s*0[\s\S]*return/.test(relicSource)) {
    fail("ElementFlaskRelic: must not add Element Flask to hand when no charges remain");
  }

  if (!/AfterCardChangedPilesLate[\s\S]*RemainingKey\]\.IntValue\s*<=\s*0/.test(readModCodeText("Cards/ElementFlask.cs"))) {
    fail("ElementFlask: must not return to hand when no uses remain");
  }

  if (!/BeforeCombatStart[\s\S]*PlayerCombatState\.AllCards\.OfType<IChargedCard>\(\)[\s\S]*InitializeChargeAtCombatStart\s*\(/.test(relicSource)) {
    fail("ElementFlaskRelic: combat start must initialize charged cards from actual combat piles before the first draw");
  }

  if (!/AfterRoomEntered[\s\S]*RoomType\.RestSite[\s\S]*AncientEventModel[\s\S]*RestoreToFullCharges/.test(relicSource)) {
    fail("ElementFlaskRelic: charges must restore at rest sites and ancient event rooms");
  }

  const refinedSource = readModCodeText("Relics/RefinedElementFlaskRelic.cs");
  const orobasPatchSource = readModCodeText("Patches/TouchOfOrobasElementFlaskPatch.cs");
  if (!/RefinedChargeCap\s*=\s*5/.test(refinedSource)) {
    fail("RefinedElementFlaskRelic: Orobas upgrade must raise charge cap to 5");
  }
  if (!/GetUpgradedStarterRelic[\s\S]*is ElementFlaskRelic[\s\S]*RefinedElementFlaskRelic/.test(orobasPatchSource)) {
    fail("TouchOfOrobasElementFlaskPatch: starter Element Flask must map to refined relic at Orobas");
  }
  const relicPoolSource = readModCodeText("Character/ScarletRelicPool.cs");
  if (!/GenerateAllRelics[\s\S]*ElementFlaskRelic[\s\S]*RefinedElementFlaskRelic/.test(relicPoolSource)) {
    fail("ScarletRelicPool: refined Element Flask must be registered so Orobas can reference it");
  }
  const advancePatchSource = readModCodeText("Patches/AncientDialogueAdvancePatch.cs");
  if (!/NAncientDialogueLine[\s\S]*_Ready[\s\S]*SignalName\.Released/.test(advancePatchSource)) {
    fail("AncientDialogueAdvancePatch: must hook dialogue line Released signal, not missing OnRelease");
  }

  const orobasDialogueSource = readModCodeText("Patches/OrobasScarletDialoguePatch.cs");
  if (!/DefineDialogues[\s\S]*SCARLET_ACOLYTE[\s\S]*VisitIndex\s*=\s*0/.test(orobasDialogueSource)) {
    fail("OrobasScarletDialoguePatch: Scarlet must have multi-visit Orobas dialogues");
  }
}

function checkWillToWinHpFloor() {
  const source = readModCodeText("Powers/WillToWinPower.cs");
  if (!/WillToWinPower[\s\S]*ModifyHpLostAfterOstyLate[\s\S]*Owner\.CurrentHp\s*-\s*1m/.test(source)) {
    fail("WillToWinPower: HP floor must cap actual HP loss via ModifyHpLostAfterOstyLate");
  }
}

function checkBurningMechanic() {
  const frostbiteMechanicSource = readModCodeText("Mechanics/FrostbiteMechanic.cs");
  const burnHoverTipSource = readModCodeText("Mechanics/BurnHoverTip.cs");
  const frostPowersSource = readModCodeText("Powers/FrostPowers.cs");
  const poolExpansionSource = readModCodeText("Cards/PoolExpansionCards.cs");
  const zhsStaticHoverTips = readLocalizationFile("zhs", "static_hover_tips.json");
  const engStaticHoverTips = readLocalizationFile("eng", "static_hover_tips.json");

  if (!/BurnFrostbittenTargets[\s\S]*GetPower<FrostbitePower>[\s\S]*PowerCmd\.Remove\(frostbite\)[\s\S]*Imbalance\.Reduce\(choiceContext,\s*target,\s*2/.test(frostbiteMechanicSource)) {
    fail("FrostbiteMechanic: Burning must remove Frostbite and immediately deal 2 poise damage");
  }

  if (!/FrostbiteMechanic[\s\S]*CreatureCmd\.Damage\([\s\S]*ValueProp\.Unblockable\s*\|\s*ValueProp\.Unpowered[\s\S]*dealer:\s*null,[\s\S]*cardSource:\s*null/.test(frostbiteMechanicSource)) {
    fail("FrostbiteMechanic: Frostbite max-HP damage must be fixed and ignore Attack damage modifiers");
  }

  if (/class BurningPower/.test(frostPowersSource)) {
    fail("FrostPowers: Burning is an immediate keyword effect and must not be a lingering Power");
  }

  if (!/FlameStrike[\s\S]*BurnFrostbittenTargets/.test(poolExpansionSource) || !/NightAndFlameStanceFire[\s\S]*BurnFrostbittenTargets/.test(poolExpansionSource)) {
    fail("PoolExpansionCards: Flame Strike and Night-and-Flame fire stance must trigger Burning");
  }

  if (!/BURN\.title/.test(burnHoverTipSource) || !/BURN\.description/.test(burnHoverTipSource)) {
    fail("BurnHoverTip: Burning must use a static sidebar hover tip");
  }

  if (!/FlameStrike[\s\S]*BurnHoverTip\.Get\(\)/.test(poolExpansionSource) || !/NightAndFlameStanceFire[\s\S]*BurnHoverTip\.Get\(\)/.test(poolExpansionSource)) {
    fail("PoolExpansionCards: Burning cards must show the Burning sidebar hover tip");
  }

  if (!zhsStaticHoverTips["BURN.title"] || !zhsStaticHoverTips["BURN.description"] || !engStaticHoverTips["BURN.title"] || !engStaticHoverTips["BURN.description"]) {
    fail("static_hover_tips: Burning must have zh/eng sidebar text");
  }
}

function checkMaraisExecutionersGreatsword() {
  const cardSource = readModCodeText("Cards/PoolExpansionCards.cs");
  const powerSource = readModCodeText("Powers/DesignedCardPowers.cs");
  const mechanicSource = readModCodeText("Mechanics/MaraisExecutionersGreatswordMechanic.cs");
  const patchSource = readModCodeText("Patches/MaraisExecutionersGreatswordPatch.cs");
  const runAssetsSource = readModCodeText("ModRunAssets.cs");
  const zhsCards = readLocalizationFile("zhs", "cards.json");
  const zhsPowers = readLocalizationFile("zhs", "powers.json");

  if (!/MaraisExecutionersGreatsword[\s\S]*RegisterVictoryBonus[\s\S]*DynamicVars\["DamageIncrease"\]\.BaseValue/.test(cardSource)) {
    fail("MaraisExecutionersGreatsword: playing the card must queue its listed victory bonus");
  }

  if (!/new DynamicVar\("DamageIncrease", 5m\)/.test(cardSource)) {
    fail("MaraisExecutionersGreatsword: card must grant 5% damage on victory");
  }

  if (!/PendingVictoryBonus/.test(mechanicSource)) {
    fail("MaraisExecutionersGreatswordMechanic: bonus must wait until combat victory");
  }

  if (!/ElementFlaskRelic\.GetMaraisPermanentDamageBonus|ElementFlaskRelic\.AddMaraisPermanentDamageBonus/.test(mechanicSource)) {
    fail("MaraisExecutionersGreatswordMechanic: permanent bonus must persist via saved element flask state");
  }

  const elementFlaskSource = readModCodeText("Relics/ElementFlaskRelic.cs");
  if (!/\[SavedProperty\(SerializationCondition\.SaveIfNotTypeDefault\)\][\s\S]*MaraisPermanentDamageBonusPercent/.test(elementFlaskSource)) {
    fail("ElementFlaskRelic: Marais permanent damage bonus must be saved with the run");
  }

  if (!/FinalizeVictoryBonusBeforeCombatCleanup[\s\S]*RefreshCombatPower/.test(mechanicSource)) {
    fail("MaraisExecutionersGreatswordMechanic: victory bonus must refresh a single buff before combat cleanup");
  }

  if (!/RefreshCombatPower[\s\S]*existing\s*=\s*instances\.FirstOrDefault\(\)[\s\S]*ModPowerCmd\.ModifyAmount\(existing/.test(mechanicSource)) {
    fail("MaraisExecutionersGreatswordMechanic: victory refresh must update the existing buff UI instead of removing and re-applying it");
  }

  if (/AfterCombatVictory[\s\S]*SyncAllCombatPowers|AfterCombatVictory[\s\S]*RefreshCombatPower/.test(`${mechanicSource}\n${patchSource}`)) {
    fail("MaraisExecutionersGreatsword: must not resync Marais buff after combat powers are cleared");
  }

  if (!/GetPowerInstances<MaraisExecutionersGreatswordPower>/.test(mechanicSource)) {
    fail("MaraisExecutionersGreatswordMechanic: duplicate Marais buff instances must be consolidated");
  }

  if (!/FinalizeVictoryBonusBeforeCombatCleanup|MaraisExecutionersGreatswordAfterCombatEndPatch/.test(`${mechanicSource}\n${patchSource}`)) {
    fail("MaraisExecutionersGreatsword: queued bonus must apply on combat victory");
  }

  if (!/BeforeCombatStarted|MaraisExecutionersGreatswordBeforeCombatPatch/.test(`${mechanicSource}\n${patchSource}`)) {
    fail("MaraisExecutionersGreatsword: accumulated bonus must be re-applied at combat start");
  }

  if (!/MaraisExecutionersGreatswordPower[\s\S]*marais_executioners_greatsword_power\.png[\s\S]*big\/marais_executioners_greatsword_power\.png/.test(powerSource)) {
    fail("MaraisExecutionersGreatswordPower: buff UI must use the Marais/Eochaid skill icon");
  }

  if (!/marais_executioners_greatsword_power\.png[\s\S]*big\/marais_executioners_greatsword_power\.png/.test(runAssetsSource)) {
    fail("ModRunAssets: Marais power icons must stay loaded during runs");
  }

  const cardDesc = zhsCards["MARAIS_EXECUTIONERS_GREATSWORD.description"];
  if (!cardDesc || !/胜利后/.test(cardDesc) || !/\{DamageIncrease:diff\(\)\}/.test(cardDesc)) {
    fail("MaraisExecutionersGreatsword: card text must describe the victory bonus percentage");
  }

  const powerDesc = zhsPowers["mod-MARAIS_EXECUTIONERS_GREATSWORD_POWER.smartDescription"];
  if (!powerDesc || !/\{Amount\}/.test(powerDesc)) {
    fail("MaraisExecutionersGreatswordPower: buff tooltip must show the current bonus percentage");
  }
}

function checkJobChangePermanentMagic() {
  const cardSource = readModCodeText("Cards/PoolExpansionCards.cs");
  const elementFlaskSource = readModCodeText("Relics/ElementFlaskRelic.cs");

  if (!/JobChange[\s\S]*AddJobChangePermanentMagic\(Owner,\s*DynamicVars\["Magic"\]\.IntValue\)[\s\S]*Apply<MagicPower>/.test(cardSource)) {
    fail("JobChange: playing the card must save its Magic as a permanent run bonus and apply it immediately");
  }

  if (!/\[SavedProperty\(SerializationCondition\.SaveIfNotTypeDefault\)\][\s\S]*JobChangePermanentMagic/.test(elementFlaskSource)) {
    fail("ElementFlaskRelic: Job Change permanent Magic must be saved with the run");
  }

  if (!/BeforeCombatStart[\s\S]*JobChangePermanentMagic\s*>\s*0[\s\S]*Apply<MagicPower>\(Owner\.Creature,\s*JobChangePermanentMagic/.test(elementFlaskSource)) {
    fail("ElementFlaskRelic: Job Change permanent Magic must be re-applied at combat start");
  }
}

function checkMagicDamageFormula() {
  const magicCardsSource = readModCodeText("Cards/MagicCards.cs");
  const utilityPowersSource = readModCodeText("Powers/UtilityPowers.cs");
  const magicMarkerSource = readModCodeText("Cards/IMagicDamageCard.cs");
  const strengthPatchSource = readModCodeText("Patches/StrengthPowerMagicDamagePatch.cs");
  const designedCardPowersSource = readModCodeText("Powers/DesignedCardPowers.cs");
  const frostPowersSource = readModCodeText("Powers/FrostPowers.cs");
  const magicSupportPowersSource = readModCodeText("Powers/MagicSupportPowers.cs");
  const runAssetsSource = readModCodeText("ModRunAssets.cs");

  if (!/interface IMagicAttributeCard[\s\S]*interface IMagicDamageCard\s*:\s*IMagicAttributeCard/.test(magicMarkerSource)) {
    fail("IMagicDamageCard: magic scaling cards must extend IMagicAttributeCard");
  }

  if (!/IMagicAttributeCard/.test(strengthPatchSource)) {
    fail("StrengthPowerMagicDamagePatch: magic attribute cards must ignore Strength");
  }

  if (!/MagicPower[\s\S]*ModifyDamageAdditive[\s\S]*props\.IsPoweredAttack\(\)/.test(utilityPowersSource)) {
    fail("MagicPower: magic damage bonus must only apply to powered attacks");
  }

  if (!/GetModifiedPrimaryDamage[\s\S]*Hook\.ModifyDamage[\s\S]*ModifyDamageHookType\.All[\s\S]*VigorPower/.test(magicCardsSource)) {
    fail("MagicCardActions.GetModifiedPrimaryDamage: must include Vigor when Hook.ModifyDamage omits it");
  }

  if (!/MagicAttackThenSplash[\s\S]*BeforeDamage[\s\S]*GetModifiedPrimaryDamage[\s\S]*await attack\.Execute/.test(magicCardsSource)) {
    fail("MagicCardActions.MagicAttackThenSplash: capture primary damage during the attack so Vigor binding is active");
  }

  if (!/Splash[\s\S]*SplashDamageFromPrimary[\s\S]*DamageCmd\.Attack\s*\(\s*splashDamage\s*\)[\s\S]*\.Unpowered\(\)/.test(magicCardsSource)) {
    fail("MagicCardActions.Splash: splash hit must stay Unpowered so Magic is not added a second time");
  }

  if (!/MagicVulnerabilityPower[\s\S]*MagicDamageMultiplier\s*=\s*1\.2m/.test(frostPowersSource)) {
    fail("MagicVulnerabilityPower: shared magic damage multiplier must stay in one place");
  }

  if (!/DarkMoonGreatSwordPower[\s\S]*ModifyDamageAdditive[\s\S]*cardSource is not IMagicAttributeCard[\s\S]*MagicVulnerabilityPower[\s\S]*MagicDamageMultiplier/.test(designedCardPowersSource)) {
    fail("DarkMoonGreatSwordPower: non-magic attack bonus must scale with Magic Vulnerability");
  }

  if (!/PiercingCounterPower[\s\S]*spear_talisman_power\.png[\s\S]*big\/spear_talisman_power\.png/.test(designedCardPowersSource)) {
    fail("PiercingCounterPower: buff UI must use the Spear Talisman icon");
  }

  if (!/spear_talisman_power\.png[\s\S]*big\/spear_talisman_power\.png/.test(runAssetsSource)) {
    fail("ModRunAssets: Spear Talisman power icons must stay loaded during runs");
  }

  if (!/HoulouGroundSlamPower[\s\S]*houlou_ground_slam_power\.png[\s\S]*big\/houlou_ground_slam_power\.png/.test(magicSupportPowersSource)) {
    fail("HoulouGroundSlamPower: buff UI must use Hoarah Loux's Earthshaker skill icon");
  }

  if (!/houlou_ground_slam_power\.png[\s\S]*big\/houlou_ground_slam_power\.png/.test(runAssetsSource)) {
    fail("ModRunAssets: Houlou Ground Slam power icons must stay loaded during runs");
  }
}

function checkMultiHitAttackCommands() {
  const magicCardsSource = readModCodeText("Cards/MagicCards.cs");
  const poiseCardsSource = readModCodeText("Cards/PoiseCards.cs");
  const bloodLevySource = readModCodeText("Cards/BloodLevy.cs");

  if (!/MagicAttack[\s\S]*WithHitCount\(hits\)/.test(magicCardsSource)) {
    fail("MagicCardActions.MagicAttack: must call WithHitCount for multi-hit attacks");
  }

  if (!/AttackAndPoise[\s\S]*WithHitCount\(hits\)/.test(poiseCardsSource)) {
    fail("PoiseCardActions.AttackAndPoise: must call WithHitCount for multi-hit attacks");
  }

  if (!/AttackAllAndPoise[\s\S]*WithHitCount\(hits\)/.test(poiseCardsSource)) {
    fail("PoiseCardActions.AttackAllAndPoise: must call WithHitCount for multi-hit attacks");
  }

  if (!/BloodLevy[\s\S]*WithHitCount\(DynamicVars\["Hits"\]\.IntValue\)/.test(bloodLevySource)) {
    fail("BloodLevy: must call WithHitCount for multi-hit attacks");
  }

  if (!/GlintstoneChunk[\s\S]*MagicRandomMultiHit\(this, choiceContext, DynamicVars\["Hits"\]\.IntValue\)/.test(magicCardsSource)) {
    fail("GlintstoneChunk: random multi-hit attacks must use MagicRandomMultiHit with WithHitCount");
  }

  if (!/MagicRandomMultiHit[\s\S]*WithHitCount\(hits\)[\s\S]*TargetingRandomOpponents/.test(magicCardsSource)) {
    fail("MagicCardActions.MagicRandomMultiHit: must use WithHitCount and TargetingRandomOpponents");
  }

  const vigorPatchSource = readModCodeText("Patches/VigorMultiHitPatch.cs");
  if (!/VigorMultiHitModifyPatch[\s\S]*StoredAmountField/.test(vigorPatchSource)) {
    fail("VigorMultiHitPatch: must apply stored vigor amount to every hit in a bound attack");
  }
}

function checkSmallRoundShieldParry() {
  const powerSource = readModCodeText("Powers/PoiseSupportPowers.cs");
  const cardSource = readModCodeText("Cards/UtilityCards.cs");
  const magicPowerSource = readModCodeText("Powers/MagicSupportPowers.cs");
  const patchSource = readModCodeText("Patches/SmallRoundShieldParryAttackPatch.cs");

  if (!/SmallRoundShieldParry[\s\S]*new DynamicVar\("Ripostes",\s*1m\)[\s\S]*SetFatalStrikeGrantCount\(DynamicVars\["Ripostes"\]\.IntValue\)[\s\S]*OnUpgrade\(\)\s*=>\s*DynamicVars\["Ripostes"\]\.UpgradeValueBy\(1m\)/.test(cardSource)) {
    fail("SmallRoundShieldParry: upgraded card must display and grant 2 Exhaust Ripostes via a dynamic variable");
  }

  if (!/SmallRoundShieldParryPower[\s\S]*ModifyHpLostAfterOstyLate[\s\S]*_parriedDamage = true[\s\S]*AfterModifyingHpLostAfterOsty[\s\S]*CreateCard<FatalStrike>[\s\S]*CardPileCmd\.Add\(fatalStrike, PileType\.Hand\)/.test(powerSource)) {
    fail("SmallRoundShieldParryPower: parry resolution must grant Fatal Strike after HP loss is prevented");
  }

  if (!/SmallRoundShieldParryPower[\s\S]*_interruptedAttackTarget[\s\S]*dealer\s*==\s*_interruptedAttackTarget[\s\S]*dealer\.IsStunned[\s\S]*return\s+0m/.test(powerSource)) {
    fail("SmallRoundShieldParryPower: stunned parry target must have remaining multi-hit damage canceled");
  }

  if (!/AttackCommand[\s\S]*Execute[\s\S]*CompleteInterruptedAttackAsync/.test(patchSource)) {
    fail("SmallRoundShieldParryAttackPatch: parry interrupt state must clean up after AttackCommand.Execute");
  }

  if (!/CarianRetributionPower[\s\S]*ModifyHpLostAfterOstyLate[\s\S]*_counterTarget = dealer[\s\S]*AfterModifyingHpLostAfterOsty[\s\S]*CreatureCmd\.Stun[\s\S]*_interruptedAttackTarget[\s\S]*CompleteInterruptedAttackAsync/.test(magicPowerSource)) {
    fail("CarianRetributionPower: prevented damage must Stun its source and interrupt the rest of that Attack");
  }

  if (!/GetPowerInstances<CarianRetributionPower>/.test(patchSource)) {
    fail("SmallRoundShieldParryAttackPatch: Carian Retribution interrupt state must clean up after AttackCommand.Execute");
  }
}

function checkCardLibraryScrollPatch() {
  const patchSource = readModCodeText("Patches/ScarletCardLibraryUnlockPatch.cs");

  if (!/ScarletCardLibraryHolderUpdateStatsPatch[\s\S]*TargetMethod\(\)[\s\S]*AccessTools\.Method\(typeof\(NGridCardHolder\),\s*"UpdateStats"\)[\s\S]*Prepare\(\)[\s\S]*EnsureCardLibraryStatsExists/.test(patchSource)) {
    fail("ScarletCardLibraryUnlockPatch: optional recycled-holder patch must skip safely when UpdateStats is absent");
  }

  if (!/ScarletCardLibraryReallocateRowsPatch[\s\S]*TargetMethods\(\)[\s\S]*ReallocateAll[\s\S]*ReallocateAbove[\s\S]*ReallocateBelow[\s\S]*ReconcileRows[\s\S]*_scrollContainer[\s\S]*Position[\s\S]*RowsMatch/.test(patchSource)) {
    fail("ScarletCardLibraryUnlockPatch: card library must reconcile virtual rows from scroll position while scrolling");
  }

  if (!/ScarletCardLibraryOpenedPatch[\s\S]*LoadPortraits\(\)[\s\S]*ResetGridScroll/.test(patchSource)) {
    fail("ScarletCardLibraryOpenedPatch: portraits and scroll state must refresh every time the library opens");
  }

  if (!/ConfigureScarletCharacterPoolFilter[\s\S]*poolFilters\[scarletFilter\] = IsScarletPoolCard/.test(patchSource)) {
    fail("ScarletCardLibraryUnlockPatch: Scarlet must use BaseLib's character pool filter for all Scarlet cards");
  }

  if (/templateFilter\.Duplicate\(\)|AddChild\(scarletFilter\)/.test(patchSource)) {
    fail("ScarletCardLibraryUnlockPatch: must not add a duplicate Scarlet pool filter button");
  }
}

function checkStackedNextAttackPowers() {
  const powerSource = readModCodeText("Powers/PoiseSupportPowers.cs");
  const poiseCardsSource = readModCodeText("Cards/PoiseCards.cs");
  const imbalanceSource = readModCodeText("Mechanics/Imbalance.cs");

  function powerBlock(className) {
    const classIndex = powerSource.indexOf(`class ${className}`);
    const openBrace = powerSource.indexOf("{", classIndex);
    return classIndex >= 0 && openBrace >= 0 ? extractBlock(powerSource, openBrace) : "";
  }

  const doublePower = powerBlock("NextAttackDoublePower");
  if (!/ModifyDamageMultiplicative[\s\S]*CardType\.Attack[\s\S]*return\s+2m[\s\S]*AfterCardPlayed[\s\S]*PowerCmd\.Decrement\s*\(\s*this\s*\)/.test(doublePower) || /PowerCmd\.Remove\s*\(\s*this\s*\)/.test(doublePower)) {
    fail("NextAttackDoublePower: the next Attack must deal double damage and consume exactly one stacked use");
  }

  if (!/DeferUsesCreatedBy[\s\S]*_deferredSourceCard[\s\S]*Amount\s*<=\s*_deferredUses[\s\S]*return\s+1m/.test(doublePower)) {
    fail("NextAttackDoublePower: stacks created during an Attack must not multiply that same Attack");
  }

  const poisePower = powerBlock("NextPoiseBonusPower");
  if (!/Queue<int>[\s\S]*AddBonus[\s\S]*GetNextBonus[\s\S]*AfterCardPlayed[\s\S]*PowerCmd\.Decrement\s*\(\s*this\s*\)/.test(poisePower)) {
    fail("NextPoiseBonusPower: stacked Rock Blades must queue bonuses and consume one use per Attack");
  }

  if (!/RockBlade[\s\S]*Apply<NextPoiseBonusPower>\s*\(\s*Owner\.Creature\s*,\s*1m[\s\S]*AddBonus/.test(poiseCardsSource)) {
    fail("RockBlade: each play must add one queued poise-bonus use");
  }

  if (!/NextPoiseBonusPower[\s\S]*GetNextBonus\s*\(\s*\)/.test(imbalanceSource) || /NextPoiseBonusPower[\s\S]*PowerCmd\.Remove\s*\(\s*poiseBonus\s*\)/.test(imbalanceSource)) {
    fail("Imbalance: Rock Blade bonus must remain active for the full Attack card");
  }

  if (!/VictoryRushPower[\s\S]*Apply<NextAttackDoublePower>[\s\S]*cardSource\?\.Type\s*==\s*CardType\.Attack[\s\S]*DeferUsesCreatedBy\(cardSource,\s*1\)/.test(powerSource)) {
    fail("VictoryRushPower: double-damage stacks created by an Attack stun must be deferred until the next Attack");
  }
}

function checkImbalanceResetScaling() {
  const imbalanceSource = readModCodeText("Mechanics/Imbalance.cs");
  const imbalancePowerSource = readModCodeText("Powers/ImbalancePower.cs");

  if (!/InitialResetValue[\s\S]*CurrentResetValue[\s\S]*InitializeResetValue[\s\S]*IncreaseResetValue/.test(imbalancePowerSource)) {
    fail("ImbalancePower: must track initial and current reset values for repeated stuns");
  }

  if (!/IncreaseResetValue\(int amount\)[\s\S]*Math\.Min\(CurrentResetValue \+ amount,\s*InitialResetValue \+ 10\)/.test(imbalancePowerSource)) {
    fail("ImbalancePower: reset value must grow by the requested amount and cap at initial + 10");
  }

  if (!/ApplyInitialPower[\s\S]*GetInitialValue\(target\)[\s\S]*Apply<ImbalancePower>[\s\S]*InitializeResetValue\(initialValue\)/.test(imbalanceSource)) {
    fail("Imbalance: initial reset value must be captured when applying Imbalance");
  }

  if (!/Break[\s\S]*CreatureCmd\.Stun\(target\)[\s\S]*IncreaseResetValue\(2\)[\s\S]*SetAmount\(nextResetValue,\s*silent:\s*true\)/.test(imbalanceSource)) {
    fail("Imbalance: after a stun, reset value must increase by 2 and use that value");
  }

  if (/Break[\s\S]*SetAmount\(GetInitialValue\(target\)/.test(imbalanceSource)) {
    fail("Imbalance: repeated stuns must not reset back to the original initial value");
  }
}

function checkCardPortraitLoading() {
  const modCardSource = readModCodeText("Cards/ModCard.cs");
  const libraryPatchSource = readModCodeText("Patches/ScarletCardLibraryUnlockPatch.cs");

  if (!/ExtraRunAssetPaths\s*=>\s*AllPortraitPaths/.test(modCardSource)) {
    fail("ModCard: every portrait must be included in run assets so asset-set transitions cannot unload it");
  }

  if (!/LoadPortraits[\s\S]*GetPortraitPaths\(\)[\s\S]*ResourceLoader\.Load<Texture2D>[\s\S]*LoadedPortraits\.Add/.test(libraryPatchSource)) {
    fail("ScarletCardLibraryUnlockPatch: card library must load and retain Scarlet portraits before building its grid");
  }

  if (!/ScarletCardLibraryGridReadyPatch[\s\S]*LoadPortraits\(\)/.test(libraryPatchSource)) {
    fail("ScarletCardLibraryGridReadyPatch: portraits must load before the card-library grid initializes");
  }

  if (!/ScarletCardLibraryOpenedPatch[\s\S]*LoadPortraits\(\)/.test(libraryPatchSource)) {
    fail("ScarletCardLibraryOpenedPatch: portraits must reload whenever the card library is reopened");
  }

  if (!/GetPortraitPaths\(\)/.test(libraryPatchSource)) {
    fail("ScarletCardLibraryUnlockPatch: portrait preload paths must be derived from Scarlet cards");
  }

  for (const file of listFiles(cardsDir, ".cs")) {
    const source = fs.readFileSync(file, "utf8");
    for (const match of source.matchAll(/PortraitPath\s*=>\s*"(res:\/\/mod\/images\/card_portraits\/[^"]+)"/g)) {
      const portraitFile = path.join(modDir, match[1].replace(/^res:\/\/mod\//, "mod/"));
      if (!fs.existsSync(portraitFile)) {
        fail(`${path.relative(rootDir, file)}: missing portrait file ${match[1]}`);
      }
    }
  }
}

function checkAncientCardVisualFallback() {
  const ancientVisualSource = readModCodeText("ModAncientCardVisuals.cs");
  const ancientPatchSource = readModCodeText("Patches/ModAncientCardVisualPatch.cs");

  if (!/Texture2D\?\s+GetFrameTexture\(CardModel model\)[\s\S]*Texture2D\?\s+GetPortraitBorderTexture\(CardModel model\)[\s\S]*Texture2D\?\s+GetBannerTexture\(\)/.test(ancientVisualSource)) {
    fail("ModAncientCardVisuals: standard Ancient frame texture lookups must be nullable");
  }

  if (!/GetPortraitTexture\(CardModel model\)[\s\S]*model\.AllPortraitPaths[\s\S]*ResourceLoader\.Load<Texture2D>[\s\S]*return model\.Portrait/.test(ancientVisualSource)) {
    fail("ModAncientCardVisuals: mod Ancient cards must reload portraits from AllPortraitPaths in combat compendium views");
  }

  if (!/portraitBorderTexture\s*==\s*null[\s\S]*frameTexture\s*==\s*null[\s\S]*bannerTexture\s*==\s*null[\s\S]*portraitTexture\s*==\s*null[\s\S]*standard frame resources are not loaded[\s\S]*return;[\s\S]*portraitBorder\.Visible\s*=\s*true[\s\S]*ancientPortrait\.Visible\s*=\s*false/.test(ancientPatchSource)) {
    fail("ModAncientCardVisualPatch: must keep vanilla Ancient visuals when standard frame resources are missing");
  }

  if (!/portrait\.Texture\s*=\s*portraitTexture/.test(ancientPatchSource) || /portrait\.Texture\s*=\s*model\.Portrait/.test(ancientPatchSource)) {
    fail("ModAncientCardVisualPatch: must use the resolved portrait texture instead of model.Portrait");
  }

  if (!/ModCardPortraitReloadPatch[\s\S]*ReloadPortraitFromModelPaths[\s\S]*GetPortraitTexture\(model\)[\s\S]*%Portrait[\s\S]*portrait\.Texture\s*=\s*portraitTexture[\s\S]*%AncientPortrait[\s\S]*ancientPortrait\.Texture\s*=\s*portraitTexture/.test(ancientPatchSource)) {
    fail("ModAncientCardVisualPatch: combat compendium cards must refresh both standard and Ancient portrait nodes from model paths");
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

const KEYWORD_DESCRIPTION_PREFIXES = {
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

function startsWithCardKeywordPrefix(text, keywords, locale) {
  let value = String(text ?? "");
  for (const keyword of keywords) {
    const prefix = KEYWORD_DESCRIPTION_PREFIXES[locale][keyword];
    if (prefix && value.startsWith(prefix)) {
      return keyword;
    }
  }
  return null;
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

const CUSTOM_MECHANIC_COLOR_TERMS = {
  eng: [
    "Scarlet Corruption",
    "Magic Vulnerability",
    "magic damage",
    "Poise damage",
    "poise damage",
    "Poise",
    "Splash",
    "Repeat",
    "Frostbite",
    "Burn",
  ],
  zhs: [
    "猩红腐败",
    "魔法易伤",
    "魔法伤害",
    "削韧",
    "溅射",
    "重复",
    "冻伤",
    "燃烧",
  ],
};

function textOutsideGoldAndPlaceholders(text) {
  return String(text ?? "")
    .replace(/\[gold\][\s\S]*?\[\/gold\]/g, "")
    .replace(/\{[^}]*\}/g, "");
}

function checkCustomMechanicColors(locale, cardsJson) {
  for (const [key, value] of Object.entries(cardsJson)) {
    if (typeof value !== "string" || !/(description|upgradeDescription)$/.test(key)) {
      continue;
    }

    if (/\{[^}]*\[gold\]/.test(value)) {
      fail(`${locale}: ${key} must not color text inside dynamic var placeholders`);
    }

    if (/\[gold\][^\[]*\[gold\]/.test(value) || /\[gold\]\[gold\]/.test(value)) {
      fail(`${locale}: ${key} has nested gold tags`);
    }

    const plainText = textOutsideGoldAndPlaceholders(value);
    for (const term of CUSTOM_MECHANIC_COLOR_TERMS[locale]) {
      if (plainText.includes(term)) {
        fail(`${locale}: ${key} must color custom mechanic term ${term}`);
      }
    }
  }
}

function checkLocalization() {
  const cards = parseCards();
  const expectedById = new Map(expectation.cards.map((card) => [card.id, card]));
  const expectedIds = [...expectedById.keys()];

  for (const locale of ["eng", "zhs"]) {
    const cardsJson = readLocalization(locale);
    checkCustomMechanicColors(locale, cardsJson);
    const localizedTitleIds = Object.keys(cardsJson)
      .filter((key) => key.endsWith(".title"))
      .map((key) => key.replace(/\.title$/, ""))
      .filter((id) => expectedById.has(id));
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

      const actualCard = [...cards.values()].find((card) => card.id === expected.id);
      if (actualCard) {
        const duplicateBase = startsWithCardKeywordPrefix(
          cardsJson[`${expected.id}.description`],
          actualCard.keywords,
          locale
        );
        if (duplicateBase) {
          fail(`${locale}: ${expected.id}.description repeats keyword ${duplicateBase}; card keywords already show it`);
        }

        const duplicateUpgrade = startsWithCardKeywordPrefix(
          cardsJson[`${expected.id}.upgradeDescription`],
          actualCard.upgradedKeywords,
          locale
        );
        if (duplicateUpgrade) {
          fail(`${locale}: ${expected.id}.upgradeDescription repeats keyword ${duplicateUpgrade}; card keywords already show it`);
        }
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
checkWillToWinHpFloor();
checkBurningMechanic();
checkMaraisExecutionersGreatsword();
checkJobChangePermanentMagic();
checkMagicDamageFormula();
checkMultiHitAttackCommands();
checkSmallRoundShieldParry();
checkCardLibraryScrollPatch();
checkStackedNextAttackPowers();
checkImbalanceResetScaling();
checkCardPortraitLoading();
checkAncientCardVisualFallback();
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
