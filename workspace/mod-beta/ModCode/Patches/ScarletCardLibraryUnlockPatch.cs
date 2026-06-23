using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;
using FatBaby.ModCode;
using FatBaby.ModCode.Character;

namespace FatBaby.ModCode.Patches;

internal static class ScarletCardLibraryUnlocks
{
    private const string LegacyScarletPoolFilterName = "ScarletPool";

    private static readonly List<Texture2D> LoadedPortraits = [];

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

    private static bool IsScarletPoolCard(CardModel card) => card.Pool is ScarletCardPool;

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

    public static IEnumerable<string> GetPortraitPaths() =>
        GetCards().SelectMany(card => card.AllPortraitPaths).Distinct();

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
        foreach (string path in GetPortraitPaths())
        {
            try
            {
                Texture2D? portrait = ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
                if (portrait == null)
                {
                    MainFile.Logger.Info($"Unable to load Scarlet card library portrait: {path}");
                    continue;
                }

                if (!LoadedPortraits.Contains(portrait))
                {
                    LoadedPortraits.Add(portrait);
                }
            }
            catch (Exception ex)
            {
                MainFile.Logger.Info($"Unable to load Scarlet card library portrait {path}: {ex}");
            }
        }
    }

    public static void ResetGridScroll(NCardLibrary library)
    {
        if (AccessTools.Field(typeof(NCardLibrary), "_grid")?.GetValue(library) is not NCardLibraryGrid grid)
        {
            return;
        }

        AccessTools.Field(typeof(NCardGrid), "_slidingWindowCardIndex")?.SetValue(grid, 0);
        AccessTools.Field(typeof(NCardGrid), "_targetDrag")?.SetValue(grid, 0f);
        grid.SetScrollPosition(0f);
    }

    public static void ConfigureOtherCardFilters(NCardLibrary library)
    {
        try
        {
            if (AccessTools.Field(typeof(NCardLibrary), "_poolFilters")?.GetValue(library) is Dictionary<NCardPoolFilter, Func<CardModel, bool>> poolFilters
                && AccessTools.Field(typeof(NCardLibrary), "_miscPoolFilter")?.GetValue(library) is NCardPoolFilter miscPoolFilter)
            {
                poolFilters[miscPoolFilter] = IsVanillaMiscPoolCard;
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

            ConfigureScarletCharacterPoolFilter(library);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"Unable to configure Scarlet other-card library filters: {ex}");
        }
    }

    private static void ConfigureScarletCharacterPoolFilter(NCardLibrary library)
    {
        if (!ModelDb.Contains(typeof(ScarletAcolyte))
            || AccessTools.Field(typeof(NCardLibrary), "_poolFilters")?.GetValue(library) is not Dictionary<NCardPoolFilter, Func<CardModel, bool>> poolFilters
            || AccessTools.Field(typeof(NCardLibrary), "_cardPoolFilters")?.GetValue(library) is not Dictionary<CharacterModel, NCardPoolFilter> cardPoolFilters
            || !cardPoolFilters.TryGetValue(ModelDb.Character<ScarletAcolyte>(), out NCardPoolFilter? scarletFilter))
        {
            return;
        }

        RemoveLegacyScarletPoolDuplicate(library, poolFilters);
        poolFilters[scarletFilter] = IsScarletPoolCard;
    }

    private static void RemoveLegacyScarletPoolDuplicate(
        NCardLibrary library,
        Dictionary<NCardPoolFilter, Func<CardModel, bool>> poolFilters)
    {
        if (AccessTools.Field(typeof(NCardLibrary), "_miscPoolFilter")?.GetValue(library) is not NCardPoolFilter miscPoolFilter)
        {
            return;
        }

        NCardPoolFilter? legacyFilter = miscPoolFilter.GetParent()
            .GetNodeOrNull<NCardPoolFilter>(LegacyScarletPoolFilterName);
        if (legacyFilter == null)
        {
            return;
        }

        poolFilters.Remove(legacyFilter);
        legacyFilter.QueueFree();
    }
}

