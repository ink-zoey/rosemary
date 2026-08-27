using Daybreak.Hooks;
using Daybreak.MonoMod;
using MonoMod.Cil;
using Rosemary.Common;
using System.Diagnostics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Rosemary.Vanity.Content;

public static class DevMountSets
{
    private static bool[] ignoresHoverFatigue = [];

    private static bool[] allowsStepUp = [];

    private static Mod Mod => ModContent.GetInstance<ModImpl>();

    [ModSystemHooks.ResizeArrays]
    private static void ResizeArrays()
    {
        ignoresHoverFatigue = CreateSet(nameof(ignoresHoverFatigue), false);
        allowsStepUp = CreateSet(nameof(allowsStepUp), false);

        return;

        static T[] CreateSet<T>(string name, T defaultState)
        {
            return MountID.Sets.Factory.CreateNamedSet(Mod, name)
                          .RegisterCustomSet(defaultState);
        }
    }

    extension(MountID.Sets)
    {
        public static bool[] IgnoresHoverFatigue => ignoresHoverFatigue;

        /// <summary>
        /// If <see langword="true"/> for a given mount, the mount will allow the
        /// player to "step up" tiles similarly to the Magic Carpet. 
        /// </summary>
        public static bool[] AllowsStepUp => allowsStepUp;
    }

    [OnLoad]
    private static void Load()
    {
        // ignoresHoverFatigue
        On_Mount.DoesHoverIgnoresFatigue += DoesHoverIgnoresFatigue_IgnoresHoverFatigue;
        IL_Mount.Hover += _ => { };
        IL_Mount.TryBeginningFlight += _ => { };

        // allowsStepUp
        On_Player.DryCollision += DryCollision_AllowsStepUp;
        IL_Player.Update += Update_AllowsStepUp;
    }

    private static void Update_AllowsStepUp(ILContext il)
    {
        var c = new ILCursor(il);

        var playerIndex = ParameterIndex.Invalid;

        for (var j = 0; j < 2; j++)
        {
            c.GotoNext(
                MoveType.After,
                i => i.MatchCall<Collision>(nameof(Collision.StepUp))
            );

            var c2 = c.Clone();
            {
                ILLabel? jumpCheckTarget = null;

                c2.GotoPrev(
                    MoveType.After,
                    i => i.MatchLdarg(out playerIndex),
                    i => i.MatchLdfld<Player>(nameof(Player.carpetFrame)),
                    i => i.MatchLdcI4(-1),
                    i => i.MatchBneUn(out jumpCheckTarget)
                );

                Debug.Assert(jumpCheckTarget is not null);

                c2.EmitLdarg(playerIndex);
                c2.EmitDelegate(
                    static (Player player) => player.mount.Active && MountID.Sets.AllowsStepUp[player.mount.Type]
                );
                c2.EmitBrtrue(jumpCheckTarget);
            }
        }
    }

    private static void DryCollision_AllowsStepUp(On_Player.orig_DryCollision orig, Player self, bool fallThrough, bool ignorePlats)
    {
        if (!self.mount.Active || !MountID.Sets.AllowsStepUp[self.mount.Type])
        {
            orig(self, fallThrough, ignorePlats);
            return;
        }

        var prior = self.carpetFrame;
        self.carpetFrame = 0;
        orig(self, fallThrough, ignorePlats);
        self.carpetFrame = prior;
    }

    private static bool DoesHoverIgnoresFatigue_IgnoresHoverFatigue(On_Mount.orig_DoesHoverIgnoresFatigue orig, Mount self)
    {
        return orig(self) || MountID.Sets.IgnoresHoverFatigue[self.Type];
    }
}
