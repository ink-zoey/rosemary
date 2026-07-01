using Daybreak.Common.Features.Authorship;
using Daybreak.Common.Features.ModPanel;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Rosemary;

partial class ModImpl : IHasCustomAuthorMessage
{
    public ModImpl()
    {
        // Handled by the asset generator.
        MusicAutoloadingEnabled = false;
    }

    string IHasCustomAuthorMessage.GetAuthorText()
    {
        return AuthorText.GetAuthorTooltip(this, headerText: null);
    }
}

// HotReload+ launch profile logic.
#if DEBUG
// ReSharper disable once ClassNeverInstantiated.Local
file class Program
{
    public static void Main(string[] args)
    {
        var file = GetDotNetPath();

        var arguments = args.ToList();

        arguments.RemoveAt(0);

        if (!File.Exists(file))
        {
            Console.WriteLine($"File {file} was not found!");

            return;
        }

        Console.WriteLine(file);
        Console.WriteLine();

        Environment.CurrentDirectory = Path.GetDirectoryName(file)!;

        var assembly = Assembly.LoadFile(file);

        var entryPointInfo = assembly.EntryPoint;
        entryPointInfo?.Invoke(null, [arguments.ToArray()]);

        return;

        string GetDotNetPath()
        {
            var pathEnv = Environment.GetEnvironmentVariable("PATH");
            var paths = pathEnv?.Split(Path.PathSeparator);

            return paths?.Select(p => Path.Combine(p, args[0]))
                         .FirstOrDefault(File.Exists)!;
        }
    }
}
#endif
