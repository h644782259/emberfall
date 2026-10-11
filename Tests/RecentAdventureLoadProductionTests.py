#!/usr/bin/env python3
"""Execute actual recent-save UI methods with narrow session/engine boundaries.

The session double supplies load acceptance/completion and save enumeration;
production recent-slot selection, load dispatch, failure routing and UI cleanup
execute unchanged. This is not Unity touch delivery, async scheduling or save I/O.
Usage: python3 Tests/RecentAdventureLoadProductionTests.py /path/to/dotnet
       python3 Tests/RecentAdventureLoadProductionTests.py /path/to/dotnet --source-ref HEAD
"""
from pathlib import Path
import argparse
import os
import re
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]


def read_source(path, reference):
    if reference:
        return subprocess.check_output(
            ["git", "show", reference + ":" + path], cwd=ROOT, text=True
        )
    return (ROOT / path).read_text()


def member(source, signature):
    start = source.index(signature)
    end = source.index("{", start) + 1
    depth = 1
    while depth:
        depth += (source[end] == "{") - (source[end] == "}")
        end += 1
    return source[start:end]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("dotnet", nargs="?", default=os.environ.get("DOTNET", "dotnet"))
    parser.add_argument("--source-ref", help="read production sources from a Git ref for the pre-fix failure replay")
    args = parser.parse_args()
    base = read_source("Assets/Scripts/UI/GameUI.cs", args.source_ref)
    flow = read_source("Assets/Scripts/UI/GameUI.SaveFlow.cs", args.source_ref)
    service = read_source("Assets/Scripts/Core/ProgressionService.cs", args.source_ref)
    signatures = ["private SaveSlotInfo RecentAdventureSlot()", "private void ContinueRecentAdventure()",
                  "private void RefreshSaveSlots()", "private void OpenSaveSelection()",
                  "private void ContinueSelectedSave()"]
    methods = "\n".join(member(base, signature) for signature in signatures)
    methods += "\n" + member(flow, "private void CompleteSaveLoadUI()")
    hud_gate = re.search(r"GUI\.enabled\s*=\s*priorEnabled\s*&&\s*panel\s*==\s*Panel\.None\s*&&\s*!session\.InputBlocked\s*;", base)
    if hud_gate is None:
        raise AssertionError("update this production HUD gate extraction after changing its contract")
    slot_type = member(service, "public sealed class SaveSlotInfo")
    fixture = r'''
using System;using System.Collections.Generic;using UnityEngine;
namespace UnityEngine {
 public struct Vector2 { public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static Vector2 zero=>new Vector2(); }
 public static class GUI { public static bool enabled; }
}
namespace Emberfall {
 public enum HeroClass { Vanguard }
 SLOT_TYPE
 public sealed class ProgressionBoundary {
  public readonly List<SaveSlotInfo> Slots=new List<SaveSlotInfo>();public int Reads;
  public List<SaveSlotInfo> GetSaveSlots(){Reads++;return new List<SaveSlotInfo>(Slots);}
 }
 public sealed class SessionBoundary {
  public ProgressionBoundary Progression=new ProgressionBoundary();public bool HasStarted,SceneLoading,Paused,UIBlocking;
  public string SaveLoadError,RequestedId;public int Calls;public string Mode="async";
  public bool InputBlocked=>SceneLoading||!HasStarted||Paused||UIBlocking;
  public void SetPaused(bool paused){Paused=paused;}public void SetUIBlocking(bool blocked){UIBlocking=blocked;}
  public bool ContinueGame(string id){Calls++;RequestedId=id;if(Mode=="failure")return false;if(Mode=="sync")HasStarted=true;else SceneLoading=true;return true;}
  public void FinishAsync(){SceneLoading=false;HasStarted=true;}
 }
 public sealed class SaveFlowBoundary { public bool Open; }
 public sealed class GameUI {
  private enum Panel { None,SaveSelection }
  private SessionBoundary session=new SessionBoundary();private SaveFlowBoundary saveFlow=new SaveFlowBoundary();
  private List<SaveSlotInfo> saveSlots=new List<SaveSlotInfo>();private bool saveSlotsDirty=true,saveSelectionFromPause;
  private string selectedSaveId,saveSelectionError,saveFlowError;private object selectedItem;
  private Vector2 saveSelectionScroll,inventoryScroll,skillScroll;private int selectedSkill,rebindingSlot=-1,blocks,pausedLoadRequests;
  private void BlockUITransition(){blocks++;}private void RequestLoadSelectedSave(SaveSlotInfo target){pausedLoadRequests++;}
  private Panel panel=Panel.None;
  METHODS
  private bool ReplayHudGate(){bool priorEnabled=true;HUD_GATE return GUI.enabled;}
  private static int checks;
  private static void Check(bool condition,string message){checks++;if(!condition)throw new Exception(message);}
  private static bool Zero(Vector2 value)=>value.x==0&&value.y==0;
  private static SaveSlotInfo Slot(string id,int day,bool canLoad=true,bool pending=false)=>new SaveSlotInfo{Id=id,CanLoad=canLoad,DeletionPending=pending,SavedAtUtc=new DateTime(2026,10,day)};
  private static GameUI Fresh(string mode="async"){
   var ui=new GameUI();ui.session.Mode=mode;ui.session.Progression.Slots.Add(Slot("older",1));ui.session.Progression.Slots.Add(Slot("recent",2));
   ui.session.Progression.Slots.Add(Slot("invalid",4,false));ui.session.Progression.Slots.Add(Slot("pending-delete",5,true,true));return ui;
  }
  private static void AssertAccepted(GameUI ui,string context){
   Check(!ui.session.HasStarted&&ui.session.SceneLoading,context+" accepted async load exposes the pre-start loading interval");
   Check(ui.panel==Panel.None,"accepted async recent load must keep Panel.None before HasStarted");
   Check(ui.selectedSaveId=="recent"&&ui.session.RequestedId=="recent",context+" newest valid non-deleting role is selected");
   Check(ui.saveSelectionError==null&&ui.saveFlowError==null&&!ui.saveSelectionFromPause,context+" accepted load clears error and pause-selection ownership");
   Check(ui.selectedItem==null&&Zero(ui.inventoryScroll)&&Zero(ui.skillScroll)&&ui.selectedSkill==0&&ui.rebindingSlot==-1,context+" real completion cleanup resets selection and bindings");
   Check(!ui.ReplayHudGate(),context+" loading keeps production HUD gate closed");
   ui.session.FinishAsync();Check(ui.panel==Panel.None&&ui.ReplayHudGate(),context+" camp completion enables production HUD gate");
  }
  public static void Main(){
   var ui=Fresh();ui.selectedItem=new object();ui.inventoryScroll=new Vector2(2,3);ui.skillScroll=new Vector2(4,5);ui.selectedSkill=8;ui.rebindingSlot=4;ui.saveSelectionFromPause=true;ui.saveFlowError="old failure";
   ui.ContinueRecentAdventure();Check(ui.session.Calls==1&&ui.session.Progression.Reads==1,"dirty recent list refreshes once then dispatches one load");AssertAccepted(ui,"initial");Check(ui.blocks==1,"accepted async route arms only completion transition, not a failure screen");
   for(int attempt=0;attempt<3;attempt++){
    ui.session.HasStarted=false;ui.session.SceneLoading=false;ui.ContinueRecentAdventure();AssertAccepted(ui,"repeat "+attempt);
   }
   Check(ui.session.Calls==4,"re-entering from title dispatches exactly one load per entry");
   ui=Fresh("sync");ui.ContinueRecentAdventure();Check(ui.session.HasStarted&&!ui.session.SceneLoading&&ui.panel==Panel.None&&ui.ReplayHudGate(),"synchronous accepted load retains the camp surface");Check(ui.session.Calls==1&&ui.blocks==1,"synchronous success dispatches and cleans up once");
   foreach(string error in new[]{"resource preflight rejected",""}){
    ui=Fresh("failure");ui.session.SaveLoadError=error;ui.saveSelectionFromPause=true;ui.saveSelectionScroll=new Vector2(6,7);ui.ContinueRecentAdventure();
    Check(!ui.session.HasStarted&&!ui.session.SceneLoading&&ui.panel==Panel.SaveSelection,"preflight failure displays role list");
    Check(ui.saveSelectionError==(error.Length==0?"无法读取该存档，请返回后重新打开存档页重试。":error),"preflight failure retains exact explicit or fallback error");
    Check(ui.session.Calls==1&&ui.session.Progression.Reads==2,"failed load refreshes available roles without duplicate dispatch");
    Check(!ui.saveSelectionFromPause&&Zero(ui.saveSelectionScroll)&&ui.blocks==1,"failure route resets list scroll and owns one transition");
    ui.session.Mode="async";ui.session.SaveLoadError=null;ui.ContinueRecentAdventure();AssertAccepted(ui,"retry after failure");
   }
   foreach(int shape in new[]{0,1,2}){
    ui=new GameUI();if(shape==1)ui.session.Progression.Slots.Add(Slot("unreadable",2,false));if(shape==2)ui.session.Progression.Slots.Add(Slot("deleted",2,true,true));
    ui.saveSelectionError="stale";ui.saveSelectionScroll=new Vector2(7,8);ui.ContinueRecentAdventure();
    Check(ui.panel==Panel.SaveSelection&&ui.session.Calls==0,"no valid recent role opens selection without calling load");
    Check(ui.saveSelectionError==null&&Zero(ui.saveSelectionScroll)&&!ui.saveSelectionFromPause,"empty/invalid recent list starts with a clean selection surface");
    Check(ui.session.Progression.Reads==2&&ui.blocks==1,"no-recent route uses actual selection refresh and transition");
   }
   Console.WriteLine("PASS: "+checks+" recent-adventure production route assertions; session acceptance/completion and engine boundaries are simulated");
  }
 }
}
'''.replace("SLOT_TYPE", slot_type).replace("METHODS", methods).replace("HUD_GATE", hud_gate.group(0))
    with tempfile.TemporaryDirectory(prefix="emberfall-recent-load-") as temporary:
        work = Path(temporary)
        source = work / "Fixture.cs"
        source.write_text(fixture)
        project = work / "Test.csproj"
        project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NuGetAudit>false</NuGetAudit><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>')
        config = work / "NuGet.Config"
        config.write_text('<configuration><packageSources><clear/></packageSources></configuration>')
        environment = dict(os.environ, DOTNET_CLI_HOME=str(work / "cli"), DOTNET_NOLOGO="1", DOTNET_CLI_TELEMETRY_OPTOUT="1")
        build = [args.dotnet, "build", str(project), "--configfile", str(config), "-v:q"]
        run = [args.dotnet, str(work / "bin/Debug/net8.0/Test.dll")]
        subprocess.run(build, env=environment, check=True)
        subprocess.run(run, env=environment, check=True)
        # Mandatory compiled negative control: restore the precise old routing
        # condition and require the async Panel.None assertion, not a build error.
        correct = "if(!session.HasStarted&&!session.SceneLoading)"
        original = member(base, "private void ContinueRecentAdventure()")
        if correct not in original:
            raise AssertionError("expected the production async-acceptance guard for the negative control")
        mutant = original.replace(correct, "if(!session.HasStarted)", 1)
        source.write_text(fixture.replace(original, mutant, 1))
        subprocess.run(build, env=environment, check=True, stdout=subprocess.DEVNULL)
        failure = subprocess.run(run, env=environment, capture_output=True, text=True)
        expected = "System.Exception: accepted async recent load must keep Panel.None before HasStarted"
        if failure.returncode == 0 or expected not in failure.stdout + failure.stderr:
            raise AssertionError("old routing mutant did not fail its exact async acceptance assertion: " + failure.stdout + failure.stderr)
        print("PASS: compiled old HasStarted-only routing fails the exact async Panel.None assertion")


if __name__ == "__main__":
    main()
