#if EMBERFALL_VISUAL_VALIDATION
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    public sealed partial class VisualValidationPlayer
    {
        private IEnumerator VerifyCombatFlow()
        {
            result.captureScope="Live player impact physics, blink escape, and Redrock final-room automatic transition/settlement; isolated saves.";
            yield return SetResolution(1600,900);SetField("selectedClass",HeroClass.Arcanist);Invoke("StartSelectedHero");yield return WaitForSceneReady();
            foreach(var enemy in session.Enemies)if(enemy!=null)enemy.gameObject.SetActive(false);session.Enemies.Clear();
            var p=session.Player;session.Progression.Profile.level=100;p.RefreshStats(true);
            p.Teleport(WorldTraversal.NearestWalkable(new Vector3(0,0,-5),.45f));
            yield return new WaitForSeconds(.7f); // Scene-entry protection must expire before the hit fixture.
            Vector3 before=p.transform.position;float hp=p.Health;
            p.TakeDamageFrom(30,"Light test",before+Vector3.forward,PlayerHitReaction.Light);
            Check(p.Health<hp&&p.HurtFlash>0&&!p.ImpactControlled&&p.transform.position==before,"Light hit gives feedback without taking movement control");
            yield return new WaitForSeconds(.3f);
            p.TakeDamageFrom(30,"Push test",p.transform.position+Vector3.forward,PlayerHitReaction.Push);
            Check(p.ImpactControlled,"Heavy contact owns a short recoil");
            yield return new WaitForSeconds(.3f);
            float moved=Vector3.Distance(before,p.transform.position);
            Check(moved>.1f&&moved<1f&&!p.ImpactControlled,"Small push travels a bounded distance and restores control");
            p.TakeDamageFrom(30,"Repeated push",p.transform.position+Vector3.forward,PlayerHitReaction.Push);
            Check(!p.ImpactControlled,"Repeated hits cannot chain-lock player");
            yield return new WaitForSeconds(1);
            float ground=p.transform.position.y;
            p.TakeDamageFrom(30,"Slam test",p.transform.position+Vector3.forward,PlayerHitReaction.Launch);
            yield return new WaitForSeconds(.3f);
            Check(p.transform.position.y>ground+.4f&&p.ImpactControlled,"Slam launches authoritative body and visible model");
            yield return Capture("player-hit-airborne",0);
            session.SetPaused(true);float pausedHeight=p.transform.position.y;yield return new WaitForSecondsRealtime(.2f);
            Check(p.transform.position.y==pausedHeight,"Pause freezes airborne reaction");session.SetPaused(false);
            yield return new WaitForSeconds(1.2f);
            Check(!p.ImpactControlled&&Mathf.Abs(p.transform.position.y-ground)<.1f,"Gravity lands player and restores movement");
            yield return new WaitForSeconds(.3f);
            p.TakeDamageFrom(30,"Escape slam",p.transform.position+Vector3.forward,PlayerHitReaction.Launch);yield return new WaitForSeconds(.15f);
            Check(p.TryBlink(Vector3.back)&&!p.ImpactControlled,"Blink escapes launched reaction");
            hp=p.Health;p.TakeDamageFrom(30,"Invulnerable slam",p.transform.position+Vector3.forward,PlayerHitReaction.Launch);
            Check(p.Health==hp&&!p.ImpactControlled,"Invulnerable blink rejects both damage and reaction");
            yield return new WaitForSeconds(.5f);
            // Drive a real guardian through anticipation, then evade before contact.
            p.Teleport(WorldTraversal.NearestWalkable(new Vector3(0,0,-5),.45f));
            yield return new WaitForSeconds(.7f);
            var guardianRoot=new GameObject("Control warning fixture");guardianRoot.transform.position=p.transform.position+Vector3.forward*1.6f;
            var guardian=guardianRoot.AddComponent<EnemyController>();guardian.Initialize(session,EnemyKind.Guardian,1);
            session.Enemies.Add(guardian);
            var enemyType=typeof(EnemyController);var attackType=enemyType.GetNestedType("AttackType",System.Reflection.BindingFlags.NonPublic);
            enemyType.GetMethod("PrepareAttack",PrivateInstance).Invoke(guardian,new object[]{System.Enum.Parse(attackType,"Slam"),false,p.transform.position});
            Check(guardian.AttackWindupRemaining>=1.35f,"Guardian control attack allows at least 1.35 seconds to react");
            hp=p.Health;yield return new WaitForSeconds(.75f);
            Check(p.Health==hp&&!p.ImpactControlled&&guardian.IsPreparingAttack,"Visible windup cannot apply early damage or control");
            Check(guardian.GetType().GetField("telegraph",PrivateInstance).GetValue(guardian)!=null,"Control windup owns a visible ground warning");
            yield return Capture("guardian-control-windup",0);
            typeof(PlayerController).GetField("dodgeCooldown",PrivateInstance).SetValue(p,0f);
            Check(p.TryBlink(Vector3.back),"Player can blink during enemy anticipation");
            yield return new WaitForSeconds(.8f);
            Check(p.Health==hp&&!p.ImpactControlled,"Blinking outside locked slam footprint avoids damage and launch");
            session.Enemies.Remove(guardian);Destroy(guardianRoot);
            var bossRoot=new GameObject("Boss paralysis fixture");bossRoot.transform.position=p.transform.position+Vector3.forward*1.6f;
            var boss=bossRoot.AddComponent<EnemyController>();boss.Initialize(session,EnemyKind.Guardian,100,true);session.Enemies.Add(boss);
            enemyType.GetMethod("PrepareAttack",PrivateInstance).Invoke(boss,new object[]{System.Enum.Parse(attackType,"Slam"),false,p.transform.position});
            Check(boss.AttackWindupProgress<.01f,"Boss charge bar begins empty");
            yield return new WaitForSeconds(.25f);Check(boss.AttackWindupProgress>0&&boss.AttackWindupProgress<1,"Boss charge progress follows actual windup");
            Check(boss.TrySkillInterrupt(p,0,p.NewCastId())&&boss.IsBossParalyzed&&!boss.IsPreparingAttack,"Control interrupts major attack into paralysis");
            Vector3 bossAt=boss.transform.position;float bossHp=boss.Health;
            boss.TakeDamage(100,Vector3.zero,impact:false);Check(Mathf.Abs((bossHp-boss.Health)-135)<.1f,"Paralysis grants exactly 35 percent vulnerability");
            yield return new WaitForSeconds(.5f);Check(boss.transform.position==bossAt&&!boss.IsPreparingAttack,"Paralyzed boss neither navigates nor attacks");
            session.SetPaused(true);float paralysis=boss.BossParalysisRemaining;yield return new WaitForSecondsRealtime(.2f);Check(boss.BossParalysisRemaining==paralysis,"Pause freezes paralysis");session.SetPaused(false);
            yield return Capture("boss-paralyzed-top-panel",0);
            yield return new WaitForSeconds(4);Check(!boss.IsBossParalyzed,"Boss paralysis expires");
            session.Enemies.Remove(boss);Destroy(bossRoot);
            var service=session.Progression;service.Profile.gold=100000;
            int stones=service.Profile.revivalStones;
            var quote=service.PrepareRevivalPurchase(true,2);
            Check(quote!=null&&!service.BuyAtMerchant(quote,false)&&service.Profile.gold==100000,"Revival purchase rejected outside merchant without charging");
            quote=service.PrepareRevivalPurchase(true,2);
            Check(service.BuyAtMerchant(quote,true)&&service.Profile.gold==90000&&service.Profile.revivalStones==stones+2,"Revival merchant charges exactly 5000 gold per stone");
            Check(!service.BuyAtMerchant(quote,true),"Stale purchase cannot grant duplicate stones");
            service.Profile.chapterDifficultyBestTiers[2]=2;
            stones=service.Profile.revivalStones;
            Check(service.ClaimAchievement("revival/0")&&service.Profile.revivalStones==stones+6,"Two first-clear tiers grant six stones including historical saves");
            Check(!service.ClaimAchievement("revival/0"),"Repeated tier claim cannot grant stones twice");
            service.Profile.chapterDifficultyBestTiers[2]=3;
            Check(service.ClaimAchievement("revival/0")&&service.Profile.revivalStones==stones+9,"Next new tier grants three more stones");
            int gearCount=service.Profile.inventory.Count;int goldBefore=service.Profile.gold;
            Check(service.ClaimAchievement("level/100")&&service.Profile.gold==goldBefore+15000&&service.Profile.inventory.Count==gearCount+1,"Hard achievement grants increased gold and real equipment atomically");
            Check(service.Profile.inventory[service.Profile.inventory.Count-1].rarity==Rarity.Legendary,"Level 100 achievement equipment is legendary");
            Check(!service.ClaimAchievement("level/100"),"Equipment achievement cannot be claimed twice");
            service.Profile.clearedRuns=50;
            Check(service.ClaimAchievement("clears/50")&&service.Profile.fashions.Exists(f=>f.id=="fashion-0-3"),"Difficult clear milestone grants legendary wings");
            Check(ProgressionService.AffixReforgeStonePrice==20000&&AdventureRewardRules.EnemyEquipmentChance(false,false)==5&&AdventureRewardRules.EnemyEquipmentChance(false,true)==5,"Updated prices and five-percent non-boss loot chance");
            int[] rarityCounts=new int[4];for(int roll=0;roll<100;roll++)rarityCounts[(int)TierRewardRules.DropRarity(false,10,roll)]++;
            Check(rarityCounts[0]==65&&rarityCounts[1]==25&&rarityCounts[2]==9&&rarityCounts[3]==1,"Equipment quality uses exact 65/25/9/1 distribution");
            service.Profile.mechanicMaterials=1000;
            Check(!service.BuyGemBag(ItemSlot.Weapon,false)&&service.Profile.mechanicMaterials==1000,"Gem bag requires merchant access");
            for(int slot=0;slot<3;slot++)
            {
                int funds=service.Profile.mechanicMaterials;
                Check(service.BuyGemBag((ItemSlot)slot,true)&&service.Profile.mechanicMaterials==funds-100&&service.Profile.gemBags[slot]==1,"Gem bag costs 100 shards for slot "+slot);
                string outcome;Check(service.OpenGemBag((ItemSlot)slot,out outcome)&&service.Profile.gemBags[slot]==0&&outcome.Length>0,"Open owned gem bag exactly once for slot "+slot);
                Check(!service.OpenGemBag((ItemSlot)slot,out outcome),"Empty bag cannot grant another gem");
            }
            int[] bagRarities=new int[4];for(int roll=0;roll<100;roll++)bagRarities[(int)ProgressionService.GemBagRarity(roll)]++;
            Check(bagRarities[0]==45&&bagRarities[1]==30&&bagRarities[2]==20&&bagRarities[3]==5,"Gem bags use exact disclosed rarity odds");
            foreach(HeroClass hero in System.Enum.GetValues(typeof(HeroClass)))
            {var catalog=BuildCatalog.GemsFor(hero);Check(catalog.Length==15&&System.Array.TrueForAll(catalog,BuildCatalog.IsAttributeGem),"All classes expose only fifteen attribute gems: "+hero);}
            var retired=new GameProfile{mechanicMaterials=0};
            retired.attachments.Add(new MechanicAttachment{mechanic=EquipmentMechanic.CinderTrail,rarity=Rarity.Legendary,upgradeRank=6,ascensionRank=2});
            var normalize=typeof(ProgressionService).GetMethod("NormalizeAttachments",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
            normalize.Invoke(null,new object[]{retired});
            Check(retired.attachments.Count==0&&retired.mechanicMaterials==108,"Retired mechanism gem returns purchase and upgrade investment");
            normalize.Invoke(null,new object[]{retired});Check(retired.mechanicMaterials==108,"Retired gem migration does not pay twice");
            service.Profile.mechanicMaterials=1000;
            foreach(var kind in new[]{EquipmentMechanic.WeaponFlow,EquipmentMechanic.WeaponTempo,EquipmentMechanic.ArmorFlow,EquipmentMechanic.ArmorStride,EquipmentMechanic.RelicVitality,EquipmentMechanic.RelicWard})
            {
                foreach(var other in service.Profile.attachments)other.mounted=false;
                var gem=service.Attachment(kind);if(gem==null){gem=new MechanicAttachment{mechanic=kind,rarity=Rarity.Legendary,ascensionRank=0};service.Profile.attachments.Add(gem);}
                gem.mounted=true;gem.upgradeRank=2;gem.ascensionRank=0;gem.variant=0;
                int funds=service.Profile.mechanicMaterials;
                Check(!service.AscendAttachment(kind,true)&&service.Profile.mechanicMaterials==funds,"Ascension requires three ranks: "+kind);
                Check(service.UpgradeAttachment(kind,true)&&service.Attachment(kind).upgradeRank==3,"New gem upgrades: "+kind);
                Check(service.AscendAttachment(kind,true)&&service.Attachment(kind).ascensionRank==1,"New gem ascends after third rank: "+kind);
                var stats=service.GetStats();var slot=BuildCatalog.MechanicSlot(kind);
                Check(slot==ItemSlot.Weapon?stats.GemHealthyDamage>=.05f:slot==ItemSlot.Armor?stats.GemLowHealthGuard>=.05f:stats.GemLowEnergyRecovery>=.2f,"Ascended new gem affects combat stats: "+kind);
                Check(service.ToggleAttachmentVariant(kind,true),"New gem supports alternate ascension form: "+kind);
                stats=service.GetStats();Check(slot==ItemSlot.Weapon?stats.GemLowHealthDamage>=.075f:slot==ItemSlot.Armor?stats.GemHealthyGuard>=.05f:stats.GemHighEnergyRecovery>=.2f,"Alternate form affects combat stats: "+kind);
            }
            foreach(var kind in BuildCatalog.GemsFor(service.Profile.heroClass))if(BuildCatalog.MechanicSlot(kind)==ItemSlot.Weapon)
            {var gem=service.Attachment(kind);if(gem==null){gem=new MechanicAttachment{mechanic=kind};service.Profile.attachments.Add(gem);}gem.rarity=Rarity.Legendary;gem.upgradeRank=9;gem.ascensionRank=3;gem.mounted=false;}
            service.Profile.gemBags[0]=1;int beforeDuplicate=service.Profile.mechanicMaterials;string duplicateResult;
            Check(service.OpenGemBag(ItemSlot.Weapon,out duplicateResult)&&duplicateResult.Contains("重复转为")&&service.Profile.mechanicMaterials>beforeDuplicate,"Duplicate bag returns disclosed shards");
            foreach(var gem in service.Profile.attachments)if(BuildCatalog.MechanicSlot(gem.mechanic)==ItemSlot.Weapon)Check(gem.upgradeRank==9&&gem.ascensionRank==3,"Duplicate bag preserves gem investment");
            foreach(var other in service.Profile.attachments)other.mounted=false;
            OpenPanel("Skills");SetField("blinkFollowupSelected",true);yield return Capture("blink-followup-skill-tree",0);ResetPanels();
            Invoke("OpenProgressionGoals");yield return Capture("achievement-rewards",0);
            var cachedRows=GetField("achievementRows");Invoke("RefreshAchievementCache");Check(object.ReferenceEquals(cachedRows,GetField("achievementRows")),"Achievement cache reuses rows across redraws");
            int achievementStart=Time.frameCount;double achievementTime=Time.realtimeSinceStartupAsDouble;yield return new WaitForSecondsRealtime(2);
            Debug.Log("ACHIEVEMENT_PANEL_FPS="+((Time.frameCount-achievementStart)/(Time.realtimeSinceStartupAsDouble-achievementTime)).ToString("F1"));ResetPanels();
            MobileControls.SimulationEnabled=true;yield return SetResolution(1280,720);
            OpenPanel("Skills");SetField("blinkFollowupSelected",true);yield return Capture("mobile-blink-followup-tree",0);ResetPanels();
            Invoke("OpenProgressionGoals");yield return Capture("mobile-achievement-rewards",0);ResetPanels();
            MobileControls.SimulationEnabled=false;yield return SetResolution(1600,900);
            foreach(var gemKind in new[]{EquipmentMechanic.RelicPower,EquipmentMechanic.RelicPrecision,EquipmentMechanic.RelicRuin})
            {
                var fixture=service.Attachment(gemKind);
                if(fixture==null){fixture=new MechanicAttachment{mechanic=gemKind};service.Profile.attachments.Add(fixture);}
                fixture.mounted=true;fixture.rarity=Rarity.Legendary;fixture.upgradeRank=6;fixture.ascensionRank=1;fixture.variantUnlocked=true;
            }
            foreach(var size in new[]{new Vector2Int(1600,900),new Vector2Int(1170,540),new Vector2Int(450,975),new Vector2Int(1024,768)})
            {
                MobileControls.ValidationUsesSimulation=true;MobileControls.ValidationTablet=size.x==1024;MobileControls.SimulationEnabled=size.x!=1600;yield return SetResolution(size.x,size.y);
                ResetPanels();Invoke("OpenHubService",HubNpcKind.Blacksmith);SetField("smithCategory",1);SetField("smithSelectedSlot",2);
                yield return Capture("gem-growth-"+size.x+"x"+size.y,.3f);
                Invoke("OpenGemPreview",EquipmentMechanic.RelicPower,false);yield return Capture("gem-upgrade-preview-"+size.x+"x"+size.y,.2f);
                Invoke("OpenGemPreview",EquipmentMechanic.RelicPower,true);yield return Capture("gem-ascension-preview-"+size.x+"x"+size.y,.2f);SetField("smithPreviewMechanic",EquipmentMechanic.None);ResetPanels();yield return new WaitForSecondsRealtime(.4f);
                Invoke("OpenHubService",HubNpcKind.Merchant);SetField("merchantMode",1);Check(GetField("panel").ToString()=="Inventory","Merchant visible at "+size);yield return Capture("gem-bags-"+size.x+"x"+size.y,.2f);
                SetField("entryRewardSelection","merchant:gem-bag:0");yield return Capture("gem-bag-detail-"+size.x+"x"+size.y,.2f);
                Invoke("OpenHubService",HubNpcKind.Blacksmith);Check(GetField("inventoryHubNpc").ToString()=="Blacksmith","Merchant switches directly to smith at "+size);ResetPanels();
                yield return new WaitForSecondsRealtime(.4f);OpenPanel("Skills");SetField("skillSection",2);yield return Capture("class-model-selection-"+size.x+"x"+size.y,.5f);
                SetField("skillSection",0);SetField("blinkFollowupSelected",false);SetField("selectedSkill",7);yield return Capture("skill-damage-numbers-"+size.x+"x"+size.y,.2f);SetField("blinkFollowupSelected",true);yield return Capture("followup-tree-"+size.x+"x"+size.y,.2f);ResetPanels();
            }
            MobileControls.SimulationEnabled=false;yield return SetResolution(1600,900);
            var view=Camera.main.GetComponent<AdventureCamera>();var viewType=typeof(AdventureCamera);
            foreach(float lowPitch in new[]{-18f,0f,38f})
            {
                viewType.GetField("pitch",PrivateInstance).SetValue(view,lowPitch);viewType.GetField("yaw",PrivateInstance).SetValue(view,0f);viewType.GetField("smoothYaw",PrivateInstance).SetValue(view,0f);
                var orbit=viewType.GetMethod("ProcessOrbitInput",PrivateInstance);
                orbit.Invoke(view,new object[]{new Vector2(400,400),true,true,false,true,true,0f});
                orbit.Invoke(view,new object[]{new Vector2(600,400),false,true,false,true,true,0f});
                viewType.GetMethod("AssistCombatView",PrivateInstance).Invoke(view,null);
                Check(Mathf.Abs(Mathf.DeltaAngle(view.Yaw,44))<.01f,"Horizontal right-drag survives automatic camera at pitch "+lowPitch);
                orbit.Invoke(view,new object[]{new Vector2(600,400),false,false,true,true,true,0f});
            }
            view.Snap();
            ResetPanels();yield return new WaitForSecondsRealtime(.4f);
            p.Teleport(GameSession.ChapterEntrancePosition);
            Check(session.IsNearChapterEntrance&&!session.IsNearDungeonEntrance,"Star chapter portal is separate from ordinary portal");
            session.EnterDungeon();Check(GetField("panel").ToString()=="Chapter","Star portal opens chapter selection");yield return Capture("star-chapter-entrance",.2f);
            ResetPanels();yield return new WaitForSecondsRealtime(.4f);p.Teleport(new Vector3(0,0,11));session.EnterDungeon();
            Check(session.DungeonSelectionOpen&&GetField("panel").ToString()!="Chapter","Ordinary portal opens ordinary adventures");yield return Capture("ordinary-adventure-entrance",.2f);session.CancelDungeonSelection();
            session.Progression.Profile.chapterCompletedMask=7;p.RefreshStats(true);
            p.Teleport(new Vector3(0,0,-10));session.SelectedChapterNode=ChapterNode.Redrock;session.SelectedChapterTier=1;
            Check(session.ConfirmChapterEnter(),"Enter real Redrock dungeon");yield return WaitForSceneReady();
            yield return new WaitForSeconds(.8f);
            Check(p.SelectNearestTabTarget()&&p.AimTarget!=null&&p.AimTarget.Level>0,"Tab selects a nearby living enemy with level metadata");
            yield return Capture("selected-enemy-target-panel",0);
            int epochBefore=p.CombatEpoch;stones=service.Profile.revivalStones;
            p.TakeDamageFrom(1e12f,"Revival fixture");
            Check(session.IsDead&&session.RevivalPending&&!session.ChapterRun.Failed&&Time.timeScale==1,"Death offers ghost state without failing or pausing dungeon");
            Check(!p.IsSkillAvailable(0)&&!p.TryBlink(Vector3.right),"Ghost cannot release skills or blink");
            Vector3 ghostStart=p.transform.position;var movingEnemy=session.Enemies[0];Vector3 enemyStart=movingEnemy.transform.position;
            PerformanceMove=Vector2.right;yield return new WaitForSeconds(1);PerformanceMove=Vector2.zero;
            Check(Vector3.Distance(ghostStart,p.transform.position)>.3f,"Ghost can move freely");
            yield return new WaitForSeconds(2);
            Check(Vector3.Distance(enemyStart,movingEnemy.transform.position)>.05f,"Enemies continue moving while player is a ghost");
            yield return Capture("ghost-death-choice",0);
            Check(session.ReviveInDungeon()&&!session.IsDead&&p.Health==p.MaxHealth&&service.Profile.revivalStones==stones-1,"Revival consumes one stone and restores health");
            Check(p.CombatEpoch==epochBefore&&!session.ReviveInDungeon(),"Revival preserves enemy receipts and cannot consume twice");
            hp=p.Health;p.TakeDamageFrom(1e12f,"Revival protection");Check(p.Health==hp,"Revival grants protection against immediate damage");
            foreach(var e in new List<EnemyController>(session.Enemies))if(e!=null&&!e.IsDead)e.TakeDamage(1e20f,Vector3.forward,impact:false);
            Check(session.ChapterRun.DoorUnlocked&&!session.ChapterFinished,"Intermediate room still advances through its exit");
            var plan=ChapterRoomGeometry.Plan(session.ActiveChapterNode,session.ChapterRoomIndex,session.ChapterSeed);
            p.RepositionAtOrigin(plan.Exit);Check(session.EnterNextChapterRoom(),"Enter final Redrock room");yield return WaitForSceneReady();
            p.RefreshStats(true);Vector3 finishPosition=p.transform.position;
            foreach(var e in new List<EnemyController>(session.Enemies))if(e!=null&&!e.IsDead)e.TakeDamage(1e20f,Vector3.forward,impact:false);
            Check(session.ChapterFinished&&session.DungeonCleared&&!session.ChapterRewardPending,"Last kill automatically completes and saves Redrock reward");
            Check(!session.FinishedResultReady,"Automatic completion first shows victory transition");
            yield return Capture("redrock-auto-victory-transition",0);
            float deadline=Time.realtimeSinceStartup+6;
            while(!session.FinishedResultReady&&Time.realtimeSinceStartup<deadline)yield return null;
            Check(session.FinishedResultReady&&p.transform.position==finishPosition,"Settlement becomes ready without moving to or interacting with portal");
            yield return Capture("redrock-auto-settlement",0);
            session.ReturnToCamp();yield return WaitForSceneReady();ResetPanels();
            service.Profile.pendingFashionChest=false;service.Profile.pendingChestReveal=false;service.Profile.pendingAdventureChest=false;
            p.Teleport(new Vector3(0,0,-10));session.SelectedChapterNode=ChapterNode.Redrock;session.SelectedChapterTier=1;
            Check(session.ConfirmChapterEnter(),"Enter new chapter to test end battle");yield return WaitForSceneReady();
            yield return new WaitForSeconds(.8f);p.TakeDamageFrom(1e12f,"End battle fixture");Check(session.RevivalPending,"New death starts as ghost");
            int beforeEnd=service.Profile.revivalStones;session.EndDeathBattle();
            Check(session.IsDead&&!session.RevivalPending&&session.ChapterRun.Failed&&session.LastRunSummary!=null&&service.Profile.revivalStones==beforeEnd,"End battle opens failed settlement without consuming revival stone");
            yield return Capture("ghost-end-battle-settlement",.2f);
            foreach(ChapterNode node in new[]{ChapterNode.ForestCourt,ChapterNode.Redrock,ChapterNode.StarPlatform})
            {
                var run=new ChapterCombatRun(node,ChapterDifficulty.Heroic,1);
                typeof(ChapterCombatRun).GetProperty("RoomIndex").SetValue(run,ChapterDefinition.RoomCount(node)-1);run.BindRoom(9);
                for(int i=0;i<run.EnemyCount;i++)run.Register(run.RoomIndex,9,i);
                for(int i=0;i<run.EnemyCount;i++)run.Defeat(run.RoomIndex,9,i);
                Check(run.FinishClearedFinalRoom()&&run.Finished&&!run.FinishClearedFinalRoom(),"All chapter final rooms finish once on clear: "+node);
            }
        }
    }
}
#endif
