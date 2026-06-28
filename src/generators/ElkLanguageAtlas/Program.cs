using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.Serialization;

namespace ElkLanguageAtlas;

internal static class Program
{
    private const string input_dir = "input";
    private const string output_dir = "output";

    [Serializable]
    private readonly record struct ElkSymbol(string Name, int X, int Y, int Width, int Height, float RealHeight) : ISerializable
    {
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Name), Name);
            info.AddValue(nameof(X), X);
            info.AddValue(nameof(Y), Y);
            info.AddValue(nameof(Width), Width);
            info.AddValue(nameof(Height), Height);
            info.AddValue(nameof(RealHeight), RealHeight);
        }
    };

    public static void Main()
    {
        var dir = Directory.GetCurrentDirectory();

        var inputPath = Path.Combine(dir, input_dir);

        var images = Directory.EnumerateFiles(inputPath, "*.png", SearchOption.TopDirectoryOnly).ToArray();

        GenerateAtlas(dir, images);
    }

    private static void GenerateAtlas(string dir, string[] paths)
    {
        var symbols = GetSymbols(out var atlasWidth, out var atlasHeight);

        CreateAtlas();
        CreateJson();

        foreach (var s in symbols)
        {
            s.Dispose();
        }

        return;

        Image<Rgba32>[] GetSymbols(out int greatestWidth, out int totalHeight)
        {
            var output = new Image<Rgba32>[paths.Length];

            totalHeight = 0;
            greatestWidth = 0;

            for (var i = 0; i < paths.Length; i++)
            {
                output[i] = Image.Load<Rgba32>(paths[i]);
                totalHeight += output[i].Height;
                if (output[i].Width > greatestWidth)
                {
                    greatestWidth = output[i].Width;
                }
            }

            return output;
        }

        void CreateAtlas()
        {
            using var outputImage = new Image<Rgba32>(atlasWidth, atlasHeight);

            var currentY = 0;

            for (var i = 0; i < symbols.Length; i++)
            {
                var symbol = symbols[i];

                for (var x = 0; x < symbol.Width; x++)
                for (var y = 0; y < symbol.Height; y++)
                {
                    var pixel = symbol[x, y];

                    var atlasX = x;
                    var atlasY = currentY + y;

                    outputImage[atlasX, atlasY] = pixel;
                }

                currentY += symbol.Height;
            }

            var outputPath = Path.Combine(dir, output_dir, "atlas.png");

            outputImage.SaveAsPng(outputPath);
        }

        void CreateJson()
        {
            var data = new ElkSymbol[symbols.Length];

            for (var i = 0; i < data.Length; i++)
            {
                var segments = Path.GetFileNameWithoutExtension(paths[i]).Split('-');

                var name = segments[0];

                var x = ParseInt(segments[1]);
                var y = ParseInt(segments[2]);
                var width = symbols[i].Width;
                var height = symbols[i].Width;

                var realHeight = float.Parse(segments[3]);

                data[i] = new ElkSymbol(name, x, y, width, height, realHeight);

                Console.WriteLine(data[i]);
            }

            var outputPath = Path.Combine(dir, output_dir, "atlas.json");

            File.WriteAllText(outputPath, JsonConvert.SerializeObject(data, Formatting.Indented));

            return;

            static int ParseInt(string str)
            {
                str = str.Replace('m', '-');

                return int.Parse(str);
            }
        }
    }
}
