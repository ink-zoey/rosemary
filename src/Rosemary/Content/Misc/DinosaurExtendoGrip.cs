using Daybreak.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using Rosemary.Common;
using System;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Rosemary.Content;

public sealed class DinosaurExtendoGrip : ModItem
{
    public override string Texture => Assets.Misc.DinosaurExtendoGrip.KEY;

    public override string LocalizationCategory => "Content.Misc";

    public override void Load()
    {
        IL_Main.DrawMouseOver += DrawMouseOver_DisplayHeldItemTooltip;

        On_Main.DrawMouseOver += DrawMouseOver_HideItemTooltips;
        MonoModHooks.Add(
            typeof(Player).GetMethod(
                nameof(Player.TileInteractionsMouseOver),
                BindingFlags.Instance | BindingFlags.NonPublic
            ),
            TileInteractions_HideTileIcons
        );
        MonoModHooks.Add(
            typeof(Player).GetMethod(
                nameof(Player.TileInteractionsCheckLongDistance),
                BindingFlags.Instance | BindingFlags.NonPublic
            ),
            TileInteractions_HideTileIcons
        );
    }

    private static void TileInteractions_HideTileIcons(Action<Player, int, int> orig, Player self, int myX, int myY)
    {
        if (self.PriorHeldProj == -1)
        {
            orig(self, myX, myY);
            return;
        }

        var projectile = Main.projectile[self.PriorHeldProj];

        if (projectile.ModProjectile is DinosaurExtendoGripHoldout holdout
         && holdout.HeldItem != -1)
        {
            return;
        }

        orig(self, myX, myY);
    }

    private static void DrawMouseOver_HideItemTooltips(On_Main.orig_DrawMouseOver orig, Main self)
    {
        if (Main.LocalPlayer.heldProj == -1)
        {
            orig(self);
            return;
        }

        var projectile = Main.projectile[Main.LocalPlayer.heldProj];

        if (projectile.ModProjectile is DinosaurExtendoGripHoldout holdout
         && holdout.HeldItem != -1
         && Main.LocalPlayer.cursorItemIconEnabled)
        {
            return;
        }

        orig(self);
    }

    private static void DrawMouseOver_DisplayHeldItemTooltip(ILContext il)
    {
        var c = new ILCursor(il);

        var worldItemIndexIndex = -1;

        c.GotoNext(
            MoveType.After,
            i => i.MatchLdsfld<Main>(nameof(Main.item)),
            i => i.MatchLdloc(out worldItemIndexIndex),
            i => i.MatchLdelemRef(),
            i => i.MatchCallvirt<WorldItem>($"get_{nameof(WorldItem.master)}")
        );

        c.GotoPrev(
            MoveType.After,
            i => i.MatchCall<Rectangle>(nameof(Rectangle.Intersects))
        );

        c.EmitLdloc(worldItemIndexIndex);
        c.EmitDelegate(
            static (int i) =>
            {
                if (Main.LocalPlayer.heldProj == -1)
                {
                    return false;
                }

                var projectile = Main.projectile[Main.LocalPlayer.heldProj];

                if (projectile.ModProjectile is not DinosaurExtendoGripHoldout holdout)
                {
                    return false;
                }

                return i == holdout.HeldItem;
            }
        );

        c.EmitOr();
    }

    public override void SetStaticDefaults()
    {
        ItemID.Sets.ShimmerTransformToItem[ItemID.ExtendoGrip] = Type;
        ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.ExtendoGrip;

        ItemID.Sets.BlocksItemPickupsWhenHeld[Type] = true;

        Item.ResearchUnlockCount = 1;
    }

