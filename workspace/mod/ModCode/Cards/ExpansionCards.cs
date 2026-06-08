using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using Mod.ModCode.Commands;
using Mod.ModCode.Mechanics;
using Mod.ModCode.Powers;

namespace Mod.ModCode.Cards;

public sealed class HoarfrostStomp() : ModCard(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies), IMagicDamageCard
{
    public override string PortraitPath => "res://mod/images/card_portraits/hoarfrost_stomp.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MagicPower>(), HoverTipFactory.FromPower<FrostbitePower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null)
        {
            return;
        }

        List<Creature> targets = CombatState.HittableEnemies.Where(enemy => enemy.IsAlive).ToList();
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        await FrostbiteMechanic.Apply(choiceContext, targets, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}

public sealed class BloodSlash() : ModCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    public override string PortraitPath => "res://mod/images/card_portraits/blood_slash.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<ImbalancePower>(), HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10m, ValueProp.Move),
        new DynamicVar("HpLoss", 1m),
        new DynamicVar("Exhaust", 2m),
        new DynamicVar("Imbalance", 2m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await CreatureCmd.Damage(choiceContext, Owner.Creature, DynamicVars["HpLoss"].BaseValue, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this);
        await PoiseCardActions.AttackAndPoise(this, choiceContext, cardPlay.Target);
        await UtilityCardActions.ExhaustFromHand(this, choiceContext, DynamicVars["Exhaust"].IntValue);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}

public sealed class ZamorIceStorm() : ModCard(1, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies), IMagicDamageCard
{
    public override string PortraitPath => "res://mod/images/card_portraits/zamor_ice_storm.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<MagicPower>(),
        HoverTipFactory.FromPower<FrostbitePower>(),
        HoverTipFactory.FromPower<ImbalancePower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(1m, ValueProp.Move),
        new DynamicVar("Hits", 5m),
        new DynamicVar("Imbalance", 2m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null)
        {
            return;
        }

        await PoiseCardActions.AttackAllAndPoise(this, choiceContext, DynamicVars["Hits"].IntValue);
        List<Creature> targets = CombatState.HittableEnemies.Where(enemy => enemy.IsAlive).ToList();
        await FrostbiteMechanic.Apply(choiceContext, targets, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["Hits"].UpgradeValueBy(2m);
}

public sealed class RennalasFullMoon() : ModCard(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies), IMagicDamageCard
{
    public override string PortraitPath => "res://mod/images/card_portraits/rennalas_full_moon.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<MagicPower>(),
        HoverTipFactory.FromPower<FrostbitePower>(),
        HoverTipFactory.FromPower<MagicVulnerabilityPower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(15m, ValueProp.Move),
        new DynamicVar("VulnerableTurns", 3m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null)
        {
            return;
        }

        List<Creature> targets = CombatState.HittableEnemies.Where(enemy => enemy.IsAlive).ToList();
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        await FrostbiteMechanic.Apply(choiceContext, targets, Owner.Creature, this);
        await ModPowerCmd.Apply<MagicVulnerabilityPower>(targets, DynamicVars["VulnerableTurns"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}

public sealed class RaptorOfMists() : ModCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/raptor_of_mists.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<RaptorOfMistsPower>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await ModPowerCmd.Apply<RaptorOfMistsPower>(Owner.Creature, 1m, Owner.Creature, this);

        CardModel? selectedCard = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            PileType.Discard.GetPile(Owner).Cards,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1))).FirstOrDefault();
        if (selectedCard != null)
        {
            await CardPileCmd.Add(selectedCard, PileType.Hand);
        }
    }

    protected override void OnUpgrade()
    {
    }
}

public sealed class SealOfPromise() : ModCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/proof_of_a_concord_kept.png";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Gold", 0m)];

    protected override bool IsPlayable => Owner != null && PileType.Draw.GetPile(Owner).Cards.Count == 0;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (IsUpgraded)
        {
            await PlayerCmd.GainGold(50m, Owner);
        }

        List<Creature> livingEnemies = Owner?.Creature.CombatState?.Enemies
            .Where(enemy => enemy.IsAlive)
            .ToList() ?? [];
        if (livingEnemies.Count > 0)
        {
            await CreatureCmd.Kill(livingEnemies);
        }

        if (!await CombatManager.Instance.CheckWinCondition())
        {
            await CombatManager.Instance.EndCombatInternal();
        }
    }

    protected override void OnUpgrade() => DynamicVars["Gold"].UpgradeValueBy(50m);
}

public sealed class WhyAreTheyFighting() : ModCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/why_are_they_fighting.png";

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        IEnumerable<CardModel> cards = IsUpgraded
            ? PileType.Hand.GetPile(Owner).Cards.ToList()
            : await CardSelectCmd.FromHand(
                choiceContext,
                Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 1),
                CanRandomizeCost,
                this);

        foreach (CardModel card in cards.Where(CanRandomizeCost))
        {
            card.EnergyCost.SetThisTurnOrUntilPlayed(Owner.RunState.Rng.CombatEnergyCosts.NextInt(3));
            NCard.FindOnTable(card)?.PlayRandomizeCostAnim();
        }
    }

    private bool CanRandomizeCost(CardModel card)
    {
        return card != this
            && !card.EnergyCost.CostsX
            && card.EnergyCost.GetWithModifiers(CostModifiers.None) >= 0;
    }

    protected override void OnUpgrade()
    {
    }
}
