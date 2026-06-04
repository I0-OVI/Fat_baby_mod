using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Mod.ModCode.Mechanics;
using Mod.ModCode.Powers;

namespace Mod.ModCode.Cards;

internal static class PoiseCardActions
{
    public static async Task AttackAndPoise(ModCard card, PlayerChoiceContext choiceContext, Creature target, int hits = 1)
    {
        for (int i = 0; i < hits; i++)
        {
            await DamageCmd.Attack(card.DynamicVars.Damage.BaseValue)
                .FromCard(card)
                .Targeting(target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
            await Imbalance.Reduce(choiceContext, target, card.DynamicVars["Imbalance"].IntValue, card.Owner.Creature, card);
        }
    }

    public static async Task AttackAllAndPoise(ModCard card, PlayerChoiceContext choiceContext, int hits = 1)
    {
        if (card.CombatState == null)
        {
            return;
        }

        for (int i = 0; i < hits; i++)
        {
            IReadOnlyList<Creature> targets = card.CombatState.HittableEnemies.ToList();
            await DamageCmd.Attack(card.DynamicVars.Damage.BaseValue)
                .FromCard(card)
                .TargetingAllOpponents(card.CombatState)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);

            foreach (Creature target in targets)
            {
                await Imbalance.Reduce(choiceContext, target, card.DynamicVars["Imbalance"].IntValue, card.Owner.Creature, card);
            }
        }
    }
}

