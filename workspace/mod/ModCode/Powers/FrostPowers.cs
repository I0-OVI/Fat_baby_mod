using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Mod.ModCode.Cards;
using Mod.ModCode.Mechanics;

namespace Mod.ModCode.Powers;

public sealed class FrostbitePower : ModCustomPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override string CustomPackedIconPath => "res://mod/images/powers/frostbite_power.png";
    public override string CustomBigIconPath => "res://mod/images/powers/big/frostbite_power.png";

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        if (canonicalPower is FrostbitePower && target == Owner)
        {
            modifiedAmount = 0;
            return true;
        }

        modifiedAmount = amount;
        return false;
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (Owner == null || target != Owner || amount <= 0m || dealer == null || dealer.Side != CombatSide.Player)
        {
            return 1m;
        }

        if (cardSource is IMagicAttributeCard || !props.IsPoweredAttack())
        {
            return 1m;
        }

        return FrostbiteMechanic.PhysicalDamageMultiplier;
    }
}

public sealed class MagicVulnerabilityPower : ModCustomPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => "res://mod/images/powers/magic_vulnerability_power.png";
    public override string CustomBigIconPath => "res://mod/images/powers/big/magic_vulnerability_power.png";

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        return target == Owner && amount > 0m && cardSource is IMagicAttributeCard && props.IsPoweredAttack() ? 1.2m : 1m;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (Owner != null && side == Owner.Side)
        {
            await PowerCmd.Decrement(this);
        }
    }
}

public sealed class RaptorOfMistsPower : ModCustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => "res://mod/images/powers/raptor_of_mists_power.png";
    public override string CustomBigIconPath => "res://mod/images/powers/big/raptor_of_mists_power.png";

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        return target == Owner && amount > 0m ? System.Math.Max(0m, 1m - Amount / 100m) : 1m;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (Owner != null && player.Creature == Owner)
        {
            await PowerCmd.Remove(this);
        }
    }
}
