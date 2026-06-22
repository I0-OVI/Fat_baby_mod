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
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;
using Mod.ModCode.Mechanics;
using Mod.ModCode.Powers;
using Mod.ModCode.Commands;

namespace Mod.ModCode.Cards;

internal static class MagicCardActions
{
    public static async Task MagicAttack(ModCard card, PlayerChoiceContext choiceContext, Creature target, int hits = 1, bool playAnim = true)
    {
        var attack = DamageCmd.Attack(card.DynamicVars.Damage.BaseValue)
            .FromCard(card)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash");

        if (hits > 1)
        {
            attack.WithHitCount(hits);
        }

        if (!playAnim)
        {
            attack.WithNoAttackerAnim();
        }

        await attack.Execute(choiceContext);
    }

    public static async Task MagicRandomMultiHit(ModCard card, PlayerChoiceContext choiceContext, int hits, bool playAnim = true)
    {
        if (card.CombatState == null || hits <= 0)
        {
            return;
        }

        var attack = DamageCmd.Attack(card.DynamicVars.Damage.BaseValue)
            .WithHitCount(hits)
            .FromCard(card)
            .TargetingRandomOpponents(card.CombatState)
            .WithHitFx("vfx/vfx_attack_slash");

        if (!playAnim)
        {
            attack.WithNoAttackerAnim();
        }

        await attack.Execute(choiceContext);
    }

    public static async Task MagicAttackAndPoise(ModCard card, PlayerChoiceContext choiceContext, Creature target, int hits = 1)
    {
        await MagicAttack(card, choiceContext, target, hits);
        for (int i = 0; i < hits; i++)
        {
            await Imbalance.Reduce(choiceContext, target, card.DynamicVars["Imbalance"].IntValue, card.Owner.Creature, card);
        }
    }

    public static Creature? RandomEnemy(Creature owner)
    {
        IReadOnlyList<Creature> enemies = owner.CombatState?.HittableEnemies.Where(enemy => enemy.IsAlive).ToList() ?? [];
        if (enemies.Count == 0)
        {
            return null;
        }

        return owner.Player?.RunState.Rng.CombatTargets.NextItem(enemies) ?? enemies[0];
    }

    public static decimal MaxHpPercentDamage(Creature target, decimal percent) =>
        Math.Max(1m, Math.Floor(target.MaxHp * percent / 100m));

    public static async Task MagicMaxHpPercentAttack(ModCard card, PlayerChoiceContext choiceContext, Creature target, decimal percent)
    {
        decimal damage = MaxHpPercentDamage(target, percent);
        await DamageCmd.Attack(damage)
            .FromCard(card)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    public static decimal GetModifiedPrimaryDamage(ModCard card, Creature primaryTarget)
    {
        ArgumentNullException.ThrowIfNull(card.CombatState);

        decimal damage = Hook.ModifyDamage(
            card.Owner.RunState,
            card.CombatState,
            primaryTarget,
            card.Owner.Creature,
            card.DynamicVars.Damage.BaseValue,
            ValueProp.Move,
            card,
            ModifyDamageHookType.All,
            CardPreviewMode.None,
            out IEnumerable<AbstractModel> modifiers);

        VigorPower? vigor = card.Owner?.Creature.GetPower<VigorPower>();
        if (vigor != null && vigor.Amount > 0 && !modifiers.Any(modifier => modifier is VigorPower))
        {
            damage += vigor.Amount;
        }

        return damage;
    }

    public static decimal SplashDamageFromPrimary(decimal primaryDamage) =>
        Math.Max(1m, Math.Floor(primaryDamage / 2m));

    public static async Task MagicAttackThenSplash(
        ModCard card,
        PlayerChoiceContext choiceContext,
        Creature primaryTarget,
        int hits = 1,
        int splashCount = 1)
    {
        decimal primaryDamage = 0m;
        var captureDone = false;

        var attack = DamageCmd.Attack(card.DynamicVars.Damage.BaseValue)
            .FromCard(card)
            .Targeting(primaryTarget)
            .WithHitFx("vfx/vfx_attack_slash")
            .BeforeDamage(() =>
            {
                if (!captureDone)
                {
                    primaryDamage = GetModifiedPrimaryDamage(card, primaryTarget);
                    captureDone = true;
                }

                return Task.CompletedTask;
            });

        if (hits > 1)
        {
            attack.WithHitCount(hits);
        }

        await attack.Execute(choiceContext);

        if (!captureDone)
        {
            primaryDamage = GetModifiedPrimaryDamage(card, primaryTarget);
        }

        for (int i = 0; i < splashCount; i++)
        {
            await Splash(card, choiceContext, primaryTarget, primaryDamage);
        }
    }

    public static async Task Splash(ModCard card, PlayerChoiceContext choiceContext, Creature primaryTarget, decimal primaryDamage)
    {
        if (card.CombatState == null)
        {
            return;
        }

        decimal splashDamage = SplashDamageFromPrimary(primaryDamage);
        IEnumerable<Creature> splashTargets = card.CombatState.HittableEnemies.Where(enemy => enemy != primaryTarget && enemy.IsAlive);
        foreach (Creature target in splashTargets)
        {
            await DamageCmd.Attack(splashDamage)
                .FromCard(card)
                .Targeting(target)
                .Unpowered()
                .WithNoAttackerAnim()
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }
    }
}

public sealed class RockBall() : ModCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy), IMagicDamageCard
{
    public override string PortraitPath => "res://mod/images/card_portraits/rock_ball.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MagicPower>(), HoverTipFactory.FromPower<ImbalancePower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, ValueProp.Move), new DynamicVar("Hits", 3m), new DynamicVar("Imbalance", 2m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await MagicCardActions.MagicAttackAndPoise(this, choiceContext, cardPlay.Target, DynamicVars["Hits"].IntValue);
    }

    protected override void OnUpgrade() => DynamicVars["Hits"].UpgradeValueBy(1m);
}