public sealed class BasicAttack() : ModCard(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
{
    public override string PortraitPath => "res://mod/images/card_portraits/basic_attack.png";

    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<ImbalancePower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6m, ValueProp.Move), new DynamicVar("Imbalance", 2m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await PoiseCardActions.AttackAndPoise(this, choiceContext, cardPlay.Target);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}

public sealed class BasicDefense() : ModCard(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/basic_defense.png";

    protected override HashSet<CardTag> CanonicalTags => [CardTag.Defend];
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(5m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}

public sealed class ChargedAttack() : ModCard(2, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
{
    public override string PortraitPath => "res://mod/images/card_portraits/charged_attack.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<ImbalancePower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(15m, ValueProp.Move), new DynamicVar("Imbalance", 5m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await PoiseCardActions.AttackAndPoise(this, choiceContext, cardPlay.Target);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}

public sealed class ChargedHeavyAttack() : ModCard(1, CardType.Attack, CardRarity.Ancient, TargetType.AnyEnemy)
{
    public override string PortraitPath => "res://mod/images/card_portraits/charged_heavy_attack.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<ImbalancePower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(15m, ValueProp.Move), new DynamicVar("Imbalance", 5m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await PoiseCardActions.AttackAndPoise(this, choiceContext, cardPlay.Target);

        CardModel? card = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            PileType.Discard.GetPile(Owner).Cards,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1))).FirstOrDefault();

        if (card != null)
        {
            await CardPileCmd.Add(card, PileType.Hand);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}

public sealed class GuardCounter() : ChargedModCard<GuardCounterChargePower>(0, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
{
    public override string PortraitPath => "res://mod/images/card_portraits/guard_counter.png";

    protected override int ChargeAmount => DynamicVars["Charge"].IntValue;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => ChargeHoverTips.Concat([HoverTipFactory.FromPower<ImbalancePower>(), HoverTipFactory.FromPower<GuardCounterPower>()]);
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6m, ValueProp.Move), new DynamicVar("Imbalance", 5m), new DynamicVar("Charge", 5m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await PowerCmd.Apply<GuardCounterPower>(Owner.Creature, 1m, Owner.Creature, this);
        await PoiseCardActions.AttackAndPoise(this, choiceContext, cardPlay.Target);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Imbalance"].UpgradeValueBy(1m);
        DynamicVars["Charge"].UpgradeValueBy(-1m);
    }
}

public sealed class Stormcaller() : ModCard(0, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
{
    public override string PortraitPath => "res://mod/images/card_portraits/stormcaller.png";

    protected override bool HasEnergyCostX => true;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<ImbalancePower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3m, ValueProp.Move), new DynamicVar("Imbalance", 2m), new DynamicVar("BonusHits", 1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int hits = ResolveEnergyXValue() + DynamicVars["BonusHits"].IntValue;
        await PoiseCardActions.AttackAllAndPoise(this, choiceContext, Math.Max(0, hits));
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars["BonusHits"].UpgradeValueBy(1m);
    }
}

public sealed class ErdtreeShock() : ModCard(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
{
    public override string PortraitPath => "res://mod/images/card_portraits/erdtree_shock.png";
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<ImbalancePower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(7m, ValueProp.Move), new CardsVar(1), new DynamicVar("Imbalance", 2m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PoiseCardActions.AttackAllAndPoise(this, choiceContext);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}

public sealed class CaestusStrike() : ModCard(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    public override string PortraitPath => "res://mod/images/card_portraits/caestus_strike.png";
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<ImbalancePower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(4m, ValueProp.Move), new DynamicVar("Imbalance", 8m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await PoiseCardActions.AttackAndPoise(this, choiceContext, cardPlay.Target);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars["Imbalance"].UpgradeValueBy(2m);
    }
}

public sealed class ShieldCrash() : ModCard(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
{
    public override string PortraitPath => "res://mod/images/card_portraits/shield_crash.png";

    public override bool GainsBlock => true;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<ImbalancePower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(4m, ValueProp.Move), new BlockVar(6m, ValueProp.Move), new DynamicVar("Imbalance", 2m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await PoiseCardActions.AttackAllAndPoise(this, choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}

public sealed class FatalStrike() : ModCard(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    public override string PortraitPath => "res://mod/images/card_portraits/fatal_strike.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(9m, ValueProp.Move), new DynamicVar("Hits", 2m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        if (!cardPlay.Target.IsStunned)
        {
            return;
        }

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(DynamicVars["Hits"].IntValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}

public sealed class GiantHunt() : ModCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    public override string PortraitPath => "res://mod/images/card_portraits/giant_hunt.png";
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<WeakPower>(), HoverTipFactory.FromPower<ImbalancePower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(5m, ValueProp.Move), new PowerVar<WeakPower>(1m), new DynamicVar("Imbalance", 3m), new DynamicVar("Hits", 2m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await PoiseCardActions.AttackAndPoise(this, choiceContext, cardPlay.Target, DynamicVars["Hits"].IntValue);
        await PowerCmd.Apply<WeakPower>(cardPlay.Target, DynamicVars.Weak.BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}

public sealed class CarianSlicer() : ModCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy), IMagicDamageCard
{
    public override string PortraitPath => "res://mod/images/card_portraits/carian_slicer.png";
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MagicPower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card == this && cardPlay.IsLastInSeries && cardPlay.ResultPile == PileType.Discard && Pile?.Type == PileType.Play)
        {
            await CardPileCmd.Add(this, PileType.Hand);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}

public sealed class StampUppercut() : ModCard(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    public override string PortraitPath => "res://mod/images/card_portraits/stamp_uppercut.png";

    public override bool GainsBlock => true;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<ImbalancePower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6m, ValueProp.Move), new BlockVar(4m, ValueProp.Move), new DynamicVar("Imbalance", 6m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await PoiseCardActions.AttackAndPoise(this, choiceContext, cardPlay.Target);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}

public sealed class ReadyStance() : ModCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override string PortraitPath => "res://mod/images/card_portraits/ready_stance.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<ImbalancePower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(4m, ValueProp.Move), new DynamicVar("Strength", 1m), new DynamicVar("Imbalance", 4m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await PoiseCardActions.AttackAndPoise(this, choiceContext, cardPlay.Target);
        await PowerCmd.Apply<StrengthPower>(Owner.Creature, DynamicVars["Strength"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Strength"].UpgradeValueBy(1m);
        DynamicVars["Imbalance"].UpgradeValueBy(1m);
    }
}

public sealed class CarianPiercer() : ModCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies), IMagicDamageCard
{
    public override string PortraitPath => "res://mod/images/card_portraits/carian_piercer.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MagicPower>(), HoverTipFactory.FromPower<WeakPower>(), HoverTipFactory.FromPower<ImbalancePower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(9m, ValueProp.Move), new PowerVar<WeakPower>(1m), new DynamicVar("Imbalance", 2m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null)
        {
            return;
        }

        await PoiseCardActions.AttackAllAndPoise(this, choiceContext);
        await PowerCmd.Apply<WeakPower>(CombatState.HittableEnemies, DynamicVars.Weak.BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars.Weak.UpgradeValueBy(1m);
    }
}

public sealed class LionClaw() : ModCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override string PortraitPath => "res://mod/images/card_portraits/lion_claw.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<ImbalancePower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(20m, ValueProp.Move), new DynamicVar("Imbalance", 6m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await PoiseCardActions.AttackAndPoise(this, choiceContext, cardPlay.Target);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(7m);
}

public sealed class TotemTablet() : ChargedModCard<TotemTabletChargePower>(0, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    public override string PortraitPath => "res://mod/images/card_portraits/totem_tablet.png";

    protected override int ChargeAmount => DynamicVars["Charge"].IntValue;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => ChargeHoverTips;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(10m, ValueProp.Move), new DynamicVar("Charge", 10m)];

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

        foreach (Creature target in targets.Where(target => target.IsAlive))
        {
            await CreatureCmd.Stun(target);
            await VictoryRushPower.Trigger(choiceContext, Owner.Creature, this);
            await BreakingMomentumPower.Trigger(choiceContext, Owner.Creature, this);
        }

        await TotemTabletKnowledgeDemonMechanic.RegisterTotemTabletRelease(choiceContext, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars["Charge"].UpgradeValueBy(-2m);
    }
}

public sealed class RockBlade() : ModCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/rock_blade.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<NextPoiseBonusPower>(), HoverTipFactory.FromPower<ImbalancePower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Imbalance", 3m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<NextPoiseBonusPower>(Owner.Creature, DynamicVars["Imbalance"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Imbalance"].UpgradeValueBy(1m);
    }
}

public sealed class GlintbladePhalanx() : ModCard(3, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/glintblade_phalanx.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<GlintbladePhalanxPower>(), HoverTipFactory.FromPower<ImbalancePower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Imbalance", 5m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<GlintbladePhalanxPower>(Owner.Creature, DynamicVars["Imbalance"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class VictoryRush() : ModCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/victory_rush.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<VictoryRushPower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Energy", 1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<VictoryRushPower>(Owner.Creature, DynamicVars["Energy"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class BreakingMomentum() : ModCard(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/breaking_momentum.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<BreakingMomentumPower>(),
        HoverTipFactory.FromPower<ExtraPoisePower>(),
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<ImbalancePower>()
    ];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Imbalance", 1m), new DynamicVar("Strength", 0m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        BreakingMomentumPower? power = await PowerCmd.Apply<BreakingMomentumPower>(Owner.Creature, DynamicVars["Imbalance"].BaseValue, Owner.Creature, this);
        power?.AddStrengthBonus(DynamicVars["Strength"].BaseValue);
    }

    protected override void OnUpgrade() => DynamicVars["Strength"].UpgradeValueBy(1m);
}
