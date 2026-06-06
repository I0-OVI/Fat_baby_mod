using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Mod.ModCode.Powers;

public sealed class NextTurnEnergyPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomPackedIconPath => "res://images/atlases/ui_atlas.sprites/card/energy_ironclad.tres";
    public override string CustomBigIconPath => "res://images/packed/sprite_fonts/ironclad_energy_icon.png";

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner == null || player.Creature != Owner)
        {
            return;
        }

        await PlayerCmd.GainEnergy(Amount, player);
        await PowerCmd.Remove(this);
    }
}
