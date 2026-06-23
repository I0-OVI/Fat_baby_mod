using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using FatBaby.ModCode.Character;

namespace FatBaby.ModCode.Patches;

[HarmonyPatch(typeof(NMerchantRoom), "AfterRoomIsLoaded")]
internal static class ScarletMerchantCharacterPatch
{
    private const string MerchantTexturePath = "res://mod/images/merchant/fat_baby_merchant_battle_task_style.png";

    private static readonly FieldInfo? PlayersField = AccessTools.Field(typeof(NMerchantRoom), "_players");
    private static readonly FieldInfo? CharacterContainerField = AccessTools.Field(typeof(NMerchantRoom), "_characterContainer");
    private static readonly FieldInfo? PlayerVisualsField = AccessTools.Field(typeof(NMerchantRoom), "_playerVisuals");

    [HarmonyPrefix]
    private static bool CreateScarletMerchantCharacter(NMerchantRoom __instance)
    {
        List<Player>? players = PlayersField?.GetValue(__instance) as List<Player>;
        Control? characterContainer = CharacterContainerField?.GetValue(__instance) as Control;
        List<NMerchantCharacter>? playerVisuals = PlayerVisualsField?.GetValue(__instance) as List<NMerchantCharacter>;
        if (players == null || characterContainer == null || playerVisuals == null)
        {
            return true;
        }

        Player? me = LocalContext.GetMe(players);
        if (me != null)
        {
            players.Remove(me);
            players.Insert(0, me);
        }

        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(players.Count));
        for (int row = 0; row < gridSize; row++)
        {
            float x = -140f * row;
            for (int col = 0; col < gridSize; col++)
            {
                int playerIndex = row * gridSize + col;
                if (playerIndex >= players.Count)
                {
                    break;
                }

                NMerchantCharacter character = CreateMerchantCharacter(players[playerIndex]);
                characterContainer.AddChildSafely(character);
                characterContainer.MoveChild(character, 0);
                character.Position = new Vector2(x, -50f * row);
                if (row > 0)
                {
                    character.Modulate = new Color(0.5f, 0.5f, 0.5f);
                }

                x -= 275f;
                playerVisuals.Add(character);
            }
        }

        return false;
    }

    private static NMerchantCharacter CreateMerchantCharacter(Player player)
    {
        if (player.Character is ScarletAcolyte)
        {
            return BuildScarletCharacterNode();
        }

        return PreloadManager.Cache
            .GetScene(player.Character.MerchantAnimPath)
            .Instantiate<NMerchantCharacter>(PackedScene.GenEditState.Disabled);
    }

    private static ScarletProgrammaticMerchantCharacter BuildScarletCharacterNode()
    {
        ScarletProgrammaticMerchantCharacter root = new()
        {
            Name = "ScarletAcolyteMerchant"
        };

        Texture2D texture = ResourceLoader.Load<Texture2D>(MerchantTexturePath);
        const float targetHeight = 520f;
        const float verticalOffset = 72f;
        float scale = targetHeight / texture.GetHeight();
        Sprite2D sprite = new()
        {
            Name = "CharacterImage",
            Centered = false,
            Texture = texture,
            Scale = new Vector2(scale, scale),
            Position = new Vector2(-texture.GetWidth() * scale * 0.5f, -texture.GetHeight() * scale + verticalOffset)
        };
        root.AddChild(sprite);

        return root;
    }
}

[HarmonyPatch(typeof(NMerchantCharacter), nameof(NMerchantCharacter.PlayAnimation))]
internal static class ScarletMerchantCharacterAnimationPatch
{
    [HarmonyPrefix]
    private static bool SkipStaticMerchantAnimation(NMerchantCharacter __instance) =>
        __instance is not ScarletProgrammaticMerchantCharacter;
}

public partial class ScarletProgrammaticMerchantCharacter : NMerchantCharacter
{
    public override void _Ready()
    {
    }
}
