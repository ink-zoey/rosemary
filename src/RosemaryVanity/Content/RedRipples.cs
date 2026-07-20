using System.Collections.Generic;
using Daybreak.Models;
using Daybreak.Rendering.Buffers;
using Rosemary.Vanity.Core;
using Daybreak.Hooks;
using Daybreak.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rosemary.Common;
using Rosemary.Core;
using Terraria;
using Terraria.ModLoader;

namespace Rosemary.Vanity.Content;

[Autoload(Side = ModSide.Client)]
public static class RedRipples
{
    private const float target_size = 0.5f;

    public record struct Info(Vector2 Position, float Size);

    private sealed class Data : IStatic<Data>
    {
        public required WrapperShaderData<Assets.Vanity.RippleCompute.Parameters> RippleComputeShader { get; init; }

        public required WrapperShaderData<Assets.Vanity.RippleProcessor.Parameters> RippleNegativeShader { get; init; }

        public required RenderTargetLease RippleTarget { get; init; }

        public required RenderTargetLease RippleTargetSwap { get; init; }

        public static Data LoadData(Mod mod)
        {
            return Main.RunOnMainThread(
                () => new Data
                {
                    RippleComputeShader = Assets.Vanity.RippleCompute.CreateRippleComputeShader(),
                    RippleNegativeShader = Assets.Vanity.RippleProcessor.CreateRippleNegativeShader(),
                    RippleTarget = ScreenspaceTargetProvider.Shared.Create(Main.instance.GraphicsDevice, GetTargetSize, RenderTargetDescriptor.Default with { Format = SurfaceFormat.HalfVector4 }),
                    RippleTargetSwap = ScreenspaceTargetProvider.Shared.Create(Main.instance.GraphicsDevice, GetTargetSize, RenderTargetDescriptor.Default with { Format = SurfaceFormat.HalfVector4 }),
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
                }
            );
        }
    }

    private static readonly Queue<Info> ripples = [];

    public static void QueueRipple(Info info)
    {
        ripples.Enqueue(info);
    }

    [OnLoad]
    private static void Load()
    {
        On_Main.DoDraw += DoDraw_RenderRipples;
    }

    private static Vector2 lastScreenPosition;

    private static void DoDraw_RenderRipples(On_Main.orig_DoDraw orig, Main self, GameTime gameTime)
    {
        Render();

        orig(self, gameTime);

        return;

        static void Render()
        {
            if (!FocusHelper.UpdateVisualEffects)
            {
                return;
            }

            var sb = Main.spriteBatch;

            var target = Data.Instance.RippleTarget.Target;
            var targetSwap = Data.Instance.RippleTargetSwap.Target;

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
                        sb.Draw(
                            new DrawParameters(texture)
                            {
                                Position = info.Position - Main.screenPosition,
                                Color = new Color(0f, 0f, 1f, 0f),
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

                    compute.Parameters.Decay = 0.94f;
                    compute.Parameters.Strength = 2f;
                    compute.Parameters.StepSize = 4f * target_size;

                    compute.Apply();

                    var position = Main.screenPosition - lastScreenPosition;

                    sb.Draw(targetSwap, -position, Color.White);
                }
                sb.End();
            }

            lastScreenPosition = Main.screenPosition;
        }
    }

    [ParticleLayers.UnderPlayers]
    private static void DrawRipples(SpriteBatch sb)
    {
        var target = Data.Instance.RippleTarget.Target;

        var negative = Data.Instance.RippleNegativeShader;

        sb.Begin(SpriteSortMode.Immediate, BlendState.Multiplicative, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
        {
            negative.Apply();

            sb.Draw(target, Vector2.Zero, Color.White);
        }
        sb.End();

        sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
        {
        }
        sb.End();
    }
}
