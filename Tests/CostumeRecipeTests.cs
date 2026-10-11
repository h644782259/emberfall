using System;
using System.Collections.Generic;
using Emberfall;
public static class CostumeRecipeTests
{
    public static string Run()
    {
        int checks=0;Action<bool,string> check=(ok,message)=>{checks++;if(!ok)throw new Exception(message);};
        foreach(var mesh in new[]{CostumeRecipes.Feather(),CostumeRecipes.Crystal(),CostumeRecipes.Ring()})
        {
            check(mesh.Positions.Length<=900&&mesh.Triangles.Length<=1800,"fixed shared mesh budget");
            var edges=new Dictionary<string,int>();
            for(int i=0;i<mesh.Positions.Length;i++)check(!float.IsNaN(mesh.Positions[i])&&!float.IsInfinity(mesh.Positions[i])&&Math.Abs(mesh.Positions[i])<=1.11f,"finite bounded vertices");
            for(int i=0;i<mesh.Triangles.Length;i+=3)
            {
                int a=mesh.Triangles[i],b=mesh.Triangles[i+1],c=mesh.Triangles[i+2];
                foreach(int v in new[]{a,b,c})check(v>=0&&v<mesh.Positions.Length/3,"valid index");
                float ax=mesh.Positions[b*3]-mesh.Positions[a*3],ay=mesh.Positions[b*3+1]-mesh.Positions[a*3+1],az=mesh.Positions[b*3+2]-mesh.Positions[a*3+2];
                float bx=mesh.Positions[c*3]-mesh.Positions[a*3],by=mesh.Positions[c*3+1]-mesh.Positions[a*3+1],bz=mesh.Positions[c*3+2]-mesh.Positions[a*3+2];
                float nx=ay*bz-az*by,ny=az*bx-ax*bz,nz=ax*by-ay*bx;check(nx*nx+ny*ny+nz*nz>1e-12f,"nondegenerate faces");
                foreach(var edge in new[]{new[]{a,b},new[]{b,c},new[]{c,a}})
                {string key=Math.Min(edge[0],edge[1])+":"+Math.Max(edge[0],edge[1]);int n;edges.TryGetValue(key,out n);edges[key]=n+1;}
            }
            foreach(int count in edges.Values)check(count==2,"closed manifold solid silhouette");
        }
        check(CostumeRecipes.WingStyle(Rarity.Common)==WingSilhouette.Feather&&CostumeRecipes.WingStyle(Rarity.Rare)==WingSilhouette.Feather,"feather identities preserved");
        check(CostumeRecipes.WingStyle(Rarity.Epic)==WingSilhouette.Crystal&&CostumeRecipes.WingStyle(Rarity.Legendary)==WingSilhouette.Mechanical,"epic crystal and legendary mechanical silhouettes distinct");
        foreach(int tier in new[]{int.MinValue,0,1,2,3,4,999})
        {
            float tank=CostumeRecipes.ChestWidth(HeroClass.Vanguard,tier),robe=CostumeRecipes.ChestWidth(HeroClass.Arcanist,tier),ranger=CostumeRecipes.ChestWidth(HeroClass.Ranger,tier),shaman=CostumeRecipes.ChestWidth(HeroClass.Summoner,tier);
            check(tank>shaman&&shaman>ranger&&ranger>robe,"every equipment tier preserves class silhouette hierarchy");
            check(tank<=.95f&&robe>=.4f,"extreme item input clamped");
        }
        check(2+8*2+1<=CostumeRecipes.MaximumWingParts,"largest mechanical outfit bounded");
        return "PASS: "+checks+" costume topology/bounds/identity checks (no rendered-frame validation)";
    }
}
