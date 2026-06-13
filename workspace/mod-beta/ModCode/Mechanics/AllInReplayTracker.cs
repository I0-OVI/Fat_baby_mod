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
        public bool DrainRequested { get; set; }
        public CardModel? RepeatTemplate { get; set; }
        public CardModel? RepeatSourceCard { get; set; }
        public PlayerChoiceContext? RepeatChoiceContext { get; set; }
    }

    private static readonly ConditionalWeakTable<Player, PlayerState> States = new();
    private static readonly object ReplayCopyMarker = new();
    private static readonly ConditionalWeakTable<CardModel, object> ReplayCopies = new();

    internal static void BeginRepeatBatch(Player player, PlayerChoiceContext choiceContext, CardModel template)
    {
        PlayerState state = States.GetValue(player, static _ => new PlayerState());
        state.RepeatTemplate = template;
        state.RepeatChoiceContext = choiceContext;
        state.RepeatSourceCard = player.Creature.CombatState?.CloneCard(template);
    }

    internal static void EnqueueCopy(Player player, PlayerChoiceContext choiceContext, CardModel copy)
    {
        copy.BaseReplayCount = 0;
        ReplayCopies.Add(copy, ReplayCopyMarker);
        States.GetValue(player, static _ => new PlayerState()).Pending.Enqueue(new PendingReplay
        {
            ChoiceContext = choiceContext,
            Copy = copy
        });
    }

    internal static bool IsReplayCopy(CardModel card) => ReplayCopies.TryGetValue(card, out _);

    internal static void RequestDrain(Player player) =>
        States.GetValue(player, static _ => new PlayerState()).DrainRequested = true;

    internal static bool TryConsumeDrainRequest(Player player)
    {
        if (!States.TryGetValue(player, out PlayerState? state) || !state.DrainRequested)
        {
            return false;
        }

        state.DrainRequested = false;
        return true;
    }

    internal static async Task DrainPendingAsync(Player player)
    {
        if (!States.TryGetValue(player, out PlayerState? state) || state.IsDraining || state.Pending.Count == 0)
        {
            return;
        }

        state.IsDraining = true;
        CardModel? repeatTemplate = state.RepeatTemplate;
        CardModel? repeatSourceCard = state.RepeatSourceCard;
        PlayerChoiceContext? repeatChoiceContext = state.RepeatChoiceContext;
        state.RepeatTemplate = null;
        state.RepeatSourceCard = null;
        state.RepeatChoiceContext = null;

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

                if (repeatTemplate != null && repeatSourceCard != null && repeatChoiceContext != null
                    && AllInRepeatHelper.SupportsIncrementalRepeat(repeatTemplate))
                {
                    await AllInRepeatHelper.AccumulateRepeat(
                        player,
                        repeatChoiceContext,
                        repeatTemplate,
                        repeatSourceCard);
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
            state.DrainRequested = false;
            state.RepeatTemplate = null;
            state.RepeatSourceCard = null;
            state.RepeatChoiceContext = null;
        }
    }
}
