using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Hooks;
using Mod.ModCode.Mechanics;

namespace Mod.ModCode.Patches;

[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeSideTurnStart))]
internal static class TotemTabletKnowledgeDemonPatch
{
    [HarmonyPostfix]
    private static void TrackPlayerTurnStart(CombatSide side, CombatState combatState)
    {
        if (side == CombatSide.Player)
        {
            TotemTabletKnowledgeDemonTracker.OnPlayerTurnStart(combatState);
        }
    }
}
