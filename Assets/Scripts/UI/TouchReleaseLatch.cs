using System;
namespace Emberfall
{
    /// <summary>Consumes the initiating pointer through a UI transition; unrelated
    /// joystick fingers need not be lifted for a context interaction to finish.</summary>
    public sealed class TouchReleaseLatch
    {
        public const int AnyPointer=int.MinValue;
        public int Finger {get;private set;}=AnyPointer;
        private float until;
        private bool waiting;
        public void Block(float now,int finger=AnyPointer)
        {Finger=finger;until=Finite(now)?now+.4f:0;waiting=true;}
        public bool IsBlocked(float now,bool anyPointerHeld,bool triggeringPointerHeld)
        {
            if(!Finite(now))return waiting;
            if(now<until)return true;
            if(waiting&&(Finger==AnyPointer?anyPointerHeld:triggeringPointerHeld))return true;
            waiting=false;return false;
        }
        private static bool Finite(float v){return !float.IsNaN(v)&&!float.IsInfinity(v);}
    }
}
