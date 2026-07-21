using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Daybreak.Rendering;
using Daybreak.Rendering.Buffers;
using Rosemary.Common;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

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
         || drawInfo.headOnlyRender)
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

        using (RedRipples.RippleMaskTarget.Scope(clearColor: Color.Transparent))
        {
            sb.Begin(ss with { TransformMatrix = Matrix.Identity });

            sb.Draw(lease.Target, Vector2.Zero, Color.White);

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

    public override void UpdateEffects(Player player)
    {
        var moving = player.velocity.Length() > 5f;

        if (moving)
        {
            RedRipples.QueueRipple(new RedRipples.Info(player.Center, 40f, 0.4f));
            return;
        }

        if (Main.timeForVisualEffects % 10 == 0)
        {
            RedRipples.QueueRipple(new RedRipples.Info(player.Center, 40f, 1f));
        }
    }
}
