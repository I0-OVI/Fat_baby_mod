using System;
using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Events;

namespace FatBaby.ModCode.Patches;

[HarmonyPatch(typeof(TheArchitect), "get_DialogueSet")]
internal static class TheArchitectScarletDialoguePatch
{
    private const string AncientEntry = "THE_ARCHITECT";

    private static readonly string[] CharacterEntries =
    [
        "SCARLET_ACOLYTE",
        "mod-SCARLET_ACOLYTE",
        "MOD-SCARLET_ACOLYTE"
    ];

    [HarmonyPostfix]
    private static void AddScarletAcolyteDialogues(AncientDialogueSet __result)
    {
        foreach (string characterEntry in CharacterEntries)
        {
            if (__result.CharacterDialogues.ContainsKey(characterEntry))
            {
                continue;
            }

            __result.CharacterDialogues[characterEntry] = BuildDialogues(characterEntry);
        }
    }

    private static IReadOnlyList<AncientDialogue> BuildDialogues(string characterEntry)
    {
        AncientDialogue[] dialogues =
        [
            new("", "", "", "")
            {
                VisitIndex = 0,
                EndAttackers = ArchitectAttackers.Architect
            },
            new("", "")
            {
                VisitIndex = 1,
                EndAttackers = ArchitectAttackers.Architect
            }
        ];

        for (int i = 0; i < dialogues.Length; i++)
        {
            PopulateLocKeys(dialogues[i], characterEntry, i);
        }

        return dialogues;
    }

    private static void PopulateLocKeys(AncientDialogue dialogue, string characterEntry, int dialogueIndex)
    {
        dialogue.PopulateLines(AncientEntry, characterEntry, dialogueIndex);

        for (int i = 0; i < dialogue.Lines.Count - 1; i++)
        {
            AncientDialogueLine line = dialogue.Lines[i];
            string locEntryKey = line.LineText?.LocEntryKey ?? throw new InvalidOperationException("Architect dialogue line was not populated.");
            string lineKeyPrefix = locEntryKey[..locEntryKey.LastIndexOf('.')];
            line.NextButtonText = new LocString("ancients", lineKeyPrefix + ".next");
        }
    }
}
