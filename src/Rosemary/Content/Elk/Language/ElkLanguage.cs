using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Rosemary.Content;

public sealed class ElkPhrase : List<ElkSymbol>;

public struct ElkSymbol(Vector2 Position, Vector2 Size, float Height);