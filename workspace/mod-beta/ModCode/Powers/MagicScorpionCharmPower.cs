using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Mod.ModCode.Powers;

public sealed class MagicScorpionCharmPower : ModCustomPowerModel
{
    public const decimal DamageMultiplier = 1.15m;

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override string CustomPackedIconPath => "res://mod/images/powers/magic_scorpion_charm_power.png";
    public override string CustomBigIconPath => "res://mod/images/powers/big/magic_scorpion_charm_power.png";

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (Owner == null || target != Owner || amount <= 0m || dealer == null || dealer == Owner || dealer.Side == Owner.Side)
        {
            return 1m;
        }

        return DamageMultiplier;
    }
}
