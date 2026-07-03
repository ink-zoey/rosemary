namespace Rosemary.Core;

public interface IUpdatingParticle
{
    bool Update();
}

public sealed class UpdatingParticleHandler<T>(int count) : ParticleHandler<T>(count)
    where T : struct, IUpdatingParticle
{
    public void Update()
    {
        foreach (var index in this)
        {
            ref var particle = ref this[index];

            if (!particle.Update())
            {
                Deactivate(index);
            }
        }
    }
}
