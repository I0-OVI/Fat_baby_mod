using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using Mod.ModCode.Relics;

namespace Mod.ModCode.Patches;

/// <summary>
/// Restores element flask charges when entering or resuming a rest site (in addition to the relic hook).
/// </summary>
[HarmonyPatch(typeof(RestSiteRoom))]
internal static class ElementFlaskRestSitePatch
{
    [HarmonyPatch("ShowRoomNode")]
    [HarmonyPostfix]
    private static void AfterShowRoomNode(IRunState runState)
    {
        RestoreForRun(runState);
    }

    [HarmonyPatch(nameof(RestSiteRoom.Resume))]
    [HarmonyPostfix]
    private static void AfterResume(IRunState? runState)
    {
        if (runState != null)
        {
            RestoreForRun(runState);
        }
    }

    private static void RestoreForRun(IRunState runState)
    {
        foreach (Player player in runState.Players)
        {
            if (player.IsActiveForHooks)
            {
                ElementFlaskRelic.RestoreChargesForPlayer(player);
            }
        }
    }
}
