using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Assets;

namespace Mod.ModCode.Powers;

/// <summary>
/// Marker base for mod powers that supply textures via <see cref="CustomPowerModel.CustomPackedIconPath"/>.
/// Combat icons are applied in <see cref="Patches.NPowerModIconPatch"/>.
/// </summary>
public abstract class ModCustomPowerModel : CustomPowerModel;

internal static class ModPowerTextures
{
    internal static Texture2D Load(string? path)
    {
        if (string.IsNullOrEmpty(path) || !ResourceLoader.Exists(path))
        {
            return PreloadManager.Cache.GetTexture2D("res://images/powers/missing_power.png");
        }

        return PreloadManager.Cache.GetTexture2D(path);
    }
}