[HarmonyPatch(typeof(NCapstoneSubmenuStack), nameof(NCapstoneSubmenuStack.ShowScreen))]
internal static class ScarletCompendiumRunStatePatch
{
    private static void Postfix(CapstoneSubmenuType type, NSubmenu __result)
    {
        if (type != CapstoneSubmenuType.Compendium || __result is not NCompendiumSubmenu compendium)
        {
            return;
        }

        RunState? runState = RunManager.Instance.DebugOnlyGetState();
        if (runState == null)
        {
            return;
        }

        compendium.Initialize(runState);
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

[HarmonyPatch(typeof(NCardLibrary), nameof(NCardLibrary.OnSubmenuOpened))]
internal static class ScarletCardLibraryOpenedPatch
{
    private static void Prefix(NCardLibrary __instance)
    {
        ScarletCardLibraryUnlocks.ConfigureOtherCardFilters(__instance);
        ScarletCardLibraryUnlocks.LoadPortraits();
        ScarletCardLibraryUnlocks.ResetGridScroll(__instance);
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

    [HarmonyPostfix]
    private static void Postfix(NCardLibraryGrid __instance, List<NGridCardHolder> row)
    {
        HashSet<ModelId>? seenCards = AccessTools.Field(typeof(NCardLibraryGrid), "_seenCards")?.GetValue(__instance) as HashSet<ModelId>;
        foreach (NGridCardHolder item in row)
        {
            if (item.CardNode?.Model is not CardModel model)
            {
                continue;
            }

            item.EnsureCardLibraryStatsExists();
            item.CardLibraryStats?.UpdateStats(model);
            if (seenCards != null)
            {
                item.Hitbox.MouseDefaultCursorShape = seenCards.Contains(model.Id)
                    ? Control.CursorShape.PointingHand
                    : Control.CursorShape.Arrow;
            }
        }
    }
}

/// <summary>
/// The card library recycles grid holders while scrolling. When scrolling back up from
/// the bottom, reused holders can update stats before their stats node exists.
/// </summary>
[HarmonyPatch]
internal static class ScarletCardLibraryHolderUpdateStatsPatch
{
    private static MethodBase? TargetMethod() => AccessTools.Method(typeof(NGridCardHolder), "UpdateStats");

    private static bool Prepare() => TargetMethod() != null;

    [HarmonyPrefix]
    private static void Prefix(NGridCardHolder __instance)
    {
        __instance.EnsureCardLibraryStatsExists();
    }
}

/// <summary>
/// In combat capstone overlays, GlobalPosition can include transient combat UI transforms.
/// Drive card-library virtualization from the scroll container offset instead.
/// </summary>
[HarmonyPatch]
internal static class ScarletCardLibraryReallocateRowsPatch
{
    private const float CardPadding = 40f;
    private const float TopMargin = 80f;

    private static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (string methodName in new[] { "ReallocateAll", "ReallocateAbove", "ReallocateBelow" })
        {
            MethodInfo? method = AccessTools.Method(typeof(NCardGrid), methodName);
            if (method != null)
            {
                yield return method;
            }
        }
    }

    [HarmonyPrefix]
    private static bool Prefix(NCardGrid __instance)
    {
        if (__instance is not NCardLibraryGrid)
        {
            return true;
        }

        try
        {
            ReconcileRows(__instance);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"Unable to reconcile Scarlet card library rows: {ex}");
        }

        return false;
    }

    private static void ReconcileRows(NCardGrid grid)
    {
        if (AccessTools.Field(typeof(NCardGrid), "_cardRows")?.GetValue(grid) is not List<List<NGridCardHolder>> rows
            || rows.Count == 0
            || rows[0].Count == 0
            || AccessTools.Field(typeof(NCardGrid), "_cards")?.GetValue(grid) is not List<CardModel> cards
            || cards.Count == 0
            || AccessTools.Field(typeof(NCardGrid), "_scrollContainer")?.GetValue(grid) is not Control scrollContainer
            || AccessTools.Field(typeof(NCardGrid), "_cardSize")?.GetValue(grid) is not Vector2 cardSize)
        {
            return;
        }

        int columns = AccessTools.PropertyGetter(typeof(NCardGrid), "Columns")?.Invoke(grid, []) is int value
            ? value
            : 0;
        float rowHeight = cardSize.Y + CardPadding;
        if (columns <= 0 || rowHeight <= 0f)
        {
            return;
        }

        float contentTop = -scrollContainer.Position.Y;
        int firstVisibleRow = Mathf.FloorToInt((contentTop - grid.YOffset - TopMargin) / rowHeight);
        int desiredFirstRow = Math.Max(0, firstVisibleRow - 1);
        int totalRows = Mathf.CeilToInt((float)cards.Count / columns);
        int maxFirstRow = Math.Max(0, totalRows - rows.Count);
        desiredFirstRow = Math.Min(desiredFirstRow, maxFirstRow);

        int desiredIndex = desiredFirstRow * columns;
        int currentIndex = AccessTools.Field(typeof(NCardGrid), "_slidingWindowCardIndex")?.GetValue(grid) is int current
            ? current
            : -1;
        if (currentIndex == desiredIndex && RowsMatch(cards, rows, desiredIndex, columns))
        {
            return;
        }

        AccessTools.Field(typeof(NCardGrid), "_slidingWindowCardIndex")?.SetValue(grid, desiredIndex);
        MethodInfo? assignCardsToRow = AccessTools.Method(grid.GetType(), "AssignCardsToRow")
            ?? AccessTools.Method(typeof(NCardGrid), "AssignCardsToRow");
        MethodInfo? updateGridPositions = AccessTools.Method(typeof(NCardGrid), "UpdateGridPositions");
        MethodInfo? updateGridNavigation = AccessTools.Method(grid.GetType(), "UpdateGridNavigation")
            ?? AccessTools.Method(typeof(NCardGrid), "UpdateGridNavigation");

        for (int i = 0; i < rows.Count; i++)
        {
            assignCardsToRow?.Invoke(grid, [rows[i], desiredIndex + i * columns]);
        }

        updateGridPositions?.Invoke(grid, [desiredIndex]);
        updateGridNavigation?.Invoke(grid, []);
    }

    private static bool RowsMatch(List<CardModel> cards, List<List<NGridCardHolder>> rows, int startIndex, int columns)
    {
        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            List<NGridCardHolder> row = rows[rowIndex];
            for (int column = 0; column < row.Count; column++)
            {
                int cardIndex = startIndex + rowIndex * columns + column;
                NGridCardHolder holder = row[column];
                if (cardIndex >= cards.Count)
                {
                    if (holder.Visible)
                    {
                        return false;
                    }

                    continue;
                }

                if (!holder.Visible || holder.CardModel != cards[cardIndex])
                {
                    return false;
                }
            }
        }

        return true;
    }
}