    public override void SetDefaults()
    {
        Item.rare = ItemRarityID.Orange;
        Item.value = Item.buyPrice(0, 10);

        Item.UseSound = Assets.Misc.DinosaurExtendoGripCreak.Asset with
        {
            SoundLimitBehavior = SoundLimitBehavior.IgnoreNew,
            MaxInstances = 4,
            PitchRange = (-0.1f, 0.2f),
            Volume = 0.25f,
        };

        Item.useStyle = ItemUseStyleID.Shoot;
        Item.useAnimation = 10;
        Item.useTime = 10;
        Item.reuseDelay = 5;
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

    public override string LocalizationCategory => "Content.Misc";

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

    public float MaxReach
    {
        get => Projectile.ai[2];
        set => Projectile.ai[2] = value;
    }

    private float clawInterpolator;

    private float GetReach()
    {
        const float min_reach = 170f;

        return MathF.Max(MaxReach, min_reach);
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        return false;
    }

    public override void OnKill(int timeLeft)
    {
        if (HeldItem != -1)
        {
            LetGoOfItem(Main.player[Projectile.owner]);
        }
    }

    public override void OnSpawn(IEntitySource source)
    {
        HeldItem = -1;

        MaxReach = 0;

        if (Main.myPlayer == Projectile.owner)
        {
            MaxReach = Math.Min(Player.tileRangeX * 16f, 1000f);
        }

        Projectile.netUpdate = true;
    }

    public override void AI()
    {
        var player = Main.player[Projectile.owner];

        var stillInUse = player is { channel: true, noItems: false, CCed: false, dead: false };

        UpdatePlayerHoldout(player);

        GrabBehaviour(player);

        if (HeldItem != -1)
        {
            clawInterpolator += 0.3f;
            clawInterpolator = MathF.Min(clawInterpolator, 1f);
        }
        else if (player.AltChannel && stillInUse)
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
        const float min_length = 60f;
        const float min_speed = 7.4f;

        const int despawn_frames = 25;

        var stillInUse = player is { channel: true, noItems: false, CCed: false, dead: false };

        if (stillInUse)
        {
            Projectile.timeLeft = despawn_frames;
        }

        var lifetimeRatio = (float)Projectile.timeLeft / despawn_frames;

        var reach = GetReach();

        var innerMaxLength = reach - 15f;
        var overMaxLength = reach + 20f;

        var center = player.RotatedRelativePoint(player.MountedCenter, true);

        Projectile.spriteDirection = Projectile.direction = Projectile.Center.X >= center.X ? 1 : -1;

        player.ChangeDir(Projectile.direction);
        player.heldProj = Projectile.whoAmI;
        Projectile.drawLayer = ProjectileDrawLayerID.HeldProj;
        player.SetDummyItemTime(2);

        CompositeArm();

        var dir = (Projectile.Center - center) * Projectile.spriteDirection;
        player.itemRotation = dir.ToRotation();

        if (Main.myPlayer != Projectile.owner)
        {
            return;
        }

        var target = Main.MouseWorld;
        target -= center;

        var currentLength = (Projectile.Center - center).Length();

        Projectile.velocity = GetVelocity(target) * 0.15f;

        if (Projectile.velocity.Length() > min_speed && Rand.NextBoolean(10))
        {
            var soundPosition = Vector2.Lerp(center, Projectile.Center, 0.5f);

            SoundEngine.PlaySound(
                Assets.Misc.DinosaurExtendoGripCreak.Asset with
                {
                    SoundLimitBehavior = SoundLimitBehavior.IgnoreNew,
                    MaxInstances = 4,
                    PitchRange = (-0.1f, 0.2f),
                    Volume = 0.12f,
                },
                soundPosition
            );
        }

        Projectile.velocity += player.velocity;

        var overExtended = currentLength > (Projectile.tileCollide ? overMaxLength : innerMaxLength);

        Projectile.tileCollide = !overExtended && stillInUse;

        return;

        void CompositeArm()
        {
            var rotation = GetArmRotation(player);

            var offset = Utils.Remap(clawInterpolator, 0f, 1f, 0f, 0.4f, clamped: false);

            var backRotation = rotation + (offset * Projectile.spriteDirection);

            player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, backRotation);

            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);
        }

