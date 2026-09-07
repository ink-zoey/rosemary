using Terraria;
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
    }

    public override void SetDefaults()
    {
        Item.width = 14;
        Item.height = 26;
        Item.maxStack = Item.CommonMaxStack;

        Item.rare = ItemRarityID.Purple;

        Item.value = Item.buyPrice(gold: 3);
    }
}
