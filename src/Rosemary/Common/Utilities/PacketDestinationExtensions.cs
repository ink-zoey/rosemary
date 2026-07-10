using System;
using System.Collections.Generic;
using System.Text;
using Daybreak.Networking;

namespace Rosemary.Common;

public static class PacketDestinationExtensions
{
    extension(PacketDestination)
    {
        public static PacketDestination From(int toWho, int fromWho)
        {
            return toWho == -1 ? PacketDestination.AllExcept(fromWho) : PacketDestination.Only(toWho);
        }
    }
}