public sealed class GlintstoneChunk() : ModCard(0, CardType.Attack, CardRarity.Common, TargetType.AllEnemies), IMagicDamageCard
{
    public override string PortraitPath => "res://mod/images/card_portraits/glintstone_chunk.png";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MagicPower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, ValueProp.Move), new DynamicVar("Hits", 6m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await MagicCardActions.MagicRandomMultiHit(this, choiceContext, DynamicVars["Hits"].IntValue);
    }

    protected override void OnUpgrade() => DynamicVars["Hits"].UpgradeValueBy(2m);
}

public sealed class HoulouGroundSlam() : ModCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    public override string PortraitPath => "res://mod/images/card_portraits/houlou_ground_slam.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<ImbalancePower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5m, ValueProp.Move),
        new DynamicVar("Hits", 2m),
        new DynamicVar("Imbalance", 3m),
        new DynamicVar("NextDamage", 6m),
        new DynamicVar("NextImbalance", 4m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PoiseCardActions.AttackAllAndPoise(this, choiceContext, DynamicVars["Hits"].IntValue);
        HoulouGroundSlamPower? power = await ModPowerCmd.Apply<HoulouGroundSlamPower>(Owner.Creature, DynamicVars["NextDamage"].BaseValue, Owner.Creature, this);
        power?.SetPoise(DynamicVars["NextImbalance"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars["NextDamage"].UpgradeValueBy(1m);
    }
}

public sealed class CometShard() : ModCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy), IMagicDamageCard
{
    public override string PortraitPath => "res://mod/images/card_portraits/comet_shard.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MagicPower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(9m, ValueProp.Move), new DynamicVar("Magic", 2m), new DynamicVar("CostReduction", 1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await MagicCardActions.MagicAttack(this, choiceContext, cardPlay.Target);
        await ModPowerCmd.Apply<MagicPower>(Owner.Creature, DynamicVars["Magic"].BaseValue, Owner.Creature, this);
        EnergyCost.AddThisCombat(-DynamicVars["CostReduction"].IntValue, reduceOnly: true);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars["Magic"].UpgradeValueBy(1m);
    }
}

public sealed class NightComet() : ModCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy), IMagicDamageCard
{
    public override string PortraitPath => "res://mod/images/card_portraits/night_comet.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MagicPower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(8m, ValueProp.Move), new DynamicVar("BonusDamage", 4m), new DynamicVar("BonusHits", 2m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        Creature target = cardPlay.Target;
        await MagicCardActions.MagicAttack(this, choiceContext, target);

        if (!target.IsDead)
        {
            return;
        }

        for (int i = 0; i < DynamicVars["BonusHits"].IntValue; i++)
        {
            Creature? bonusTarget = MagicCardActions.RandomEnemy(Owner.Creature);
            if (bonusTarget == null)
            {
                return;
            }

            await DamageCmd.Attack(DynamicVars["BonusDamage"].BaseValue)
                .FromCard(this)
                .Targeting(bonusTarget)
                .WithNoAttackerAnim()
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars["BonusDamage"].UpgradeValueBy(1m);
    }
}

public sealed class CrystalBurst() : ModCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy), IMagicDamageCard
{
    public override string PortraitPath => "res://mod/images/card_portraits/crystal_burst.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MagicPower>(), SplashHoverTip.Get()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(4m, ValueProp.Move), new DynamicVar("Hits", 2m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await MagicCardActions.MagicAttackThenSplash(
            this,
            choiceContext,
            cardPlay.Target,
            DynamicVars["Hits"].IntValue,
            DynamicVars["Hits"].IntValue);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}

public sealed class LorettasGreatbow() : ModCard(3, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy), IMagicDamageCard
{
    public override string PortraitPath => "res://mod/images/card_portraits/lorettas_greatbow.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MagicPower>(), SplashHoverTip.Get()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(24m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await MagicCardActions.MagicAttackThenSplash(this, choiceContext, cardPlay.Target);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(8m);
}

public sealed class AncientDeathsRancor() : ModCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies), IMagicDamageCard
{
    public override string PortraitPath => "res://mod/images/card_portraits/ancient_deaths_rancor.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MagicPower>(), HoverTipFactory.FromPower<AncientDeathsRancorPower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, ValueProp.Move), new DynamicVar("Hits", 6m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await MagicCardActions.MagicRandomMultiHit(this, choiceContext, DynamicVars["Hits"].IntValue);

