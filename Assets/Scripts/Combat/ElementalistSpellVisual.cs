using UnityEngine;
using UnityEngine.Rendering;
namespace Emberfall
{
    // Presentation only. Producers own its clock, lifetime and all gameplay.
    internal sealed class ElementalistSpellVisual
    {
        internal enum Kind { Laser, Pressure, Lift, Meteor, Frost }
        private readonly Transform root,core,volume;
        private readonly float coreRadius;
        // Slightly below ordinary enemy head height; presentation only.
        private const float PressureClearance=2.52f;
        private readonly Transform[] debris;
        private readonly Vector3[] sizes;
        private readonly Material matter,cloud,sun;
        private readonly Kind kind;
        private readonly float radius;
        internal static Material IceMaterial()
        {
            var material=new Material(Resources.Load<Shader>("HeroSkillBody"));
            material.SetColor("_Tint",new Color(.18f,.57f,.85f));
            SkillBodyMaterialLibrary.Apply(material,SkillBodyMaterialLibrary.Matter.Ice);
            return material;
        }
        internal ElementalistSpellVisual(Transform parent,Kind kind,float radius)
        {
            this.kind=kind;this.radius=radius;
            root=new GameObject("Sculpted elemental matter / "+kind).transform;root.SetParent(parent,false);
            cloud=new Material(Resources.Load<Shader>("ElementalistVolume"));cloud.SetTexture("_FlowNoise",SkillFlowVolume.FlowNoise);
            cloud.SetFloat("_Kind",(float)kind);cloud.SetFloat("_Steps",Application.isMobilePlatform||EffectPreferences.ReducedEffects?8:16);
            cloud.SetFloat("_Density",1);cloud.SetFloat("_AlphaLimit",kind==Kind.Laser?.28f:kind==Kind.Meteor?.8f:.24f);
            cloud.SetColor("_Tint",new Color(.12f,.65f,1));
            float height=kind==Kind.Laser?14:kind==Kind.Meteor?5:kind==Kind.Frost?1.1f:kind==Kind.Pressure?PressureClearance+radius*1.8f:6;
            volume=Part("Turbulent volumetric mantle",ProceduralVisuals.Shape(PrimitiveType.Cube),cloud,new Vector3(radius*2,height,radius*2));
            volume.localPosition=Vector3.up*height*.5f;
            if(!Application.isMobilePlatform&&Camera.main!=null)Camera.main.depthTextureMode|=DepthTextureMode.Depth;
            if(kind==Kind.Laser)
            {
                sun=new Material(Resources.Load<Shader>("LaserFootprint"));
                var disc=Part("Solar footprint and luminous corona",ProceduralVisuals.Shape(PrimitiveType.Quad),sun,Vector3.one*radius*2f);
                disc.localRotation=Quaternion.Euler(90,0,0);disc.localPosition=Vector3.up*.07f;
            }
            if(kind==Kind.Laser||kind==Kind.Meteor||kind==Kind.Frost){debris=new Transform[0];sizes=new Vector3[0];Draw(0,0);return;}
            matter=new Material(Resources.Load<Shader>("ElementalistMatter"));matter.SetTexture("_FlowNoise",SkillFlowVolume.FlowNoise);
            var basalt=Resources.Load<Texture2D>("WorldArt/GravityBasalt");if(basalt!=null)matter.SetTexture("_RockTex",basalt);
            matter.SetColor("_Tint",kind==Kind.Laser?new Color(.08f,.6f,1):new Color(.58f,.16f,1));
            if(kind==Kind.Pressure)
            {
                core=Part("Fractured rotating gravity planet",HeroSkillBodyAssets.GravityPlanet,matter,Vector3.one*radius*1.35f);
                // A rotationally invariant mesh radius keeps every rock above the enemies.
                foreach(var vertex in HeroSkillBodyAssets.GravityPlanet.vertices)coreRadius=Mathf.Max(coreRadius,vertex.magnitude*radius*1.35f);
                core.localPosition=Vector3.up*(PressureClearance+coreRadius);
            }
            if(kind==Kind.Lift)
            {
                core=Part("Lifted fractured keystone",HeroSkillBodyAssets.Rock??ProceduralVisuals.WeatheredRock,matter,new Vector3(1.4f,.8f,1.2f));
                core.localPosition=new Vector3(radius*.55f,.15f,0);
            }
            int count=EffectPreferences.ReducedEffects?8:14;
            debris=new Transform[count];sizes=new Vector3[count];
            for(int i=0;i<count;i++)
            {
                sizes[i]=Vector3.one*(.24f+(i%4)*.15f);
                debris[i]=Part("Sculpted orbiting basalt fragment",HeroSkillBodyAssets.Rock??ProceduralVisuals.WeatheredRock,matter,sizes[i]);
            }
            // A broken-ground relief anchors the magic in the scene, without adding collision.
            if(kind!=Kind.Pressure)Part("Fractured ground relief",HeroSkillBodyAssets.GroundBreak,matter,new Vector3(radius*1.4f,.65f,radius*1.4f));
            Draw(0,0);
        }
        private Transform Part(string name,Mesh mesh,Material material,Vector3 scale)
        {
            var part=new GameObject(name).transform;part.SetParent(root,false);part.localScale=scale;
            part.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=part.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=material==cloud?ShadowCastingMode.Off:ShadowCastingMode.On;renderer.receiveShadows=false;MobileSceneDepth.Track(renderer);
            return part;
        }
        internal void Draw(float age,float opacity)
        {
            if(root==null)return;
            if(sun!=null){sun.SetFloat("_Phase",age);sun.SetFloat("_Opacity",opacity);}
            cloud.SetFloat("_Clock",age);cloud.SetFloat("_Opacity",opacity);
            if(matter!=null){matter.SetFloat("_Phase",age);matter.SetFloat("_Opacity",opacity);}
            if(core!=null)core.localRotation=Quaternion.Euler(age*37,age*51,age*23);
            if(core!=null&&kind==Kind.Lift){float t=Mathf.Clamp01((age-.15f)/1.65f);core.localPosition=new Vector3(radius*.55f,.2f+Mathf.Sin(t*Mathf.PI)*3.2f,0);}
            for(int i=0;i<debris.Length;i++)
            {
                float angle=i*2.399963f+age*(kind==Kind.Lift?.2f:.8f),r=radius*(.57f+.3f*(i%3)/2f);
                float y;
                if(kind==Kind.Lift)
                {
                    // Shared lift/fall landmarks: launch .15, touchdown 1.8 seconds.
                    float t=Mathf.Clamp01((age-.15f)/1.65f);y=.12f+Mathf.Sin(t*Mathf.PI)*(2.4f+(i%3)*.35f);
                }
                else if(kind==Kind.Pressure)y=PressureClearance+.6f+Mathf.Repeat(i*.17f+age*.23f,1)*coreRadius*1.5f;
                else y=.1f+Mathf.Repeat(i*.19f+age*.35f,1)*1.8f;
                debris[i].localPosition=new Vector3(Mathf.Cos(angle)*r,y,Mathf.Sin(angle)*r);
                debris[i].localRotation=Quaternion.Euler(i*37+age*43,i*53+age*31,age*57);
                debris[i].localScale=Vector3.Scale(sizes[i],new Vector3(1,.7f,1.2f))*Mathf.Min(1,opacity*3);
            }
        }
        internal void Dispose()
        {
            if(root!=null){root.gameObject.SetActive(false);Object.Destroy(root.gameObject);}
            if(matter!=null)Object.Destroy(matter);if(cloud!=null)Object.Destroy(cloud);if(sun!=null)Object.Destroy(sun);
        }
    }
}
