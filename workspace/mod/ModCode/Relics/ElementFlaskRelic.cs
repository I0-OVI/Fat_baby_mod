using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using Mod.ModCode.Cards;
using Mod.ModCode.Mechanics;
using Mod.ModCode.Powers;

namespace Mod.ModCode.Relics;

public class ElementFlaskRelic : RelicModel
{
    public const int DefaultChargeCap = 3;
    private const string ChargesKey = "Charges";

    protected virtual int ChargeCap => DefaultChargeCap;

    private int _chargesRemaining = DefaultChargeCap;

    public override RelicRarity Rarity => RelicRarity.Starter;

    public override bool ShowCounter => true;

    public override int DisplayAmount => ChargesRemaining;

    public override string PackedIconPath => "res://mod/images/relics/atlases/element_flask.tres";
    protected override string PackedIconOutlinePath => "res://mod/images/relics/atlases/element_flask.tres";
    protected override string BigIconPath => "res://mod/images/relics/element_flask.png";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(ChargesKey, ChargeCap)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<ElementFlask>()];

    [SavedProperty]
    public int ChargesRemaining
    {
        get => _chargesRemaining;
        set
        {
            AssertMutable();
            ApplyCharges(remaining: value);
        }
    }

    public static ElementFlaskRelic? GetForPlayer(Player player) =>
        player.Relics.OfType<ElementFlaskRelic>().FirstOrDefault();

    public static void RestoreChargesForPlayer(Player player)
    {
        GetForPlayer(player)?.RestoreToFullCharges(flash: true);
    }

    public static void SetCharges(Player player, int remaining)
    {
        GetForPlayer(player)?.ApplyCharges(remaining);
    }

    protected void RestoreToFullCharges(bool flash)
    {
        if (!IsMutable)
        {
            return;
        }

        ApplyCharges(ChargeCap);
        if (flash)
        {
            Flash();
        }
    }

    protected void ApplyCharges(int remaining)
    {
        _chargesRemaining = remaining;
        DynamicVars[ChargesKey].BaseValue = _chargesRemaining;
        InvokeDisplayAmountChanged();
    }

    public override async Task BeforeCombatStart()
    {
        if (Owner.PlayerCombatState == null)
        {
            return;
        }

        foreach (IChargedCard chargedCard in Owner.PlayerCombatState.AllCards.OfType<IChargedCard>().ToList())
        {
            await chargedCard.InitializeChargeAtCombatStart();
        }

        if (ChargesRemaining <= 0)
        {
            return;
        }

        Flash();
        CardModel card = Owner.Creature.CombatState!.CreateCard<ElementFlask>(Owner);
        if (card is ElementFlask elementFlask)
        {
            elementFlask.SetRemaining(ChargesRemaining);
        }

        await CardPileCmd.Add(card, PileType.Hand);
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room.RoomType == RoomType.RestSite || room is EventRoom { CanonicalEvent: AncientEventModel })
        {
            RestoreToFullCharges(flash: true);
        }

        return Task.CompletedTask;
    }

    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, CombatState combatState)
    {
        if (side == CombatSide.Player)
        {
            await Imbalance.EnsureOnEnemies(choiceContext, combatState, Owner.Creature, null);
        }
    }

    public override async Task AfterCreatureAddedToCombat(Creature creature)
    {
        if (creature.CombatState == null || creature.Side != CombatSide.Enemy || !creature.IsMonster || !creature.IsAlive)
        {
            return;
        }

        if (creature.HasPower<ImbalancePower>())
        {
            return;
        }

        await PowerCmd.Apply<ImbalancePower>(
            creature,
            Imbalance.GetInitialValue(creature),
            Owner.Creature,
            null,
            silent: true
        );
    }
}
