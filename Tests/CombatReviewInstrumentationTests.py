#!/usr/bin/env python3
"""Execute extracted production telemetry statements against counting ID API doubles.
This is not controller execution, Unity allocation profiling, or a gameplay test.
"""
import argparse
import importlib.util
import os
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]


def production_statements():
    statements = []
    for path in sorted((ROOT / 'Assets/Scripts').rglob('*.cs')):
        for line_number, line in enumerate(path.read_text().splitlines(), 1):
            call = line.find('CombatReviewEvents.Emit(')
            if call < 0:
                continue
            start = line.rfind('if (', 0, call)
            guard = line[start:call] if start >= 0 else ''
            if 'CombatReviewEvents.Enabled' not in guard or '||' in guard:
                raise AssertionError(f'Unguarded eager telemetry construction: {path}:{line_number}')
            # These sites deliberately keep one guarded Emit statement on one line.
            depth = 0
            quoted = escaped = False
            end = None
            for i in range(line.index('(', call), len(line)):
                char = line[i]
                if quoted:
                    if escaped: escaped = False
                    elif char == '\\': escaped = True
                    elif char == '"': quoted = False
                    continue
                if char == '"': quoted = True
                elif char == '(': depth += 1
                elif char == ')':
                    depth -= 1
                    if depth == 0:
                        end = i + 1
                        break
            if end is None or line[end:end+1] != ';':
                raise AssertionError(f'Unsupported Emit statement: {path}:{line_number}')
            statements.append((str(path.relative_to(ROOT)), line_number, line[start:end+1]))
    if len(statements) != 17:
        raise AssertionError(f'Review added/removed telemetry sites explicitly: found {len(statements)}')
    return statements


def run(dotnet):
    statements = production_statements()
    spec = importlib.util.spec_from_file_location('cloud', ROOT / 'Tools/cloud-validation.py')
    cloud = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(cloud)
    program = r'''
using System;
using System.Collections.Generic;
using Emberfall;
using UnityEngine;
namespace UnityEngine {
    public struct EntityId {
        internal ulong raw;
        public static ulong ToULong(EntityId value) { return value.raw; }
        [Obsolete("No lossy conversion",true)] public static implicit operator int(EntityId value) { return 0; }
    }
    public class Object {
        public static int Reads;
        public ulong raw=ulong.MaxValue;
        public float Health=2;
        public EntityId GetEntityId() { Reads++; return new EntityId { raw=raw }; }
        [Obsolete("Use EntityId",true)] public int GetInstanceID() { return 0; }
    }
    public static class Mathf { public static float Max(float a,float b) { return Math.Max(a,b); } }
}
static class GameBalance { public static float SkillEnergyCost(int hero,int skill) { return 1; } }
sealed class Harness:UnityEngine.Object {
    UnityEngine.Object owner=new UnityEngine.Object(), player=new UnityEngine.Object(), projectile=new UnityEngine.Object(),
        tracking=new UnityEngine.Object(), selected=new UnityEngine.Object(), enemy=new UnityEngine.Object();
    float previousHealth=5,healthBeforeHit=5,healthBefore=5,Energy=0;
    int skill=2,skillIndex=2,slot=2,SkillIndex=2,HeroClass=0,step=0;
    bool basic=true,lastMeleeDamagedEnemy=false,hostile=false;
    string source="synthetic-source",terminationReason="expired";
    List<int> hitTargets=new List<int>();
    public void Exercise() {
        /*PRODUCTION_STATEMENTS*/
    }
}
static class Program {
    static void Main() {
        var harness=new Harness();var observed=new List<CombatReviewEvents.Entry>();
        Action<CombatReviewEvents.Entry> sink=e=>observed.Add(e);
        if(CombatReviewEvents.Enabled)throw new Exception("leaked listener");
        for(int i=0;i<10000;i++)harness.Exercise();
        if(UnityEngine.Object.Reads!=0||observed.Count!=0)throw new Exception("disabled recording constructed object IDs");
        CombatReviewEvents.Observed+=sink;
        try {
            harness.Exercise();
            if(observed.Count!=17||UnityEngine.Object.Reads<=0)throw new Exception("enabled recording lost production events");
            foreach(var e in observed) {
                if(e.actorId!="0"&&e.actorId!="18446744073709551615")throw new Exception("actor identity changed");
                if(e.targetId!="0"&&e.targetId!="18446744073709551615")throw new Exception("target identity changed");
                if(e.kind=="projectileend"&&!e.detail.StartsWith("18446744073709551615:"))throw new Exception("projectile detail ID changed");
                if(e.kind=="damage"&&e.amount!=3)throw new Exception("health loss payload changed");
            }
        } finally { CombatReviewEvents.Observed-=sink; }
        UnityEngine.Object.Reads=0;
        for(int i=0;i<10000;i++)harness.Exercise();
        if(UnityEngine.Object.Reads!=0||observed.Count!=17)throw new Exception("unsubscribe failed to stop eager construction");
        Console.WriteLine("PASS: 17 extracted production capture statements; disabled/unsubscribed 340,000 executions perform zero ID API reads; enabled captures retain complete IDs and damage payload. API doubles, no Unity allocation/frame claim.");
    }
}
'''.replace('/*PRODUCTION_STATEMENTS*/', '\n'.join(s[2] for s in statements))
    with tempfile.TemporaryDirectory(prefix='EmberfallReviewInstrumentation-') as temporary:
        directory = Path(temporary)
        config = directory / 'NuGet.Config'
        config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
        project = cloud.write_project(directory / 'probe', [ROOT / 'Assets/Scripts/Core/CombatReviewEvents.cs',
                                      ROOT / 'Assets/Scripts/Core/CombatReviewObjectId.cs'], program,
                                      defines='UNITY_6000_6_OR_NEWER')
        env = os.environ.copy()
        env.update(DOTNET_CLI_HOME=str(directory / 'home'), DOTNET_NOLOGO='1', DOTNET_CLI_TELEMETRY_OPTOUT='1')
        subprocess.run([dotnet, 'restore', str(project), '--configfile', str(config), '--verbosity', 'quiet'], env=env, check=True)
        subprocess.run([dotnet, 'run', '--project', str(project), '--no-restore', '-c', 'Release'], env=env, check=True)


if __name__ == '__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dotnet', default='dotnet')
    run(parser.parse_args().dotnet)
