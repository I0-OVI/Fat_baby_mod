using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using FatBaby.ModCode.Mechanics;

namespace FatBaby.ModCode.Patches;

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterTurnEnd))]
internal static class FrostbiteDurationPatch
{
    [HarmonyPostfix]
    private static void AfterTurnEnd(ICombatState combatState, CombatSide side, IEnumerable<Creature> participants, ref Task __result)
    {
        __result = ContinueAfterTurnEnd(__result ?? Task.CompletedTask, combatState, side);
    }

    private static async Task ContinueAfterTurnEnd(Task originalTask, ICombatState combatState, CombatSide side)
    {
        await originalTask;

        if (side != CombatSide.Player || combatState is not CombatState state)
        {
            return;
        }

        await FrostbiteMechanic.TickAfterPlayerTurnEnd(state);
    }
}
