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

        ItemID.Sets.BlocksItemPickupsWhenHeld[Type] = true;

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
        Projectile.width = 24;
        Projectile.height = 24;
        Projectile.scale = 1f;

        Projectile.penetrate = -1;

        Projectile.friendly = false;
        Projectile.hostile = false;

        Projectile.tileCollide = true;
        Projectile.ignoreWater = true;
        Projectile.hide = true;

        Projectile.manualDirectionChange = true;
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

    private float clawInterpolator;

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        return false;
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

        if (HeldItem != -1)
        {
            clawInterpolator += 0.3f;
            clawInterpolator = MathF.Min(clawInterpolator, 1f);
        }
        else if (player.AltChannel)
        {
            clawInterpolator -= 0.05f;
            clawInterpolator = MathF.Max(clawInterpolator, -0.6f);
        }
        else
        {
            clawInterpolator = MathF.Lerp(clawInterpolator, 0f, 0.3f);
        }
    }

    private void UpdatePlayerHoldout(Player player)
    {
        const float min_length = 37f;
        const float max_length = 170f;
        const float inner_max_length = 155f;
        const float over_max_length = 185f;

        var center = player.RotatedRelativePoint(player.MountedCenter, true);

        Projectile.spriteDirection = Projectile.direction = Projectile.Center.X >= center.X ? 1 : -1;

        player.ChangeDir(Projectile.direction);
        player.heldProj = Projectile.whoAmI;
        Projectile.drawLayer = ProjectileDrawLayerID.HeldProj;
        player.SetDummyItemTime(2);

        var dir = (Projectile.Center - center) * Projectile.spriteDirection;
        var rotation = dir.ToRotation();
        player.itemRotation = rotation;

        Projectile.timeLeft = 4;

        if (Main.myPlayer != Projectile.owner)
        {
            return;
        }

        var target = Main.MouseWorld;
        target -= center;

        var currentLength = (Projectile.Center - center).Length();

        Projectile.velocity = GetVelocity(target) * 0.15f + player.velocity;

        var overExtended = currentLength > (Projectile.tileCollide ? over_max_length : inner_max_length);

        Projectile.tileCollide = !overExtended;

        var stillInUse = player is { channel: true, noItems: false, CCed: false };

        if (!stillInUse)
        {
            Projectile.Kill();
        }

        return;

        Vector2 GetVelocity(Vector2 target)
        {
            var targetLength = MathF.Clamp(target.Length(), min_length, max_length);

            var length = MathF.Lerp(currentLength, targetLength, 0.5f);

            target = target.WithLength(length);

            return target + center - Projectile.Center;
        }
    }

    private void GrabBehaviour(Player player)
    {
        var center = player.RotatedRelativePoint(player.MountedCenter, true);

        var rotation = (Projectile.Center - center).ToRotation();

        // We should drop the item if it's in a wall.
        if (!player.AltChannel
         || !Projectile.tileCollide)
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

            InitialRotation = rotation - Main.item[HeldItem].Rotation;
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
                var hitbox = item.Hitbox;

                hitbox.Inflate(8, 8);

                if (!hitbox.Intersects(Projectile.Hitbox))
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

            var item = Main.item[HeldItem];

            if (player.whoAmI != Main.myPlayer)
            {
                return;
            }

            var length = (Projectile.Center - center).Length();

            if (length > pickup_distance)
            {
                item.velocity = (Projectile.Center - center).WithLength(3.4f);

                var offset = Projectile.velocity * 2.3f;

                offset.Y *= 0.23f;

                offset = offset.WithLength(MathF.Min(offset.Length(), 10f));

                item.velocity += offset;
                item.Hidden = false;

                return;
            }

            item.noGrabDelay = 0;
            player.PickupItem(item);
        }

        void HoldItem()
        {
            var item = Main.item[HeldItem];

            item.noGrabDelay = 30;

            Main.instance.DrawItem_GetBasics(item.inner, item.whoAmI, out _, out var frame, out _);

            var offset = frame.Size() * 0.5f;

            offset += new Vector2((item.width * 0.5f) - offset.X, item.height - frame.Height);

            var position = Projectile.Center;

            // Cheap hack.
            if (!Collision.SolidCollision(Projectile.position - new Vector2(2), Projectile.width + 4, Projectile.height + 4))
            {
                position += Projectile.velocity;
            }

            item.position = position - offset;
            item.velocity = Vector2.Zero;
            item.Rotation = rotation - InitialRotation;

            item.onConveyor = false;
            item.shimmered = false;
            item.shimmerTime = 0f;

            item.Hidden = true;
        }
    }

    public override bool PreDraw(Player player, ref Color lightColor)
    {
        var texture = Assets.Misc.DinosaurExtendoGripBits.Asset.Value;

        var center = player.RotatedRelativePoint(player.MountedCenter, true);

        var effects = Projectile.spriteDirection == -1
            ? SpriteEffects.FlipHorizontally
            : SpriteEffects.None;

        var dir = (Projectile.Center - center) * Projectile.spriteDirection;
        var direction = dir.ToRotation();

        var centerDirection = Projectile.Center - center;

        var color = lightColor;

        DrawHandle();
        DrawHeldItem();
        DrawClaw();

        return false;

        void DrawHeldItem()
        {
            if (HeldItem == -1)
            {
                return;
            }

            Main.instance.DrawItem(Main.item[HeldItem], HeldItem);
        }

        void DrawHandle()
        {
            var frame = new Rectangle(82, 0, 16, 16);

            var origin = frame.Size() * 0.5f;

            var rotation = direction;
            rotation += MathF.PiOver4 * Projectile.spriteDirection;

            var position = center + centerDirection.WithLength(12f) - Main.screenPosition;

            Main.EntitySpriteDraw(texture, position, frame, color, rotation, origin, 1f, effects);
        }

        void DrawClaw()
        {
            var upperFrame = new Rectangle(32, 0, 22, 26);
            var upperOrigin = new Vector2(5, 25);

            var lowerFrame = new Rectangle(56, 0, 24, 16);
            var lowerOrigin = new Vector2(1, 13);

            var boltFrame = new Rectangle(20, 0, 10, 10);
            var boltOrigin = boltFrame.Size() * 0.5f;

            var position = Projectile.Center - centerDirection.WithLength(16f) - Main.screenPosition;

            if (effects.HasFlag(SpriteEffects.FlipHorizontally))
            {
                upperOrigin.X = upperFrame.Width - upperOrigin.X;
                lowerOrigin.X = lowerFrame.Width - lowerOrigin.X;
            }

            var jawRotation = Utils.Remap(clawInterpolator, 0f, 1f, 0.3f, -0.2f, clamped: false);

            var upperRotation = direction;
            upperRotation += (MathF.PiOver4 - jawRotation) * Projectile.spriteDirection;

            var lowerRotation = direction;
            lowerRotation += (MathF.PiOver4 + jawRotation) * Projectile.spriteDirection;

            Main.EntitySpriteDraw(texture, position, lowerFrame, color, lowerRotation, lowerOrigin, 1f, effects);
            Main.EntitySpriteDraw(texture, position, upperFrame, color, upperRotation, upperOrigin, 1f, effects);

            Main.EntitySpriteDraw(texture, position, boltFrame, color, 0f, boltOrigin, 1f, effects);
        }
    }
}
