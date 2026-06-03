using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Mod.ModCode.Mechanics;
using Mod.ModCode.Powers;

namespace Mod.ModCode.Cards;

public sealed class ScarletTemptation() : ModCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/scarlet_temptation.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ScarletCorruptionPower>(),
        HoverTipFactory.FromPower<ScarletTemptationPower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Cards", 2m),
        new DynamicVar("ScarletCorruption", 3m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<ScarletTemptationPower>(Owner.Creature, DynamicVars["Cards"].BaseValue, Owner.Creature, this);
        await ScarletCorruption.Apply(choiceContext, Owner.Creature, DynamicVars["ScarletCorruption"].IntValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}
