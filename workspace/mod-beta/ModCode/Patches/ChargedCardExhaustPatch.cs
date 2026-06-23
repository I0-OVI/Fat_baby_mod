using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using FatBaby.ModCode.Cards;

namespace FatBaby.ModCode.Patches;

/// <summary>
/// Restart charged-card timers when a card is exhausted without being played.
/// Hooks on the card model can miss the combat clone instance (deck canonical vs combat clone),
/// so we drive charge directly from <see cref="CardCmd.Exhaust"/> on the card being moved.
/// </summary>
[HarmonyPatch(typeof(CardCmd), nameof(CardCmd.Exhaust), typeof(PlayerChoiceContext), typeof(CardModel), typeof(bool), typeof(bool))]
internal static class ChargedCardExhaustPatch
{
    [HarmonyPrefix]
    private static void CaptureOldPile(CardModel card, ref PileType __state)
    {
        __state = card.Pile?.Type ?? PileType.None;
    }

    [HarmonyPostfix]
    private static void AfterExhaust(CardModel card, PileType __state, ref Task __result)
    {
        __result = RestartChargeAfterExhaust(__result ?? Task.CompletedTask, card, __state);
    }

    private static async Task RestartChargeAfterExhaust(Task originalTask, CardModel card, PileType oldPileType)
    {
        await originalTask;

        if (card is not IChargedCard charged || card.Pile?.Type != PileType.Exhaust || oldPileType == PileType.Exhaust)
        {
            return;
        }

        await charged.OnEnteredExhaustFromOtherPileAsync(oldPileType, showInSidebar: true);
    }
}
