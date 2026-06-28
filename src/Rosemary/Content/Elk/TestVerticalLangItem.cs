using Terraria.ModLoader;

namespace Rosemary.Content;

public sealed class TestVerticalLangItem : ModItem, IElkLanguage<ModItem>
{
    public override string Texture => Assets.Elk.TestItem.KEY;

    ElkLanguagePhrase IElkLanguage<ModItem>.Phrase => /*Whatever*/;
}
