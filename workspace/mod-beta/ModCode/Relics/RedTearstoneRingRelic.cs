using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Mod.ModCode.Powers;
using Mod.ModCode.Commands;

namespace Mod.ModCode.Relics;

public sealed class RedTearstoneRingRelic : RelicModel
{
    public const decimal HpThresholdRatio = 0.4m;
    public const decimal StrengthBonus = 10m;
    public const decimal MagicBonus = 10m;

    private bool _bonusActive;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override string PackedIconPath => "res://mod/images/relics/atlases/red_tearstone_ring.tres";
    protected override string PackedIconOutlinePath => "res://mod/images/relics/atlases/red_tearstone_ring.tres";
    protected override string BigIconPath => "res://mod/images/relics/red_tearstone_ring.png";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("HpThreshold", 40m),
        new DynamicVar("Strength", StrengthBonus),
        new DynamicVar("Magic", MagicBonus),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<MagicPower>(),
    ];

    public override Task BeforeCombatStart()
    {
        _bonusActive = false;
        return SyncBonus();
    }

    public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Owner.Creature)
        {
            return Task.CompletedTask;
        }

        return SyncBonus();
    }

    internal static bool IsBelowThreshold(Creature creature) =>
        creature.MaxHp > 0m && creature.CurrentHp <= creature.MaxHp * HpThresholdRatio;

    private async Task SyncBonus()
    {
        if (Owner.PlayerCombatState == null)
        {
            return;
        }

        Creature creature = Owner.Creature;
        bool shouldHaveBonus = IsBelowThreshold(creature);
        if (shouldHaveBonus == _bonusActive)
        {
            return;
        }

        if (shouldHaveBonus)
        {
            await ModPowerCmd.Apply<StrengthPower>(creature, StrengthBonus, creature, null);
            await ModPowerCmd.Apply<MagicPower>(creature, MagicBonus, creature, null);
            Flash();
        }
        else
        {
            await ModPowerCmd.Apply<StrengthPower>(creature, -StrengthBonus, creature, null);
            await ModPowerCmd.Apply<MagicPower>(creature, -MagicBonus, creature, null);
        }

        _bonusActive = shouldHaveBonus;
    }
}
