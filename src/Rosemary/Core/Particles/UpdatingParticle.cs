namespace Rosemary.Core;

public interface IUpdatingParticle
{
    bool Update();
}

public class UpdatingParticleHandler<T>(int count) : ParticleHandler<T>(count)
    where T : struct, IUpdatingParticle
{
    public override int ActiveParticleCount => count;

    private int count;

    public void Update()
    {
        foreach (var index in this)
        {
            ref var particle = ref this[index];

            if (!particle.Update())
            {
                Deactivate(index);

                continue;
            }

            count++;
        }
    }
}
