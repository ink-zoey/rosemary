using Daybreak.Hooks;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics.Light;

namespace Rosemary.Common;

// TODO: Port over to render reprise when tomat gets that usable #use
internal static class FullBrightLightMap
{
    private static readonly LightMap full_bright_light_map = new();

    private static readonly LegacyLighting.LightingState default_lighting_state = new()
    {
        R = 1f,
        G = 1f,
        B = 1f,
    };

    private static LegacyLighting.LightingState[] innerStateArray = [];
    private static LegacyLighting.LightingState[][] outerStateArray = [];

    static FullBrightLightMap()
    {
        EnsureLightMapSize(LightMap.DEFAULT_WIDTH, LightMap.DEFAULT_HEIGHT, firstInit: true);
    }

    [OnLoad]
    private static void ApplyHooks()
    {
        On_LightMap.Clear += (orig, self) =>
        {
            if (self == full_bright_light_map)
            {
                return;
            }

            orig(self);
        };
    }

    public static LightMap GetLightMap(int width, int height)
    {
        EnsureLightMapSize(width, height);
        return full_bright_light_map;
    }

    private static void EnsureLightMapSize(int width, int height, bool firstInit = false)
    {
        if (!firstInit && full_bright_light_map.Width >= width && full_bright_light_map.Height >= height)
        {
            return;
        }

        full_bright_light_map.SetSize(width, height);
        {
            full_bright_light_map._colors.AsSpan().Fill(Vector3.One);

            // Probably not necessary?  Only if somehow mutated :(
            // masks = new LightMaskMode[size];
        }
    }

    public static LegacyLighting.LightingState[][] GetLegacyStates(LegacyLighting.LightingState[][] states)
    {
        default_lighting_state.R = 1f;
        default_lighting_state.G = 1f;
        default_lighting_state.B = 1f;

        var length1 = states.Length;
        var length2 = states[0].Length;

        EnsureLegacyStatesSize(length1, length2);
        return outerStateArray;
    }

    private static void EnsureLegacyStatesSize(int length1, int length2)
    {
        var dirty = false;
        if (innerStateArray.Length < length2)
        {
            innerStateArray = new LegacyLighting.LightingState[length2];
            innerStateArray.AsSpan().Fill(default_lighting_state);
            dirty = true;
        }

        if (dirty || outerStateArray.Length < length1)
        {
            outerStateArray = new LegacyLighting.LightingState[length1][];
            outerStateArray.AsSpan().Fill(innerStateArray);
        }
    }
}

public sealed class FullBrightScope : IDisposable
{
    private readonly LightMap prior;

    public FullBrightScope()
    {
        var engine = Lighting.NewEngine;

        prior = engine._activeLightMap;
        engine._activeLightMap = FullBrightLightMap.GetLightMap(engine._activeLightMap.Width, engine._activeLightMap.Height);
    }

    public void Dispose()
    {
        var engine = Lighting.NewEngine;

        engine._activeLightMap = prior;
    }
}
