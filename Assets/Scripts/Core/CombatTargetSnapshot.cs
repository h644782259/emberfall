using System;
using System.Collections.Generic;

namespace Emberfall
{
    // Immediate impacts need an isolated admission boundary even when damage
    // callbacks remove enemies or recursively resolve another skill. Buffers are
    // retained after use; each active/nested impact owns a different lease.
    internal static class CombatTargetSnapshot<T>
    {
        internal sealed class Buffer
        {
            internal readonly List<T> Targets = new List<T>();
            internal bool InUse;
            internal ulong Generation;
        }

        private static readonly List<Buffer> buffers = new List<Buffer>();

        internal static Lease Capture(IList<T> current)
        {
            Buffer buffer = null;
            for (int i = 0; i < buffers.Count; i++)
                if (!buffers[i].InUse) { buffer = buffers[i]; break; }
            if (buffer == null) { buffer = new Buffer(); buffers.Add(buffer); }
            buffer.InUse = true;
            unchecked { buffer.Generation++; }
            try
            {
                int count = current == null ? 0 : current.Count;
                if (buffer.Targets.Capacity < count) buffer.Targets.Capacity = count;
                for (int i = 0; i < count; i++) buffer.Targets.Add(current[i]);
                return new Lease(buffer);
            }
            catch
            {
                buffer.Targets.Clear(); buffer.InUse = false;
                throw;
            }
        }

        internal readonly struct Lease : IDisposable
        {
            private readonly Buffer buffer;
            private readonly ulong generation;
            internal Lease(Buffer value) { buffer = value; generation = value.Generation; }
            internal int Count { get { EnsureActive(); return buffer.Targets.Count; } }
            internal T this[int index] { get { EnsureActive(); return buffer.Targets[index]; } }
            private void EnsureActive()
            {
                if (buffer == null || !buffer.InUse || buffer.Generation != generation)
                    throw new ObjectDisposedException("Combat target snapshot");
            }
            public void Dispose()
            {
                if (buffer == null || !buffer.InUse || buffer.Generation != generation) return;
                buffer.Targets.Clear(); buffer.InUse = false;
            }
        }
    }

    // Chain searches admit live targets at each bounce, so their visited set is
    // separate from an impact snapshot. Nested casts cannot clear an outer set.
    internal static class CombatTargetSet<T>
    {
        internal sealed class Buffer
        {
            internal readonly HashSet<T> Targets = new HashSet<T>();
            internal bool InUse;
            internal ulong Generation;
        }
        private static readonly List<Buffer> buffers = new List<Buffer>();
        internal static Lease Capture()
        {
            Buffer buffer = null;
            for (int i = 0; i < buffers.Count; i++)
                if (!buffers[i].InUse) { buffer = buffers[i]; break; }
            if (buffer == null) { buffer = new Buffer(); buffers.Add(buffer); }
            buffer.InUse = true;
            unchecked { buffer.Generation++; }
            return new Lease(buffer);
        }
        internal readonly struct Lease : IDisposable
        {
            private readonly Buffer buffer;
            private readonly ulong generation;
            internal Lease(Buffer value) { buffer = value; generation = value.Generation; }
            internal int Count { get { EnsureActive(); return buffer.Targets.Count; } }
            internal bool Contains(T target) { EnsureActive(); return buffer.Targets.Contains(target); }
            internal bool Add(T target) { EnsureActive(); return buffer.Targets.Add(target); }
            private void EnsureActive()
            {
                if (buffer == null || !buffer.InUse || buffer.Generation != generation)
                    throw new ObjectDisposedException("Combat target set");
            }
            public void Dispose()
            {
                if (buffer == null || !buffer.InUse || buffer.Generation != generation) return;
                buffer.Targets.Clear(); buffer.InUse = false;
            }
        }
    }
}
