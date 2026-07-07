using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Rosemary.Core;

public interface IPacketHandler
{
    void Write(BinaryWriter writer);

    void Read(BinaryReader reader, int sender);

    void Send(ModPacket packet, int toClient = -1, int ignoreClient = -1);
}

public interface IPacketHandler<T> : IPacketHandler, ILoadable
    where T : struct, IPacketHandler<T>
{
    static virtual ushort Id => PacketHandler.PacketId<T>.Id;

    void ILoadable.Load(Mod mod)
    {
        PacketHandler.Register<T>(this);
    }

    void ILoadable.Unload()
    { }

    void IPacketHandler.Send(ModPacket packet, int toClient, int ignoreClient)
    {
        packet.Write(T.Id);

        Write(packet);

        packet.Send(toClient, ignoreClient);
    }
}

public static class PacketHandler
{
    internal static class PacketId<T> where T : struct, IPacketHandler<T>
    {
        // ReSharper disable once StaticMemberInGenericType
        public static ushort Id { get; set; }
    }

    public static readonly Dictionary<ushort, IPacketHandler> HANDLERS_BY_ID = [];

    private static ushort newId;

    public static void Register<T>(IPacketHandler handler)
        where T : struct, IPacketHandler<T>
    {
        PacketId<T>.Id = newId;
        HANDLERS_BY_ID[newId] = handler;

        newId++;
    }

    public static void Handle(Mod mod, BinaryReader reader, int whoAmI)
    {
        if (Main.netMode == NetmodeID.SinglePlayer || !mod.IsNetSynced)
        {
            return;
        }

        var index = reader.ReadUInt16();

        var handler = HANDLERS_BY_ID[index];

        handler.Read(reader, whoAmI);

        if (Main.netMode == NetmodeID.Server)
        {
            handler.Send(mod.GetPacket(), -1, whoAmI);
        }
    }

    extension<T>(IPacketHandler<T> handler)
        where T : struct, IPacketHandler<T>
    {
        public void Send(Mod mod, int toClient = -1, int ignoreClient = -1)
        {
            handler.Send(mod.GetPacket(), toClient, ignoreClient);
        }

        public void Send(int toClient = -1, int ignoreClient = -1) => handler.Send(ModContent.GetInstance<ModImpl>(), toClient, ignoreClient);
    }
}
