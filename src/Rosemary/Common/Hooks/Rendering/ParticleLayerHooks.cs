using Daybreak.Hooks;
using JetBrains.Annotations;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Reflection;
using Daybreak.Rendering;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics.Renderers;
using Terraria.ModLoader;

namespace Rosemary.Common;

public enum ParticleLayers
{
    OverCursor,
    OverInventory,
    BehindPlayers,
    OverPlayers,
}

file delegate void ParticleLayerDefinition(
    [Omittable] SpriteBatch sb,
    [Omittable] GraphicsDevice device
);

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
[HookMetadata(DelegateType = typeof(ParticleLayerDefinition))]
public sealed class ParticleLayer(ParticleLayers layer) : BaseHookAttribute
{
    public override void Apply(MethodInfo bindingMethod, object? instance)
    {
        var method = HookSubscriber.BuildWrapper<ParticleLayerDefinition>(bindingMethod, instance);

        ParticleLayerRenderer.LAYERS.TryAdd(layer, []);
        ParticleLayerRenderer.LAYERS[layer].Add(method);
    }
}

// TODO: DrawCapture support assuming no rewrite in RenderReprise
[Autoload(Side = ModSide.Client)]
file static class ParticleLayerRenderer
{
    public static readonly Dictionary<ParticleLayers, List<ParticleLayerDefinition>> LAYERS = [];

    [OnLoad]
    private static void Load()
    {
        IL_Main.DoDraw += DoDraw_DrawParticleLayers;
        On_Main.DrawInventory += DrawInventory_DrawParticleLayers;
        On_Main.DrawCursor += DrawCursor_DrawParticleLayers;
    }

    private static void DrawCursor_DrawParticleLayers(On_Main.orig_DrawCursor orig, Vector2 bonus, bool smart)
    {
        orig(bonus, smart);

        using var _ = Main.spriteBatch.Scope();

        DrawParticleLayer(ParticleLayers.OverCursor);
    }

    private static void DrawInventory_DrawParticleLayers(On_Main.orig_DrawInventory orig, Main self)
    {
        orig(self);

        using var _ = Main.spriteBatch.Scope();

        DrawParticleLayer(ParticleLayers.OverInventory);
    }

    private static void DoDraw_DrawParticleLayers(ILContext il)
    {
        var c = new ILCursor(il);

        c.GotoNext(
            MoveType.After,
            i => i.MatchLdsfld<Main>(nameof(Main.ParticleSystem_World_OverPlayers)),
            i => i.MatchLdsfld<Main>(nameof(Main.spriteBatch)),
            i => i.MatchCallvirt<ParticleRenderer>(nameof(ParticleRenderer.Draw))
        );

        var c2 = c.Clone();
        {
            c2.GotoPrev(
                MoveType.After,
                i => i.MatchLdsfld<Main>(nameof(Main.spriteBatch)),
                i => i.MatchCallvirt<SpriteBatch>(nameof(SpriteBatch.End))
            );

            c.EmitLdcI4((int)ParticleLayers.BehindPlayers);
            c2.EmitDelegate(DrawParticleLayer);
        }

        c.GotoNext(
            MoveType.After,
            i => i.MatchLdsfld<Main>(nameof(Main.spriteBatch)),
            i => i.MatchCallvirt<SpriteBatch>(nameof(SpriteBatch.End))
        );

        c.EmitLdcI4((int)ParticleLayers.OverPlayers);
        c.EmitDelegate(DrawParticleLayer);
    }

    private static void DrawParticleLayer(ParticleLayers layer)
    {
        if (!LAYERS.TryGetValue(layer, out var layers))
        {
            return;
        }

        foreach (var renderLayer in layers)
        {
            renderLayer(Main.spriteBatch, Main.graphics.GraphicsDevice);
        }
    }
}
