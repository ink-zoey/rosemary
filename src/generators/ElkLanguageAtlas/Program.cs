using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.Serialization;
using RectpackSharp;

namespace Rosemary.Core.SourceGen;

internal static class Program
{
    private const string atlas_image_name = "ElkAtlas.png";
    private const string atlas_descriptor_name = "ElkAtlas.json";

    private const string input_dir = "input";
    private const string output_dir = "output";

    [Serializable]
    private readonly record struct ElkSymbol(string Name, int X, int Y, ElkSymbol.Rectangle Source, float Height) : ISerializable
    {
        // Dummy type to serialize rectangles properly.
        [Serializable]
        public readonly record struct Rectangle(int X, int Y, int Width, int Height) : ISerializable
        {
            public Rectangle(SerializationInfo info, StreamingContext context)
                : this(
                    info.GetInt32(nameof(X)),
                    info.GetInt32(nameof(Y)),
                    info.GetInt32(nameof(Width)),
                    info.GetInt32(nameof(Height)))
            { }

            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue(nameof(X), X);
                info.AddValue(nameof(Y), Y);
                info.AddValue(nameof(Width), Width);
                info.AddValue(nameof(Height), Height);
            }
        };

        public ElkSymbol(SerializationInfo info, StreamingContext context)
            : this(
                info.GetString(nameof(Name))!,
                info.GetInt32(nameof(X)),
                info.GetInt32(nameof(Y)),
                (Rectangle)info.GetValue(nameof(Source), typeof(Rectangle))!,
                info.GetSingle(nameof(Height)))
        { }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Name), Name);
            info.AddValue(nameof(X), X);
            info.AddValue(nameof(Y), Y);
            info.AddValue(nameof(Source), Source);
            info.AddValue(nameof(Height), Height);
        }
    }

    public static void Main()
    {
        var dir = Directory.GetCurrentDirectory();

        var inputPath = Path.Combine(dir, input_dir);

        var images = Directory.EnumerateFiles(inputPath, "*.png", SearchOption.TopDirectoryOnly).ToArray();

        GenerateAtlas(dir, images);
    }

    private static void GenerateAtlas(string dir, string[] paths)
    {
        const int padding = 2;

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
            var rects = new PackingRectangle[paths.Length];

            for (var i = 0; i < paths.Length; i++)
            {
                images[i] = Image.Load<Rgba32>(paths[i]);
                rects[i] = new PackingRectangle(0, 0, (uint)images[i].Width + (padding * 2), (uint)images[i].Height + (padding * 2), i);
            }

            return (images, rects);
        }

        void CreateAtlas()
        {
            using var outputImage = new Image<Rgba32>((int)bounds.Width, (int)bounds.Height);

            for (var i = 0; i < symbols.Length; i++)
            {
                var symbol = symbols[i];
                var rectangle = rectangles.First(r => r.Id == i);

                for (var x = 0; x < rectangle.Width - (padding * 2); x++)
                for (var y = 0; y < rectangle.Height - (padding * 2); y++)
                {
                    var pixel = symbol[x, y];

                    pixel = Premultiply(pixel);

                    var atlasX = (int)rectangle.X + padding + x;
                    var atlasY = (int)rectangle.Y + padding + y;

                    outputImage[atlasX, atlasY] = pixel;
                }

                Console.WriteLine($"{rectangle.X}, {rectangle.Y}");
            }

            var outputPath = Path.Combine(dir, output_dir, atlas_image_name);

            outputImage.SaveAsPng(outputPath);

            return;

            static Rgba32 Premultiply(Rgba32 color)
            {
                var alpha = (float)color.A / byte.MaxValue;

                if (alpha <= 0)
                {
                    return color;
                }

                color.R = (byte)(color.R * alpha);
                color.G = (byte)(color.G * alpha);
                color.B = (byte)(color.B * alpha);

                return color;
            }
        }

        void CreateJson()
        {
            var data = new ElkSymbol[symbols.Length];

            for (var i = 0; i < data.Length; i++)
            {
                var segments = Path.GetFileNameWithoutExtension(paths[i]).Split('-');

                var name = segments[0];

                var rectangle = rectangles.First(r => r.Id == i);

                var source = new ElkSymbol.Rectangle((int)rectangle.X + padding, (int)rectangle.Y + padding, (int)rectangle.Width - (padding * 2), (int)rectangle.Height - (padding * 2));

                var realHeight = float.Parse(segments[3]);

                data[i] = new ElkSymbol(name, ParseInt(segments[1]), ParseInt(segments[2]), source, realHeight);

                Console.WriteLine(data[i]);
            }

            var outputPath = Path.Combine(dir, output_dir, atlas_descriptor_name);

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
