using Daybreak.Hooks;
using Daybreak.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using Rosemary.Core;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ObjectData;
using Terraria.UI;

// ReSharper disable InconsistentNaming
// ReSharper disable UseSymbolAlias
namespace Rosemary.Common;

file static class ChestBehavior
{
    internal static readonly int[] silentOpenAnimationTime = new int[Main.maxChests];

    [OnLoad]
    private static void Load()
    {
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
}

[Autoload(Side = ModSide.Client)]
file static class ChestParticles
{
    internal record struct ItemTransferData(
        int ItemType,
        Vector2 Position,
        Vector2 ChestPosition,
        int LifeTime,
        bool RandomizeStartPosition,
        bool RandomizeEndPosition,
        bool TransitionIn,
        bool FullBright,
        bool AnimateChest,
        bool Silent
    );

    private record struct Packet(ItemTransferData Data) : IPacket<Packet>
    {
        public void Write(BinaryWriter writer)
        {
            writer.Write(Data.ItemType);
            writer.WriteVector2(Data.Position);
            writer.WriteVector2(Data.ChestPosition);
            writer.Write(Data.LifeTime);

            writer.WriteFlags(
                Data.RandomizeStartPosition,
                Data.RandomizeEndPosition,
                Data.TransitionIn,
                Data.FullBright,
                Data.AnimateChest,
                Data.Silent
            );
        }

        public static void Receive(BinaryReader reader, int sender)
        {
            var type = reader.ReadInt32();
            var position = reader.ReadVector2();
            var chestPosition = reader.ReadVector2();
            var lifeTime = reader.ReadInt32();

            reader.ReadFlags(
                out var randomizeStartPosition,
                out var randomizeEndPosition,
                out var transitionIn,
                out var fullBright,
                out var animateChest,
                out var silent
            );

            var data = new ItemTransferData(
                type,
                position,
                chestPosition,
                lifeTime,
                randomizeStartPosition,
                randomizeEndPosition,
                transitionIn,
                fullBright,
                animateChest,
                silent
            );

            if (Main.netMode != NetmodeID.Server)
            {
                ItemTransfer += new ItemTransferParticle(data);
            }
        }
    }

    private struct ItemTransferParticle : IUpdatingParticle
    {
        public Vector2 Position { get; private set; }

        public int ItemType { get; }

        private Vector2 StartPosition { get; }

        private Vector2 EndPosition { get; }

        private Vector2 StartOffset { get; }

        private Vector2 EndOffset { get; }

        private Vector2 BezierHelper1 { get; }

        private Vector2 BezierHelper2 { get; }

        private SoundStyle? Sound { get; }

        public bool TransitionIn { get; }

        public bool FullBright { get; }

        public int LifeTime { get; private set; }

        public int LifeTimeTotal { get; }

        public ItemTransferParticle(ItemTransferData data)
        {
            ItemType = data.ItemType;
            StartPosition = data.Position;
            EndPosition = data.ChestPosition;

            var offset = Rand.NextSquare(-1f, 1f);

            StartOffset = data.RandomizeStartPosition ? (offset * 24f) : Vector2.Zero;
            EndOffset = data.RandomizeEndPosition ? (offset * 8f) : Vector2.Zero;

            TransitionIn = data.TransitionIn;
            FullBright = data.FullBright;
            Sound = data.Silent ? null : SoundID.Grab;

            LifeTime = 0;
            LifeTimeTotal = data.LifeTime;

            if (data.AnimateChest)
            {
                Chest.AskForChestToOpenSilently(EndPosition, LifeTimeTotal);
            }

            var length = (EndPosition - StartPosition).Length();

            BezierHelper1 = (-Vector2.UnitY * length) + Rand.NextUnitVector(32f);
            BezierHelper2 = (Vector2.UnitY * length) + Rand.NextUnitVector(32f);
        }

        bool IUpdatingParticle.Update()
        {
            LifeTime++;

            var ratio = (float)LifeTime / LifeTimeTotal;

            // Mysterious vanilla logic
            var toMin = Utils.Remap(ratio, 0.1f, 0.5f, 0f, 0.85f);
            toMin = Utils.Remap(ratio, 0.5f, 0.9f, toMin, 1f);

            Position = Vector2.Hermite(StartPosition, BezierHelper1, EndPosition, BezierHelper2, toMin);

            var offset = ratio switch
            {
                <= 0.15f => Vector2.Lerp(Vector2.Zero, StartOffset, ratio / 0.15f),
                <= 0.5f => Vector2.Lerp(StartOffset, EndOffset, (ratio - 0.15f) / 0.35f),
                > 0.85f => Vector2.Lerp(EndOffset, Vector2.Zero, Utils.Remap(ratio, 0.85f, 0.95f, 0f, 1f)),
                _ => EndOffset,
            };

            Position += offset;

            if (LifeTime == LifeTimeTotal && Sound is not null)
            {
                SoundEngine.PlaySound(Sound, Position);
            }

            return LifeTime <= LifeTimeTotal;
        }
    }

    private static UpdatingParticleHandler<ItemTransferParticle> ItemTransfer { get; set; } = new(30);

    public static void BroadcastChestTransfer(ItemTransferData data)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            new Packet(data).Send(PacketDestination.Broadcast);
            return;
        }

        ItemTransfer += new ItemTransferParticle(data);
    }

    [ModSystemHooks.PostUpdateDusts]
    private static void UpdateParticles()
    {
        ItemTransfer.Update();
    }

    [ParticleLayers.OverPlayers]
    private static void DrawParticlesOverPlayers(SpriteBatch sb)
    {
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
        {
            DrawItemTransfer();
        }
        sb.End();

        return;

        void DrawItemTransfer()
        {
            const int magic_context = 31;
            const float size_limit = 32f;

            if (ItemTransfer.ActiveParticleCount <= 0)
            {
                return;
            }

            foreach (var index in ItemTransfer)
            {
                var itemTransfer = ItemTransfer[index];

                var position = itemTransfer.Position - Main.screenPosition;

                if (!ContentSamples.ItemsByType.TryGetValue(itemTransfer.ItemType, out var item))
                {
                    continue;
                }

                var ratio = (float)itemTransfer.LifeTime / itemTransfer.LifeTimeTotal;

                var color = itemTransfer.FullBright
                    ? Color.White
                    : Lighting.GetColor(itemTransfer.Position.ToTileCoordinates());

                var scale = item.scale;

                if (itemTransfer.TransitionIn)
                {
                    scale *= Utils.Remap(ratio, 0f, 0.15f, 0f, 1f);
                }

                scale *= Utils.Remap(ratio, 0.65f, 0.95f, 1f, 0f);

                ItemSlot.DrawItemIcon(item, magic_context, sb, position, scale, size_limit, color);
            }
        }
    }
}

