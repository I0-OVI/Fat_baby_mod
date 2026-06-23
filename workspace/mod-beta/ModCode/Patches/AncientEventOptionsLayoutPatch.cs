using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace FatBaby.ModCode.Patches;

/// <summary>
/// OnSetupComplete scrolls before option buttons have a layout size, hiding relic choices on single-line ancients.
/// </summary>
[HarmonyPatch(typeof(NAncientEventLayout), nameof(NAncientEventLayout.OnSetupComplete))]
internal static class AncientEventOptionsLayoutPatch
{
    [HarmonyPostfix]
    private static void ScheduleOptionsLayoutRefresh(NAncientEventLayout __instance)
    {
        ScheduleRefresh(__instance);
    }

    internal static void ScheduleRefresh(NAncientEventLayout layout)
    {
        TaskHelper.RunSafely(RefreshOptionsLayoutAfterLayout(layout));
    }

    private static async Task RefreshOptionsLayoutAfterLayout(NAncientEventLayout layout)
    {
        await Cmd.Wait(0.05f);
        RefreshOptionsLayout(layout);
        await Cmd.Wait(0.15f);
        RefreshOptionsLayout(layout);
    }

    private static void RefreshOptionsLayout(NAncientEventLayout layout)
    {
        List<AncientDialogueLine>? dialogue = AccessTools.Field(typeof(NAncientEventLayout), "_dialogue")
            ?.GetValue(layout) as List<AncientDialogueLine>;
        if (dialogue == null || dialogue.Count == 0)
        {
            return;
        }

        int currentLine = (int)(AccessTools.Field(typeof(NAncientEventLayout), "_currentDialogueLine")
            ?.GetValue(layout) ?? 0);
        if (currentLine < dialogue.Count - 1)
        {
            return;
        }

        if (AccessTools.Field(typeof(NAncientEventLayout), "_content")?.GetValue(layout) is not VBoxContainer content
            || AccessTools.Field(typeof(NAncientEventLayout), "_contentContainer")?.GetValue(layout) is not Control contentContainer
            || AccessTools.Field(typeof(NAncientEventLayout), "_dialogueContainer")?.GetValue(layout) is not VBoxContainer dialogueContainer
            || AccessTools.Field(typeof(NAncientEventLayout), "_optionsContainer")?.GetValue(layout) is not VBoxContainer optionsContainer)
        {
            return;
        }

        if (AccessTools.Field(typeof(NAncientEventLayout), "_contentTween")?.GetValue(layout) is Tween tween)
        {
            tween.Pause();
            tween.CustomStep(1.0);
            tween.Kill();
            AccessTools.Field(typeof(NAncientEventLayout), "_contentTween")?.SetValue(layout, null);
        }

        if (AccessTools.Field(typeof(NAncientEventLayout), "_originalContentContainerHeight")?.GetValue(layout) is float originalHeight)
        {
            contentContainer.Size = new Vector2(contentContainer.Size.X, originalHeight);
        }

        optionsContainer.ResetSize();
        float optionsHeight = optionsContainer.Size.Y;
        if (optionsHeight <= 1f)
        {
            optionsHeight = optionsContainer.GetCombinedMinimumSize().Y;
        }
        if (optionsHeight <= 1f)
        {
            optionsHeight = 220f;
        }

        NAncientDialogueLine? activeLine = dialogueContainer.GetChildOrNull<NAncientDialogueLine>(currentLine);
        float dialogueBottom = activeLine == null ? 0f : activeLine.Position.Y + activeLine.Size.Y;
        float targetY = contentContainer.Size.Y - dialogueBottom - optionsHeight - 18f;

        content.Position = new Vector2(content.Position.X, targetY);
        foreach (NEventOptionButton optionButton in layout.OptionButtons)
        {
            optionButton.Modulate = Colors.White;
            optionButton.FocusMode = Control.FocusModeEnum.All;
            optionButton.EnableButton();
        }

        layout.DefaultFocusedControl?.TryGrabFocus();
    }
}

[HarmonyPatch(typeof(NAncientEventLayout), "OnDialogueHitboxClicked")]
internal static class AncientEventDialogueAdvanceLayoutPatch
{
    [HarmonyPostfix]
    private static void RefreshAfterDialogueAdvance(NAncientEventLayout __instance)
    {
        AncientEventOptionsLayoutPatch.ScheduleRefresh(__instance);
    }
}
