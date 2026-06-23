using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace FatBaby.ModCode.Powers;

public sealed class ScarletTemptationPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/corruption_power.tres";
    public override string CustomBigIconPath => "res://images/powers/corruption_power.png";

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner == null || player.Creature != Owner)
        {
            return;
        }

        await CardPileCmd.Draw(choiceContext, Amount, player);

        var selectableCards = CardPile.GetCards(player, [PileType.Hand]).ToList();
        if (selectableCards.Count == 0)
        {
            return;
        }

        var prefs = new CardSelectorPrefs(
            new LocString("cards", "SCARLET_TEMPTATION.prompt"),
            minCount: 1,
            maxCount: 1
        )
        {
            Cancelable = false,
            RequireManualConfirmation = true
        };

        CardModel? cardToExhaust = (await CardSelectCmd.FromHand(choiceContext, player, prefs, null, this)).FirstOrDefault();
        if (cardToExhaust == null)
        {
            return;
        }

        await CardCmd.Exhaust(choiceContext, cardToExhaust, true, false);
    }
}
