using System;
using Daybreak.Common.Features.Hooks;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using Terraria;
using Terraria.DataStructures;

namespace Rosemary.Common;

file static class WorldItemBehavior
{
    internal static readonly float[] rotations = new float[Main.maxItems];

    internal static readonly bool[] hidden = new bool[Main.maxItems];

    [OnLoad]
    private static void Load()
    {
        IL_Main.DrawItem += DrawItem_Rotation;
        On_WorldItem.UpdateItem += UpdateItem_UpdateRotation;

        IL_Main.DrawItems += DrawItems_HideHidden;
        IL_Main.DoDraw += _ => { };
        IL_Main.DrawCapture += _ => { };

        On_Item.NewItem_Inner += NewItem_Inner_RefreshHidden;
    }

    private static int NewItem_Inner_RefreshHidden(
        On_Item.orig_NewItem_Inner orig,
        IEntitySource source,
        int x,
        int y,
        int width,
        int height,
        Item itemToClone,
        int type,
        int stack,
        bool noBroadcast,
        int prefix,
        bool noGrabDelay
    )
    {
        var index = orig(source, x,y,width,height, itemToClone,type,stack, noBroadcast, prefix, noGrabDelay);

        if (hidden.IndexInRange(index))
        {
            hidden[index] = false;
        }

        return index;
    }

    private static void DrawItems_HideHidden(ILContext il)
    {
        var c = new ILCursor(il);

        var itemIndexIndex = -1; // loc

        var loopTarget = c.DefineLabel();

        c.GotoNext(
            MoveType.Before,
            i => i.MatchLdarg(out _),
            i => i.MatchLdsfld<Main>(nameof(Main.item)),
            i => i.MatchLdloc(out itemIndexIndex)
        );

        c.MoveAfterLabels();

        c.EmitLdloc(itemIndexIndex);
        c.EmitDelegate(
            static (int index) => hidden[index]
        );
        c.EmitBrtrue(loopTarget);

        c.GotoNext(
            MoveType.Before,
            i => i.MatchLdloc(itemIndexIndex),
            i => i.MatchLdcI4(1),
            i => i.MatchAdd()
        );

        c.MarkLabel(loopTarget);
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
}

public static class WorldItemExtensions
{
    extension(WorldItem item)
    {
        public float Rotation
        {
            get => WorldItemBehavior.rotations[item.whoAmI];
            set => WorldItemBehavior.rotations[item.whoAmI] = value;
        }

        public bool Hidden
        {
            get => WorldItemBehavior.hidden[item.whoAmI];
            set => WorldItemBehavior.hidden[item.whoAmI] = value;
        }
    }
}
