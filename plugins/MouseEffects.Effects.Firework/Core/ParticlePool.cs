using System;
using MouseEffects.Effects.Firework.Styles;

namespace MouseEffects.Effects.Firework.Core;

public class ParticlePool
{
    private FireworkParticle[] _particles;
    private int _nextIndex;

    public int Capacity => _particles.Length;

    public ParticlePool(int capacity)
    {
        _particles = new FireworkParticle[capacity];
        for (int i = 0; i < capacity; i++)
            _particles[i] = new FireworkParticle { Life = 0f };
    }

    public void Resize(int newCapacity)
    {
        if (newCapacity == _particles.Length) return;

        var newParticles = new FireworkParticle[newCapacity];
        int copyCount = Math.Min(_particles.Length, newCapacity);

        for (int i = 0; i < copyCount; i++)
            newParticles[i] = _particles[i];

        // Initialize new slots if growing
        for (int i = copyCount; i < newCapacity; i++)
            newParticles[i] = new FireworkParticle { Life = 0f };

        _particles = newParticles;
        _nextIndex = _nextIndex % newCapacity;
    }

    public ref FireworkParticle Spawn(in FireworkParticle template)
    {
        int startIndex = _nextIndex;
        do
        {
            ref var slot = ref _particles[_nextIndex];
            _nextIndex = (_nextIndex + 1) % _particles.Length;

            if (slot.Life <= 0f)
            {
                slot = template;
                return ref slot;
            }
        } while (_nextIndex != startIndex);

        // Pool full, overwrite oldest
        ref var oldest = ref _particles[_nextIndex];
        oldest = template;
        _nextIndex = (_nextIndex + 1) % _particles.Length;
        return ref oldest;
    }

    public void ForEachAlive(Action<int, FireworkParticle> action)
    {
        for (int i = 0; i < _particles.Length; i++)
            if (_particles[i].Life > 0)
                action(i, _particles[i]);
    }

    public void UpdateAll(float dt, float gravity, float drag, IFireworkStyle style, float time)
    {
        for (int i = 0; i < _particles.Length; i++)
        {
            ref var p = ref _particles[i];
            if (p.Life <= 0f) continue;

            p.Life -= dt;
            if (p.Life <= 0f) continue;

            // Common physics
            p.Position += p.Velocity * dt;
            p.Velocity.Y += gravity * dt;
            p.Velocity *= drag;

            // Style-specific update
            style.UpdateParticle(ref p, dt, time);
        }
    }

    public int CopyToGpu(ParticleGPU[] gpuBuffer, IFireworkStyle style, int maxCount)
    {
        int count = 0;
        for (int i = 0; i < _particles.Length && count < maxCount; i++)
        {
            ref var p = ref _particles[i];
            if (p.Life <= 0f) continue;

            gpuBuffer[count] = new ParticleGPU
            {
                Position = p.Position,
                Velocity = p.Velocity,
                Color = p.Color,
                Size = p.Size,
                Life = p.Life,
                MaxLife = p.MaxLife,
                StyleData1 = p.StyleData1,
                StyleData2 = p.StyleData2,
                StyleData3 = p.StyleData3,
                StyleFlags = (uint)p.StyleId
            };

            style.FillStyleData(ref gpuBuffer[count], in p);
            count++;
        }
        return count;
    }

    public ref FireworkParticle GetParticle(int index) => ref _particles[index];

    public int CountAlive()
    {
        int count = 0;
        for (int i = 0; i < _particles.Length; i++)
            if (_particles[i].Life > 0)
                count++;
        return count;
    }
}
