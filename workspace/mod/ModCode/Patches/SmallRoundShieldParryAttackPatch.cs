using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using FatBaby.ModCode.Powers;

namespace FatBaby.ModCode.Patches;

/// <summary>
/// Lets parry powers interrupt the rest of the current multi-hit attack
/// after the first prevented hit stuns the attacker.
/// </summary>
[HarmonyPatch(typeof(AttackCommand), nameof(AttackCommand.Execute))]
internal static class SmallRoundShieldParryAttackPatch
{
    [HarmonyPostfix]
    private static void AfterExecute(AttackCommand __instance, ref Task<AttackCommand> __result)
    {
        __result = CompleteParryInterrupts(__result, __instance.Attacker);
    }

    private static async Task<AttackCommand> CompleteParryInterrupts(Task<AttackCommand> originalTask, Creature? attacker)
    {
        AttackCommand command = await originalTask;
        if (attacker?.CombatState == null)
        {
            return command;
        }

        foreach (SmallRoundShieldParryPower power in attacker.CombatState.Creatures
            .SelectMany(creature => creature.GetPowerInstances<SmallRoundShieldParryPower>())
            .ToList())
        {
            await power.CompleteInterruptedAttackAsync(attacker);
        }

        foreach (CarianRetributionPower power in attacker.CombatState.Creatures
            .SelectMany(creature => creature.GetPowerInstances<CarianRetributionPower>())
            .ToList())
        {
            await power.CompleteInterruptedAttackAsync(attacker);
        }

        return command;
    }
}
