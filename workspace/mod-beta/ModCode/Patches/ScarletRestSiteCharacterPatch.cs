using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using Mod.ModCode.Character;

namespace Mod.ModCode.Patches;

[HarmonyPatch(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter.Create))]
internal static class ScarletRestSiteCharacterPatch
{
    private const string RestSiteTexturePath = "res://mod/images/rest_site/fat_baby_rest_site_sit_34_pct68.png";
    private const string SelectionReticleScenePath = "res://scenes/ui/selection_reticle.tscn";

    private static readonly FieldInfo? PlayerField = AccessTools.Field(typeof(NRestSiteCharacter), "<Player>k__BackingField");
    private static readonly FieldInfo? CharacterIndexField = AccessTools.Field(typeof(NRestSiteCharacter), "_characterIndex");

    [HarmonyPrefix]
    private static bool CreateScarletRestSiteCharacter(Player player, int characterIndex, ref NRestSiteCharacter __result)
    {
        if (player.Character is not ScarletAcolyte)
        {
            return true;
        }

        ScarletProgrammaticRestSiteCharacter character = BuildCharacterNode();
        PlayerField?.SetValue(character, player);
        CharacterIndexField?.SetValue(character, characterIndex);
        __result = character;
        return false;
    }

    private static ScarletProgrammaticRestSiteCharacter BuildCharacterNode()
    {
        ScarletProgrammaticRestSiteCharacter root = new()
        {
            Name = "ScarletAcolyteRestSite",
            Scale = new Vector2(0.5f, 0.5f)
        };

        Control controlRoot = new()
        {
            Name = "ControlRoot",
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        root.AddChild(controlRoot);

        TextureRect characterImage = new()
        {
            Name = "CharacterImage",
            Position = new Vector2(-461.5f, -690f),
            Size = new Vector2(923f, 912f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Texture = ResourceLoader.Load<Texture2D>(RestSiteTexturePath),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
        };
        controlRoot.AddChild(characterImage);

        NSelectionReticle selectionReticle = CreateSelectionReticle();
        selectionReticle.Name = "SelectionReticle";
        selectionReticle.UniqueNameInOwner = true;
        selectionReticle.Position = new Vector2(-500f, -690f);
        selectionReticle.Size = new Vector2(1000f, 950f);
        selectionReticle.MouseFilter = Control.MouseFilterEnum.Ignore;
        controlRoot.AddChild(selectionReticle);

        Control hitbox = new()
        {
            Name = "Hitbox",
            UniqueNameInOwner = true,
            Position = new Vector2(-500f, -690f),
            Size = new Vector2(1000f, 950f)
        };
        controlRoot.AddChild(hitbox);

        controlRoot.AddChild(CreateThoughtBubbleAnchor("ThoughtBubbleRight", new Vector2(360f, -640f)));
        controlRoot.AddChild(CreateThoughtBubbleAnchor("ThoughtBubbleLeft", new Vector2(-360f, -640f)));

        return root;
    }

    private static NSelectionReticle CreateSelectionReticle()
    {
        if (ResourceLoader.Exists(SelectionReticleScenePath))
        {
            PackedScene? scene = ResourceLoader.Load<PackedScene>(SelectionReticleScenePath);
            NSelectionReticle? reticle = scene?.Instantiate<NSelectionReticle>(PackedScene.GenEditState.Disabled);
            if (reticle != null)
            {
                return reticle;
            }
        }

        return new NSelectionReticle();
    }

    private static Control CreateThoughtBubbleAnchor(string name, Vector2 position)
    {
        return new Control
        {
            Name = name,
            UniqueNameInOwner = true,
            Position = position,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
    }
}

public partial class ScarletProgrammaticRestSiteCharacter : NRestSiteCharacter
{
}
