using System;
using Emberfall;

public static class SkillVfxEnvelopeTests
{
    public static string Run()
    {
        int checks=0;
        foreach(FilledVfxKind kind in Enum.GetValues(typeof(FilledVfxKind)))
        {
            float previous=1;
            for(int i=0;i<=1000;i++)
            {
                float t=.75f+.25f*i/1000;
                float opacity=FilledVfxRecipes.Sample(kind,t,1).Opacity;
                if(float.IsNaN(opacity)||opacity<0||opacity>previous+.000001f)
                    throw new Exception(kind+": fade must remain finite, bounded and monotonic");
                previous=opacity;checks++;
            }
            if(previous!=0)throw new Exception(kind+": final frame must be transparent");
            float edge=FilledVfxRecipes.Sample(kind,.9999f,1).Opacity;
            if(edge>.00001f)throw new Exception(kind+": tail still ends with a visible jump");
            checks+=2;
        }
        foreach(float fade in new[]{.1f,.35f,.8f,1.8f})
        {
            if(SkillVisualAftermathTiming.Coverage(0,fade)!=0||SkillVisualAftermathTiming.Coverage(fade,fade)!=1)
                throw new Exception("Aftermath endpoints changed");
            float step=fade*.0001f;
            if(SkillVisualAftermathTiming.Coverage(step,fade)>.00001f||1-SkillVisualAftermathTiming.Coverage(fade-step,fade)>.00001f)
                throw new Exception("Aftermath must join both rest states smoothly");
            checks+=2;
        }
        return "PASS: "+checks+" skill-family fade and continuity checks";
    }
}
