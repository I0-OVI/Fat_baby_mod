using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace FatBaby.ModCode.Commands;

/// <summary>
/// Compatibility helpers for game builds where <see cref="PowerCmd.Apply"/> requires a <see cref="PlayerChoiceContext"/>.
/// </summary>
internal static class ModPowerCmd
{
    internal static Task<T?> Apply<T>(
        Creature target,
        decimal amount,
        Creature? applier,
        CardModel? cardSource,
        bool silent = false
    ) where T : PowerModel =>
        PowerCmd.Apply<T>(CreateContext(applier ?? target), target, amount, applier, cardSource, silent);

    internal static Task<IReadOnlyList<T>> Apply<T>(
        IEnumerable<Creature> targets,
        decimal amount,
        Creature? applier,
        CardModel? cardSource,
        bool silent = false
    ) where T : PowerModel
    {
        Creature contextCreature = applier ?? targets.First();
        return PowerCmd.Apply<T>(CreateContext(contextCreature), targets, amount, applier, cardSource, silent);
    }

    internal static Task<int> ModifyAmount(
        PowerModel power,
        decimal delta,
        Creature? applier,
        CardModel? cardSource,
        bool silent = false
    )
    {
        Creature contextCreature = applier ?? power.Owner ?? throw new System.InvalidOperationException("Power has no owner.");
        return PowerCmd.ModifyAmount(CreateContext(contextCreature), power, delta, applier, cardSource, silent);
    }

    private static HookPlayerChoiceContext CreateContext(Creature creature) =>
        new(creature.Player ?? throw new System.InvalidOperationException("Creature has no player."), 0UL, GameActionType.Combat);
}
