using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Saves.Runs;
using FatBaby.ModCode.Relics;

namespace FatBaby.ModCode;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "fat_baby";
    public const string ResPath = "res://mod";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(ElementFlaskRelic));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(RefinedElementFlaskRelic));
        Harmony harmony = new(ModId);
        harmony.PatchAll();
    }
}
