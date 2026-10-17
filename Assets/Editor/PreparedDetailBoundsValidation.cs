using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Emberfall.Editor
{
    public static class PreparedDetailBoundsValidation
    {
        public static void Run()
        {
            var type = typeof(GameSession).Assembly.GetType("Emberfall.PreparedDetailBounds");
            var calculate = type.GetMethod("TryCalculate", BindingFlags.Static | BindingFlags.NonPublic);
            var root = new GameObject("Detail bounds fixture");
            var child = new GameObject("Rotated immutable detail"); child.transform.SetParent(root.transform, false);
            var material = new Material(Resources.Load<Shader>("WorldArt/WeatheredWorld"));
            var vertices = new List<Vector3>(); var colors = new List<Color>();
            for (int z = 0; z < 9; z++) for (int x = 0; x < 9; x++)
            { vertices.Add(new Vector3(x * 2 - 8, Mathf.Sin(x + z) * .3f, z * 2 - 8)); colors.Add(new Color(1, 1, 1, ((x + z) % 5) * .5f - .2f)); }
            bool enabled = WorldTerrain.Enabled, dungeon = WorldTerrain.Dungeon, relief = WorldTerrain.ReliefEnabled;
            float radius = WorldTerrain.Radius;
            int checks = 0;
            try
            {
                WorldTerrain.ReliefEnabled = true;
                foreach (var zone in new[] { ZoneKind.Wilderness, ZoneKind.Dungeon })
                foreach (float scale in new[] { .4f, 1f, 2.7f })
                foreach (float follow in new[] { -1f, 0f, 1f })
                {
                    WorldTerrain.Configure(zone, 0, 54);
                    root.transform.position = new Vector3(-13, 2, -11);
                    root.transform.rotation = Quaternion.Euler(23, 61, -17);
                    root.transform.localScale = new Vector3(scale, .8f, 1.3f);
                    child.transform.localRotation = Quaternion.Euler(12, -33, 8);
                    material.SetFloat("_TerrainFollow", follow); material.SetFloat("_Wind", 1.8f); material.SetFloat("_VertexTint", 2.5f);
                    object[] args = { vertices, colors, child.transform, new[] { material }, default(Bounds) };
                    if (!(bool)calculate.Invoke(null, args)) throw new Exception("Could not calculate known shader bounds");
                    var bounds = (Bounds)args[4];
                    for (int step = 0; step < 80; step++)
                    for (int i = 0; i < vertices.Count; i++)
                    {
                        float time = step * .371f;
                        var p = child.transform.TransformPoint(vertices[i]);
                        p.y += WorldTerrain.Height(p) * follow;
                        float sway = 1.8f * (1 + (colors[i].a - 1) * 2.5f);
                        p.x += sway * Mathf.Sin(time * 1.3f + p.z * .8f + p.x * .5f) * .035f;
                        p.z += sway * Mathf.Cos(time * .9f + p.x * .7f) * .02f;
                        if (!bounds.Contains(child.transform.InverseTransformPoint(p))) throw new Exception("Shader-deformed vertex escaped culling bounds");
                        checks++;
                    }
                }
                Debug.Log("PREPARED DETAIL BOUNDS PASS: " + checks + " terrain/wind positions contained; tilted nonuniform transforms, signed terrain follow and vertex tint");
            }
            finally
            {
                WorldTerrain.ReliefEnabled = enabled;
                WorldTerrain.Configure(dungeon ? ZoneKind.Dungeon : ZoneKind.Wilderness, 0, radius);
                WorldTerrain.ReliefEnabled = relief;
                UnityEngine.Object.DestroyImmediate(material); UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
