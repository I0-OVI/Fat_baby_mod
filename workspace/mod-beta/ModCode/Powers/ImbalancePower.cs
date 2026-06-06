using MegaCrit.Sts2.Core.Entities.Powers;

namespace Mod.ModCode.Powers;

public sealed class ImbalancePower : ModCustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomPackedIconPath => "res://mod/images/powers/atlases/imbalance_power.tres";
    public override string CustomBigIconPath => "res://mod/images/powers/big/imbalance_power.png";
}
