using System.Collections.Generic;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using FatBaby.ModCode.Cards;
using FatBaby.ModCode.Relics;

namespace FatBaby.ModCode.Character;

public sealed class ScarletAcolyte : PlaceholderCharacterModel
{
    public const string CharacterId = "ScarletAcolyte";

    public static readonly Color ScarletColor = new("B8324A");
    private const string CharacterSelectBgPath = "res://mod/scenes/screens/char_select/fat_baby_select_bg.tscn";
    private const string CombatVisualPath = "res://mod/scenes/character/fat_baby_combat_visual.tscn";
    private const string RestSiteScenePath = "res://scenes/rest_site/characters/scarletacolyte_rest_site.tscn";
    private const string FatBabySelectIconPath = "res://mod/images/character_select/fat_baby_select_icon.png";
    private const string CharacterUiIconPath = "res://mod/images/character_ui/fat_baby_onion_icon.svg";

    public override Color NameColor => ScarletColor;
    public override Color MapDrawingColor => ScarletColor;
    public override Color RemoteTargetingLineColor => ScarletColor;
    public override CharacterGender Gender => CharacterGender.Neutral;
    public override int StartingHp => 72;
    public override string CustomCharacterSelectBg => CharacterSelectBgPath;
    public override string CustomVisualPath => CombatVisualPath;
    public override string CustomRestSiteAnimPath => RestSiteScenePath;
    public override string CustomCharacterSelectIconPath => FatBabySelectIconPath;
    public override string CustomCharacterSelectLockedIconPath => FatBabySelectIconPath;
    public override string CustomIconTexturePath => CharacterUiIconPath;

    public override Control CustomIcon
    {
        get
        {
            TextureRect icon = new()
            {
                Texture = ResourceLoader.Load<Texture2D>(CustomIconTexturePath),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
            };
            icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            return icon;
        }
    }

    public override CardPoolModel CardPool => ModelDb.CardPool<ScarletCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<ScarletRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<ScarletPotionPool>();

    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<BasicAttack>(),
        ModelDb.Card<BasicAttack>(),
        ModelDb.Card<BasicAttack>(),
        ModelDb.Card<BasicAttack>(),
        ModelDb.Card<BasicDefense>(),
        ModelDb.Card<BasicDefense>(),
        ModelDb.Card<BasicDefense>(),
        ModelDb.Card<BasicDefense>(),
        ModelDb.Card<ChargedAttack>(),
        ModelDb.Card<GuardCounter>()
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<ElementFlaskRelic>()
    ];

    /// <summary>
    /// Keep custom mod textures in the run preload set so act transitions do not unload them.
    /// </summary>
    protected override IEnumerable<string> ExtraAssetPaths => ModRunAssets.PersistentTexturePaths;
}
