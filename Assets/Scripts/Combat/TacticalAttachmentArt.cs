using UnityEngine;
namespace Emberfall
{
    // Three independent body sockets. No gameplay state is stored here.
    internal sealed class TacticalAttachmentArt
    {
        internal static bool Enabled=true;
        private static readonly Mesh[] meshes=new Mesh[3];
        private static readonly Material[] materials=new Material[3];
        private static readonly bool[] attempted=new bool[3];
        private readonly MeshRenderer[] parts=new MeshRenderer[3];
        private static readonly string[] names={"SupplierBackpack","HuntBadge","SupportMantle"};
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {for(int i=0;i<3;i++){if(meshes[i]!=null)Object.Destroy(meshes[i]);if(materials[i]!=null)Object.Destroy(materials[i]);meshes[i]=null;materials[i]=null;attempted[i]=false;}Enabled=true;}
        internal static TacticalAttachmentArt Create(EnemyController enemy)
        {
            var result=new TacticalAttachmentArt();if(!Enabled||enemy==null)return result;
            MeshFilter body=null;
            foreach(var filter in enemy.GetComponentsInChildren<MeshFilter>(true))
                if(filter.sharedMesh!=null&&(filter.name=="Breastplate"||filter.name=="Slime Body"||filter.name=="Spirit Core")){body=filter;break;}
            if(body==null)return result; // Unmapped rigs retain the complete existing marker presentation.
            var bounds=body.sharedMesh.bounds;
            if(bounds.size.x<=0||bounds.size.y<=0||bounds.size.z<=0)return result;
            for(int i=0;i<3;i++)
            {
                if(!attempted[i])
                {
                    attempted[i]=true;var data=Resources.Load<TextAsset>("TacticalAttachments/"+names[i]);
                    meshes[i]=data==null?null:AuthoredActorMeshes.Decode(data.bytes,names[i]);
                    var shader=Shader.Find("Standard");
                    if(meshes[i]!=null&&shader!=null&&shader.isSupported)
                    {materials[i]=new Material(shader);materials[i].color=i==0?new Color(.92f,.64f,.12f):i==1?new Color(.93f,.25f,.12f):new Color(.12f,.72f,.84f);}
                }
                if(meshes[i]==null||materials[i]==null)continue;
                var obj=new GameObject("Tactical attachment / "+names[i]);obj.transform.SetParent(body.transform,false);
                // Coordinates are fractions of the real authored/procedural torso mesh; body scale,
                // crouch, knockdown, slime squash and boss scale remain inherited automatically.
                obj.transform.localScale=bounds.size;
                obj.transform.localPosition=bounds.center;
                if(i==2&&body.name=="Breastplate")
                {
                    // Keep the support fins above the ordinary/guardian shoulder armour.
                    obj.transform.localPosition+=new Vector3(0,bounds.size.y*.35f,0);
                    obj.transform.localScale=new Vector3(bounds.size.x*1.35f,bounds.size.y,bounds.size.z);
                }
                if(i==1)
                {
                    if(body.name=="Slime Body"||body.name=="Spirit Core")
                    {
                        // These bodies also carry the face: use a small lower-front seal.
                        obj.transform.localScale=bounds.size*.6f;
                        obj.transform.localPosition+=new Vector3(0,-bounds.size.y*.34f,0);
                    }
                    // Guardian armour is a separate outer shell. Seat the badge beyond that
                    // shell (including its raised crystal), measured in the moving body's frame.
                    float front=bounds.center.z+bounds.size.z*.5f;
                    foreach(var shell in enemy.GetComponentsInChildren<MeshFilter>(false))
                    {
                        if(shell.sharedMesh==null||(shell.name!="Guardian chest plate"&&shell.name!="Guardian ember crystal"))continue;
                        var shellRenderer=shell.GetComponent<MeshRenderer>();if(shellRenderer==null||!shellRenderer.enabled)continue;
                        var shellBounds=shell.sharedMesh.bounds;
                        for(int corner=0;corner<8;corner++)
                        {
                            var point=shellBounds.center+new Vector3((corner&1)==0?-shellBounds.size.x*.5f:shellBounds.size.x*.5f,(corner&2)==0?-shellBounds.size.y*.5f:shellBounds.size.y*.5f,(corner&4)==0?-shellBounds.size.z*.5f:shellBounds.size.z*.5f);
                            front=Mathf.Max(front,body.transform.InverseTransformPoint(shell.transform.TransformPoint(point)).z);
                        }
                    }
                    var badge=meshes[i].bounds;
                    float back=bounds.center.z+(badge.center.z-badge.size.z*.5f)*obj.transform.localScale.z;
                    obj.transform.localPosition+=new Vector3(0,0,Mathf.Max(0,front+bounds.size.z*.035f-back));
                }
                obj.AddComponent<MeshFilter>().sharedMesh=meshes[i];
                var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=materials[i];renderer.enabled=false;result.parts[i]=renderer;
            }
            return result;
        }
        internal bool Set(int slot,bool active)
        {var part=parts[slot];if(part==null)return false;part.enabled=Enabled&&active;return Enabled;}
        internal void Hide(){for(int i=0;i<3;i++)if(parts[i]!=null)parts[i].enabled=false;}
        internal void Destroy(){for(int i=0;i<3;i++)if(parts[i]!=null)Object.Destroy(parts[i].gameObject);}
    }
}
