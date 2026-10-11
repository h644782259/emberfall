using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    internal sealed partial class SkillPerformanceVfx
    {
        private static Mesh[] discharges;
        private static Material dischargeMaterial;
        private static Material DischargeMaterial(){if(dischargeMaterial==null)dischargeMaterial=new Material(Resources.Load<Shader>("LightningDischarge")){hideFlags=HideFlags.HideAndDontSave};return dischargeMaterial;}
        private int dischargeBeat;
        private bool dischargeArmed;
        private static void ResetDischarges()
        {if(dischargeMaterial!=null)Destroy(dischargeMaterial);dischargeMaterial=null;if(discharges!=null)foreach(var mesh in discharges)if(mesh!=null)Destroy(mesh);discharges=null;}
        // Six prebuilt branching volumes: stable endpoints during each discharge.
        // Topology changes between strikes, never by swinging a rigid bolt.
        private static Mesh DischargeMesh(int index)
        {
            if(discharges==null)
            {
                discharges=new Mesh[6];
                for(int variant=0;variant<discharges.Length;variant++)
                {
                    var random=new System.Random(9173+variant*131);
                    var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
                    var path=new Vector3[10];
                    for(int point=0;point<path.Length;point++)
                    {
                        float t=point/(float)(path.Length-1);
                        float spread=point==0||point==path.Length-1?0:.75f;
                        path[point]=new Vector3(((float)random.NextDouble()-.5f)*spread,4.8f*(1-t),((float)random.NextDouble()-.5f)*spread);
                    }
                    for(int point=0;point<path.Length-1;point++)
                    {
                        DischargeSegment(vertices,uv,triangles,path[point],path[point+1],.075f,.045f);
                        if(point==2||point==4||point==6)
                        {
                            float angle=(float)random.NextDouble()*Mathf.PI*2;
                            var fork=path[point]+new Vector3(Mathf.Cos(angle)*.65f,-.55f,Mathf.Sin(angle)*.65f);
                            var tip=fork+new Vector3(Mathf.Cos(angle)*.35f,-.65f,Mathf.Sin(angle)*.35f);
                            DischargeSegment(vertices,uv,triangles,path[point],fork,.032f,.018f);
                            DischargeSegment(vertices,uv,triangles,fork,tip,.018f,.002f);
                        }
                    }
                    for(int branch=0;branch<4;branch++)
                    {
                        float angle=branch*Mathf.PI*.5f+variant;
                        DischargeSegment(vertices,uv,triangles,Vector3.up*.05f,new Vector3(Mathf.Cos(angle)*.4f,.12f,Mathf.Sin(angle)*.4f),.028f,.002f);
                    }
                    var mesh=new Mesh{name="Latched branched lightning "+variant};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();discharges[variant]=mesh;
                }
            }
            return discharges[index%discharges.Length];
        }
        private static void DischargeSegment(List<Vector3> v,List<Vector2> uv,List<int> tr,Vector3 a,Vector3 b,float start,float end)
        {
            Vector3 axis=(b-a).normalized,side=Vector3.Cross(axis,Vector3.forward).normalized;
            if(side.sqrMagnitude<.1f)side=Vector3.right;
            Vector3 other=Vector3.Cross(axis,side);int first=v.Count;
            for(int ring=0;ring<2;ring++)for(int face=0;face<6;face++)
            {
                float angle=face*Mathf.PI/3;
                v.Add((ring==0?a:b)+(side*Mathf.Cos(angle)+other*Mathf.Sin(angle))*(ring==0?start:end));uv.Add(new Vector2(face/6f,ring));
            }
            for(int face=0;face<6;face++)
            {int next=(face+1)%6;tr.Add(first+face);tr.Add(first+6+face);tr.Add(first+next);tr.Add(first+next);tr.Add(first+6+face);tr.Add(first+6+next);}
        }
        private void LatchDischarges()
        {
            dischargeArmed=true;dischargeBeat++;
            int contact=0;
            for(int i=0;i<parts.Length;i++)
            {
                Vector3 point=transform.position;
                bool matched=false;
                while(contact<session.Enemies.Count)
                {
                    var enemy=session.Enemies[contact++];
                    if(enemy==null||enemy.IsDead||CombatFx.Flat(enemy.transform.position-transform.position).magnitude>size||!CombatSight.Area(transform.position,enemy.transform.position))continue;
                    point=enemy.transform.position;matched=true;break;
                }
                if(!matched)
                {
                    float angle=(i*2.399963f+dischargeBeat*.73f),radius=size*(.2f+((i+dischargeBeat)%3)*.2f);
                    point=transform.position+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius;
                    if(!CombatSight.Area(transform.position,point))point=transform.position;
                }
                point.y=WorldTraversal.SurfaceHeight(point);
                parts[i].position=point;parts[i].rotation=Quaternion.identity;
                parts[i].GetComponent<MeshFilter>().sharedMesh=DischargeMesh(i+dischargeBeat);
            }
        }
        private float DischargeOpacity(int strand)
        {
            if(!dischargeArmed)return 0;
            float t=beatAge-strand*.008f;
            if(t<0)return 0;
            if(t<.07f)return 1-t/.07f*.45f;
            if(t<.13f)return .1f;
            if(t<.165f)return .8f;
            return .22f*Mathf.Clamp01(1-(t-.165f)/.16f);
        }
    }
}
