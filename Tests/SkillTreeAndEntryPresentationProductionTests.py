"""Exercise production connector clipping and grouped entry identities with managed GUI boundaries."""
from pathlib import Path
import os, subprocess, sys, tempfile
root=Path(__file__).resolve().parents[1]
def member(source,signature):
    start=source.index(signature);end=source.index('{',start)+1;depth=1
    while depth:
        depth+=(source[end]=='{')-(source[end]=='}');end+=1
    return source[start:end]
tree=(root/'Assets/Scripts/UI/GameUI.MobileSkills.cs').read_text()
modes=(root/'Assets/Scripts/UI/GameUI.Modes.cs').read_text()
methods=member(tree,'private void DrawSkillTreeLine')+'\n'+member(modes,'private sealed class EntryRewardGroup')+'\n'+member(modes,'private System.Collections.Generic.List<EntryRewardGroup> GroupEntryRewards')
cache_start=modes.index('  private readonly System.Collections.Generic.Dictionary<System.Collections.Generic.List<EntryRewardPreview>')
cache_end=modes.index('\n',cache_start)
fixture=r'''
using System;using System.Collections.Generic;using UnityEngine;
namespace UnityEngine {
 public struct Color {}
 public struct Rect { public float x,y,width,height;public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}public float xMax=>x+width;public float yMax=>y+height;public float centerX=>x+width*.5f;public bool Overlaps(Rect b)=>x<b.xMax&&xMax>b.x&&y<b.yMax&&yMax>b.y; }
 public static class Mathf {public static float Min(float a,float b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);}
}
namespace Emberfall {
 enum ItemSlot {Weapon,Armor,Charm}enum Rarity {Rare,Epic,Legendary}
 class EntryRewardPreview {public string Key,Name,Description;public ItemSlot? GearSlot;public Rarity Rarity;public bool Clear;}
 class GameUI {
 CACHE
 METHODS
 List<Rect> painted=new List<Rect>();void Fill(Rect r,Color c){painted.Add(r);}
 static int checks;static void C(bool b,string message){checks++;if(!b)throw new Exception(message);}
 void Verify(Rect strip,Rect[] nodes){painted.Clear();DrawSkillTreeLine(strip,nodes,default);foreach(var r in painted){C(r.width>0&&r.height>0,"only positive strips draw");C(r.x>=strip.x&&r.y>=strip.y&&r.xMax<=strip.xMax+.001f&&r.yMax<=strip.yMax+.001f,"clipping stays within original connector");foreach(var n in nodes)C(!r.Overlaps(n),"connector never crosses any node, including long prerequisite edges");}}
 static readonly int[] Rows={0,1,2,1,3,2,4,5,4,6};static readonly float[] Cols={1,0,0,2,0,2,1,0,2,1};static readonly int[][] Parents={new int[0],new[]{0},new[]{1},new[]{0},new[]{2},new[]{3},new[]{5},new[]{4},new[]{5},new[]{7,6}};
 public static void Main(){var ui=new GameUI();foreach(float u in new[]{.65f,1f,1.5f,2f})foreach(float width in new[]{236,320,440,800})foreach(float body in new[]{252,320,420,600,768}){
 float step=Math.Max(44,Math.Min(108,(body-8)/7));bool compact=step<86;var nodes=new Rect[10];for(int i=0;i<10;i++)nodes[i]=new Rect((4+Cols[i]*(width/3))*u,(8+Rows[i]*step)*u,(width/3-8)*u,(compact?40:82)*u);
 for(int child=0;child<10;child++)foreach(int parent in Parents[child]){Rect a=nodes[parent],b=nodes[child];float mid=b.y-12*u;ui.Verify(new Rect(a.centerX-u,a.yMax,2*u,Math.Max(0,mid-a.yMax)),nodes);ui.Verify(new Rect(Math.Min(a.centerX,b.centerX),mid,Math.Max(2*u,Math.Abs(a.centerX-b.centerX)),2*u),nodes);ui.Verify(new Rect(b.centerX-u,mid,2*u,b.y-mid),nodes);}}
 var desktop=new Rect[10];for(int i=0;i<10;i++)desktop[i]=new Rect(10+Cols[i]*160,6+Rows[i]*66,144,54);for(int child=0;child<10;child++)for(int k=0;k<Parents[child].Length;k++){Rect a=desktop[Parents[child][k]],b=desktop[child];float bend=b.y-13-k*5;ui.Verify(new Rect(a.centerX-1,a.yMax,2,bend-a.yMax),desktop);ui.Verify(new Rect(Math.Min(a.centerX,b.centerX),bend,Math.Max(2,Math.Abs(a.centerX-b.centerX)),2),desktop);ui.Verify(new Rect(b.centerX-1,bend,2,b.y-bend),desktop);ui.Verify(new Rect(b.centerX-3,b.y-5,6,5),desktop);}
 ui.Verify(new Rect(0,0,-1,80),desktop);C(ui.painted.Count==0,"negative strip is skipped");ui.Verify(new Rect(0,0,80,0),desktop);C(ui.painted.Count==0,"zero strip is skipped");
 var rewards=new List<EntryRewardPreview>();foreach(ItemSlot slot in Enum.GetValues(typeof(ItemSlot)))foreach(Rarity quality in Enum.GetValues(typeof(Rarity)))rewards.Add(new EntryRewardPreview{Key="clear:"+slot+":"+quality,GearSlot=slot,Rarity=quality,Clear=true,Name=slot.ToString(),Description="unique "+slot+quality});foreach(string gem in new[]{"Ember","Frost"})foreach(Rarity quality in new[]{Rarity.Epic,Rarity.Legendary})rewards.Add(new EntryRewardPreview{Key="gem:"+gem+":"+quality,Rarity=quality,Clear=true,Description="unique "+gem+quality});rewards.Add(new EntryRewardPreview{Key="shard",Clear=true});rewards.Add(new EntryRewardPreview{Key="refinement",Clear=true});rewards.Add(new EntryRewardPreview{Key="fashion:Weapon:Legendary",Clear=true});rewards.Add(new EntryRewardPreview{Key="fashion:Wings:Legendary",Clear=true});rewards.Add(new EntryRewardPreview{Key="field:Weapon:Rare",GearSlot=ItemSlot.Weapon,Rarity=Rarity.Rare});
 var grouped=ui.GroupEntryRewards(rewards);C(grouped.Count==10,"equipment and gem variants group; resources, fashion slots, fixed/chest rewards stay distinct");var seen=new HashSet<EntryRewardPreview>();foreach(var g in grouped)foreach(var item in g.Variants){C(seen.Add(item),"each preview identity is inspectable once");C(rewards.Contains(item),"group retains original preview, icon, description and rarity object");C(item.Clear==g.First.Clear,"fixed drops cannot merge with clear chest");}C(seen.Count==rewards.Count,"grouping loses no reachable quality or reward");C(object.ReferenceEquals(grouped,ui.GroupEntryRewards(rewards)),"warm drawing reuses grouped presentation cache");
 for(int i=0;i<130;i++)ui.GroupEntryRewards(new List<EntryRewardPreview>{new EntryRewardPreview{Key="shard"}});C(ui.entryRewardGroups.Count<=64,"presentation cache remains bounded across owners and tiers");
 Console.WriteLine("PASS "+checks+" actual connector clipping and reward identity assertions; managed GUI boundaries, no native rendering");}
 }
}
'''.replace('CACHE',modes[cache_start:cache_end]).replace('METHODS',methods)
with tempfile.TemporaryDirectory(prefix='emberfall-entry-presentation-') as tmp:
    p=Path(tmp);(p/'Fixture.cs').write_text(fixture)
    (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NuGetAudit>false</NuGetAudit><NoWarn>0649</NoWarn></PropertyGroup></Project>')
    (p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
    subprocess.run([sys.argv[1] if len(sys.argv)>1 else 'dotnet','run','--project',str(p/'Test.csproj')],env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'),check=True)
