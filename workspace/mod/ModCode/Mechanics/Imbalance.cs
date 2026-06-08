using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using Mod.ModCode.Powers;
using Mod.ModCode.Commands;

namespace Mod.ModCode.Mechanics;

public static class Imbalance
{
    private const int MinionImbalance = 10;

    public static int GetInitialValue(Creature creature)
    {
        if (IsMinion(creature))
        {
            return MinionImbalance;
        }

        ICombatState? combatState = creature.CombatState;
        RoomType roomType = combatState?.Encounter?.RoomType ?? RoomType.Monster;

        return roomType switch
        {
            RoomType.Boss => 25,
            RoomType.Elite => IsGroupElite(combatState) ? 15 : 20,
            _ => 10
        };
    }

    /// <summary>
    /// Boss/elite summons (MinionPower, secondary enemies, or InfestedPower wrigglers) always use normal-minion poise.
    /// </summary>
    public static bool IsMinion(Creature creature)
    {
        if (creature.HasPower<MinionPower>() || creature.IsSecondaryEnemy)
        {
            return true;
        }

        return creature.Monster is Wriggler wriggler && wriggler.StartStunned;
    }

    public static async Task EnsureOnEnemies(
        PlayerChoiceContext choiceContext,
        CombatState combatState,
        Creature? applier = null,
        CardModel? cardSource = null
    )
    {
        foreach (Creature enemy in combatState.Enemies.Where(enemy => enemy.IsAlive && enemy.IsMonster))
        {
            if (!enemy.HasPower<ImbalancePower>())
            {
                await ModPowerCmd.Apply<ImbalancePower>(enemy, GetInitialValue(enemy), applier, cardSource, silent: true);
            }
        }
    }

    public static async Task Reduce(
        PlayerChoiceContext choiceContext,
        Creature target,
        int amount,
        Creature? applier = null,
        CardModel? cardSource = null
    )
    {
        if (cardSource?.Type == MegaCrit.Sts2.Core.Entities.Cards.CardType.Attack)
        {
            ExtraPoisePower? extraPoise = applier?.GetPower<ExtraPoisePower>();
            if (extraPoise != null)
            {
                amount += (int)extraPoise.Amount;
            }

            NextPoiseBonusPower? poiseBonus = applier?.GetPower<NextPoiseBonusPower>();
            if (poiseBonus != null)
            {
                amount += poiseBonus.GetNextBonus();
            }
        }

        if (amount <= 0 || target.IsDead || !target.IsMonster)
        {
            return;
        }

        ImbalancePower? power = target.GetPower<ImbalancePower>();
        if (power == null)
        {
            await ModPowerCmd.Apply<ImbalancePower>(target, GetInitialValue(target), applier, cardSource, silent: true);
            power = target.GetPower<ImbalancePower>();
        }

        if (power == null)
        {
            return;
        }

        if (amount < power.Amount)
        {
            await ModPowerCmd.ModifyAmount(power, -amount, applier, cardSource);
            return;
        }

        await ModPowerCmd.ModifyAmount(power, 1 - power.Amount, applier, cardSource);
        await Break(choiceContext, target, applier, cardSource);
    }

    private static async Task Break(
        PlayerChoiceContext choiceContext,
        Creature target,
        Creature? applier,
        CardModel? cardSource
    )
    {
        ImbalancePower? power = target.GetPower<ImbalancePower>();
        if (power == null)
        {
            return;
        }

        await CreatureCmd.Stun(target);
        if (applier != null)
        {
            await VictoryRushPower.Trigger(choiceContext, applier, cardSource);
            await BreakingMomentumPower.Trigger(choiceContext, applier, cardSource);
        }
        power.SetAmount(GetInitialValue(target), silent: true);
    }

    private static bool IsGroupElite(ICombatState? combatState)
    {
        return combatState?.Enemies.Count(enemy => enemy.IsAlive && enemy.IsMonster) > 1;
    }
}
