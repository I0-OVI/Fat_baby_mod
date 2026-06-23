using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using FatBaby.ModCode.Cards;
using FatBaby.ModCode.Commands;
using FatBaby.ModCode.Powers;

namespace FatBaby.ModCode.Mechanics;

/// <summary>
/// Stacks next-turn repeat counters as each All In copy finishes playing.
/// </summary>
internal static class AllInRepeatHelper
{
    internal static bool SupportsIncrementalRepeat(CardModel template) =>
        template is AncientDeathsRancor or AdulasMoonblade;

    internal static async Task AccumulateRepeat(
        Player player,
        PlayerChoiceContext choiceContext,
        CardModel template,
        CardModel sourceCard)
    {
        if (player.Creature.CombatState == null)
        {
            return;
        }

        switch (template)
        {
            case AncientDeathsRancor rancor:
            {
                AncientDeathsRancorPower? power = await ModPowerCmd.Apply<AncientDeathsRancorPower>(
                    player.Creature,
                    1m,
                    player.Creature,
                    sourceCard);
                power?.Configure(rancor.DynamicVars["Hits"].IntValue, rancor.DynamicVars.Damage.BaseValue, sourceCard);
                break;
            }

            case AdulasMoonblade moonblade:
            {
                AdulasMoonbladePower? power = await ModPowerCmd.Apply<AdulasMoonbladePower>(
                    player.Creature,
                    moonblade.DynamicVars["Repeats"].BaseValue,
                    player.Creature,
                    sourceCard);
                power?.SetSourceCard((AdulasMoonblade)sourceCard);
                break;
            }
        }
    }
}
