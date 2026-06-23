using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using FatBaby.ModCode.Cards;

namespace FatBaby.ModCode.Patches;

[HarmonyPatch(typeof(StrengthPower), nameof(StrengthPower.ModifyDamageAdditive))]
internal static class StrengthPowerMagicDamagePatch
{
    [HarmonyPostfix]
    private static void IgnoreMagicDamageCards(CardModel? cardSource, ref decimal __result)
    {
        if (cardSource is IMagicAttributeCard)
        {
            __result = 0m;
        }
    }
}
