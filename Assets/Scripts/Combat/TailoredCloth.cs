using UnityEngine;

namespace Emberfall
{
    // An authored 8x7 double-sided drape, not a cloth physics simulation. Only the
    // single hero cloak deforms; its fixed buffers are reused with no frame allocations.
    internal sealed class TailoredCloth : MonoBehaviour
    {
        private const int Columns = 8, Rows = 7, SideCount = Columns * Rows;
        private Mesh mesh;
        private HeroClass silhouette;
        private Vector3[] vertices;
        private float motion, targetMotion, phase, inertiaPitch, inertiaSide, targetPitch, targetSide;
        public void SetInertia(float pitch,float side) { targetPitch=Mathf.Clamp(pitch,-14,22);targetSide=Mathf.Clamp(side,-12,12); }
        public void SetMotion(float speed, float action) { targetMotion = Mathf.Clamp01(speed * .7f + action * .4f); }
        public void Initialize(Material material, HeroClass hero = HeroClass.Vanguard)
        {
            silhouette = hero;
            vertices = new Vector3[SideCount * 2];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[(Columns - 1) * (Rows - 1) * 12];
            int index = 0;
            for (int side = 0; side < 2; side++)
                for (int y = 0; y < Rows; y++) for (int x = 0; x < Columns; x++)
                {
                    int vertex = side * SideCount + y * Columns + x;
                    uv[vertex] = new Vector2(x / (float)(Columns - 1), y / (float)(Rows - 1));
                    if (x == Columns - 1 || y == Rows - 1 || ((silhouette == HeroClass.Arcanist || silhouette == HeroClass.Summoner) && x == 3)) continue;
                    int a = vertex, b = vertex + 1, c = vertex + Columns, d = c + 1;
                    triangles[index++] = a; triangles[index++] = side == 0 ? c : b; triangles[index++] = side == 0 ? b : c;
                    triangles[index++] = b; triangles[index++] = side == 0 ? c : d; triangles[index++] = side == 0 ? d : c;
                }
            System.Array.Resize(ref triangles, index);
            phase = transform.position.x * .5f;
            Deform(0);
            mesh = new Mesh { name = "Tailored curved cloth", vertices = vertices, uv = uv, triangles = triangles };
            mesh.MarkDynamic(); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
        }
        public void SamplePreview(float time,float action)
        {
            if(mesh==null||float.IsNaN(time)||float.IsInfinity(time))return;
            motion=Mathf.Clamp01(action)*.4f;inertiaPitch=inertiaSide=0;
            Deform(time);mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();
        }
        private void LateUpdate()
        {
            if (mesh == null || Time.deltaTime <= 0) return;
            motion = Mathf.Lerp(motion, targetMotion, 1f - Mathf.Exp(-Time.deltaTime * 9f));
            float inertiaBlend=1f-Mathf.Exp(-Time.deltaTime*7f);
            inertiaPitch=Mathf.Lerp(inertiaPitch,targetPitch,inertiaBlend);
            inertiaSide=Mathf.Lerp(inertiaSide,targetSide,inertiaBlend);
            Deform(Time.time);
            mesh.vertices = vertices; mesh.RecalculateNormals(); mesh.RecalculateBounds();
        }
        private void Deform(float time)
        {
            for (int y = 0; y < Rows; y++) for (int x = 0; x < Columns; x++)
            {
                float length = y / (float)(Rows - 1), across = x / (float)(Columns - 1) * 2f - 1f;
                Vector3 p = VisualMeshRecipes.DrapePoint(across,length,time,motion,phase);
                p = RearSilhouette.Shape(silhouette, p, across, length);
                int index = y * Columns + x;
                p += new Vector3(inertiaSide*.004f,0,-inertiaPitch*.004f)*length*length;
                vertices[index] = p; vertices[index + SideCount] = p + Vector3.back * .018f;
            }
        }
        private void OnDestroy() { if (mesh != null) Destroy(mesh); }
    }
}
