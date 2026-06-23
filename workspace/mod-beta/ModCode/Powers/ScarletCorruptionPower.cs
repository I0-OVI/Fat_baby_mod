using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace FatBaby.ModCode.Powers;

public sealed class ScarletCorruptionPower : ModCustomPowerModel
{
    public const int MaxStacks = 3;

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomPackedIconPath => "res://mod/images/powers/atlases/scarlet_corruption_power.tres";
    public override string CustomBigIconPath => "res://mod/images/powers/big/scarlet_corruption_power.png";

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount
    )
    {
        if (canonicalPower is not ScarletCorruptionPower || target != Owner || amount <= 0m)
        {
            modifiedAmount = amount;
            return false;
        }

        modifiedAmount = Math.Max(0m, Math.Min(amount, MaxStacks - Amount));
        return true;
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        ClampStacks();
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner == null || player.Creature != Owner || Owner.IsDead)
        {
            return;
        }

        decimal hpLoss = Math.Min(Amount, MaxStacks);
        if (hpLoss <= 0m)
        {
            return;
        }

        Flash();
        await CreatureCmd.Damage(choiceContext, Owner, hpLoss, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
    }

    private void ClampStacks()
    {
        if (Amount > MaxStacks)
        {
            SetAmount(MaxStacks, silent: true);
        }
    }
}
