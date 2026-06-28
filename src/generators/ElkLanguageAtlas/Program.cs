using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.Serialization;
using RectpackSharp;

namespace ElkLanguageAtlas;

internal static class Program
{
    private const string input_dir = "input";
    private const string output_dir = "output";

    [Serializable]
    private readonly record struct ElkSymbol(string Name, ElkSymbol.Rectangle Destination, ElkSymbol.Rectangle Source, float RealHeight) : ISerializable
    {
        // Dummy type to serialize rectangles properly.
        [Serializable]
        public readonly record struct Rectangle(int X, int Y, int Width, int Height) : ISerializable
        {
            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue(nameof(X), X);
                info.AddValue(nameof(Y), Y);
                info.AddValue(nameof(Width), Width);
                info.AddValue(nameof(Height), Height);;
            }
        };

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Name), Name);
            info.AddValue(nameof(Destination), Destination);
            info.AddValue(nameof(Source), Source);
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
        var (symbols, rectangles) = GetSymbols();

        RectanglePacker.Pack(rectangles, out var bounds);

        CreateAtlas();
        CreateJson();

        foreach (var s in symbols)
        {
            s.Dispose();
        }

        return;

        (Image<Rgba32>[], PackingRectangle[]) GetSymbols()
        {
            var images = new Image<Rgba32>[paths.Length];
            var rectangles = new PackingRectangle[paths.Length];

            for (var i = 0; i < paths.Length; i++)
            {
                images[i] = Image.Load<Rgba32>(paths[i]);
                rectangles[i] = new PackingRectangle(0, 0, (uint)images[i].Width, (uint)images[i].Height, i);
            }

            return (images, rectangles);
        }

        void CreateAtlas()
        {
            using var outputImage = new Image<Rgba32>((int)bounds.Width, (int)bounds.Height);

            for (var i = 0; i < symbols.Length; i++)
            {
                var symbol = symbols[i];
                var rectangle = rectangles[i];

                for (var x = 0; x < symbol.Width; x++)
                for (var y = 0; y < symbol.Height; y++)
                {
                    var pixel = symbol[x, y];

                    var atlasX = (int)rectangle.X + x;
                    var atlasY = (int)rectangle.Y + y;

                    outputImage[atlasX, atlasY] = pixel;
                }
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

                var destination = new ElkSymbol.Rectangle(ParseInt(segments[1]), ParseInt(segments[2]), symbols[i].Width, symbols[i].Height);
                var source = new ElkSymbol.Rectangle((int)rectangles[i].X, (int)rectangles[i].Y, (int)rectangles[i].Width, (int)rectangles[i].Height);

                var realHeight = float.Parse(segments[3]);

                data[i] = new ElkSymbol(name, destination, source, realHeight);

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
