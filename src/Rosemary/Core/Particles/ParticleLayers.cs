using Daybreak.Hooks;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using System;
using System.Reflection;
using Terraria;
using Terraria.Graphics.Renderers;
using Terraria.ModLoader;

namespace Rosemary.Core;

[Autoload(Side = ModSide.Client)]
public static class ParticleLayers
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    [HookMetadata(TypeContainingEvent = typeof(ParticleLayers), EventName = nameof(OverPlayers), DelegateName = nameof(OverPlayersDefinition))]
    public sealed class OverPlayersAttribute : SubscribesToAttribute;

    public delegate void OverPlayersDefinition(
        SpriteBatch sb
    );

    public static event OverPlayersDefinition? OverPlayers;

    [OnLoad]
    private static void Load()
    {
        IL_Main.DoDraw += DoDraw_DrawParticles;
    }

    private static void DoDraw_DrawParticles(ILContext il)
    {
        var c = new ILCursor(il);

        c.GotoNext(
            MoveType.After,
            i => i.MatchLdsfld<Main>(nameof(Main.ParticleSystem_World_OverPlayers)),
            i => i.MatchLdsfld<Main>(nameof(Main.spriteBatch)),
            i => i.MatchCallvirt<ParticleRenderer>(nameof(ParticleRenderer.Draw))
        );

        c.GotoNext(
            MoveType.After,
            i => i.MatchLdsfld<Main>(nameof(Main.spriteBatch)),
            i => i.MatchCallvirt<SpriteBatch>(nameof(SpriteBatch.End))
        );

        c.EmitLdsfld(
            typeof(Main).GetField(
                nameof(Main.spriteBatch),
                BindingFlags.Static | BindingFlags.Public
            )!
        );
        c.EmitDelegate(DrawParticles);
    }

    private static void DrawParticles(SpriteBatch sb)
    {
        OverPlayers?.Invoke(sb);
    }
}
