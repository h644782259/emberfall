using UnityEngine;
namespace Emberfall
{
    internal sealed partial class SkillPerformanceVfx
    {
        private bool collapseReleased;
        private Vector3[] collapseVelocity;
        internal static void CollapseGravity(PlayerController hero,Vector3 at,float radius)
        { Sustain(hero,at,radius,1.25f,SkillPerformanceBody.GravityCollapse); }
        private void BuildGravityCollapse()
        {
            int count=EffectPreferences.ReducedEffects?6:10;
            parts=new Transform[count];surfaces=new Renderer[count];collapseVelocity=new Vector3[count];
            Add(0,lensQuad,new Vector3(size*1.4f,size*1.4f,1),Vector3.up*1.45f,Quaternion.identity,new Color(1,.35f,.04f));
            Mesh rock=AuthoredProjectileMeshes.Load("MeteorRock")??orb;
            for(int i=1;i<count;i++)
            {
                float angle=i*2.399963f;
                Vector3 radial=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                float width=.22f+(i%3)*.1f;
                Add(i,rock,new Vector3(width,.3f+(i%2)*.18f,width*.8f),radial*size*.23f+Vector3.up*(.9f+(i%3)*.22f),Quaternion.Euler(i*39,i*67,23),new Color(.36f,.25f,.18f));
            }
            particles=ElementalCombatVfx.Create(transform,"Gravity collapse embers",ElementalCombatVfx.Element.Fire,0,.28f);
            if(particles!=null)
            {
                particles.transform.localPosition=Vector3.up*1.45f;
                var main=particles.main;main.startSize=new ParticleSystem.MinMaxCurve(.035f,.12f);main.startLifetime=new ParticleSystem.MinMaxCurve(.3f,.65f);main.startSpeed=new ParticleSystem.MinMaxCurve(1.2f,3.4f);main.gravityModifier=.18f;main.maxParticles=EffectPreferences.ReducedEffects?18:36;
                particles.Play();
            }
            if(!EffectPreferences.ReducedEffects&&lights<3)
            {glow=gameObject.AddComponent<Light>();glow.type=LightType.Point;glow.color=new Color(1,.35f,.045f);glow.range=4;glow.intensity=.5f;glow.shadows=LightShadows.None;ownsLight=true;lights++;}
        }
        // Swept movement uses the same authored solid obstacles as actors, with
        // extra PhysX surface contact where scene colliders exist. No combat hits.
        private static void AdvanceCollapseFragment(Transform piece,ref Vector3 velocity,float delta)
        {
            Vector3 position=piece.position;
            PhysicalSkillMotion.Advance(ref position,ref velocity,.12f,delta,.28f,.65f);
            piece.position=position;
        }
        private void AnimateGravityCollapse()
        {
            float contraction=Mathf.SmoothStep(1,.035f,Mathf.Clamp01(age/.36f));
            if(Camera.main!=null)parts[0].rotation=Quaternion.LookRotation(parts[0].position-Camera.main.transform.position,Camera.main.transform.up);
            parts[0].localScale=new Vector3(size*1.4f*contraction,size*1.4f*contraction,1);
            surfaces[0].GetPropertyBlock(block);block.SetFloat("_Opacity",1-Mathf.SmoothStep(0,1,Mathf.Clamp01((age-.28f)/.12f)));block.SetFloat("_Phase",age*3);surfaces[0].SetPropertyBlock(block);
            if(age>=.34f&&!collapseReleased)
            {
                collapseReleased=true;if(particles!=null)particles.Emit(EffectPreferences.ReducedEffects?14:30);
                for(int i=1;i<parts.Length;i++)
                {
                    float angle=i*2.399963f+.34f*4;
                    collapseVelocity[i]=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*(1.2f+(i%3)*.4f)+Vector3.up*(.7f+(i%2)*.45f);
                }
            }
            for(int i=1;i<parts.Length;i++)
            {
                float angle=i*2.399963f+Mathf.Min(age,.34f)*4;
                Vector3 radial=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                if(!collapseReleased)
                {
                    float distance=Mathf.Lerp(size*.23f,.16f,Mathf.Clamp01(age/.34f));
                    float height=Mathf.Lerp(.9f+(i%3)*.22f,1.45f,Mathf.Clamp01(age/.34f));
                    parts[i].localPosition=radial*distance+Vector3.up*height;
                }
                else
                {
                    AdvanceCollapseFragment(parts[i],ref collapseVelocity[i],Time.deltaTime);
                    if(collapseVelocity[i].sqrMagnitude>.025f)parts[i].Rotate(new Vector3(117,92,65)*Time.deltaTime,Space.Self);
                }
                if(!collapseReleased)parts[i].localRotation=Quaternion.Euler(i*39+age*117,i*67+age*92,23+age*65);
                surfaces[i].GetPropertyBlock(block);block.SetFloat("_Opacity",Mathf.Clamp01((life-age)/.075f));block.SetFloat("_Phase",age);surfaces[i].SetPropertyBlock(block);
            }
            if(glow!=null)glow.intensity=Mathf.Exp(-Mathf.Pow((age-.34f)/.11f,2))*2.1f;
        }
    }
}
