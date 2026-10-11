using UnityEngine;

namespace Emberfall
{
    // Local hit reactions never alter global time, player movement or skill timers.
    public sealed class HitFeedback : MonoBehaviour
    {
        private static float cameraStarted = -10f, cameraStrength, nextCameraTime;
        private static int activeCount;
        private Mesh mesh;
        private Vector3[] vertices;
        private Color[] colors;
        private Camera view;
        private int rayCount, ringSegments;
        private Vector3[] directions;
        private Material material;
        private float age, strength, effectsScale;
        private bool critical,registered;
        private Color coreColor;
        private float Duration { get { return critical ? .27f : .21f; } }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState() { activeCount = 0; ClearCamera(); }

        public static Vector3 CameraOffset
        {
            get
            {
                if (!EffectPreferences.CameraShake) return Vector3.zero;
                float elapsed = Time.time - cameraStarted;
                if (elapsed < 0 || elapsed >= .16f) return Vector3.zero;
                float envelope = Mathf.Sin(elapsed / .16f * Mathf.PI) * (1f - elapsed / .16f);
                return Vector3.up * (envelope * cameraStrength * EffectPreferences.EffectsScale);
            }
        }

        public static void ClearCamera() { cameraStarted = -10f; nextCameraTime = 0; cameraStrength = 0; }

        public static void Spawn(Vector3 point, Vector3 direction, float intensity, bool isCritical = false,CombatVisualPriority priority=CombatVisualPriority.RealContact)
        {
            if (activeCount >= 24) return;
            var obj = new GameObject(isCritical ? "Critical hit impact" : "Hit impact");
            if(CombatVisualLease.Attach(obj,priority)==null)return;
            obj.transform.position = point;
            var impact = obj.AddComponent<HitFeedback>();
            impact.critical = isCritical;
            impact.effectsScale = EffectPreferences.EffectsScale;
            impact.strength = Mathf.Clamp(intensity, isCritical ? 1.6f : .55f, isCritical ? 2.5f : 2f);
            impact.Build(direction);
            if (EffectPreferences.CameraShake && intensity >= 1.5f && Time.time >= nextCameraTime)
            {
                cameraStarted = Time.time;
                cameraStrength = Mathf.Min(isCritical ? .055f : .045f, .022f * intensity);
                nextCameraTime = Time.time + .18f;
            }
        }

        private void Build(Vector3 incoming)
        {
            registered=true;activeCount++;
            material = CombatFx.NewGlow();
            view = Camera.main;
            rayCount = Mathf.Max(3, Mathf.RoundToInt((critical ? 10 : strength >= 1.5f ? 7 : 5) * effectsScale));
            ringSegments = critical && !EffectPreferences.ReducedEffects ? 24 : 0;
            directions = new Vector3[rayCount];
            vertices = new Vector3[(rayCount + ringSegments) * 4];
            colors = new Color[vertices.Length];
            int[] triangles = new int[(rayCount + ringSegments) * 6];
            coreColor = critical ? new Color(1f, .88f, .3f) : new Color(1f, .93f, .72f);
            Vector3 forward = incoming.sqrMagnitude > .01f ? incoming.normalized : Vector3.forward;
            for (int i = 0; i < rayCount; i++)
            {
                float angle = i * Mathf.PI * 2 / rayCount;
                directions[i] = (new Vector3(Mathf.Cos(angle), .25f + Mathf.Sin(angle) * .65f, Mathf.Sin(angle)) + forward * .55f).normalized;
            }
            for (int i = 0; i < rayCount + ringSegments; i++)
            {
                int v = i * 4, t = i * 6;
                triangles[t] = v; triangles[t+1] = v+1; triangles[t+2] = v+2;
                triangles[t+3] = v+2; triangles[t+4] = v+1; triangles[t+5] = v+3;
            }
            // One renderer replaces up to twelve individual spark renderers per hit.
            mesh = new Mesh { name = critical ? "Critical impact fan" : "Impact fan" };
            mesh.MarkDynamic();
            UpdateGeometry(0);
            mesh.triangles = triangles;
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private void UpdateGeometry(float t)
        {
            Vector3 cameraForward = view != null ? view.transform.forward : Vector3.forward;
            Vector3 cameraRight = view != null ? view.transform.right : Vector3.right;
            Vector3 cameraUp = view != null ? view.transform.up : Vector3.up;
            float ease = 1f - Mathf.Pow(1f-t,3f), fade = (1f-t)*(1f-t);
            for (int i=0;i<rayCount;i++)
            {
                Vector3 direction = directions[i];
                Vector3 side = Vector3.Cross(direction,cameraForward).normalized;
                if (side.sqrMagnitude < .01f) side = cameraRight;
                float reach = (.12f+ease*(critical?.95f:.68f)*effectsScale)*Mathf.Sqrt(strength);
                Vector3 start = direction*reach*Mathf.Lerp(.1f,.92f,t);
                Vector3 end = direction*reach;
                float width = (critical?.058f:.043f)*Mathf.Sqrt(strength)*(1f-t);
                int v=i*4;
                vertices[v]=start-side*width; vertices[v+1]=start+side*width;
                vertices[v+2]=end-side*width*.03f; vertices[v+3]=end+side*width*.03f;
                Color inside=Color.Lerp(coreColor,Color.white,.5f); inside.a=fade;
                Color outside=critical?new Color(1f,.38f,.06f,0):new Color(1f,.64f,.23f,0);
                colors[v]=colors[v+1]=inside; colors[v+2]=colors[v+3]=outside;
            }
            float ringRadius=.12f+ease*.54f*effectsScale, ringWidth=.022f*(1f-t);
            for(int i=0;i<ringSegments;i++)
            {
                float a=i*Mathf.PI*2/ringSegments,b=(i+1)*Mathf.PI*2/ringSegments;
                Vector3 first=cameraRight*Mathf.Cos(a)+cameraUp*Mathf.Sin(a);
                Vector3 next=cameraRight*Mathf.Cos(b)+cameraUp*Mathf.Sin(b);
                int v=(rayCount+i)*4;
                vertices[v]=first*(ringRadius-ringWidth); vertices[v+1]=first*(ringRadius+ringWidth);
                vertices[v+2]=next*(ringRadius-ringWidth); vertices[v+3]=next*(ringRadius+ringWidth);
                Color color=coreColor; color.a=fade*.6f;
                colors[v]=colors[v+1]=colors[v+2]=colors[v+3]=color;
            }
            mesh.vertices=vertices; mesh.colors=colors; mesh.RecalculateBounds();
        }

        private void Update()
        {
            if (GameSession.Instance == null || !GameSession.Instance.HasStarted) { Destroy(gameObject); return; }
            age += Time.deltaTime;
            if (age >= Duration) { Destroy(gameObject); return; }
            float t = age / Duration;
            UpdateGeometry(t);
        }

        private void Release(){if(!registered)return;registered=false;activeCount=Mathf.Max(0,activeCount-1);}
        private void OnDisable(){Release();}
        private void OnDestroy() { Release(); if (material != null) Destroy(material); if(mesh!=null)Destroy(mesh); }
    }
}
