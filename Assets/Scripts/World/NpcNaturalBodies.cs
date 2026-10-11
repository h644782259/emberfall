using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    // Static settlement figures retain their working arm pivots, tools,
    // navigation clearance and shop interaction positions.
    internal static class NpcNaturalBodies
    {
        internal static void Apply(Transform root, int index, Vector3 at, WorldResources resources)
        {
            var palette = root.gameObject.AddComponent<NpcNaturalBodyMaterials>();
            Material cloth = palette.Get(index == 0 ? new Color(.38f, .29f, .20f) : new Color(.25f, .29f, .33f), VisualSurface.Cloth);
            Material skin = palette.Get(new Color(.68f, .50f, .37f), VisualSurface.Skin);
            Material leather = palette.Get(new Color(.20f, .145f, .105f), VisualSurface.Cloth);
            Material dark = palette.Get(new Color(.075f, .058f, .045f), VisualSurface.Skin);
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = null; Material material = null;
                switch (filter.name)
                {
                    case "NPC tunic": mesh = NaturalActorMeshFactory.ClothTorso("Sewn continuous work tunic and shoulder yoke", index == 1); material = cloth; break;
                    case "NPC head": mesh = NaturalActorMeshFactory.Face("Human brow cheek jaw and chin", index == 1); material = skin; break;
                    case "NPC sleeve": mesh = NaturalActorMeshFactory.Loft("Working sleeve with elbow folds", new[] { NaturalActorMeshFactory.Ring(-1, .29f, .30f), NaturalActorMeshFactory.Ring(-.66f, .34f, .36f), NaturalActorMeshFactory.Ring(-.12f, .44f, .43f), NaturalActorMeshFactory.Ring(.55f, .49f, .46f), NaturalActorMeshFactory.Ring(1, .38f, .39f) }, 24, .023f); material = cloth; break;
                    case "NPC boots": mesh = NaturalActorMeshFactory.Boot("Worn fitted calf boot and rounded toe", true); material = leather; break;
                    case "NPC hand": mesh = NaturalActorMeshFactory.Hand("Working palm thumb and four fingers"); material = skin; break;
                    case "Merchant cap": mesh = NaturalActorMeshFactory.Loft("Sewn folded merchant cap", new[] { NaturalActorMeshFactory.Ring(-1, .46f, .47f), NaturalActorMeshFactory.Ring(-.53f, .49f, .49f), NaturalActorMeshFactory.Ring(.28f, .40f, .42f, -.03f), NaturalActorMeshFactory.Ring(.76f, .28f, .30f, -.065f), NaturalActorMeshFactory.Ring(1, .08f, .08f, -.09f) }, 28, .021f); material = cloth; break;
                    case "Smith leather apron": mesh = NaturalActorMeshFactory.FrontShell("Thick hanging leather apron with folds", true); material = leather; break;
                }
                if (mesh == null) continue;
                if (filter.sharedMesh != null) NaturalActorMeshFactory.FitEnvelope(mesh, filter.sharedMesh.bounds);
                resources.Own(mesh); filter.sharedMesh = mesh; filter.GetComponent<Renderer>().sharedMaterial = material;
            }
            // Small actual anatomical features add a face to the existing head,
            // while the main skull remains one continuous sculpted surface.
            for (int side = -1; side <= 1; side += 2)
            {
                Add(root, resources, "NPC inset eye", at + new Vector3(side * .064f, 1.696f, .226f), new Vector3(.043f, .020f, .010f), NaturalActorMeshFactory.Face("Small almond eye socket"), dark);
                Add(root, resources, "NPC lower eyelid", at + new Vector3(side * .064f, 1.683f, .221f), new Vector3(.060f, .015f, .017f), NaturalActorMeshFactory.Face("Raised eyelid tissue"), skin);
                Add(root, resources, "NPC ear cartilage", at + new Vector3(side * .219f, 1.676f, -.025f), new Vector3(.050f, .090f, .060f), NaturalActorMeshFactory.Face("Folded outer ear"), skin);
            }
            Add(root, resources, "NPC nasal bridge", at + new Vector3(0, 1.659f, .214f), new Vector3(.070f, .107f, .085f), NaturalActorMeshFactory.Face("Sculpted nose bridge and tip"), skin);
            Add(root, resources, "NPC mouth cleft", at + new Vector3(0, 1.591f, .205f), new Vector3(.089f, .020f, .023f), NaturalActorMeshFactory.CurveTube("Inset natural mouth cleft", new[] { new Vector3(-.42f, .08f, 0), new Vector3(0, -.10f, 0), new Vector3(.42f, .08f, 0) }, .07f), dark);
            Add(root, resources, "NPC tailored waist band", at + new Vector3(0, .758f, 0), new Vector3(index == 1 ? .71f : .60f, .084f, index == 1 ? .54f : .46f), NaturalActorMeshFactory.Band("Fitted belt with thick leather edges"), leather);
            if (index == 1)
            {
                Material hair = palette.Get(new Color(.22f, .17f, .12f), VisualSurface.Cloth);
                Add(root, resources, "Smith fitted beard", at + new Vector3(0, 1.51f, .145f), new Vector3(.25f, .18f, .13f), NaturalActorMeshFactory.Loft("Natural beard tapered locks", new[] { NaturalActorMeshFactory.Ring(-.5f, .08f, .10f), NaturalActorMeshFactory.Ring(-.25f, .29f, .29f), NaturalActorMeshFactory.Ring(.04f, .49f, .44f), NaturalActorMeshFactory.Ring(.5f, .42f, .35f) }, 28, .026f), hair);
            }
        }

        private static void Add(Transform parent, WorldResources resources, string name, Vector3 at, Vector3 size, Mesh mesh, Material material)
        {
            var part = new GameObject(name); part.transform.SetParent(parent, false); part.transform.localPosition = at; part.transform.localScale = size;
            part.AddComponent<MeshFilter>().sharedMesh = mesh; part.AddComponent<MeshRenderer>().sharedMaterial = material; resources.Own(mesh);
        }
    }

    internal sealed class NpcNaturalBodyMaterials : MonoBehaviour
    {
        private readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        internal Material Get(Color color, VisualSurface surface)
        {
            string key = ColorUtility.ToHtmlStringRGBA(color) + (int)surface; Material material;
            if (materials.TryGetValue(key, out material)) return material;
            Shader shader = ActorSurfaceMaterial.ShaderFor(surface) ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            material = new Material(shader) { color = color, name = "Owned settlement actor / " + surface };
            ProceduralVisuals.ApplySurface(material, surface); ActorSurfaceMaterial.Apply(material, surface, color); materials.Add(key, material); return material;
        }
        private void OnDestroy() { foreach (Material material in materials.Values) if (material != null) Destroy(material); materials.Clear(); }
    }
}
