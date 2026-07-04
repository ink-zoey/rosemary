using Microsoft.CodeAnalysis;
using System;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;

namespace Rosemary.Core.SourceGen;

[Generator]
public sealed class ElkAtlasReferenceGenerator : IIncrementalGenerator
{
    private const string atlas_descriptor_name = "ElkAtlas.json";

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

    void IIncrementalGenerator.Initialize(
        IncrementalGeneratorInitializationContext context
    )
    {
        var rootNamespaceProvider = AdditionalValueProviders.GetRootNamespaceOrAssemblyName(
            context.AnalyzerConfigOptionsProvider,
            context.CompilationProvider
        );

        var atlasDescriptorProvider = context.AdditionalTextsProvider
                                     .Where(file => file.Path.EndsWith(atlas_descriptor_name))
                                     .Collect();

        var providers = rootNamespaceProvider.Combine(atlasDescriptorProvider);

        context.RegisterSourceOutput(
            providers,
            (ctx, tuple) =>
            {
                var (rootNamespace, descriptors) = tuple;

                ctx.AddSource(
                    "ElkSymbols.g.cs",
                    GenerateReferences(rootNamespace, descriptors[0])
                );
            }
        );
    }

    private static string GenerateReferences(string rootNamespace, AdditionalText atlasDescriptor)
    {
        var sb = new StringBuilder();

        sb.AppendLine(
            $$"""
              #nullable enable
              #pragma warning disable CS8981

              namespace {{rootNamespace}}.Content.Elk;

              // ReSharper disable ArrangeAccessorOwnerBody
              // ReSharper disable InconsistentNaming
              [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
              public static partial class ElkSymbols
              {
              """
        );

        var symbols = JsonConvert.DeserializeObject<ElkSymbol[]>(atlasDescriptor.GetText()!.ToString())!;

        foreach (var symbol in symbols)
        {
            var name = NameSanitizer.ToValidIdentifier(symbol.Name);

            sb.AppendLine(
                $$"""
                      [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
                      public static global::{{rootNamespace}}.Content.Elk.ElkSymbol {{name}}
                      {
                          get
                          {
                              return new global::{{rootNamespace}}.Content.Elk.ElkSymbol(
                                  new global::Microsoft.Xna.Framework.Vector2({{symbol.X}}, {{symbol.Y}}),
                                  new global::Microsoft.Xna.Framework.Rectangle({{symbol.Source.X}}, {{symbol.Source.Y}}, {{symbol.Source.Width}}, {{symbol.Source.Height}}),
                                  {{symbol.Height}}
                              );
                          }
                      }
                      
                      extension(global::{{rootNamespace}}.Content.Elk.ElkPhrase phrase)
                      {
                          [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
                          public global::{{rootNamespace}}.Content.Elk.ElkPhrase {{name}}
                          {
                              get
                              {
                                  phrase.Add({{name}});
                                  return phrase;
                              }
                          }
                      }
                      
                  """
            );
        }

        sb.AppendLine("}");

        return sb.ToString();
    }
}

