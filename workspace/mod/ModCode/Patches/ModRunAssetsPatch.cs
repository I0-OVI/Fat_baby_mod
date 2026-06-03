using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Models;

namespace Mod.ModCode.Patches;

[HarmonyPatch(typeof(PreloadManager), nameof(PreloadManager.LoadRunAssets))]
internal static class ModRunAssetsPatch
{
    [HarmonyPostfix]
    private static void EnsureModTexturesInRunSet(IEnumerable<CharacterModel> characters)
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
