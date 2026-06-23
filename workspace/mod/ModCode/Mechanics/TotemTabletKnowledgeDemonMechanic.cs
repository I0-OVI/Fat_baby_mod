using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using FatBaby.ModCode.Cards;

namespace FatBaby.ModCode.Mechanics;

/// <summary>
/// Act 2 Hive boss: playing Totem Tablet three times within two player turns instantly kills Knowledge Demon.
/// </summary>
internal static class TotemTabletKnowledgeDemonMechanic
{
    private static readonly ModelId TotemTabletId = ModelDb.Card<TotemTablet>().Id;

    public static bool IsKnowledgeDemonBossFight(CombatState? combatState)
    {
        if (combatState?.Encounter?.RoomType != RoomType.Boss)
        {
            return false;
        }

        return combatState.Enemies.Any(enemy => enemy.Monster is KnowledgeDemon);
    }

    public static bool PlayerHasTotemTablet(Player? player)
    {
        return player?.PlayerCombatState?.AllCards.Any(card => card.Id == TotemTabletId) == true;
    }

    public static Task RegisterTotemTabletRelease(PlayerChoiceContext choiceContext, Player? player) =>
        TotemTabletKnowledgeDemonTracker.RegisterRelease(choiceContext, player);
}
