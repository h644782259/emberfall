namespace Emberfall
{
    // A held pointer belongs to one coordinate system, even when a 180-degree
    // rotation keeps the same safe rectangle and pixel dimensions.
    public sealed class TouchViewportState
    {
        private bool initialized;
        private float width,height,x,y,safeWidth,safeHeight,scale,dpi;
        private int orientation;
        public bool Observe(float nextWidth,float nextHeight,float nextX,float nextY,float nextSafeWidth,float nextSafeHeight,float nextScale,float nextDpi,int nextOrientation)
        {
            bool changed=initialized&&(width!=nextWidth||height!=nextHeight||x!=nextX||y!=nextY||safeWidth!=nextSafeWidth||safeHeight!=nextSafeHeight||scale!=nextScale||dpi!=nextDpi||orientation!=nextOrientation);
            initialized=true;width=nextWidth;height=nextHeight;x=nextX;y=nextY;safeWidth=nextSafeWidth;safeHeight=nextSafeHeight;scale=nextScale;dpi=nextDpi;orientation=nextOrientation;
            return changed;
        }
    }
}
