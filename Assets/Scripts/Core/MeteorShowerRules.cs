using System;
namespace Emberfall
{
    public static class MeteorShowerRules
    {
        public const float Interval=.45f,FallTime=.7f;
        public static int Count(int rank)=>14+(Math.Max(1,Math.Min(3,rank))-1)*4;
        public static float Duration(int rank)=>(Count(rank)-1)*Interval+FallTime;
        public static float Front(int rank)=>18*GameBalance.SkillRangeMultiplier(rank);
        public static float Rear(int rank)=>4*GameBalance.SkillRangeMultiplier(rank);
        public static float HalfWidth(int rank,bool rear)=> (rear?4:7)*GameBalance.SkillRangeMultiplier(rank);
        public static float Radius(int rank,bool rear)=>(rear?1.7f:2.6f)*GameBalance.SkillRangeMultiplier(rank);
        public static bool Contains(float forward,float side,int rank)=>forward>=-Rear(rank)&&forward<=Front(rank)&&Math.Abs(side)<=HalfWidth(rank,forward<0);
        public static float Coefficient(int rank)=>.24f*CombatBalance.RankPower(rank)*(2.2f*(4+rank)+9f)/Count(rank);
    }
}