public enum PersonalStorageType
{
    PiggyBank = -2,
    Safe = -3,
    DefendersForge = -4,
    VoidVault = -5,
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

        /// <summary>
        ///     Places the item into the first available slot, automatically stacking if needed.
        /// </summary>
        /// <param name="item">
        ///     The item to add.
        /// </param>
        /// <param name="whoAmI">
        ///     The index of this chest either in <see cref="Main.chest"/> or a negative value for personal storage.
        /// </param>
        /// <param name="silent">
        ///     Disables the grab sound effect.
        /// </param>
        /// <returns>
        ///     <see langword="true"/> if any part of <paramref name="item"/> was placed into the chest;<br/>
        ///     check <![CDATA[item.stack]]> to make sure the item was fully deposited.
        /// </returns>
        public bool TryAddingItem(Item item, int whoAmI, bool silent = true)
        {
            Item[] inv = chest.item;

            if (ChestUI.IsBlockedFromTransferIntoChest(item, inv))
            {
                return false;
            }

            if (item.maxStack > 1 && StackItems())
            {
                return true;
            }
            
            // Shouldn't really run unless there's some item with a maxStack of 1.
            return StackSingleItem();

            bool StackSingleItem()
            {
                for (var i = 0; i < chest.maxItems; i++)
                {
                    if (!inv[i].IsAir)
                    {
                        continue;
                    }

                    if (!silent)
                    {
                        SoundEngine.PlaySound(in SoundID.Grab);
                    }

                    inv[i] = item.Clone();
                    item.SetDefaults(ItemID.None);

                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        NetMessage.SendData(MessageID.SyncChestItem, -1, -1, null, whoAmI, i);
                    }

                    return true;
                }

                return false;
            }

