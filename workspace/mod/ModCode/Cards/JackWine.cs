using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using FatBaby.ModCode.Powers;
using FatBaby.ModCode.Commands;

namespace FatBaby.ModCode.Cards;

public sealed class JackWine() : ModCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/jack_wine.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<NextTurnEnergyPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new HealVar(5m),
        new DynamicVar("Energy", 1m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue, false);
        await ModPowerCmd.Apply<NextTurnEnergyPower>(Owner.Creature, DynamicVars["Energy"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Heal.UpgradeValueBy(3m);
    }
}
