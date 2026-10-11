using System;
using Emberfall;
public static class CombatTextLayoutTests
{
    static int count;
    static void Check(bool result,string message){count++;if(!result)throw new Exception(message);}
    public static string Run()
    {
        count=0;
        foreach(float density in new[]{.5f,1f,2f,3f,4f})foreach(float scale in new[]{1f,1.25f,1.8f})foreach(bool critical in new[]{false,true})
        {
            float height=CombatTextLayout.PixelHeight(scale,density,critical);
            Check(height>=21*scale*density*(critical?1.28f:1),"Bounds include critical peak pop");
            foreach(float aspect in new[]{.4f,1f,3f,8f,18f})
            {
                var boxes=new CombatTextLayout.Box[CombatTextLayout.CandidateCount];
                for(int slot=0;slot<boxes.Length;slot++)
                {
                    boxes[slot]=CombatTextLayout.Candidate(4096,100,height*aspect,height,slot);
                    Check(boxes[slot].Height>height&&boxes[slot].Width>height*aspect,"Glyph extent plus outline/padding");
                    for(int prior=0;prior<slot;prior++)Check(!boxes[slot].Overlaps(boxes[prior]),"No overlap at large text/density/pop");
                }
            }
        }
        Check(CombatTextLayout.PixelHeight(1.8f,1,true)>55,"Maximum crit bigger than legacy 34px lane");
        Check(!CombatTextLayout.Fits(new CombatTextLayout.Box(-1,2,40,40),568,320),"Reject clipping on left");
        Check(!CombatTextLayout.Fits(new CombatTextLayout.Box(550,300,40,40),568,320),"Reject clipping on bottom/right");
        Check(CombatTextLayout.Fits(new CombatTextLayout.Box(20,20,100,80),568,320),"Compact valid bounds");
        Check(CombatTextLayout.MechanismLimit==4&&CombatTextLayout.CandidateCount==12,"Finite reserve and search budget");
        return count+" measured combat text geometry assertions passed";
    }
}
