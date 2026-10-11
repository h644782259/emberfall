using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    public sealed partial class CombatModel
    {
        // Replace only rendered matter inside each accepted local envelope.
        // The existing trunk pivot, limbs, rank branches and contract sockets
        // remain the owners of all pose and gameplay dimensions.
        private void RefineTreantNaturalPart(MeshFilter filter, string name)
        {
            if (filter == null || filter.sharedMesh == null) return;
            Mesh shape = null;
            switch (name)
            {
                case "Breastplate":
                    shape = NaturalActorMeshFactory.Loft("Continuous grown treant trunk and shoulder roots", new[] {
                        NaturalActorMeshFactory.Ring(-1, .40f, .38f), NaturalActorMeshFactory.Ring(-.76f, .46f, .44f, .025f),
                        NaturalActorMeshFactory.Ring(-.25f, .38f, .43f, -.018f), NaturalActorMeshFactory.Ring(.28f, .50f, .48f, .015f),
                        NaturalActorMeshFactory.Ring(.65f, .45f, .39f, -.03f), NaturalActorMeshFactory.Ring(1, .25f, .27f, -.05f)
                    }, 32, .034f); break;
                case "Head": shape = NaturalActorMeshFactory.Face("Grown bark treant brow and root jaw", true); break;
                case "Pauldrons": shape = NaturalActorMeshFactory.Shoulder("Continuous grown treant shoulder knot"); break;
                case "Sleeve": case "Trouser": case "Treant grown shoulder branch":
                    shape = NaturalActorMeshFactory.Loft("Continuous curved treant branch / " + name, new[] {
                        NaturalActorMeshFactory.Ring(-1, .31f, .33f, .08f), NaturalActorMeshFactory.Ring(-.70f, .38f, .40f, .04f),
                        NaturalActorMeshFactory.Ring(-.22f, .43f, .42f, -.015f), NaturalActorMeshFactory.Ring(.35f, .46f, .45f, -.04f),
                        NaturalActorMeshFactory.Ring(.80f, .43f, .41f, -.02f), NaturalActorMeshFactory.Ring(1, .35f, .34f)
                    }, 24, .026f); break;
                case "Glove": shape = NaturalActorMeshFactory.Hand("Root fingers grown from treant palm"); break;
                case "Boot":
                    shape = NaturalActorMeshFactory.Loft("Continuous treant heel and spreading ground roots", new[] {
                        NaturalActorMeshFactory.Ring(-.5f, .42f, .48f, .025f), NaturalActorMeshFactory.Ring(-.30f, .50f, .50f),
                        NaturalActorMeshFactory.Ring(-.04f, .43f, .41f, -.025f), NaturalActorMeshFactory.Ring(.27f, .32f, .29f, -.14f),
                        NaturalActorMeshFactory.Ring(.5f, .29f, .25f, -.16f)
                    }, 28, .040f); break;
                case "Belt": shape = NaturalActorMeshFactory.Band("Treant grown waist root binding"); break;
                case "Buckle":
                    shape = NaturalActorMeshFactory.Loft("Raised treant bark knot", new[] {
                        NaturalActorMeshFactory.Ring(-.5f, .07f, .07f), NaturalActorMeshFactory.Ring(-.27f, .35f, .31f, .04f),
                        NaturalActorMeshFactory.Ring(.03f, .48f, .46f), NaturalActorMeshFactory.Ring(.31f, .31f, .33f, -.025f),
                        NaturalActorMeshFactory.Ring(.5f, .025f, .03f)
                    }, 24, .028f); break;
                case "Leaf crown": shape = TreantThickLeafCrown(); break;
            }
            if (shape != null) ReplaceNaturalMesh(filter, shape);
        }

        private static Mesh TreantThickLeafCrown()
        {
            var pieces = new List<CombineInstance>();
            var temporary = new List<Mesh>();
            // A branching canopy carries individually curled, closed leaves.
            // Broad tips and a raised midrib replace the old tetrahedral blocks.
            Mesh leaf = NaturalActorMeshFactory.Loft("Thick curled treant leaf with raised midrib", new[] {
                NaturalActorMeshFactory.Ring(0, .005f, .006f), NaturalActorMeshFactory.Ring(.12f, .11f, .030f, .015f),
                NaturalActorMeshFactory.Ring(.30f, .30f, .045f, .042f), NaturalActorMeshFactory.Ring(.53f, .39f, .055f, .075f),
                NaturalActorMeshFactory.Ring(.76f, .29f, .044f, .115f), NaturalActorMeshFactory.Ring(.93f, .12f, .020f, .14f),
                NaturalActorMeshFactory.Ring(1, .004f, .004f, .15f)
            }, 14, .006f);
            temporary.Add(leaf);
            for (int i = 0; i < 15; i++)
            {
                float angle = i * 2.399963f, outward = .08f + (i % 4) * .055f;
                Vector3 stem = new Vector3(Mathf.Cos(angle) * outward, -.27f + (i % 5) * .075f, Mathf.Sin(angle) * outward);
                float tilt = 32 + i % 4 * 13;
                Quaternion rotation = Quaternion.Euler(Mathf.Sin(angle) * tilt, angle * Mathf.Rad2Deg, -Mathf.Cos(angle) * tilt);
                float length = .38f + (i % 3) * .065f;
                pieces.Add(new CombineInstance { mesh = leaf, transform = Matrix4x4.TRS(stem, rotation, new Vector3(length * .82f, length, length)) });
                var path = new[] { new Vector3(0, -.38f, 0), stem * .5f + new Vector3(0, -.12f, 0), stem };
                Mesh stalk = NaturalActorMeshFactory.CurveTube("Continuous young treant leaf stalk", path, .018f);
                temporary.Add(stalk); pieces.Add(new CombineInstance { mesh = stalk, transform = Matrix4x4.identity });
            }
            var result = new Mesh { name = "Natural treant canopy / curled thick leaves and continuous stalks" };
            result.CombineMeshes(pieces.ToArray(), true, true); result.RecalculateBounds();
            foreach (var mesh in temporary) Destroy(mesh);
            return result;
        }
    }
}
