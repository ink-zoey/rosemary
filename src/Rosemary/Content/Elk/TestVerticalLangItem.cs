using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Rosemary.Content.Elk;

public sealed class TestVerticalLangItem : ModItem
{
    public override string Texture => Assets.Elk.TestItem.KEY;

    public override void SetStaticDefaults()
    {
        ItemID.Sets.UsesElkName[Type] = ItemID.Sets.UsesElkName[ItemID.TerraBlade] =
            ElkLanguage.NewPhrase
                       .CrownBLeft   .UseHeight(-12f)
                       .CrownBRight  .UseHeight(27f) .UseOffset(new Vector2(4f, -4f))
                       .OpenBracketA                 .UseOffset(new Vector2(-10f, -5f))
                       .EyeRightFlare.UseHeight(0f)  .UseOffset(new Vector2(-10f, -15f))
                       .BranchLeftC                  .UseOffset(new Vector2(-8f, 0f))
                       .LeafA        .UseHeight(0f)  .UseOffset(new Vector2(17f, 3f))
                       .ExcuseLeftA                  .UseOffset(new Vector2(0f, 9f))
                       .Space        .UseHeight(14f)
                       .SpearA       .UseHeight(0f)  .UseOffset(new Vector2(10f, -5f))
                       .DotSmall                     .UseOffset(new Vector2(-10f, 0f))
                       .CloseBracketA                .UseOffset(new Vector2(-10f, 5f))
                       .DiamondA     .UseHeight(42f) .UseOffset(new Vector2(-10f, 13f))
                       .RootsB       .UseHeight(33f) .UseOffset(new Vector2(2f, 4f))
                       .FullStopAlt;
    }

    public override void SetDefaults()
    {
        Item.width = 40;
        Item.height = 40;

        Item.useStyle = ItemUseStyleID.Swing;
        Item.useTime = 20;
        Item.useAnimation = 20;
        Item.autoReuse = true;

        Item.DamageType = DamageClass.Melee;
        Item.damage = 50;

        Item.value = Item.buyPrice(gold: 1);
        Item.UseSound = SoundID.Item1;

        // fuck me i guess
        Item.AllowReforgeForStackableItem = true;

        Item.rare = ItemRarityID.Purple;
    }
}
