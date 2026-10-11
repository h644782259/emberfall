using UnityEngine;
namespace Emberfall
{
    /// <summary>One owner for UI fonts. Consumer teardown never destroys packaged assets.</summary>
    public static class GameFont
    {
        public const string BundledUiPath="Fonts/NotoSansSC-Regular";
        public const string WorldLabelPath="Fonts/EmberfallWorldLabels";
        private static Font shared,worldLabels;
        private static bool ownsShared,reportedMissing;
        public static Font Shared
        {
            get
            {
                if(shared!=null)return shared;
                shared=Resources.Load<Font>(BundledUiPath);
#if !UNITY_ANDROID && !UNITY_IOS
                // Desktop packages can avoid shipping the 8 MB mobile font. A CJK
                // family precedes the final Latin fallback; mobile never takes this path.
                if(shared==null)
                {
                    shared=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","微软雅黑","SimHei","SimSun","Noto Sans CJK SC","Noto Sans SC","PingFang SC","Heiti SC","Arial"},64);
                    ownsShared=shared!=null;
                }
#endif
                if(shared==null&&!reportedMissing)
                {reportedMissing=true;Debug.LogError("Missing packaged UI font: "+BundledUiPath+". The world-label subset cannot render the Chinese UI.");}
                return shared;
            }
        }
        public static Font WorldLabels
        {
            get {if(worldLabels==null)worldLabels=Resources.Load<Font>(WorldLabelPath);return worldLabels!=null?worldLabels:Shared;}
        }
        public static void Apply(TextMesh text)
        {
            if(text==null)return;text.font=Shared;
            if(text.font!=null){text.font.RequestCharactersInTexture(text.text,text.fontSize,text.fontStyle);text.GetComponent<Renderer>().sharedMaterial=text.font.material;}
        }
        public static void Release(ref Font reference){reference=null;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            if(ownsShared&&shared!=null)Object.Destroy(shared);
            shared=worldLabels=null;ownsShared=false;reportedMissing=false;
        }
    }
}
