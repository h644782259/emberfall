using UnityEngine;
using System.Collections.Generic;
namespace Emberfall
{
    /// <summary>Measured screen-space text with separate bounded mechanism admission.</summary>
    public sealed class FloatingNumber : MonoBehaviour
    {
        public static int ActiveCount { get; private set; }
        public static int MechanismCount { get; private set; }
        private static Font sharedFont;
        private static int fontRevision;
        private static bool watchingFont;
        private static readonly Stack<FloatingNumber> idle=new Stack<FloatingNumber>(40);
        internal static void PrewarmDigits()
        {SyncSharedFont();if(sharedFont!=null){sharedFont.RequestCharactersInTexture("0123456789+-",64,FontStyle.Bold);sharedFont.RequestCharactersInTexture("0123456789+-",64,FontStyle.BoldAndItalic);}}
        internal static System.Collections.IEnumerator Prewarm()
        {
            PrewarmDigits();
            for(int i=idle.Count;i<16;i++)
            {
                var root=new GameObject("Prewarmed combat text");root.SetActive(false);
                var number=root.AddComponent<FloatingNumber>();
                number.criticalNumberStyle=new GUIStyle{alignment=TextAnchor.MiddleCenter,wordWrap=false,padding=new RectOffset(),font=sharedFont,fontSize=64};
                idle.Push(number);yield return null;
            }
        }
        private CombatTextMetrics metrics;
        private static void WatchFont()
        {if(watchingFont)return;Font.textureRebuilt+=OnFontTextureRebuilt;watchingFont=true;}
        private static void UnwatchFont()
        {if(!watchingFont)return;Font.textureRebuilt-=OnFontTextureRebuilt;watchingFont=false;}
        private static void OnFontTextureRebuilt(Font font)
        {if(font==sharedFont)unchecked{fontRevision++;}}
        private static void SyncSharedFont()
        {
            Font resolved=GameFont.Shared;
            if(object.ReferenceEquals(sharedFont,resolved))return;
            sharedFont=resolved;unchecked{fontRevision++;}

        }
        private static readonly List<FloatingNumber> visible=new List<FloatingNumber>(40);
        private static int reflowFrame=-1;
        private Vector3 originAtSpawn;
        private int lane;
        private float life;
        private Color color;
        private bool critical,mechanism,counted;
        private string display;
        private string damageIcon;
        private CombatTextLayout.Box bounds;
        private float Duration {get{return display!=null&&display.StartsWith("等级提升")?4.5f:mechanism?1.8f:critical?1.8f:1.5f;}}
        private static float Density {get{return MobileControls.Active?MobileControls.Layout.Scale:Mathf.Clamp(Screen.dpi>=120?Screen.dpi/163f:Screen.height/1080f,1,3);}}

