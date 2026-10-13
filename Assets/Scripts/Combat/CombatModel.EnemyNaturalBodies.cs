using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    public sealed partial class CombatModel
    {
        // Called after the original rig and authored pieces are complete. All
        // Transform identities, action joints, root scale and gameplay sockets
        // retain their original owner; only render geometry is replaced.
        private void ConfigureEnemyNaturalBodies(EnemyKind kind, bool boss)
        {
            if (kind == EnemyKind.Slime) { BuildNaturalSlime(); return; }
            if (kind == EnemyKind.Wisp) { BuildNaturalWisp(); return; }
            bool guardian = kind == EnemyKind.Guardian;
            Color skin = guardian ? new Color(.57f, .39f, .31f) : new Color(.40f, .62f, .28f);
            Material face = Mat(skin, VisualSurface.Skin), cloth = Mat(new Color(.19f, .17f, .22f), VisualSurface.Cloth);
            Material plate = Mat(guardian ? new Color(.35f, .39f, .44f) : new Color(.32f, .24f, .16f), guardian ? VisualSurface.Metal : VisualSurface.Cloth);
            Material leather = Mat(new Color(.24f, .18f, .13f), VisualSurface.Cloth);
            foreach (var filter in GetComponentsInChildren<MeshFilter>(true))
            {
                string name = filter.gameObject.name; Mesh mesh = null; Material material = null;
                switch (name)
                {
                    case "Breastplate": mesh = EnemyNaturalMeshCache.Get("0" + (guardian ? "g" : "n"), () => NaturalActorMeshFactory.ClothTorso("Continuous enemy ribcage and cloth", guardian)); material = cloth; break;
                    case "Head": mesh = EnemyNaturalMeshCache.Get("1" + (guardian ? "g" : "n"), () => NaturalActorMeshFactory.Face(guardian ? "Guardian cheek jaw and brow" : "Goblin cheek jaw and brow", guardian)); material = face; break;
                    case "Pauldrons": filter.GetComponent<Renderer>().enabled = false; break;
                    case "Guardian chest plate": mesh = EnemyNaturalMeshCache.Get("2", () => NaturalActorMeshFactory.FrontShell("Forged convex guardian cuirass with solid rim")); material = plate; break;
                    case "Guardian layered pauldron": mesh = EnemyNaturalMeshCache.Get("3", () => NaturalActorMeshFactory.Shoulder("Low forged guardian shoulder saddle")); material = plate; break;
                    case "Long Ear": mesh = EnemyNaturalMeshCache.Get("4", () => NaturalActorMeshFactory.Horn("Flexible folded goblin ear", true)); material = face; break;
                    case "Stone Horn": case "Guardian crown spike": mesh = EnemyNaturalMeshCache.Get("5", () => NaturalActorMeshFactory.Horn("Curved grown ivory horn")); material = Mat(new Color(.67f, .60f, .47f), VisualSurface.Skin); break;
                    case "Leather Cap": mesh = EnemyNaturalMeshCache.Get("6", () => NaturalActorMeshFactory.Loft("Sagging sewn goblin cap", new[] { NaturalActorMeshFactory.Ring(-.5f, .49f, .49f), NaturalActorMeshFactory.Ring(-.27f, .49f, .47f), NaturalActorMeshFactory.Ring(.10f, .43f, .44f, -.025f), NaturalActorMeshFactory.Ring(.39f, .24f, .30f, -.045f), NaturalActorMeshFactory.Ring(.5f, .02f, .025f, -.08f) }, 28, .018f)); material = leather; break;
                    case "Iron Crown": mesh = EnemyNaturalMeshCache.Get("7", () => NaturalActorMeshFactory.Band("Thick cast crown inner opening")); material = plate; break;
                    case "Belt": mesh = EnemyNaturalMeshCache.Get("8", () => NaturalActorMeshFactory.Band("Fitted leather belt with inner opening")); material = leather; break;
                    case "Buckle": mesh = EnemyNaturalMeshCache.Get("9", () => NaturalActorMeshFactory.FrontShell("Worn rounded cast belt clasp")); material = plate; break;
                    case "Forearm Bracer": mesh = EnemyNaturalMeshCache.Get("10", () => NaturalActorMeshFactory.Loft("Anatomical forearm fitted guard", new[] { NaturalActorMeshFactory.Ring(-1, .34f, .35f), NaturalActorMeshFactory.Ring(-.74f, .37f, .39f), NaturalActorMeshFactory.Ring(.25f, .44f, .45f), NaturalActorMeshFactory.Ring(.84f, .49f, .48f), NaturalActorMeshFactory.Ring(1, .48f, .46f) }, 24, .008f)); material = plate; break;
                    case "Cuff Trim": mesh = EnemyNaturalMeshCache.Get("11", () => NaturalActorMeshFactory.Band("Rolled solid cuff edge")); material = leather; break;
                    case "Shin Guard": mesh = EnemyNaturalMeshCache.Get("12", () => NaturalActorMeshFactory.Loft("Tapered shin guard and curved edges", new[] { NaturalActorMeshFactory.Ring(-1, .32f, .35f), NaturalActorMeshFactory.Ring(-.52f, .35f, .39f, .02f), NaturalActorMeshFactory.Ring(.30f, .45f, .49f, .035f), NaturalActorMeshFactory.Ring(1, .45f, .38f) }, 24, .008f)); material = plate; break;
                    case "Knee Guard": mesh = EnemyNaturalMeshCache.Get("13", () => NaturalActorMeshFactory.FrontShell("Curved solid knee shell")); material = plate; break;
                    case "Boot": mesh = EnemyNaturalMeshCache.Get("14", () => NaturalActorMeshFactory.Boot("Leather ankle and flattened rounded toe")); material = leather; break;
                    case "Neck skin": mesh = EnemyNaturalMeshCache.Get("15", () => NaturalActorMeshFactory.Loft("Continuous throat with jaw attachment", new[] { NaturalActorMeshFactory.Ring(-1, .44f, .45f), NaturalActorMeshFactory.Ring(0, .45f, .48f), NaturalActorMeshFactory.Ring(1, .47f, .48f) }, 24, .004f)); material = face; break;
                    case "Goblin nose": mesh = EnemyNaturalMeshCache.Get("16", () => NaturalActorMeshFactory.Loft("Goblin sculpted nasal bridge", new[] { NaturalActorMeshFactory.Ring(-.5f, .28f, .27f, .02f), NaturalActorMeshFactory.Ring(-.26f, .48f, .49f, .04f), NaturalActorMeshFactory.Ring(.03f, .36f, .42f), NaturalActorMeshFactory.Ring(.5f, .13f, .15f, -.1f) }, 24, .004f)); material = face; break;
                }
                if (mesh != null) ReplaceNaturalMesh(filter, mesh, material);
            }
            // Cloth crosses the shoulder origin so the upper arm emerges from
            // a continuous trunk. Existing sleeve/leg skinning still bends at
            // the actual production elbow and knee joints.
            for (int side = -1; side <= 1; side += 2)
            {
                Transform shoulder = side < 0 ? leftArm : rightArm;
                if (shoulder == null) continue;
                if (!guardian)
                {
                    Transform guard = NaturalBodyPart("Goblin sewn shoulder mantle", shoulder, new Vector3(0, -.02f, -.015f), new Vector3(.39f, .18f, .36f), EnemyNaturalMeshCache.Get("17", () => NaturalActorMeshFactory.Shoulder("Fitted goblin leather shoulder")), leather);
                    guard.localRotation = Quaternion.Euler(0, 0, side * 9);
                }
            }
            if (guardian && headRig != null)
            {
                NaturalBodyPart("Guardian nasal bridge", headRig, new Vector3(0, .145f, .237f), new Vector3(.12f, .15f, .10f), EnemyNaturalMeshCache.Get("18", () => NaturalActorMeshFactory.Face("Worn guardian nose", true)), face);
                for (int side = -1; side <= 1; side += 2)
                    NaturalBodyPart("Guardian inset nostril", headRig, new Vector3(side * .030f, .113f, .282f), new Vector3(.017f, .009f, .006f), EnemyNaturalMeshCache.Get("19", () => NaturalActorMeshFactory.Face("Small recessed nostril")), Mat(new Color(.19f, .115f, .09f), VisualSurface.Skin));
            }
            BuildEnemyContinuousTorso(guardian, cloth);
            foreach(var limb in GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if(limb.name=="Continuous articulated sleeve"||limb.name=="Continuous articulated trousers")
                {
                    limb.sharedMesh=HeroNaturalMeshLibrary.Limb(limb.name.EndsWith("trousers"));
                    limb.sharedMaterial=cloth;
                }
            foreach(var filter in GetComponentsInChildren<MeshFilter>(true))
                if(filter.name=="Glove")ReplaceNaturalMesh(filter,EnemyNaturalMeshCache.Get("hand", () => Instantiate(HeroNaturalMeshLibrary.Hand())),face);
            plate.SetFloat("_Metallic", guardian ? .68f : 0); plate.SetFloat("_Glossiness", guardian ? .38f : .17f);
        }

        private void BuildEnemyContinuousTorso(bool guardian, Material cloth)
        {
            if(spine==null||pelvis==null)return;
            var torso=new GameObject("Continuous fitted enemy body");torso.transform.SetParent(transform,false);
            var renderer=torso.AddComponent<SkinnedMeshRenderer>();var mesh=Instantiate(HeroNaturalMeshLibrary.EnemyTorso(guardian));
            mesh.bindposes=new[]{pelvis.worldToLocalMatrix*torso.transform.localToWorldMatrix,spine.worldToLocalMatrix*torso.transform.localToWorldMatrix};
            torso.AddComponent<OwnedCombatMesh>().Value=mesh;renderer.sharedMesh=mesh;renderer.sharedMaterial=cloth;
            renderer.bones=new[]{pelvis,spine};renderer.rootBone=pelvis;renderer.quality=SkinQuality.Bone2;
            renderer.localBounds=new Bounds(new Vector3(0,1.25f,0),new Vector3(guardian?1.8f:1.4f,1.95f,1.2f));
            if(body!=null){var rigid=body.GetComponent<Renderer>();if(rigid!=null)rigid.enabled=false;}
        }

        private void BuildNaturalSlime()
        {
            if (body == null) return;
            Material gel = Mat(new Color(.39f, .64f, .30f), VisualSurface.Skin);
            gel.SetFloat("_Glossiness", .82f); gel.SetFloat("_Metallic", .035f);
            if (gel.HasProperty("_TextureStrength")) gel.SetFloat("_TextureStrength", .20f);
            ReplaceNaturalMesh(body.GetComponent<MeshFilter>(), EnemyNaturalMeshCache.Get("20", () => NaturalActorMeshFactory.Loft("Continuous asymmetric hydrated slime membrane", new[] { NaturalActorMeshFactory.Ring(-.5f, .34f, .32f), NaturalActorMeshFactory.Ring(-.43f, .48f, .45f), NaturalActorMeshFactory.Ring(-.22f, .50f, .48f), NaturalActorMeshFactory.Ring(.03f, .44f, .41f), NaturalActorMeshFactory.Ring(.25f, .33f, .30f, -.02f), NaturalActorMeshFactory.Ring(.42f, .19f, .17f, -.04f), NaturalActorMeshFactory.Ring(.5f, .025f, .02f, -.045f) }, 36, .018f, "gel")), gel);
            var features = new List<Transform>();
            foreach (Transform child in transform) if (child != body) features.Add(child);
            foreach (Transform child in features)
            {
                Renderer renderer = child.GetComponent<Renderer>();
                if (child.name == "Slime Crown" || child.name == "Slime soft highlight") { if (renderer != null) renderer.enabled = false; continue; }
                if (child.name == "Smile")
                {
                    ReplaceNaturalMesh(child.GetComponent<MeshFilter>(), EnemyNaturalMeshCache.Get("21", () => NaturalActorMeshFactory.CurveTube("Indented curved slime mouth", new[] { new Vector3(-.46f, .24f, 0), new Vector3(-.25f, -.13f, 0), new Vector3(0, -.22f, 0), new Vector3(.25f, -.13f, 0), new Vector3(.46f, .24f, 0) }, .07f)));
                    child.localScale = new Vector3(.24f, .11f, .055f);
                }
                // Eyes and mouth now follow actual squash/stretch of the gel.
                child.SetParent(body, true);
            }
        }

        private void BuildNaturalWisp()
        {
            if (body == null) return;
            Material membrane = Mat(new Color(.37f, .25f, .53f), VisualSurface.Skin);
            membrane.SetFloat("_Glossiness", .58f); membrane.SetColor("_EmissionColor", new Color(.105f, .045f, .18f));
            membrane.EnableKeyword("_EMISSION");
            ReplaceNaturalMesh(body.GetComponent<MeshFilter>(), EnemyNaturalMeshCache.Get("22", () => NaturalActorMeshFactory.Loft("Continuous wisp folded mantle and trailing mass", new[] { NaturalActorMeshFactory.Ring(-1.15f, .014f, .016f, -.21f), NaturalActorMeshFactory.Ring(-.93f, .10f, .10f, -.18f), NaturalActorMeshFactory.Ring(-.62f, .17f, .19f, -.09f), NaturalActorMeshFactory.Ring(-.29f, .33f, .34f), NaturalActorMeshFactory.Ring(-.04f, .48f, .46f), NaturalActorMeshFactory.Ring(.23f, .39f, .34f, -.025f), NaturalActorMeshFactory.Ring(.41f, .19f, .19f, -.07f), NaturalActorMeshFactory.Ring(.5f, .017f, .02f, -.09f) }, 32, .025f)), membrane, false);
            var features = new List<Transform>(); foreach (Transform child in transform) if (child != body) features.Add(child);
            foreach (Transform child in features)
            {
                if (child.name == "Spirit Tail" || child.name == "Spirit Crown") { var renderer = child.GetComponent<Renderer>(); if (renderer != null) renderer.enabled = false; continue; }
                if (child.name == "Spirit swept horn") ReplaceNaturalMesh(child.GetComponent<MeshFilter>(), EnemyNaturalMeshCache.Get("23", () => NaturalActorMeshFactory.Horn("Folded wisp crest")), membrane);
                child.SetParent(body, true);
            }
            NaturalBodyPart("Wisp facial cleft", body, new Vector3(0, -.16f, .445f), new Vector3(.24f, .09f, .055f), EnemyNaturalMeshCache.Get("24", () => NaturalActorMeshFactory.CurveTube("Wisp inset mouth cavity", new[] { new Vector3(-.40f, .10f, 0), new Vector3(0, -.15f, 0), new Vector3(.40f, .10f, 0) }, .11f)), Mat(new Color(.10f, .065f, .14f), VisualSurface.Skin));
        }

        private void ConfigureLargeBossNaturalBodies()
        {
            Material iron = Mat(new Color(.18f, .24f, .29f), VisualSurface.Metal), bronze = Mat(new Color(.43f, .31f, .18f), VisualSurface.Metal);
            foreach (var filter in GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = null; Material material = null;
                switch (filter.name)
                {
                    case "Faceted engine housing": mesh = EnemyNaturalMeshCache.Get("25", () => NaturalActorMeshFactory.Loft("Forged ribbed pressure chassis with curved load path", new[] { NaturalActorMeshFactory.Ring(-1, .31f, .31f), NaturalActorMeshFactory.Ring(-.78f, .43f, .43f), NaturalActorMeshFactory.Ring(-.50f, .49f, .49f), NaturalActorMeshFactory.Ring(.15f, .50f, .50f), NaturalActorMeshFactory.Ring(.51f, .44f, .44f), NaturalActorMeshFactory.Ring(.79f, .37f, .37f), NaturalActorMeshFactory.Ring(1, .32f, .32f) }, 40, .006f)); material = iron; break;
                    case "Engine collar": mesh = EnemyNaturalMeshCache.Get("26", () => NaturalActorMeshFactory.Band("Solid bronze chassis binding rim")); material = bronze; break;
                    case "Radial beam emitter": mesh = EnemyNaturalMeshCache.Get("27", () => NaturalActorMeshFactory.Band("Thick forged emitter with recessed bore", true)); material = bronze; break;
                    case "Exposed star heart": mesh = EnemyNaturalMeshCache.Get("28", () => NaturalActorMeshFactory.Loft("Irregular mineral star heart", new[] { NaturalActorMeshFactory.Ring(-.5f, .07f, .08f), NaturalActorMeshFactory.Ring(-.29f, .30f, .29f), NaturalActorMeshFactory.Ring(-.01f, .49f, .46f), NaturalActorMeshFactory.Ring(.28f, .33f, .34f), NaturalActorMeshFactory.Ring(.5f, .025f, .025f) }, 18, .036f)); break;
                    case "Split crown spindle": mesh = EnemyNaturalMeshCache.Get("29", () => NaturalActorMeshFactory.Horn("Cast tapered crown fin")); material = bronze; break;
                    case "Thigh armour": mesh = EnemyNaturalMeshCache.Get("30", () => NaturalActorMeshFactory.Loft("Curved upper radial limb shell", new[] { NaturalActorMeshFactory.Ring(-1, .29f, .31f), NaturalActorMeshFactory.Ring(-.50f, .41f, .43f), NaturalActorMeshFactory.Ring(.30f, .49f, .47f), NaturalActorMeshFactory.Ring(1, .32f, .31f) }, 28, .007f)); material = iron; break;
                    case "Bronze knee": mesh = EnemyNaturalMeshCache.Get("31", () => NaturalActorMeshFactory.Band("Machined hollow knee bearing")); material = bronze; break;
                    case "Tapered radial shin": mesh = EnemyNaturalMeshCache.Get("32", () => NaturalActorMeshFactory.Loft("Bent load bearing radial shin", new[] { NaturalActorMeshFactory.Ring(-1, .30f, .33f, .035f), NaturalActorMeshFactory.Ring(-.62f, .37f, .40f), NaturalActorMeshFactory.Ring(.22f, .44f, .44f, -.045f), NaturalActorMeshFactory.Ring(1, .45f, .41f, -.07f) }, 28, .007f)); material = iron; break;
                    case "Grounded claw": mesh = EnemyNaturalMeshCache.Get("33", () => NaturalActorMeshFactory.Boot("Broad grounded cast toe and heel")); material = bronze; break;
                    case "Curved shell plate": mesh = EnemyNaturalMeshCache.Get("34", () => NaturalActorMeshFactory.FrontShell("Thick opening pressure petal shell")); material = iron; break;
                    case "Armour crest inlay": mesh = EnemyNaturalMeshCache.Get("35", () => NaturalActorMeshFactory.FrontShell("Embedded bronze petal crest")); material = bronze; break;
                }
                if (mesh != null) ReplaceNaturalMesh(filter, mesh, material);
            }
            iron.SetFloat("_Metallic", .72f); iron.SetFloat("_Glossiness", .34f); bronze.SetFloat("_Metallic", .65f); bronze.SetFloat("_Glossiness", .38f);
        }

        private static void ReplaceNaturalMesh(MeshFilter filter, Mesh mesh, Material material = null, bool retainEnvelope = true)
        {
            if (filter == null || mesh == null) { if (mesh != null && !EnemyNaturalMeshCache.Contains(mesh)) Destroy(mesh); return; }
            bool shared = EnemyNaturalMeshCache.Contains(mesh);
            if (retainEnvelope && filter.sharedMesh != null)
            {
                if (shared) mesh = EnemyNaturalMeshCache.Fit(mesh, filter.sharedMesh.bounds);
                else NaturalActorMeshFactory.FitEnvelope(mesh, filter.sharedMesh.bounds);
            }
            OwnedCombatMesh owner = filter.GetComponent<OwnedCombatMesh>();
            if (owner == null) owner = filter.gameObject.AddComponent<OwnedCombatMesh>(); else if (owner.Value != null) Destroy(owner.Value);
            owner.Value = shared ? null : mesh; filter.sharedMesh = mesh;
            var renderer = filter.GetComponent<Renderer>(); if (material != null && renderer != null) renderer.sharedMaterial = material;
        }

        private static Transform NaturalBodyPart(string name, Transform parent, Vector3 at, Vector3 size, Mesh mesh, Material material)
        {
            var obj = new GameObject(name); obj.transform.SetParent(parent, false); obj.transform.localPosition = at; obj.transform.localScale = size;
            var filter = obj.AddComponent<MeshFilter>(); obj.AddComponent<MeshRenderer>().sharedMaterial = material; ReplaceNaturalMesh(filter, mesh); return obj.transform;
        }
    }
}
