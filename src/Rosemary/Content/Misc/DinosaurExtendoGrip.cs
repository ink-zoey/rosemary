using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rosemary.Common;
using System;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
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

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 3;
        ProjectileID.Sets.TrailingMode[Type] = 0;
    }

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
        Projectile.hide = true;
        Projectile.tileCollide = false;
    }

    public int HeldItem
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }

    public float InitialRotation
    {
        get => Projectile.ai[1];
        set => Projectile.ai[1] = value;
    }

    public override void OnSpawn(IEntitySource source)
    {
        HeldItem = -1;

        Projectile.netUpdate = true;
    }

    public override void AI()
    {
        var player = Main.player[Projectile.owner];

        UpdatePlayerHoldout(player);

        GrabBehaviour(player);
    }

    private void UpdatePlayerHoldout(Player player)
    {
        Projectile.spriteDirection = Projectile.direction;

        player.ChangeDir(Projectile.direction);
        player.heldProj = Projectile.whoAmI;
        player.SetDummyItemTime(2);

        Projectile.drawLayer = ProjectileDrawLayerID.HeldProj;

        var dir = Projectile.velocity * Projectile.spriteDirection;

        player.itemRotation = dir.ToRotation();

        Projectile.timeLeft = 4;

        if (Main.myPlayer != Projectile.owner)
        {
            return;
        }

        var center = player.RotatedRelativePoint(player.MountedCenter, true);

        var target = Main.MouseWorld;
        target -= center;

        Projectile.Center = GetPosition(target);

        Projectile.velocity = target.Normalized;

        var stillInUse = player is { channel: true, noItems: false, CCed: false };

        if (!stillInUse)
        {
            Projectile.Kill();
        }

        return;

        Vector2 GetPosition(Vector2 target)
        {
            const float min_length = 40f;
            const float max_length = 150f;

            var maxLength = MathF.Min(max_length, Scan((int)max_length / 2, max_length) - 15f);

            var targetLength = MathHelper.Clamp(target.Length(), min_length, maxLength);

            var length = MathHelper.Lerp((Projectile.Center - center).Length(), targetLength, 0.5f);

            target = target.WithLength(length);
            target += center;

            return Vector2.Lerp(Projectile.Center, target, 0.3f);
        }

        float Scan(int count, float length)
        {
            var laserScanResults = new float[count];

            Collision.LaserScan(center, Projectile.velocity, 10, length, laserScanResults);

            var averageLengthSample = laserScanResults.Sum() / count;

            return averageLengthSample;
        }
    }

    private void GrabBehaviour(Player player)
    {
        var size = new Vector2(16);

        if (HeldItem != -1)
        {
            size = Main.item[HeldItem].Hitbox.Size();
        }

        size *= 0.6f;

        var colliding = Collision.SolidCollision(Projectile.Center - size, (int)size.X * 2, (int)size.Y * 2);

        // We should drop the item if it's in a wall.
        if (!player.AltChannel
         || colliding)
        {
            if (HeldItem != -1)
            {
                LetGoOfItem();
            }

            HeldItem = -1;

            return;
        }

        if (HeldItem == -1 && TryFindItem(out var index))
        {
            HeldItem = index;

            InitialRotation = Projectile.velocity.ToRotation() + MathF.Tau - Main.item[HeldItem].Rotation;
        }

        if (HeldItem == -1)
        {
            return;
        }

        HoldItem();

        return;

        bool TryFindItem(out int index)
        {
            index = -1;

            foreach (var item in Main.ActiveItems)
            {
                if (item.shimmered || !item.Hitbox.Intersects(Projectile.Hitbox))
                {
                    continue;
                }

                index = item.whoAmI;

                return true;
            }

            return false;
        }

        void LetGoOfItem()
        {
            const float pickup_distance = 50f;

            const float drop_speed = 3.4f;

            var item = Main.item[HeldItem];

            if (player.whoAmI != Main.myPlayer)
            {
                return;
            }

            var center = player.RotatedRelativePoint(player.MountedCenter, true);

            var length = (Projectile.Center - center).Length();

            if (length > pickup_distance)
            {
                item.velocity = Projectile.velocity.WithLength(drop_speed);

                var offset = AveragePositionChanges();

                offset.Y *= 0.2f;

                item.velocity += offset;

                return;
            }

            item.noGrabDelay = 0;
            player.PickupItem(item);

            return;

            // Gives us a more accurate velocity change, as opposed to just the prior position.
            Vector2 AveragePositionChanges()
            {
                var offset = Vector2.Zero;

                for (var i = 0; i < Projectile.oldPos.Length; i++)
                {
                    var prior = i == 0 ? Projectile.position : Projectile.oldPos[i - 1];

                    offset += prior - Projectile.oldPos[i];
                }

                offset /= Projectile.oldPos.Length;

                return offset;
            }
        }

        void HoldItem()
        {
            var item = Main.item[HeldItem];

            if (item.shimmered)
            {
                HeldItem = -1;

                return;
            }

            item.noGrabDelay = 30;

            Main.instance.DrawItem_GetBasics(item.inner, item.whoAmI, out _, out var frame, out _);

            var offset = frame.Size() * 0.5f;

            offset += new Vector2((item.width * 0.5f) - offset.X, item.height - frame.Height);

            item.position = Projectile.Center - offset;

            item.onConveyor = false;

            item.velocity = Vector2.Zero;

            var rotation = Projectile.velocity.ToRotation() + MathF.Tau - InitialRotation;
            item.Rotation = rotation;
        }
    }

    public override bool PreDraw(Player player, ref Color lightColor)
    {
        var texture = Assets.Misc.DinosaurExtendoGripBits.Asset.Value;

        var playerCenter = player.RotatedRelativePoint(player.MountedCenter, true);

        var color = lightColor;

        DrawHandle();
        DrawClaw();

        return false;

        void DrawHandle()
        {
            var frame = new Rectangle(82, 0, 16, 16);

            var origin = frame.Size() * 0.5f;

            var rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

            var position = playerCenter + Projectile.velocity.WithLength(12f);

            Main.EntitySpriteDraw(texture, position - Main.screenPosition, frame, color, rotation, origin, 1f, SpriteEffects.None);
        }

        void DrawClaw()
        {
            const float offset = 30f;

            var frame = new Rectangle(20, 0, 10, 10);

            var origin = frame.Size() * 0.5f;

            var centerDirection = Projectile.Center - playerCenter;
            centerDirection = centerDirection.WithLength(offset);

            var position = Projectile.Center - centerDirection;

            var dir = Projectile.spriteDirection == -1
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;

            Main.EntitySpriteDraw(texture, position - Main.screenPosition, frame, color, 0f, origin, 1f, dir);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, color, 0f, origin, 1f, dir);
        }
    }
}
