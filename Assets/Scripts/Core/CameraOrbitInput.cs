using UnityEngine;

namespace Emberfall
{
    /// <summary>One right-button gesture owns either an orbit drag or a release click.</summary>
    public sealed class CameraOrbitInput
    {
        public const float DragThresholdPixels = 6f;
        public bool IsHeld { get; private set; }
        public bool IsDragging { get; private set; }
        public bool Clicked { get; private set; }
        public Vector2 DragDelta { get; private set; }
        private Vector2 origin, previous;

        public void Advance(Vector2 position, bool pressed, bool held, bool released, bool canStart, bool canContinue)
        {
            Clicked = false;
            DragDelta = Vector2.zero;
            if (!canContinue) { Reset(); return; }
            if (pressed)
            {
                Reset();
                if (!canStart) return;
                IsHeld = true;
                origin = previous = position;
            }
            if (!IsHeld) return;
            Vector2 delta = position - previous;
            previous = position;
            if (!IsDragging && (position - origin).sqrMagnitude > DragThresholdPixels * DragThresholdPixels)
            {
                IsDragging = true;
                DragDelta = position - origin;
            }
            else if (IsDragging) DragDelta = delta;
            if (released)
            {
                Clicked = !IsDragging && canStart;
                IsHeld = IsDragging = false;
            }
            else if (!held && !pressed) Reset();
        }

        public void Reset()
        {
            IsHeld = IsDragging = Clicked = false;
            DragDelta = Vector2.zero;
        }
    }
}
