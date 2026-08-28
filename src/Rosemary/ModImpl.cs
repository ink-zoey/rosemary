using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Layout;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Rosemary;

partial class ModImpl
{
    public ModImpl()
    {
        // Handled by the asset generator.
        MusicAutoloadingEnabled = false;
    }

    public override void HandlePacket(BinaryReader reader, int whoAmI)
    {
        PacketHandler.Handle(this, reader, whoAmI);
    }
}

/*
 * "ProjectBuild" is a launch profile that allows MetadataUpdateHandler to work correctly in Visual Studio,
 * based on LolXD's impl https://discord.com/channels/103110554649894912/534215632795729922/1347395989559967815 (tModLoader Discord)
 * revised slightly to work correctly with tml-build's bootstrapper.
 * ProjectBuild compiles the mod as an executable and launches it, hence the need of an entrypoint.
 * Currently, uses a personal branch of daybreak-mod/assembly-split (assembly-split/rosemary) to prevent loading issues.
 * TODO:
 * - Fix side effects caused by two "instances" of the project being active at once (see Common/Utilities/HotReloading.cs.)
 * - Fix issues with child project RosemaryVanity.
 */
#if PROJECT_BUILD && DEBUG
// ReSharper disable once ClassNeverInstantiated.Local
file class Program
{
    private const string id = "ProjectBuild";

    private static readonly ILog logger = LogManager.GetLogger(id);

    public static void Main(string[] args)
    {
        var layout = new PatternLayout
        {
            ConversionPattern = "[%d{HH:mm:ss.fff}] [%t/%level] [%logger]: %m%n",
        };

        layout.ActivateOptions();

        BasicConfigurator.Configure(
            new ConsoleAppender
            {
                Name = "ConsoleAppender",
                Layout = layout,
            }
        );

        var file = args[0];

        var arguments = args.Skip(1).ToArray();

        if (!File.Exists(file))
        {
            logger.Error($"File {file} was not found!");
            return;
        }

        var assembly = Assembly.LoadFile(file);

        var directory = Path.GetDirectoryName(file)!;

        // TODO: Use a proper assembly resolver.
        var references = assembly.GetReferencedAssemblies();
        foreach (var name in references)
        {
            try
            {
                logger.Info($"Attempting to force resolve assembly: {name.Name}...");

                AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(directory, name.Name + ".dll"));

                logger.Info($"Successfully force resolved assembly: {name.Name}!");
            }
            catch (Exception e)
            {
                logger.Warn($"Could not force resolve assembly: {name.Name}! \n{e.Message}");
            }
        }
        logger.Info($"Forwarding to: {file} with arguments: {string.Join(' ', arguments)}");

        var entryPointInfo = assembly.EntryPoint;
        entryPointInfo?.Invoke(null, [arguments]);
    }
}
#endif
