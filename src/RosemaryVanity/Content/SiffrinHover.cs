using Daybreak.Rendering;
using Daybreak.Rendering.Buffers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rosemary.Common;
using Rosemary.Core;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.ModLoader;
using static System.Net.Mime.MediaTypeNames;

namespace Rosemary.Vanity.Content;

public sealed class SiffrinHover : ModItem
{
    public override string Texture => Assets.Vanity.Hat.KEY;

    public override string LocalizationCategory => "Content";

    public override void SetDefaults()
    {
        Item.mountType = ModContent.MountType<SiffrinHoverMount>();
    }
}

public sealed class SiffrinHoverBuff : ModBuff
{
    public override string Texture => Assets.Vanity.Hat.KEY;

    public override string LocalizationCategory => "Content";

    public override void SetStaticDefaults()
    {
        BuffID.Sets.MountType[Type] = ModContent.MountType<SiffrinHoverMount>();
    }
}

[Autoload(Side = ModSide.Client)]
file sealed class OutlineAfterImagesPlayer : ModPlayer
{
    private const float max_scale = 5f;

    public record struct AfterImageInfo(Vector2 Position, float Rotation, Vector2 Origin, float Scale, int Direction, int GravityDirection) : IUpdatingParticle
    {
        public static AfterImageInfo FromPlayer(Player player)
        {
            return new AfterImageInfo
            {
                Position = player.position,
                Rotation = player.fullRotation,
                Origin = player.fullRotationOrigin,
                Scale = 1f,
                Direction = player.direction,
                GravityDirection = (int)player.gravDir,
            };
        }

        bool IUpdatingParticle.Update()
        {
            Scale += 0.2f;

            return Scale < max_scale;
        }
    }

    public UpdatingParticleHandler<AfterImageInfo> AfterImages = new(10);

    public override void PostUpdate()
    {
        AfterImages.Update();

        if (!Player.mount.Active
         || Player.mount.Type != ModContent.MountType<SiffrinHoverMount>())
        {
            return;
        }

        if (Main.timeForVisualEffects % 10 == 0)
        {
            AfterImages += AfterImageInfo.FromPlayer(Player);
        }
    }

    public override void DrawPlayer(Camera camera)
    {
        if (AfterImages.ActiveParticleCount <= 0)
        {
            return;
        }

        var sb = Main.spriteBatch;

        var device = Main.graphics.GraphicsDevice;

        var effect = Assets.Vanity.PlayerOutline.CreatePlayerOutlineShader();

        using var lease = RenderTargetPool.Shared.Rent(device, device.Viewport.Width, device.Viewport.Height);

        sb.End(out var ss);

        using (lease.Scope(clearColor: Color.Transparent))
        {
            sb.Begin(ss with { TransformMatrix = Matrix.Identity });
            {
                var prior = Main.GameViewMatrix._transformationMatrix;
                Main.GameViewMatrix._transformationMatrix = Matrix.Identity;

                Main.PlayerRenderer.DrawPlayer(camera, Player, Player.position, Player.fullRotation, Player.fullRotationOrigin, 0.001f);

                Main.GameViewMatrix._transformationMatrix = prior;
            }
            sb.End();
        }

        effect.Parameters.StepSize = 2f;
        effect.Parameters.MaxScale = max_scale;

        effect.Parameters.BaseColor = Color.Red.ToVector4();

        // effect.Apply();

        sb.Begin(ss with { SortMode = SpriteSortMode.Deferred, SamplerState = SamplerState.PointClamp, CustomEffect = null });
        {
            foreach (var index in AfterImages)
            {
                var image = AfterImages[index];

                var position = image.Position - Main.screenPosition;

                var origin = (lease.Target.Size() * 0.5f) + image.Origin;

                var interpolator = image.Scale / max_scale;

                var color = new Color(new Vector4(interpolator));

                var effects = image.Direction == Player.direction ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                effects |= image.GravityDirection == (int)Player.gravDir ? SpriteEffects.None : SpriteEffects.FlipVertically;

                sb.Draw(lease.Target, position, null, Color.White, image.Rotation, origin, 2f, effects, 0f);
            }

            sb.Draw(lease.Target, new Vector2(90, 2), null, Color.White);
        }
        sb.Restart(in ss);
    }
}

public sealed class SiffrinHoverMount : ModMount
{
    public override string Texture => string.Empty;

    public override void Load()
    {
        On_PlayerDrawLayers.DrawPlayer_RenderAllLayers += DrawPlayer_RenderAllLayers_CapturePlayer;
        On_LegacyPlayerRenderer.DrawPlayerInternal += DrawPlayerInternal_DisableLighting;
    }

