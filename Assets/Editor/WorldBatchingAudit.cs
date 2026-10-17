using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Emberfall.Editor
{
    // Read-only scene audit; never changes runtime materials or batching policy.
    public static class WorldBatchingAudit
    {
        public static void AuditGroundTexture()
        {
            foreach(string name in new[]{"GroundMeadow","LivingLeafSurface","NaturalSurfaceAtlas"})
            {
                string path="Assets/Resources/WorldArt/"+name+".png";
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(texture==null||importer==null)throw new Exception("Missing texture: "+path);
                var platform=importer.GetPlatformTextureSettings("iPhone");
                var automaticFormat=importer.GetAutomaticFormat("iPhone");
                Debug.Log($"GROUND TEXTURE {name}: imported={texture.width}x{texture.height} format={texture.format} mips={texture.mipmapCount} aniso={texture.anisoLevel} filter={texture.filterMode} readable={texture.isReadable} iPhoneOverride={platform.overridden} automaticFormat={automaticFormat}");
            }
        }
        public static void Run()
        {
            var shader = Resources.Load<Shader>("WorldArt/WeatheredWorld");
            var method = typeof(ShaderUtil).GetMethod("HasInstancing", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                null, new[] { typeof(Shader) }, null);
            Debug.Log("WORLD BATCHING shader instancing=" + (method == null ? "API unavailable" : method.Invoke(null, new object[] { shader }).ToString()));
            foreach (var zone in new[] { ZoneKind.Wilderness, ZoneKind.Dungeon })
            {
                var world = WorldBuilder.Build(zone);
                try
                {
                    var groups = new Dictionary<(Mesh, Material), int>();
                    int renderers = 0, disabledInstancing = 0, propertyBlocks = 0;
                    foreach (var renderer in world.GetComponentsInChildren<MeshRenderer>())
                    {
                        if (!renderer.enabled) continue;
                        var filter = renderer.GetComponent<MeshFilter>();
                        if (filter == null || filter.sharedMesh == null) continue;
                        renderers++;
                        if (renderer.HasPropertyBlock()) propertyBlocks++;
                        foreach (var material in renderer.sharedMaterials)
                        {
                            if (material == null) continue;
                            if (!material.enableInstancing) disabledInstancing++;
                            var key = (filter.sharedMesh, material);
                            groups.TryGetValue(key, out int count); groups[key] = count + 1;
                        }
                    }
                    int repeated = 0, submissions = 0;
                    foreach (var pair in groups) if (pair.Value > 1)
                    {
                        repeated++; submissions += pair.Value;
                        Debug.Log($"WORLD GROUP {zone}: mesh={pair.Key.Item1.name} shader={pair.Key.Item2.shader.name} count={pair.Value} instancing={pair.Key.Item2.enableInstancing}");
                    }
                    Debug.Log($"WORLD BATCHING {zone}: renderers={renderers} groups={groups.Count} repeatedGroups={repeated} repeatedSubmissions={submissions} instancingDisabled={disabledInstancing} propertyBlocks={propertyBlocks}");
                }
                finally { UnityEngine.Object.DestroyImmediate(world); }
            }
        }
    }
}
