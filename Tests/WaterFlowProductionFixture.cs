// Runs the retained optional flow-band path/clock/component. Unity doubles model mesh ownership and
// lifecycle calls only; they do not validate Unity rendering or native mesh uploads.
using System;
using System.Collections.Generic;
using System.Reflection;
using Emberfall;
namespace UnityEngine {
 public class Object { public bool destroyed; public static void Destroy(Object o){o.destroyed=true;} }
 public class Component:Object {public GameObject gameObject;public Transform transform=>gameObject.transform;}
 public class MonoBehaviour:Component {}
 public class Transform:Component {public void SetParent(Transform t,bool world){}}
 public class GameObject:Object {public static GameObject Last;public Transform transform;public List<Component> components=new List<Component>();public GameObject(string n){Last=this;transform=new Transform{gameObject=this};}public T AddComponent<T>() where T:Component,new(){var c=new T{gameObject=this};components.Add(c);return c;}public T Get<T>() where T:Component {return (T)components.Find(c=>c is T);}}
 public struct Vector3 {public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public static Vector3 up=>new Vector3(0,1,0);public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);}
 public class Mesh:Object {public string name;public Vector3[] vertices,normals;public int[] triangles;public int updates;public void MarkDynamic(){}public void RecalculateBounds(){updates++;}}
 public class Material:Object{}
 public class MeshFilter:Component {public Mesh sharedMesh;}
 public class MeshRenderer:Component {public Material sharedMaterial;public Rendering.ShadowCastingMode shadowCastingMode;public bool receiveShadows;}
 public static class Time {public static float deltaTime;}
}
namespace UnityEngine.Rendering {public enum ShadowCastingMode {Off}}
public static class WaterFlowProductionTests {
 static int checks;static void Check(bool b,string message){checks++;if(!b)throw new Exception(message);}
 static void Call(object o,string method){o.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,null);}
 public static void Main(){
 var xs=new[]{0f,0f,10f};var zs=new[]{0f,10f,10f};var path=new WaterFlowPath(xs,zs);xs[1]=90;Check(path.Length==20,"path owns immutable copy");
 var p=path.Sample(15);Check(p.X==5&&p.Z==10&&p.DirectionX==1,"path follows bend rather than global scrolling");p=path.Sample(-5);Check(p.X==5&&p.Z==10,"negative distance wraps");
 Check(WaterFlowClock.Speed(WaterEnvironment.Brook)>WaterFlowClock.Speed(WaterEnvironment.Courtyard)*3,"brook meaningfully faster than courtyard");
 var clock=new WaterFlowClock();for(int i=0;i<100;i++)Check(!clock.Advance(0,1,20),"pause never advances");Check(!clock.Advance(float.NaN,1,20)&&!clock.Advance(1,float.PositiveInfinity,20),"invalid input rejected");
 var material=new UnityEngine.Material();WaterFlowBands.Create(null,new[]{new UnityEngine.Vector3(0,0,0),new UnityEngine.Vector3(0,0,10),new UnityEngine.Vector3(10,0,10)},.3f,.04f,WaterEnvironment.Brook,material);
 var go=UnityEngine.GameObject.Last;var flow=go.Get<WaterFlowBands>();var mesh=go.Get<UnityEngine.MeshFilter>().sharedMesh;var buffer=mesh.vertices;Check(buffer.Length==24&&mesh.triangles.Length==36,"fixed six-band production topology");
 for(int i=0;i<mesh.triangles.Length;i+=3){var a=buffer[mesh.triangles[i]];var b=buffer[mesh.triangles[i+1]];var c=buffer[mesh.triangles[i+2]];Check((b.z-a.z)*(c.x-a.x)-(b.x-a.x)*(c.z-a.z)>0,"top-facing winding");}
 float initial=buffer[0].z;UnityEngine.Time.deltaTime=.01f;for(int i=0;i<100;i++)Call(flow,"Update");Check(mesh.updates<=11&&mesh.updates>=9,"bounded approximately ten uploads per second");Check(ReferenceEquals(buffer,mesh.vertices),"same vertex buffer reused");Check(buffer[0].z!=initial,"production water actually moves");
 int updates=mesh.updates;UnityEngine.Time.deltaTime=0;for(int i=0;i<100;i++)Call(flow,"Update");Check(mesh.updates==updates,"paused component avoids mesh upload");Check(ReferenceEquals(material,go.Get<UnityEngine.MeshRenderer>().sharedMaterial),"world material stays shared");Call(flow,"OnDestroy");Check(mesh.destroyed&&!material.destroyed,"component releases owned mesh only");Console.WriteLine("PASS "+checks+" retained optional flow-band path/clock/topology/lifecycle assertions (Unity API doubles)");
 }
}
