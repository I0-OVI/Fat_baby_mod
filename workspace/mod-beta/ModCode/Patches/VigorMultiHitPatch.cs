using System;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Mod.ModCode.Patches;

/// <summary>
/// Vigor binds to one attack and should apply its stored bonus to every hit in that attack.
/// Use the snapshot taken in BeforeAttack instead of live Amount, and clear the binding afterward.
/// </summary>
[HarmonyPatch(typeof(VigorPower), nameof(VigorPower.ModifyDamageAdditive))]
internal static class VigorMultiHitModifyPatch
{
    private static readonly Type DataType = AccessTools.Inner(typeof(VigorPower), "Data")!;
    private static readonly FieldInfo CommandField = AccessTools.Field(DataType, "commandToModify")!;
    private static readonly FieldInfo StoredAmountField = AccessTools.Field(DataType, "amountWhenAttackStarted")!;
    private static readonly MethodInfo GetInternalData = AccessTools.Method(typeof(PowerModel), "GetInternalData")!.MakeGenericMethod(DataType);

    [HarmonyPostfix]
    private static void UseStoredAmountForBoundAttack(
        VigorPower __instance,
        Creature? dealer,
        ValueProp props,
        CardModel? cardSource,
        ref decimal __result)
    {
        if (__instance.Owner != dealer || !props.IsPoweredAttack())
        {
            return;
        }

        object data = GetInternalData.Invoke(__instance, null)!;
        if (CommandField.GetValue(data) is not AttackCommand command)
        {
            return;
        }

        if (cardSource != null && cardSource != command.ModelSource)
        {
            __result = 0m;
            return;
        }

        if (command.Attacker != dealer)
        {
            __result = 0m;
            return;
        }

        __result = (int)StoredAmountField.GetValue(data)!;
    }
}

[HarmonyPatch(typeof(VigorPower), nameof(VigorPower.AfterAttack))]
internal static class VigorMultiHitAfterAttackPatch
{
    private static readonly Type DataType = AccessTools.Inner(typeof(VigorPower), "Data")!;
    private static readonly FieldInfo CommandField = AccessTools.Field(DataType, "commandToModify")!;
    private static readonly MethodInfo GetInternalData = AccessTools.Method(typeof(PowerModel), "GetInternalData")!.MakeGenericMethod(DataType);

    [HarmonyPostfix]
    private static void ClearBoundCommand(VigorPower __instance, AttackCommand command)
    {
        object data = GetInternalData.Invoke(__instance, null)!;
        if (CommandField.GetValue(data) == command)
        {
            CommandField.SetValue(data, null);
        }
    }
}
