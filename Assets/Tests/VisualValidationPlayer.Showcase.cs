#if EMBERFALL_VISUAL_VALIDATION
using System.Collections;
using System.Reflection;
using UnityEngine;
namespace Emberfall
{
    public sealed partial class VisualValidationPlayer
    {
        private IEnumerator VerifyHeroShowcases()
        {
            result.captureScope="Native animated hero previews: title, four classes, saved appearance and inventory; isolated saves.";
            yield return SetResolution(1600,900);
            SetField("titleCreatingHero",true);
            yield return Capture("classes-animated-a",1);
            var previews=(IDictionary)typeof(GameUI).GetField("heroShowcases",PrivateInstance).GetValue(ui);
            Check(previews.Count==4,"All four classes own a visible preview");
            object display=previews["class-1"];
            var model=(CollectionModelPreview)display.GetType().GetField("Model",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(display);
            var clock=(CollectionPreviewMotion)typeof(CollectionModelPreview).GetField("motion",PrivateInstance).GetValue(model);
            float before=clock.Time;
            yield return Capture("classes-animated-b",1);
            Check(clock.Time>before+.4f,"Class mannequin animation advances in title screen");
            Check(model.LastError==null,"Class preview has no rendering failure");
            yield return new WaitForSecondsRealtime(2);
            int firstFrame=Time.frameCount;double firstTime=Time.realtimeSinceStartupAsDouble;
            yield return new WaitForSecondsRealtime(5);
            float fps=(float)((Time.frameCount-firstFrame)/(Time.realtimeSinceStartupAsDouble-firstTime));
            Debug.Log("SHOWCASE four-model average FPS="+fps);
            Check(fps>=50,"Four animated class previews sustain at least 50 FPS; observed="+fps);
            SetField("selectedClass",HeroClass.Arcanist);Invoke("StartSelectedHero");yield return WaitForSceneReady();
            session.Player.enabled=false;
            string wingId="fashion-"+(int)FashionSlot.Wings+"-3";
            session.Progression.Profile.fashions.Add(new FashionData{id=wingId,slot=FashionSlot.Wings,rarity=Rarity.Legendary,name="展示羽翼"});
            Check(session.Progression.EquipFashion(wingId),"Large wings are included in the scene composition fixture");
            Check(session.Progression.EquippedFashion(FashionSlot.Wings)!=null,"Saved equipped wings survive profile validation");
            OpenPanel("Inventory");yield return Capture("inventory-ornate-stage",1);
            Check(previews.Count==0,"Hidden menu mannequin stages are disposed");
            var wear=(CollectionModelPreview)typeof(GameUI).GetField("wearModel",PrivateInstance).GetValue(ui);
            Check(wear!=null&&wear.LastError==null,"Inventory mannequin renders on decorative stage");
            for(int rarity=0;rarity<4;rarity++)
            {
                string id="fashion-"+(int)FashionSlot.Wings+"-"+rarity;
                if(!session.Progression.Profile.fashions.Exists(f=>f.id==id))session.Progression.Profile.fashions.Add(new FashionData{id=id,slot=FashionSlot.Wings,rarity=(Rarity)rarity,name="羽翼展示"});
                Check(session.Progression.EquipFashion(id),"Equip wing rarity "+rarity);
                yield return Capture("feather-wings-rarity-"+rarity,.6f);
                var mannequin=(GameObject)typeof(CollectionModelPreview).GetField("avatar",PrivateInstance).GetValue(wear);
                int wingCount=0;foreach(var renderer in mannequin.GetComponentsInChildren<Renderer>(true))if(renderer.name.Contains("wing")||renderer.name.Contains("Wing")){wingCount++;Debug.Log("WING_RENDER "+rarity+" "+renderer.name+" enabled="+renderer.enabled+" active="+renderer.gameObject.activeInHierarchy+" bounds="+renderer.bounds+" shader="+renderer.sharedMaterial.shader.name+" supported="+renderer.sharedMaterial.shader.isSupported);}
                Check(wingCount>=2,"Preview contains both wing renderers for rarity "+rarity);
                SetField("wearPreviewYaw",180f);yield return Capture("feather-wings-back-"+rarity,.3f);SetField("wearPreviewYaw",20f);
            }
            ResetPanels();yield return new WaitForSecondsRealtime(.4f);Invoke("OpenHubService",HubNpcKind.Merchant);yield return Capture("merchant-legendary-frame",1);
            session.Progression.Save();
            string weapon=session.Progression.Equipped(ItemSlot.Weapon)?.id;
            var saves=session.Progression.GetSaveSlots();
            var saved=saves.Find(s=>s.Id==session.Progression.CurrentSlotId);
            Check(saved!=null&&saved.CanLoad&&saved.PreviewWeapon?.id==weapon,"Saved portrait uses saved equipment snapshot");
            ResetPanels();session.QuitToTitle();yield return WaitForSceneReady();
            SetField("titleCreatingHero",false);SetField("saveSlotsDirty",true);
            yield return Capture("home-saved-character",1);
            Check(previews.Contains("home"),"Home displays the recent saved character");
            Invoke("OpenSaveSelection");yield return Capture("save-character-portraits",1);
            Check(previews.Contains("save-"+saved.Id),"Save list displays the corresponding mannequin");
            ResetPanels();MobileControls.SimulationEnabled=true;
            yield return SetResolution(1280,720);
            SetField("titleCreatingHero",true);yield return Capture("mobile-class-models",1);
            Check(previews.Contains("class-3"),"Mobile class page renders all four professions");
            SetField("titleCreatingHero",false);yield return Capture("mobile-home-model",1);
            MobileControls.SimulationEnabled=false;MobileControls.ResetInput();
        }
    }
}
#endif
