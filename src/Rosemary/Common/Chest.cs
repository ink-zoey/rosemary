using Daybreak.Hooks;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.UI;
using Terraria.Utilities;

// ReSharper disable InconsistentNaming
// ReSharper disable UseSymbolAlias
namespace Rosemary.Common;

file static class ChestBehavior
{
    internal static readonly int[] silentOpenAnimationTime = new int[Main.maxChests];

    internal static bool NextChestOpenSilent;

    [OnLoad]
    private static void Load()
    {
        IL_ParticleOrchestrator.Spawn_ItemTransfer += Spawn_ItemTransfer_Ext;

        IL_Wiring.TryAddingToEmptySlot += TryAddingToEmptySlot_Ext;

        IL_Chest.UpdateChestFrames += UpdateChestFrames_SilentOpen;
    }

    private static void UpdateChestFrames_SilentOpen(ILContext il)
    {
        var c = new ILCursor(il);

        var chestIndex = -1;

        c.GotoNext(
            MoveType.Before,
            i => i.MatchLdloc(out chestIndex),
            i => i.MatchLdfld<Chest>(nameof(Chest.eatingAnimationTime))
        );

        c.MoveAfterLabels();

        c.EmitLdloc(chestIndex);

        c.EmitDelegate(
            static (Chest chest) =>
            {
                if (chest.SilentOpenAnimationTime > 0)
                {
                    chest.SilentOpenAnimationTime--;
                }
                if (chest.frameCounter < chest.SilentOpenAnimationTime)
                {
                    chest.frameCounter = chest.SilentOpenAnimationTime;
                }
            }
        );
    }

    private static void TryAddingToEmptySlot_Ext(ILContext il)
    {
        var c = new ILCursor(il);

        var jumpPlaySoundTarget = c.DefineLabel();

        c.GotoNext(
            MoveType.Before,
            i => i.MatchLdcI4(7)
        );

        c.MoveAfterLabels();

        c.EmitDelegate(
            static () =>
            {
                var wasSilent = NextChestOpenSilent;

                NextChestOpenSilent = false;

                return wasSilent;
            }
        );
        c.EmitBrtrue(jumpPlaySoundTarget);

        c.GotoNext(
            MoveType.After,
            i => i.MatchPop()
        );

        c.MarkLabel(jumpPlaySoundTarget);
    }

    private static void Spawn_ItemTransfer_Ext(ILContext il)
    {
        var c = new ILCursor(il);

        var bitsByteIndex = -1;

        c.GotoNext(
            MoveType.After,
            i => i.MatchLdloca(out bitsByteIndex),
            i => i.MatchLdcI4(3),
            i => i.MatchCall<BitsByte>("get_Item")
        );

        c.GotoNext(
            MoveType.After,
            i => i.MatchCallvirt<UnifiedRandom>(nameof(UnifiedRandom.Next))
        );

        c.EmitLdloc(bitsByteIndex);
        c.EmitDelegate(
            static (int duration, BitsByte bitsByte) =>
            {
                if (!bitsByte[4])
                {
                    return duration;
                }

                // TODO: Perhaps allow a custom value?
                return Rand.Next(5, 10);
            }
        );
    }
}

/// <summary>
///     Extended from <see cref="Chest.ItemTransferVisualizationSettings"/>;<br/>
///     provides additional properties for use with <see cref="ChestExtensions.VisualizeChestTransfer"/>.
/// </summary>
/// <param name="RandomizeStartPosition"></param>
/// <param name="RandomizeEndPosition"></param>
/// <param name="TransitionIn"></param>
/// <param name="FullBright"></param>
/// <param name="ShortAnimation">
///     Shortens the animation from 60-80 to 5-10 frames.
/// </param>
/// <param name="Silent">
///     Disables the initial grab sound when used with <see cref="ChestExtensions.TransferWorldItem"/>.
/// </param>
public record struct ItemTransferVisualizationSettingsExt(
    bool RandomizeStartPosition,
    bool RandomizeEndPosition,
    bool TransitionIn,
    bool FullBright,
    bool ShortAnimation,
    bool Silent
)
{
    public static readonly ItemTransferVisualizationSettingsExt PLAYER_TO_CHEST = new()
    {
        RandomizeStartPosition = true,
        RandomizeEndPosition = true,
        TransitionIn = true,
        FullBright = true,
    };

    public static readonly ItemTransferVisualizationSettingsExt HOPPER = new()
    {
        RandomizeEndPosition = true,
    };

    public static implicit operator BitsByte(ItemTransferVisualizationSettingsExt settings)
    {
        var bitsByte = new BitsByte(
            settings.RandomizeStartPosition,
            settings.RandomizeEndPosition,
            settings.TransitionIn,
            settings.FullBright,
            settings.ShortAnimation,
            settings.Silent
        );

        return bitsByte;
    }
}

