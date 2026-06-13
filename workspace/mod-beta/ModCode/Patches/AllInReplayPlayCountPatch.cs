using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using Mod.ModCode.Mechanics;

namespace Mod.ModCode.Patches;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.GetEnchantedReplayCount))]
internal static class AllInReplayPlayCountPatch
{
    [HarmonyPostfix]
    private static void Postfix(CardModel __instance, ref int __result)
    {
        if (AllInReplayTracker.IsReplayCopy(__instance))
        {
            __result = 0;
        }
    }
}
