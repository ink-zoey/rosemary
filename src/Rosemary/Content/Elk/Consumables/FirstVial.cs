using System;
using Microsoft.Xna.Framework;
using Rosemary.Common;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Rosemary.Content.Elk;

public sealed class FirstVial : ModItem, IViolentShimmerReactant
{
    public override string Texture => Assets.Elk.Consumables.FirstVial.KEY;

    public override string LocalizationCategory => "Content.Elk";

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

        ItemID.Sets.ViolentShimmerReaction[Type] = true;
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

    bool IViolentShimmerReactant.Ejection(WorldItem item, bool subSurface)
    {
        for (var i = 0; i < (subSurface ? 12 : 6); i++)
        {
            var velocity = -Vector2.UnitY * Rand.Next(4f, 11f);
            velocity = velocity.RotatedByRandom(subSurface ? MathF.PI : 0.7f);

            var gore = Gore.NewGorePerfect(new EntitySource_Parent(item, "SHIMMER_BAD"), item.Center, velocity, ModContent.GoreType<VialGore>());

            if (subSurface)
            {
                gore.scale = 0.7f;
            }

            gore.ShimmerData ??= new ShimmerReactionGore.ShimmerReactionData
            {
                Shimmering = false,
                SpawnedSubSurface = false,
            };

            gore.ShimmerData.SpawnedSubSurface = subSurface;
        }

        if (subSurface)
        {
            return true;
        }

        var bright = new Color(179, 133, 255, 120);
        for (var i = 0; i < 17; i++)
        {
            var velocity = -Vector2.UnitY * Rand.Next(2f, 7f);
            velocity = velocity.RotatedByRandom(MathF.PiOver2);

            var offset = Vector2.Normalize(velocity) * 10f;

            ElkParticles.Sparks += new ElkParticles.Spark(
                item.Center + offset,
                velocity,
                Main.rand.NextFloat(2.5f, 6f),
                bright,
                Rand.Next((byte)3)
            );
        }

        return true;
    }
}

file sealed class VialGore : ShimmerReactionGore
{
    public override string Texture => Assets.Elk.Consumables.FirstVialShatter.KEY;

    public override void OnSpawn(Gore gore, IEntitySource source)
    {
        base.OnSpawn(gore, source);

        gore.Frame = new SpriteFrame(3, 1, Rand.Next((byte)3), 0);
    }
}
