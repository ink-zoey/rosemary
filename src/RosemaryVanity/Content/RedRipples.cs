using Daybreak.Hooks;
using Daybreak.Models;
using Daybreak.Rendering;
using Daybreak.Rendering.Buffers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rosemary.Common;
using Rosemary.Core;
using Rosemary.Vanity.Core;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;
using static tModPorter.ProgressUpdate;

namespace Rosemary.Vanity.Content;

[Autoload(Side = ModSide.Client)]
public static class RedRipples
{
    private const float target_size = 0.5f;

    public record struct Info(Vector2 Position, float Size, float Intensity);

    private sealed class Data : IStatic<Data>
    {
        public required WrapperShaderData<Assets.Vanity.RippleCompute.Parameters> RippleComputeShader { get; init; }

        public required WrapperShaderData<Assets.Vanity.RippleProcessor.Parameters> RippleRedShader { get; init; }

        public required RenderTargetLease RippleTarget { get; init; }

        public required RenderTargetLease RippleTargetSwap { get; init; }

        public required RenderTargetLease RippleMaskTarget { get; init; }

        public static Data LoadData(Mod mod)
        {
            return Main.RunOnMainThread(
                () => new Data
                {
                    RippleComputeShader = Assets.Vanity.RippleCompute.CreateRippleComputeShader(),
                    RippleRedShader = Assets.Vanity.RippleProcessor.CreateRippleRedShader(),
                    RippleTarget = ScreenspaceTargetProvider.Shared.Create(Main.instance.GraphicsDevice, GetTargetSize, RenderTargetDescriptor.Default with { Format = SurfaceFormat.HalfVector4 }),
                    RippleTargetSwap = ScreenspaceTargetProvider.Shared.Create(Main.instance.GraphicsDevice, GetTargetSize, RenderTargetDescriptor.Default with { Format = SurfaceFormat.HalfVector4 }),
                    RippleMaskTarget = ScreenspaceTargetProvider.Shared.Create(Main.instance.GraphicsDevice, RenderTargetDescriptor.Default with { Format = SurfaceFormat.Alpha8 }),
                }
            ).GetAwaiter().GetResult();

            static (int w, int h) GetTargetSize(int width, int height, int targetWidth, int targetHeight)
            {
                return ((int)(width * target_size), (int)(height * target_size));
            }
        }

        public static void UnloadData(Data data)
        {
            Main.RunOnMainThread(
                () =>
                {
                    data.RippleTarget.Dispose();
                    data.RippleTargetSwap.Dispose();
                    data.RippleMaskTarget.Dispose();
                }
            );
        }
    }

    public sealed class RippleRenderer : IScreenFilterStep
    {
        public EffectPriority Priority => EffectPriority.Low;

        public bool Apply(in ScreenFilterRendererContext ctx)
        {
            return ApplyShader(ctx.ScreenTarget, ctx.ScreenTargetSwap, ctx.Color);
        }
    }

    public static RenderTargetLease RippleTarget => Data.Instance.RippleTarget;

    public static RenderTargetLease RippleTargetSwap => Data.Instance.RippleTargetSwap;

    public static RenderTargetLease RippleMaskTarget => Data.Instance.RippleMaskTarget;

    private static readonly Queue<Info> ripples = [];

    public static void QueueRipple(Info info)
    {
        ripples.Enqueue(info);
    }

    [OnLoad]
    private static void Load()
    {
        On_Main.DoDraw += DoDraw_ComputeRipples;
    }

    private static Vector2 lastScreenPosition;

    private static void DoDraw_ComputeRipples(On_Main.orig_DoDraw orig, Main self, GameTime gameTime)
    {
        Compute();

        orig(self, gameTime);

        return;

        static void Compute()
        {
            if (!FocusHelper.UpdateVisualEffects)
            {
                return;
            }

            var sb = Main.spriteBatch;

            var target = RippleTarget.Target;
            var targetSwap = RippleTargetSwap.Target;

            var compute = Data.Instance.RippleComputeShader;

            using var _ = sb.Scope();

            using (targetSwap.Scope(clearColor: new Color(0.5f, 0.5f, 0f, 1f)))
            {
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                {
                    sb.Draw(target, Vector2.Zero, Color.White);

                    var texture = Assets.Circle.Asset.Value;

                    while (ripples.TryDequeue(out var info))
                    {
                        var position = info.Position - Main.screenPosition;

                        position *= target_size;

                        sb.Draw(
                            new DrawParameters(texture)
                            {
                                Position = position,
                                Color = new Color(0f, 0f, info.Intensity, 0f),
                                Size = new Vector2(info.Size) * target_size,
                                Origin = Origin.Center,
                            }
                        );
                    }
                }
                sb.End();
            }

            using (target.Scope(clearColor: new Color(0.5f, 0.5f, 0f, 1f)))
            {
                sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                {
                    compute.Parameters.Texture = new HlslSampler
                    {
                        Sampler = SamplerState.LinearClamp,
                        Texture = targetSwap,
                    };

                    compute.Parameters.Decay = 0.9f;
                    compute.Parameters.Strength = 5f;
                    compute.Parameters.StepSize = 4f * target_size;

                    compute.Apply();

                    var position = Main.screenPosition - lastScreenPosition;

                    position *= target_size;

                    sb.Draw(targetSwap, -position, Color.White);
                }
                sb.End();
            }

            lastScreenPosition = Main.screenPosition;
        }
    }

    private static bool ApplyShader(RenderTarget2D screen, RenderTarget2D screenSwap, Color color)
    {
        var sb = Main.spriteBatch;

        var device = Main.graphics.GraphicsDevice;

        var target = RippleTarget.Target;

        var shader = Data.Instance.RippleRedShader;

        shader.Parameters.Texture = new HlslSampler
        {
            Sampler = SamplerState.LinearClamp,
            Texture = target,
        };

        shader.Parameters.Mask = new HlslSampler
        {
            Sampler = SamplerState.PointClamp,
            Texture = RippleMaskTarget.Target,
        };

        shader.Parameters.StepSize = 4f * target_size;

        shader.Apply();

        sb.Begin(SpriteSortMode.Immediate, BlendState.Multiplicative, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, shader.Shader, Main.Transform);
        {
            sb.Draw(target, device.Viewport.Bounds, Color.DarkGreen);
        }
        sb.End();

        sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, shader.Shader, Main.Transform);
        {
            sb.Draw(target, device.Viewport.Bounds, Color.Red);
        }
        sb.End();

        device.SetRenderTarget(RippleMaskTarget.Target);
        device.Clear(Color.Transparent);

        device.SetRenderTargets(screenSwap);

        return false;
    }
}
