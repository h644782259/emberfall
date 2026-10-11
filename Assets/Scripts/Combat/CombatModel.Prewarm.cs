using System;
using System.Collections;
using UnityEngine;

namespace Emberfall
{
    public sealed partial class CombatModel
    {
        private static bool modelCachesReady;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetModelPreparation(){modelCachesReady=false;}

        // Populate the same shared caches the real factories read. No preview,
        // temporary player, equipment or logical actor is constructed here.
        public static IEnumerator PrewarmModels(Action<float> progress = null)
        {
            if(modelCachesReady){progress?.Invoke(1);yield break;}
            string[] shapes={"face","shoulder","bracer","boot","sole","plate","knee","panel","strap","hat","skirt","helmet","hair-lock","nose","neck","gorget"};
            int total=4+2+shapes.Length+4+AuthoredActorMeshes.PrewarmKeys.Length+ActorSilhouetteF1.PrewarmKeys.Length+EnemySilhouetteArt.PrewarmKeys.Length+1, completed=0;
            foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass)))
            {HeroNaturalMeshLibrary.Torso(hero);progress?.Invoke(++completed/(float)total);yield return null;}
            foreach(bool guardian in new[]{false,true})
            {HeroNaturalMeshLibrary.EnemyTorso(guardian);progress?.Invoke(++completed/(float)total);yield return null;}
            foreach(string shape in shapes)
            {HeroNaturalMeshLibrary.Get(shape);progress?.Invoke(++completed/(float)total);yield return null;}
            HeroNaturalMeshLibrary.Limb(false);progress?.Invoke(++completed/(float)total);yield return null;
            HeroNaturalMeshLibrary.Limb(true);progress?.Invoke(++completed/(float)total);yield return null;
            HeroNaturalMeshLibrary.Hand();progress?.Invoke(++completed/(float)total);yield return null;
            HeroNaturalMeshLibrary.Hair();progress?.Invoke(++completed/(float)total);yield return null;
            foreach(string key in AuthoredActorMeshes.PrewarmKeys)
            {AuthoredActorMeshes.Prewarm(key);progress?.Invoke(++completed/(float)total);yield return null;}
            foreach(string key in ActorSilhouetteF1.PrewarmKeys)
            {ActorSilhouetteF1.Prewarm(key);progress?.Invoke(++completed/(float)total);yield return null;}
            foreach(string key in EnemySilhouetteArt.PrewarmKeys)
            {EnemySilhouetteArt.Prewarm(key);progress?.Invoke(++completed/(float)total);yield return null;}
            VanguardActionLibrary.Load();progress?.Invoke(++completed/(float)total);yield return null;
            modelCachesReady=true;progress?.Invoke(1);
        }
    }
}
