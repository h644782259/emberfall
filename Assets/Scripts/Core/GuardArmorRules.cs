namespace Emberfall
{
    // The visual state and damage path share the existing front-armor rule.
    public static class GuardArmorRules
    {
        public static bool Closed(bool guardian,bool boss,bool preparing){return guardian&&!boss&&!preparing;}
        public static float Multiplier(bool guardian,bool boss,bool preparing,float directionSquared,float frontalDot)
        {return Closed(guardian,boss,preparing)&&directionSquared>.01f&&frontalDot>.45f?.65f:1f;}
    }
}
