using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Mod.ModCode.Cards;
using Mod.ModCode.Commands;

namespace Mod.ModCode.Powers;

public sealed class ThisRoadIsBlockedPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/intangible_power.tres";
    public override string CustomBigIconPath => "res://images/powers/intangible_power.png";

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        return target == Owner && amount > 0m ? Math.Max(0m, 1m - Amount / 100m) : 1m;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner != null && player.Creature == Owner)
        {
            await PowerCmd.Remove(this);
        }
    }
}

public sealed class DarkMoonGreatSwordPower : ModCustomPowerModel
{
    private sealed class TemporaryBonus(int turns, decimal bonusDamage)
    {
        public int Turns { get; set; } = turns;
        public decimal BonusDamage { get; } = bonusDamage;
    }

    private readonly Queue<TemporaryBonus> _bonuses = new();

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => "res://mod/images/powers/dark_moon_great_sword_power.png";
    public override string CustomBigIconPath => "res://mod/images/powers/big/dark_moon_great_sword_power.png";

    public void AddBonus(int turns, decimal bonusDamage)
    {
        _bonuses.Enqueue(new TemporaryBonus(turns, bonusDamage));
    }

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (Owner == null || dealer != Owner || target == null || target.Side == Owner.Side || amount <= 0m)
        {
            return 0m;
        }

        decimal bonus = Amount;
        if (cardSource is not IMagicAttributeCard
            && props.IsPoweredAttack()
            && target.GetPower<MagicVulnerabilityPower>() is { Amount: > 0 })
        {
            bonus *= MagicVulnerabilityPower.MagicDamageMultiplier;
        }

        return bonus;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner == null || player.Creature != Owner || _bonuses.Count == 0)
        {
            return;
        }

        decimal expiredBonusDamage = 0m;
        int bonusCount = _bonuses.Count;
        for (int index = 0; index < bonusCount; index++)
        {
            TemporaryBonus bonus = _bonuses.Dequeue();
            bonus.Turns--;
            if (bonus.Turns <= 0)
            {
                expiredBonusDamage += bonus.BonusDamage;
            }
            else
            {
                _bonuses.Enqueue(bonus);
            }
        }

        int expiredCount = bonusCount - _bonuses.Count;
        if (expiredCount <= 0)
        {
            return;
        }

        Flash();
        await ModPowerCmd.ModifyAmount(this, -expiredBonusDamage, Owner, null);
    }
}

public sealed class CorruptionResonancePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => "res://images/atlases/ui_atlas.sprites/card/energy_ironclad.tres";
    public override string CustomBigIconPath => "res://images/packed/sprite_fonts/ironclad_energy_icon.png";

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner == null || player.Creature != Owner || Owner.GetPower<ScarletCorruptionPower>() == null)
        {
            return;
        }

        Flash();
        await PlayerCmd.GainEnergy(Amount, player);
    }
}

public sealed class PiercingCounterPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/double_damage_power.tres";
    public override string CustomBigIconPath => "res://images/powers/double_damage_power.png";

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        return Owner != null && dealer == Owner && target != null && target.Side != Owner.Side && amount > 0m
            ? Math.Max(1m, Math.Floor(amount * Amount / 100m))
            : 0m;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner != null && player.Creature == Owner)
        {
            await PowerCmd.Remove(this);
        }
    }
}

public sealed class InnerPotentialPower : CustomPowerModel
{
    private sealed class TemporaryEffect(decimal damageIncrease, decimal hpLoss)
    {
        public decimal DamageIncrease { get; } = damageIncrease;
        public decimal HpLoss { get; } = hpLoss;
        public bool HasPaidNextTurnHp { get; set; }
    }

    private readonly Queue<TemporaryEffect> _effects = new();

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/double_damage_power.tres";
    public override string CustomBigIconPath => "res://images/powers/double_damage_power.png";

    public void AddEffect(decimal damageIncrease, decimal hpLoss)
    {
        _effects.Enqueue(new TemporaryEffect(damageIncrease, hpLoss));
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        return Owner != null && dealer == Owner && target != null && target.Side != Owner.Side && amount > 0m
            ? 1m + Amount / 100m
            : 1m;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner == null || player.Creature != Owner || _effects.Count == 0)
        {
            return;
        }

        decimal hpLoss = 0m;
        decimal expiredDamageIncrease = 0m;
        int effectCount = _effects.Count;
        for (int index = 0; index < effectCount; index++)
        {
            TemporaryEffect effect = _effects.Dequeue();
            if (!effect.HasPaidNextTurnHp)
            {
                effect.HasPaidNextTurnHp = true;
                hpLoss += effect.HpLoss;
                _effects.Enqueue(effect);
            }
            else
            {
                expiredDamageIncrease += effect.DamageIncrease;
            }
        }

        if (hpLoss > 0m)
        {
            Flash();
            await CreatureCmd.Damage(
                choiceContext,
                Owner,
                hpLoss,
                ValueProp.Unblockable | ValueProp.Unpowered,
                null,
                null);
        }

        if (expiredDamageIncrease > 0m)
        {
            await ModPowerCmd.ModifyAmount(this, -expiredDamageIncrease, Owner, null);
        }
    }
}
