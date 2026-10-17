using System;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Emberfall.Editor
{
    public static class ForwardShaderBuildValidation
    {
        public static void Run()
        {
            var group=BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            var tiers=new[]{GraphicsTier.Tier1,GraphicsTier.Tier2,GraphicsTier.Tier3};
            var saved=new TierSettings[3];
            var policy=new ForwardShaderBuildPolicy();
            var scene=EditorSceneManager.NewPreviewScene();
            try
            {
                for(int i=0;i<3;i++)saved[i]=EditorGraphicsSettings.GetTierSettings(group,tiers[i]);
                for(int i=0;i<3;i++){var value=saved[i];value.renderingPath=RenderingPath.Forward;EditorGraphicsSettings.SetTierSettings(group,tiers[i],value);}
                policy.OnPreprocessBuild(null);
                foreach(string shader in new[]{"Emberfall/Weathered World","Emberfall/Actor Surface"})
                {
                    Check(ForwardShaderBuildPolicy.ShouldStrip(shader,PassType.Deferred),"unused Deferred removed");
                    foreach(var pass in new[]{PassType.ForwardBase,PassType.ForwardAdd,PassType.ShadowCaster,PassType.Meta})
                        Check(!ForwardShaderBuildPolicy.ShouldStrip(shader,pass),"production pass retained: "+pass);
                }
                Check(!ForwardShaderBuildPolicy.ShouldStrip("Standard",PassType.Deferred),"unrelated shader retained");
                for(int i=0;i<3;i++)
                {
                    var value=saved[i];value.renderingPath=RenderingPath.DeferredShading;EditorGraphicsSettings.SetTierSettings(group,tiers[i],value);
                    policy.OnPreprocessBuild(null);
                    Check(!ForwardShaderBuildPolicy.ShouldStrip("Emberfall/Weathered World",PassType.Deferred),"tier override retained "+tiers[i]);
                    value.renderingPath=RenderingPath.Forward;EditorGraphicsSettings.SetTierSettings(group,tiers[i],value);
                }
                policy.OnPreprocessBuild(null);
                var go=new GameObject("Explicit deferred camera");SceneManager.MoveGameObjectToScene(go,scene);
                go.AddComponent<Camera>().renderingPath=RenderingPath.DeferredShading;go.SetActive(false);
                policy.OnProcessScene(scene,null);
                Check(!ForwardShaderBuildPolicy.ShouldStrip("Emberfall/Actor Surface",PassType.Deferred),"inactive camera override retained");
                Debug.Log("FORWARD BUILD VALIDATION PASS: 15 checks; forward, additive lights, shadows, meta and deferred overrides preserved");
            }
            finally
            {
                for(int i=0;i<3;i++)EditorGraphicsSettings.SetTierSettings(group,tiers[i],saved[i]);
                EditorSceneManager.ClosePreviewScene(scene);policy.OnPreprocessBuild(null);
            }
        }
        static void Check(bool condition,string label){if(!condition)throw new Exception(label);}
    }
}
