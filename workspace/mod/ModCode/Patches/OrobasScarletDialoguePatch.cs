using System;
using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Events;

namespace FatBaby.ModCode.Patches;

/// <summary>
/// Orobas has no Scarlet dialogue in vanilla; mod characters fall back to single-line ANY lines with no .next,
/// which makes the event feel stuck before relic options appear.
/// </summary>
[HarmonyPatch(typeof(Orobas), "DefineDialogues")]
internal static class OrobasScarletDialoguePatch
{
    private const string AncientEntry = "OROBAS";
    private const string AgnosticEntry = "ANY";

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
            new("", "")
            {
                VisitIndex = 0
            },
            new("", "")
            {
                VisitIndex = 1
            },
            new("", "")
            {
                VisitIndex = 4
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
        ApplyLineFallbacks(dialogue, characterEntry, dialogueIndex);

        for (int i = 0; i < dialogue.Lines.Count - 1; i++)
        {
            AncientDialogueLine line = dialogue.Lines[i];
            string locEntryKey = line.LineText?.LocEntryKey
                ?? throw new InvalidOperationException("Orobas dialogue line was not populated.");
            string lineKeyPrefix = locEntryKey[..locEntryKey.LastIndexOf('.')];
            string nextKey = lineKeyPrefix + ".next";
            if (LocString.Exists("ancients", nextKey))
            {
                line.NextButtonText = new LocString("ancients", nextKey);
            }
            else
            {
                line.NextButtonText = new LocString("ancients", "THE_ARCHITECT.CONTINUE");
            }
        }
    }

    /// <summary>
    /// Mod ancients.json only applies after PCK export; until then PopulateLines may pick .char keys with no text.
    /// Prefer mod lines, then vanilla OROBAS.talk.ANY.* which ships with the base game.
    /// </summary>
    private static void ApplyLineFallbacks(AncientDialogue dialogue, string characterEntry, int dialogueIndex)
    {
        for (int i = 0; i < dialogue.Lines.Count; i++)
        {
            AncientDialogueLine line = dialogue.Lines[i];
            if (line.LineText != null && LocString.Exists("ancients", line.LineText.LocEntryKey))
            {
                continue;
            }

            foreach (string entryKey in GetCandidateLineKeys(characterEntry, dialogueIndex, i))
            {
                if (!LocString.Exists("ancients", entryKey))
                {
                    continue;
                }

                line.LineText = new LocString("ancients", entryKey);
                line.Speaker = entryKey.EndsWith(".ancient", StringComparison.Ordinal)
                    ? AncientDialogueSpeaker.Ancient
                    : AncientDialogueSpeaker.Character;
                break;
            }
        }
    }

    private static IEnumerable<string> GetCandidateLineKeys(string characterEntry, int dialogueIndex, int lineIndex)
    {
        string modPrefix = $"{AncientEntry}.talk.{characterEntry}.{dialogueIndex}-{lineIndex}";
        string anyPrefix = $"{AncientEntry}.talk.{AgnosticEntry}.{dialogueIndex}-{lineIndex}";
        yield return $"{modPrefix}r.ancient";
        yield return $"{modPrefix}.ancient";
        yield return $"{modPrefix}r.char";
        yield return $"{modPrefix}.char";
        yield return $"{anyPrefix}r.ancient";
        yield return $"{anyPrefix}.ancient";
        yield return $"{anyPrefix}r.char";
        yield return $"{anyPrefix}.char";
    }
}
