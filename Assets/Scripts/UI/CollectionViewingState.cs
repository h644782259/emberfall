namespace Emberfall
{
    // Two trial slots and three view angles; presentation only, never writes a profile.
    public sealed class CollectionViewingState
    {
        private readonly FashionData[] trials=new FashionData[2];
        private readonly float[] angles={20,20,160};
        public CollectionPreviewComposition Mode {get;private set;}
        public float Yaw {get{return angles[(int)Mode];}}
        public FashionData Trial(FashionSlot slot){return trials[(int)slot];}
        public void TryOn(FashionData item){if(item!=null)trials[(int)item.slot]=item;}
        public void View(CollectionPreviewComposition mode){if((int)mode>=0&&(int)mode<3)Mode=mode;}
        public void Rotate(float delta){float value=(Yaw+delta)%360;angles[(int)Mode]=value<0?value+360:value;}
        public void Restore(){trials[0]=trials[1]=null;}
        public void Reset(){Restore();Mode=CollectionPreviewComposition.Full;angles[0]=angles[1]=20;angles[2]=160;}
    }
}
