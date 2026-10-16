using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Emberfall
{
    // Presentation follows the existing travel endpoint. It never owns a trigger,
    // adds navigation obstacles or changes a player's interaction distance.
    public sealed class WorldPortal3DVisual : MonoBehaviour
    {
        public Vector3 InteractionCenter { get; private set; }
        public float InteractionRadius { get; private set; }
        public bool IsOpen { get; private set; }
        public float PassageDepth { get { return 1.12f * (presentation!=null?presentation.lossyScale.z:transform.lossyScale.z); } }
        private Transform presentation, medium, grains;
        private Light contactLight;
        private readonly List<Material> materials = new List<Material>();
        private Mesh grainMesh;
        private Vector3[] grainVertices;
        private readonly Vector3[] positions = new Vector3[18], velocities = new Vector3[18];
        private readonly float[] ages = new float[18];
        private static readonly Vector3[] grainCorners = { Vector3.up, Vector3.right, Vector3.forward, Vector3.left, Vector3.back, Vector3.down };
        private static readonly int[] grainFaces = { 0,2,1,0,3,2,0,4,3,0,1,4,5,1,2,5,2,3,5,3,4,5,4,1 };

        internal static WorldPortal3DVisual Create(Transform parent, WorldResources resources, Vector3 localCenter, float radius, bool open, string name,float visualClearance=-1)
        {
            var root = new GameObject(name); root.transform.SetParent(parent, false); root.transform.localPosition = localCenter;
            var gate = root.AddComponent<WorldPortal3DVisual>(); gate.InteractionCenter = root.transform.position; gate.InteractionRadius = radius;
            gate.Build(resources, (visualClearance<0?radius:visualClearance) < 1.5f ? .70f : 1); gate.SetOpen(open); return gate;
        }

        private void Build(WorldResources resources, float size)
        {
            presentation = new GameObject("Grounded worn portal masonry").transform; presentation.SetParent(transform, false); presentation.localScale = Vector3.one * size;
            Material stone = Own(resources.Material(new Color(.31f,.35f,.34f), false, VisualSurface.Stone));
            Material dark = Own(resources.Material(new Color(.21f,.26f,.25f), false, VisualSurface.Stone));
            // Individual beveled stones carry actual joints, chips and a deep jamb.
            for(int side=-1;side<=1;side+=2)
            {
                Stone(resources, "Portal load-bearing worn footing", new Vector3(side*1.91f,.16f,0), new Vector3(.79f,.31f,1.28f), stone);
                for(int course=0;course<4;course++)
                {
                    GameObject block=Stone(resources,"Portal weathered wall course "+course,new Vector3(side*1.83f,.47f+course*.58f,.02f),new Vector3(.55f,.555f,1.12f),course%2==0?stone:dark);
                    block.transform.localRotation=Quaternion.Euler(0,side*(course%2==0?1.8f:-1.1f),side*.6f);
                }
            }
            const int pieces=11;
            for(int i=0;i<pieces;i++)
            {
                float angle=(i+.5f)*Mathf.PI/pieces;
                GameObject arch=Stone(resources,"Portal hand-cut arch voussoir "+i,new Vector3(Mathf.Cos(angle)*1.83f,2.35f+Mathf.Sin(angle)*1.73f,0),new Vector3(.55f,.59f,1.12f),i==pieces/2?dark:stone);
                arch.transform.localRotation=Quaternion.Euler(0,0,angle*Mathf.Rad2Deg-90);
            }
            // A broad shallow worn sill remains flush with feet; no raised step is
            // introduced into the travel route and the interior has 1.12 m depth.
            Stone(resources,"Portal worn entry sill footing",new Vector3(0,.001f,0),new Vector3(3.45f,.016f,1.35f),dark);
            Shader shader=Resources.Load<Shader>("WorldArt/PortalVolume");
            if(shader==null)throw new System.InvalidOperationException("PortalVolume shader missing.");
            Material fog=new Material(shader){name="Owned deep flowing travel medium"};materials.Add(fog);
            if(Application.isMobilePlatform && SystemInfo.supports3DTextures)
            {
                fog.SetTexture("_FlowNoise",SkillFlowVolume.FlowNoise);
                fog.EnableKeyword("EMBER_PORTAL_TEXTURE_NOISE");
            }
            fog.SetColor("_Color",new Color(.25f,.63f,.53f,1));fog.SetColor("_DeepColor",new Color(.045f,.095f,.12f,1));
            medium=new GameObject("Filled volumetric portal passage").transform;medium.SetParent(presentation,false);
            Mesh volume=VolumeMesh();resources.Own(volume);medium.gameObject.AddComponent<MeshFilter>().sharedMesh=volume;
            var renderer=medium.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=fog;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            grains=new GameObject("Travel grains accelerating into doorway").transform;grains.SetParent(presentation,false);
            grainMesh=new Mesh{name="Merged solid portal dust grains"};grainMesh.MarkDynamic();resources.Own(grainMesh);
            grainVertices=new Vector3[positions.Length*grainCorners.Length];var triangles=new int[positions.Length*grainFaces.Length];
            for(int i=0;i<positions.Length;i++){ResetGrain(i,true);for(int j=0;j<grainFaces.Length;j++)triangles[i*grainFaces.Length+j]=i*grainCorners.Length+grainFaces[j];}
            grainMesh.vertices=grainVertices;grainMesh.triangles=triangles;
            grains.gameObject.AddComponent<MeshFilter>().sharedMesh=grainMesh;var dust=grains.gameObject.AddComponent<MeshRenderer>();
            Material grain=Own(resources.Material(new Color(.38f,.67f,.53f),true,VisualSurface.Crystal));
            if(grain.HasProperty("_EmissionColor"))grain.SetColor("_EmissionColor",new Color(.10f,.23f,.15f));
            dust.sharedMaterial=grain;dust.shadowCastingMode=ShadowCastingMode.Off;
            var glow=new GameObject("Portal grounded contact light");glow.transform.SetParent(presentation,false);glow.transform.localPosition=new Vector3(0,.85f,-.25f);
            contactLight=glow.AddComponent<Light>();contactLight.type=LightType.Point;contactLight.color=new Color(.45f,.73f,.59f);contactLight.intensity=.86f;contactLight.range=4.4f;contactLight.shadows=LightShadows.None;
            SnapToGround();UpdateGrains(0);
        }

        private Material Own(Material source)
        {
            Material copy=new Material(source);materials.Add(copy);if(copy.HasProperty("_TerrainFollow"))copy.SetFloat("_TerrainFollow",0);return copy;
        }
        private GameObject Stone(WorldResources resources,string name,Vector3 at,Vector3 size,Material material)
        {
            GameObject part=ProceduralVisuals.Create(name,PrimitiveType.Cube,material);NaturalSceneryMeshLibrary.Apply(part,name,PrimitiveType.Cube,size,resources);
            part.transform.SetParent(presentation,false);part.transform.localPosition=at;part.transform.localScale=size;return part;
        }
        private static Mesh VolumeMesh()
        {
            // Closed curved lens, including front/back and its perimeter. The
            // shader samples a 3D medium through this volume, never a flat quad.
            const int rows=28,columns=24;int face=(rows+1)*(columns+1);var vertices=new Vector3[face*2];var triangles=new List<int>();
            for(int side=0;side<2;side++)for(int y=0;y<=rows;y++)for(int x=0;x<=columns;x++)
            {
                float height=.06f+y/(float)rows*3.91f,half=height<2.35f?1.53f:1.53f*Mathf.Sqrt(Mathf.Max(.003f,1-Mathf.Pow((height-2.35f)/1.65f,2)));
                float u=x/(float)columns*2-1,z=(.11f+.38f*Mathf.Sqrt(Mathf.Max(0,1-u*u))*Mathf.Sin(y/(float)rows*Mathf.PI))*(side==0?-1:1);
                int n=side*face+y*(columns+1)+x;vertices[n]=new Vector3(u*half,height,z);
                if(y<rows&&x<columns){int b=n+columns+1;if(side==0)triangles.AddRange(new[]{n,b,n+1,n+1,b,b+1});else triangles.AddRange(new[]{n,n+1,b,n+1,b+1,b});}
            }
            var edge=new List<int>();for(int x=0;x<=columns;x++)edge.Add(x);for(int y=1;y<=rows;y++)edge.Add(y*(columns+1)+columns);for(int x=columns-1;x>=0;x--)edge.Add(rows*(columns+1)+x);for(int y=rows-1;y>0;y--)edge.Add(y*(columns+1));
            for(int i=0;i<edge.Count;i++){int a=edge[i],b=edge[(i+1)%edge.Count];triangles.AddRange(new[]{a,b,a+face,b,b+face,a+face});}
            var mesh=new Mesh{name="Closed bowed 3D portal medium",vertices=vertices,triangles=triangles.ToArray()};mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        public void SetOpen(bool open)
        {
            IsOpen=open;if(medium!=null)medium.gameObject.SetActive(open);if(grains!=null)grains.gameObject.SetActive(open);if(contactLight!=null)contactLight.enabled=open;
        }
        private void Start(){SnapToGround();}
        private void SnapToGround()
        {
            if(presentation==null)return;Vector3 at=transform.position;float ground=WorldTraversal.SurfaceHeight(at,.1f);
            presentation.localPosition=Vector3.up*((ground-at.y)/Mathf.Max(.001f,transform.lossyScale.y));
        }
        private void Update()
        {
            if(!IsOpen||Time.deltaTime<=0)return;var game=GameSession.Instance;if(game!=null&&game.InputBlocked)return;
            UpdateGrains(Mathf.Min(Time.deltaTime,.033f));if(contactLight!=null)contactLight.intensity=.83f+Mathf.Sin(Time.time*1.2f)*.045f;
        }
        private void ResetGrain(int i,bool stagger)
        {
            float phase=i*2.39996f+(stagger?0:Time.time*.31f);positions[i]=new Vector3(Mathf.Sin(phase)*1.70f,.28f+(i%7)*.46f,-1.05f-(i%3)*.13f);
            velocities[i]=new Vector3(-positions[i].x*.08f,.09f,.10f);ages[i]=stagger?i*.055f:0;
        }
        private void UpdateGrains(float dt)
        {
            Vector3 sink=new Vector3(0,1.95f,.25f);
            for(int i=0;i<positions.Length;i++)
            {
                ages[i]+=dt;Vector3 toward=sink-positions[i];Vector3 swirl=Vector3.Cross(Vector3.forward,toward)*.23f;
                velocities[i]+=(toward*.82f+swirl-velocities[i]*.28f)*dt;positions[i]+=velocities[i]*dt;
                if(ages[i]>2.65f||toward.sqrMagnitude<.08f)ResetGrain(i,false);
                float size=(.014f+(i%3)*.005f)*Mathf.Clamp01(ages[i]*4)*Mathf.Clamp01((2.7f-ages[i])*3);
                for(int j=0;j<grainCorners.Length;j++)grainVertices[i*grainCorners.Length+j]=positions[i]+grainCorners[j]*size;
            }
            grainMesh.vertices=grainVertices;grainMesh.RecalculateNormals();grainMesh.RecalculateBounds();
        }
        private void OnDestroy(){foreach(Material material in materials)if(material!=null)Destroy(material);}
    }

    // The existing room marker's active state is the sole unlock authority.
    internal sealed class WorldPortalUnlockVisual : MonoBehaviour
    {
        private WorldPortal3DVisual gate;
        internal void Bind(WorldPortal3DVisual target){gate=target;gate.SetOpen(gameObject.activeInHierarchy);}
        private void OnEnable(){if(gate!=null)gate.SetOpen(true);}
        private void OnDisable(){if(gate!=null)gate.SetOpen(false);}
    }
}