            bool StackItems()
            {
                var returnValue = false;

                for (var i = 0; i < chest.maxItems; i++)
                {
                    // Empty slot, just deposit the remainder of the item.
                    if (inv[i].IsAir)
                    {
                        returnValue = true;

                        inv[i] = item.Clone();
                        item.SetDefaults(ItemID.None);

                        if (Main.netMode == NetmodeID.MultiplayerClient)
                        {
                            NetMessage.SendData(MessageID.SyncChestItem, -1, -1, null, whoAmI, i);
                        }

                        break;
                    }

                    if (inv[i].stack >= inv[i].maxStack
                     || !Item.CanStack(item, inv[i])
                     || !ItemLoader.CanStack(item, inv[i]))
                    {
                        continue;
                    }

                    returnValue = true;

                    ItemLoader.StackItems(inv[i], item, out _);

                    if (item.stack <= 0)
                    {
                        item.SetDefaults(ItemID.None);

                        if (Main.netMode == NetmodeID.MultiplayerClient)
                        {
                            NetMessage.SendData(MessageID.SyncChestItem, -1, -1, null, whoAmI, i);
                        }

                        break;
                    }

                    // Item was not fully deposited, loop.
                }

                if (!silent && returnValue)
                {
                    SoundEngine.PlaySound(in SoundID.Grab);
                }

                return returnValue;
            }
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

        /// <summary>
        /// Tries to find the index of the <see cref="Chest"/> at <paramref name="position"/> (in tile coordinates.)<br/>
        /// </summary>
        /// <param name="position">Target position in tile coordinates, does not have to be the top left of the tile.</param>
        /// <returns>
        ///     The index of the <see cref="Chest"/>, <![CDATA[-1]]> if not found.
        /// </returns>
        public static int GetFreeChest(Point position)
        {
            var tile = Framing.GetTileSafely(position);

            // For whatever reason trapped containers aren't considered under IsAContainer?
            if (!TileID.Sets.IsAContainer[tile.TileType]
             && tile.TileType != TileID.FakeContainers
             && tile.TileType != TileID.FakeContainers2)
            {
                return -1;
            }

            var (i, j) = position;

            if (!TryFindChest(out var chestIndex)
             && !TryFindVanilla(out chestIndex)
             && !TryFindDressers(out chestIndex)
             && !TryFindFromTopLeft(out chestIndex))
            {
                return -1;
            }

            if (Chest.IsLocked(i, j))
            {
                return -1;
            }

            if (chestIndex == -1 || Chest.UsingChest(chestIndex) != -1)
            {
                return -1;
            }

            return chestIndex;

            bool TryFindChest(out int index)
            {
                index = Chest.FindChest(i, j);

                return index != -1;
            }

            bool TryFindVanilla(out int index)
            {
                var (i2, j2) = (i, j);

                if (tile.frameX % 36 != 0)
                {
                    i2--;
                }

                if (tile.frameY % 36 != 0)
                {
                    j2--;
                }

                index = Chest.FindChest(i2, j2);

                if (index == -1)
                {
                    return false;
                }

                (i, j) = (i2, j2);
                return true;
            }

            bool TryFindDressers(out int index)
            {
                index = -1;

                if (!TileID.Sets.BasicDresser[tile.TileType])
                {
                    return false;
                }

                var frameX = tile.TileFrameX / 18;
                frameX %= 3;
                var i2 = i - frameX;

                var frameY = tile.TileFrameY / 18;
                frameY %= 2;
                var j2 = j - frameY;

                index = Chest.FindChest(i2, j2);

                if (index == -1)
                {
                    return false;
                }

                (i, j) = (i2, j2);
                return true;
            }

            bool TryFindFromTopLeft(out int index)
            {
                index = -1;

                var (i2, j2) = TileObjectData.TopLeft(i, j);

                index = Chest.FindChest(i2, j2);

                if (index == -1)
                {
                    return false;
                }

                (i, j) = (i2, j2);
                return true;
            }
        }

