using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Mod.ModCode.Mechanics;
using Mod.ModCode.Powers;

namespace Mod.ModCode.Cards;

public sealed class PrayerStrike() : ModCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override string PortraitPath => "res://mod/images/card_portraits/prayer_strike.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ScarletCorruptionPower>(),
        HoverTipFactory.FromPower<ImbalancePower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(4m, ValueProp.Move),
        new DynamicVar("ScarletCorruption", 1m),
        new DynamicVar("Imbalance", 2m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await ScarletCorruption.Reduce(choiceContext, Owner.Creature, DynamicVars["ScarletCorruption"].IntValue, Owner.Creature, this);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        await Imbalance.Reduce(choiceContext, cardPlay.Target, DynamicVars["Imbalance"].IntValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars["ScarletCorruption"].UpgradeValueBy(1m);
    }
}
