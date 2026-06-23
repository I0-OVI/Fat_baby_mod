using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using FatBaby.ModCode.Commands;
using FatBaby.ModCode.Mechanics;
using FatBaby.ModCode.Powers;
using FatBaby.ModCode.Relics;

namespace FatBaby.ModCode.Cards;

public sealed class FrostbiteGrease() : ModCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/frostbite_grease.png";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        HoverTipFactory.FromPower<FrostbitePower>(),
        HoverTipFactory.FromPower<FrostbiteGreasePower>()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await ModPowerCmd.Apply<FrostbiteGreasePower>(Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class JobChange() : ModCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/job_change.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MagicPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Magic", 1m),
        new DynamicVar("MaxHpLoss", 2m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ElementFlaskRelic.AddJobChangePermanentMagic(Owner, DynamicVars["Magic"].IntValue);
        await ModPowerCmd.Apply<MagicPower>(Owner.Creature, DynamicVars["Magic"].BaseValue, Owner.Creature, this);
        await CreatureCmd.LoseMaxHp(choiceContext, Owner.Creature, DynamicVars["MaxHpLoss"].BaseValue, isFromCard: true);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Magic"].UpgradeValueBy(1m);
        DynamicVars["MaxHpLoss"].UpgradeValueBy(2m);
    }
}

public sealed class MaraisExecutionersGreatsword() : ModCard(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/marais_executioners_greatsword.png";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromKeyword(CardKeyword.Ethereal),
        HoverTipFactory.FromPower<MaraisExecutionersGreatswordPower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("DamageIncrease", 5m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await MaraisExecutionersGreatswordMechanic.RegisterVictoryBonus(
            Owner,
            DynamicVars["DamageIncrease"].BaseValue);
    }

    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Ethereal);
}

public sealed class FlameStrike() : ModCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override string PortraitPath => "res://mod/images/card_portraits/flame_strike.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        BurnHoverTip.Get(),
        HoverTipFactory.FromPower<FrostbitePower>(),
        HoverTipFactory.FromPower<ImbalancePower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(7m, ValueProp.Move), new DynamicVar("Imbalance", 2m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        await FrostbiteMechanic.BurnFrostbittenTargets(choiceContext, [cardPlay.Target], Owner.Creature, this);
        await Imbalance.Reduce(choiceContext, cardPlay.Target, DynamicVars["Imbalance"].IntValue, Owner.Creature, this);
    }
}

public sealed class NightAndFlameStanceNight() : ModCard(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy), IMagicDamageCard
{
    public override string PortraitPath => "res://mod/images/card_portraits/night_and_flame_stance_night.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MagicPower>(), HoverTipFactory.FromPower<ImbalancePower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(10m, ValueProp.Move), new DynamicVar("Imbalance", 5m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        await Imbalance.Reduce(choiceContext, cardPlay.Target, DynamicVars["Imbalance"].IntValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars["Imbalance"].UpgradeValueBy(2m);
    }
}

public sealed class NightAndFlameStanceFire() : ModCard(1, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    public override string PortraitPath => "res://mod/images/card_portraits/night_and_flame_stance_fire.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        BurnHoverTip.Get(),
        HoverTipFactory.FromPower<FrostbitePower>(),
        HoverTipFactory.FromPower<ImbalancePower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(8m, ValueProp.Move), new DynamicVar("Imbalance", 3m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null)
        {
            return;
        }

        IReadOnlyList<Creature> targets = CombatState.HittableEnemies.ToList();
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        await FrostbiteMechanic.BurnFrostbittenTargets(choiceContext, targets, Owner.Creature, this);

        foreach (Creature target in targets)
        {
            await Imbalance.Reduce(choiceContext, target, DynamicVars["Imbalance"].IntValue, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars["Imbalance"].UpgradeValueBy(1m);
    }
}
