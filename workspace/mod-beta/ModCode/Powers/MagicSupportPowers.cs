using System;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using FatBaby.ModCode.Cards;
using FatBaby.ModCode.Mechanics;

namespace FatBaby.ModCode.Powers;

public sealed class HoulouGroundSlamPower : CustomPowerModel
{
    private const string PowerIconPath = "res://mod/images/powers/houlou_ground_slam_power.png";
    private const string PowerBigIconPath = "res://mod/images/powers/big/houlou_ground_slam_power.png";

    private int _poiseDamage;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomPackedIconPath => PowerIconPath;
    public override string CustomBigIconPath => PowerBigIconPath;

    public void SetPoise(int poiseDamage)
    {
        _poiseDamage = poiseDamage;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner == null || player.Creature != Owner)
        {
            return;
        }

        foreach (Creature target in Owner.CombatState?.HittableEnemies.ToList() ?? [])
        {
            await CreatureCmd.Damage(choiceContext, target, Amount, ValueProp.Move | ValueProp.Unpowered, Owner, null);
            await Imbalance.Reduce(choiceContext, target, _poiseDamage, Owner, null);
        }

        await PowerCmd.Remove(this);
    }
}

public sealed class AdulasMoonbladePower : CustomPowerModel
{
    private AdulasMoonblade? _sourceCard;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/focus_power.tres";
    public override string CustomBigIconPath => "res://images/powers/focus_power.png";

    public void SetSourceCard(AdulasMoonblade sourceCard)
    {
        _sourceCard = sourceCard;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner == null || player.Creature != Owner || _sourceCard == null)
        {
            return;
        }

        for (int i = 0; i < Amount; i++)
        {
            Creature? target = MagicCardActions.RandomEnemy(Owner);
            if (target == null)
            {
                break;
            }

            await AdulasMoonblade.ExecuteEffect(_sourceCard, choiceContext, target);
        }

        await PowerCmd.Remove(this);
    }
}

public sealed class AncientDeathsRancorPower : CustomPowerModel
{
    private int _hits = 6;
    private decimal _damagePerHit = 2m;
    private CardModel? _sourceCard;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/focus_power.tres";
    public override string CustomBigIconPath => "res://images/powers/focus_power.png";

    public void Configure(int hits, decimal damagePerHit, CardModel sourceCard)
    {
        _hits = hits;
        _damagePerHit = damagePerHit;
        _sourceCard = sourceCard;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner == null || player.Creature != Owner || Owner.CombatState == null || _sourceCard == null || _hits <= 0)
        {
            return;
        }

        int repeats = Math.Max(0, (int)Amount);
        for (int repeat = 0; repeat < repeats; repeat++)
        {
            await DamageCmd.Attack(_damagePerHit)
                .WithHitCount(_hits)
                .FromCard(_sourceCard)
                .TargetingRandomOpponents(Owner.CombatState)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }

        await PowerCmd.Remove(this);
    }
}

public sealed class CarianRetributionPower : CustomPowerModel
{
    private CardModel? _sourceCard;
    private Creature? _counterTarget;
    private decimal _counterDamage;
    private Creature? _interruptedAttackTarget;
    private bool _removeAfterInterruptedAttack;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomPackedIconPath => "res://mod/images/powers/buckler_shield_power.png";
    public override string CustomBigIconPath => "res://mod/images/powers/big/buckler_shield_power.png";

    public void SetSourceCard(CardModel sourceCard)
    {
        _sourceCard = sourceCard;
    }

    public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (Owner != null && target == Owner && amount > 0m && dealer != null && dealer == _interruptedAttackTarget && dealer.IsStunned)
        {
            return 0m;
        }

        if (Owner == null || target != Owner || amount <= 0m || Amount <= 0m || dealer == null || dealer == Owner)
        {
            return amount;
        }

        _counterTarget = dealer;
        _counterDamage = amount;
        return 0m;
    }

    public override async Task AfterModifyingHpLostAfterOsty()
    {
        if (Owner == null || _counterTarget == null || _counterDamage <= 0m)
        {
            return;
        }

        PlayerChoiceContext choiceContext = new HookPlayerChoiceContext(Owner.Player!, 0UL, GameActionType.Combat);
        Creature counterTarget = _counterTarget;
        decimal counterDamage = _counterDamage;
        _counterTarget = null;
        _counterDamage = 0m;

        bool interruptedAttack = false;
        if (counterTarget.IsAlive)
        {
            await CreatureCmd.Stun(counterTarget);
            await BreakingMomentumPower.Trigger(choiceContext, Owner, _sourceCard);
            interruptedAttack = counterTarget.IsStunned;
            if (interruptedAttack)
            {
                _interruptedAttackTarget = counterTarget;
            }

            await CreatureCmd.Damage(choiceContext, counterTarget, counterDamage, ValueProp.Move | ValueProp.Unpowered, Owner, _sourceCard);
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