        if (!AllInReplayTracker.IsReplayCopy(this))
        {
            AncientDeathsRancorPower? power = await ModPowerCmd.Apply<AncientDeathsRancorPower>(Owner.Creature, 1m, Owner.Creature, this);
            power?.Configure(DynamicVars["Hits"].IntValue, DynamicVars.Damage.BaseValue, this);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Hits"].UpgradeValueBy(1m);
}

public sealed class PunishingThorns() : ModCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies), IMagicDamageCard
{
    public override string PortraitPath => "res://mod/images/card_portraits/punishing_thorns.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MagicPower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(9m, ValueProp.Move), new DynamicVar("HpLoss", 1m), new DynamicVar("Magic", 1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null)
        {
            return;
        }

        await CreatureCmd.Damage(choiceContext, Owner.Creature, DynamicVars["HpLoss"].BaseValue, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        await ModPowerCmd.Apply<MagicPower>(Owner.Creature, DynamicVars["Magic"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars["Magic"].UpgradeValueBy(1m);
    }
}

public sealed class CarianRetribution() : ModCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self), IMagicAttributeCard
{
    public override string PortraitPath => "res://mod/images/card_portraits/carian_retribution.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<CarianRetributionPower>(),
        StunIntent.GetStaticHoverTip()
    ];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Blocks", 1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CarianRetributionPower? power = await ModPowerCmd.Apply<CarianRetributionPower>(
            Owner.Creature,
            DynamicVars["Blocks"].BaseValue,
            Owner.Creature,
            this);
        power?.SetSourceCard(this);
    }

    protected override void OnUpgrade() => DynamicVars["Blocks"].UpgradeValueBy(1m);
}

public sealed class ProfoundWisdom() : ModCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/profound_wisdom.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MagicPower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Magic", 2m), new CardsVar(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await ModPowerCmd.Apply<MagicPower>(Owner.Creature, DynamicVars["Magic"].BaseValue, Owner.Creature, this);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(2m);
}

public sealed class AdulasMoonblade() : ModCard(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy), IMagicDamageCard
{
    public override string PortraitPath => "res://mod/images/card_portraits/adulas_moonblade.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<MagicPower>(),
        SplashHoverTip.Get(),
        HoverTipFactory.FromPower<FrostbitePower>(),
        HoverTipFactory.FromPower<AdulasMoonbladePower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(20m, ValueProp.Move),
        new DynamicVar("Repeats", 1m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await ExecuteEffect(this, choiceContext, cardPlay.Target);
        if (!AllInReplayTracker.IsReplayCopy(this))
        {
            AdulasMoonbladePower? power = await ModPowerCmd.Apply<AdulasMoonbladePower>(
                Owner.Creature,
                DynamicVars["Repeats"].BaseValue,
                Owner.Creature,
                this);
            power?.SetSourceCard(this);
        }
    }

    internal static async Task ExecuteEffect(AdulasMoonblade card, PlayerChoiceContext choiceContext, Creature target)
    {
        await MagicCardActions.MagicAttackThenSplash(card, choiceContext, target);
        await FrostbiteMechanic.Apply(choiceContext, [target], card.Owner.Creature, card);
    }

    protected override void OnUpgrade() => DynamicVars["Repeats"].UpgradeValueBy(1m);
}

public sealed class SpiralGlintstone() : ModCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy), IMagicAttributeCard
{
    public override string PortraitPath => "res://mod/images/card_portraits/spiral_glintstone.png";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("HpPercent", 10m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await MagicCardActions.MagicMaxHpPercentAttack(this, choiceContext, cardPlay.Target, DynamicVars["HpPercent"].BaseValue);
    }

    protected override void OnUpgrade() => DynamicVars["HpPercent"].UpgradeValueBy(2m);
}

public sealed class CometAzur() : ModCard(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy), IMagicDamageCard
{
    public override string PortraitPath => "res://mod/images/card_portraits/comet_azur.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MagicPower>(), HoverTipFactory.FromPower<StrengthPower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(5m, ValueProp.Move), new DynamicVar("Strength", 3m), new DynamicVar("Magic", 3m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        List<CardModel> drawPile = PileType.Draw.GetPile(Owner).Cards.ToList();
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 0, drawPile.Count) { Cancelable = true };
        List<CardModel> selectedCards = (await CardSelectCmd.FromSimpleGrid(choiceContext, drawPile, Owner, prefs)).ToList();

        foreach (CardModel selectedCard in selectedCards)
        {
            await CardCmd.Exhaust(choiceContext, selectedCard);
            if (cardPlay.Target.IsAlive)
            {
                await MagicCardActions.MagicAttack(this, choiceContext, cardPlay.Target, playAnim: selectedCard == selectedCards.First());
            }
        }

        await ModPowerCmd.Apply<StrengthPower>(Owner.Creature, -DynamicVars["Strength"].BaseValue, Owner.Creature, this);
        await ModPowerCmd.Apply<MagicPower>(Owner.Creature, -DynamicVars["Magic"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
