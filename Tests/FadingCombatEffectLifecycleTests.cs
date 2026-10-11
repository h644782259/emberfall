// Compile with the actual FadingCombatEffect class, DecorationLease and DecorationBudget.
// Managed Unity substitutes exercise retirement ordering; they do not verify rendering.
using System;
using System.Collections.Generic;
using System.Reflection;
using Emberfall;
using UnityEngine;
public static class FadingCombatEffectLifecycleTests
{
    static int checks;
    static void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
    static FadingCombatEffect Spawn(bool bolt=false)
    {
        var obj=new GameObject();
        if(bolt)Check(DecorationLease.Attach(obj,1),"bolt admitted");
        var effect=obj.AddComponent<FadingCombatEffect>();
        effect.Setup(obj.AddComponent<LineRenderer>(),new Color(),1,1,!bolt);
        return effect;
    }
    static void Tick(FadingCombatEffect effect){effect.gameObject.Call("Update");}
    static GameSession Session(){return GameSession.Instance=new GameSession{HasStarted=true,Player=new PlayerController{CombatEpoch=12}};}
    public static string Run()
    {
        var game=Session();Time.deltaTime=0;
        var paused=Spawn();Tick(paused);
        Check(paused.gameObject.activeSelf&&FadingCombatEffect.ActiveCount==1,"ordinary paused combat retains its ring");
        Time.deltaTime=.5f;Tick(paused);Time.deltaTime=0;
        Tick(paused);Check(paused.gameObject.activeSelf,"paused age does not advance");
        game.CombatEnded=true;Tick(paused);
        Check(paused.gameObject.activeSelf,"ordinary cleared dungeon retains final impact until its natural lifetime");
        game.CombatEnded=false;
        foreach(var reason in new[]{"title","death","success-result","failed-result","player","owner-destroyed","session-destroyed","epoch"})
        {
            UnityEngine.Object.Flush();
            game=Session();var fx=Spawn();
            switch(reason){case "title":game.HasStarted=false;break;case "death":game.IsDead=true;break;case "success-result":case "failed-result":game.ModeFinished=true;break;case "player":game.Player=new PlayerController{CombatEpoch=12};break;case "owner-destroyed":game.Player=null;break;case "session-destroyed":GameSession.Instance=null;break;case "epoch":game.Player.CombatEpoch++;break;}
            Tick(fx);
            Check(!fx.gameObject.activeSelf,reason+" retires even at zero deltaTime");
            Check(fx.gameObject.PendingDestroy,reason+" schedules object destruction");
            Tick(paused); // An old session reference must not survive a replacement session/player.
            Check(FadingCombatEffect.ActiveCount==0,reason+" returns count before deferred Destroy");
            UnityEngine.Object.Flush();Check(FadingCombatEffect.ActiveCount==0,reason+" deferred OnDestroy is idempotent");
        }
        game=Session();var bolts=new List<FadingCombatEffect>();
        for(int i=0;i<5;i++)bolts.Add(Spawn(true));
        var overflow=new GameObject();Check(!DecorationLease.Attach(overflow,1),"reduced bolt cap reached");
        game.HasStarted=false;
        foreach(var bolt in bolts)Tick(bolt);
        Check(FadingCombatEffect.ActiveCount==0,"title releases every standalone fork before Destroy flush");
        game=Session();var next=Spawn(true);
        Check(next.gameObject.activeSelf,"new adventure immediately admits fork using returned lease");
        UnityEngine.Object.Flush();Check(FadingCombatEffect.ActiveCount==1,"old Destroy callbacks cannot consume new reservation");
        Time.deltaTime=1.1f;Tick(next);UnityEngine.Object.Flush();
        Check(FadingCombatEffect.ActiveCount==0,"normal lifetime still expires");
        GameSession.Instance=null;Time.deltaTime=0;Time.unscaledDeltaTime=.25f;
        var title=Spawn();Tick(title);Check(title.gameObject.activeSelf,"unbound title effect gets its own finite unscaled lifetime");
        Time.unscaledDeltaTime=1;Tick(title);UnityEngine.Object.Flush();
        Check(FadingCombatEffect.ActiveCount==0,"unbound effect expires without a session or scaled time");
        return "PASS: "+checks+" actual fading-effect managed lifecycle checks (not Unity execution)";
    }
}
namespace Emberfall
{
    public class GameSession {public static GameSession Instance;public bool HasStarted,IsDead,CombatEnded,ModeFinished;public PlayerController Player;}
    public class PlayerController {public int CombatEpoch;}
    public static class EffectPreferences {public static float EffectsScale=1;public static bool ReducedEffects=true;}
    public static class WorldTraversal {public static int Revision;}
    public static class CombatSight {public static void FillAreaBoundary(Vector3[] points,Vector3 center,float radius){}}
}
namespace UnityEngine
{
    public enum RuntimeInitializeLoadType{SubsystemRegistration}
    [AttributeUsage(AttributeTargets.Method)]public class RuntimeInitializeOnLoadMethodAttribute:Attribute{public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type){}}
    public enum Space{Self}
    public static class Application{public static bool isMobilePlatform=true;}
    public static class Time{public static float deltaTime,unscaledDeltaTime;}
    public class Object
    {
        static readonly List<GameObject> pending=new List<GameObject>();
        public static void Destroy(Object obj){if(obj is GameObject go&&!go.PendingDestroy){go.PendingDestroy=true;pending.Add(go);}}
        public static void Flush(){foreach(var go in pending){go.SetActive(false);go.Call("OnDestroy");}pending.Clear();}
    }
    public class Component:Object{public GameObject gameObject;public Transform transform{get{return gameObject.transform;}}}
    public class MonoBehaviour:Component{}
    public class Material:Object{}
    public class GameObject:Object
    {
        public bool activeSelf=true,PendingDestroy;public Transform transform=new Transform();readonly List<Component> components=new List<Component>();
        public T GetComponent<T>()where T:Component{foreach(var c in components)if(c is T value)return value;return null;}
        public T AddComponent<T>()where T:Component,new(){var c=new T{gameObject=this};components.Add(c);Invoke(c,"OnEnable");return c;}
        static void Invoke(Component c,string name){c.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)?.Invoke(c,null);}
        public void Call(string name){foreach(var c in components)Invoke(c,name);}
        public void SetActive(bool value){if(activeSelf==value)return;activeSelf=value;Call(value?"OnEnable":"OnDisable");}
    }
    public class Transform{public Vector3 position,localScale;public void Rotate(float x,float y,float z,Space space){}}
    public class LineRenderer:Component{public bool useWorldSpace;public Color startColor,endColor;public Material sharedMaterial=new Material();public void SetPositions(Vector3[] points){}}
    public struct Color{public float a;}
    public struct Vector3
    {
        public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static Vector3 one{get{return new Vector3(1,1,1);}}
        public float sqrMagnitude{get{return x*x+y*y+z*z;}}
        public static Vector3 operator *(Vector3 a,float b){return new Vector3(a.x*b,a.y*b,a.z*b);}
        public static Vector3 operator -(Vector3 a,Vector3 b){return new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);}
    }
    public static class Mathf
    {
        public static int Max(int a,int b){return Math.Max(a,b);}public static float Max(float a,float b){return Math.Max(a,b);}
        public static float Lerp(float a,float b,float t){return a+(b-a)*Clamp01(t);}public static float Pow(float a,float b){return (float)Math.Pow(a,b);}
        public static float Clamp01(float f){return Math.Max(0,Math.Min(1,f));}
    }
}
