using BaseLib.Abstracts;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Mod.ModCode.Powers;

namespace Mod.ModCode.Patches;

[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.Icon), MethodType.Getter)]
internal static class PowerModelModIconPatch
{
    [HarmonyPostfix]
    private static void UseCustomIcon(PowerModel __instance, ref Texture2D __result)
    {
        if (__instance is ModCustomPowerModel customPower && !string.IsNullOrEmpty(customPower.CustomPackedIconPath))
        {
            __result = ModPowerTextures.Load(customPower.CustomPackedIconPath);
        }
    }
}

[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.BigIcon), MethodType.Getter)]
internal static class PowerModelModBigIconPatch
{
    [HarmonyPostfix]
    private static void UseCustomBigIcon(PowerModel __instance, ref Texture2D __result)
    {
        if (__instance is ModCustomPowerModel customPower && !string.IsNullOrEmpty(customPower.CustomBigIconPath))
        {
            __result = ModPowerTextures.Load(customPower.CustomBigIconPath);
        }
    }
}

[HarmonyPatch(typeof(NPower), "Reload")]
internal static class NPowerModIconPatch
{
    [HarmonyPostfix]
    private static void ApplyCustomPowerTextures(NPower __instance)
    {
        if (!__instance.IsNodeReady())
        {
            return;
        }

        if (__instance.Model is BufferPower)
        {
            ApplyBufferPowerIcon(__instance);
            return;
        }

        if (__instance.Model is not CustomPowerModel customPower)
        {
            return;
        }

        string? packedPath = customPower.CustomPackedIconPath;
        string? bigPath = customPower.CustomBigIconPath;
        if (string.IsNullOrEmpty(packedPath) && string.IsNullOrEmpty(bigPath))
        {
            return;
        }

        TextureRect icon = __instance.GetNode<TextureRect>("%Icon");
        CpuParticles2D flash = __instance.GetNode<CpuParticles2D>("%PowerFlash");

        if (!string.IsNullOrEmpty(packedPath))
        {
            icon.Texture = ModPowerTextures.Load(packedPath);
        }

        if (!string.IsNullOrEmpty(bigPath))
        {
            flash.Texture = ModPowerTextures.Load(bigPath);
        }
    }

    private static void ApplyBufferPowerIcon(NPower instance)
    {
        Texture2D texture = ModPowerTextures.Load(ModBufferPowerUi.IconPath);
        TextureRect icon = instance.GetNode<TextureRect>("%Icon");
        CpuParticles2D flash = instance.GetNode<CpuParticles2D>("%PowerFlash");
        icon.Texture = texture;
        flash.Texture = texture;
    }
}
