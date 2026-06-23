using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace FatBaby.ModCode.Patches;

[HarmonyPatch(typeof(NCard), "Reload")]
internal static class ModCardPortraitReloadPatch
{
    [HarmonyPostfix]
    private static void ReloadPortraitFromModelPaths(NCard __instance)
    {
        CardModel? model = __instance.Model;
        if (model == null)
        {
            return;
        }

        Texture2D? portraitTexture = ModAncientCardVisuals.GetPortraitTexture(model);
        if (portraitTexture == null)
        {
            return;
        }

        TextureRect? portrait = __instance.GetNodeOrNull<TextureRect>("%Portrait");
        if (portrait != null)
        {
            portrait.Texture = portraitTexture;
        }

        TextureRect? ancientPortrait = __instance.GetNodeOrNull<TextureRect>("%AncientPortrait");
        if (ancientPortrait != null)
        {
            ancientPortrait.Texture = portraitTexture;
        }
    }
}

[HarmonyPatch(typeof(NCard), "Reload")]
internal static class ModAncientCardVisualReloadPatch
{
    [HarmonyPostfix]
    private static void ApplyStandardFrameForModAncient(NCard __instance)
    {
        CardModel? maybeModel = __instance.Model;
        if (!ModAncientCardVisuals.ShouldUseStandardFrame(maybeModel))
        {
            return;
        }

        CardModel model = maybeModel!;
        TextureRect? portraitBorder = __instance.GetNodeOrNull<TextureRect>("%PortraitBorder");
        TextureRect? portrait = __instance.GetNodeOrNull<TextureRect>("%Portrait");
        TextureRect? frame = __instance.GetNodeOrNull<TextureRect>("%Frame");
        TextureRect? ancientPortrait = __instance.GetNodeOrNull<TextureRect>("%AncientPortrait");
        TextureRect? ancientBorderGlass = __instance.GetNodeOrNull<TextureRect>("%AncientBorderGlassOverlay");
        TextureRect? ancientBorder = __instance.GetNodeOrNull<TextureRect>("%AncientBorder");
        TextureRect? ancientTextBg = __instance.GetNodeOrNull<TextureRect>("%AncientTextBg");
        Control? ancientBanner = __instance.GetNodeOrNull<Control>("%AncientBanner");
        TextureRect? banner = __instance.GetNodeOrNull<TextureRect>("%TitleBanner");
        CanvasGroup? portraitCanvasGroup = __instance.GetNodeOrNull<CanvasGroup>("%PortraitCanvasGroup");

        if (portraitBorder == null || portrait == null || frame == null || banner == null)
        {
            MainFile.Logger.Info($"Unable to apply standard frame to {model.Id}: card node is missing standard frame parts.");
            return;
        }

        Texture2D? portraitBorderTexture = ModAncientCardVisuals.GetPortraitBorderTexture(model);
        Texture2D? frameTexture = ModAncientCardVisuals.GetFrameTexture(model);
        Texture2D? bannerTexture = ModAncientCardVisuals.GetBannerTexture();
        Texture2D? portraitTexture = ModAncientCardVisuals.GetPortraitTexture(model);
        if (portraitBorderTexture == null || frameTexture == null || bannerTexture == null || portraitTexture == null)
        {
            MainFile.Logger.Info($"Unable to apply standard frame to {model.Id}: standard frame resources are not loaded.");
            return;
        }

        portraitBorder.Visible = true;
        portrait.Visible = true;
        frame.Visible = true;
        if (ancientPortrait != null)
        {
            ancientPortrait.Visible = false;
            ancientPortrait.Material = null;
        }
        if (ancientBorderGlass != null)
        {
            ancientBorderGlass.Visible = false;
        }
        if (ancientBorder != null)
        {
            ancientBorder.Visible = false;
        }
        if (ancientTextBg != null)
        {
            ancientTextBg.Visible = false;
        }
        if (ancientBanner != null)
        {
            ancientBanner.Visible = false;
        }
        banner.Visible = true;

        if (portraitCanvasGroup != null)
        {
            portraitCanvasGroup.Material = null;
        }
        portrait.Material = null;

        portrait.Texture = portraitTexture;
        portraitBorder.Texture = portraitBorderTexture;
        portraitBorder.Material = ModAncientCardVisuals.CreateSilverBannerMaterial();
        frame.Texture = frameTexture;
        frame.Material = ModAncientCardVisuals.CreateSilverBannerMaterial();
        banner.Texture = bannerTexture;
        banner.Material = ModAncientCardVisuals.CreateSilverBannerMaterial();
        if (ancientPortrait != null)
        {
            ancientPortrait.Material = ModAncientCardVisuals.CreateSilverBannerMaterial();
        }
        if (ancientBorderGlass != null)
        {
            ancientBorderGlass.Material = ModAncientCardVisuals.CreateSilverBannerMaterial();
        }
        if (ancientBorder != null)
        {
            ancientBorder.Material = ModAncientCardVisuals.CreateSilverBannerMaterial();
        }
        if (ancientTextBg != null)
        {
            ancientTextBg.Material = ModAncientCardVisuals.CreateSilverBannerMaterial();
        }
        if (ancientBanner != null)
        {
            ancientBanner.Material = ModAncientCardVisuals.CreateSilverBannerMaterial();
        }
    }
}

[HarmonyPatch(typeof(NCard), "ReloadOverlay")]
internal static class ModAncientCardVisualOverlayPatch
{
    [HarmonyPostfix]
    private static void HideAncientOverlayForModAncient(NCard __instance)
    {
        CardModel? model = __instance.Model;
        if (!ModAncientCardVisuals.ShouldUseStandardFrame(model) || model == null)
        {
            return;
        }

        if (ModAncientCardVisuals.GetFrameTexture(model) == null
            || ModAncientCardVisuals.GetPortraitBorderTexture(model) == null
            || ModAncientCardVisuals.GetBannerTexture() == null
            || ModAncientCardVisuals.GetPortraitTexture(model) == null)
        {
            return;
        }

        TextureRect? frame = __instance.GetNodeOrNull<TextureRect>("%Frame");
        if (frame != null)
        {
            frame.Visible = true;
        }

        TextureRect? ancientBorder = __instance.GetNodeOrNull<TextureRect>("%AncientBorder");
        if (ancientBorder != null)
        {
            ancientBorder.Visible = false;
        }
    }
}

[HarmonyPatch(typeof(NCard), "GetTitleLabelOutlineColor")]
internal static class ModAncientCardTitleOutlinePatch
{
    [HarmonyPostfix]
    private static void UseSilverTitleOutline(NCard __instance, ref Color __result)
    {
        if (ModAncientCardVisuals.ShouldUseStandardFrame(__instance.Model))
        {
            __result = ModAncientCardVisuals.TitleOutlineColor;
        }
    }
}

[HarmonyPatch(typeof(NCard), "UpdateTypePlaque")]
internal static class ModAncientCardTypePlaquePatch
{
    [HarmonyPostfix]
    private static void ApplySilverTypePlaque(NCard __instance)
    {
        if (!ModAncientCardVisuals.ShouldUseStandardFrame(__instance.Model))
        {
            return;
        }

        NinePatchRect? typePlaque = __instance.GetNodeOrNull<NinePatchRect>("%TypePlaque");
        if (typePlaque != null)
        {
            typePlaque.Material = ModAncientCardVisuals.CreateSilverBannerMaterial();
        }
    }
}
