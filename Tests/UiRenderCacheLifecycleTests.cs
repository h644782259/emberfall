// Runs the actual FloatingNumber source against managed counters/geometry below.
// This verifies event ownership and glyph-query calls, NOT Unity font metrics, rendering or GC costs.
using System;
using System.Collections.Generic;
using System.Reflection;
using Emberfall;
using UnityEngine;
public static class UiRenderCacheLifecycleTests
{
    static int checks,frame;
    static void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
    static void Reflow(FloatingNumber value)
    {Time.frameCount=++frame;typeof(FloatingNumber).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(value,null);}
    static CombatTextLayout.Box Box(FloatingNumber value)
    {return (CombatTextLayout.Box)typeof(FloatingNumber).GetField("bounds",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(value);}
    public static string Run()
    {
        checks=0;frame=10;Camera.main=new Camera();GameFont.Shared=new Font();Font.Requests=Font.Queries=Font.Destroyed=0;
        var a=FloatingNumber.Spawn(Vector3.zero,"1234",new Color(1,1,1));
        Check(a!=null&&FloatingNumber.ActiveCount==1&&Font.Subscribers==1,"first live caption owns one texture subscription");
        Check(Font.Requests==1&&Font.Queries==4,"admitted original glyph metrics are consumed without a second query");
        for(int i=0;i<48;i++)Reflow(a);
        Check(Font.Requests==1&&Font.Queries==4,"stable caption reflows do not request or query glyphs again");
        var old=Box(a);EffectPreferences.CombatTextScale=1.8f;Screen.dpi=326;Reflow(a);
        Check(Box(a).Height>old.Height&&Font.Requests==1,"scale/DPI update box without querying unchanged font");
        Font.Emit(new Font());Reflow(a);Check(Font.Requests==1,"unrelated font atlas does not invalidate metrics");
        Font.Emit(GameFont.Shared);Reflow(a);Check(Font.Requests==2&&Font.Queries==8,"active atlas rebuild refreshes glyph metrics once");
        var priorFont=GameFont.Shared;GameFont.Shared=new Font{GlyphWidth=64};Reflow(a);
        Check(Font.Requests==3&&Font.Queries==12,"font replacement refreshes metrics once");
        foreach(var text in a.gameObject.GetComponentsInChildren<TextMesh>())Check(text.font==GameFont.Shared,"replacement updates primary and outline font binding");
        var b=FloatingNumber.Spawn(new Vector3(3,0,0),"5678",new Color(1,1,1));
        Check(b!=null&&FloatingNumber.ActiveCount==2&&Font.Subscribers==1,"multiple captions share one event handler");
        a.gameObject.SetActive(false);Check(FloatingNumber.ActiveCount==1&&Font.Subscribers==1,"disable releases one caption but retains live subscription");
        UnityEngine.Object.Destroy(b.gameObject);Check(FloatingNumber.ActiveCount==0&&Font.Subscribers==0,"last destroy removes subscription");
        Font.Emit(GameFont.Shared);Check(Font.Destroyed==0,"caption lifecycle never destroys resolver-owned fonts");
        UnityEngine.Object.Destroy(a.gameObject);Check(FloatingNumber.ActiveCount==0&&Font.Subscribers==0,"disable then destroy cannot double-release");
        var c=FloatingNumber.Spawn(Vector3.zero,"1234",new Color(1,1,1));
        Check(c!=null&&Font.Subscribers==1,"new batch restores one subscription");
        GameFont.Shared.EmitOnNextRequest=true;
        var d=FloatingNumber.Spawn(new Vector3(3,0,0),"新的",new Color(1,1,1));int requests=Font.Requests;
        Reflow(c);Check(Font.Requests==requests+1,"atlas growth during admission invalidates older caption only; new caption retains post-rebuild metrics");
        UnityEngine.Object.Destroy(c.gameObject);UnityEngine.Object.Destroy(d.gameObject);
        Check(FloatingNumber.ActiveCount==0&&FloatingNumber.MechanismCount==0&&Font.Subscribers==0,"all live reservations and subscriptions retire");
        requests=Font.Requests;Check(!FloatingNumber.CanSpawn(new Vector3(-100,0,0))&&Font.Requests==requests,"rejected off-screen admission does not request glyphs");
        return "PASS: "+checks+" actual FloatingNumber managed lifecycle checks; stable glyph queries, texture rebuild/replacement, live DPI, disable/destroy and resolver ownership (not Unity execution)";
    }
}
namespace Emberfall
{
    public static class GameFont {public static Font Shared;}
    public static class EffectPreferences {public static float CombatTextScale=1.25f,EffectsScale=1;}
    public static class MobileControls {public static bool Active=false;public static readonly LayoutInfo Layout=new LayoutInfo();public class LayoutInfo {public float Scale=1;}}
}
namespace UnityEngine
{
    public class Object
    {
        public static void Destroy(Object value)
        {
            if(value is Font){Font.Destroyed++;return;}
            if(value is GameObject go){go.SetActive(false);go.Call("OnDestroy");}
        }
    }
    public class Component:Object
    {
        public GameObject gameObject;public Transform transform {get{return gameObject.transform;}}
        public T GetComponent<T>() where T:class {return gameObject.GetComponent<T>();}
    }
    public class MonoBehaviour:Component {}
    public class GameObject:Object
    {
        readonly List<Component> components=new List<Component>();public readonly Transform transform;bool active=true;
        public GameObject(string name=""){transform=new Transform{gameObject=this};}
        public T AddComponent<T>() where T:Component,new()
        {var c=new T{gameObject=this};components.Add(c);if(c is TextMesh&&GetComponent<MeshRenderer>()==null)AddComponent<MeshRenderer>();return c;}
        public T GetComponent<T>() where T:class {foreach(var c in components)if(c is T found)return found;return null;}
        public T[] GetComponentsInChildren<T>() where T:class
        {var result=new List<T>();foreach(var c in components)if(c is T found)result.Add(found);foreach(var child in transform.children)result.AddRange(child.gameObject.GetComponentsInChildren<T>());return result.ToArray();}
        public void SetActive(bool value){if(active==value)return;active=value;if(!value)Call("OnDisable");}
        public void Call(string name){foreach(var c in components)c.GetType().GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance)?.Invoke(c,null);}
    }
    public class Transform
    {
        public GameObject gameObject;public Vector3 position,localPosition,localScale;public Quaternion rotation;public readonly List<Transform> children=new List<Transform>();public Vector3 forward=Vector3.forward;
        public void SetParent(Transform parent,bool world){parent.children.Add(this);}
    }
    public class Renderer:Component {public Material sharedMaterial;}
    public class MeshRenderer:Renderer {public int sortingOrder;public Bounds localBounds=new Bounds{size=new Vector3(.5f,1,0)};}
    public class Material {}
    public struct Bounds {public Vector3 size;}
    public class TextMesh:Component {public string text;public int fontSize;public float characterSize;public TextAnchor anchor;public TextAlignment alignment;public FontStyle fontStyle;public Color color;public Font font;}
    public enum TextAnchor {MiddleCenter}public enum TextAlignment{Center}public enum FontStyle{Bold}
    public class Font:Object
    {
        static Action<Font> rebuilt;public static int Subscribers,Requests,Queries,Destroyed;public int GlyphWidth=32;public bool EmitOnNextRequest;public Material material=new Material();
        public static event Action<Font> textureRebuilt {add{rebuilt+=value;Subscribers++;}remove{rebuilt-=value;Subscribers--;}}
        public static void Emit(Font font){rebuilt?.Invoke(font);}
        public void RequestCharactersInTexture(string value,int size,FontStyle style){Requests++;if(EmitOnNextRequest){EmitOnNextRequest=false;Emit(this);}}
        public bool GetCharacterInfo(char c,out CharacterInfo info,int size,FontStyle style){Queries++;info=new CharacterInfo{maxX=GlyphWidth,maxY=64,advance=GlyphWidth};return true;}
    }
    public struct CharacterInfo {public int minX,maxX,minY,maxY,advance;}
    public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}}
    public struct Quaternion {}
    public struct Vector2
    {
        public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}
        public static implicit operator Vector2(Vector3 v)=>new Vector2(v.x,v.y);
        public static float Distance(Vector2 a,Vector2 b)=>(float)Math.Sqrt((a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y));
    }
    public struct Vector3
    {
        public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static Vector3 zero=>new Vector3();public static Vector3 one=>new Vector3(1,1,1);public static Vector3 forward=>new Vector3(0,0,1);
        public static Vector3 operator*(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);
        public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
        public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;
        public static float Distance(Vector3 a,Vector3 b){var v=a-b;return (float)Math.Sqrt(Dot(v,v));}
    }
    public class Camera
    {
        public static Camera main;public int pixelWidth=2000,pixelHeight=1200;public float orthographicSize=10,fieldOfView=60;public bool orthographic=true;public Transform transform=new Transform();
        public Vector3 WorldToScreenPoint(Vector3 v)=>new Vector3(1000+v.x*150,400+v.y*150,10);
        public Vector3 ScreenToWorldPoint(Vector3 v)=>new Vector3((v.x-1000)/150,(v.y-400)/150,10);
    }
    public static class Screen {public static float dpi=163;public static int height=1080;}
    public static class Time {public static int frameCount;public static float deltaTime=1f/60;}
    public static class Mathf
    {
        public const float Deg2Rad=(float)(Math.PI/180);
        public static float Max(float a,float b)=>Math.Max(a,b);public static int Max(int a,int b)=>Math.Max(a,b);
        public static float Min(float a,float b)=>Math.Min(a,b);public static float Clamp(float a,float lo,float hi)=>Math.Min(hi,Math.Max(lo,a));
        public static float Clamp01(float v)=>Clamp(v,0,1);public static float Tan(float v)=>(float)Math.Tan(v);
    }
}
