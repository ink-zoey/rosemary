using Daybreak.Hooks;
using Daybreak.MonoMod;
using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.Graphics.Effects;

namespace Rosemary.Common;

file delegate bool ScreenFilterDefinition(
    SpriteBatch sb,
    GraphicsDevice device,
    RenderTarget2D screen,
    RenderTarget2D screenSwap,
    [Omittable] Color color
);

/// <summary>
///     Applies the decorated method as a screen filter during <see cref="FilterManager.EndCapture(RenderTarget2D, RenderTarget2D, RenderTarget2D)"/>.<br/><br/>
/// 
///     <b>Arguments:</b>
///     <list type="bullet">
///         <item>
///             <term>screen</term>
///             <description>
///                 <see cref="RenderTarget2D"/> containing the contents of the screen with all prior filters in the hierarchy applied.
///             </description>
///         </item>
///         <item>
///             <term>screenSwap</term>
///             <description>
///                 Empty* <see cref="RenderTarget2D"/>; final rendering should be drawn to this target if returning <see langword="true"/>.
///             </description>
///         </item>
///         <item>
///             <term>color</term>
///             <description>
///                 Equivalent to <see cref="Main.ColorOfTheSkies"/>, provided as vanilla screen filters pass this into draw calls,
///                 notably the screen itself should not be colored by this value when drawn to <b>screenSwap</b>,
///                 instead should be used for filter specific coloration.<br/>
///                 Will always be <see cref="Color.White"/> when <see cref="Lighting.UpdateEveryFrame"/> is <see langword="true"/>.
///             </description>
///         </item>
///     </list>
///
///     Return <see langword="true"/> to swap the targets, should be done if <b>screenSwap</b> has been drawn to before returning.
/// </summary>
/// <param name="priority">
///     Where this filter will be drawn during vanilla filter application, will be run before the first vanilla filter of the priority.
/// </param>
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
[HookMetadata(DelegateType = typeof(ScreenFilterDefinition))]
public sealed class ScreenFilterAttribute(EffectPriority priority) : SubscribesToAttribute
{
    public override void Apply(MethodInfo bindingMethod, object? instance)
    {
        var method = HookSubscriber.BuildWrapper<ScreenFilterDefinition>(bindingMethod, instance);

        ScreenFilterRenderer.FILTERS_BY_PRIORITY.TryAdd(priority, []);
        ScreenFilterRenderer.FILTERS_BY_PRIORITY[priority].Add(method);
    }
}

file static class ScreenFilterRenderer
{
    public static readonly Dictionary<EffectPriority, List<ScreenFilterDefinition>> FILTERS_BY_PRIORITY = [];

    [OnLoad]
    private static void Load()
    {
        IL_FilterManager.EndCapture_RenderTarget2D_RenderTarget2D_RenderTarget2D_Vector2_Vector2_Vector2 += EndCapture_ScreenFilters;
        On_FilterManager.CanCapture += (_, _) => true;
        IL_Main.DoDraw += _ => { };
    }

    private static void EndCapture_ScreenFilters(ILContext il)
    {
        var c = new ILCursor(il);

        var priorPriority = c.AddVariable<EffectPriority?>();

        var tIndex = -1;  // loc
        var t2Index = -1; // loc

        var value2Index = -1; // loc

        c.GotoNext(
            MoveType.After,
            i => i.MatchLdloca(out t2Index),
            i => i.MatchLdloca(out tIndex),
            i => i.MatchCall(typeof(Utils), nameof(Utils.Swap))
        );

        c.GotoNext(
            MoveType.After,
            i => i.MatchLdloc(out int _),
            i => i.MatchCallvirt<LinkedListNode<Filter>>($"get_{nameof(LinkedListNode<>.Value)}"),
            i => i.MatchStloc(out value2Index)
        );

        c.EmitLdloca(tIndex);
        c.EmitLdloca(t2Index);

        c.EmitLdloca(priorPriority);

        c.EmitLdloc(value2Index);
        c.EmitCallvirt(
            typeof(GameEffect).GetProperty(
                nameof(GameEffect.Priority),
                BindingFlags.Instance | BindingFlags.Public
            )!.GetMethod!
        );

        c.EmitDelegate(ApplyFiltersToPriority);

        // Run all remaining shaders if the VeryHigh priority was not reached
        c.GotoNext(
            MoveType.Before,
            i => i.MatchLdloc(out int _),
            i => i.MatchLdarg(out int _),
            i => i.MatchCallvirt<GraphicsDevice>(nameof(GraphicsDevice.SetRenderTarget))
        );

        c.EmitLdloca(tIndex);
        c.EmitLdloca(t2Index);

        c.EmitLdloca(priorPriority);
        c.EmitLdcI4((int)EffectPriority.VeryHigh);

        c.EmitDelegate(ApplyFiltersToPriority);
    }

    private static void ApplyFiltersToPriority(ref RenderTarget2D target, ref RenderTarget2D target2, ref EffectPriority? prior, EffectPriority nextPriority)
    {
        if (prior != null
         && prior == nextPriority)
        {
            return;
        }

        var color = Lighting.UpdateEveryFrame ? Color.White : Main.ColorOfTheSkies;

        prior ??= EffectPriority.VeryLow;

        for (var p = prior.Value; p <= nextPriority; p++)
        {
            if (!FILTERS_BY_PRIORITY.TryGetValue(p, out var filters))
            {
                continue;
            }

            foreach (var filter in filters)
            {
                if (filter(Main.spriteBatch, Main.graphics.GraphicsDevice, target, target2, color))
                {
                    Utils.Swap(ref target2, ref target);
                }
            }
        }
        prior = nextPriority;
    }
}
