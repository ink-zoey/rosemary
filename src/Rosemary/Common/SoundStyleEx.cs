using Daybreak.Hooks;
using GoldMeridian.CodeAnalysis;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;

namespace Rosemary.Common;

[ExtensionDataFor<ActiveSound>]
internal sealed class ActiveSoundData
{
    public required float AttenuationDistance { get; set; }
}

file static class ActiveSoundDataBehavior
{
    [OnLoad]
    private static void Load()
    {
        IL_ActiveSound.DetermineIntendedVolume += DetermineIntendedVolume_AttenuationDistance;
    }

    private static void DetermineIntendedVolume_AttenuationDistance(ILContext il)
    {
        var c = new ILCursor(il);

        var selfIndex = -1; // arg

        c.GotoNext(
            MoveType.After,
            i => i.MatchLdarg(out selfIndex)
        );

        c.GotoNext(
            MoveType.After,
            i => i.MatchLdsfld<LegacySoundPlayer>(nameof(LegacySoundPlayer.SoundAttenuationDistance))
        );

        c.EmitLdarg(selfIndex);
        c.EmitDelegate(
            static (float dist, ActiveSound sound) =>
            {
                if (sound.Data is null)
                {
                    return dist;
                }

                return sound.Data.AttenuationDistance;
            }
        );
    }
}

public static class SoundEngineExtensions
{
    extension(SoundEngine)
    {
        public static SlotId PlaySound(in SoundStyle style, Vector2? position = null, SoundUpdateCallback? updateCallback = null, float attenuationDistance = 2500f)
        {
            if (!Program.IsMainThread)
            {
                var styleCopy = style;
                return Main.RunOnMainThread(() => SoundEngine.PlaySound(in styleCopy, position, updateCallback, attenuationDistance)).GetAwaiter().GetResult();
            }

            var slot = SoundEngine.PlaySound(in style, attenuationDistance >= 10000f ? null : position, updateCallback);

            if (!SoundEngine.TryGetActiveSound(slot, out var activeSound))
            {
                return slot;
            }

            activeSound.Position = position;

            activeSound.Data ??= new ActiveSoundData
            {
                AttenuationDistance = attenuationDistance,
            };

            activeSound.Data?.AttenuationDistance = attenuationDistance;

            return slot;
        }
    }
}
