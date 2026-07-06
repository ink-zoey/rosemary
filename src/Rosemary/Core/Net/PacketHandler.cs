using Daybreak.Common.Features.Hooks;
using Rosemary.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Rosemary.Core;

public interface IPacketHandler
{
    void Write(BinaryWriter writer, int sender);

    void Read(BinaryReader reader, int sender);
}

public interface IPacketHandler<T> : IPacketHandler
    where T : IPacketHandler<T>
{
    static void Send(Mod mod, int toClient = -1, int ignoreClient = -1)
    {
        var (handler, index) = PacketHandler.HANDLER_INFO_BY_TYPES[typeof(T)];

        if (index == -1)
        {
            return;
        }

        var packet = mod.GetPacket();
        packet.Write(index);
        {
            handler.Write(packet, ignoreClient);
        }
        packet.Send(toClient, ignoreClient);
    }
}

public static class PacketHandler
{
    // TODO: Better method of indexing packets.
    public static readonly Dictionary<Type, (IPacketHandler, int)> HANDLER_INFO_BY_TYPES = [];

    private static readonly Dictionary<int, IPacketHandler> handlers_by_id = [];

    [OnLoad]
    private static void Load(Mod mod)
    {
        var asm = mod.Code;

        var types = asm.GetTypes()
                       .Where(t => t.IsAssignableTo(typeof(IPacketHandler)))
                       .ToArray();

        for (var i = 0; i < types.Length; i++)
        {
            var handler = GetHandler(types[i]);

            HANDLER_INFO_BY_TYPES[types[i]] = (handler, i);
            handlers_by_id[i] = handler;
        }

        return;

        static IPacketHandler GetHandler(Type type)
        {
            if (ModContent.TryGetInstanceAs<IPacketHandler>(type, out var handler))
            {
                return handler;
            }

            return (IPacketHandler)Activator.CreateInstance(type)!;
        }
    }

    public static void Handle(Mod mod, BinaryReader reader, int whoAmI)
    {
        if (Main.netMode == NetmodeID.SinglePlayer || !mod.IsNetSynced)
        {
            return;
        }

        var index = reader.ReadInt32();

        var handler = handlers_by_id[index];

        handler.Read(reader, whoAmI);

        if (Main.netMode == NetmodeID.Server)
        {
            Send();
        }

        return;

        // ReSharper disable once LocalFunctionHidesMethod
        void Send()
        {
            var packet = mod.GetPacket();
            packet.Write(index);
            {
                handler.Write(packet, whoAmI);
            }
            packet.Send(-1, whoAmI);
        }
    }

    extension<T>(IPacketHandler<T>)
        where T : IPacketHandler<T>, new()
    {
        public static void Send(Mod mod, int toClient = -1, int ignoreClient = -1) => IPacketHandler<T>.Send(mod, toClient, ignoreClient);
    }
}
