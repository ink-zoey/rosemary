using JetBrains.Annotations;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace Rosemary.Common;

file delegate void RenderLayerDefinition(
    [Omittable] SpriteBatch sb,
    [Omittable] GraphicsDevice device
);

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
[HookMetadata(DelegateType = typeof(RenderLayerDefinition))]
public sealed class RenderLayerAttribute(RenderLayers layer) : BaseHookAttribute
{
    public override void Apply(MethodInfo bindingMethod, object? instance)
    {
        var method = HookSubscriber.BuildWrapper<RenderLayerDefinition>(bindingMethod, instance);

        RenderLayerRenderer.LAYERS.TryAdd(layer, []);
        RenderLayerRenderer.LAYERS[layer].Add(method);
    }
}

// TODO: DrawCapture support assuming no rewrite in RenderReprise
[Autoload(Side = ModSide.Client)]
file static class RenderLayerRenderer
{
    public static readonly Dictionary<RenderLayers, List<RenderLayerDefinition>> LAYERS = [];

    [OnLoad]
    private static void Load()
    {
        On_OverlayManager.Draw += Draw_RenderLayers;
        IL_Main.DoDraw += _ => { };
        IL_Main.DoDraw_WallsAndBlacks += _ => { };
    }

    private static void Draw_RenderLayers(On_OverlayManager.orig_Draw orig, OverlayManager self, SpriteBatch sb, RenderLayers layer, bool beginSpriteBatch)
    {
        using (sb.Scope())
        {
            if (LAYERS.TryGetValue(layer, out var layers))
            {
                foreach (var renderLayer in layers)
                {
                    renderLayer(sb, Main.graphics.GraphicsDevice);
                }
            }
        }

        orig(self, sb, layer, beginSpriteBatch);
    }
}
