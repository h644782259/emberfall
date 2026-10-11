using System;
using System.Reflection;
using Emberfall;
using UnityEngine;
public static class GameFontTests
{
    public static string Run()
    {
        int checks=0;Action<bool,string> check=(ok,message)=>{checks++;if(!ok)throw new Exception(message);};
        Action reset=()=>typeof(GameFont).GetMethod("Reset",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
        reset();Resources.Full=new Font();Resources.Labels=new Font();Font.Created=0;UnityEngine.Object.Destroyed=0;
        Font first=GameFont.Shared,alias=GameFont.Shared;
        check(first==Resources.Full&&first==alias&&Font.Created==0,"packaged CJK font shared across consumers");
        GameFont.Release(ref alias);check(alias==null&&GameFont.Shared==first&&UnityEngine.Object.Destroyed==0,"consumer release never destroys shared asset");
        check(GameFont.WorldLabels==Resources.Labels,"small world subset stays scoped to authored labels");
        reset();check(UnityEngine.Object.Destroyed==0,"subsystem reset does not destroy Resources fonts");
        Resources.Full=null;Resources.Labels=null;Debug.Errors=0;
#if UNITY_ANDROID || UNITY_IOS
        check(GameFont.Shared==null&&Font.Created==0,"mobile missing full font does not fall back to OS or 18-glyph subset");
        check(GameFont.Shared==null&&Debug.Errors==1,"missing bundle reported once");
#else
        Font os=GameFont.Shared;check(os!=null&&Font.Created==1,"desktop uses one owned dynamic CJK fallback");
        check(Font.Families[0]=="Microsoft YaHei"&&Array.IndexOf(Font.Families,"SimSun")>=0,"desktop fallback includes CJK families before Arial");
        GameFont.Release(ref os);check(GameFont.Shared!=null&&Font.Created==1&&UnityEngine.Object.Destroyed==0,"desktop aliases share bounded font lifetime");
        reset();check(UnityEngine.Object.Destroyed==1,"only resolver destroys owned dynamic desktop font");
#endif
        Resources.Full=new Font();reset();check(GameFont.WorldLabels==Resources.Full,"missing small subset can use full UI font");
        return "PASS: "+checks+" font resolver ownership/platform checks (managed API fixture, no glyph rasterization)";
    }
}
namespace UnityEngine
{
    public class Object {public static int Destroyed;public static void Destroy(Object value){Destroyed++;}}
    public class Font:Object
    {
        public static int Created;public static string[] Families;public object material=new object();
        public static Font CreateDynamicFontFromOSFont(string[] families,int size){Created++;Families=families;return new Font();}
        public void RequestCharactersInTexture(string text,int size,FontStyle style){}
    }
    public class Renderer {public object sharedMaterial;}
    public class TextMesh {public Font font;public string text="";public int fontSize;public FontStyle fontStyle;public T GetComponent<T>() where T:new(){return new T();}}
    public enum FontStyle{Normal,Bold}
    public static class Resources {public static Font Full,Labels;public static T Load<T>(string path) where T:class{return (path==GameFont.BundledUiPath?Full:Labels) as T;}}
    public static class Debug {public static int Errors;public static void LogError(string text){Errors++;}}
    public enum RuntimeInitializeLoadType{SubsystemRegistration}
    public class RuntimeInitializeOnLoadMethodAttribute:Attribute {public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType kind){}}
}
