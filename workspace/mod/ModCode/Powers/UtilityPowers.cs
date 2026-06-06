using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Mod.ModCode.Cards;

namespace Mod.ModCode.Powers;

public sealed class EndurePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/intangible_power.tres";
    public override string CustomBigIconPath => "res://images/powers/intangible_power.png";

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        return target == Owner && amount > 0m ? 0.5m : 1m;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner != null && player.Creature == Owner)
        {
            await PowerCmd.Remove(this);
        }
    }
}

public sealed class MagicPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/focus_power.tres";
    public override string CustomBigIconPath => "res://images/powers/focus_power.png";

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (Owner == null || dealer != Owner || cardSource is not IMagicDamageCard || !props.IsPoweredAttack())
        {
            return 0m;
        }

        return Amount;
    }
}

public sealed class ExtraPoisePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomPackedIconPath => "res://mod/images/powers/atlases/imbalance_power.tres";
    public override string CustomBigIconPath => "res://mod/images/powers/big/imbalance_power.png";
}

public sealed class MagicRealmPower : CustomPowerModel
{
    private const decimal BlockPerTurn = 10m;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomPackedIconPath => "res://mod/images/powers/magic_realm_power.png";
    public override string CustomBigIconPath => "res://mod/images/powers/big/magic_realm_power.png";

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner == null || player.Creature != Owner)
        {
            return;
        }

        await PowerCmd.Apply<MagicPower>(Owner, Amount, Owner, null);
        await CreatureCmd.GainBlock(Owner, BlockPerTurn, ValueProp.Unpowered, null);
    }
}

public sealed class StartupProtectionPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/buffer_power.tres";
    public override string CustomBigIconPath => "res://images/powers/buffer_power.png";

    public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        return target == Owner ? 0m : amount;
    }

    public override Task AfterModifyingHpLostAfterOsty()
    {
        Flash();
        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (Owner != null && side != Owner.Side)
        {
            await PowerCmd.Decrement(this);
        }
    }
}
