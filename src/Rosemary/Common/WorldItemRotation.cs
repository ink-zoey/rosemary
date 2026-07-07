using System;
using Daybreak.Common.Features.Hooks;
using Daybreak.Common.Mathematics;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using Terraria;

namespace Rosemary.Common;

public static class WorldItemRotation
{
    private static readonly float[] rotations = new float[Main.maxItems];

    [OnLoad]
    private static void Load()
    {
        IL_Main.DrawItem += DrawItem_Rotation;
        On_WorldItem.UpdateItem += UpdateItem_UpdateRotation;
    }

    private static void UpdateItem_UpdateRotation(On_WorldItem.orig_UpdateItem orig, WorldItem self, int i)
    {
        orig(self, i);

        var interpolator = MathF.Min(self.velocity.Length(), 12f);
        interpolator /= 12f;

        interpolator = MathHelper.Lerp(0.02f, 0.2f, interpolator);

        self.Rotation = self.Rotation.AngleLerp(0f, interpolator);
    }

    private static void DrawItem_Rotation(ILContext il)
    {
        var c = new ILCursor(il);

        var itemIndex = -1;     // arg
        var rotationIndex = -1; // loc

        c.GotoNext(
            MoveType.Before,
            i => i.MatchLdarg(out itemIndex),
            i => i.MatchLdfld<WorldItem>(nameof(WorldItem.shimmered))
        );

        c.GotoPrev(
            MoveType.After,
            i => i.MatchStloc(out rotationIndex)
        );

        c.MoveAfterLabels();

        c.EmitLdarg(itemIndex);
        c.EmitLdloca(rotationIndex);

        c.EmitDelegate(
            static (WorldItem item, ref float rotation) =>
            {
                rotation += item.Rotation;
            }
        );
    }

    extension(WorldItem item)
    {
        public float Rotation
        {
            get => rotations[item.whoAmI];
            set => rotations[item.whoAmI] = value;
        }
    }
}
