using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Mod.ModCode.Powers;

public sealed class PlunderingFirePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/regeneration_power.tres";
    public override string CustomBigIconPath => "res://images/powers/regeneration_power.png";

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource
    )
    {
        if (Owner == null || dealer != Owner || cardSource?.Type != CardType.Attack || result.UnblockedDamage <= 0m)
        {
            return;
        }

        await CreatureCmd.Heal(Owner, result.UnblockedDamage, false);
    }

    public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner == null || side != Owner.Side)
        {
            return;
        }

        await PowerCmd.Remove(this);
    }
}
