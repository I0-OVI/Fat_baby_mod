using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using Mod.ModCode.Mechanics;
using Mod.ModCode.Powers;
using Mod.ModCode.Commands;

namespace Mod.ModCode.Cards;

internal static class UtilityCardActions
{
    public const int MaxHandSize = 10;

    public static async Task ExhaustFromHand(ModCard card, PlayerChoiceContext choiceContext, int count)
    {
        if (count <= 0)
        {
            return;
        }

        int availableCount = CardPile.GetCards(card.Owner, [PileType.Hand]).Count();
        int cardsToExhaust = Math.Min(count, availableCount);
        if (cardsToExhaust <= 0)
        {
            return;
        }

        var prefs = new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, cardsToExhaust);
        foreach (CardModel selectedCard in await CardSelectCmd.FromHand(choiceContext, card.Owner, prefs, null, card))
        {
            await CardCmd.Exhaust(choiceContext, selectedCard);
        }
    }
}

public sealed class TwinStingPoisonFlower() : ModCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    public override string PortraitPath => "res://mod/images/card_portraits/twin_sting_poison_flower.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<ScarletCorruptionPower>(), HoverTipFactory.FromPower<ImbalancePower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(3m, ValueProp.Move),
        new DynamicVar("HpLoss", 1m),
        new DynamicVar("ScarletCorruption", 3m),
        new DynamicVar("Imbalance", 1m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.Damage(choiceContext, Owner.Creature, DynamicVars["HpLoss"].BaseValue, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this);
        await ScarletCorruption.Reduce(choiceContext, Owner.Creature, DynamicVars["ScarletCorruption"].IntValue, Owner.Creature, this);
        await PoiseCardActions.AttackAllAndPoise(this, choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}

public sealed class SmallRoundShieldParry() : ModCard(1, CardType.Skill, CardRarity.Ancient, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/small_round_shield_parry.png";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<SmallRoundShieldParryPower>(),
        StunIntent.GetStaticHoverTip(),
        HoverTipFactory.FromCard(ModelDb.Card<FatalStrike>())
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Blocks", 1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SmallRoundShieldParryPower? power = await ModPowerCmd.Apply<SmallRoundShieldParryPower>(Owner.Creature, DynamicVars["Blocks"].BaseValue, Owner.Creature, this);
        power?.SetFatalStrikeGrantCount(IsUpgraded ? 2 : 1);
    }
}

public sealed class Truce() : ModCard(2, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/truce.png";

    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<NextTurnEnergyPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(20m, ValueProp.Move), new DynamicVar("Energy", 1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await ModPowerCmd.Apply<NextTurnEnergyPower>(Owner.Creature, DynamicVars["Energy"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
        DynamicVars["Energy"].UpgradeValueBy(1m);
    }
}

public sealed class Intimidation() : ModCard(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    public override string PortraitPath => "res://mod/images/card_portraits/intimidation.png";

    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<WeakPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<WeakPower>(1m), new BlockVar(8m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await ModPowerCmd.Apply<WeakPower>(cardPlay.Target, DynamicVars.Weak.BaseValue, Owner.Creature, this);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Weak.UpgradeValueBy(1m);
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}

public sealed class Determination() : ModCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/determination.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<NextAttackDoublePower>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await ModPowerCmd.Apply<NextAttackDoublePower>(Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class Search() : ModCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/search.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Exhaust", 2m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        while (CardPile.GetCards(Owner, [PileType.Hand]).Count() < UtilityCardActions.MaxHandSize)
        {
            CardModel? drawnCard = (await CardPileCmd.Draw(choiceContext, 1m, Owner)).FirstOrDefault();
            if (drawnCard == null || drawnCard.Type != CardType.Attack)
            {
                break;
            }
        }

        await UtilityCardActions.ExhaustFromHand(this, choiceContext, DynamicVars["Exhaust"].IntValue);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class BattleCry() : ModCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/battle_cry.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Strength", 1m), new CardsVar(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await ModPowerCmd.Apply<StrengthPower>(Owner.Creature, DynamicVars["Strength"].BaseValue, Owner.Creature, this);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}

public sealed class EasyHandling() : ModCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/easy_handling.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3), new DynamicVar("Exhaust", 1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        await UtilityCardActions.ExhaustFromHand(this, choiceContext, DynamicVars["Exhaust"].IntValue);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}

public sealed class Endure() : ModCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/endure.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<EndurePower>()];

    public override bool GainsBlock => IsUpgraded;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(0m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await ModPowerCmd.Apply<EndurePower>(Owner.Creature, 1m, Owner.Creature, this);
        if (IsUpgraded)
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(5m);
}

public sealed class Unburden() : ModCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/unburden.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [EnergyHoverTip, HoverTipFactory.FromPower<StrengthPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Strength", 1m), new DynamicVar("Energy", 2m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await ModPowerCmd.Apply<StrengthPower>(Owner.Creature, -DynamicVars["Strength"].BaseValue, Owner.Creature, this);
        await PlayerCmd.GainEnergy(DynamicVars["Energy"].BaseValue, Owner);
    }

    protected override void OnUpgrade() => DynamicVars["Energy"].UpgradeValueBy(1m);
}

public sealed class GoodLuck() : ModCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/good_luck.png";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [EnergyHoverTip, HoverTipFactory.FromPower<StrengthPower>(), HoverTipFactory.FromPower<MagicPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Strength", 1m),
        new DynamicVar("Magic", 1m),
        new DynamicVar("Energy", 1m),
        new CardsVar(1)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await ModPowerCmd.Apply<StrengthPower>(Owner.Creature, DynamicVars["Strength"].BaseValue, Owner.Creature, this);
        await ModPowerCmd.Apply<MagicPower>(Owner.Creature, DynamicVars["Magic"].BaseValue, Owner.Creature, this);
        await PlayerCmd.GainEnergy(DynamicVars["Energy"].BaseValue, Owner);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Energy"].UpgradeValueBy(1m);
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}

