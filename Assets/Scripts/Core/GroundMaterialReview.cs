using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    internal static class GroundMaterialReview
    {
        struct Saved { internal Texture Main,Ground; internal bool Keyword; }
        static readonly Dictionary<Material,Saved> saved=new Dictionary<Material,Saved>();
        static int activeMaterials;
        internal static int ActiveMaterials => activeMaterials;
        internal static void Apply(int mode)
        {
            Restore();
            var original=Resources.Load<Texture2D>("WorldArt/GroundMeadow");
            var compact=mode==1?Resources.Load<Texture2D>("WorldArt/Review/GroundMeadow512"):null;
            if(original==null||(mode==1&&compact==null))return;
            foreach(var material in Resources.FindObjectsOfTypeAll<Material>())
            {
                if(material==null||material.shader==null||material.shader.name!="Emberfall/Weathered World")continue;
                bool main=material.mainTexture==original;
                bool ground=material.HasProperty("_GroundTex")&&material.GetTexture("_GroundTex")==original;
                if(!main&&(mode!=1||!ground))continue;
                saved.Add(material,new Saved{Main=material.mainTexture,Ground=material.GetTexture("_GroundTex"),Keyword=material.IsKeywordEnabled("EMBER_MEADOW_SIMPLE")});
                if(mode==1)
                {if(main){material.mainTexture=compact;material.DisableKeyword("EMBER_MEADOW_SIMPLE");}if(ground)material.SetTexture("_GroundTex",compact);}
                else if(mode==2)material.EnableKeyword("EMBER_MEADOW_SIMPLE");
                else material.DisableKeyword("EMBER_MEADOW_SIMPLE");
            }
            activeMaterials=mode==0?0:saved.Count;
        }
        internal static void Restore()
        {
            foreach(var pair in saved)if(pair.Key!=null)
            {
                pair.Key.mainTexture=pair.Value.Main;pair.Key.SetTexture("_GroundTex",pair.Value.Ground);
                if(pair.Value.Keyword)pair.Key.EnableKeyword("EMBER_MEADOW_SIMPLE");else pair.Key.DisableKeyword("EMBER_MEADOW_SIMPLE");
            }
            saved.Clear();activeMaterials=0;
        }
    }
}
