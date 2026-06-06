using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;
using Mod.ModCode.Relics;

namespace Mod.ModCode.Character;

public sealed class ScarletRelicPool : RelicPoolModel
{
    public override string EnergyColorName => "ironclad";
    public override Color LabOutlineColor => ScarletAcolyte.ScarletColor;

    protected override IEnumerable<RelicModel> GenerateAllRelics() =>
    [
        ModelDb.Relic<ElementFlaskRelic>(),
        ModelDb.Relic<RefinedElementFlaskRelic>(),
        ModelDb.Relic<RedTearstoneRingRelic>(),
        ModelDb.Relic<MagicScorpionCharmRelic>()
    ];
}