public sealed class Seppuku() : ModCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/seppuku.png";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<StrengthPower>(), HoverTipFactory.FromPower<MagicPower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("HpLoss", 5m), new DynamicVar("Strength", 1m), new DynamicVar("Magic", 1m)];

    private PlayerChoiceContext? _pendingHandExhaustContext;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _pendingHandExhaustContext = choiceContext;
        await CreatureCmd.Damage(choiceContext, Owner.Creature, DynamicVars["HpLoss"].BaseValue, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this);
    }

    public override async Task AfterCardChangedPilesLate(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (card != this || Owner == null)
        {
            return;
        }

        if (oldPileType != PileType.Play || Pile?.Type != PileType.Exhaust)
        {
            return;
        }

        PlayerChoiceContext? choiceContext = _pendingHandExhaustContext;
        _pendingHandExhaustContext = null;
        if (choiceContext == null)
        {
            return;
        }

        // Wait until this card leaves Play before exhausting the hand (charged-card deadlock fix).
        // Element Flask cannot be exhausted and bounces back from Exhaust via AfterCardChangedPilesLate.
        List<CardModel> handCards = CardPile.GetCards(Owner, [PileType.Hand])
            .Where(handCard => handCard != this && handCard is not ElementFlask)
            .ToList();

        if (handCards.Count == 0)
        {
            return;
        }

        foreach (CardModel handCard in handCards)
        {
            await CardCmd.Exhaust(choiceContext, handCard);
        }

        await ModPowerCmd.Apply<StrengthPower>(Owner.Creature, DynamicVars["Strength"].BaseValue * handCards.Count, Owner.Creature, this);
        await ModPowerCmd.Apply<MagicPower>(Owner.Creature, DynamicVars["Magic"].BaseValue * handCards.Count, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["HpLoss"].UpgradeValueBy(-3m);
}

public sealed class AllIn() : ModCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/all_in.png";

    protected override bool HasEnergyCostX => true;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("BonusPlays", 0m)];

    private PlayerChoiceContext? _pendingChoiceContext;
    private int _pendingPlayCount;

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _pendingChoiceContext = choiceContext;
        int xValue = cardPlay.Resources.EnergySpent;
        if (xValue <= 0 && HasEnergyCostX)
        {
            xValue = ResolveEnergyXValue();
        }

        _pendingPlayCount = Math.Max(0, xValue + DynamicVars["BonusPlays"].IntValue);
        return Task.CompletedTask;
    }

    public override async Task AfterCardChangedPilesLate(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (card != this || Owner == null || CombatState == null)
        {
            return;
        }

        if (oldPileType != PileType.Play || Pile?.Type != PileType.Discard)
        {
            return;
        }

        PlayerChoiceContext? choiceContext = _pendingChoiceContext;
        int playCount = _pendingPlayCount;
        if (playCount <= 0)
        {
            playCount = Math.Max(0, ResolveEnergyXValue() + DynamicVars["BonusPlays"].IntValue);
        }

        _pendingChoiceContext = null;
        _pendingPlayCount = 0;
        if (choiceContext == null)
        {
            return;
        }

        CardModel? selectedCard = (await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1),
            null,
            this)).FirstOrDefault();
        if (selectedCard == null)
        {
            return;
        }

        await CardCmd.Exhaust(choiceContext, selectedCard);
        if (playCount <= 0)
        {
            return;
        }

        for (int i = 0; i < playCount; i++)
        {
            CardModel copy = CombatState.CloneCard(selectedCard);
            AllInReplayTracker.EnqueueCopy(Owner, choiceContext, copy);
        }
    }

    protected override void OnUpgrade() => DynamicVars["BonusPlays"].UpgradeValueBy(1m);
}

