using UnityEngine;

namespace Emberfall
{
    public sealed partial class CombatModel
    {
        // Called after imported weapon modules are resolved, so gameplay, collection
        // preview and production photographs all observe the same finished staff.
        private void RefineContractWeaponDetails(ItemData weapon)
        {
            if (!isHero || heroClass != HeroClass.Summoner || staffRig == null) return;
            var starterDetails = staffRig.Find("Contract living focus");
            if (starterDetails != null) starterDetails.gameObject.SetActive(weapon == null);
            Transform root = weapon == null ? staffRig : equipmentWeapon;
            if (root == null) return;
            Color bark = new Color(.29f, .20f, .115f);
            Color jade = weapon == null ? new Color(.34f, .73f, .47f) : Color.Lerp(new Color(.34f, .73f, .47f), new EquipmentAppearance(weapon).Glow, .22f);
            Material wood = Mat(bark, VisualSurface.Wood), core = Mat(jade, VisualSurface.Crystal);
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                string name = filter.name;
                if (name == "Staff" || name == "Inlaid staff") ContractStaffSurface(filter, "shaft", wood);
                else if (name == "Crystal prong" || name == "Staff Crystal Crown") ContractStaffSurface(filter, "bough", wood);
                else if (name == "Focus crystal" || name == "Arcane Crystal") ContractStaffSurface(filter, "heart", core);
            }
            // Decorations are parent-owned and added once to a particular gear root.
            // Their complete vertices remain inside the existing focus sphere/collar
            // envelope: this changes materials/forms without changing combat reach.
            if (root.Find("Contract living focus") != null) return;
            Transform details = NewJoint("Contract living focus", root, Vector3.zero);
            Vector3 collar = WeaponAnchorLocal(WeaponVisualAnchor.StaffCollar);
            Vector3 focus = WeaponAnchorLocal(WeaponVisualAnchor.StaffCore);
            float diameter = weaponStructure.StaffCoreDiameter;
            float reach = diameter * .30f;
            Color leafColor = Color.Lerp(new Color(.21f, .48f, .25f), jade, .3f);
            for (int side = -1; side <= 1; side += 2)
            {
                var path = new Vector3[7];
                for (int i = 0; i < path.Length; i++)
                {
                    float t = i / (float)(path.Length - 1);
                    path[i] = new Vector3(side * reach * Mathf.Sin(t * Mathf.PI * .54f),
                        Mathf.Lerp(collar.y - .055f, focus.y + diameter * .34f, t),
                        focus.z - diameter * .16f * Mathf.Sin(t * Mathf.PI));
                }
                Mesh fork = NaturalActorMeshFactory.CurveTube("Continuous textured contract staff bough", path, diameter * .070f);
                ContractOwnedPart("Contract focus wooden fork", details, Vector3.zero, Vector3.one, fork, wood);
                Mesh leaf = NaturalActorMeshFactory.Loft("Folded contract staff leaf with raised midrib", new[] {
                    NaturalActorMeshFactory.Ring(0, .008f, .008f), NaturalActorMeshFactory.Ring(.12f, .12f, .06f, .02f),
                    NaturalActorMeshFactory.Ring(.32f, .34f, .14f, .06f), NaturalActorMeshFactory.Ring(.55f, .41f, .18f, .10f),
                    NaturalActorMeshFactory.Ring(.76f, .31f, .13f, .11f), NaturalActorMeshFactory.Ring(.92f, .14f, .05f, .10f),
                    NaturalActorMeshFactory.Ring(1, .005f, .004f, .085f) }, 12, .018f);
                var leafPart = ContractOwnedPart("Contract focus curled leaf", details,
                    focus + new Vector3(side * reach * .67f, -diameter * .28f, diameter * .15f),
                    new Vector3(diameter * .21f, diameter * .55f, diameter * .21f), leaf, Mat(leafColor, VisualSurface.Foliage));
                leafPart.localRotation = Quaternion.Euler(-18, side * 18, -side * 18);
            }
        }
        private void ContractStaffSurface(MeshFilter filter, string kind, Material material)
        {
            var binding = filter.GetComponent<ContractStaffDetailBinding>();
            if (binding == null)
            {
                binding = filter.gameObject.AddComponent<ContractStaffDetailBinding>();
                Mesh shape;
                if (kind == "shaft") shape = NaturalActorMeshFactory.Loft("Gnarled continuous contract staff shaft", new[] {
                    NaturalActorMeshFactory.Ring(-1, .39f, .39f), NaturalActorMeshFactory.Ring(-.84f, .42f, .43f, .025f),
                    NaturalActorMeshFactory.Ring(-.35f, .44f, .42f, -.025f), NaturalActorMeshFactory.Ring(.14f, .46f, .43f, .018f),
                    NaturalActorMeshFactory.Ring(.64f, .49f, .47f, -.02f), NaturalActorMeshFactory.Ring(1, .41f, .40f) }, 18, .045f);
                else if (kind == "bough") shape = NaturalActorMeshFactory.Loft("Tapering carved contract staff tine", new[] {
                    NaturalActorMeshFactory.Ring(-.5f, .39f, .40f), NaturalActorMeshFactory.Ring(-.32f, .48f, .45f, .03f),
                    NaturalActorMeshFactory.Ring(.02f, .40f, .42f, .04f), NaturalActorMeshFactory.Ring(.31f, .27f, .26f, .08f),
                    NaturalActorMeshFactory.Ring(.5f, .025f, .03f, .10f) }, 14, .012f);
                else shape = NaturalActorMeshFactory.Loft("Irregular mineral heart in contract staff", new[] {
                    NaturalActorMeshFactory.Ring(-.5f, .03f, .04f), NaturalActorMeshFactory.Ring(-.30f, .32f, .30f, .015f),
                    NaturalActorMeshFactory.Ring(-.02f, .49f, .43f, -.015f), NaturalActorMeshFactory.Ring(.25f, .35f, .38f, .02f),
                    NaturalActorMeshFactory.Ring(.5f, .02f, .015f) }, 12, .028f);
                if (filter.sharedMesh != null) NaturalActorMeshFactory.FitEnvelope(shape, filter.sharedMesh.bounds);
                binding.Shape = shape;
                var owner = filter.GetComponent<OwnedCombatMesh>();
                if (owner == null) owner = filter.gameObject.AddComponent<OwnedCombatMesh>();
                else if (owner.Value != null) Destroy(owner.Value);
                owner.Value = shape;
            }
            filter.sharedMesh = binding.Shape;
            var renderer = filter.GetComponent<Renderer>(); if (renderer != null) renderer.sharedMaterial = material;
        }
        private static Transform ContractOwnedPart(string name, Transform parent, Vector3 position, Vector3 scale, Mesh mesh, Material material)
        {
            var part = new GameObject(name); part.transform.SetParent(parent, false); part.transform.localPosition = position; part.transform.localScale = scale;
            part.AddComponent<MeshFilter>().sharedMesh = mesh; part.AddComponent<MeshRenderer>().sharedMaterial = material;
            part.AddComponent<OwnedCombatMesh>().Value = mesh; return part.transform;
        }
    }
    internal sealed class ContractStaffDetailBinding : MonoBehaviour { internal Mesh Shape; }
}
