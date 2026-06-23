using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;

namespace FatBaby.ModCode.Character;

public sealed class ScarletPotionPool : PotionPoolModel
{
    public override string EnergyColorName => "ironclad";
    public override Color LabOutlineColor => ScarletAcolyte.ScarletColor;

    protected override IEnumerable<PotionModel> GenerateAllPotions() => Array.Empty<PotionModel>();
}
