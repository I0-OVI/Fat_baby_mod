using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using Mod.ModCode.Mechanics;
using Mod.ModCode.Commands;

namespace Mod.ModCode.Powers;

public sealed class GarbageKingBlessingPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/strength_power.tres";
    public override string CustomBigIconPath => "res://images/powers/strength_power.png";

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner == null || player.Creature != Owner)
        {
            return;
        }

        await ModPowerCmd.Apply<StrengthPower>(Owner, Amount, Owner, null);
        await ScarletCorruption.Apply(choiceContext, Owner, 3, Owner);
    }
}
