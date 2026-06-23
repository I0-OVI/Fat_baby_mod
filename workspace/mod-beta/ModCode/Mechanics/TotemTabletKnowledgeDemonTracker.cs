using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Monsters;

namespace FatBaby.ModCode.Mechanics;

/// <summary>
/// Hidden combat-only tracker for Totem Tablet releases vs Knowledge Demon (no power UI).
/// </summary>
internal static class TotemTabletKnowledgeDemonTracker
{
    private const int RequiredReleases = 3;
    private const int MaxPlayerTurnSpan = 1;

    private sealed class TrackerState
    {
        public int PlayerTurnCounter;
        public List<int> ReleaseTurns = [];
        public bool Triggered;
    }

    private static readonly ConditionalWeakTable<CombatState, TrackerState> States = new();

    private static TrackerState GetState(CombatState combatState) =>
        States.GetValue(combatState, static _ => new TrackerState());

    public static void OnPlayerTurnStart(CombatState combatState)
    {
        if (!TotemTabletKnowledgeDemonMechanic.IsKnowledgeDemonBossFight(combatState))
        {
            return;
        }

        GetState(combatState).PlayerTurnCounter++;
    }

    public static async Task RegisterRelease(PlayerChoiceContext choiceContext, Player? player)
    {
        if (player?.Creature?.CombatState is not CombatState combatState)
        {
            return;
        }

        if (!TotemTabletKnowledgeDemonMechanic.IsKnowledgeDemonBossFight(combatState)
            || !TotemTabletKnowledgeDemonMechanic.PlayerHasTotemTablet(player))
        {
            return;
        }

        TrackerState state = GetState(combatState);
        if (state.Triggered)
        {
            return;
        }

        state.ReleaseTurns.Add(state.PlayerTurnCounter);
        state.ReleaseTurns.RemoveAll(turn => state.PlayerTurnCounter - turn > MaxPlayerTurnSpan);

        if (state.ReleaseTurns.Count < RequiredReleases)
        {
            return;
        }

        state.Triggered = true;
        await KillKnowledgeDemon(combatState);
    }

    private static async Task KillKnowledgeDemon(CombatState combatState)
    {
        List<Creature> demons = combatState.Enemies
            .Where(enemy => enemy.IsAlive && enemy.Monster is KnowledgeDemon)
            .ToList();

        foreach (Creature demon in demons)
        {
            await CreatureCmd.Kill(demon, force: true);
        }
    }
}
