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

        var entryPointInfo = assembly.EntryPoint;
        entryPointInfo?.Invoke(null, [arguments]);
    }
}
#endif
