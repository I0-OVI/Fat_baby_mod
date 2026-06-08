using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Mod.ModCode.Powers;
using Mod.ModCode.Commands;

namespace Mod.ModCode.Cards;

public interface IChargedCard
{
    Task InitializeChargeAtCombatStart();

    /// <summary>Called when this combat card instance enters the exhaust pile from any other pile.</summary>
    Task OnEnteredExhaustFromOtherPileAsync(PileType oldPileType, bool showInSidebar = false);
}

public abstract class ChargedModCard<TPower>(int cost, CardType type, CardRarity rarity, TargetType target)
    : ModCard(cost, type, rarity, target)
    , IChargedCard
    where TPower : ChargePower
{
    private int _remainingCharge;
    private TPower? _chargePower;

    protected abstract int ChargeAmount { get; }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, CardKeyword.Retain];

    protected virtual IEnumerable<IHoverTip> ChargeHoverTips => [HoverTipFactory.FromPower<TPower>()];

    private async Task RemoveTrackedChargePower()
    {
        if (_chargePower != null)
        {
            await PowerCmd.Remove(_chargePower);
            _chargePower = null;
            return;
        }

        if (Owner?.Creature == null)
        {
            return;
        }

        TPower? orphan = Owner.Creature.GetPower<TPower>();
        if (orphan != null)
        {
            await PowerCmd.Remove(orphan);
        }
    }

    private async Task SyncChargePower(bool showInSidebar = false)
    {
        if (Owner?.Creature == null)
        {
            return;
        }

        if (_remainingCharge <= 0 || Pile?.Type != PileType.Exhaust)
        {
            await RemoveTrackedChargePower();
            return;
        }

        if (_chargePower != null && Owner.Creature.GetPowerInstances<TPower>().Contains(_chargePower))
        {
            int delta = _remainingCharge - _chargePower.Amount;
            if (delta != 0)
            {
                await ModPowerCmd.ModifyAmount(_chargePower, delta, Owner.Creature, this, silent: !showInSidebar);
            }
            else if (showInSidebar)
            {
                _chargePower.RefreshSidebarDisplay();
            }

            return;
        }

        TPower? existing = Owner.Creature.GetPower<TPower>();
        if (existing != null)
        {
            _chargePower = existing;
            int delta = _remainingCharge - _chargePower.Amount;
            if (delta != 0)
            {
                await ModPowerCmd.ModifyAmount(_chargePower, delta, Owner.Creature, this, silent: !showInSidebar);
            }
            else if (showInSidebar)
            {
                _chargePower.RefreshSidebarDisplay();
            }

            return;
        }

        await RemoveTrackedChargePower();
        _chargePower = await ModPowerCmd.Apply<TPower>(Owner.Creature, _remainingCharge, Owner.Creature, this, silent: !showInSidebar);
    }

    protected async Task StartChargeCycle(bool showInSidebar = false)
    {
        _remainingCharge = ChargeAmount;
        await SyncChargePower(showInSidebar);
    }

    private async Task ReturnToHandFromExhaust()
    {
        if (Owner == null || _remainingCharge > 0 || Pile?.Type != PileType.Exhaust)
        {
            return;
        }

        if (CardPile.Get(PileType.Hand, Owner)?.Cards.Count >= UtilityCardActions.MaxHandSize)
        {
            await StartChargeCycle(showInSidebar: true);
            return;
        }

        await RemoveTrackedChargePower();
        await CardPileCmd.Add(this, PileType.Hand, CardPilePosition.Bottom, this);
    }

    async Task IChargedCard.InitializeChargeAtCombatStart()
    {
        await InitializeChargeAtCombatStart();
    }

    private async Task InitializeChargeAtCombatStart()
    {
        if (!IsInCombat || Pile == null)
        {
            return;
        }

        if (Pile.Type != PileType.Exhaust)
        {
            await CardPileCmd.Add(this, PileType.Exhaust, CardPilePosition.Bottom, this, skipVisuals: true);
        }

        await StartChargeCycle();
    }

    public override async Task BeforeCombatStart()
    {
        await InitializeChargeAtCombatStart();
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null || cardPlay.Card.Owner != Owner || cardPlay.Card == this)
        {
            return;
        }

        if (_remainingCharge <= 0 || Pile?.Type != PileType.Exhaust)
        {
            return;
        }

        _remainingCharge--;
        if (_chargePower != null)
        {
            int newAmount = await ModPowerCmd.ModifyAmount(_chargePower, -1m, Owner.Creature, this);
            if (newAmount <= 0)
            {
                _chargePower = null;
            }
        }
        else
        {
            await SyncChargePower();
        }

        if (_remainingCharge <= 0)
        {
            await ReturnToHandFromExhaust();
        }
    }

    public async Task OnEnteredExhaustFromOtherPileAsync(PileType oldPileType, bool showInSidebar = false)
    {
        if (Owner == null || Pile?.Type != PileType.Exhaust || oldPileType == PileType.Exhaust)
        {
            return;
        }

        // Entering exhaust (played or forced) restarts the charge cycle: unplayable in exhaust
        // until other cards tick the counter down to 0, then it returns to hand.
        await StartChargeCycle(showInSidebar);
    }

    public override async Task AfterCardChangedPilesLate(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (card == this)
        {
            await OnEnteredExhaustFromOtherPileAsync(oldPileType, showInSidebar: true);
        }
    }

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card == this)
        {
            await OnEnteredExhaustFromOtherPileAsync(PileType.None, showInSidebar: true);
        }
    }

}
