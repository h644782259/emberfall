using System;
namespace Emberfall
{
    // Intentionally has no timer, resource, damage or healing state.
    public sealed class CompanionDirective<T> where T : class
    {
        private T target;
        public bool Recalling { get; private set; }
        public void Focus(T value) { target = value; Recalling = false; }
        public void Recall() { target = null; Recalling = true; }
        public void Clear() { target = null; Recalling = false; }
        public T Resolve(Func<T, bool> valid)
        { if (target != null && !valid(target)) target = null; return target; }
    }
}
