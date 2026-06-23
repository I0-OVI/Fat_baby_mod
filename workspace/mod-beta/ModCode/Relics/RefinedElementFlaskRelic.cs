using MegaCrit.Sts2.Core.Entities.Relics;

namespace FatBaby.ModCode.Relics;

/// <summary>
/// Orobas Touch of Orobas upgrade for the starter Element Flask (5 uses per rest / ancient).
/// </summary>
public sealed class RefinedElementFlaskRelic : ElementFlaskRelic
{
    public const int RefinedChargeCap = 5;

    protected override int ChargeCap => RefinedChargeCap;

    protected override void AfterCloned()
    {
        base.AfterCloned();
        ApplyCharges(ChargeCap);
    }
}
