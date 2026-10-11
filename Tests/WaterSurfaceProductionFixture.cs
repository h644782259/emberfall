// Managed Unity API boundary only. Mesh arrays, normals, components and world
// ownership execute production code; shader resources/GPU uploads are separate.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
namespace UnityEngine
{
 public enum PrimitiveType {Sphere,Cube,Cylinder,Capsule,Quad}
 public class Object {public string name;public bool Destroyed;public static void Destroy(Object value){if(value==null||value.Destroyed)return;value.Destroyed=true;if(value is GameObject go){go.Call("OnDestroy");foreach(var child in GameObject.All.Where(o=>o.transform.parent==go.transform).ToArray())Destroy(child);}}}
 public class Component:Object {public GameObject gameObject;public Transform transform=>gameObject.transform;public T GetComponent<T>()where T:Component=>gameObject.GetComponent<T>();}
 public class MonoBehaviour:Component {}
 public class GameObject:Object {public static readonly List<GameObject> All=new List<GameObject>();public readonly Transform transform;private readonly List<Component> components=new List<Component>();public GameObject(string name=""){this.name=name;transform=new Transform{gameObject=this};All.Add(this);}public T AddComponent<T>()where T:Component,new(){var c=new T{gameObject=this};components.Add(c);return c;}public T GetComponent<T>()where T:Component=>components.OfType<T>().FirstOrDefault();public void Call(string method){foreach(var c in components)c.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic)?.Invoke(c,null);}}
 public class Transform {public GameObject gameObject;public Transform parent;public Vector3 localPosition,localScale=Vector3.one;public void SetParent(Transform p,bool world){parent=p;}}
 public class Mesh:Object {public Vector3[] vertices,normals;public int[] triangles;public Color[] colors;public Vector2[] uv;public Rendering.IndexFormat indexFormat;public void SetVertices(List<Vector3> v){vertices=v.ToArray();}public void SetTriangles(List<int> t,int sub){triangles=t.ToArray();}public void SetColors(List<Color> c){colors=c.ToArray();}public void SetUVs(int channel,List<Vector2> value){uv=value.ToArray();}public void RecalculateBounds(){}public void RecalculateNormals(){normals=new Vector3[vertices.Length];for(int i=0;i<triangles.Length;i+=3){int a=triangles[i],b=triangles[i+1],c=triangles[i+2];var n=Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]);normals[a]+=n;normals[b]+=n;normals[c]+=n;}for(int i=0;i<normals.Length;i++)normals[i]=normals[i].normalized;}}
 public class MeshFilter:Component {public Mesh sharedMesh;}
 public class Renderer:Component {public Material sharedMaterial;public Rendering.ShadowCastingMode shadowCastingMode;}
 public class MeshRenderer:Renderer {}
 public class Shader:Object {public static Shader Find(string s)=>new Shader();}
 public static class Resources {public static T Load<T>(string name)where T:new()=>new T();}
 public class Material:Object {public Color color;private readonly Dictionary<string,float> values=new Dictionary<string,float>();public Material(Shader shader){}public bool HasProperty(string key)=>true;public void SetFloat(string key,float value){values[key]=value;}public float GetFloat(string key)=>values.TryGetValue(key,out var value)?value:0;public void SetColor(string key,Color value){} }
 public static class ColorUtility {public static string ToHtmlStringRGBA(Color c)=>$"{c.r},{c.g},{c.b},{c.a}";}
 public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}public static Color operator*(Color c,float f)=>new Color(c.r*f,c.g*f,c.b*f,c.a*f);public static Color Lerp(Color a,Color b,float t){t=Mathf.Clamp01(t);return new Color(a.r+(b.r-a.r)*t,a.g+(b.g-a.g)*t,a.b+(b.b-a.b)*t,a.a+(b.a-a.a)*t);}}
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public float sqrMagnitude=>x*x+y*y;public static Vector2 operator-(Vector2 a,Vector2 b)=>new Vector2(a.x-b.x,a.y-b.y);public static Vector2 operator*(Vector2 a,float b)=>new Vector2(a.x*b,a.y*b);}
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static Vector3 zero=>new Vector3();public static Vector3 one=>new Vector3(1,1,1);public static Vector3 up=>new Vector3(0,1,0);public float sqrMagnitude=>x*x+y*y+z*z;public float magnitude=>(float)Math.Sqrt(sqrMagnitude);public Vector3 normalized=>magnitude>1e-10f?this/magnitude:zero;public void Normalize(){this=normalized;}public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator*(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);public static Vector3 operator/(Vector3 a,float b)=>a*(1/b);public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*Mathf.Clamp01(t);public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;public static Vector3 Cross(Vector3 a,Vector3 b)=>new Vector3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);public static float Distance(Vector3 a,Vector3 b)=>(a-b).magnitude;public static Vector3 ClampMagnitude(Vector3 a,float m)=>a.magnitude>m?a.normalized*m:a;}
 public struct Rect {public float xMin,xMax,yMin,yMax;public Rect(float x,float y,float w,float h){xMin=x;xMax=x+w;yMin=y;yMax=y+h;}public float width=>xMax-xMin;public float height=>yMax-yMin;public Vector2 center=>new Vector2((xMin+xMax)*.5f,(yMin+yMax)*.5f);}
 public static class Time {public static float time;}
 public static class Mathf {public const float PI=(float)Math.PI;public static float Min(float a,float b)=>Math.Min(a,b);public static int Min(int a,int b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int Max(int a,int b)=>Math.Max(a,b);public static float Abs(float a)=>Math.Abs(a);public static float Clamp(float v,float a,float b)=>Math.Clamp(v,a,b);public static int Clamp(int v,int a,int b)=>Math.Clamp(v,a,b);public static float Clamp01(float v)=>Math.Clamp(v,0,1);public static int CeilToInt(float v)=>(int)Math.Ceiling(v);public static int RoundToInt(float v)=>(int)Math.Round(v);public static float Sin(float v)=>(float)Math.Sin(v);public static float Cos(float v)=>(float)Math.Cos(v);public static float Pow(float a,float b)=>(float)Math.Pow(a,b);public static float SmoothStep(float a,float b,float t){t=Clamp01(t);return a+(b-a)*t*t*(3-2*t);}}
}
namespace UnityEngine.Rendering {public enum ShadowCastingMode {Off}public enum IndexFormat {UInt16,UInt32}}
namespace Emberfall
{
 public enum ZoneKind {Wilderness,Dungeon}internal enum VisualSurface {Stone,Water,Wood,Crystal}
 // Resource/color binding is outside this managed geometry fixture.
 public static class SurfaceTextureLibrary {public static bool Enabled=false;}
 internal static class NaturalWorldMaterial {internal static void Apply(Material m,VisualSurface s,bool e){}internal static void Trail(Material m){}}
 public static class ProceduralVisuals {internal static void ApplySurface(Material m,VisualSurface s){}}
 public static class PlayerUpgradeRules {public static float FindSafeBlinkDistance(float d,Func<float,bool>a,Func<float,bool>b){throw new NotSupportedException("Blink is outside the actual water-entry fixture.");}}
 public static class CombatFx {public static Vector3 Flat(Vector3 v){v.y=0;return v;}public static float SegmentDistance(Vector3 p,Vector3 a,Vector3 b){p.y=a.y=b.y=0;var d=b-a;return Vector3.Distance(p,a+d*(d.sqrMagnitude<.0001f?0:Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude)));}}
 public static partial class WorldBuilder
 {
  // Bridge decorative Primitive calls occur after the real water entry. They
  // retain their supplied positions; surface conversion is tested separately.
  public static GameObject Primitive(Transform p,string n,PrimitiveType t,Vector3 at,Vector3 size,Material m){var o=new GameObject(n);o.transform.SetParent(p,false);o.transform.localPosition=at;o.transform.localScale=size;return o;}
  private static void SubdivideGround(ref List<Vector3> v,ref List<int> t){throw new NotSupportedException("Continuous water is not a terrain ground mesh.");}
  private static void WaterEntry(bool baseline,Transform p,WorldResources r,string n,Vector3[] path,float width,float height,WaterEnvironment e){if(!baseline)BuildWaterSurface(p,r,n,path,width,height,e);}
  private static void BedEntry(bool baseline,Transform p,WorldResources r,Vector3[] path,float width,float height){if(!baseline)BuildNaturalStreamBed(p,r,path,width,height);}
  public static Vector3[] LongPath(Transform p,WorldResources r){var path=Enumerable.Range(0,200).Select(i=>new Vector3(i,0,0)).ToArray();WorldTraversal.SetRiver(path,2,new Rect(-1,-1,2,2));BuildWaterSurface(p,r,"long",path,2,.02f,WaterEnvironment.Brook);return path;}
 }
}
