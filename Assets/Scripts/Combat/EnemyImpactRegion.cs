using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    internal static class EnemyImpactRegion
    {
        internal static bool Contains(Vector3 attacker,Vector3 center,Vector3 point,float radius)
        {
            Vector3 offset=CombatFx.Flat(point-center);
            return PlayerUpgradeRules.IsInsideArea(offset.x,offset.z,radius,true)&&WorldTraversal.HasGroundPath(attacker,point,.12f);
        }
        // Bounded marching contour, not rays from the impact center: the center may itself be across cover.
        // Each edge brackets the actual hit predicate. Disconnected reachable lobes stay disconnected.
        internal static List<Vector3> Outline(Vector3 attacker,Vector3 center,float radius)
        {
            const int side=24;var result=new List<Vector3>();var points=new Vector3[side+1,side+1];var inside=new bool[side+1,side+1];
            for(int x=0;x<=side;x++)for(int z=0;z<=side;z++)
            {points[x,z]=center+new Vector3((x*2f/side-1)*radius,0,(z*2f/side-1)*radius);inside[x,z]=Contains(attacker,center,points[x,z],radius);}
            var corners=new Vector3[4];var valid=new bool[4];var crossings=new Vector3[4];
            for(int x=0;x<side;x++)for(int z=0;z<side;z++)
            {
                corners[0]=points[x,z];corners[1]=points[x+1,z];corners[2]=points[x+1,z+1];corners[3]=points[x,z+1];
                valid[0]=inside[x,z];valid[1]=inside[x+1,z];valid[2]=inside[x+1,z+1];valid[3]=inside[x,z+1];int count=0;
                for(int edge=0;edge<4;edge++)
                {
                    int next=(edge+1)%4;if(valid[edge]==valid[next])continue;
                    Vector3 yes=valid[edge]?corners[edge]:corners[next],no=valid[edge]?corners[next]:corners[edge];
                    for(int n=0;n<8;n++){Vector3 mid=(yes+no)*.5f;if(Contains(attacker,center,mid,radius))yes=mid;else no=mid;}
                    crossings[count++]=yes;
                }
                if(count==2){result.Add(crossings[0]);result.Add(crossings[1]);}
                else if(count==4)
                {
                    bool mid=Contains(attacker,center,(corners[0]+corners[2])*.5f,radius);
                    int shift=mid==valid[0]?0:1;
                    result.Add(crossings[shift]);result.Add(crossings[(shift+1)%4]);result.Add(crossings[(shift+2)%4]);result.Add(crossings[(shift+3)%4]);
                }
            }
            return result;
        }
    }
}
