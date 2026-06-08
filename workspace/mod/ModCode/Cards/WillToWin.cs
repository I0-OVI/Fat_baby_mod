using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using Mod.ModCode.Powers;
using Mod.ModCode.Commands;

namespace Mod.ModCode.Cards;

public sealed class WillToWin() : ModCard(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/will_to_win.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<WillToWinPower>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await ModPowerCmd.Apply<WillToWinPower>(Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
