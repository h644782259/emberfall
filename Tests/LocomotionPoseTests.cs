using System;
using Emberfall;
public static class LocomotionPoseTests
{
    private static int checks;
    private static void Check(bool ok,string reason){checks++;if(!ok)throw new Exception(reason);}
    private static bool Near(float a,float b){return Math.Abs(a-b)<.0001f;}
    public static string Run()
    {
        checks=0;float phase=-1;
        foreach(int frames in new[]{4,10,60,120})
        {
            var state=new LocomotionPoseState();float dt=1f/frames;
            for(int i=0;i<frames;i++)state.Advance(0,4*dt,dt,4,true,false,0);
            if(phase<0)phase=state.Phase;else Check(Near(phase,state.Phase),"equal walking distance yields equal step phase across frame rates");
            Check(state.Forward>.99f&&Near(state.Side,0),"actual forward velocity is preserved");
            float stopped=state.Phase;
            for(int i=0;i<frames*2;i++)state.Advance(0,0,dt,4,true,false,0);
            Check(Near(stopped,state.Phase)&&state.Speed<.001f,"wall/stop does not progress gait and settles limbs");
            state.Advance(90,80,dt,4,false,false,0);
            Check(Near(stopped,state.Phase),"teleport/knockback excluded by movement classification");
            state.Reset();Check(state.Speed==0&&state.CloakPitch==0&&state.Phase==0,"scene teleport resets momentum");
        }
        foreach(int side in new[]{-1,1})
        {
            var s=new LocomotionPoseState();for(int i=0;i<60;i++)s.Advance(side*.05f,0,1f/60,3,true,false,0);
            Check(s.Side*side>.99f&&Near(s.Forward,0),"sideways movement drives lateral rather than forward stepping");
            Check(s.CloakSide*side<0,"cape trails sideways motion");
        }
        var jump=new LocomotionPoseState();jump.Advance(1,1,.1f,4,true,true,.5f);
        Check(jump.JumpTuck>.99f&&jump.Phase==0,"jump tucks knees without advancing ground gait");
        jump.Advance(0,0,.1f,4,false,false,1);Check(jump.Landing==1,"landing has distinct compression");
        for(int i=0;i<3;i++)jump.Advance(0,0,.1f,4,false,false,0);
        Check(jump.Landing==0,"landing recovers within bounded window");
        float before=jump.Phase;jump.Advance(1,1,0,4,true,false,0);Check(jump.Phase==before,"pause freezes state");
        var random=new Random(83);var varied=new LocomotionPoseState();
        for(int i=0;i<2000;i++)
        {
            float dt=.005f+(float)random.NextDouble()*.5f;
            varied.Advance((float)random.NextDouble()*.4f-.2f,(float)random.NextDouble()*.4f-.2f,dt,4,true,i%17==0,.5f);
            Check(varied.Speed>=0&&varied.Speed<=1&&varied.Phase>=0&&varied.Phase<(float)Math.PI*2,"bounded phase and stride strength");
            Check(varied.CloakPitch>=-14&&varied.CloakPitch<=22&&Math.Abs(varied.CloakSide)<=12,"bounded cloth inertia under variable frame times");
        }
        Check(EnemyActionPose.Windup(EnemyPosePhase.Windup,0)==0&&EnemyActionPose.Windup(EnemyPosePhase.Windup,1)==1,"windup maps actual warning progress");
        Check(EnemyActionPose.Contact(EnemyPosePhase.Windup,1)==0&&EnemyActionPose.Contact(EnemyPosePhase.Recovery,0)==1,"release is tied to actual impact transition");
        Check(EnemyActionPose.Contact(EnemyPosePhase.Recovery,1)==0,"recovery returns to idle");
        return "PASS: "+checks+" displacement, jump, cloth and enemy-phase assertions (no rendered acceptance)";
    }
}
