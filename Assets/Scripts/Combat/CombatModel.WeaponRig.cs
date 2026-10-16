using UnityEngine;
namespace Emberfall
{
    public sealed partial class CombatModel
    {
        private WeaponStructure weaponStructure = new WeaponStructure(0);
        // Most callers use these for visual connections. Basic attacks sample the
        // committed release pose once as their physical muzzle, with a cover check;
        // later animation must never move an existing shot or extend melee reach.
        public bool TryGetWeaponVisualAnchor(WeaponVisualAnchor anchor, out Vector3 worldPosition)
        {
            if(pilotVisible && blenderPilot != null && anchor <= WeaponVisualAnchor.SwordTip)
                return blenderPilot.Anchor(anchor,out worldPosition);
            Transform rig = anchor <= WeaponVisualAnchor.SwordTip ? swordRig :
                anchor <= WeaponVisualAnchor.StaffTop ? staffRig : bowRig;
            worldPosition = transform.position;
            if (rig == null || anchor < WeaponVisualAnchor.SwordPommel || anchor > WeaponVisualAnchor.BowArrowRest) return false;
            worldPosition = rig.TransformPoint(WeaponAnchorLocal(anchor));
            return true;
        }
        // A continuous tapered arc keeps the bow/string silhouette readable at gameplay scale.
        // MeshPart owns the mesh; equipment replacement disposes it with its gear root.
        private void CurvedBowLimb(Transform parent, int side, float reach, float width, Color color)
        {
            const int steps=12, sides=6;
            var vertices=new Vector3[(steps+1)*sides+2];
            var triangles=new int[steps*sides*6+sides*6];
            for(int i=0;i<=steps;i++)
            {
                float t=i/(float)steps;
                Vector3 center=new Vector3(0,side*reach*t,.05f+.35f*Mathf.Cos(t*Mathf.PI*.5f));
                Vector3 tangent=new Vector3(0,side*reach,-.35f*Mathf.PI*.5f*Mathf.Sin(t*Mathf.PI*.5f)).normalized;
                Vector3 across=Vector3.Cross(tangent,Vector3.right).normalized;
                float radius=width*.5f*Mathf.Lerp(1,.55f,t);
                for(int j=0;j<sides;j++)
                {
                    float angle=j*Mathf.PI*2/sides;
                    vertices[i*sides+j]=center+radius*(Mathf.Cos(angle)*Vector3.right+Mathf.Sin(angle)*across);
                }
            }
            int index=0;
            for(int i=0;i<steps;i++)for(int j=0;j<sides;j++)
            {
                int a=i*sides+j,b=i*sides+(j+1)%sides,c=a+sides,d=b+sides;
                triangles[index++]=a;triangles[index++]=b;triangles[index++]=c;
                triangles[index++]=b;triangles[index++]=d;triangles[index++]=c;
            }
            int start=vertices.Length-2,end=start+1;
            vertices[start]=new Vector3(0,0,.40f);vertices[end]=new Vector3(0,side*reach,.05f);
            for(int j=0;j<sides;j++)
            {
                triangles[index++]=start;triangles[index++]=(j+1)%sides;triangles[index++]=j;
                triangles[index++]=end;triangles[index++]=steps*sides+j;triangles[index++]=steps*sides+(j+1)%sides;
            }
            MeshPart("Curved bow limb",parent,Vector3.zero,vertices,triangles,color,VisualSurface.Wood);
        }
        // Alternating swing direction is shared by pose and its visual ribbon.
        public int WeaponSwingSide { get { return pilotVisible ? -1 : swingCount % 2 == 0 ? 1 : -1; } }
        private Vector3 WeaponAnchorLocal(WeaponVisualAnchor anchor)
        {
            switch (anchor)
            {
                case WeaponVisualAnchor.SwordPommel: return new Vector3(0, weaponStructure.Tier == 0 ? -.17f : -.2f, 0);
                case WeaponVisualAnchor.SwordGrip: return new Vector3(0, weaponStructure.Tier == 0 ? 0 : -.03f, 0);
                case WeaponVisualAnchor.SwordGuard: return new Vector3(0, weaponStructure.Tier == 0 ? .16f : .17f, 0);
                case WeaponVisualAnchor.SwordRoot: return new Vector3(0, weaponStructure.SwordRoot, 0);
                case WeaponVisualAnchor.SwordTip: return new Vector3(0, weaponStructure.SwordTip, 0);
                case WeaponVisualAnchor.StaffBottom: return new Vector3(0, weaponStructure.StaffBottom, .03f);
                case WeaponVisualAnchor.StaffGrip: return new Vector3(0, 0, .03f);
                case WeaponVisualAnchor.StaffCollar: return new Vector3(0, weaponStructure.StaffCollar, .03f);
                case WeaponVisualAnchor.StaffCore: return new Vector3(0, weaponStructure.StaffCore, .03f);
                case WeaponVisualAnchor.StaffTop: return new Vector3(0, weaponStructure.StaffTop, .03f);
                case WeaponVisualAnchor.BowGrip: return new Vector3(0, 0, .40f);
                case WeaponVisualAnchor.BowUpperTip: return new Vector3(0, weaponStructure.BowReach, .05f);
                case WeaponVisualAnchor.BowLowerTip: return new Vector3(0, -weaponStructure.BowReach, .05f);
                case WeaponVisualAnchor.BowNock: return arrowRig != null ? arrowRig.localPosition : new Vector3(0, 0, .05f);
                case WeaponVisualAnchor.BowArrowRest: return new Vector3(0, 0, .40f);
                default: return Vector3.zero;
            }
        }
    }
}