public sealed class Ember() : ModCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/ember.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<MagicPower>(),
        HoverTipFactory.FromPower<ExtraPoisePower>(),
        HoverTipFactory.FromPower<ImbalancePower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Strength", 1m),
        new DynamicVar("Magic", 1m),
        new DynamicVar("MaxHp", 2m),
        new DynamicVar("Imbalance", 1m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await ModPowerCmd.Apply<StrengthPower>(Owner.Creature, DynamicVars["Strength"].BaseValue, Owner.Creature, this);
        await ModPowerCmd.Apply<MagicPower>(Owner.Creature, DynamicVars["Magic"].BaseValue, Owner.Creature, this);
        await CreatureCmd.GainMaxHp(Owner.Creature, DynamicVars["MaxHp"].BaseValue);
        await ModPowerCmd.Apply<ExtraPoisePower>(Owner.Creature, DynamicVars["Imbalance"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Strength"].UpgradeValueBy(1m);
        DynamicVars["MaxHp"].UpgradeValueBy(1m);
    }
}

public sealed class WaitForMeToStart() : ModCard(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/wait_for_me_to_start.png";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Innate, CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StartupProtectionPower>(),
        HoverTipFactory.FromPower<WeakPower>(),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        HoverTipFactory.FromKeyword(CardKeyword.Retain)
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Turns", 2m), new PowerVar<WeakPower>(2m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await ModPowerCmd.Apply<StartupProtectionPower>(Owner.Creature, DynamicVars["Turns"].BaseValue, Owner.Creature, this);
        await ModPowerCmd.Apply<RetainHandPower>(Owner.Creature, DynamicVars["Turns"].BaseValue, Owner.Creature, this);
        await ModPowerCmd.Apply<WeakPower>(Owner.Creature, DynamicVars.Weak.BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Turns"].UpgradeValueBy(1m);
        DynamicVars.Weak.UpgradeValueBy(1m);
    }
}

public sealed class MagicRealm() : ModCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/magic_realm.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MagicRealmPower>(), HoverTipFactory.FromPower<MagicPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Magic", 3m), new BlockVar(10m, ValueProp.Unpowered)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await ModPowerCmd.Apply<MagicRealmPower>(Owner.Creature, DynamicVars["Magic"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Magic"].UpgradeValueBy(1m);
    }
}

public sealed class TravelLight() : ModCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override string PortraitPath => "res://mod/images/card_portraits/travel_light.png";

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(8m, ValueProp.Move), new DynamicVar("DiscardBlock", 4m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        IEnumerable<CardModel> selected = await CardSelectCmd.FromHandForDiscard(
            choiceContext,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 0, 999999999),
            null,
            this
        );
        List<CardModel> toDiscard = selected.ToList();
        if (toDiscard.Count == 0)
        {
            return;
        }

        await CardCmd.Discard(choiceContext, toDiscard);

        decimal bonusBlock = DynamicVars["DiscardBlock"].BaseValue * toDiscard.Count;
        if (bonusBlock > 0m)
        {
            await CreatureCmd.GainBlock(Owner.Creature, bonusBlock, ValueProp.Move, cardPlay);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
        DynamicVars["DiscardBlock"].UpgradeValueBy(1m);
    }
}
