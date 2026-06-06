using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using Mod.ModCode.Mechanics;

namespace Mod.ModCode.Patches;

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardChangedPiles))]
internal static class AllInReplayPatch
{
    [HarmonyPostfix]
    private static void AfterCardChangedPiles(IRunState runState, ref Task __result)
    {
        __result = DrainAllInReplaysAfterPileChange(__result ?? Task.CompletedTask, runState);
    }

    private static async Task DrainAllInReplaysAfterPileChange(Task originalTask, IRunState runState)
    {
        await originalTask;

        foreach (Player player in runState.Players)
        {
            await AllInReplayTracker.DrainPendingAsync(player);
        }
    }
}