        public static bool CanSpawn(Vector3 origin,bool isCritical=false)
        {
            FloatingNumber replacement;int selectedLane;Camera camera;CombatTextLayout.Box box;CombatTextMetrics measured;
            return TrySelectAdmission(origin,"1",isCritical,false,out replacement,out selectedLane,out camera,out box,out measured);
        }
        public static FloatingNumber Spawn(Vector3 origin,string value,Color tint,bool isCritical=false,string objectName="Combat Text",bool isMechanism=false,string damageIcon=null)
        {
            string shown=Display(value,isCritical);
            FloatingNumber replacement;int selectedLane;Camera camera;CombatTextLayout.Box box;CombatTextMetrics measured;
            if(!TrySelectAdmission(origin,shown,isCritical,isMechanism,out replacement,out selectedLane,out camera,out box,out measured,damageIcon))return null;
            FloatingNumber number=null;while(idle.Count>0&&number==null)number=idle.Pop();
            if(number==null)number=new GameObject(objectName).AddComponent<FloatingNumber>();
            number.gameObject.name=objectName;number.transform.position=origin;
            number.life=0;number.numberCaption=null;number.captionRevision=-1;
            number.gameObject.SetActive(true);
            number.damageIcon=damageIcon;
            number.InitializeAdmitted(shown,tint,isCritical,isMechanism,replacement,selectedLane,camera,box,measured);
            return number;
        }
        private static string Display(string value,bool critical)
        {
            string shown=value??"";
            double numeric;
            if(double.TryParse(shown.TrimEnd('!','！'),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out numeric))
                return (shown.StartsWith("+")?"+":"")+numeric.ToString("0",System.Globalization.CultureInfo.InvariantCulture);
            // Combat captions are short; detailed explanations stay in the HUD/log.
            if(shown.Length>24)shown=shown.Substring(0,23)+"…";
            return critical?shown.TrimEnd('!','！'):shown;
        }
        private static void FontReady(string value,bool critical)
        {
            SyncSharedFont();
            if(sharedFont!=null)sharedFont.RequestCharactersInTexture(value,64,critical?FontStyle.BoldAndItalic:FontStyle.Bold);
        }
        private static float MeasureAspect(string value,bool critical)
        {
            FontReady(value,critical);float advance=0,min=0,max=0,bottom=float.MaxValue,top=float.MinValue;
            foreach(char c in value)
            {
                CharacterInfo info;
                if(sharedFont!=null&&sharedFont.GetCharacterInfo(c,out info,64,critical?FontStyle.BoldAndItalic:FontStyle.Bold))
                {min=Mathf.Min(min,advance+info.minX);max=Mathf.Max(max,advance+info.maxX);bottom=Mathf.Min(bottom,info.minY);top=Mathf.Max(top,info.maxY);advance+=info.advance;}
                else {advance+=64;max=advance;bottom=Mathf.Min(bottom,0);top=Mathf.Max(top,64);}
            }
            return Mathf.Max(1,max-min)/Mathf.Max(1,top-bottom)+(critical?.16f:0);
        }
        private static Vector2 ScaledSize(float aspect,bool critical)
        {
            float pixels=CombatTextLayout.PixelHeight(EffectPreferences.CombatTextScale,Density,critical);
            float w=aspect*pixels;
            // Critical emphasis comes from the larger glyph, without a backing plate.
            return new Vector2(w,pixels);
        }
        private static bool TrySelectAdmission(Vector3 origin,string value,bool isCritical,bool isMechanism,out FloatingNumber replacement,
            out int selectedLane,out Camera camera,out CombatTextLayout.Box box,out CombatTextMetrics measured,string damageIcon=null)
        {
            replacement=null;selectedLane=0;camera=Camera.main;box=default(CombatTextLayout.Box);measured=default(CombatTextMetrics);
            int globalLimit=MobileControls.Active?20:36;
            if(isMechanism)
            {
                if(MechanismCount>=CombatTextLayout.MechanismLimit)return false;
            }
            else if(ActiveCount-MechanismCount>=globalLimit)
            {
                if(!isCritical)return false;
                foreach(var number in visible)if(number!=null&&!number.critical&&!number.mechanism){replacement=number;break;}
                if(replacement==null)return false;
            }
            Vector3 point=camera!=null?camera.WorldToScreenPoint(origin):Vector3.zero;
            if(camera!=null&&(point.z<=0||point.x<0||point.y<0||point.x>camera.pixelWidth||point.y>camera.pixelHeight))return false;
            int nearby=0;
            foreach(var number in visible)
            {
                if(number==null||number==replacement||number.mechanism!=isMechanism)continue;
                bool close=camera!=null?Vector2.Distance(point,camera.WorldToScreenPoint(number.originAtSpawn))<110*Density:Vector3.Distance(origin,number.originAtSpawn)<2f;
                if(close)nearby++;
            }
            if(nearby>=(isMechanism?4:isCritical?6:4))return false;
            float aspect=MeasureAspect(value,isCritical)+(damageIcon==null?0:.9f);measured=new CombatTextMetrics(aspect,fontRevision);
            Vector2 size=ScaledSize(aspect,isCritical)*(isMechanism?1f:1.22f);
            if(Place(point,size,replacement,null,camera,out selectedLane,out box))return true;
            if(isMechanism)
                foreach(var number in visible)
                    if(number!=null&&!number.mechanism&&Place(point,size,number,null,camera,out selectedLane,out box))
                    {replacement=number;return true;}
            return false;
        }
        private static bool Place(Vector3 point,Vector2 size,FloatingNumber replacement,FloatingNumber self,Camera camera,out int slot,out CombatTextLayout.Box box)
        {
            box=default(CombatTextLayout.Box);
            // Keep an admitted caption in its lane while fading. Camera or
            // font movement must not send it back to the first free lane.
            int preferred=self==null?0:self.lane;
            for(int attempt=0;attempt<CombatTextLayout.CandidateCount;attempt++)
            {
                slot=(preferred+attempt)%CombatTextLayout.CandidateCount;
                box=CombatTextLayout.Candidate(point.x,point.y,size.x,size.y,slot);
                if(camera!=null&&!CombatTextLayout.Fits(box,camera.pixelWidth,camera.pixelHeight))continue;
                bool blocked=camera!=null&&EnemyController.OverlapsHealthLabel(new Rect(box.X,box.Y,box.Width,box.Height),camera);
                foreach(var number in visible)
                {
                    if(number==null||number==replacement||number==self||!number.counted)continue;
                    if(box.Overlaps(number.bounds)){blocked=true;break;}
                }
                if(!blocked)return true;
            }
            slot=0;return false;
        }
        public void Initialize(string value,Color tint,bool isCritical=false)
        {
            if(counted)return;
            string shown=Display(value,isCritical);FloatingNumber replacement;Camera camera;int selectedLane;CombatTextLayout.Box box;CombatTextMetrics measured;
            if(!TrySelectAdmission(transform.position,shown,isCritical,false,out replacement,out selectedLane,out camera,out box,out measured)){Destroy(gameObject);return;}
            InitializeAdmitted(shown,tint,isCritical,false,replacement,selectedLane,camera,box,measured);
        }
        private void InitializeAdmitted(string value,Color tint,bool isCritical,bool isMechanism,FloatingNumber replacement,int selectedLane,Camera camera,CombatTextLayout.Box box,CombatTextMetrics measured)
        {
            if(replacement!=null)replacement.Retire();
            critical=isCritical;mechanism=isMechanism;originAtSpawn=transform.position;lane=selectedLane;display=value;bounds=box;metrics=measured;
            ActiveCount++;if(mechanism)MechanismCount++;counted=true;visible.Add(this);WatchFont();
            color=critical?new Color(.96f,.76f,.43f):tint;
            // Metrics already come from the shared font. Do not create an
            // invisible TextMesh and renderer for a screen-space label.
        }
        private GUIStyle criticalNumberStyle;
        private GUIContent numberCaption;
        private int captionRevision=-1;
        private Vector2 captionSize;
        private void OnGUI()
        {
            if(!counted||Event.current.type!=EventType.Repaint||GameSession.Instance!=null&&GameSession.Instance.InputBlocked)return;
            Matrix4x4 priorMatrix=GUI.matrix;Color priorColor=GUI.color;int priorDepth=GUI.depth;
            GUI.matrix=Matrix4x4.identity;GUI.depth=1;
            float alpha=Mathf.Clamp01((Duration-life)/(display!=null&&display.StartsWith("等级提升")?1f:mechanism?.35f:.45f));
            GUI.color=new Color(1,1,1,alpha);
            Rect burst=new Rect(bounds.X,Screen.height-bounds.Y-bounds.Height,bounds.Width,bounds.Height);
            if(damageIcon!=null)
            {
                float iconSize=Mathf.Min(burst.height*.75f,burst.width*.3f);
                GUI.DrawTexture(new Rect(burst.x,burst.center.y-iconSize*.5f,iconSize,iconSize),UIIconAtlas.Utility(damageIcon),ScaleMode.ScaleToFit);
                burst.x+=iconSize+2*Density;burst.width-=iconSize+2*Density;
            }
            if(criticalNumberStyle==null)criticalNumberStyle=new GUIStyle{alignment=TextAnchor.MiddleCenter,wordWrap=false,padding=new RectOffset()};
            criticalNumberStyle.font=sharedFont;criticalNumberStyle.fontSize=64;
            criticalNumberStyle.fontStyle=critical?FontStyle.BoldAndItalic:FontStyle.Bold;
            GUIContent caption=numberCaption??(numberCaption=new GUIContent(display));
            if(captionRevision!=fontRevision)
            {captionSize=criticalNumberStyle.CalcSize(caption);captionRevision=fontRevision;}
            float scale=Mathf.Max(.001f,Mathf.Min(burst.width/Mathf.Max(1,captionSize.x),burst.height/Mathf.Max(1,captionSize.y)));
            GUI.matrix=Matrix4x4.TRS(new Vector3(burst.center.x,burst.center.y,0),Quaternion.identity,new Vector3(scale,scale,1));
            Rect label=new Rect(-captionSize.x*.5f,-captionSize.y*.5f,captionSize.x,captionSize.y);
            float shadow=Mathf.Max(1,Density*.85f)/scale;
            criticalNumberStyle.normal.textColor=new Color(.015f,.02f,.03f,.85f);
            GUI.Label(new Rect(label.x+shadow,label.y+shadow,label.width,label.height),caption,criticalNumberStyle);
            GUI.Label(new Rect(label.x-shadow,label.y+shadow,label.width,label.height),caption,criticalNumberStyle);
            GUI.Label(new Rect(label.x-shadow,label.y-shadow,label.width,label.height),caption,criticalNumberStyle);
            GUI.Label(new Rect(label.x+shadow,label.y-shadow,label.width,label.height),caption,criticalNumberStyle);
            criticalNumberStyle.normal.textColor=critical?new Color(1f,.76f,.3f):color;
            GUI.Label(label,caption,criticalNumberStyle);
            GUI.matrix=priorMatrix;GUI.color=priorColor;GUI.depth=priorDepth;
        }
        private void Update()
        {
            life+=Time.deltaTime;
            if(life>Duration)Retire();
        }
        private void LateUpdate()
        {
            if(reflowFrame==Time.frameCount)return;reflowFrame=Time.frameCount;Camera camera=Camera.main;if(camera==null)return;SyncSharedFont();
            // Reproject all text together after camera motion. Old bounds remain
            // reserved until replaced, so no pair can overlap during reflow.
            for(int i=visible.Count-1;i>=0;i--)
            {
                var number=visible[i];if(number==null)continue;
                Vector3 point=camera.WorldToScreenPoint(number.originAtSpawn);point.y+=number.life*(number.mechanism?18:30)*Density*EffectPreferences.EffectsScale;
                float aspect;
                if(!number.metrics.TryGet(fontRevision,out aspect))
                {aspect=MeasureAspect(number.display,number.critical)+(number.damageIcon==null?0:.9f);number.metrics=new CombatTextMetrics(aspect,fontRevision);}
                Vector2 size=ScaledSize(aspect,number.critical)*(number.mechanism?1f:number.DamageFloatScale);
                int slot;CombatTextLayout.Box box;
                if(point.z<=0||!Place(point,size,null,number,camera,out slot,out box)){number.Retire();continue;}
                number.lane=slot;number.bounds=box;
            }
        }
        private float DamageFloatScale {get{return Mathf.Lerp(1.22f,.92f,Mathf.Clamp01(life/.18f));}}
        private void ReleaseCount(){if(!counted)return;counted=false;ActiveCount=Mathf.Max(0,ActiveCount-1);if(mechanism)MechanismCount=Mathf.Max(0,MechanismCount-1);visible.Remove(this);if(ActiveCount==0)UnwatchFont();}
        public static void ClearForWorldTransition()
        {
            // Retire immediately so no old-world caption is drawn during travel.
            while(visible.Count>0)
            {
                var number=visible[visible.Count-1];
                if(number==null)visible.RemoveAt(visible.Count-1);else number.Retire();
            }
            reflowFrame=-1;
        }
        private void Retire(){if(!counted)return;ReleaseCount();gameObject.SetActive(false);if(idle.Count<40)idle.Push(this);else Destroy(gameObject);}
        private void OnDisable(){ReleaseCount();}
        private void OnDestroy(){ReleaseCount();}
    }
}
