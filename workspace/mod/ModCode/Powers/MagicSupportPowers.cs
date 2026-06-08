using System.Linq;
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
using Mod.ModCode.Mechanics;

namespace Mod.ModCode.Powers;

public sealed class HoulouGroundSlamPower : CustomPowerModel
{
    private int _poiseDamage;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/barricade_power.tres";
    public override string CustomBigIconPath => "res://images/powers/barricade_power.png";

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

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/focus_power.tres";
    public override string CustomBigIconPath => "res://images/powers/focus_power.png";

    public void SetHits(int hits)
    {
        _hits = hits;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner == null || player.Creature != Owner)
        {
            return;
        }

        for (int i = 0; i < _hits; i++)
        {
            Creature? target = MagicCardActions.RandomEnemy(Owner);
            if (target == null)
            {
                break;
            }

            await CreatureCmd.Damage(choiceContext, target, Amount, ValueProp.Move, Owner, null);
        }

        await PowerCmd.Remove(this);
    }
}

public sealed class CarianRetributionPower : CustomPowerModel
{
    private Creature? _counterTarget;
    private decimal _counterDamage;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomPackedIconPath => "res://mod/images/powers/buckler_shield_power.png";
    public override string CustomBigIconPath => "res://mod/images/powers/big/buckler_shield_power.png";

    public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (Owner == null || target != Owner || amount <= 0m || dealer == null || dealer == Owner)
        {
            return amount;
        }

        _counterTarget = dealer;
        _counterDamage = amount;
        return 0m;
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (Owner == null || target != Owner || _counterTarget == null || _counterDamage <= 0m)
        {
            return;
        }

        Creature counterTarget = _counterTarget;
        decimal counterDamage = _counterDamage;
        _counterTarget = null;
        _counterDamage = 0m;

        if (counterTarget.IsAlive)
        {
            await CreatureCmd.Damage(choiceContext, counterTarget, counterDamage, ValueProp.Move | ValueProp.Unpowered, Owner, null);
        }

        await PowerCmd.Decrement(this);
    }
}
