#!/usr/bin/env python3
"""Actual water-entry slices plus complete production water/geometry/resource/navigation generators."""
import os, sys, subprocess, tempfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def member(source, signature):
    start=source.index(signature);end=source.index('{',start)+1;depth=1
    while depth:depth+=(source[end]=='{')-(source[end]=='}');end+=1
    return source[start:end]
def once(source, old, new):
    assert source.count(old)==1,old
    return source.replace(old,new)
def run(dotnet,variants=('current','missing-hook','shared-backface-vertices','outside-navigation-width')):
    world=(ROOT/'Assets/Scripts/World/WorldBuilder.cs').read_text()
    linked=(ROOT/'Assets/Scripts/World/WorldBuilder.LinkedRooms.cs').read_text();tactical=(ROOT/'Assets/Scripts/World/WorldBuilder.TacticalRooms.cs').read_text()
    brook=world[world.index('            Vector3[] stream = {'):world.index('            BuildWaterBankDetail(lowland,r,stream);')]
    courtyard=linked[linked.index('    Vector3[] stream={'):linked.index('    Primitive(parent,"Sunken courtyard bridge"')]
    flooded=member(tactical,'if(TacticalRoomGeometry.Flooded(layout))');flooded=flooded[flooded.index('{')+1:flooded.rfind('}')]
    entries=[]
    for name,code in [('Brook',brook),('Courtyard',courtyard),('Tactical',flooded)]:
        current=code.replace('BuildWaterSurface(', 'WaterEntry(baseline,').replace('BuildNaturalStreamBed(', 'BedEntry(baseline,')
        entries.append('public static void '+name+'(Transform parent,WorldResources r,int layout=22,bool baseline=false){var lowland=parent;'+('TacticalRoomGeometry.Register(layout);' if name=='Tactical' else '')+current+'}')
    helpers='using System.Collections.Generic;using UnityEngine;namespace Emberfall{public static partial class WorldBuilder{'+member(world,'private static GameObject Geometry(')+''.join(entries)+'}'+member(world,'public sealed class WorldResources')+'}'
    water=(ROOT/'Assets/Scripts/World/WorldBuilder.WaterSurface.cs').read_text()
    with tempfile.TemporaryDirectory(prefix='shore-contact-') as temporary:
        for variant in variants:
            folder=Path(temporary)/variant;folder.mkdir();(folder/'WorldExtract.cs').write_text(helpers)
            code=water
            if variant=='missing-hook':code=once(code,'            BuildShoreWaterContact(parent,r,name,samples,profile.Width,height);','')
            elif variant=='shared-backface-vertices':
                code=once(code,'int[] face={a,b,a+1,a+1,b,b+1};','int[] face={a,b,a+1,a+1,b,b+1,a+1,b,a,b+1,b,a+1};')
                begin=code.index('            int frontVertices=line.Count,frontIndices=lineTriangles.Count;');end=code.index('            Geometry(parent,r,name+" shore damp seam"',begin);code=code[:begin]+code[end:]
            elif variant=='outside-navigation-width':code=once(code,'profile.Width*.5f*miter-inward','profile.Width*.5f*miter+inward')
            (folder/'Water.cs').write_text(code)
            for relative in ['Assets/Scripts/Core/WaterPresentation.cs','Assets/Scripts/World/WorldTraversal.cs','Assets/Scripts/World/TacticalRoomGeometry.cs','Tests/WaterSurfaceProductionFixture.cs','Tests/ShoreContactProductionTests.cs']:(folder/Path(relative).name).write_text((ROOT/relative).read_text())
            (folder/'Program.cs').write_text('System.Console.WriteLine(ShoreContactProductionTests.Run());')
            config=folder/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
            project=folder/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>')
            env=dict(os.environ,DOTNET_CLI_HOME=str(folder/'cli'),DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_NOLOGO='1')
            build=subprocess.run([dotnet,'build',str(project),'--configfile',str(config),'-v:q'],env=env,capture_output=True,text=True)
            if build.returncode:print(build.stdout+build.stderr);build.check_returncode()
            result=subprocess.run([dotnet,str(folder/'bin/Debug/net8.0/Test.dll')],env=env,capture_output=True,text=True);output=result.stdout+result.stderr
            if variant=='current':print(output,end='');result.check_returncode()
            else:
                expected={'missing-hook':'actual water entry must build both static shoreline layers','shared-backface-vertices':'WATERLINE_NORMAL_ZERO','outside-navigation-width':'WATER_OUTSIDE_NAVIGATION_WIDTH'}[variant]
                assert result.returncode and 'System.Exception: '+expected in output,output
                print('PASS: compiled '+variant+' control fails the actual-entry '+expected+' assertion.')
if __name__=='__main__':run(sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet'))
