using Microsoft.Xna.Framework;
using Terraria.Graphics.CameraModifiers;

namespace Rosemary.Common;

public delegate bool PunchCameraCallback(CallbackPunchCameraModifier modifier);

public class CallbackPunchCameraModifier(
    Vector2 startPosition,
    Vector2 direction,
    float strength,
    float vibrationCyclesPerSecond,
    int frames,
    PunchCameraCallback callback,
    float distanceFalloff = -1f,
    string? uniqueIdentity = null
) : PunchCameraModifier(startPosition, direction, strength, vibrationCyclesPerSecond, frames, distanceFalloff, uniqueIdentity),
    ICameraModifier
{
    void ICameraModifier.Update(ref CameraInfo cameraInfo)
    {
        base.Update(ref cameraInfo);

        if (callback(this))
        {
            Finished = false;
        }
    }
}
