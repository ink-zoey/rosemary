using Rosemary.Content;
using Terraria.UI.Chat;

namespace Rosemary.Common;

public static class ChatTagsExtensions
{
    extension(ChatTags)
    {
        public static ShakyTextTag Shaky => (ShakyTextTag)ChatManager.GetHandler(ShakyTextTag.TAG_NAME);
    }
}
