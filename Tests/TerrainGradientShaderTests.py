#!/usr/bin/env python3
"""Compile the actual HLSL terrain functions as scalar C++ and check derivatives.

This tests source math, not GPU compilation or rendering. Unity export separately
compiles the real Metal shader. No terrain formula is duplicated in the test.
"""
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
SHADER = ROOT / "Assets/Resources/WorldArt/TerrainField.cginc"
COMPAT = r'''
#include <cmath>
#include <algorithm>
#include <cstdio>
#include <random>
using std::abs;
struct float2 { float x,y; float2(float a,float b):x(a),y(b){} };
struct float3 { float x,y,z; float3(float a,float b,float c):x(a),y(b),z(c){} };
float2 operator-(float2 a,float2 b){return {a.x-b.x,a.y-b.y};}
float2 operator+(float2 a,float2 b){return {a.x+b.x,a.y+b.y};}
float3 operator+(float3 a,float3 b){return {a.x+b.x,a.y+b.y,a.z+b.z};}
float3 operator*(float s,float3 a){return {s*a.x,s*a.y,s*a.z};}
float dot(float2 a,float2 b){return a.x*b.x+a.y*b.y;}
float saturate(float a){return std::max(0.f,std::min(1.f,a));}
float sign(float a){return (a>0)-(a<0);}
'''
CHECKS = r'''
int main(){
 std::mt19937 random(1793);std::uniform_real_distribution<float> coordinate(-45,45);
 float maxHeight=0,maxSlope=0;int checks=0;
 for(int enabled=0;enabled<2;enabled++)for(int dungeon=0;dungeon<2;dungeon++)for(float radius:{12.f,18.f,25.f,36.f}){
  _EmberTerrainEnabled=enabled;_EmberTerrainDungeon=dungeon;_EmberTerrainRadius=radius;
  for(int i=0;i<25000;i++){
   float2 p(coordinate(random),coordinate(random));
   // Include both sides of dungeon transition boundaries and hill centers.
   if(i<100){p.x=(i%2?-1:1)*(3.f+(i%5)*.75f);p.y=3.3f+(i%7)/3.f;}
   float3 g=EmberHeightGradient(p);float h=EmberHeight(p);
   float dx=(EmberHeight(p+float2(.02f,0))-EmberHeight(p-float2(.02f,0)))/.04f;
   float dz=(EmberHeight(p+float2(0,.02f))-EmberHeight(p-float2(0,.02f)))/.04f;
   maxHeight=std::max(maxHeight,abs(g.x-h));
   maxSlope=std::max(maxSlope,std::max(abs(g.y-dx),abs(g.z-dz)));
   if(!std::isfinite(g.x)||!std::isfinite(g.y)||!std::isfinite(g.z)||abs(g.x-h)>.00001f||abs(g.y-dx)>.001f||abs(g.z-dz)>.001f){
    std::printf("FAIL enabled=%d dungeon=%d radius=%g p=%g,%g height=%g slope=%g,%g\n",enabled,dungeon,radius,p.x,p.y,g.x-h,g.y-dx,g.z-dz);return 1;
   }
   checks++;
  }
 }
 std::printf("PASS %d actual shader terrain samples; maximum height difference %.9f m, maximum slope difference %.9f\n",checks,maxHeight,maxSlope);
}
'''

with tempfile.TemporaryDirectory(prefix="emberfall-terrain-gradient-") as directory:
    source = Path(directory) / "terrain.cpp"
    binary = Path(directory) / "terrain-test"
    source.write_text(COMPAT + '\n#include "' + str(SHADER) + '"\n' + CHECKS)
    subprocess.run(["clang++", "-std=c++17", "-O2", str(source), "-o", str(binary)], check=True)
    subprocess.run([str(binary)], check=True)
