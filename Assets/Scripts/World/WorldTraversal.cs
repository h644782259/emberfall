using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    /// <summary>Shared ground rules for direct movement, leaps and creature routes.</summary>
    public static partial class WorldTraversal
    {
        public sealed class ObstacleHandle { internal ObstacleHandle(){} }
        private struct Obstacle { public Vector2 Center, Half; public float Radius,Height; public ObstacleHandle Handle; public bool HasPhysicalBounds; public Vector3 PhysicalCenter,PhysicalHalf; }
        // Immutable-in-use search order; private and never returned or written after initialization.
        private static readonly int[] NeighborX = { -1, 0, 1, -1, 1, -1, 0, 1 };
        private static readonly int[] NeighborY = { -1, -1, -1, 0, 0, 1, 1, 1 };
        private static readonly List<Obstacle> obstacles = new List<Obstacle>();
        // The exact circle/rounded-box rules below remain authoritative. This
        // broad phase only excludes solids whose bounds cannot reach a query.
        private const float SolidCell = 4f;
        private static readonly Dictionary<long,List<int>> solidBuckets = new Dictionary<long,List<int>>();
        private static readonly List<int> broadSolids = new List<int>(), solidCandidates = new List<int>();
        private static int solidIndexRevision = -1;
        private static uint[] solidMarks = new uint[0];
        private static uint solidQuery;
        private static bool solidCandidateCellsValid;
        private static int candidateA,candidateB,candidateC,candidateD;
        private static long SolidKey(int x,int z){return ((long)x<<32)|(uint)z;}
        private static bool SolidCells(double x0,double x1,double z0,double z1,out int a,out int b,out int c,out int d)
        {
            a=b=c=d=0;
            if(double.IsNaN(x0)||double.IsNaN(x1)||double.IsNaN(z0)||double.IsNaN(z1)||
                x0<-4096||x1>4096||z0<-4096||z1>4096)return false;
            a=(int)System.Math.Floor(x0/SolidCell);b=(int)System.Math.Floor(x1/SolidCell);
            c=(int)System.Math.Floor(z0/SolidCell);d=(int)System.Math.Floor(z1/SolidCell);
            return b>=a&&d>=c&&b-a<=64&&d-c<=64&&(b-a+1)*(d-c+1)<=1024;
        }
        private static void EnsureSolidIndex()
        {
            if(solidIndexRevision==revision)return;
            solidBuckets.Clear();broadSolids.Clear();solidCandidates.Clear();solidCandidateCellsValid=false;
            if(solidMarks.Length<obstacles.Count)solidMarks=new uint[obstacles.Count];
            System.Array.Clear(solidMarks,0,solidMarks.Length);solidQuery=0;
            for(int i=0;i<obstacles.Count;i++)
            {
                var o=obstacles[i];double ex=o.Radius>0?o.Radius:System.Math.Max(0,o.Half.x),ez=o.Radius>0?o.Radius:System.Math.Max(0,o.Half.y);
                double x0=o.Center.x-ex-.001,x1=o.Center.x+ex+.001,z0=o.Center.y-ez-.001,z1=o.Center.y+ez+.001;
                if(o.HasPhysicalBounds)
                {
                    x0=System.Math.Min(x0,(double)o.PhysicalCenter.x-o.PhysicalHalf.x-.001);
                    x1=System.Math.Max(x1,(double)o.PhysicalCenter.x+o.PhysicalHalf.x+.001);
                    z0=System.Math.Min(z0,(double)o.PhysicalCenter.z-o.PhysicalHalf.z-.001);
                    z1=System.Math.Max(z1,(double)o.PhysicalCenter.z+o.PhysicalHalf.z+.001);
                }
                if(!Finite(o.Center.x)||!Finite(o.Center.y)||!Finite(o.Radius)||!Finite(o.Half.x)||!Finite(o.Half.y)||
                    !SolidCells(x0,x1,z0,z1,out int a,out int b,out int c,out int d))
                {broadSolids.Add(i);continue;}
                for(int x=a;x<=b;x++)for(int z=c;z<=d;z++)
                {long key=SolidKey(x,z);if(!solidBuckets.TryGetValue(key,out var bucket)){bucket=new List<int>();solidBuckets.Add(key,bucket);}bucket.Add(i);}
            }
            solidIndexRevision=revision;
        }
        private static void CollectSolidCandidates(Vector3 point,float radius)
        {
            double reach=System.Math.Abs(radius)+.001;
            CollectSolidCandidatesBounds(point.x-reach,point.x+reach,point.z-reach,point.z+reach);
        }
        // The complete projected footprint is conservative. Exact support and
        // triangle tests below still decide whether a candidate blocks the skill.
        private static void CollectSolidCandidatesBounds(double x0,double x1,double z0,double z1)
        {
            EnsureSolidIndex();
            if(!SolidCells(x0,x1,z0,z1,out int a,out int b,out int c,out int d))
            {solidCandidateCellsValid=false;solidCandidates.Clear();for(int i=0;i<obstacles.Count;i++)solidCandidates.Add(i);return;}
            // Consecutive samples along a route usually occupy the same cells.
            // Reuse only their candidate list; exact point/height checks still run.
            if(solidCandidateCellsValid&&a==candidateA&&b==candidateB&&c==candidateC&&d==candidateD)return;
            candidateA=a;candidateB=b;candidateC=c;candidateD=d;solidCandidateCellsValid=true;
            solidCandidates.Clear();
            unchecked{solidQuery++;}if(solidQuery==0){System.Array.Clear(solidMarks,0,solidMarks.Length);solidQuery=1;}
            foreach(int i in broadSolids){solidMarks[i]=solidQuery;solidCandidates.Add(i);}
            for(int x=a;x<=b;x++)for(int z=c;z<=d;z++)
                if(solidBuckets.TryGetValue(SolidKey(x,z),out var bucket))foreach(int i in bucket)
                {if(solidMarks[i]==solidQuery)continue;solidMarks[i]=solidQuery;solidCandidates.Add(i);}
        }
        // Thin rendered floors support bodies/VFX without occupying navigation.
        private struct SupportTriangle{public Vector3 A,B,C;public bool Terrain;public float X0,X1,Z0,Z1;}
        private static readonly Dictionary<int,List<SupportTriangle>> groundSupports=new Dictionary<int,List<SupportTriangle>>();
        private static int SupportCell(float p){return (int)System.Math.Floor(p/2f);}
        private static int SupportKey(int x,int z){return (x+256)*1024+z+256;}
        public static void AddSurfaceBox(Vector3 center,Vector2 size,float top,bool terrain=false)
        {
            center=ScenePoint(center);size*=sceneBuildScale;
            Vector3 a=new Vector3(center.x-size.x*.5f,top,center.z-size.y*.5f),b=new Vector3(center.x+size.x*.5f,top,center.z-size.y*.5f);
            Vector3 c=new Vector3(center.x+size.x*.5f,top,center.z+size.y*.5f),d=new Vector3(center.x-size.x*.5f,top,center.z+size.y*.5f);
            AddSurfaceTriangle(a,b,c,terrain);AddSurfaceTriangle(a,c,d,terrain);
        }
        // Vertices are already in actual world space, as submitted to the shader.
        public static void AddSurfaceTriangle(Vector3 a,Vector3 b,Vector3 c,bool terrain=false)
        {
            if(!Finite(a.x)||!Finite(a.y)||!Finite(a.z)||!Finite(b.x)||!Finite(b.y)||!Finite(b.z)||!Finite(c.x)||!Finite(c.y)||!Finite(c.z))return;
            double area=((double)b.x-a.x)*(c.z-a.z)-((double)b.z-a.z)*(c.x-a.x);if(System.Math.Abs(area)<.0000001)return;
            var t=new SupportTriangle{A=a,B=b,C=c,Terrain=terrain,X0=Mathf.Min(a.x,Mathf.Min(b.x,c.x)),X1=Mathf.Max(a.x,Mathf.Max(b.x,c.x)),Z0=Mathf.Min(a.z,Mathf.Min(b.z,c.z)),Z1=Mathf.Max(a.z,Mathf.Max(b.z,c.z))};
            // Only nearby triangles are visited by a body/particle ground query.
            int x0=SupportCell(t.X0),x1=SupportCell(t.X1),z0=SupportCell(t.Z0),z1=SupportCell(t.Z1);if(x1-x0>128||z1-z0>128)return;
            for(int x=x0;x<=x1;x++)for(int z=z0;z<=z1;z++){int key=SupportKey(x,z);if(!groundSupports.TryGetValue(key,out var bucket)){bucket=new List<SupportTriangle>();groundSupports.Add(key,bucket);}bucket.Add(t);}
        }
        private static float GroundSupportHeight(Vector3 point,float ceiling=float.MaxValue)
        {
            float terrainHeight=WorldTerrain.Height(point),height=terrainHeight;if(!groundSupports.TryGetValue(SupportKey(SupportCell(point.x),SupportCell(point.z)),out var bucket))return height;
            foreach(var t in bucket)
            {
                if(point.x<t.X0-.0001f||point.x>t.X1+.0001f||point.z<t.Z0-.0001f||point.z>t.Z1+.0001f)continue;
                double d=((double)t.B.z-t.C.z)*(t.A.x-t.C.x)+((double)t.C.x-t.B.x)*(t.A.z-t.C.z);
                double a=(((double)t.B.z-t.C.z)*(point.x-t.C.x)+((double)t.C.x-t.B.x)*(point.z-t.C.z))/d;
                double b=(((double)t.C.z-t.A.z)*(point.x-t.C.x)+((double)t.A.x-t.C.x)*(point.z-t.C.z))/d,c=1-a-b;
                if(a<-.00001||b<-.00001||c<-.00001)continue;
                float top=(float)(a*t.A.y+b*t.B.y+c*t.C.y)+(t.Terrain?terrainHeight:0);
                if(top<=ceiling)height=Mathf.Max(height,top);
            }
            return height;
        }
        private static readonly Dictionary<int, bool[]> grids = new Dictionary<int, bool[]>();
        private static readonly Dictionary<float,ushort[]> routeEdges = new Dictionary<float,ushort[]>();
        private static int routeEdgeRevision=-1;
        private static bool ClearGridEdge(int node,int candidate,int direction,float radius)
        {
            if(routeEdgeRevision!=revision){routeEdges.Clear();routeEdgeRevision=revision;}
            if(!routeEdges.TryGetValue(radius,out var edges))
            {
                // Exact radii retain the existing clearance rules. Bound storage
                // even if future actors continuously vary their body size.
                if(routeEdges.Count>=64)return ClearSegment(Point(node),Point(candidate),radius,false);
                edges=new ushort[Side*Side];routeEdges.Add(radius,edges);
            }
            int tested=1<<direction,clear=tested<<8;
            if((edges[node]&tested)==0)
            {
                bool open=ClearSegment(Point(node),Point(candidate),radius,false);
                edges[node]|=(ushort)(tested|(open?clear:0));
            }
            return (edges[node]&clear)!=0;
        }
        private static Vector3[] river;
        private static float riverHalfWidth;
        private static Rect bridge;
        private static float arena = 22;
        private static float sceneBuildScale=1;
        internal static float SceneLocalArenaRadius {get{return arena/sceneBuildScale;}}
        internal static float SceneBuildScale {get{return sceneBuildScale;}}
        internal static void BeginSceneBuild(float scale){sceneBuildScale=Finite(scale)&&scale>=1&&scale<=1.5f?scale:1;}
        internal static void EndSceneBuild(){sceneBuildScale=1;}
        private static Vector3 ScenePoint(Vector3 p){return new Vector3(p.x*sceneBuildScale,p.y,p.z*sceneBuildScale);}
        private static int revision;
        public static int Revision {get{return revision;}}
        private static int mapKeyRevision = int.MinValue;
        private static string mapKey;
        // Revision is an invalidation signal, not a map identity: a rebuilt camp
        // has the same navigation geometry despite receiving a new revision.
        public static string NavigationMapKey
        {
            get
            {
                if(mapKeyRevision==revision)return mapKey;
                var key=new System.Text.StringBuilder();
                AppendMapValue(key,arena);
                if(river!=null){AppendMapValue(key,riverHalfWidth);AppendMapValue(key,bridge.xMin);AppendMapValue(key,bridge.yMin);AppendMapValue(key,bridge.xMax);AppendMapValue(key,bridge.yMax);}
                key.Append('|').Append(river==null?0:river.Length);
                if(river!=null)foreach(var point in river){AppendMapValue(key,point.x);AppendMapValue(key,point.z);}
                key.Append('|').Append(obstacles.Count);
                foreach(var obstacle in obstacles)
                {AppendMapValue(key,obstacle.Center.x);AppendMapValue(key,obstacle.Center.y);AppendMapValue(key,obstacle.Half.x);AppendMapValue(key,obstacle.Half.y);AppendMapValue(key,obstacle.Radius);}
                mapKey=key.ToString();mapKeyRevision=revision;return mapKey;
            }
        }
        private static void AppendMapValue(System.Text.StringBuilder key,float value)
        {key.Append(';').Append(value.ToString("R",System.Globalization.CultureInfo.InvariantCulture));}
        // One solid/river traversal per pixel instead of up to three traversals.
        public static int MapSurface(Vector3 point,float radius=.16f)
        {
            if(!ClearOfSolids(point,radius))return 0;
            Vector2 p=new Vector2(point.x,point.z);
            if(river==null||(p.x>=bridge.xMin+radius&&p.x<=bridge.xMax-radius&&p.y>=bridge.yMin&&p.y<=bridge.yMax))return 1;
            for(int i=1;i<river.Length;i++)if(CombatFx.SegmentDistance(point,river[i-1],river[i])<riverHalfWidth+radius)return 2;
            return 1;
        }
        private const float Cell = .7f;
        private const int Side = 103;
        private const float GridOrigin = -35.7f;
        public static void Reset(ZoneKind zone)
        {
            WorldTerrain.Configure(zone,-1);
            sceneBuildScale=1;obstacles.Clear(); groundSupports.Clear();grids.Clear(); river = null; arena = zone == ZoneKind.Dungeon ? 18 : 22; revision++;
        }
        public static float ArenaRadius { get { return arena; } }
        public static void SetArenaRadius(float radius)
        {
            if(!Finite(radius)||radius<18||radius>35)return;
            if(arena==radius)return;
            arena=radius;grids.Clear();revision++;
        }
        public static void AddCircle(Vector3 center, float radius)
        {
            center=ScenePoint(center);radius*=sceneBuildScale;
            obstacles.Add(new Obstacle { Center = new Vector2(center.x, center.z), Radius = radius }); grids.Clear(); revision++;
        }
        public static void AddBox(Vector3 center, Vector2 size)
        {
            center=ScenePoint(center);size*=sceneBuildScale;
            obstacles.Add(new Obstacle { Center = new Vector2(center.x, center.z), Half = size * .5f }); grids.Clear(); revision++;
        }
        public static ObstacleHandle AddDynamicCircle(Vector3 center,float radius)
        {
            if(!Finite(center.x)||!Finite(center.z)||!Finite(radius)||radius<=0)return null;
            center=ScenePoint(center);radius*=sceneBuildScale;
            var handle=new ObstacleHandle();
            obstacles.Add(new Obstacle{Center=new Vector2(center.x,center.z),Radius=radius,Handle=handle});
            grids.Clear();revision++;return handle;
        }
        public static ObstacleHandle AddDynamicBox(Vector3 center,Vector2 size)
        {
            if(!Finite(center.x)||!Finite(center.z)||!Finite(size.x)||!Finite(size.y)||size.x<=0||size.y<=0)return null;
            center=ScenePoint(center);size*=sceneBuildScale;
            var handle=new ObstacleHandle();
            obstacles.Add(new Obstacle{Center=new Vector2(center.x,center.z),Half=size*.5f,Handle=handle});
            grids.Clear();revision++;return handle;
        }
        public static bool RemoveDynamicObstacle(ObstacleHandle handle)
        {
            if(handle==null)return false;
            for(int i=0;i<obstacles.Count;i++)if(object.ReferenceEquals(obstacles[i].Handle,handle))
            {obstacles.RemoveAt(i);grids.Clear();revision++;return true;}
            return false;
        }
        internal static void SetPhysicalBounds(ObstacleHandle handle,Vector3 center,Vector3 size)
        {
            if(handle==null||!Finite(center.x)||!Finite(center.y)||!Finite(center.z)||!Finite(size.x)||!Finite(size.y)||!Finite(size.z)||size.x<=0||size.y<=0||size.z<=0)return;
            for(int i=0;i<obstacles.Count;i++)if(object.ReferenceEquals(obstacles[i].Handle,handle))
            {var o=obstacles[i];o.HasPhysicalBounds=true;o.PhysicalCenter=center;o.PhysicalHalf=size*.5f;obstacles[i]=o;solidIndexRevision=-1;return;}
        }
        public static bool HasLineOfSightIgnoringObstacle(Vector3 from,Vector3 to,ObstacleHandle handle)
        {return ClearSegment(from,to,.04f,true,handle);}
        public static void SetRiver(Vector3[] centerline, float width, Rect crossing)
        {
            river=centerline==null?null:new Vector3[centerline.Length];if(river!=null)for(int i=0;i<river.Length;i++)river[i]=ScenePoint(centerline[i]);
            riverHalfWidth=width*sceneBuildScale*.5f;bridge=new Rect(crossing.xMin*sceneBuildScale,crossing.yMin*sceneBuildScale,(crossing.xMax-crossing.xMin)*sceneBuildScale,(crossing.yMax-crossing.yMin)*sceneBuildScale); grids.Clear(); revision++;
        }
        public static bool IsWalkable(Vector3 point, float radius = .45f)
        {
            if (!ClearOfSolids(point, radius)) return false;
            Vector2 p = new Vector2(point.x, point.z);
            if (river == null || (p.x >= bridge.xMin + radius && p.x <= bridge.xMax - radius && p.y >= bridge.yMin && p.y <= bridge.yMax)) return true;
            for (int i = 1; i < river.Length; i++)
                if (CombatFx.SegmentDistance(point, river[i - 1], river[i]) < riverHalfWidth + radius) return false;
            return true;
        }
        // Navigation map classification uses the same geometry as movement. Bridges
        // remain ground; solid obstacles are never mislabelled as water.
        public static bool IsOpenWater(Vector3 point, float radius = .16f)
        { return ClearOfSolids(point, radius) && !IsWalkable(point, radius); }
        private static bool ClearOfSolids(Vector3 point, float radius, ObstacleHandle ignored=null)
        {
            Vector2 p = new Vector2(point.x, point.z);
            if (p.sqrMagnitude > (arena - radius) * (arena - radius)) return false;
            // Synchronous geometry only: this reusable buffer is never exposed
            // or held across callbacks/nested traversal queries.
            CollectSolidCandidates(point,radius);
            foreach (int index in solidCandidates)
            {
                Obstacle obstacle=obstacles[index];
                if(ignored!=null&&object.ReferenceEquals(obstacle.Handle,ignored))continue;
                Vector2 delta = p - obstacle.Center;
                if (obstacle.Radius > 0) { if (delta.sqrMagnitude < (obstacle.Radius + radius) * (obstacle.Radius + radius)) return false; }
                else
                {
                    Vector2 nearest = new Vector2(Mathf.Max(0, Mathf.Abs(delta.x) - obstacle.Half.x), Mathf.Max(0, Mathf.Abs(delta.y) - obstacle.Half.y));
                    if (nearest.sqrMagnitude <= radius * radius) return false;
                }
            }
            return true;
        }
        // The union of these three fans is the convex hull of origin and the face.
        // Every ray to every point of the face (also under radial animation shrink)
        // lies in that hull. Exact distances use the same .04 LOS clearance and the
        // same registered solids; unlike a constant swept disk, no face-width radius
        // is imposed at the real impact origin. Water is intentionally not a solid.
        public static bool HasClearVisualTriangle(Vector3 origin,Vector3 a,Vector3 b,Vector3 c)
        {
            const double clearance=.04f;
            double limit=arena-clearance;
            if(!VisualInsideArena(origin,limit)||!VisualInsideArena(a,limit)||!VisualInsideArena(b,limit)||!VisualInsideArena(c,limit))return false;
            double padding=clearance+.001;
            CollectSolidCandidatesBounds(System.Math.Min(origin.x,System.Math.Min(a.x,System.Math.Min(b.x,c.x)))-padding,
                System.Math.Max(origin.x,System.Math.Max(a.x,System.Math.Max(b.x,c.x)))+padding,
                System.Math.Min(origin.z,System.Math.Min(a.z,System.Math.Min(b.z,c.z)))-padding,
                System.Math.Max(origin.z,System.Math.Max(a.z,System.Math.Max(b.z,c.z)))+padding);
            foreach(int index in solidCandidates)
            {
                Obstacle obstacle=obstacles[index];
                if(VisualFanClear(origin,a,b,obstacle,clearance)&&VisualFanClear(origin,b,c,obstacle,clearance)&&VisualFanClear(origin,c,a,obstacle,clearance))continue;
                // A supported top is a floor. All four real vertex heights must
                // clear it; mixed high/low faces still clip at the platform side.
                if(obstacle.Height>0&&VisualAboveSupport(origin,obstacle)&&VisualAboveSupport(a,obstacle)&&VisualAboveSupport(b,obstacle)&&VisualAboveSupport(c,obstacle))continue;
                return false;
            }
            return true;
        }
        private static bool VisualAboveSupport(Vector3 p,Obstacle o)
        {return Finite(p.y)&&p.y>=WorldTerrain.Height(p)+o.Height-.015f;}
        private static bool VisualInsideArena(Vector3 p,double limit)
        {return Finite(p.x)&&Finite(p.y)&&Finite(p.z)&&(double)p.x*p.x+(double)p.z*p.z<=limit*limit;}
        private static bool VisualFanClear(Vector3 a,Vector3 b,Vector3 c,Obstacle obstacle,double clearance)
        {
            // Strictly disjoint bounds cannot affect the exact triangle test below.
            double ex=(obstacle.Radius>0?obstacle.Radius:obstacle.Half.x)+clearance+.001;
            double ez=(obstacle.Radius>0?obstacle.Radius:obstacle.Half.y)+clearance+.001;
            if(System.Math.Max(a.x,System.Math.Max(b.x,c.x))<obstacle.Center.x-ex||
                System.Math.Min(a.x,System.Math.Min(b.x,c.x))>obstacle.Center.x+ex||
                System.Math.Max(a.z,System.Math.Max(b.z,c.z))<obstacle.Center.y-ez||
                System.Math.Min(a.z,System.Math.Min(b.z,c.z))>obstacle.Center.y+ez)return true;
            Vector3 center=new Vector3(obstacle.Center.x,0,obstacle.Center.y);
            if(obstacle.Radius>0)
            {double radius=obstacle.Radius+clearance;return VisualPointTriangleDistance(center,a,b,c)>=radius*radius;}
            // Distance to the actual rectangle, not its expanded AABB, preserves
            // legal near-corner rays while respecting its round .04 clearance.
            double x0=(double)obstacle.Center.x-obstacle.Half.x,x1=(double)obstacle.Center.x+obstacle.Half.x;
            double z0=(double)obstacle.Center.y-obstacle.Half.y,z1=(double)obstacle.Center.y+obstacle.Half.y;
            if(VisualInsideBox(a,x0,x1,z0,z1)||VisualInsideBox(b,x0,x1,z0,z1)||VisualInsideBox(c,x0,x1,z0,z1))return false;
            Vector3 p=new Vector3((float)x0,0,(float)z0),q=new Vector3((float)x1,0,(float)z0),r=new Vector3((float)x1,0,(float)z1),t=new Vector3((float)x0,0,(float)z1);
            double distance=System.Math.Min(System.Math.Min(VisualPointTriangleDistance(p,a,b,c),VisualPointTriangleDistance(q,a,b,c)),System.Math.Min(VisualPointTriangleDistance(r,a,b,c),VisualPointTriangleDistance(t,a,b,c)));
            distance=System.Math.Min(distance,VisualEdgeBoxDistance(a,b,p,q,r,t));
            distance=System.Math.Min(distance,VisualEdgeBoxDistance(b,c,p,q,r,t));
            distance=System.Math.Min(distance,VisualEdgeBoxDistance(c,a,p,q,r,t));
            return distance>clearance*clearance;
        }
        private static bool VisualInsideBox(Vector3 p,double x0,double x1,double z0,double z1)
        {return p.x>=x0&&p.x<=x1&&p.z>=z0&&p.z<=z1;}
        private static double VisualEdgeBoxDistance(Vector3 a,Vector3 b,Vector3 p,Vector3 q,Vector3 r,Vector3 t)
        {return System.Math.Min(System.Math.Min(VisualSegmentsDistance(a,b,p,q),VisualSegmentsDistance(a,b,q,r)),System.Math.Min(VisualSegmentsDistance(a,b,r,t),VisualSegmentsDistance(a,b,t,p)));}
        private static double VisualCross(Vector3 a,Vector3 b,Vector3 p)
        {return ((double)b.x-a.x)*((double)p.z-a.z)-((double)b.z-a.z)*((double)p.x-a.x);}
        private static double VisualPointSegmentDistance(Vector3 p,Vector3 a,Vector3 b)
        {
            double dx=(double)b.x-a.x,dz=(double)b.z-a.z,len=dx*dx+dz*dz;
            double t=len==0?0:System.Math.Max(0,System.Math.Min(1,(((double)p.x-a.x)*dx+((double)p.z-a.z)*dz)/len));
            double x=(double)p.x-a.x-t*dx,z=(double)p.z-a.z-t*dz;return x*x+z*z;
        }
        private static double VisualPointTriangleDistance(Vector3 p,Vector3 a,Vector3 b,Vector3 c)
        {
            double x=VisualCross(a,b,p),y=VisualCross(b,c,p),z=VisualCross(c,a,p);
            if(VisualCross(a,b,c)!=0&&((x>=0&&y>=0&&z>=0)||(x<=0&&y<=0&&z<=0)))return 0;
            return System.Math.Min(VisualPointSegmentDistance(p,a,b),System.Math.Min(VisualPointSegmentDistance(p,b,c),VisualPointSegmentDistance(p,c,a)));
        }
        private static double VisualSegmentsDistance(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
        {
            double x=VisualCross(a,b,c),y=VisualCross(a,b,d),z=VisualCross(c,d,a),w=VisualCross(c,d,b);
            if(((x>0&&y<0)||(x<0&&y>0))&&((z>0&&w<0)||(z<0&&w>0)))return 0;
            return System.Math.Min(System.Math.Min(VisualPointSegmentDistance(a,c,d),VisualPointSegmentDistance(b,c,d)),System.Math.Min(VisualPointSegmentDistance(c,a,b),VisualPointSegmentDistance(d,a,b)));
        }
        // Exact solid clearance for the complete telegraphed sweep capsule.
        // Sampling alone can miss a narrow pillar tangent between sample points.
        public static bool HasClearSweepCapsule(Vector3 from,Vector3 to,float radius)
        {
            if(!Finite(radius)||radius<=0||!VisualInsideArena(from,arena-radius)||!VisualInsideArena(to,arena-radius))return false;
            foreach(var obstacle in obstacles)if(!VisualFanClear(from,to,to,obstacle,radius))return false;
            return river==null||HasGroundPath(from,to,radius); // Keep the established river/bridge policy.
        }
        public static bool HasClearVisualFootprint(Vector3 origin, Vector3 point, float radius)
        { return ClearSegment(origin, point, Mathf.Max(.04f,radius), true); }
        public static bool HasLineOfSight(Vector3 from, Vector3 to) { return ClearSegment(from, to, .04f, true); }
        public static bool HasGroundPath(Vector3 from, Vector3 to, float radius = .45f) { return ClearSegment(from, to, radius, false); }
        public static bool CanLeap(Vector3 from, Vector3 to, float radius = .45f)
        {
            return IsWalkable(from, radius) && IsWalkable(to, radius) && ClearSegment(from, to, radius, true);
        }
        // Water may be crossed, but solids and arena edges stop the entire path.
        // Keep the furthest safe landing before that stop rather than reject a
        // useful shorter blink just because the requested endpoint is unsafe.
        public static bool TryResolveBlink(Vector3 from, Vector3 direction, float distance, float radius, float bound, out Vector3 landing)
        {
            landing = CombatFx.Flat(from);
            direction = CombatFx.Flat(direction);
            if (!Finite(landing.x) || !Finite(landing.z) || !Finite(direction.x) || !Finite(direction.z) ||
                !Finite(distance) || !Finite(radius) || !Finite(bound) || distance <= 0 || radius <= 0 || bound <= radius ||
                direction.sqrMagnitude < .0001f || !IsWalkable(landing, radius)) return false;
            Vector3 origin = landing;
            direction.Normalize();
            float safeDistance = PlayerUpgradeRules.FindSafeBlinkDistance(distance,
                travelled =>
                {
                    Vector3 point = origin + direction * travelled;
                    return point.sqrMagnitude <= bound * bound && ClearOfSolids(point, radius);
                },
                travelled => IsWalkable(origin + direction * travelled, radius));
            landing = origin + direction * safeDistance;
            landing.y=GroundSupportHeight(landing);
            return safeDistance >= .35f;
        }

        // A blocked movement skill still casts: use the last safe point, including the origin.
        public static Vector3 ResolveSkillLanding(Vector3 from, Vector3 direction, float distance, float radius, float bound)
        {
            Vector3 landing;
            if(from.y>GroundSupportHeight(from)+.05f)
            {return TryResolvePlatformJump(from,direction,distance,radius,out landing)?landing:from;}
            TryResolveBlink(from,direction,distance,radius,bound,out landing);
            return landing;
        }

        public static ObstacleHandle AddPlatform(Vector3 center,Vector2 size,float height)
        {
            if(!Finite(height)||height<.2f||height>1.1f||size.x<1.2f||size.y<1.2f)return null;
            var handle=AddDynamicBox(center,size);
            if(handle==null)return null;
            int last=obstacles.Count-1;var obstacle=obstacles[last];obstacle.Height=height;obstacles[last]=obstacle;return handle;
        }
        public static void AddJumpPlatform(Vector3 center,float radius,float height)
        {
            center=ScenePoint(center);radius*=sceneBuildScale;
            obstacles.Add(new Obstacle { Center=new Vector2(center.x,center.z), Radius=radius, Height=height });
            grids.Clear();revision++;
        }
        private static bool ContainsTop(Obstacle o,Vector3 point,float radius)
        {
            if(o.Height<=0)return false;
            if(o.Radius>0)
            {float r=Mathf.Max(0,o.Radius-.12f);return new Vector2(point.x-o.Center.x,point.z-o.Center.y).sqrMagnitude<=r*r;}
            return Mathf.Abs(point.x-o.Center.x)<=o.Half.x-radius&&Mathf.Abs(point.z-o.Center.y)<=o.Half.y-radius;
        }
        public static float SurfaceHeight(Vector3 point,float radius=.45f)
        {
            float height=GroundSupportHeight(point);
            CollectSolidCandidates(point,radius);
            bool terrainSampled=false;float terrainHeight=0;
            foreach(int index in solidCandidates)
            {
                var o=obstacles[index];if(!ContainsTop(o,point,radius))continue;
                if(!terrainSampled){terrainHeight=WorldTerrain.Height(point);terrainSampled=true;}
                height=Mathf.Max(height,terrainHeight+o.Height);
            }
            return height;
        }
        internal static bool IsPhysicalSpaceClear(Vector3 point,float radius,ObstacleHandle ignored=null)
        {return ClearAtHeight(point,radius,point.y-radius,ignored,true);}
        internal static float PhysicalSurfaceHeight(Vector3 point,float radius,float previousHeight)
        {
            float top=SurfaceHeight(point,radius);
            foreach(var o in obstacles)if(o.HasPhysicalBounds)
            {
                float surface=o.PhysicalCenter.y+o.PhysicalHalf.y;
                if(previousHeight-radius<surface-.015f)continue;
                Vector3 delta=point-o.PhysicalCenter;
                float x=Mathf.Max(0,Mathf.Abs(delta.x)-o.PhysicalHalf.x),z=Mathf.Max(0,Mathf.Abs(delta.z)-o.PhysicalHalf.z);
                if(x*x+z*z<radius*radius)top=Mathf.Max(top,surface);
            }
            return top;
        }
        private static bool ClearAtHeight(Vector3 point,float radius,float height,ObstacleHandle ignored=null,bool physical=false)
        {
            if(!Finite(point.x)||!Finite(point.z)||!Finite(height)||CombatFx.Flat(point).magnitude>arena-radius)return false;
            CollectSolidCandidates(point,radius);
            foreach(int index in solidCandidates)
            {
                var o=obstacles[index];
                if(ignored!=null&&object.ReferenceEquals(o.Handle,ignored))continue;
                if(physical&&o.HasPhysicalBounds)
                {
                    Vector3 d3=point-o.PhysicalCenter;
                    float x=Mathf.Max(0,Mathf.Abs(d3.x)-o.PhysicalHalf.x),y=Mathf.Max(0,Mathf.Abs(d3.y)-o.PhysicalHalf.y),z=Mathf.Max(0,Mathf.Abs(d3.z)-o.PhysicalHalf.z);
                    if(x*x+y*y+z*z<=radius*radius)return false;
                    continue;
                }
                Vector2 d=new Vector2(point.x-o.Center.x,point.z-o.Center.y);
                bool overlaps;
                if(o.Radius>0){float clearance=o.Height>0?Mathf.Min(radius,.12f):radius;overlaps=d.sqrMagnitude<(o.Radius+clearance)*(o.Radius+clearance);}
                else {var nearest=new Vector2(Mathf.Max(0,Mathf.Abs(d.x)-o.Half.x),Mathf.Max(0,Mathf.Abs(d.y)-o.Half.y));overlaps=nearest.sqrMagnitude<=radius*radius;}
                // Distant platforms cannot block this sample; don't evaluate their terrain.
                if(!overlaps)continue;
                if(o.Height>0&&height>=WorldTerrain.Height(point)+o.Height-.015f)continue;
                return false;
            }
            return true;
        }
        public static bool CanStand(Vector3 point,float radius=.45f)
        {
            float height=StandingHeight(point,radius,point.y);
            return Mathf.Abs(point.y-height)<.035f&&ClearAtHeight(point,radius,height)&&(height>0||IsWalkable(point,radius));
        }
        public static Vector3 JumpPosition(Vector3 from,Vector3 to,float progress)
        {
            float t=Mathf.Clamp01(progress);
            // Rise clear of the starting ledge before moving across its side.
            float travel=t;
            return Vector3.Lerp(from,to,travel)+Vector3.up*(6.6f*t*(1-t));
        }
        public static bool TryResolvePlatformJump(Vector3 from,Vector3 direction,float distance,float radius,out Vector3 landing)
        {
            landing=from;direction=CombatFx.Flat(direction).normalized;
            if(!CanStand(from,radius)||direction.sqrMagnitude<.01f)return false;
            // Try the intended distance first; shorter safe points allow landing on
            // a narrow box without requiring pixel-perfect movement input.
            for(int pass=0;pass<(from.y<=GroundSupportHeight(from)+.05f?2:1);pass++)
            for(float d=distance;d>=.05f;d-=.05f)
            {
                var to=CombatFx.Flat(from)+direction*d;to.y=SurfaceHeight(to,radius);
                if(from.y<=GroundSupportHeight(from)+.05f&&(pass==0?to.y<=GroundSupportHeight(to):to.y>GroundSupportHeight(to)))continue;
                if(!CanStand(to,radius))continue;
                int samples=Mathf.Max(24,Mathf.CeilToInt(d/.08f));bool clear=true;
                for(int i=0;i<=samples;i++)
                {float t=i/(float)samples;var p=JumpPosition(from,to,t);if(!ClearAtHeight(p,radius,p.y)){clear=false;break;}}
                if(clear){landing=to;return true;}
            }
            return false;
        }
        private static float StandingHeight(Vector3 point,float radius,float ceiling)
        {
            float height=GroundSupportHeight(point,ceiling+.035f);
            foreach(var o in obstacles)
            {
                if(o.Height<=0||WorldTerrain.Height(point)+o.Height>ceiling+.035f)continue;
                Vector2 d=new Vector2(point.x-o.Center.x,point.z-o.Center.y);
                float clearance=o.Radius>0?Mathf.Min(radius,.12f):radius;
                bool overlap;
                if(o.Radius>0)overlap=d.sqrMagnitude<(o.Radius+clearance)*(o.Radius+clearance);
                else {var nearest=new Vector2(Mathf.Max(0,Mathf.Abs(d.x)-o.Half.x),Mathf.Max(0,Mathf.Abs(d.y)-o.Half.y));overlap=nearest.sqrMagnitude<=radius*radius;}
                if(overlap)height=Mathf.Max(height,WorldTerrain.Height(point)+o.Height);
            }
            return height;
        }
        // Keep horizontal control while gravity settles the player onto the next lower surface.
        public static Vector3 MovePlayer(Vector3 from,Vector3 delta,float deltaTime,ref float fallSpeed,float radius=.45f)
        {
            if(!Finite(deltaTime)||deltaTime<=0)return from;
            if(from.y<=GroundSupportHeight(from)+.035f){fallSpeed=0;return Move(from,delta,radius);}
            delta=CombatFx.Flat(delta);
            int steps=Mathf.Max(1,Mathf.Max(Mathf.CeilToInt(delta.magnitude/.08f),Mathf.CeilToInt(deltaTime/.02f)));
            float dt=deltaTime/steps;
            for(int i=0;i<steps;i++)
            {
                Vector3 next=from+delta/steps;
                if(!ClearAtHeight(next,radius,from.y)||IsOpenWater(next,radius))next=from;
                float support=StandingHeight(next,radius,from.y);
                if(from.y<=support+.001f){next.y=support;fallSpeed=0;}
                else
                {
                    float oldSpeed=fallSpeed;
                    fallSpeed+=18f*dt;
                    next.y=Mathf.Max(support,from.y-(oldSpeed+fallSpeed)*.5f*dt);
                    if(next.y<=support+.001f)fallSpeed=0;
                }
                from=next;
            }
            return from;
        }
        private static Vector3 MoveOnPlatform(Vector3 from,Vector3 delta,float radius)
        {
            float speed=0;
            return MovePlayer(from,delta,1f/60f,ref speed,radius);
        }
        public static bool HeightAttackAllowed(Vector3 from,Vector3 to,bool ranged)
        {return Mathf.Abs(from.y-to.y)<(ranged?2.5f:.65f);}

        private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }

        // Convex arena + obstacle-free bounding rectangle proves every sample clear.
        // Keep the original sampled policy whenever anything could intersect.
        private static bool OpenSightBounds(Vector3 from,Vector3 to,float radius)
        {
            if(!Finite(radius)||radius<0||radius>=arena)return false;
            double limit=arena-radius-.001;
            if(!VisualInsideArena(from,limit)||!VisualInsideArena(to,limit))return false;
            double x0=System.Math.Min(from.x,to.x)-radius-.001,x1=System.Math.Max(from.x,to.x)+radius+.001;
            double z0=System.Math.Min(from.z,to.z)-radius-.001,z1=System.Math.Max(from.z,to.z)+radius+.001;
            CollectSolidCandidatesBounds(x0,x1,z0,z1);
            foreach(int index in solidCandidates)
            {
                var o=obstacles[index];
                double ex=o.Radius>0?o.Radius:o.Half.x,ez=o.Radius>0?o.Radius:o.Half.y;
                if(x1>=o.Center.x-ex&&x0<=o.Center.x+ex&&z1>=o.Center.y-ez&&z0<=o.Center.y+ez)return false;
            }
            return true;
        }
        private static bool ClearSegment(Vector3 from, Vector3 to, float radius, bool ignoreWater, ObstacleHandle ignored=null)
        {
            if(ignoreWater&&OpenSightBounds(from,to,radius))return true;
            int samples = Mathf.Max(1, Mathf.CeilToInt(CombatFx.Flat(to - from).magnitude / .18f));
            for (int i = 0; i <= samples; i++)
            {
                Vector3 p = Vector3.Lerp(from, to, i / (float)samples);
                if(ignoreWater&&(from.y>.05f||to.y>.05f)){if(!ClearAtHeight(p,radius,p.y+.9f,ignored))return false;}
                else if (ignoreWater ? !ClearOfSolids(p, radius, ignored) : !IsWalkable(p, radius)) return false;
            }
            return true;
        }
        public static Vector3 Move(Vector3 from, Vector3 delta, float radius = .45f)
        {
            if(from.y>GroundSupportHeight(from)+.05f&&CanStand(from,radius))return MoveOnPlatform(from,delta,radius);
            from = CombatFx.Flat(from); delta = CombatFx.Flat(delta);
            if (!IsWalkable(from, radius)) from = NearestWalkable(from, radius);
            int steps = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / .16f));
            Vector3 step = delta / steps;
            for (int i = 0; i < steps; i++)
            {
                Vector3 next = from + step;
                if (IsWalkable(next, radius)) { from = next; continue; }
                Vector3 alongX = from + new Vector3(step.x, 0, 0);
                if (IsWalkable(alongX, radius)) from = alongX;
                Vector3 alongZ = from + new Vector3(0, 0, step.z);
                if (IsWalkable(alongZ, radius)) from = alongZ;
            }
            from.y=GroundSupportHeight(from);
            return from;
        }
        // Search outward in distance order; never fall back to an obstructed origin.
        public static bool TryReturnPortalPosition(Vector3 from,out Vector3 position,float clearance=2.65f)
        {
            position=Vector3.zero;
            from=NearestWalkable(from,.45f);
            if(IsWalkable(position,clearance)&&CanReach(from,position,.45f))return true;
            for(float distance=.25f;distance<=arena-clearance;distance+=.25f)
            {
                int samples=Mathf.Max(32,Mathf.CeilToInt(distance*2*Mathf.PI/.5f));
                for(int i=0;i<samples;i++)
                {
                    float angle=i*2*Mathf.PI/samples;
                    Vector3 candidate=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*distance;
                    if(!IsWalkable(candidate,clearance)||!CanReach(from,candidate,.45f))continue;
                    position=candidate;return true;
                }
            }
            return false;
        }

        public static Vector3 NearestWalkable(Vector3 point, float radius = .45f)
        {
            point = Vector3.ClampMagnitude(CombatFx.Flat(point), arena - radius - .02f);
            if (IsWalkable(point, radius)) {point.y=GroundSupportHeight(point);return point;}
            for (float distance = .2f; distance <= arena * 2; distance += .25f)
                for (int i = 0; i < 32; i++)
                {
                    float angle = i * Mathf.PI / 16;
                    Vector3 probe = point + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * distance;
                    if (IsWalkable(probe, radius)) {probe.y=GroundSupportHeight(probe);return probe;}
                }
            return Vector3.zero;
        }

        public sealed class Route
        {
            private List<Vector3> waypoints;
            private int next, builtRevision = -1;
            private Vector3 goal;
            private float refresh,builtRadius;
            private void KeepPath(List<Vector3> path,Vector3 destination,float radius)
            {waypoints=path;next=0;goal=destination;builtRevision=revision;builtRadius=radius;refresh=Time.time+.85f;}
            // Wander commits the path it just certified, rather than discarding
            // it and repeating the same A* search in Direction this frame.
            public bool TryPlan(Vector3 from,Vector3 destination,float radius=.45f)
            {
                destination=NearestWalkable(destination,radius);
                var path=FindPath(from,destination,radius);if(path.Count==0)return false;
                KeepPath(path,destination,radius);return true;
            }
            public Vector3 Direction(Vector3 from, Vector3 destination, float radius = .45f)
            {
                destination = NearestWalkable(destination, radius);
                if (ClearSegment(from, destination, radius, false)) return CombatFx.Flat(destination - from).normalized;
                float goalMovement=(destination-goal).sqrMagnitude;
                // A static, still-reachable route does not become obsolete with
                // time. Moving goals and failed routes retain the original retry
                // interval; topology and exact actor radius invalidate at once.
                if (waypoints == null || builtRevision != revision || builtRadius!=radius || goalMovement>6.25f ||
                    Time.time>=refresh&&(waypoints.Count==0||goalMovement>.0001f))
                {
                    KeepPath(FindPath(from,destination,radius),destination,radius);
                }
                while (next < waypoints.Count && CombatFx.Flat(waypoints[next] - from).sqrMagnitude < .22f)
                {
                    if (next + 1 >= waypoints.Count) { if (CombatFx.Flat(waypoints[next] - from).sqrMagnitude < .01f) next++; break; }
                    if (!ClearSegment(from, waypoints[next + 1], radius, false)) break;
                    next++;
                }
                if (next >= waypoints.Count) return Vector3.zero;
                if (!ClearSegment(from, waypoints[next], radius, false))
                {
                    KeepPath(FindPath(from,destination,radius),destination,radius);
                    if (waypoints.Count == 0) return Vector3.zero;
                }
                if (next + 1 < waypoints.Count && ClearSegment(from, waypoints[next + 1], radius, false)) next++;
                return CombatFx.Flat(waypoints[next] - from).normalized;
            }
        }
        public static bool CanReach(Vector3 from,Vector3 target,float radius=.65f)
        { return IsWalkable(from,radius) && IsWalkable(target,radius) && FindPath(from,target,radius).Count>0; }
        private static Vector3 Point(int index) { return new Vector3(GridOrigin + index % Side * Cell, 0, GridOrigin + index / Side * Cell); }
        private static int NearestNode(Vector3 point, bool[] grid, float radius)
        {
            int cx = Mathf.Clamp(Mathf.RoundToInt((point.x - GridOrigin) / Cell), 0, Side - 1);
            int cy = Mathf.Clamp(Mathf.RoundToInt((point.z - GridOrigin) / Cell), 0, Side - 1);
            int best = -1; float distance = float.MaxValue;
            for (int y = Mathf.Max(0, cy - 2); y <= Mathf.Min(Side - 1, cy + 2); y++)
                for (int x = Mathf.Max(0, cx - 2); x <= Mathf.Min(Side - 1, cx + 2); x++)
                {
                    int node = y * Side + x;
                    float d = (Point(node) - point).sqrMagnitude;
                    if (grid[node] && d < distance && ClearSegment(point, Point(node), radius, false)) { best = node; distance = d; }
                }
            return best;
        }
        public static List<Vector3> FindPath(Vector3 from, Vector3 target, float radius = .45f)
        {
            from = NearestWalkable(from, radius); target = NearestWalkable(target, radius);
            var path = new List<Vector3>();
            if (ClearSegment(from, target, radius, false)) { path.Add(target); return path; }
            int key = Mathf.CeilToInt(radius * 20);
            float gridRadius = key / 20f;
            bool[] grid;
            if (!grids.TryGetValue(key, out grid))
            {
                grid = new bool[Side * Side];
                for (int i = 0; i < grid.Length; i++) grid[i] = IsWalkable(Point(i), gridRadius);
                grids[key] = grid;
            }
            int start = NearestNode(from, grid, radius), end = NearestNode(target, grid, radius);
            if (start < 0 || end < 0) return path;
            float[] cost = new float[grid.Length]; int[] parent = new int[grid.Length]; bool[] closed = new bool[grid.Length];
            for (int i = 0; i < cost.Length; i++) { cost[i] = float.MaxValue; parent[i] = -1; }
            var queue = new Heap(); cost[start] = 0; queue.Add(start, 0);
            while (queue.Count > 0)
            {
                int node = queue.Pop(); if (closed[node]) continue; closed[node] = true;
                if (node == end)
                {
                    for (int n = end; n >= 0; n = parent[n]) { path.Add(Point(n)); if (n == start) break; }
                    path.Reverse(); path.Add(target); return path;
                }
                int x = node % Side, y = node / Side;
                for (int i = 0; i < 8; i++)
                {
                    int nx = x + NeighborX[i], ny = y + NeighborY[i];
                    if (nx < 0 || ny < 0 || nx >= Side || ny >= Side) continue;
                    int candidate = ny * Side + nx;
                    if (!grid[candidate] || closed[candidate]) continue;
                    bool diagonal = NeighborX[i] != 0 && NeighborY[i] != 0;
                    if (diagonal && (!grid[y * Side + nx] || !grid[ny * Side + x])) continue;
                    float nextCost = cost[node] + (diagonal ? 1.414214f : 1f);
                    if (nextCost >= cost[candidate]) continue;
                    if (!ClearGridEdge(node,candidate,i,radius)) continue;
                    cost[candidate] = nextCost; parent[candidate] = node;
                    queue.Add(candidate, nextCost + Vector3.Distance(Point(candidate), Point(end)) / Cell);
                }
            }
            return path;
        }
        private sealed class Heap
        {
            private struct Entry { public int Node; public float Score; }
            private readonly List<Entry> items = new List<Entry>();
            public int Count { get { return items.Count; } }
            public void Add(int node, float score)
            {
                var value = new Entry { Node = node, Score = score }; items.Add(value); int i = items.Count - 1;
                while (i > 0) { int p = (i - 1) / 2; if (items[p].Score <= score) break; items[i] = items[p]; i = p; }
                items[i] = value;
            }
            public int Pop()
            {
                int result = items[0].Node; Entry tail = items[items.Count - 1]; items.RemoveAt(items.Count - 1);
                if (items.Count == 0) return result;
                int i = 0;
                while (i * 2 + 1 < items.Count)
                {
                    int child = i * 2 + 1;
                    if (child + 1 < items.Count && items[child + 1].Score < items[child].Score) child++;
                    if (items[child].Score >= tail.Score) break;
                    items[i] = items[child]; i = child;
                }
                items[i] = tail; return result;
            }
        }
    }
}

