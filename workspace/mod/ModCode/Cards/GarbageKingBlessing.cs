using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Mod.ModCode.Powers;
using Mod.ModCode.Commands;

namespace Mod.ModCode.Cards;

public sealed class GarbageKingBlessing() : ModCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/garbage_king_blessing.png";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromKeyword(CardKeyword.Ethereal),
        HoverTipFactory.FromPower<ScarletCorruptionPower>(),
        HoverTipFactory.FromPower<GarbageKingBlessingPower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Strength", 4m),
        new DynamicVar("ScarletCorruption", 3m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await ModPowerCmd.Apply<GarbageKingBlessingPower>(Owner.Creature, DynamicVars["Strength"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Strength"].UpgradeValueBy(1m);
    }
}
