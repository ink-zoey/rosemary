using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Rosemary.Vanity.Content;

public sealed class SiffrinTransform : ModItem
{
    public override string Texture => Assets.Vanity.Hat.KEY;

    public override string LocalizationCategory => "Content";

    public override void Load()
    {
        if (Main.dedServ)
        {
            return;
        }

        EquipLoader.AddEquipTexture(Mod, Assets.Vanity.Undershirt_Equip.KEY, EquipType.Body, this);
        EquipLoader.AddEquipTexture(Mod, Assets.Vanity.Leggings_Equip.KEY, EquipType.Legs, this);

        On_PlayerDrawSet.BoringSetup_2 += BoringSetup_2_SkinColor;
        On_PlayerDrawLayers.DrawPlayer_21_Head += DrawPlayer_21_Head_HairStyle;
    }

    private static void DrawPlayer_21_Head_HairStyle(On_PlayerDrawLayers.orig_DrawPlayer_21_Head orig, ref PlayerDrawSet drawInfo)
    {
        if (!IsVisible(drawInfo))
        {
            orig(ref drawInfo);
            return;
        }

        var prior = drawInfo.drawPlayer.hair;
        drawInfo.drawPlayer.hair = ModContent.GetInstance<SiffrinHairstyle>().Type;
        {
            orig(ref drawInfo);
        }
        drawInfo.drawPlayer.hair = prior;
    }

    private static void BoringSetup_2_SkinColor(
        On_PlayerDrawSet.orig_BoringSetup_2 orig,
        ref PlayerDrawSet self,
        Player player,
        List<DrawData> drawData,
        List<int> dust,
        List<int> gore,
        Vector2 drawPosition,
        float shadowOpacity,
        float rotation,
        Vector2 rotationOrigin
    )
    {
        if (!IsVisible(self))
        {
            orig(ref self, player, drawData, dust, gore, drawPosition, shadowOpacity, rotation, rotationOrigin);
            return;
        }

        var priorEye = player.eyeColor;
        var priorSkin = player.skinColor;
        var priorHair = player.hairColor;
        player.eyeColor = Color.Black;
        player.skinColor = new Color(210, 210, 210, byte.MaxValue);
        player.hairColor = Color.White;
        {
            orig(ref self, player, drawData, dust, gore, drawPosition, shadowOpacity, rotation, rotationOrigin);
        }
        player.hairColor = priorHair;
        player.skinColor = priorSkin;
        player.eyeColor = priorEye;

        self.hairDyePacked = 0;
    }

    private static bool IsVisible(PlayerDrawSet drawInfo)
    {
        return drawInfo.drawPlayer.body == EquipLoader.GetEquipSlot(ModContent.GetInstance<ModImpl>(), ModContent.GetInstance<SiffrinTransform>().Name, EquipType.Body);
    }

    public override void SetStaticDefaults()
    {
        // var equipSlotBody = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Body);
        var equipSlotLegs = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Legs);

        ArmorIDs.Legs.Sets.HidesBottomSkin[equipSlotLegs] = true;
    }

    public override void SetDefaults()
    {
        Item.width = 20;
        Item.height = 20;

        Item.accessory = true;
        Item.vanity = true;

        if (Main.dedServ)
        {
            return;
        }

        var equipSlotBody = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Body);
        var equipSlotLegs = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Legs);

        Item.bodySlot = equipSlotBody;
        Item.legSlot = equipSlotLegs;
    }

    public override void UpdateVisibleAccessory(Player player, bool hideVisual)
    {
        if (hideVisual)
        {
            return;
        }
        
        player.body = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Body);
        player.legs = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Legs);
    }
}
