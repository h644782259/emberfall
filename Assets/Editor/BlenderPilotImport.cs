using System;
using UnityEditor;
using UnityEngine;
namespace Emberfall.EditorTools
{
    // Bounded import policy: no changes to unrelated FBX, texture, or project assets.
    public sealed class BlenderPilotImport : AssetPostprocessor
    {
        private const string Root = "Assets/Resources/BlenderPilot/";
        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Root,StringComparison.Ordinal)) return;
            var importer=(ModelImporter)assetImporter;
            importer.globalScale=1;
            importer.useFileScale=true;
            importer.addCollider=false;
            importer.isReadable=false;
            importer.importNormals=ModelImporterNormals.Import;
            importer.importTangents=ModelImporterTangents.CalculateMikk;
            importer.optimizeGameObjects=false; // named sockets must remain available
            importer.importAnimation=assetPath.EndsWith("Vanguard.fbx",StringComparison.Ordinal);
            importer.animationType=ModelImporterAnimationType.Legacy;
            importer.animationWrapMode=WrapMode.ClampForever;
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
        }
        private Material OnAssignMaterialModel(Material material,Renderer renderer)
        {
            if (!assetPath.StartsWith(Root,StringComparison.Ordinal)) return null;
            return AssetDatabase.LoadAssetAtPath<Material>(Root+"Pilot_Atlas_Standard.mat");
        }
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root,StringComparison.Ordinal)) return;
            var importer=(TextureImporter)assetImporter;
            importer.textureType=TextureImporterType.Default;
            importer.sRGBTexture=!assetPath.Contains("MetallicSmoothness");
            importer.alphaSource=TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency=false;
            importer.mipmapEnabled=true;
            importer.maxTextureSize=1024;
            importer.textureCompression=TextureImporterCompression.Compressed;
            importer.isReadable=false;
        }
    }
    [InitializeOnLoad]
    public static class BlenderPilotMenu
    {
        private const string Key="Emberfall.BlenderPilot.Enabled";
        static BlenderPilotMenu() { ApplySessionChoice(); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplySessionChoice() { BlenderPilotArt.Enabled=SessionState.GetBool(Key,false); }
        [MenuItem("Emberfall/Art Pilot/Enable for Play (base outfit only)")]
        private static void Enable() { SessionState.SetBool(Key,true); BlenderPilotArt.Enabled=true; }
        [MenuItem("Emberfall/Art Pilot/Disable (procedural default)")]
        private static void Disable() { SessionState.SetBool(Key,false); BlenderPilotArt.Enabled=false; }
    }
}