public static class ChestExtensions
{
    extension(Chest chest)
    {
        public int SilentOpenAnimationTime
        {
            get => ChestBehavior.silentOpenAnimationTime[chest.index];
            set => ChestBehavior.silentOpenAnimationTime[chest.index] = value;
        }
    }

    extension(Chest)
    {
        /// <summary>
        ///     <inheritdoc cref="Chest.AskForChestToEatItem"/><br/>
        ///     Does not play the grab sound effect upon closing.
        /// </summary>
        /// <inheritdoc cref="Chest.AskForChestToEatItem"/>
        public static void AskForChestToOpenSilently(Vector2 worldPosition, int duration)
        {
            var tilePosition = worldPosition.ToTileCoordinates();
            var index = Chest.GetFreeChest(tilePosition);

            if (index == -1)
            {
                return;
            }

            var chest = Main.chest[index];
            chest.SilentOpenAnimationTime = Math.Max(duration, chest.SilentOpenAnimationTime);
        }

        public static int GetFreeChest(Point position)
        {
            var tile = Framing.GetTileSafely(position);

            var (i, j) = position;

            if (tile.frameX % 36 != 0)
            {
                i--;
            }
            if (tile.frameY % 36 != 0)
            {
                j--;
            }

            if (Chest.IsLocked(i, j))
            {
                return -1;
            }

            var chestIndex = Chest.FindChest(i, j);

            if (chestIndex == -1 || Chest.UsingChest(chestIndex) != -1)
            {
                return -1;
            }

            return chestIndex;
        }

        public static bool TransferWorldItem(int worldItemIndex, int chestIndex, bool sort, ItemTransferVisualizationSettingsExt settings)
        {
            var item = Main.item[worldItemIndex];

            // Cache the item type as 'Wiring.TryToPutItemInChest' will run 'TurnToAir.'
            var type = item.type;

            // Cheap hack to have 'Wiring.TryToPutItemInChest' not play the pickup sound.
            ChestBehavior.NextChestOpenSilent = settings.Silent;

            if (!item.active
             || ItemID.Sets.ItemsThatShouldNotBeInInventory[type]
             || !Wiring.TryToPutItemInChest(worldItemIndex, chestIndex))
            {
                return false;
            }

            NetMessage.SendData(MessageID.SyncItem, -1, -1, null, worldItemIndex);

            var chest = Main.chest[chestIndex];
            var chestPosition = new Point(chest.x, chest.y);

            var chestCenter = chestPosition.ToWorldCoordinates(16f, 16f);

            Chest.VisualizeChestTransfer(item.Center, chestCenter, type, settings);

            if (sort)
            {
                ItemSorting.SortInventory(Main.chest[chestIndex], withSync: false, withFeedback: false);
            }

            return true;
        }

        public static void VisualizeChestTransfer(
            Vector2 position,
            Vector2 chestPosition,
            int itemType,
            ItemTransferVisualizationSettingsExt settings
        )
        {
            ParticleOrchestrator.BroadcastOrRequestParticleSpawn(
                ParticleOrchestraType.ItemTransfer,
                new ParticleOrchestraSettings
                {
                    PositionInWorld = position,
                    MovementVector = chestPosition - position,
                    UniqueInfoPiece = itemType | (BitsByte)settings << 24,
                }
            );
        }
    }
}
