using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Mod.ModCode.Commands;
using Mod.ModCode.Powers;

namespace Mod.ModCode.Mechanics;

internal static class FrostbiteMechanic
{
    internal const decimal MaxHpDamagePercent = 0.07m;
    internal const decimal PhysicalDamageMultiplier = 1.2m;
    internal const int DurationTurns = 2;

    private sealed class CombatTracker
    {
        public Dictionary<Creature, int> TurnsRemaining = new();
    }

    private static readonly ConditionalWeakTable<CombatState, CombatTracker> Trackers = new();

    internal static async Task Apply(
        PlayerChoiceContext choiceContext,
        IEnumerable<Creature> targets,
        Creature applier,
        CardModel? cardSource)
    {
        foreach (Creature target in targets.Where(creature => creature.IsAlive && !creature.HasPower<FrostbitePower>()))
        {
            decimal damage = Math.Max(1m, Math.Ceiling(target.MaxHp * MaxHpDamagePercent));
            await CreatureCmd.Damage(
                choiceContext,
                target,
                damage,
                ValueProp.Unblockable | ValueProp.Unpowered,
                applier,
                cardSource);
            await ModPowerCmd.Apply<FrostbitePower>(target, DurationTurns, applier, cardSource);
            RegisterDuration(target);
        }
    }

    internal static void RegisterDuration(Creature target)
    {
        if (target.CombatState is not CombatState combatState)
        {
            return;
        }

        CombatTracker tracker = Trackers.GetOrCreateValue(combatState);
        tracker.TurnsRemaining[target] = DurationTurns;
    }

    internal static async Task TickAfterPlayerTurnEnd(CombatState combatState)
    {
        if (!Trackers.TryGetValue(combatState, out CombatTracker? tracker))
        {
            return;
        }

        foreach ((Creature creature, int turns) in tracker.TurnsRemaining.ToList())
        {
            if (!creature.IsAlive || creature.CombatState != combatState)
            {
                tracker.TurnsRemaining.Remove(creature);
                continue;
            }

            int remaining = turns - 1;
            if (remaining <= 0)
            {
                tracker.TurnsRemaining.Remove(creature);
                if (creature.GetPower<FrostbitePower>() is FrostbitePower power)
                {
                    await PowerCmd.Remove(power);
                }
            }
            else
            {
                tracker.TurnsRemaining[creature] = remaining;
            }
        }
    }
}
