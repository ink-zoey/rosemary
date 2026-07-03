using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace Rosemary.Core;

public class ParticleHandler<T>(int max) : IEnumerable<int>
    where T : struct
{
    protected const int BITS_PER_CHUNK = sizeof(ulong) * 8;

    public struct ActiveParticleEnumerator(ulong[] mask) : IEnumerator<int>
    {
        private int maskIterator;
        private ulong bits;

        public int Current { get; private set; }

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (bits != 0)
            {
                var bitIndex = BitOperations.TrailingZeroCount(bits);
                Current = maskIterator * BITS_PER_CHUNK + bitIndex;

                bits &= bits - 1;

                return true;
            }

            maskIterator++;

            if (maskIterator < mask.Length)
            {
                bits = mask[maskIterator];

                return true;
            }

            return false;
        }

        public void Reset()
        {
            maskIterator = 0;
            bits = mask[0];
        }

        void IDisposable.Dispose()
        { }
    }

    public readonly T[] Particles = new T[max];
    protected readonly ulong[] ParticleMask = new ulong[(int)Math.Ceiling((double)max / BITS_PER_CHUNK)];

    public ref T this[int index] => ref Particles[index];

    public int GetFirstInactive()
    {
        for (var i = 0; i < ParticleMask.Length; i++)
        {
            var offset = BitOperations.TrailingZeroCount(~ParticleMask[i]);

            var allBitsAreOccupied = offset == BITS_PER_CHUNK;

            if (allBitsAreOccupied)
            {
                continue;
            }

            return offset + (i * BITS_PER_CHUNK);
        }

        return -1;
    }

    public bool Add(T particle)
    {
        var index = GetFirstInactive();

        if (index < 0)
        {
            return false;
        }

        Particles[index] = particle;

        return true;
    }

    public void Deactivate(int index)
    {
        var maskIndex = (int)Math.Floor((float)index / BITS_PER_CHUNK);
        var bitIndex = index % BITS_PER_CHUNK;

        ParticleMask[maskIndex] ^= 1uL << bitIndex;
    }

    public IEnumerator<int> GetEnumerator()
    {
        return new ActiveParticleEnumerator(ParticleMask);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public static ParticleHandler<T> operator +(ParticleHandler<T> handler, T particle)
    {
        handler.Add(particle);

        return handler;
    }
}
