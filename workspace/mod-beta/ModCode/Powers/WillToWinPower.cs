using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace FatBaby.ModCode.Powers;

public sealed class WillToWinPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomPackedIconPath => "res://mod/images/powers/atlases/will_to_win_power.tres";
    public override string CustomBigIconPath => "res://mod/images/powers/big/will_to_win_power.png";

    public override decimal ModifyHpLostAfterOstyLate(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource
    )
    {
        if (Owner == null || target != Owner || amount <= 0m)
        {
            return amount;
        }

        decimal maxLoss = System.Math.Max(0m, Owner.CurrentHp - 1m);
        return System.Math.Min(amount, maxLoss);
    }
}
