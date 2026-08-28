using Terraria.UI.Chat;

namespace Rosemary.Common;

// TODO: Remove when Daybreak.ChatTags fixes this.
file static class TEMP_ChatManagerFix
{
    [OnLoad]
    private static void Load()
    {
        IL_ChatManager.DrawColorCodedStringShadow_SpriteBatch_DynamicSpriteFont_IEnumerable1_Vector2_Color_float_Vector2_Vector2_float_float += _ => { };
        IL_ChatManager.DrawColorCodedStringShadow_SpriteBatch_DynamicSpriteFont_List1_Vector2_Color_float_Vector2_Vector2_float += _ => { };
        IL_ChatManager.DrawColorCodedStringShadow_SpriteBatch_DynamicSpriteFont_string_Vector2_Color_float_Vector2_Vector2_float_float += _ => { };
        IL_ChatManager.DrawColorCodedStringWithShadow_SpriteBatch_DynamicSpriteFont_TextSnippetArray_Vector2_Color_Color_float_Vector2_Vector2_refInt32_float_float += _ => { };
        IL_ChatManager.DrawColorCodedStringWithShadow_SpriteBatch_DynamicSpriteFont_TextSnippetArray_Vector2_Color_float_Vector2_Vector2_refInt32_float_float += _ => { };
        IL_ChatManager.DrawColorCodedStringWithShadow_SpriteBatch_DynamicSpriteFont_TextSnippetArray_Vector2_float_Vector2_Vector2_refInt32_float_float += _ => { };
        IL_ChatManager.DrawColorCodedStringWithShadow_SpriteBatch_DynamicSpriteFont_string_Vector2_Color_Color_float_Vector2_Vector2_float_float += _ => { };
        IL_ChatManager.DrawColorCodedStringWithShadow_SpriteBatch_DynamicSpriteFont_string_Vector2_Color_float_Vector2_Vector2_float_float += _ => { };
        IL_ChatManager.DrawColorCodedString_SpriteBatch_DynamicSpriteFont_IEnumerable1_Vector2_Color_float_Vector2_Vector2_refInt32_float_bool += _ => { };
        IL_ChatManager.DrawColorCodedString_SpriteBatch_DynamicSpriteFont_IEnumerable1_Vector2_float_Vector2_Vector2_refInt32_Nullable1 += _ => { };
        IL_ChatManager.DrawColorCodedString_SpriteBatch_DynamicSpriteFont_IEnumerable1_Vector2_float_Vector2_Vector2_refInt32_float += _ => { };
        IL_ChatManager.DrawColorCodedString_SpriteBatch_DynamicSpriteFont_string_Vector2_Color_float_Vector2_Vector2_float_bool += _ => { };
    }
}
