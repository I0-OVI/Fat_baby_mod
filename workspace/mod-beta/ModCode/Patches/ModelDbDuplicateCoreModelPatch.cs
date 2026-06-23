using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace FatBaby.ModCode.Patches;

[HarmonyPatch]
internal static class ModelDbDuplicateCoreModelPatch
{
    private static readonly System.Reflection.Assembly GameAssembly = typeof(AbstractModel).Assembly;

    private static System.Reflection.MethodBase? TargetMethod() =>
        AccessTools.PropertyGetter(typeof(ModelDb), nameof(ModelDb.AllAbstractModelSubtypes));

    private static void Postfix(ref Type[] __result)
    {
        HashSet<string> gameModelNames = __result
            .Where(type => type.Assembly == GameAssembly)
            .Select(type => type.FullName)
            .Where(name => name != null)
            .ToHashSet()!;

        List<Type> filtered = new(__result.Length);
        foreach (Type type in __result)
        {
            string? fullName = type.FullName;
            if (type.Assembly != GameAssembly && fullName != null && gameModelNames.Contains(fullName))
            {
                MainFile.Logger.Info($"Filtered duplicate core model type from external assembly: {fullName} ({type.Assembly.GetName().Name})");
                continue;
            }

            filtered.Add(type);
        }

        if (filtered.Count != __result.Length)
        {
            __result = filtered.ToArray();
        }
    }
}
