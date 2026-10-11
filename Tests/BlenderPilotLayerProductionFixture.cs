// Imported-adapter tests explicitly authorize preview construction; subsequent clock tests exercise its sampler in isolation.
// Managed animation/transform boundaries, deliberately independent of production owner tables.
// Clips overwrite every bone, so retaining stale lower locals cannot accidentally pass.
using System;
using System.Linq;
using UnityEngine;
using Emberfall;
static class LayerFixture
{
    static readonly string[] names={"Root","Pelvis","Spine","Head","Mantle","Thigh.L","Shin.L","Foot.L","Thigh.R","Shin.R","Foot.R","Tabard.L","Tabard.R","UpperArm.L","Forearm.L","Hand.L","UpperArm.R","Forearm.R","Hand.R"};
    static readonly string[] parents={null,"Root","Pelvis","Spine","Spine","Pelvis","Thigh.L","Shin.L","Pelvis","Thigh.R","Shin.R","Pelvis","Pelvis","Spine","UpperArm.L","Forearm.L","Spine","UpperArm.R","Forearm.R"};
    static readonly string[] upper={"Spine","Head","Mantle","UpperArm.L","Forearm.L","Hand.L","UpperArm.R","Forearm.R","Hand.R"};
    static readonly string[] sockets={"Anchor_Pommel","Anchor_Grip","Anchor_Guard","Anchor_BladeRoot","Anchor_Tip"};
    static int checks;
    static void C(bool b,string m){checks++;if(!b)throw new Exception(m);}
    public static bool Near(Vector3 a,Vector3 b)=>Math.Abs(a.x-b.x)<.0001&&Math.Abs(a.y-b.y)<.0001&&Math.Abs(a.z-b.z)<.0001;
    static bool Near(Quaternion a,Quaternion b)=>Math.Abs(System.Numerics.Quaternion.Dot(a.q,b.q))>.99999;
    static Transform Bone(GameObject root,string name)=>root.GetComponentsInChildren<Transform>(true).Single(x=>x.name==name);
    public static void Build(GameObject root,string omit=null)
    {
        for(int i=0;i<names.Length;i++) {var g=new GameObject(names[i]);g.transform.SetParent(parents[i]==null?root.transform:Bone(root,parents[i]));}
        for(int i=0;i<sockets.Length;i++)if(sockets[i]!=omit){var g=new GameObject(sockets[i]);g.transform.SetParent(Bone(root,"Hand.R"));g.transform.localPosition=new Vector3(.1f,i*.2f,0);}
    }
    public struct Pose
    {
        public Vector3 P,S;public Quaternion Q;
        public void Apply(Transform t){t.localPosition=P;t.localScale=S;t.localRotation=Q;}
    }
    // Adversarial managed clip boundary, NOT the actual FBX or an authored root-motion claim.
    // Root has a static bind-like local TRS, and Basic deliberately overwrites it differently;
    // preserving the locomotion Root must therefore be observable in all three channels.
    static Pose Clip(string clip,int i,float t)
    {
        if(i==0)return clip=="Pilot_Basic"
            ?new Pose{P=new Vector3(-.4f,.3f,.2f),S=new Vector3(.8f,1.2f,.9f),Q=Quaternion.Euler(-20,35,10)}
            :new Pose{P=new Vector3(.12f,-.08f,.24f),S=new Vector3(1.1f,.95f,1.05f),Q=Quaternion.Euler(15,-25,8)};
        int c=clip=="Pilot_Idle"?1:clip=="Pilot_Move"?3:clip=="Pilot_Basic"?7:clip=="Pilot_Hit"?11:13;
        float wave=(float)Math.Sin(t*Math.PI*2),v=c*(1+wave*.1f)+i*.13f;
        return new Pose{P=new Vector3(v*.012f,i*.025f,v*.003f),S=new Vector3(1+v*.002f,1+v*.001f,1),Q=Quaternion.Euler(v*2,v*3,v*4)};
    }
    static Pose Mix(Pose a,Pose b,float w)=>new Pose{P=Vector3.Lerp(a.P,b.P,w),S=Vector3.Lerp(a.S,b.S,w),Q=Quaternion.Slerp(a.Q,b.Q,w)};
    static bool Is(Transform t,Pose p)=>Near(t.localPosition,p.P)&&Near(t.localScale,p.S)&&Near(t.localRotation,p.Q);
    public static void ApplyClip(GameObject root,string clip,float t)
    {for(int i=0;i<names.Length;i++)foreach(var bone in root.GetComponentsInChildren<Transform>(true).Where(x=>x.name==names[i]))Clip(clip,i,t).Apply(bone);}
    public static bool MatchesClip(GameObject root,string clip,float t)=>names.Select((name,i)=>Is(Bone(root,name),Clip(clip,i,t))).All(x=>x);
    static float Repeat(float n)=>n-(float)Math.Floor(n);
    static float Safe(float n)=>float.IsNaN(n)||float.IsInfinity(n)?0:Math.Max(0,Math.Min(1,n));
    static void Expected(GameObject root,float time,float phase,float speed,bool basic,float progress)
    {
        float u=Safe((progress-.75f)/.25f),weight=1-u*u*(3-2*u);
        for(int i=0;i<names.Length;i++)
        {
            var b=Mix(Clip("Pilot_Idle",i,Repeat(time/2)),Clip("Pilot_Move",i,Repeat(phase/(2*(float)Math.PI))),Safe(speed));
            if(basic&&upper.Contains(names[i]))b=Mix(b,Clip("Pilot_Basic",i,Safe(progress)),weight);
            C(Is(Bone(root,names[i]),b),"composed local TRS ownership: "+names[i]);
        }
    }
    static void ResourcesReady()
    {
        var root=new GameObject("layer asset");root.AddComponent<Renderer>();root.AddComponent<MeshFilter>().sharedMesh=new Mesh();Build(root);
        Resources.Items["BlenderPilot/Vanguard"]=root;Resources.Items["BlenderPilot/Pilot_Atlas_Standard"]=new Material();
        Resources.Clips=new[]{"Pilot_Idle","Pilot_Move","Pilot_Basic","Pilot_Hit","Pilot_Skill"}.Select(n=>new AnimationClip{name=n,length=2}).ToArray();BlenderPilotArt.Enabled=true;
    }
    static void Rejected(string reason,Action<GameObject> change)
    {
        ResourcesReady();change((GameObject)Resources.Items["BlenderPilot/Vanguard"]);
        C(BlenderPilotVisual.Create(null)==null,"layer binding rejected: "+reason);
    }
    public static void Run()
    {
        // Hand-computed rotation/scale golden: (1,0,0)*2, rotate Z90, add(1,2,3).
        var p=new GameObject("golden parent");p.transform.localPosition=new Vector3(1,2,3);p.transform.localScale=new Vector3(2,2,2);p.transform.localRotation=Quaternion.Euler(0,0,90);
        var child=new GameObject("golden socket");child.transform.SetParent(p.transform);child.transform.localPosition=new Vector3(1,0,0);
        C(Near(child.transform.position,new Vector3(1,4,3)),"transform double rotates/scales parent socket goldens");
        foreach(string n in names)
        {
            Rejected("missing "+n,g=>{Bone(g,n).gameObject.name="missing";Bone(g,n).name="missing";});
            Rejected("duplicate "+n,g=>{var d=new GameObject(n);d.transform.SetParent(g.transform);});
        }
        foreach(string n in names.Skip(1))Rejected("wrong parent "+n,g=>Bone(g,n).SetParent(g.transform));
        foreach(string n in sockets)
        {
            Rejected("duplicate "+n,g=>{var d=new GameObject(n);d.transform.SetParent(Bone(g,"Hand.R"));});
            Rejected("wrong parent "+n,g=>Bone(g,n).SetParent(Bone(g,"Hand.L")));
        }
        ResourcesReady();var host=new GameObject("layer host");var m=host.AddComponent<CombatModel>();m.heroClass=HeroClass.Vanguard;m.isolatedPreview=true;m.Init();m.isolatedPreview=false;
        foreach(float speed in new[]{0f,.049f,.05f,.051f,.2f,.75f,1f,2f,-1f,float.NaN,float.PositiveInfinity})
        foreach(float phase in new[]{0f,.7f,3f,6.28f,6.29f,14f})
        {
            m.locomotion.Speed=speed;m.locomotion.Phase=phase;Time.time=4.3f;
            C(m.Sample(),"normal base remains supported");Expected(m.View.gameObject,Time.time,phase,speed,false,0);
            foreach(float progress in new[]{.52f,.65f,.75f,.8f,.875f,.95f,1f})
            {m.actionBasic=true;C(m.Sample(true,progress),"moving basic remains supported");Expected(m.View.gameObject,Time.time,phase,speed,true,progress);}
        }
        m.locomotion.Speed=.8f;m.locomotion.Phase=1.2f;Time.deltaTime=.02f;
        foreach(float interval in new[]{.18f,.46f,.92f})
        {
            Time.frameCount++;m.PlayAction(-1,true,interval);Expected(m.View.gameObject,Time.time,1.2f,.8f,true,.52f);
            float age=m.actionAge;m.Sample(true,.52f);C(m.actionAge==age,"repeat sampling cannot advance authoritative action age");
            C(m.Visible&&m.View.gameObject.activeSelf,"run basic run keeps same imported renderer family");
            m.CancelAction();C(m.actionDuration==0,"actual cancellation clears action");m.Sample();Expected(m.View.gameObject,Time.time,1.2f,.8f,false,0);
        }
        // Execute the extracted production AnimateHero action clock, rather than supplying
        // progress to Sample. Locomotion inputs remain explicit managed boundary values.
        foreach(float interval in new[]{.18f,.46f,.92f})
        {
            Time.frameCount++;m.locomotion.Speed=.8f;m.PlayAction(-1,true,interval);
            float age=m.actionAge;m.Tick(.03f);
            C(m.actionAge==age,"actual AnimateHero cannot advance on commit frame");
            Time.frameCount++;m.Tick(0);
            C(m.actionAge==age,"actual AnimateHero zero dt preserves committed phase");
            for(int frame=0;frame<40;frame++)
            {
                Time.frameCount++;Time.time+=.03f;
                m.locomotion.Speed=frame<5?1:frame<10?0:.6f;
                m.locomotion.Phase=6.2f+frame*.1f;
                float phase=m.locomotion.Phase,expectedAge=Math.Min(age+.03f,m.actionDuration);
                m.Tick(.03f);
                C(Math.Abs(m.actionAge-expectedAge)<.000001f,"actual AnimateHero advances authoritative action age by dt");
                C(m.locomotion.Phase==phase,"actual AnimateHero retains supplied continuous gait phase");
                float progress=expectedAge/m.actionDuration;
                Expected(m.View.gameObject,Time.time,phase,m.locomotion.Speed,progress<1,progress);
                C(m.Visible,"actual AnimateHero keeps imported rig through contact recovery and end");age=expectedAge;
            }
            C(m.actionAge==m.actionDuration,"actual AnimateHero reaches original duration without extra recovery clock");
        }
        // Stop/restart sequence changes existing speed/phase only, no sampler state or hidden clock.
        float[] speeds={1,.5f,.051f,.049f,0,0,.02f,.2f,.7f,1};
        for(int i=0;i<speeds.Length;i++)
        {m.locomotion.Speed=speeds[i];m.locomotion.Phase=6.2f+i*.05f;m.Sample(true,.8f);Expected(m.View.gameObject,Time.time,m.locomotion.Phase,speeds[i],true,.8f);}
        // Compose a world-space socket oracle independently from each expected parent local.
        m.locomotion.Speed=.6f;m.locomotion.Phase=1;m.Sample(true,.52f);
        var expected=new GameObject("expected hierarchy");Build(expected);
        for(int i=0;i<names.Length;i++)
        {var b=Mix(Clip("Pilot_Idle",i,Repeat(Time.time/2)),Clip("Pilot_Move",i,Repeat(1/(2*(float)Math.PI))),.6f);if(upper.Contains(names[i]))b=Clip("Pilot_Basic",i,.52f);b.Apply(Bone(expected,names[i]));}
        foreach(string s in sockets)
        {
            int index=Array.IndexOf(sockets,s);C(m.TryGetWeaponVisualAnchor((WeaponVisualAnchor)((int)WeaponVisualAnchor.SwordPommel+index),out var point)&&Near(point,Bone(expected,s).position),"composed socket follows actual Hand.R hierarchy "+s);
            var anchor=Bone(m.View.gameObject,s);var old=anchor.position;Bone(m.View.gameObject,"Spine").localRotation=Quaternion.Euler(0,90,0);
            C(!Near(old,anchor.position),"unchanged socket local follows rotating ancestor "+s);m.Sample(true,.52f);
        }
        // Hit and representative stationary skill still sample original full body clips.
        m.locomotion.Speed=0;Time.time=20;m.Sample(false,0,true);C(MatchesClip(m.View.gameObject,"Pilot_Hit",0),"hit remains full body");
        m.actionBasic=false;m.actionSkill=7;m.Sample(true,.4f);C(MatchesClip(m.View.gameObject,"Pilot_Skill",.4f),"skill remains full body");
        Time.time=20.05f;m.PlayAction(-1,true,.46f);
        Expected(m.View.gameObject,Time.time,m.locomotion.Phase,0,true,.52f);
        m.CancelAction();m.Sample();C(MatchesClip(m.View.gameObject,"Pilot_Hit",.05f/.22f),"cancel during live hurt window restores full body Hit");
        Time.time=20.3f;m.Sample();Expected(m.View.gameObject,Time.time,m.locomotion.Phase,0,false,0);
        // Runtime failure after readiness must restore procedural visibility synchronously.
        var original=host.AddComponent<Renderer>();original.enabled=true;m.Unsupported("airborne");m.Sample();m.Unsupported("");m.Sample();
        Resources.Clips.Single(c=>c.name=="Pilot_Basic").ThrowOnSample=true;m.actionBasic=true;
        C(!m.Sample(true,.52f)&&!m.Visible&&!m.View.gameObject.activeSelf&&original.enabled,"runtime sample failure hides partially composed rig");
        Resources.Clips.Single(c=>c.name=="Pilot_Basic").ThrowOnSample=false;
        C(m.Sample(true,.52f)&&m.Visible&&!original.enabled,"sample recovery composes before showing rig");
        foreach(bool initiallyVisible in new[]{false,true})
        foreach(string failingClip in new[]{"Pilot_Idle","Pilot_Move","Pilot_Basic"})
        {
            ResourcesReady();var h=new GameObject("failure host");var visible=h.AddComponent<Renderer>();visible.enabled=true;
            var hidden=new GameObject("hidden procedural");hidden.transform.SetParent(h.transform);var hiddenRenderer=hidden.AddComponent<Renderer>();hiddenRenderer.enabled=false;
            var model=h.AddComponent<CombatModel>();model.heroClass=HeroClass.Vanguard;model.isolatedPreview=true;model.Init();model.isolatedPreview=false;model.locomotion.Speed=.7f;model.actionBasic=true;
            if(initiallyVisible)model.Sample(true,.52f);
            Resources.Clips.Single(c=>c.name==failingClip).ThrowOnSample=true;
            C(!model.Sample(true,.52f)&&!model.Visible&&!model.View.gameObject.activeSelf&&visible.enabled&&!hiddenRenderer.enabled,"each failed composition stage restores original renderer states");
            Resources.Clips.Single(c=>c.name==failingClip).ThrowOnSample=false;
            C(model.Sample(true,.52f)&&model.Visible&&!visible.enabled&&!hiddenRenderer.enabled,"each failed stage recovers only after complete sample");
            Expected(model.View.gameObject,Time.time,0,.7f,true,.52f);
            C(model.TryGetWeaponVisualAnchor(WeaponVisualAnchor.SwordTip,out var tip)&&Near(tip,Bone(model.View.gameObject,"Anchor_Tip").position),"recovered sample refreshes imported anchors");
        }
        Console.WriteLine("PASS: "+checks+" layered production sampling/ownership/state/binding assertions (managed animation boundary)");
    }
}
