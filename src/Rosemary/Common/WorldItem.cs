using Daybreak.Hooks;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using System;
using GoldMeridian.CodeAnalysis;
using Terraria;
using Terraria.DataStructures;

// ReSharper disable InconsistentNaming
namespace Rosemary.Common;

[ExtensionDataFor<WorldItem>("CommonData")]
internal sealed class WorldItemCommonData
{
    public required float Rotation { get; set; }

    public required bool Hidden { get; set; }
}

file static class WorldItemDataBehavior
{
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

        if (index == -1)
        {
            return -1;
        }

        var item = Main.item[index];

        item.Hidden = false;

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
            static (int index) => Main.item[index].Hidden
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
        private WorldItemCommonData GetOrInitializeData()
        {
            item.CommonData ??= new WorldItemCommonData
            {
                Hidden = false,
                Rotation = 0f,
            };

            return item.CommonData!;
        }

        /// <summary>
        ///     Extra rotation above the x velocity based rotation, interpolates back to 0 over time.
        /// </summary>
        public float Rotation
        {
            get => item.GetOrInitializeData().Rotation;
            set => item.GetOrInitializeData().Rotation = value;
        }

        /// <summary>
        ///     Hides the item from standard rendering in <see cref="Main.DrawItems"/>, should be manually drawn if applicable.
        /// </summary>
        public bool Hidden
        {
            get => item.GetOrInitializeData().Hidden;
            set => item.GetOrInitializeData().Hidden = value;
        }
    }
}
