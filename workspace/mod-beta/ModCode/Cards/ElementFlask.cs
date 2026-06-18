using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Mod.ModCode.Relics;

namespace Mod.ModCode.Cards;

public sealed class ElementFlask() : ModCard(0, CardType.Skill, CardRarity.Token, TargetType.Self, showInCardLibrary: false)
{
    public override string PortraitPath => "res://mod/images/card_portraits/element_flask.png";

    public const string RemainingKey = "Remaining";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Retain)];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new HealVar(8m),
        new DynamicVar("Remaining", 0m)
    ];

    protected override bool IsPlayable => DynamicVars[RemainingKey].IntValue > 0;

    internal void SetRemaining(int remaining) => DynamicVars[RemainingKey].BaseValue = remaining;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue, false);
        int remaining = DynamicVars[RemainingKey].IntValue - 1;
        ElementFlaskRelic.SetCharges(Owner, remaining);
        if (remaining <= 0)
        {
            SetRemaining(0);
            await CardPileCmd.Add(this, PileType.Exhaust);
            return;
        }

        SetRemaining(remaining);
    }

    public override async Task AfterCardChangedPilesLate(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (card != this || Owner == null || DynamicVars[RemainingKey].IntValue <= 0)
        {
            return;
        }

        if (Pile?.Type is PileType.Discard or PileType.Exhaust)
        {
            await CardPileCmd.Add(this, PileType.Hand, CardPilePosition.Bottom, this);
        }
    }
}
