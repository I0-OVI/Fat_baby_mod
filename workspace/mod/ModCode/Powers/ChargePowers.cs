using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Mod.ModCode.Powers;

public abstract class ChargePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>Re-flash the sidebar icon when charge was updated silently (e.g. forced Exhaust).</summary>
    internal void RefreshSidebarDisplay()
    {
        if (Amount > 0)
        {
            Flash();
        }
    }
    public override string CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/energized_power.tres";
    public override string CustomBigIconPath => "res://images/powers/energized_power.png";
}

public sealed class GuardCounterChargePower : ChargePower
{
    public override string CustomPackedIconPath => "res://mod/images/powers/atlases/guard_counter_power.tres";
    public override string CustomBigIconPath => "res://mod/images/powers/big/guard_counter_power.png";
}

public sealed class TotemTabletChargePower : ChargePower
{
    public override string CustomPackedIconPath => "res://mod/images/powers/atlases/totem_tablet_power.tres";
    public override string CustomBigIconPath => "res://mod/images/powers/big/totem_tablet_power.png";
}
