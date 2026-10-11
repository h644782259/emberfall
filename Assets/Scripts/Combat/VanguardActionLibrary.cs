using System;
using System.IO;
using UnityEngine;
namespace Emberfall
{
    internal enum VanguardArtPose { Idle, Forward, Backward, Left, Right, TurnLeft, TurnRight, Basic, Skill, Jump, Landing, Hit, Death }
    // One immutable, all-or-nothing library. An incomplete import keeps the complete old rig.
    internal sealed class VanguardActionLibrary
    {
        private const int Clips=13, Bones=9, Samples=33;
        private readonly Vector3[] values;
        private VanguardActionLibrary(Vector3[] values) { this.values=values; }
        private static bool attempted;
        private static VanguardActionLibrary cached;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset(){attempted=false;cached=null;}
        internal static VanguardActionLibrary Load()
        {
            if(attempted)return cached;
            attempted=true;
            var source=Resources.Load<TextAsset>("VanguardActions/Swordguard");
            cached=source==null?null:Decode(source.bytes);return cached;
        }
        internal static VanguardActionLibrary Decode(byte[] bytes)
        {
            if(bytes==null || bytes.Length!=16+Clips*Bones*Samples*12)return null;
            try
            {
                using(var stream=new MemoryStream(bytes,false))using(var reader=new BinaryReader(stream))
                {
                    if(reader.ReadUInt32()!=0x31414756 || reader.ReadInt32()!=Clips || reader.ReadInt32()!=Bones || reader.ReadInt32()!=Samples)return null;
                    var data=new Vector3[Clips*Bones*Samples];
                    for(int i=0;i<data.Length;i++)
                    {
                        float x=reader.ReadSingle(),y=reader.ReadSingle(),z=reader.ReadSingle();
                        if(!Finite(x)||!Finite(y)||!Finite(z)||Math.Abs(x)>40||Math.Abs(y)>40||Math.Abs(z)>40)return null;
                        data[i]=new Vector3(x,y,z);
                    }
                    return new VanguardActionLibrary(data);
                }
            }
            catch(IOException){return null;}
        }
        private static bool Finite(float value){return !float.IsNaN(value)&&!float.IsInfinity(value);}
        internal Vector3 Sample(VanguardArtPose clip,int bone,float progress)
        {
            if((int)clip<0||(int)clip>=Clips||bone<0||bone>=Bones||!Finite(progress))return Vector3.zero;
            float position=Mathf.Clamp01(progress)*(Samples-1);int a=(int)position,b=Math.Min(a+1,Samples-1);
            return Vector3.Lerp(values[((int)clip*Samples+a)*Bones+bone],values[((int)clip*Samples+b)*Bones+bone],position-a);
        }
    }
}
