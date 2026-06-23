using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using FatBaby.ModCode.Commands;
using FatBaby.ModCode.Powers;
using FatBaby.ModCode.Relics;

namespace FatBaby.ModCode.Mechanics;

/// <summary>
/// Tracks Marais Executioner's Greatsword damage bonus across combats within a run.
/// Playing the card queues a bonus that is granted only after winning that combat.
/// Permanent bonus is stored on the starter element flask relic so it survives save/load.
/// </summary>
internal static class MaraisExecutionersGreatswordMechanic
{
    private sealed class PendingState
    {
        public decimal PendingVictoryBonus;
    }

    private static readonly ConditionalWeakTable<Player, PendingState> PendingStates = new();

    internal static decimal GetBonus(Player player) =>
        ElementFlaskRelic.GetMaraisPermanentDamageBonus(player);

    internal static async Task RegisterVictoryBonus(Player player, decimal amount)
    {
        if (amount <= 0m)
        {
            return;
        }

        PendingState state = PendingStates.GetValue(player, static _ => new PendingState());
        state.PendingVictoryBonus += amount;

        await SyncCombatPower(player, player.Creature, null);
    }

    /// <summary>
    /// Grant queued victory bonus and refresh the combat buff to the final total before
    /// player powers are cleared at combat end. Avoids a second Apply after cleanup, which
    /// leaves duplicate Marais icons on the victory screen.
    /// </summary>
    internal static async Task FinalizeVictoryBonusBeforeCombatCleanup(CombatState combatState)
    {
        if (!GrantPendingVictoryBonuses(combatState))
        {
            return;
        }

        foreach (Player player in combatState.Players)
        {
            await RefreshCombatPower(player, player.Creature, null);
        }
    }

    internal static async Task BeforeCombatStarted(CombatState combatState)
    {
        foreach (Player player in combatState.Players)
        {
            if (PendingStates.TryGetValue(player, out PendingState? state))
            {
                state.PendingVictoryBonus = 0m;
            }
        }

        await SyncAllCombatPowers(combatState);
    }

    private static bool GrantPendingVictoryBonuses(CombatState combatState)
    {
        bool bonusGranted = false;
        foreach (Player player in combatState.Players)
        {
            if (!PendingStates.TryGetValue(player, out PendingState? state) || state.PendingVictoryBonus <= 0m)
            {
                continue;
            }

            ElementFlaskRelic.AddMaraisPermanentDamageBonus(player, (int)state.PendingVictoryBonus);
            state.PendingVictoryBonus = 0m;
            bonusGranted = true;
        }

        return bonusGranted;
    }

    internal static async Task SyncAllCombatPowers(CombatState combatState)
    {
        foreach (Player player in combatState.Players)
        {
            await SyncCombatPower(player, player.Creature, null);
        }
    }

    internal static async Task SyncCombatPower(Player player, Creature applier, CardModel? cardSource)
    {
        await ReconcileCombatPower(player, applier, cardSource);
    }

    private static async Task RefreshCombatPower(Player player, Creature applier, CardModel? cardSource)
    {
        await ReconcileCombatPower(player, applier, cardSource);
    }

    private static async Task ReconcileCombatPower(Player player, Creature applier, CardModel? cardSource)
    {
        if (player.Creature.CombatState == null)
        {
            return;
        }

        decimal bonus = GetBonus(player);
        List<MaraisExecutionersGreatswordPower> instances =
            player.Creature.GetPowerInstances<MaraisExecutionersGreatswordPower>().ToList();
        MaraisExecutionersGreatswordPower? existing = instances.FirstOrDefault();

        for (int i = 1; i < instances.Count; i++)
        {
            await PowerCmd.Remove(instances[i]);
        }

        if (bonus <= 0m)
        {
            if (existing != null)
            {
                await PowerCmd.Remove(existing);
            }

            return;
        }

        if (existing == null)
        {
            await ModPowerCmd.Apply<MaraisExecutionersGreatswordPower>(player.Creature, bonus, applier, cardSource);
            return;
        }

        decimal delta = bonus - existing.Amount;
        if (delta != 0m)
        {
            await ModPowerCmd.ModifyAmount(existing, delta, applier, cardSource);
        }
    }
}
