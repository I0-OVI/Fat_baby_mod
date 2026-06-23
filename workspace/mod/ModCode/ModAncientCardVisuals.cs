using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using FatBaby.ModCode.Character;

namespace FatBaby.ModCode;

internal static class ModAncientCardVisuals
{
    internal const string SilverBannerMaterialPath =
        "res://mod/materials/cards/banners/card_banner_ancient_silver_mat.tres";

    private static readonly Color SilverTitleOutline = new("777A80FF");

    internal static bool ShouldUseStandardFrame(CardModel? model) =>
        model is { Rarity: CardRarity.Ancient, Pool: ScarletCardPool };

    internal static Material CreateSilverBannerMaterial() =>
        (Material)PreloadManager.Cache.GetMaterial(SilverBannerMaterialPath).Duplicate(deep: true);

    internal static Color TitleOutlineColor => SilverTitleOutline;

    internal static Texture2D? GetFrameTexture(CardModel model)
    {
        string path = ImageHelper.GetImagePath(
            "atlases/ui_atlas.sprites/card/card_frame_" + GetFrameTypeKey(model.Type) + "_s.tres");
        return ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
    }

    internal static Texture2D? GetPortraitBorderTexture(CardModel model)
    {
        string path = ImageHelper.GetImagePath(
            "atlases/ui_atlas.sprites/card/card_portrait_border_" + GetFrameTypeKey(model.Type) + "_s.tres");
        return ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
    }

    internal static Texture2D? GetBannerTexture() =>
        ResourceLoader.Load<Texture2D>(
            ImageHelper.GetImagePath("atlases/ui_atlas.sprites/card/card_banner.tres"),
            null,
            ResourceLoader.CacheMode.Reuse);

    internal static Texture2D? GetPortraitTexture(CardModel model)
    {
        foreach (string path in model.AllPortraitPaths)
        {
            Texture2D? portrait = ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
            if (portrait != null)
            {
                return portrait;
            }
        }

        return model.Portrait;
    }

    private static string GetFrameTypeKey(CardType type) => type switch
    {
        CardType.Status or CardType.Curse => "skill",
        CardType.Attack or CardType.Skill or CardType.Power or CardType.Quest => type.ToString().ToLowerInvariant(),
        _ => "skill"
    };
}
