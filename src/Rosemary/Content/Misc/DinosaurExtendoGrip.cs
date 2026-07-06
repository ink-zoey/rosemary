using Microsoft.Xna.Framework;
using Rosemary.Common;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Rosemary.Content;

public sealed class DinosaurExtendoGrip : ModItem
{
    public override string Texture => Assets.Misc.DinosaurExtendoGrip.KEY;

    public override void SetStaticDefaults()
    {
        ItemID.Sets.SkipsInitialUseSound[Type] = true;
        ItemID.Sets.ShimmerTransformToItem[ItemID.ExtendoGrip] = Type;
        ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.ExtendoGrip;

        Item.ResearchUnlockCount = 1;
    }

    public override void SetDefaults()
    {
        Item.rare = ItemRarityID.Orange;
        Item.value = Item.buyPrice(0, 10);

        Item.UseSound = SoundID.Item95;

        Item.useStyle = ItemUseStyleID.Shoot;
        Item.useAnimation = 10;
        Item.useTime = 10;
        Item.reuseDelay = 5;
        Item.UseSound = SoundID.Item95;
        Item.autoReuse = true;
        Item.noUseGraphic = true;
        Item.noMelee = true;
        Item.channel = true;

        Item.shootSpeed = 1f;
        Item.shoot = ModContent.ProjectileType<DinosaurExtendoGripHoldout>();
    }

    public override bool CanUseItem(Player player)
    {
        return player.ownedProjectileCounts[Item.shoot] <= 0;
    }
}

public sealed class DinosaurExtendoGripHoldout : ModProjectile
{
    public override string Texture => Assets.Misc.DinosaurExtendoGrip.KEY;

    public override void SetDefaults()
    {
        Projectile.width = 30;
        Projectile.height = 30;
        Projectile.scale = 1f;

        Projectile.penetrate = -1;

        Projectile.friendly = false;
        Projectile.hostile = false;

        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        // Projectile.hide = true;
        Projectile.tileCollide = false;
    }

    public override void AI()
    {
        var player = Main.player[Projectile.owner];

        UpdatePlayerHoldout();

        

        return;

        void GrabBehaviour()
        {

        }

        void UpdatePlayerHoldout()
        {
            const float max_length = 120f;

            Projectile.spriteDirection = Projectile.direction;

            player.ChangeDir(Projectile.direction);
            player.heldProj = Projectile.whoAmI;
            player.SetDummyItemTime(2);

            var rotationOffset = Projectile.spriteDirection == -1 ? MathF.PI : 0;
            var dir = Projectile.velocity;

            player.itemRotation = dir.ToRotation() + rotationOffset;

            Projectile.timeLeft = 4;

            if (Main.myPlayer != Projectile.owner)
            {
                return;
            }

            var center = player.RotatedRelativePoint(player.MountedCenter, true);

            var offset = Main.MouseWorld - center;

            var targetLength = MathF.Min(offset.Length(), max_length);

            var length = MathHelper.Lerp((Projectile.Center - center).Length(), targetLength, 0.3f);

            offset = offset.Normalized * length;

            Projectile.Center = center + offset;

            Projectile.velocity = offset.Normalized;

            var stillInUse = player is { channel: true, noItems: false, CCed: false };

            if (!stillInUse)
            {
                Projectile.Kill();
            }
        }
    }
}
