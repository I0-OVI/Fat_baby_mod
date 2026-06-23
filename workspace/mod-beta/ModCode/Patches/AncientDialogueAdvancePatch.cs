using System.Collections.Generic;
using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace FatBaby.ModCode.Patches;

/// <summary>
/// Ancient dialogue bubbles are clickable but do not advance the conversation; only the invisible hitbox does.
/// </summary>
[HarmonyPatch(typeof(NAncientDialogueLine), "_Ready")]
internal static class AncientDialogueAdvancePatch
{
    [HarmonyPostfix]
    private static void ConnectAdvanceOnClick(NAncientDialogueLine __instance)
    {
        __instance.Connect(
            NClickableControl.SignalName.Released,
            Callable.From<NClickableControl>(_ => TryAdvance(__instance))
        );
    }

    private static void TryAdvance(NAncientDialogueLine line)
    {
        if (NEventRoom.Instance?.Layout is not NAncientEventLayout layout)
        {
            return;
        }

        List<AncientDialogueLine>? dialogue = AccessTools.Field(typeof(NAncientEventLayout), "_dialogue")
            ?.GetValue(layout) as List<AncientDialogueLine>;
        int currentLine = (int)(AccessTools.Field(typeof(NAncientEventLayout), "_currentDialogueLine")
            ?.GetValue(layout) ?? 0);
        VBoxContainer? dialogueContainer = AccessTools.Field(typeof(NAncientEventLayout), "_dialogueContainer")
            ?.GetValue(layout) as VBoxContainer;

        if (dialogue == null || dialogueContainer == null || currentLine >= dialogue.Count - 1)
        {
            return;
        }

        if (dialogueContainer.GetChildOrNull<NAncientDialogueLine>(currentLine) != line)
        {
            return;
        }

        AccessTools.Method(typeof(NAncientEventLayout), "OnDialogueHitboxClicked")
            ?.Invoke(layout, [line]);
    }
}
