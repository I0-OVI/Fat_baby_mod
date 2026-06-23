using MegaCrit.Sts2.Core.Entities.Powers;

namespace FatBaby.ModCode.Powers;

public sealed class ImbalancePower : ModCustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomPackedIconPath => "res://mod/images/powers/atlases/imbalance_power.tres";
    public override string CustomBigIconPath => "res://mod/images/powers/big/imbalance_power.png";

    public int InitialResetValue { get; private set; }
    public int CurrentResetValue { get; private set; }

    public void InitializeResetValue(int value)
    {
        if (InitialResetValue > 0)
        {
            return;
        }

        InitialResetValue = value;
        CurrentResetValue = value;
    }

    public int IncreaseResetValue(int amount)
    {
        InitializeResetValue((int)Amount);
        CurrentResetValue = System.Math.Min(CurrentResetValue + amount, InitialResetValue + 10);
        return CurrentResetValue;
    }
}
