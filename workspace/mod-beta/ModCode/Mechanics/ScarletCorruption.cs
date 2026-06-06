using System.Threading.Tasks;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Mod.ModCode.Powers;
using Mod.ModCode.Commands;

namespace Mod.ModCode.Mechanics;

public static class ScarletCorruption
{
    public const int MaxStacks = ScarletCorruptionPower.MaxStacks;

    public static async Task Apply(
        PlayerChoiceContext choiceContext,
        Creature owner,
        int stacks,
        Creature? applier = null,
        CardModel? cardSource = null
    )
    {
        if (stacks <= 0)
        {
            return;
        }

        decimal previousAmount = owner.GetPower<ScarletCorruptionPower>()?.Amount ?? 0m;
        await ModPowerCmd.Apply<ScarletCorruptionPower>(owner, stacks, applier, cardSource);
        decimal currentAmount = owner.GetPower<ScarletCorruptionPower>()?.Amount ?? 0m;

        if (currentAmount <= previousAmount)
        {
            return;
        }

        CorruptionSensePower? sensePower = owner.GetPower<CorruptionSensePower>();
        Player? player = FindOwnerPlayer(owner);
        if (sensePower != null && player != null)
        {
            await CardPileCmd.Draw(choiceContext, sensePower.Amount, player);
        }
    }

    public static async Task Reduce(
        PlayerChoiceContext choiceContext,
        Creature owner,
        int stacks,
        Creature? applier = null,
        CardModel? cardSource = null
    )
    {
        if (stacks <= 0)
        {
            return;
        }

        ScarletCorruptionPower? power = owner.GetPower<ScarletCorruptionPower>();
        if (power == null)
        {
            return;
        }

        await ModPowerCmd.ModifyAmount(power, -stacks, applier, cardSource);
    }

    private static Player? FindOwnerPlayer(Creature owner)
    {
        return owner.CombatState?.Players.FirstOrDefault(player => player.Creature == owner);
    }
}
