using Daybreak.Hooks;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.UI;

namespace Rosemary.Common;

public static class GameInterfaceLayers
{
    public const string INTERFACE_LOGIC_1           = "Vanilla: Interface Logic 1";
    public const string MP_PLAYER_NAMES             = "Vanilla: MP Player Names";
    public const string EMOTE_BUBBLES               = "Vanilla: Emote Bubbles";
    public const string ENTITY_MARKERS              = "Vanilla: Entity Markers";
    public const string SMART_CURSOR_TARGETS        = "Vanilla: Smart Cursor Targets";
    public const string LASER_RULER                 = "Vanilla: Laser Ruler";
    public const string RULER                       = "Vanilla: Ruler";
    public const string GAMEPAD_LOCK_ON             = "Vanilla: Gamepad Lock On";
    public const string TILE_GRID_OPTION            = "Vanilla: Tile Grid Option";
	public const string TOWN_NPC_HOUSE_BANNERS      = "Vanilla: Town NPC House Banners";
	public const string HIDE_UI_TOGGLE              = "Vanilla: Hide UI Toggle";
	public const string WIRE_SELECTION              = "Vanilla: Wire Selection";
	public const string CAPTURE_MANAGER_CHECK       = "Vanilla: Capture Manager Check";
	public const string IN_GAME_OPTIONS             = "Vanilla: Ingame Options";
	public const string FANCY_UI                    = "Vanilla: Fancy UI";
	public const string ACHIEVEMENT_COMPLETE_POPUPS = "Vanilla: Achievement Complete Popups";
	public const string ENTITY_HEALTH_BARS          = "Vanilla: Entity Health Bars";
	public const string INVASION_PROGRESS_BARS      = "Vanilla: Invasion Progress Bars";
	public const string MAP_MINIMAP                 = "Vanilla: Map / Minimap";
	public const string DIAGNOSE_NET                = "Vanilla: Diagnose Net";
	public const string SIGN_TILE_BUBBLE            = "Vanilla: Sign Tile Bubble";
	public const string HAIR_WINDOW                 = "Vanilla: Hair Window";
	public const string DRESSER_WINDOW              = "Vanilla: Dresser Window";
	public const string NPC_SIGN_DIALOG             = "Vanilla: NPC / Sign Dialog";
	public const string INTERFACE_LOGIC_2           = "Vanilla: Interface Logic 2";
	public const string RESOURCE_BARS               = "Vanilla: Resource Bars";
	public const string INTERFACE_LOGIC_3           = "Vanilla: Interface Logic 3";
	public const string INVENTORY                   = "Vanilla: Inventory";
	public const string INFO_ACCESSORIES_BAR        = "Vanilla: Info Accessories Bar";
	public const string SETTINGS_BUTTON             = "Vanilla: Settings Button";
	public const string CONTROL_HINTS               = "Vanilla: Control Hints";
	public const string HOTBAR                      = "Vanilla: Hotbar";
	public const string BUILDER_ACCESSORIES_BAR     = "Vanilla: Builder Accessories Bar";
	public const string RADIAL_HOTBARS              = "Vanilla: Radial Hotbars";
	public const string MOUSE_TEXT                  = "Vanilla: Mouse Text";
	public const string PLAYER_CHAT                 = "Vanilla: Player Chat";
	public const string DEATH_TEXT                  = "Vanilla: Death Text";
	public const string CURSOR                      = "Vanilla: Cursor";
	public const string DEBUG_STUFF                 = "Vanilla: Debug Stuff";
	public const string MOUSE_ITEM_NPC_HEAD         = "Vanilla: Mouse Item / NPC Head";
	public const string MOUSE_OVER                  = "Vanilla: Mouse Over";
	public const string INTERACT_ITEM_ICON          = "Vanilla: Interact Item Icon";
	public const string INTERFACE_LOGIC_4           = "Vanilla: Interface Logic 4";

    /// <summary>
    ///     Inserts the decorated method as a <see cref="GameInterfaceLayer"/> before/after the target layer.
    /// </summary>
    /// <param name="targetLayer">
    ///     Name of the target <see cref="GameInterfaceLayer"/>, constants are provided for vanilla layers in <see cref="GameInterfaceLayers"/>.
    /// </param>
    /// <param name="scaleType">
    ///     Changes how the cursor/screen is scaled, along with what matrix is used in the given SpriteBatch.
    /// </param>
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    [HookMetadata(DelegateType = typeof(GameInterfaceDrawMethod))]
    public class InsertAttribute(string targetLayer, InterfaceScaleType scaleType, bool after) : BaseHookAttribute
    {
        public readonly string TargetLayer = targetLayer;

        public readonly InterfaceScaleType ScaleType = scaleType;

        public readonly bool After = after;

        /// <summary>
        ///     Identifier of this layer, usually <c>"ModName: UIName"</c>,
        ///     leave <see langword="null"/> to use default identifier based on <c>"AssemblyName: MethodName"</c>.
        /// </summary>
        public string? Name { get; set; }

        public override void Apply(MethodInfo bindingMethod, object? instance)
        {
            var name = Name ?? $"{bindingMethod.DeclaringType!.Assembly.GetName().Name}: {bindingMethod.Name}";

            layers.Add(
                new Layer(
                    bindingMethod.CreateDelegate<GameInterfaceDrawMethod>(),
                    name,
                    TargetLayer,
                    ScaleType,
                    After
                )
            );
        }
    }

    /// <summary>
    ///     Inserts the decorated method as a <see cref="GameInterfaceLayer"/> before the target layer.
    /// </summary>
    /// <inheritdoc cref="InsertAttribute"/>
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    [HookMetadata(DelegateType = typeof(GameInterfaceDrawMethod))]
    public sealed class BeforeAttribute(string targetLayer, InterfaceScaleType scaleType)
        : InsertAttribute(targetLayer, scaleType, false);

    /// <summary>
    ///     Inserts the decorated method as a <see cref="GameInterfaceLayer"/> after the target layer.
    /// </summary>
    /// <inheritdoc cref="InsertAttribute"/>
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    [HookMetadata(DelegateType = typeof(GameInterfaceDrawMethod))]
    public sealed class AfterAttribute(string targetLayer, InterfaceScaleType scaleType)
        : InsertAttribute(targetLayer, scaleType, true);

    private record struct Layer(GameInterfaceDrawMethod Method, string Name, string TargetLayer, InterfaceScaleType ScaleType, bool After);

    private static readonly HashSet<Layer> layers = [];

    [ModSystemHooks.ModifyInterfaceLayers]
    private static void PostSetupContent([OriginalName("layers")] List<GameInterfaceLayer> interfaceLayers)
    {
        foreach (var layer in layers)
        {
            var index = interfaceLayers.FindIndex(l => l.Name.Equals(layer.TargetLayer));

            if (index <= -1)
            {
                continue;
            }

            if (layer.After)
            {
                index++;
            }

            interfaceLayers.Insert(
                index,
                new LegacyGameInterfaceLayer(
                    layer.Name,
                    layer.Method,
                    layer.ScaleType
                )
            );
        }
    }
}
