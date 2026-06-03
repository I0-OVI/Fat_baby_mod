using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Mod.ModCode.Powers;

namespace Mod.ModCode.Cards;

public interface IChargedCard
{
    Task InitializeChargeAtCombatStart();
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

    private async Task SyncChargePower()
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
                await PowerCmd.ModifyAmount(_chargePower, delta, Owner.Creature, this);
            }

            return;
        }

        await RemoveTrackedChargePower();
        _chargePower = await PowerCmd.Apply<TPower>(Owner.Creature, _remainingCharge, Owner.Creature, this, silent: true);
    }

    protected async Task StartChargeCycle()
    {
        _remainingCharge = ChargeAmount;
        await SyncChargePower();
    }

    private async Task ReturnToHandFromExhaust()
    {
        if (Owner == null || _remainingCharge > 0 || Pile?.Type != PileType.Exhaust)
        {
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
            int newAmount = await PowerCmd.ModifyAmount(_chargePower, -1m, Owner.Creature, this);
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

    public override async Task AfterCardChangedPilesLate(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (Owner == null || card != this)
        {
            return;
        }

        if (oldPileType == PileType.Play && Pile?.Type == PileType.Exhaust)
        {
            await StartChargeCycle();
        }
    }

}
