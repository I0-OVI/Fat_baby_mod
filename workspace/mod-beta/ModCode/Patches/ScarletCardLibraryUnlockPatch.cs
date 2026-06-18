using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;
using Mod.ModCode;
using Mod.ModCode.Character;

namespace Mod.ModCode.Patches;

internal static class ScarletCardLibraryUnlocks
{
    private static readonly List<Texture2D> LoadedPortraits = [];

    private static bool IsScarletOtherCard(CardModel card)
    {
        return card is { Pool: ScarletCardPool, Rarity: CardRarity.Basic or CardRarity.Ancient };
    }

    private static bool IsAncientsPoolCard(CardModel card)
    {
        return card.Rarity == CardRarity.Ancient;
    }

    private static bool IsVanillaMiscPoolCard(CardModel card)
    {
        CardRarity rarity = card.Rarity;
        return (uint)(rarity - 6) <= 4u;
    }

    private static bool IsVanillaOtherRarity(CardModel card)
    {
        CardRarity rarity = card.Rarity;
        bool isCommonUncommonRare = (uint)(rarity - 2) <= 2u;
        return !isCommonUncommonRare;
    }

    public static bool IsScarletCard(CardModel card)
    {
        return GetCardIds().Contains(card.Id);
    }

    public static IEnumerable<CardModel> GetCards()
    {
        try
        {
            if (!ModelDb.Contains(typeof(ScarletCardPool)))
            {
                return [];
            }

            return ModelDb.CardPool<ScarletCardPool>().AllCards;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"Unable to resolve Scarlet card library cards: {ex}");
            return [];
        }
    }

    public static HashSet<ModelId> GetCardIds()
    {
        return GetCards().Select(card => card.Id).ToHashSet();
    }

    public static void MarkProgressDiscovered()
    {
        try
        {
            bool changed = false;
            ProgressState progress = SaveManager.Instance.Progress;
            foreach (ModelId cardId in GetCardIds())
            {
                changed |= progress.MarkCardAsSeen(cardId);
            }

            if (changed)
            {
                SaveManager.Instance.SaveProgressFile();
                MainFile.Logger.Info("Marked all Scarlet cards as discovered for the card library.");
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"Unable to mark Scarlet cards as discovered: {ex}");
        }
    }

    public static void LoadPortraits()
    {
        LoadedPortraits.Clear();

        foreach (string path in GetCards().SelectMany(card => card.AllPortraitPaths).Distinct())
        {
            try
            {
                Texture2D? portrait = ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
                if (portrait != null)
                {
                    LoadedPortraits.Add(portrait);
                }
                else
                {
                    MainFile.Logger.Info($"Unable to load Scarlet card library portrait: {path}");
                }
            }
            catch (Exception ex)
            {
                MainFile.Logger.Info($"Unable to load Scarlet card library portrait {path}: {ex}");
            }
        }
    }

    public static void ReleasePortraits()
    {
        LoadedPortraits.Clear();
    }

    public static void ConfigureOtherCardFilters(NCardLibrary library)
    {
        try
        {
            if (AccessTools.Field(typeof(NCardLibrary), "_poolFilters")?.GetValue(library) is Dictionary<NCardPoolFilter, Func<CardModel, bool>> poolFilters
                && AccessTools.Field(typeof(NCardLibrary), "_miscPoolFilter")?.GetValue(library) is NCardPoolFilter miscPoolFilter)
            {
                poolFilters[miscPoolFilter] = card => IsVanillaMiscPoolCard(card) || IsScarletOtherCard(card);
                if (AccessTools.Field(typeof(NCardLibrary), "_ancientsFilter")?.GetValue(library) is NCardPoolFilter ancientsFilter)
                {
                    poolFilters[ancientsFilter] = IsAncientsPoolCard;
                }
            }

            if (AccessTools.Field(typeof(NCardLibrary), "_rarityFilters")?.GetValue(library) is Dictionary<NCardRarityTickbox, Func<CardModel, bool>> rarityFilters
                && AccessTools.Field(typeof(NCardLibrary), "_otherFilter")?.GetValue(library) is NCardRarityTickbox otherFilter)
            {
                rarityFilters[otherFilter] = card => card.Pool is ScarletCardPool
                    ? card.Rarity is CardRarity.Basic or CardRarity.Ancient
                    : IsVanillaOtherRarity(card);
            }

            if (ModelDb.Contains(typeof(ScarletAcolyte))
                && AccessTools.Field(typeof(NCardLibrary), "_cardPoolFilters")?.GetValue(library) is Dictionary<CharacterModel, NCardPoolFilter> cardPoolFilters
                && AccessTools.Field(typeof(NCardLibrary), "_miscPoolFilter")?.GetValue(library) is NCardPoolFilter scarletFallbackFilter)
            {
                cardPoolFilters[ModelDb.Character<ScarletAcolyte>()] = scarletFallbackFilter;
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"Unable to configure Scarlet other-card library filters: {ex}");
        }
    }
}

