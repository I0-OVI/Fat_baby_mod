using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using FatBaby.ModCode.Cards;

namespace FatBaby.ModCode.Patches;

/// <summary>
/// Element Flask cannot be discarded or exhausted by other effects.
/// </summary>
[HarmonyPatch]
internal static class ElementFlaskCardPatch
{
    [HarmonyPatch(typeof(CardSelectCmd), nameof(CardSelectCmd.FromHand))]
    [HarmonyPrefix]
    private static void ExcludeFromHandSelection(ref Func<CardModel, bool>? filter)
    {
        Func<CardModel, bool>? previous = filter;
        filter = card => card is not ElementFlask && (previous == null || previous(card));
    }

    [HarmonyPatch(typeof(CardCmd), nameof(CardCmd.Discard), typeof(PlayerChoiceContext), typeof(IEnumerable<CardModel>))]
    [HarmonyPrefix]
    private static void ExcludeFromDiscard(ref IEnumerable<CardModel> cards)
    {
        cards = cards.Where(card => card is not ElementFlask).ToList();
    }

    [HarmonyPatch(typeof(CardCmd), nameof(CardCmd.Exhaust), typeof(PlayerChoiceContext), typeof(CardModel), typeof(bool), typeof(bool))]
    [HarmonyPrefix]
    private static bool BlockExhaust(CardModel card)
    {
        if (card is ElementFlask)
        {
            return false;
        }

        return true;
    }
}
