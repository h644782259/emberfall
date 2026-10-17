using System;
using UnityEditor;
using UnityEngine;
namespace Emberfall.Editor
{
    public static class ActorReliefStorageValidation
    {
        public static void Run()
        {
            const string path="Assets/Resources/WorldArt/ActorReliefAtlas.png";
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            bool readable=importer.isReadable;
            var previous=importer.GetPlatformTextureSettings("iPhone");
            bool success=false;
            try
            {
                var originalFormat=importer.GetPlatformTextureSettings("iPhone");
                originalFormat.overridden=false;
                importer.SetPlatformTextureSettings(originalFormat);
                importer.isReadable=true;importer.SaveAndReimport();
                var baseline=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var pixels=new Color32[baseline.mipmapCount][];
                for(int mip=0;mip<pixels.Length;mip++)pixels[mip]=baseline.GetPixels32(mip);
                Debug.Log($"RELIEF STORAGE BEFORE {baseline.width}x{baseline.height} {baseline.format} rawBytes={baseline.GetRawTextureData<byte>().Length}");
                var settings=importer.GetPlatformTextureSettings("iPhone");
                settings.overridden=true;settings.format=TextureImporterFormat.RG16;
                importer.SetPlatformTextureSettings(settings);importer.SaveAndReimport();
                var candidate=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if(candidate.format!=TextureFormat.RG16||candidate.mipmapCount!=pixels.Length)throw new Exception("Unexpected relief format or mip count");
                long checkedPixels=0;
                for(int mip=0;mip<pixels.Length;mip++)
                {
                    var actual=candidate.GetPixels32(mip);
                    if(actual.Length!=pixels[mip].Length)throw new Exception("Mip dimensions changed");
                    for(int i=0;i<actual.Length;i++)
                        if(actual[i].r!=pixels[mip][i].r||actual[i].g!=pixels[mip][i].g)throw new Exception($"Relief channels changed at mip {mip}, pixel {i}");
                    checkedPixels+=actual.Length;
                }
                Debug.Log($"RELIEF STORAGE PASS {candidate.format} rawBytes={candidate.GetRawTextureData<byte>().Length} identicalRG={checkedPixels}");
                success=true;
            }
            finally
            {
                if(!success)importer.SetPlatformTextureSettings(previous);
                importer.isReadable=readable;importer.SaveAndReimport();
            }
        }
    }
}
