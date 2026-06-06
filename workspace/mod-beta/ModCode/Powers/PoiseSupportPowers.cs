using System;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Mod.ModCode.Cards;
using Mod.ModCode.Mechanics;
using Mod.ModCode.Commands;

namespace Mod.ModCode.Powers;

public sealed class GuardCounterPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => "res://mod/images/powers/atlases/guard_counter_power.tres";
    public override string CustomBigIconPath => "res://mod/images/powers/big/guard_counter_power.png";

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

public sealed class NextDamageReductionPower : CustomPowerModel
{
    private bool _reducedDamage;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/intangible_power.tres";
    public override string CustomBigIconPath => "res://images/powers/intangible_power.png";

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || amount <= 0m)
        {
            return 1m;
        }

        _reducedDamage = true;
        return 0.25m;
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner && _reducedDamage)
        {
            await PowerCmd.Remove(this);
        }
    }
}

public sealed class NextPoiseBonusPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/vulnerable_power.tres";
    public override string CustomBigIconPath => "res://images/powers/vulnerable_power.png";
}

public sealed class NextAttackDoublePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/double_damage_power.tres";
    public override string CustomBigIconPath => "res://images/powers/double_damage_power.png";

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        return Owner != null && dealer == Owner && cardSource?.Type == CardType.Attack ? 2m : 1m;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner != null && cardPlay.Card.Owner?.Creature == Owner && cardPlay.Card.Type == CardType.Attack)
        {
            await PowerCmd.Remove(this);
        }
    }
}

public sealed class VictoryRushPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => "res://images/atlases/ui_atlas.sprites/card/energy_ironclad.tres";
    public override string CustomBigIconPath => "res://images/packed/sprite_fonts/ironclad_energy_icon.png";

    public static async Task Trigger(PlayerChoiceContext choiceContext, Creature owner, CardModel? cardSource)
    {
        VictoryRushPower? power = owner.GetPower<VictoryRushPower>();
        Player? player = owner.CombatState?.Players.FirstOrDefault(player => player.Creature == owner);
        if (power == null || player == null)
        {
            return;
        }

        await PlayerCmd.GainEnergy(power.Amount, player);
        await ModPowerCmd.Apply<NextAttackDoublePower>(owner, 1m, owner, cardSource);
    }
}

public sealed class BreakingMomentumPower : CustomPowerModel
{
    private decimal _strengthBonus;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/vulnerable_power.tres";
    public override string CustomBigIconPath => "res://images/powers/vulnerable_power.png";

    public void AddStrengthBonus(decimal amount)
    {
        _strengthBonus += amount;
    }

    public static async Task Trigger(PlayerChoiceContext choiceContext, Creature owner, CardModel? cardSource)
    {
        BreakingMomentumPower? power = owner.GetPower<BreakingMomentumPower>();
        if (power == null)
        {
            return;
        }

        await ModPowerCmd.Apply<ExtraPoisePower>(owner, power.Amount, owner, cardSource);
        if (power._strengthBonus > 0m)
        {
            await ModPowerCmd.Apply<StrengthPower>(owner, power._strengthBonus, owner, cardSource);
        }
    }
}

public sealed class SmallRoundShieldParryPower : CustomPowerModel
{
    private bool _parriedDamage;
    private bool _removeAfterInterruptedAttack;
    private int _fatalStrikeGrantCount = 1;
    private Creature? _interruptedAttackTarget;
    private Creature? _parryTarget;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => "res://mod/images/powers/buckler_shield_power.png";
    public override string CustomBigIconPath => "res://mod/images/powers/big/buckler_shield_power.png";

    public void SetFatalStrikeGrantCount(int count)
    {
        _fatalStrikeGrantCount = Math.Max(_fatalStrikeGrantCount, count);
    }

    public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (Owner != null && target == Owner && amount > 0m && dealer != null && dealer == _interruptedAttackTarget && dealer.IsStunned)
        {
            return 0m;
        }

        if (Owner == null || target != Owner || amount <= 0m)
        {
            return amount;
        }

        _parriedDamage = true;
        _parryTarget = dealer != null && dealer != Owner && dealer.Side != Owner.Side ? dealer : null;
        return 0m;
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (Owner == null || target != Owner || !_parriedDamage)
        {
            return;
        }

        Creature? parryTarget = _parryTarget;
        _parriedDamage = false;
        _parryTarget = null;
        bool interruptedAttack = false;

        if (parryTarget is { IsAlive: true })
        {
            await CreatureCmd.Stun(parryTarget);
            await BreakingMomentumPower.Trigger(choiceContext, Owner, cardSource);
            interruptedAttack = parryTarget.IsStunned;
            if (interruptedAttack)
            {
                _interruptedAttackTarget = parryTarget;
            }
        }

        Player? player = Owner.Player;
        if (Owner.CombatState == null || player == null)
        {
            await PowerCmd.Decrement(this);
            return;
        }

        for (int i = 0; i < _fatalStrikeGrantCount; i++)
        {
            CardModel fatalStrike = Owner.CombatState.CreateCard<FatalStrike>(player);
            fatalStrike.AddKeyword(CardKeyword.Exhaust);
            await CardPileCmd.Add(fatalStrike, PileType.Hand);
        }

        if (interruptedAttack && Amount <= 1)
        {
            _removeAfterInterruptedAttack = true;
            return;
        }

        await PowerCmd.Decrement(this);
    }

    internal async Task CompleteInterruptedAttackAsync(Creature attacker)
    {
        if (_interruptedAttackTarget == null || attacker != _interruptedAttackTarget)
        {
            return;
        }

        _interruptedAttackTarget = null;
        if (_removeAfterInterruptedAttack)
        {
            _removeAfterInterruptedAttack = false;
            await PowerCmd.Remove(this);
        }
    }
}

public sealed class GlintbladePhalanxPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => "res://mod/images/powers/atlases/glintblade_phalanx_power.tres";
    public override string CustomBigIconPath => "res://mod/images/powers/big/glintblade_phalanx_power.png";

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner == null || player.Creature != Owner)
        {
            return;
        }

        for (int i = 0; i < 3; i++)
        {
            Creature? target = Owner.CombatState?.HittableEnemies.FirstOrDefault();
            if (target == null)
            {
                break;
            }

            await CreatureCmd.Damage(choiceContext, target, 7m, ValueProp.Move | ValueProp.Unpowered, Owner, null);
            await Imbalance.Reduce(choiceContext, target, (int)Amount, Owner, null);
        }

        await PowerCmd.Remove(this);
    }
}
