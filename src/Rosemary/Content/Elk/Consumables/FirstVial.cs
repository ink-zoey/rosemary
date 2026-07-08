using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Rosemary.Content.Elk;

public sealed class FirstVial : ModItem
{
    public override string Texture => Assets.Elk.Consumables.FirstVial.KEY;

    public override string LocalizationCategory => Mods.Rosemary.Content.Elk.KEY;

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 20;

        ItemID.Sets.DrinkParticleColors[Type] = [
            new Color(179, 133, 255),
        ];

        ItemID.Sets.UsesElkName[Type] = ItemID.Sets.UsesElkName[ItemID.TerraBlade] =
            ElkLanguage.NewPhrase
                       .EyeUp
                       .ConstellationA.UseHeight(9f).UseOffset(new Vector2(-20f, 0f))
                       .IBeam.UseOffset(new Vector2(6f, 0f))
                       .BranchRightC.UseOffset(new Vector2(4f, 4f))
                       .Space.UseHeight(12f)
                       .DotSmall.UseHeight(0f).UseOffset(new Vector2(18f, -4f))
                       .RootsC.UseHeight(20f)
                       .CurlB.UseHeight(15f)
                       .LargeRightSpike.UseOffset(new Vector2(0, -13f))
                       .FullStop;
    }

    public override void SetDefaults()
    {
        Item.width = 14;
        Item.height = 26;
        Item.useStyle = ItemUseStyleID.DrinkLiquid;
        Item.useAnimation = 23;
        Item.useTime = 23;
        Item.useTurn = true;
        // Item.UseSound = Sounds.InkEffectDrinkStart;
        Item.maxStack = Item.CommonMaxStack;
        Item.consumable = true;

        Item.rare = ItemRarityID.Purple;

        Item.value = Item.buyPrice(gold: 3);
        // Item.buffType = ModContent.BuffType<InkDrugStatBuff>();
        // Item.buffTime = 36000;
    }
}
