using UnityEngine;
namespace Emberfall
{
    public sealed partial class CombatModel
    {
        private bool heroNaturalBodyBuilt;
        private void BuildHeroNaturalBodies()
        {
            if(heroNaturalBodyBuilt||!isHero||spine==null||pelvis==null)return;heroNaturalBodyBuilt=true;
            // Bind to the accepted bones in their construction pose. No joint,
            // weapon anchor, controller collider or animation curve is moved.
            Color cloth=heroClass==HeroClass.Vanguard?new Color(.24f,.27f,.25f):heroClass==HeroClass.Arcanist?new Color(.27f,.30f,.34f):heroClass==HeroClass.Ranger?new Color(.25f,.31f,.22f):new Color(.29f,.34f,.25f);
            var torso=new GameObject("Continuous fitted hero body");torso.transform.SetParent(transform,false);
            var renderer=torso.AddComponent<SkinnedMeshRenderer>();var mesh=Instantiate(HeroNaturalMeshLibrary.Torso(heroClass));
            mesh.name="Owned natural hero torso / "+heroClass;
            mesh.bindposes=new[]{pelvis.worldToLocalMatrix*torso.transform.localToWorldMatrix,spine.worldToLocalMatrix*torso.transform.localToWorldMatrix};
            torso.AddComponent<OwnedCombatMesh>().Value=mesh;renderer.sharedMesh=mesh;renderer.sharedMaterial=Mat(cloth,VisualSurface.Cloth);renderer.bones=new[]{pelvis,spine};renderer.rootBone=pelvis;renderer.quality=SkinQuality.Bone2;renderer.localBounds=new Bounds(new Vector3(0,1.2f,0),new Vector3(1.4f,1.9f,1.1f));
            if(body!=null){var original=body.GetComponent<Renderer>();if(original!=null)original.enabled=false;}
            foreach(var skin in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if(skin.name=="Continuous articulated sleeve"||skin.name=="Continuous articulated trousers")
                {skin.sharedMesh=HeroNaturalMeshLibrary.Limb(skin.name.EndsWith("trousers"));skin.sharedMaterial=Mat(cloth,VisualSurface.Cloth);}
            }
            RefineNaturalParts(transform,false);
            BuildNaturalFaceAndHair();
            BuildNaturalGarmentDetails(cloth);
            BuildNaturalBootDetails();
        }
        private void RefineHeroNaturalEquipment()
        {
            if(!heroNaturalBodyBuilt)return;
            if(equipmentArmor!=null)RefineNaturalParts(equipmentArmor,true);
            if(equipmentLeftShoulder!=null)RefineNaturalParts(equipmentLeftShoulder,true);
            if(equipmentRightShoulder!=null)RefineNaturalParts(equipmentRightShoulder,true);
            if(equipmentHead!=null)RefineNaturalParts(equipmentHead,true);
        }
        private void RefineNaturalParts(Transform root,bool equipment)
        {
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                Transform part=filter.transform;if(!equipment&&NaturalWeaponPart(part))continue;
                string n=part.name,kind=null;
                if(n=="Head")kind="face";
                else if(n=="Neck skin")kind="neck";
                else if(n=="Collar")kind="gorget";
                else if(n=="Glove")kind="hand";
                else if(n=="Boot")kind="boot";
                else if(n=="Nose")kind="nose";
                else if(n.StartsWith("Hair")){var oldHair=part.GetComponent<Renderer>();if(oldHair!=null)oldHair.enabled=false;continue;}
                else if(n=="Pauldrons"||n=="Single leather shoulder"||n=="Tailored shoulder mantle"||n=="Shoulder shell"||n=="Layered pauldron"||n=="Master hunter raised left guard")kind="shoulder";
                else if(n=="Helmet"||n=="Leather Cap")kind="helmet";
                else if(n=="Pointed Wizard Hat")kind="hat";
                else if(n=="Layered Robe"||n=="Equipped robe skirt")kind="skirt";
                else if(n=="Helmet Crest")kind="hair-lock";
                else if(n=="Forearm Bracer"||n=="Shin Guard")kind="bracer";
                else if(n=="Knee Guard")kind="knee";
                else if(n=="Cuirass"||n=="Raised breastplate"||n=="Helmet Cheek Guard"||n=="Armor Tasset"||n=="Overlapping Armor"||n=="Chest Crest"||n=="Central crest"||n=="Waist plate"||n.Contains("chest wrap")||n.Contains("ritual bib"))kind="plate";
                else if(n=="Cross Body Strap"||n=="Diagonal ranger sash"||n=="Scarf"||n=="Belt"||n=="Nose Guard"||n.Contains("harness")||n.Contains("strap"))kind="strap";
                else if(n=="Leather Vest"||n=="Embroidered Stole"||n=="Robe Hem"||n=="Scarf Tail"||n=="Scholar rear high collar"||n.Contains("robe panel")||n.Contains("robe front panel")||n.Contains("lining")||n.Contains("stole")||n.Contains("shawl tail")||n.Contains("hip tab")||n.Contains("utility flap")||n.Contains("under-robe hem")||n.Contains("collar fin")||n.Contains("Raised scholar collar")||n.Contains("arm guard"))kind="panel";
                if(kind==null)continue;
                filter.sharedMesh=kind=="hand"?HeroNaturalMeshLibrary.Hand():HeroNaturalMeshLibrary.Get(kind);
                if(kind=="neck"||kind=="gorget")part.localScale=new Vector3(part.localScale.x,part.localScale.y*2,part.localScale.z);
                if(!equipment)
                {
                    var surface=part.GetComponent<Renderer>();
                    if(surface!=null&&(kind=="face"||kind=="hand"||kind=="nose"||kind=="neck"))surface.sharedMaterial=Mat(new Color(.79f,.63f,.49f),VisualSurface.Skin);
                    else if(surface!=null&&kind=="boot")surface.sharedMaterial=Mat(new Color(.20f,.17f,.13f),VisualSurface.Cloth);
                }
                // The hat's previous vertices encode metre dimensions directly.
                // Preserve its existing transform and fit the new closed profile
                // to the same authored base width/height inside its mesh instead.
                if(kind=="hat")
                {
                    var shaped=Instantiate(filter.sharedMesh);var vertices=shaped.vertices;
                    for(int i=0;i<vertices.Length;i++){vertices[i].x*=.60f;vertices[i].y*=.62f;vertices[i].z*=.60f;}
                    shaped.vertices=vertices;shaped.RecalculateNormals();shaped.RecalculateTangents();shaped.RecalculateBounds();filter.sharedMesh=shaped;
                    var owned=part.GetComponent<OwnedCombatMesh>();if(owned==null)owned=part.gameObject.AddComponent<OwnedCombatMesh>();else if(owned.Value!=null)Destroy(owned.Value);owned.Value=shaped;
                }
            }
        }
        private bool NaturalWeaponPart(Transform part)
        {
            return swordRig!=null&&part.IsChildOf(swordRig)||staffRig!=null&&part.IsChildOf(staffRig)||bowRig!=null&&part.IsChildOf(bowRig)||fashionWings!=null&&part.IsChildOf(fashionWings)||fashionWeapon!=null&&part.IsChildOf(fashionWeapon)||part==castingOrb;
        }
        private Transform NaturalMeshPart(string name,Transform parent,Vector3 at,Vector3 scale,Mesh mesh,Color color,VisualSurface surface)
        {
            var part=NewJoint(name,parent,at);part.localScale=scale;part.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;part.gameObject.AddComponent<MeshRenderer>().sharedMaterial=Mat(color,surface);return part;
        }
        private void BuildNaturalFaceAndHair()
        {
            if(headRig==null)return;Transform head=headRig.Find("Head");if(head==null)return;
            var lip=headRig.Find("Lower lip");if(lip!=null){lip.localPosition=new Vector3(0,.085f,.217f);lip.localScale=new Vector3(.085f,.017f,.025f);}
            Color hair=heroClass==HeroClass.Arcanist?new Color(.40f,.35f,.28f):heroClass==HeroClass.Summoner?new Color(.20f,.17f,.12f):new Color(.18f,.13f,.095f);
            NaturalMeshPart("Sculpted combed hair",headRig,head.localPosition,head.localScale,HeroNaturalMeshLibrary.Hair(),hair,VisualSurface.Cloth);
            for(int side=-1;side<=1;side+=2)
            {
                Vector3 start=new Vector3(side*.068f,.238f,.245f),mid=new Vector3(side*.105f,.249f,.252f),end=new Vector3(side*.147f,.239f,.238f);
                var brow=HeroNaturalMeshLibrary.Tube("Owned expressive brow",new[]{start,mid,end},new[]{.008f,.011f,.004f},8);
                var part=NaturalMeshPart("Natural brow",headRig,Vector3.zero,Vector3.one,brow,hair,VisualSurface.Cloth);part.gameObject.AddComponent<OwnedCombatMesh>().Value=brow;
                var cheek=HeroNaturalMeshLibrary.Tube("Owned eyelid crease",new[]{new Vector3(side*.076f,.204f,.242f),new Vector3(side*.106f,.219f,.248f),new Vector3(side*.135f,.207f,.239f)},new[]{.004f,.006f,.003f},8);
                part=NaturalMeshPart("Natural eye crease",headRig,Vector3.zero,Vector3.one,cheek,new Color(.56f,.38f,.28f),VisualSurface.Skin);part.gameObject.AddComponent<OwnedCombatMesh>().Value=cheek;
            }
        }
        private void BuildNaturalGarmentDetails(Color cloth)
        {
            Color stitch=Color.Lerp(cloth,new Color(.60f,.55f,.42f),.42f);
            // A continuous raised seam follows the fitted chest, with a real
            // stitched edge and muted inner lining instead of floating rectangles.
            for(int side=-1;side<=1;side+=2)
            {
                var path=new[]{new Vector3(side*.12f,.57f,.105f),new Vector3(side*.22f,.43f,.211f),new Vector3(side*.24f,.19f,.225f),new Vector3(side*.19f,-.12f,.194f)};
                var mesh=HeroNaturalMeshLibrary.Tube("Owned tailored garment seam",path,new[]{.012f,.009f,.009f,.012f},8);
                var seam=NaturalMeshPart("Tailored chest seam",spine,Vector3.zero,Vector3.one,mesh,stitch,VisualSurface.Cloth);seam.gameObject.AddComponent<OwnedCombatMesh>().Value=mesh;
                if(heroClass!=HeroClass.Vanguard)
                {
                    var under=NaturalMeshPart("Folded cloth hip lining",side<0?leftLeg:rightLeg,new Vector3(side*.035f,-.11f,.105f),new Vector3(.23f,.24f,.06f),HeroNaturalMeshLibrary.Get("panel"),cloth*.87f,VisualSurface.Cloth);
                    under.localRotation=Quaternion.Euler(-8,0,side*7);
                }
            }
        }
        private void BuildNaturalBootDetails()
        {
            foreach(var filter in GetComponentsInChildren<MeshFilter>(true))
            {
                if(filter.name!="Boot")continue;Transform boot=filter.transform;
                NaturalMeshPart("Layered stitched leather outsole",boot,new Vector3(0,-.45f,0),new Vector3(1,.10f,1),HeroNaturalMeshLibrary.Get("sole"),new Color(.095f,.084f,.069f),VisualSurface.Cloth);
                for(int i=0;i<3;i++)
                {
                    float y=.11f-i*.10f,edge=i==0?.15f:i==1?.29f:.395f,centre=i==0?.225f:i==1?.355f:.452f;
                    var lace=HeroNaturalMeshLibrary.Tube("Owned curved boot lace",new[]{new Vector3(-.20f,y,edge),new Vector3(0,y,centre),new Vector3(.20f,y,edge)},new[]{.014f,.015f,.014f},8);
                    var part=NaturalMeshPart("Crossed leather boot lace",boot,Vector3.zero,Vector3.one,lace,new Color(.45f,.37f,.25f),VisualSurface.Cloth);part.gameObject.AddComponent<OwnedCombatMesh>().Value=lace;
                }
            }
        }
    }
}
