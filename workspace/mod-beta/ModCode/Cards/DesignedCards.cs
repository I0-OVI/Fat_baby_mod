using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Mod.ModCode.Commands;
using Mod.ModCode.Powers;

namespace Mod.ModCode.Cards;

public sealed class BlackFlameRitual() : ModCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/black_flame_ritual.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1),
        new DynamicVar("Exhaust", 1m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        List<CardModel> drawPile = PileType.Draw.GetPile(Owner).Cards.ToList();
        int cardsToChoose = Math.Min(DynamicVars.Cards.IntValue, drawPile.Count);
        if (cardsToChoose > 0)
        {
            var prefs = new CardSelectorPrefs(SelectionScreenPrompt, cardsToChoose);
            foreach (CardModel selectedCard in await CardSelectCmd.FromSimpleGrid(choiceContext, drawPile, Owner, prefs))
            {
                await CardPileCmd.Add(selectedCard, PileType.Hand);
            }
        }

        await UtilityCardActions.ExhaustFromHand(this, choiceContext, DynamicVars["Exhaust"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
        DynamicVars["Exhaust"].UpgradeValueBy(1m);
    }
}

public sealed class ThisRoadIsBlocked() : ModCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/this_road_is_blocked.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<FrostbitePower>(),
        HoverTipFactory.FromPower<ThisRoadIsBlockedPower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Reduction", 25m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        bool anyFrostbittenEnemy = CombatState?.Enemies.Any(enemy =>
            enemy.IsAlive && enemy.GetPower<FrostbitePower>() != null) == true;
        if (anyFrostbittenEnemy)
        {
            await ModPowerCmd.Apply<ThisRoadIsBlockedPower>(
                Owner.Creature,
                DynamicVars["Reduction"].BaseValue,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Reduction"].UpgradeValueBy(25m);
}

public sealed class DarkMoonGreatSword() : ModCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/dark_moon_great_sword.png";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        HoverTipFactory.FromPower<DarkMoonGreatSwordPower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Turns", 2m),
        new DynamicVar("BonusDamage", 4m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        decimal bonusDamage = DynamicVars["BonusDamage"].BaseValue;
        DarkMoonGreatSwordPower? power = await ModPowerCmd.Apply<DarkMoonGreatSwordPower>(
            Owner.Creature,
            bonusDamage,
            Owner.Creature,
            this);
        power?.AddBonus(DynamicVars["Turns"].IntValue, bonusDamage);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["BonusDamage"].UpgradeValueBy(1m);
    }
}

public sealed class CorruptionResonance() : ModCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/corruption_resonance.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ScarletCorruptionPower>(),
        HoverTipFactory.FromPower<CorruptionResonancePower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Energy", 1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await ModPowerCmd.Apply<CorruptionResonancePower>(
            Owner.Creature,
            DynamicVars["Energy"].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class PiercingCounter() : ModCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/piercing_counter.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<PiercingCounterPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("DamageIncrease", 20m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        bool anyEnemyIntendsToAttack = CombatState?.Enemies.Any(enemy =>
            enemy.IsAlive && enemy.Monster?.IntendsToAttack == true) == true;
        if (anyEnemyIntendsToAttack)
        {
            await ModPowerCmd.Apply<PiercingCounterPower>(
                Owner.Creature,
                DynamicVars["DamageIncrease"].BaseValue,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade() => DynamicVars["DamageIncrease"].UpgradeValueBy(5m);
}

public sealed class Gamble() : ModCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/gamble.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [EnergyHoverTip, HoverTipFactory.FromPower<NextTurnEnergyPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Energy", 5m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await ModPowerCmd.Apply<NextTurnEnergyPower>(
            Owner.Creature,
            DynamicVars["Energy"].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Energy"].UpgradeValueBy(1m);
    }
}

public sealed class InnerPotential() : ModCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/inner_potential.png";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        HoverTipFactory.FromPower<InnerPotentialPower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("DamageIncrease", 30m),
        new DynamicVar("HpLoss", 5m),
        new DynamicVar("Turns", 2m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.Damage(
            choiceContext,
            Owner.Creature,
            DynamicVars["HpLoss"].BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            this);
        InnerPotentialPower? power = await ModPowerCmd.Apply<InnerPotentialPower>(
            Owner.Creature,
            DynamicVars["DamageIncrease"].BaseValue,
            Owner.Creature,
            this);
        power?.AddEffect(DynamicVars["DamageIncrease"].BaseValue, DynamicVars["HpLoss"].BaseValue);
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}
