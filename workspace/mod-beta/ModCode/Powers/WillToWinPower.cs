using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Mod.ModCode.Powers;

public sealed class WillToWinPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/buffer_power.tres";
    public override string CustomBigIconPath => "res://images/powers/buffer_power.png";

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource
    )
    {
        if (Owner == null || target != Owner || Owner.CurrentHp <= 0m || amount < Owner.CurrentHp)
        {
            return 0m;
        }

        return Owner.CurrentHp > 1m ? Owner.CurrentHp - 1m - amount : -amount;
    }
}
