using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using Mod.ModCode.Relics;

namespace Mod.ModCode.Patches;

[HarmonyPatch(typeof(TouchOfOrobas), nameof(TouchOfOrobas.GetUpgradedStarterRelic))]
internal static class TouchOfOrobasGetUpgradedStarterRelicPatch
{
    [HarmonyPostfix]
    private static void MapElementFlaskUpgrade(RelicModel starterRelic, ref RelicModel __result)
    {
        if (starterRelic is ElementFlaskRelic)
        {
            __result = ModelDb.Relic<RefinedElementFlaskRelic>().ToMutable();
        }
    }
}
