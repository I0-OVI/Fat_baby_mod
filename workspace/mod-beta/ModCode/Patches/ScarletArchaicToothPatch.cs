using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using FatBaby.ModCode.Cards;

namespace FatBaby.ModCode.Patches;

internal static class ScarletArchaicToothMapping
{
    private static bool _isInstalled;

    public static void EnsureInstalled()
    {
        if (_isInstalled || !ModelDb.Contains(typeof(ChargedAttack)) || !ModelDb.Contains(typeof(ChargedHeavyAttack)))
        {
            return;
        }

        var field = AccessTools.Field(typeof(ArchaicTooth), "TranscendenceUpgrades");
        if (field?.GetValue(null) is not Dictionary<ModelId, CardModel> upgrades)
        {
            return;
        }

        upgrades[ModelDb.Card<ChargedAttack>().Id] = ModelDb.Card<ChargedHeavyAttack>();
        _isInstalled = true;
    }
}

[HarmonyPatch(typeof(ArchaicTooth), nameof(ArchaicTooth.SetupForPlayer))]
internal static class ScarletArchaicToothSetupPatch
{
    [HarmonyPrefix]
    private static void InstallScarletTranscendence()
    {
        ScarletArchaicToothMapping.EnsureInstalled();
    }
}

[HarmonyPatch(typeof(ArchaicTooth), nameof(ArchaicTooth.AfterObtained))]
internal static class ScarletArchaicToothPickupPatch
{
    [HarmonyPrefix]
    private static void InstallScarletTranscendence()
    {
        ScarletArchaicToothMapping.EnsureInstalled();
    }
}

[HarmonyPatch(typeof(DustyTome), nameof(DustyTome.SetupForPlayer))]
internal static class ScarletDustyTomeSetupPatch
{
    [HarmonyPrefix]
    private static void InstallScarletTranscendence()
    {
        ScarletArchaicToothMapping.EnsureInstalled();
    }
}
