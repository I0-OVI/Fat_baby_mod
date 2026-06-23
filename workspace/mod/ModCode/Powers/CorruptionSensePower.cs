using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace FatBaby.ModCode.Powers;

public sealed class CorruptionSensePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/corruption_power.tres";
    public override string CustomBigIconPath => "res://images/powers/corruption_power.png";
}
