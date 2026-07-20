using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Daybreak.Networking;

namespace Rosemary.Vanity;

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

#if PROJECT_BUILD && DEBUG
// ReSharper disable once ClassNeverInstantiated.Local
file class Program
{
    public static void Main(string[] args)
    {
        var file = args[0];

        var arguments = args.Skip(1).ToArray();

        if (!File.Exists(file))
        {
            Console.WriteLine($"File {file} was not found!");
            return;
        }

        Console.WriteLine($"ProjectBuild forwarding to: {file} with arguments: {string.Join(' ', arguments)}");
        Console.WriteLine();

        var assembly = Assembly.LoadFile(file);

        var directory = Path.GetDirectoryName(file)!;

        // TODO: Use a proper assembly resolver.
        var references = assembly.GetReferencedAssemblies();
        foreach (var name in references)
        {
            try
            {
                Console.WriteLine($"Attempting to force resolve assembly: {name.Name}...");

                AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(directory, name.Name + ".dll"));
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not force resolve assembly: {name.Name}! {e.Message}");
            }
        }
        Console.WriteLine();

        var entryPointInfo = assembly.EntryPoint;
        entryPointInfo?.Invoke(null, [arguments]);
    }
}
#endif