[HarmonyPatch(typeof(ProgressState), "get_DiscoveredCards")]
internal static class ScarletDiscoveredCardsPatch
{
    private static void Postfix(ref IReadOnlySet<ModelId> __result)
    {
        HashSet<ModelId> cardIds = ScarletCardLibraryUnlocks.GetCardIds();
        if (cardIds.Count == 0 || cardIds.All(__result.Contains))
        {
            return;
        }

        __result = __result.Concat(cardIds).ToHashSet();
    }
}

[HarmonyPatch(typeof(UnlockState), "get_Cards")]
internal static class ScarletUnlockStateCardsPatch
{
    private static void Postfix(ref IEnumerable<CardModel> __result)
    {
        IReadOnlyList<CardModel> scarletCards = ScarletCardLibraryUnlocks.GetCards().ToList();
        if (scarletCards.Count == 0)
        {
            return;
        }

        __result = __result.Concat(scarletCards)
            .GroupBy(card => card.Id)
            .Select(group => group.First());
    }
}

[HarmonyPatch(typeof(NCardLibraryGrid), nameof(NCardLibraryGrid._Ready))]
internal static class ScarletCardLibraryGridReadyPatch
{
    private static void Prefix()
    {
        ScarletCardLibraryUnlocks.LoadPortraits();
        ScarletCardLibraryUnlocks.MarkProgressDiscovered();
    }
}

[HarmonyPatch(typeof(NCardLibrary), nameof(NCardLibrary._Ready))]
internal static class ScarletCardLibraryScreenReadyPatch
{
    private static void Postfix(NCardLibrary __instance)
    {
        ScarletCardLibraryUnlocks.ConfigureOtherCardFilters(__instance);
    }
}

[HarmonyPatch(typeof(NCardLibrary), nameof(NCardLibrary.OnSubmenuClosed))]
internal static class ScarletCardLibraryScreenClosedPatch
{
    private static void Postfix()
    {
        ScarletCardLibraryUnlocks.ReleasePortraits();
    }
}

[HarmonyPatch(typeof(NCardLibraryGrid), "GetCardVisibility")]
internal static class ScarletCardLibraryVisibilityPatch
{
    private static void Postfix(CardModel card, ref ModelVisibility __result)
    {
        if (ScarletCardLibraryUnlocks.IsScarletCard(card))
        {
            __result = ModelVisibility.Visible;
        }
    }
}

/// <summary>
/// Virtualized card rows call UpdateStats without EnsureCardLibraryStatsExists, which breaks scroll-back.
/// </summary>
[HarmonyPatch(typeof(NCardLibraryGrid), "AssignCardsToRow")]
internal static class ScarletCardLibraryAssignCardsToRowPatch
{
    [HarmonyPrefix]
    private static void Prefix(List<NGridCardHolder> row)
    {
        foreach (NGridCardHolder item in row)
        {
            item.EnsureCardLibraryStatsExists();
        }
    }
}