        /// <summary>
        ///     Transfers the <see cref="WorldItem"/> to the specified chest.
        /// </summary>
        /// <param name="worldItemIndex">
        ///     The index of the <see cref="WorldItem"/>.
        /// </param>
        /// <param name="chestIndex">
        ///     The index of the <see cref="Chest"/>, should not be negative.
        /// </param>
        /// <param name="sort">
        ///     Whether to sort the chest after adding the item.
        /// </param>
        /// <param name="silent">
        ///     Disables the grab sound from placing the item into storage.
        /// </param>
        /// <returns>
        ///     <see langword="true"/> if any part of the item was placed into the chest;<br/>
        ///     check <![CDATA[item.stack]]> to make sure the item was fully deposited.
        /// </returns>
        public static bool TransferWorldItem(
            int worldItemIndex,
            int chestIndex,
            bool sort,
            bool silent = true
        )
        {
            if (chestIndex == -1)
            {
                return false;
            }

            var item = Main.item[worldItemIndex];

            // Cache the item type as adding the item to the chest may turn it into air.
            var type = item.type;

            var chest = Main.chest[chestIndex];

            if (!item.active
             || ItemID.Sets.ItemsThatShouldNotBeInInventory[type]
             || !chest.TryAddingItem(item.inner, chestIndex, silent))
            {
                return false;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetMessage.SendData(MessageID.SyncItem, -1, -1, null, worldItemIndex);
            }

            if (sort)
            {
                ItemSorting.SortInventory(chest, withSync: false, withFeedback: false);
            }

            return true;
        }

        /// <summary>
        ///     Transfers the <see cref="WorldItem"/> to the specified personal storage.<br/>
        ///     Should only be called on the client whose storage the item should be deposited to.
        /// </summary>
        /// <param name="worldItemIndex">
        ///     The index of the <see cref="WorldItem"/>.
        /// </param>
        /// <param name="storageType">
        ///     The type of personal storage to add to.
        /// </param>
        /// <param name="sort">
        ///     Whether to sort the storage after adding the item.
        /// </param>
        /// <param name="silent">
        ///     Disables the grab sound from placing the item into storage.
        /// </param>
        /// <returns>
        ///     <see langword="true"/> if any part of the item was placed into the storage;<br/>
        ///     check <![CDATA[item.stack]]> to make sure the item was fully deposited.
        /// </returns>
        public static bool TransferWorldItemPersonalStorage(
            int worldItemIndex,
            PersonalStorageType storageType,
            bool sort,
            bool silent = true
        )
        {
            var player = Main.LocalPlayer;

            var item = Main.item[worldItemIndex];

            var chest = Chest.GetPersonalStorage(storageType, player);

            // Cache the item type as 'Wiring.TryToPutItemInChest' will run 'TurnToAir.'
            var type = item.type;

            if (!item.active
             || ItemID.Sets.ItemsThatShouldNotBeInInventory[type]
             || !chest.TryAddingItem(item.inner, (int)storageType, silent))
            {
                return false;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetMessage.SendData(MessageID.SyncItem, -1, -1, null, worldItemIndex);
            }

            if (sort)
            {
                ItemSorting.SortInventory(chest, withSync: false, withFeedback: false);
            }

            return true;
        }

        public static Chest GetPersonalStorage(PersonalStorageType type, Player player)
        {
            return type switch
            {
                PersonalStorageType.PiggyBank => player.bank,
                PersonalStorageType.Safe => player.bank2,
                PersonalStorageType.DefendersForge => player.bank3,
                _ => player.bank4,
            };
        }

        /// <summary>
        /// Broadcasts an item transfer particle to all clients.
        /// </summary>
        /// <param name="type">
        ///     The item's type.
        /// </param>
        /// <param name="startPosition"></param>
        /// <param name="endPosition"></param>
        /// <param name="lifeTime">
        ///     How long the animation will last.
        /// </param>
        /// <param name="randomizeStartPosition"></param>
        /// <param name="randomizeEndPosition"></param>
        /// <param name="transitionIn">
        ///     If the particle will scale up when spawning, used for quick-stacking into chests from the inventory.
        /// </param>
        /// <param name="fullBright"></param>
        /// <param name="animateChest">
        ///     Whether to animate the chest at <see cref="endPosition"/>,
        /// </param>
        /// <param name="silent">
        ///     Whether to play the grab sound upon the animation's completion.
        /// </param>
        public static void VisualizeChestTransfer(
            int type,
            Vector2 startPosition,
            Vector2 endPosition,
            int lifeTime,
            bool randomizeStartPosition = false,
            bool randomizeEndPosition = false,
            bool transitionIn = false,
            bool fullBright = false,
            bool animateChest = false,
            bool silent = false
        )
        {
            ChestParticles.BroadcastChestTransfer(
                new ChestParticles.ItemTransferData(
                    type,
                    startPosition,
                    endPosition,
                    lifeTime,
                    randomizeStartPosition,
                    randomizeEndPosition,
                    transitionIn,
                    fullBright,
                    animateChest,
                    silent
                )
            );
        }
    }
}
