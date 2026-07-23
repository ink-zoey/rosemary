using Daybreak.Rendering;
using Daybreak.Rendering.Buffers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using Rosemary.Common;
using Rosemary.Core;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.ModLoader;

namespace Rosemary.Vanity.Content;

public sealed class SiffrinHover : ModItem
{
    public override string Texture => Assets.Vanity.Hat.KEY;

    public override string LocalizationCategory => "Content";

    public override void SetDefaults()
    {
        Item.width = 20;
        Item.height = 20;

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
    private const float max_scale = 3.5f;

    public record struct AfterImageInfo(Vector2 Position, float Rotation, Vector2 Origin, float Scale, int Direction, int GravityDirection) : IUpdatingParticle
    {
        public static AfterImageInfo FromPlayer(Player player)
        {
            return new AfterImageInfo
            {
                Position = player.Center,
                Rotation = player.fullRotation,
                Origin = player.fullRotationOrigin,
                Scale = 1f,
                Direction = player.direction,
                GravityDirection = (int)player.gravDir,
            };
        }

        bool IUpdatingParticle.Update()
        {
            Scale += 0.03f * (Scale * 2f);

            return Scale < max_scale;
        }
    }

    public UpdatingParticleHandler<AfterImageInfo> AfterImages = new(15);

    private static bool drawingAfterImage;

    public override void Load()
    {
        IL_PlayerDrawSet.BoringSetup_2 += BoringSetup_2_ShowSkin;
    }

    private static void BoringSetup_2_ShowSkin(ILContext il)
    {
        var c = new ILCursor(il);

        c.GotoNext(
            MoveType.After,
            i => i.MatchLdsfld<Main>(nameof(Main.mouseTextColor)),
            i => i.MatchConvR4(),
            i => i.MatchLdcR4(200f),
            i => i.MatchDiv()
        );

        for (var j = 0; j < 3; j++)
        {
            c.GotoNext(
                MoveType.After,
                i => i.MatchLdfld<PlayerDrawSet>(nameof(PlayerDrawSet.shadow))
            );

            c.EmitDelegate(
                static (float shadow) => drawingAfterImage ? 0f : shadow
            );
        }
    }

    public override void PostUpdate()
    {
        AfterImages.Update();
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
            sb.Begin(ss with { TransformMatrix = Matrix.Identity, SamplerState = SamplerState.LinearClamp });
            {
                var prior = Main.GameViewMatrix._transformationMatrix;
                Main.GameViewMatrix._transformationMatrix = Matrix.Identity;
                drawingAfterImage = true;
                {
                    Main.PlayerRenderer.DrawPlayer(camera, Player, Player.position, Player.fullRotation, Player.fullRotationOrigin, float.Epsilon);
                }
                drawingAfterImage = false;
                Main.GameViewMatrix._transformationMatrix = prior;
            }
            sb.End();
        }

        effect.Parameters.StepSize = 2f;
        effect.Parameters.MaxScale = max_scale;

        var color = Color.Red with { A = 70 };
        effect.Parameters.BaseColor = color.ToVector4();

        effect.Apply();

        sb.Begin(ss with { SortMode = SpriteSortMode.Deferred, SamplerState = SamplerState.LinearClamp, CustomEffect = effect.Shader });
        {
            foreach (var index in AfterImages)
            {
                var image = AfterImages[index];

                var position = image.Position - Main.screenPosition;

                var origin = Player.Center - Main.screenPosition + image.Origin;

                var interpolator = image.Scale / max_scale;

                // Pass the scale in as a color to have everything batch nicely.
                var inputColor = new Color(new Vector4(interpolator));

                var effects = image.Direction == Player.direction ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                effects |= image.GravityDirection == (int)Player.gravDir ? SpriteEffects.None : SpriteEffects.FlipVertically;

                sb.Draw(lease.Target, position, null, inputColor, image.Rotation, origin, image.Scale, effects, 0f);
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

        const float max_velocity = 18f;

        var amplitude = 2.5f * MathF.Saturate((max_velocity - drawInfo.drawPlayer.velocity.Length()) / max_velocity);

        var position = Vector2.Zero;

        if (drawInfo.drawPlayer.velocity.Y != 0f)
        {
            position.Y += MathF.Sin(Main.GlobalTimeWrappedHourly * 3f) * amplitude;
        }

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
            var offset = new Vector2(2, 0).RotatedBy((i / 4f) * MathF.Tau);

            sb.Draw(lease.Target, position + offset, Color.Black);
        }

        var player = drawInfo.drawPlayer;

        var top = new Vector2(player.Center.X, player.Top.Y) - Main.screenPosition;
        var bottom = new Vector2(player.Center.X, player.Bottom.Y) - Main.screenPosition;

        effect.Parameters.PlayerTop = top.Y;
        effect.Parameters.PlayerBottom = bottom.Y;

        effect.Apply();

        sb.Draw(lease.Target, position, Color.White);

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
        MountData.acceleration = 0.91f;
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

    public override void UpdateEffects(Player player)
    {
        var effectsPlayer = player.GetModPlayer<OutlineAfterImagesPlayer>();

        if (Main.timeForVisualEffects % 6 == 0)
        {
            effectsPlayer.AfterImages += OutlineAfterImagesPlayer.AfterImageInfo.FromPlayer(player);
        }
    }
}
