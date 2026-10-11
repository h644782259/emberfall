namespace Emberfall
{
    // Presentation only: accepted displacement and the existing action clock remain authoritative.
    public readonly struct HeroMotionStyle
    {
        public readonly float Step, SideStep, Stance, Crouch, Bob, Torso, Cloth, Observation;
        private HeroMotionStyle(float step,float side,float stance,float crouch,float bob,float torso,float cloth,float observation)
        {Step=step;SideStep=side;Stance=stance;Crouch=crouch;Bob=bob;Torso=torso;Cloth=cloth;Observation=observation;}
        public static HeroMotionStyle For(HeroClass hero)
        {
            switch(hero)
            {
                case HeroClass.Vanguard:return new HeroMotionStyle(25,20,5,.032f,.55f,.65f,.7f,0);
                case HeroClass.Ranger:return new HeroMotionStyle(32,30,3,.012f,.8f,.7f,.8f,0);
                case HeroClass.Arcanist:return new HeroMotionStyle(18,15,2,0,.3f,.3f,.5f,0);
                case HeroClass.Summoner:return new HeroMotionStyle(22,19,3,.008f,.45f,.45f,.6f,4);
                default:return new HeroMotionStyle(30,24,2,0,1,1,1,0);
            }
        }
    }
}
