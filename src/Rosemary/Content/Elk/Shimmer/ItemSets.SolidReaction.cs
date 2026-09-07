using System;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using Rosemary.Common;
using Terraria;
using Terraria.ID;

namespace Rosemary.Content.Elk;

public static partial class ElkShimmerItemSets
{
    [OnLoad]
    private static void Load_SolidShimmerReaction()
    {
        On_WorldItem.ApplyMovement += ApplyMovement_ShimmerWalk;

        IL_WorldItem.UpdateItem += _ => { };
        IL_WorldItem.MoveInWorld += _ => { };

        On_WorldItem.Shimmering += Shimmering_SolidReaction;

        IL_WorldItem.UpdateItem += UpdateItem_ShimmerSlowdown;
    }

    private static void UpdateItem_ShimmerSlowdown(ILContext il)
    {
        var c = new ILCursor(il);

        var itemIndex = ParameterIndex.Invalid;

        c.GotoNext(
            MoveType.After,
            i => i.MatchLdfld<Entity>(nameof(Entity.shimmerWet))
        );

        c.GotoNext(
            MoveType.After,
            i => i.MatchLdcR4(0.375f)
        );

        c.FindPrev(
            out _,
            i => i.MatchLdarg(out itemIndex)
        );

        c.EmitLdarg(itemIndex);

        c.EmitStaticDelegateUnsafe(
            static (float orig, WorldItem item) =>
                ItemID.Sets.SolidShimmerReaction[item.type] ? 1f : orig
        );
    }

    private static void Shimmering_SolidReaction(On_WorldItem.orig_Shimmering orig, WorldItem self)
    {
        if (!ItemID.Sets.SolidShimmerReaction[self.type])
        {
            orig(self);

            return;
        }

        if (self.shimmerWet)
        {
            var curPosition = FindShimmerSurface(self, 32);

            var dist = curPosition == self.Bottom
                ? 1f
                : MathF.Saturate((MathF.Abs(self.Center.Y - curPosition.Y) / 80f) + 0.1f);

            self.velocity.Y = -12f * dist;
        }

        orig(self);
    }

    private static void ApplyMovement_ShimmerWalk(On_WorldItem.orig_ApplyMovement orig, WorldItem self, ref Vector2 wetVelocity)
    {
        var velocity = self.wet ? wetVelocity : self.velocity;

        if (ItemID.Sets.SolidShimmerReaction[self.type] && !self.shimmerWet)
        {
            var prior = velocity;

            velocity = ShimmerCollision(self, velocity);

            if (velocity != prior)
            {
                self.velocity.X /= 0.95f;
                self.velocity.X *= 0.99f;
            }
        }

        self.position += velocity;

        return;

        static Vector2 ShimmerCollision(Entity item, Vector2 velocity)
        {
            var result = velocity;
            var nextPosition = item.position + velocity;
            var position = item.position;

            var startX = (int)(item.position.X / 16f) - 1;
            var endX = (int)((item.position.X + item.width) / 16f) + 2;
            var startY = (int)(item.position.Y / 16f) - 1;
            var endY = (int)((item.position.Y + item.height) / 16f) + 2;

            startX = Utils.Clamp(startX, 0, Main.maxTilesX - 1);
            endX = Utils.Clamp(endX, 0, Main.maxTilesX - 1);
            startY = Utils.Clamp(startY, 0, Main.maxTilesY - 40);
            endY = Utils.Clamp(endY, 0, Main.maxTilesY - 40);

            var current = Vector2.Zero;

            for (var i = startX; i < endX; i++)
            for (var j = startY; j < endY; j++)
            {
                var tile = Main.tile[i, j];

                if (!tile.HasShimmer || Main.tile[i, j - 1].HasShimmer)
                {
                    continue;
                }

                var level = 16f - (((Main.tile[i, j].LiquidAmount / (float)byte.MaxValue) * 16f) - 2);

                current.X = i * 16f;
                current.Y = j * 16f + level;

                if (nextPosition.X + item.width > current.X
                 && nextPosition.X < current.X + 16f
                 && nextPosition.Y + item.height > current.Y
                 && nextPosition.Y < current.Y + level
                 && position.Y + item.height <= current.Y)
                {
                    result.Y = current.Y - (position.Y + item.height);
                }
            }

            return result;
        }
    }

    private static void MoveInWorld_ShimmerWalk(ILContext il)
    {
        return;

        
    }
}
