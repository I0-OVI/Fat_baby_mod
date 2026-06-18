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

public sealed class BloodLevy() : ModCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override string PortraitPath => "res://mod/images/card_portraits/blood_levy.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<ImbalancePower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(2m, ValueProp.Move),
        new HealVar(3m),
        new DynamicVar("Hits", 3m),
        new DynamicVar("Imbalance", 1m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        var attack = DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash");

        if (DynamicVars["Hits"].IntValue > 1)
        {
            attack.WithHitCount(DynamicVars["Hits"].IntValue);
        }

        await attack.Execute(choiceContext);
        for (int i = 0; i < DynamicVars["Hits"].IntValue; i++)
        {
            await Imbalance.Reduce(choiceContext, cardPlay.Target, DynamicVars["Imbalance"].IntValue, Owner.Creature, this);
        }

        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue, false);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars.Heal.UpgradeValueBy(1m);
    }
}
