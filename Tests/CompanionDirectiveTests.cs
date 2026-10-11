using System;
using Emberfall;
public static class CompanionDirectiveTests
{
    public static string Run()
    {
        int checks=0; var directive=new CompanionDirective<object>(); var a=new object();var b=new object();
        directive.Focus(b);
        for(int frame=0;frame<600;frame++) {if(directive.Resolve(x=>true)!=b)throw new Exception("six seconds cannot expire explicit B or select auto A");checks++;}
        directive.Recall();if(directive.Resolve(x=>true)!=null||!directive.Recalling)throw new Exception("recall clears focus");checks++;
        directive.Focus(a);if(directive.Recalling||directive.Resolve(x=>true)!=a)throw new Exception("new command replaces recall");checks++;
        if(directive.Resolve(x=>false)!=null)throw new Exception("dead/out-of-range removed");checks++;
        directive.Focus(b);directive.Clear();if(directive.Resolve(x=>true)!=null||directive.Recalling)throw new Exception("epoch ends orders");checks++;
        return checks+" companion directive checks passed";
    }
}
