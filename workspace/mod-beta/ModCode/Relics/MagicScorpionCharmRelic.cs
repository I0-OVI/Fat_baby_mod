using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using FatBaby.ModCode.Commands;
using FatBaby.ModCode.Powers;

namespace FatBaby.ModCode.Relics;

public sealed class MagicScorpionCharmRelic : RelicModel
{
    public const decimal CombatStartMagic = 7m;
    public const decimal DamageTakenBonusPercent = 15m;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override string PackedIconPath => "res://mod/images/relics/atlases/magic_scorpion_charm.tres";
    protected override string PackedIconOutlinePath => "res://mod/images/relics/atlases/magic_scorpion_charm.tres";
    protected override string BigIconPath => "res://mod/images/relics/magic_scorpion_charm.png";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Magic", CombatStartMagic),
        new DynamicVar("DamageTaken", DamageTakenBonusPercent),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<MagicPower>(),
        HoverTipFactory.FromPower<MagicScorpionCharmPower>(),
    ];

    public override async Task BeforeCombatStart()
    {
        await ModPowerCmd.Apply<MagicPower>(Owner.Creature, CombatStartMagic, Owner.Creature, null);
        await ModPowerCmd.Apply<MagicScorpionCharmPower>(Owner.Creature, 1m, Owner.Creature, null, silent: true);
    }
}