    private void DrawPlayerInternal_DisableLighting(
        On_LegacyPlayerRenderer.orig_DrawPlayerInternal orig,
        LegacyPlayerRenderer self,
        Camera camera,
        Player drawPlayer,
        Vector2 position,
        float rotation,
        Vector2 rotationOrigin,
        float shadow,
        float alpha,
        float scale,
        bool headOnly
    )
    {
        if(!drawPlayer.mount.Active
         || drawPlayer.mount.Type != ModContent.MountType<SiffrinHoverMount>()
         || headOnly)
        {
            orig(self, camera, drawPlayer, position, rotation, rotationOrigin, shadow, alpha, scale, headOnly);

            return;
        }

        using (new FullBrightScope())
        {
            orig(self, camera, drawPlayer, position, rotation, rotationOrigin, shadow, alpha, scale, headOnly);
        }
    }

    private void DrawPlayer_RenderAllLayers_CapturePlayer(On_PlayerDrawLayers.orig_DrawPlayer_RenderAllLayers orig, ref PlayerDrawSet drawInfo)
    {
        if (!drawInfo.drawPlayer.mount.Active
         || drawInfo.drawPlayer.mount.Type != ModContent.MountType<SiffrinHoverMount>()
         || drawInfo.headOnlyRender
         || drawInfo.shadow > 0)
        {
            orig(ref drawInfo);

            return;
        }

        var effect = Assets.Vanity.InvertPlayer.CreateInvertPlayerShader();

        var sb = Main.spriteBatch;

        var device = Main.graphics.GraphicsDevice;

        sb.End(out var ss);

        using var lease = RenderTargetPool.Shared.Rent(device, device.Viewport.Width, device.Viewport.Height);

        using (lease.Scope(clearColor: Color.Transparent))
        {
            sb.Begin(ss with { TransformMatrix = Matrix.Identity });

            orig(ref drawInfo);

            sb.End();
        }

        sb.Begin(ss with { SortMode = SpriteSortMode.Immediate });

        for (var i = 0; i < 4; i++)
        {
            var offset = new Vector2(2, 0).RotatedBy((i / 4f) * MathF.Tau) * Main.GameZoomTarget;

            sb.Draw(lease.Target, offset, Color.Black);
        }

        var player = drawInfo.drawPlayer;

        var top = new Vector2(player.Center.X, player.Top.Y) - Main.screenPosition;
        var bottom = new Vector2(player.Center.X, player.Bottom.Y) - Main.screenPosition;

        effect.Parameters.PlayerTop = top.Y;
        effect.Parameters.PlayerBottom = bottom.Y;

        effect.Apply();

        sb.Draw(lease.Target, Vector2.Zero, Color.White);

        sb.Restart(in ss);
    }

    public override void SetStaticDefaults()
    {
        MountID.Sets.DoesNotOverrideBackpackDraw[Type] = true;
        MountID.Sets.DoesNotOverrideBodyFrames[Type] = true;
        MountID.Sets.DoesNotOverrideLegFrames[Type] = true;
        MountID.Sets.IgnoresHoverFatigue[Type] = true;
        MountID.Sets.CanUseHooks[Type] = true;

        MountData.buff = ModContent.BuffType<SiffrinHoverBuff>();

        MountData.heightBoost = 0;

        MountData.flightTimeMax = 1;
        MountData.fatigueMax = 1;

        MountData.fallDamage = 0f;

        MountData.usesHover = true;

        MountData.runSpeed = 9f;
        MountData.dashSpeed = 9f;
        MountData.acceleration = 0.36f;
        MountData.jumpHeight = 10;
        MountData.jumpSpeed = 4f;

        MountData.blockExtraJumps = true;

        MountData.playerHeadOffset = 0;
        MountData.playerYOffsets = [0];

        MountData.totalFrames = 1;

        MountData.standingFrameCount = 1;
        MountData.standingFrameDelay = 0;
        MountData.standingFrameStart = 0;

        MountData.runningFrameCount = 1;
        MountData.runningFrameDelay = 0;
        MountData.runningFrameStart = 0;

        MountData.flyingFrameCount = 1;
        MountData.flyingFrameDelay = 0;
        MountData.flyingFrameStart = 0;

        MountData.inAirFrameCount = 1;
        MountData.inAirFrameDelay = 0;
        MountData.inAirFrameStart = 0;

        MountData.idleFrameCount = 0;
        MountData.idleFrameDelay = 0;
        MountData.idleFrameStart = 0;
        MountData.idleFrameLoop = true;

        MountData.swimFrameCount = 0;
        MountData.swimFrameDelay = 0;
        MountData.swimFrameStart = 0;
    }

    public override void SetMount(Player player, ref bool skipDust)
    {
        skipDust = true;
    }
}