        Vector2 GetVelocity(Vector2 target)
        {
            var targetLength = MathF.Clamp(target.Length(), min_length, reach);

            var length = MathF.Lerp(currentLength, targetLength, 0.55f) * MathF.Pow(lifetimeRatio, 2f);

            target = target.WithLength(length);

            return target + center - Projectile.Center;
        }
    }

    private void GrabBehaviour(Player player)
    {
        var center = player.RotatedRelativePoint(player.MountedCenter, true);

        var rotation = (Projectile.Center - center).ToRotation();

        var alive = player is { noItems: false, CCed: false, dead: false };

        var overExtended = !Projectile.tileCollide && player.channel;

        // We should drop the item if it's in a wall.
        if (!player.AltChannel
         || !alive
         || overExtended)
        {
            if (HeldItem != -1)
            {
                LetGoOfItem(player);
            }

            HeldItem = -1;

            return;
        }

        if (HeldItem == -1 && TryFindItem(out var index))
        {
            HeldItem = index;

            InitialRotation = rotation - Main.item[HeldItem].Rotation;

            SoundEngine.PlaySound(
                SoundID.Item168 with
                {
                    Pitch = -0.8f,
                    PitchRange = (-0.1f, 0.2f),
                },
                Projectile.Center
            );
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

        void HoldItem()
        {
            var item = Main.item[HeldItem];

            if (!item.active)
            {
                HeldItem = -1;

                return;
            }

            item.noGrabDelay = 30;

            Main.instance.DrawItem_GetBasics(item.inner, item.whoAmI, out _, out var frame, out _);

            var offset = frame.Size() * 0.5f;

            offset += new Vector2((item.width * 0.5f) - offset.X, item.height - frame.Height);

            var position = Projectile.Center;

            Chest.AskForChestToOpenSilently(position, 10);

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

            item.Hidden = true;

            if (player.whoAmI != Main.myPlayer)
            {
                return;
            }

            var index = Chest.GetFreeChest(Projectile.Center.ToTileCoordinates());

            if (index == -1)
            {
                return;
            }

            player.cursorItemIconText = Mods.Rosemary.Content.Misc.DinosaurExtendoGrip.DepositItems.GetTextValue();
            player.cursorItemIconID = -1;
            player.cursorItemIconEnabled = true;
            Main.mouseText = true;
        }
    }

    private void LetGoOfItem(Player player)
    {
        const float pickup_distance = 90f;

        var center = player.RotatedRelativePoint(player.MountedCenter, true);

        var item = Main.item[HeldItem];

        if (player.whoAmI != Main.myPlayer)
        {
            return;
        }

        if (TryPlacingItemInContainers(Projectile.Center.ToTileCoordinates()))
        {
            return;
        }

        var length = (Projectile.Center - center).Length();

        if (length > pickup_distance)
        {
            DropItem();

            return;
        }

        item.noGrabDelay = 0;
        player.PickupItem(item);

        return;

        void DropItem()
        {
            item.velocity = (Projectile.Center - center).WithLength(3.4f);

            var offset = Projectile.velocity * 2.3f;

            offset.Y *= 0.23f;

            offset = offset.WithLength(MathF.Min(offset.Length(), 10f));

            item.velocity += offset;
            item.Hidden = false;
        }

        bool TryPlacingItemInContainers(Point position)
        {
            // TODO: Personal storage

            if (Chest.TransferWorldItem(
                    HeldItem,
                    Chest.GetFreeChest(position),
                    false,
                    ItemTransferVisualizationSettingsExt.HOPPER with
                    {
                        ShortAnimation = true,
                    }
                ))
            {
                return item.IsAir;
            }

            return false;
        }
    }

    public override bool PreDraw(Player player, ref Color lightColor)
    {
        var sb = Main.spriteBatch;

        var texture = Assets.Misc.DinosaurExtendoGripBits.Asset.Value;

        var center = player.GetFrontHandPosition(
            Player.CompositeArmStretchAmount.Full,
            GetArmRotation(player)
        );

        var effects = Projectile.spriteDirection == -1
            ? SpriteEffects.FlipHorizontally
            : SpriteEffects.None;

        var dir = (Projectile.Center - center) * Projectile.spriteDirection;
        var direction = dir.ToRotation();

        var centerDirection = Projectile.Center - center;

        var color = lightColor;

        var handlePosition = center + centerDirection.WithLength(4f);
        var clawPosition = Projectile.Center - centerDirection.WithLength(16f);

        DrawHeldItem();

        sb.End(out var ss);
        sb.Begin(ss with { SortMode = SpriteSortMode.Deferred });
        {
            DrawChain();
            DrawHandle();
            DrawClaw();
        }
        sb.Restart(in ss);

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

            var position = handlePosition - Main.screenPosition;

            var handleColor = Lighting.GetColor(handlePosition.ToTileCoordinates());

            sb.Draw(texture, position, frame, handleColor, rotation, origin, 1f, effects, 0f);
        }

        void DrawChain()
        {
            const float segment_size = 32;

            var reach = GetReach() + 20f;

            var brightFrame = new Rectangle(0, 0, 18, 6);

            var darkFrame = new Rectangle(0, 8, 18, 6);

            var origin = new Vector2(1, 3);

            var segments = (int)Math.Ceiling(reach / segment_size) - 1;

            for (var i = 0; i < segments; i++)
            {
                var position = Vector2.Lerp(handlePosition, clawPosition, (float)i / segments) - Main.screenPosition;
                var nextPosition = Vector2.Lerp(handlePosition, clawPosition, (float)(i + 1) / segments) - Main.screenPosition;

                DrawSegment(position, nextPosition);
            }

            return;

            void DrawSegment(Vector2 position, Vector2 nextPosition)
            {
                const float size = 16;

                var segmentDirection = nextPosition - position;

                var length = segmentDirection.Length();

                // Get the angle of the right triangle formed by base: length/2 hyp: size.

                var angle = MathF.Acos((length * 0.5f) / size);

                var rotation = segmentDirection.ToRotation();

                var worldPosition = Vector2.Lerp(position, nextPosition, 0.5f) + Main.screenPosition;
                var segmentColor = Lighting.GetColor(worldPosition.ToTileCoordinates());

                sb.Draw(texture, position, darkFrame, segmentColor, rotation - angle, origin, 1f, SpriteEffects.None, 0f);
                sb.Draw(texture, position, brightFrame, segmentColor, rotation + angle, origin, 1f, SpriteEffects.None, 0f);

                sb.Draw(texture, nextPosition, darkFrame, segmentColor, MathF.PI + rotation - angle, origin, 1f, SpriteEffects.None, 0f);
                sb.Draw(texture, nextPosition, brightFrame, segmentColor, MathF.PI + rotation + angle, origin, 1f, SpriteEffects.None, 0f);
            }
        }

        void DrawClaw()
        {
            var upperFrame = new Rectangle(32, 0, 22, 26);
            var upperOrigin = new Vector2(5, 25);

            var lowerFrame = new Rectangle(56, 0, 24, 16);
            var lowerOrigin = new Vector2(1, 13);

            var boltFrame = new Rectangle(20, 0, 10, 10);
            var boltOrigin = boltFrame.Size() * 0.5f;

            var position = clawPosition - Main.screenPosition;

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

            sb.Draw(texture, position, lowerFrame, color, lowerRotation, lowerOrigin, 1f, effects, 0f);
            sb.Draw(texture, position, upperFrame, color, upperRotation, upperOrigin, 1f, effects, 0f);

            sb.Draw(texture, position, boltFrame, color, 0f, boltOrigin, 1f, effects, 0f);
        }
    }

    private float GetArmRotation(Player player)
    {
        return (Projectile.Center - player.MountedCenter).ToRotation() - MathF.PiOver2 - player.fullRotation;
    }
}
