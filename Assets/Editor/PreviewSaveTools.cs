using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Emberfall.Editor
{
    // Editor-only fixture creation; always writes a fresh slot through the normal save service.
    public static class PreviewSaveTools
    {
        [MenuItem("Emberfall/创建100级测试存档 Preview Save", false, 4)]
        public static void Create()
        {
            try
            {
                var game=GameSession.Instance;
                HeroClass hero=game!=null&&game.Progression!=null?game.Progression.Profile.heroClass:HeroClass.Vanguard;
                var service=new ProgressionService();
                var create=typeof(ProgressionService).GetMethod("CreateProfile",BindingFlags.NonPublic|BindingFlags.Static);
                var profile=(GameProfile)create.Invoke(null,new object[]{hero});
                typeof(ProgressionService).GetProperty("Profile").SetValue(service,profile);
                profile.level=100;profile.xp=0;profile.gold=5000000;
                profile.potions=200;profile.revivalStones=100;
                profile.mechanicMaterials=2000;profile.fashionThreads=2000;
                profile.refinementStones=500;profile.affixReforgeStones=500;
                profile.gemBags=new[]{30,30,30};profile.tutorialMask=int.MaxValue;profile.classTutorialCompleted=true;
                for(int i=0;i<profile.skillRanks.Length;i++)profile.skillRanks[i]=3;
                profile.inventory.Clear();profile.weaponId=profile.armorId=profile.relicId=null;
                var roll=typeof(ProgressionService).GetMethod("SetRolledStats",BindingFlags.NonPublic|BindingFlags.Static);
                var basis=typeof(ProgressionService).GetMethod("EnsureUpgradeBasis",BindingFlags.NonPublic|BindingFlags.Static);
                foreach(ItemSlot slot in Enum.GetValues(typeof(ItemSlot)))
                foreach(Rarity rarity in Enum.GetValues(typeof(Rarity)))
                foreach(int level in new[]{40,100})
                {
                    var item=new ItemData{id=Guid.NewGuid().ToString("N"),slot=slot,rarity=rarity,level=level,
                        name="测试 · "+GameBalance.RarityName(rarity)+GameBalance.SlotName(slot)};
                    roll.Invoke(null,new object[]{item});basis.Invoke(null,new object[]{item});profile.inventory.Add(item);
                    if(level==100&&rarity==Rarity.Epic)
                    {if(slot==ItemSlot.Weapon)profile.weaponId=item.id;else if(slot==ItemSlot.Armor)profile.armorId=item.id;else profile.relicId=item.id;}
                }
                profile.attachments.Clear();
                foreach(var gem in BuildCatalog.DroppableGemsFor(hero))
                    profile.attachments.Add(new MechanicAttachment{id=Guid.NewGuid().ToString("N"),mechanic=gem,rarity=(Rarity)(1+(int)gem%3),level=100,mounted=false,ascensionRank=0});
                profile.fashions.Clear();
                foreach(FashionSlot slot in Enum.GetValues(typeof(FashionSlot)))
                foreach(Rarity rarity in Enum.GetValues(typeof(Rarity)))
                    profile.fashions.Add(new FashionData{id=Guid.NewGuid().ToString("N"),slot=slot,rarity=rarity,appearanceTier=(int)rarity,name=ProgressionService.FashionName(slot,rarity,hero)});
                if(!service.SaveAsNewSlot())throw new InvalidOperationException(service.LastError);
                var verify=new ProgressionService();
                if(!verify.LoadSlot(service.CurrentSlotId)||verify.Profile.level!=100||verify.Profile.inventory.Count<24)
                    throw new InvalidOperationException("测试存档读取校验失败："+verify.LastError);
                string report="已创建独立100级测试存档\n职业："+hero+"\n金币：500万；装备："+verify.Profile.inventory.Count+
                    "；宝石："+verify.Profile.attachments.Count+"；时装："+verify.Profile.fashions.Count+
                    "\n存档编号："+service.CurrentSlotId+"\n路径："+service.SaveFilePath+"\n退出试玩后重新运行，在读取存档列表中选择最新的100级角色。原存档未修改。";
                Directory.CreateDirectory("Temp");File.WriteAllText("Temp/preview-save-result.txt",report);
                Debug.Log(report);
                EditorUtility.DisplayDialog("100级测试存档已就绪",report,"知道了");
            }
            catch(Exception error){Debug.LogException(error);EditorUtility.DisplayDialog("测试存档创建失败",error.GetBaseException().Message,"关闭");}
        }
    }
}
