using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using FatBaby.ModCode.Mechanics;

namespace FatBaby.ModCode.Patches;

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCombatEnd))]
internal static class MaraisExecutionersGreatswordAfterCombatEndPatch
{
    [HarmonyPostfix]
    private static void AfterCombatEnd(IRunState runState, CombatState combatState, CombatRoom room, ref Task __result)
    {
        __result = ContinueAfterCombatEnd(__result ?? Task.CompletedTask, combatState);
    }

    private static async Task ContinueAfterCombatEnd(Task originalTask, CombatState combatState)
    {
        await originalTask;
        await MaraisExecutionersGreatswordMechanic.FinalizeVictoryBonusBeforeCombatCleanup(combatState);
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCombatStart))]
internal static class MaraisExecutionersGreatswordBeforeCombatPatch
{
    [HarmonyPostfix]
    private static void BeforeCombatStart(IRunState runState, CombatState combatState, ref Task __result)
    {
        __result = ContinueBeforeCombatStart(__result ?? Task.CompletedTask, combatState);
    }

    private static async Task ContinueBeforeCombatStart(Task originalTask, CombatState combatState)
    {
        await originalTask;
        await MaraisExecutionersGreatswordMechanic.BeforeCombatStarted(combatState);
    }
}
