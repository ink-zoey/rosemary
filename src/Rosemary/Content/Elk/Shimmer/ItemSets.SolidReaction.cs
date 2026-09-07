using Microsoft.Xna.Framework;
using MonoMod.Cil;
using Terraria;
using Terraria.ID;

namespace Rosemary.Content.Elk;

public static partial class ElkShimmerItemSets
{
    [OnLoad]
    private static void Load_SolidShimmerReaction()
    {
        On_WorldItem.ApplyMovement += ApplyMovement_ShimmerWalk;

        IL_WorldItem.MoveInWorld += _ => { };
        IL_WorldItem.UpdateItem += _ => { };
    }

    private static void ApplyMovement_ShimmerWalk(On_WorldItem.orig_ApplyMovement orig, WorldItem self, ref Vector2 wetVelocity)
    {
        var velocity = self.wet ? wetVelocity : self.velocity;

        if (ItemID.Sets.SolidShimmerReaction[self.type] && !self.wet)
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
