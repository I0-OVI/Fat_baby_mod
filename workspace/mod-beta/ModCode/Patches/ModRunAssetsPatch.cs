using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Models;

namespace Mod.ModCode.Patches;

internal static class ModRunAssetsPatchLogic
{
    internal static void EnsurePersistentAssetsInRunSet()
    {
        if (AssetSets.RunSet == null)
        {
            return;
        }

        HashSet<string> expanded = new(AssetSets.RunSet);
        foreach (string path in ModRunAssets.PersistentTexturePaths)
        {
            expanded.Add(path);
        }

        AssetSets.RunSet = expanded;
    }
}

[HarmonyPatch(typeof(PreloadManager), nameof(PreloadManager.LoadRunAssets))]
internal static class ModRunAssetsLoadRunPatch
{
    [HarmonyPostfix]
    private static void Postfix() => ModRunAssetsPatchLogic.EnsurePersistentAssetsInRunSet();
}

/// <summary>
/// Elite and other combat rooms reload assets here; without this postfix mod combat visuals can unload
/// and both the player and enemies fail to appear.
/// </summary>
[HarmonyPatch(typeof(PreloadManager), nameof(PreloadManager.LoadRoomCombatAssets))]
internal static class ModRunAssetsLoadRoomCombatPatch
{
    [HarmonyPostfix]
    private static void Postfix() => ModRunAssetsPatchLogic.EnsurePersistentAssetsInRunSet();
}

[HarmonyPatch(typeof(PreloadManager), nameof(PreloadManager.LoadActAssets))]
internal static class ModRunAssetsLoadActPatch
{
    [HarmonyPostfix]
    private static void Postfix() => ModRunAssetsPatchLogic.EnsurePersistentAssetsInRunSet();
}
