using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace Mod.ModCode.Patches;

[HarmonyPatch(typeof(NCard), "Reload")]
internal static class ModAncientCardVisualReloadPatch
{
    [HarmonyPostfix]
    private static void ApplyStandardFrameForModAncient(NCard __instance)
    {
        CardModel? model = __instance.Model;
        if (!ModAncientCardVisuals.ShouldUseStandardFrame(model))
        {
            return;
        }

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
            MainFile.Logger.Info($"Unable to apply standard frame to {model!.Id}: card node is missing standard frame parts.");
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

        Material silver = ModAncientCardVisuals.SilverBannerMaterial;
        portrait.Texture = model!.Portrait;
        portraitBorder.Texture = ModAncientCardVisuals.GetPortraitBorderTexture(model);
        portraitBorder.Material = silver;
        frame.Texture = ModAncientCardVisuals.GetFrameTexture(model);
        frame.Material = silver;
        banner.Texture = ModAncientCardVisuals.GetBannerTexture();
        banner.Material = silver;
        if (ancientPortrait != null)
        {
            ancientPortrait.Material = silver;
        }
        if (ancientBorderGlass != null)
        {
            ancientBorderGlass.Material = silver;
        }
        if (ancientBorder != null)
        {
            ancientBorder.Material = silver;
        }
        if (ancientTextBg != null)
        {
            ancientTextBg.Material = silver;
        }
        if (ancientBanner != null)
        {
            ancientBanner.Material = silver;
        }
    }
}

[HarmonyPatch(typeof(NCard), "ReloadOverlay")]
internal static class ModAncientCardVisualOverlayPatch
{
    [HarmonyPostfix]
    private static void HideAncientOverlayForModAncient(NCard __instance)
    {
        if (!ModAncientCardVisuals.ShouldUseStandardFrame(__instance.Model))
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
            typePlaque.Material = ModAncientCardVisuals.SilverBannerMaterial;
        }
    }
}