namespace Emberfall
{
    // Smooth compact hills. The brook, crossing, camp, travel road and courtyard
    // keep their authored datum. Rendering and traversal sample this same field.
    public static class WorldTerrain
    {
        public static bool ReliefEnabled=true;
        public static bool Enabled { get; private set; }
        public static bool Dungeon { get; private set; }
        public static float Radius { get; private set; } = 18;
        public static void Configure(ZoneKind zone,int hub,float radius=18)
        {
            Enabled=ReliefEnabled&&(zone==ZoneKind.Dungeon||hub==0);
            Dungeon=zone==ZoneKind.Dungeon;Radius=radius;
        }
        static float Hill(float x,float z,float cx,float cz,float radius,float height)
        {
            float q=Mathf.Clamp01(1-((x-cx)*(x-cx)+(z-cz)*(z-cz))/(radius*radius));
            return height*q*q*q;
        }
        public static float Height(Vector3 p)
        {
            if(!Enabled)return 0;
            if(Dungeon)
            {
                float side=Mathf.Clamp01((Mathf.Abs(p.x)-3)/3);side=side*side*(3-2*side);
                float crossing=Mathf.Clamp01((Mathf.Abs(p.z)-3.3f)/2);crossing=crossing*crossing*(3-2*crossing);
                return side*crossing*(Hill(p.x,p.z,-Radius*.52f,-Radius*.38f,Radius*.42f,2.4f)+
                    Hill(p.x,p.z,Radius*.48f,Radius*.42f,Radius*.40f,2.1f)+
                    Hill(p.x,p.z,-Radius*.46f,Radius*.50f,Radius*.34f,1.4f)+
                    Hill(p.x,p.z,Radius*.55f,-Radius*.40f,Radius*.35f,1.3f));
            }
            return Hill(p.x,p.z,-13,-11,7,2f)+Hill(p.x,p.z,16,1,5.5f,.85f)+Hill(p.x,p.z,-17,13,5,1.4f)+Hill(p.x,p.z,-20,-2,5,1.1f)+
                Hill(p.x,p.z,-24,-18,7.5f,3.1f)+Hill(p.x,p.z,23,13,8,2.6f)+
                Hill(p.x,p.z,-9,25,7,2.8f)+Hill(p.x,p.z,11,26,6.5f,1.8f)+Hill(p.x,p.z,27,-15,5,1.9f);
        }
        public static Vector3 Ground(Vector3 p,float clearance=0)
        {p.y=WorldTraversal.SurfaceHeight(p,.04f)+clearance;return p;}
    }
}
