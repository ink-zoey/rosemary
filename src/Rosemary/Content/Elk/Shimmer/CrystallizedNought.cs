using Microsoft.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using Rosemary.Common;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Rosemary.Content.Elk;

public sealed class CrystallizedNought : ModItem
{
    public override string Texture => Assets.Elk.Shimmer.CrystallizedNought.KEY;

    public override string LocalizationCategory => "Content.Elk";

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 20;

        ItemID.Sets.SolidShimmerReaction[Type] = true;

        Main.itemAnimations[Type] = new DrawAnimationStatic(1, 4);
    }

    public override void SetDefaults()
    {
        Item.width = 14;
        Item.height = 26;
        Item.maxStack = Item.CommonMaxStack;

        Item.rare = ItemRarityID.Purple;

        Item.value = Item.buyPrice(gold: 3);
    }

    public override bool PreDrawInWorld(WorldItem item, SpriteBatch sb, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
    {
        Main.instance.DrawItem_GetBasics(item.inner, whoAmI, out var texture, out var frame, out _);

        var origin = frame.Size() * 0.5f;

        var off = new Vector2((item.width * 0.5f) - origin.X, item.height - frame.Height);

        var position = (item.position + origin + off);

        sb.Draw(texture, position - Main.screenPosition, frame, Color.Black, rotation, origin, scale, SpriteEffects.None, 0f);

        var right = Lighting.GetSubLight(position + new Vector2(item.width * 0.5f, 0f));
        var down = Lighting.GetSubLight(position + new Vector2(0f, item.height * 0.5f));
        var diagRight = Lighting.GetSubLight(position + new Vector2(item.width, item.height * 0.5f));
        var diagDown = Lighting.GetSubLight(position + new Vector2(item.width * 0.5f, item.height));

        var lightDirection = new Vector2(Sum(diagRight) - Sum(down), Sum(diagDown) - Sum(right));

        lightDirection = lightDirection.RotatedBy(-rotation);
        lightDirection.Y -= 0.15f;

        lightDirection = lightDirection.Normalized;

        var redColor = Color.White * (Vector2.Dot(Vector2.UnitX, lightDirection) + 0.25f);
        var greenColor = Color.White * (Vector2.Dot(-Vector2.UnitX, lightDirection) + 0.1f);
        var blueColor = Color.White * (Vector2.Dot(-Vector2.UnitY, lightDirection) + 0.4f);
        redColor.A = 0;
        greenColor.A = 0;
        blueColor.A = 0;

        sb.Draw(texture, position - Main.screenPosition, texture.Frame(1, 4, 0, 1), redColor, rotation, origin, scale, SpriteEffects.None, 0f);
        sb.Draw(texture, position - Main.screenPosition, texture.Frame(1, 4, 0, 2), greenColor, rotation, origin, scale, SpriteEffects.None, 0f);
        sb.Draw(texture, position - Main.screenPosition, texture.Frame(1, 4, 0, 3), blueColor, rotation, origin, scale, SpriteEffects.None, 0f);

        return false;

        static float Sum(Vector3 vector)
        {
            return vector.X + vector.Y + vector.Z;
        }
    }
}
