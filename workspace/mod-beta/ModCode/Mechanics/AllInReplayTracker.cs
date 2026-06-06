using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Mod.ModCode.Mechanics;

/// <summary>
/// Queues All In copy cards and drains them after pile-change hooks finish.
/// </summary>
internal static class AllInReplayTracker
{
    private sealed class PendingReplay
    {
        public required PlayerChoiceContext ChoiceContext { get; init; }
        public required CardModel Copy { get; init; }
    }

    private sealed class PlayerState
    {
        public Queue<PendingReplay> Pending { get; } = new();
        public bool IsDraining { get; set; }
    }

    private static readonly ConditionalWeakTable<Player, PlayerState> States = new();

    internal static void EnqueueCopy(Player player, PlayerChoiceContext choiceContext, CardModel copy) =>
        States.GetValue(player, static _ => new PlayerState()).Pending.Enqueue(new PendingReplay
        {
            ChoiceContext = choiceContext,
            Copy = copy
        });

    internal static async Task DrainPendingAsync(Player player)
    {
        if (!States.TryGetValue(player, out PlayerState? state) || state.IsDraining || state.Pending.Count == 0)
        {
            return;
        }

        state.IsDraining = true;
        try
        {
            while (state.Pending.Count > 0)
            {
                PendingReplay replay = state.Pending.Dequeue();
                await CardCmd.AutoPlay(replay.ChoiceContext, replay.Copy, null, skipXCapture: true);
                if (replay.Copy.Pile?.IsCombatPile == true)
                {
                    await CardPileCmd.RemoveFromCombat(replay.Copy, skipVisuals: true);
                }
            }
        }
        finally
        {
            state.IsDraining = false;
        }
    }

    internal static void ClearPending(Player player)
    {
        if (States.TryGetValue(player, out PlayerState? state))
        {
            state.Pending.Clear();
            state.IsDraining = false;
        }
    }
}
