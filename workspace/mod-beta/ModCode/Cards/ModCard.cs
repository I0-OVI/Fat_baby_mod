using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace Mod.ModCode.Cards;

public abstract class ModCard(int cost, CardType type, CardRarity rarity, TargetType target, bool showInCardLibrary = true)
    : CardModel(cost, type, rarity, target, showInCardLibrary)
{
    public override string PortraitPath => CardModel.MissingPortraitPath;
    public override string BetaPortraitPath => PortraitPath;

    public override IEnumerable<string> AllPortraitPaths =>
        PortraitPath == BetaPortraitPath ? [PortraitPath] : [PortraitPath, BetaPortraitPath];

    protected override IEnumerable<string> ExtraRunAssetPaths => AllPortraitPaths;
}
