using System;
using UnityEngine;

namespace Emberfall
{
    internal sealed class VisualMeshData
    {
        public Vector3[] Vertices, Normals;
        public Vector2[] Uv;
        public int[] Triangles;
        public VisualMeshData(int vertices, int triangles)
        {
            Vertices = new Vector3[vertices]; Normals = new Vector3[vertices];
            Uv = new Vector2[vertices]; Triangles = new int[triangles * 3];
        }
    }

    // Managed-only recipes are separately testable without starting an Editor.
    internal static class VisualMeshRecipes
    {
        public static Vector3 DrapePoint(float across, float length, float time, float motion, float phase)
        {
            across = Mathf.Clamp(across,-1f,1f); length = Mathf.Clamp(length,0f,1f); motion = Mathf.Clamp(motion,0f,1f);
            float fold = Mathf.Cos(across*Mathf.PI*3f)*.024f;
            float sway = Mathf.Sin(time*3.1f-length*3.5f+across*.8f+phase)*(.025f+.07f*motion);
            return new Vector3(across*(.31f+.18f*length)+sway*length*.3f,
                -length*1.21f+Math.Abs(across)*length*.09f,
                -.035f-length*length*(.22f+motion*.24f)+(fold+sway)*length);
        }

        public static VisualMeshData BevelBox()
        {
            const int count = 6;
            float[] cuts = { -.5f, -.46f, -.4f, .4f, .46f, .5f };
            VisualMeshData data = new VisualMeshData(6 * count * count, 6 * (count - 1) * (count - 1) * 2);
            Vector3[] normals = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            int index = 0;
            for (int face = 0; face < 6; face++)
            {
                Vector3 normal = normals[face];
                Vector3 axisU = face < 2 ? Vector3.forward : Vector3.right;
                Vector3 axisV = Vector3.Cross(normal, axisU);
                for (int y = 0; y < count; y++) for (int x = 0; x < count; x++)
                {
                    Vector3 p = normal * .5f + axisU * cuts[x] + axisV * cuts[y];
                    Vector3 inner = new Vector3(Mathf.Clamp(p.x, -.4f, .4f), Mathf.Clamp(p.y, -.4f, .4f), Mathf.Clamp(p.z, -.4f, .4f));
                    int vertex = face * count * count + y * count + x;
                    Vector3 roundedNormal = (p - inner).normalized;
                    data.Vertices[vertex] = inner + roundedNormal * .1f;
                    data.Normals[vertex] = roundedNormal;
                    data.Uv[vertex] = new Vector2(x / (float)(count - 1), y / (float)(count - 1));
                    if (x == count - 1 || y == count - 1) continue;
                    data.Triangles[index++] = vertex; data.Triangles[index++] = vertex + 1; data.Triangles[index++] = vertex + count;
                    data.Triangles[index++] = vertex + 1; data.Triangles[index++] = vertex + count + 1; data.Triangles[index++] = vertex + count;
                }
            }
            return data;
        }

        public static VisualMeshData RoundBody(bool capsule, int segments, int rings)
        {
            segments = Math.Max(8, Math.Min(32, segments)); rings = Math.Max(6, Math.Min(24, rings));
            rings = ((rings + 1) / 2) * 2;
            // Separate hemispheres connect through two equators; no stretched triangles at the waist.
            int rows = capsule ? rings + 2 : rings + 1;
            VisualMeshData data = new VisualMeshData(rows * (segments + 1), (rows - 1) * segments * 2);
            int tri = 0;
            for (int row = 0; row < rows; row++)
            {
                float latitude = capsule ? (row <= rings / 2 ? row : row - 1) * Mathf.PI / rings : row * Mathf.PI / rings;
                float sin = Mathf.Sin(latitude), cos = Mathf.Cos(latitude);
                float offset = capsule ? (row <= rings / 2 ? .5f : -.5f) : 0;
                for (int segment = 0; segment <= segments; segment++)
                {
                    float angle = segment * Mathf.PI * 2f / segments;
                    int vertex = row * (segments + 1) + segment;
                    Vector3 normal = new Vector3(sin * Mathf.Cos(angle), cos, sin * Mathf.Sin(angle));
                    data.Vertices[vertex] = normal * .5f + Vector3.up * offset;
                    data.Normals[vertex] = normal;
                    data.Uv[vertex] = new Vector2(segment / (float)segments, row / (float)(rows - 1));
                    if (row == rows - 1 || segment == segments) continue;
                    data.Triangles[tri++] = vertex; data.Triangles[tri++] = vertex + 1; data.Triangles[tri++] = vertex + segments + 1;
                    data.Triangles[tri++] = vertex + 1; data.Triangles[tri++] = vertex + segments + 2; data.Triangles[tri++] = vertex + segments + 1;
                }
            }
            return data;
        }

        public static VisualMeshData Cylinder(int segments)
        {
            segments = Math.Max(8, Math.Min(32, segments));
            VisualMeshData data = new VisualMeshData((segments + 1) * 4 + 2, segments * 4);
            int tri = 0, centerBottom = data.Vertices.Length - 2, centerTop = data.Vertices.Length - 1;
            data.Vertices[centerBottom] = Vector3.down; data.Normals[centerBottom] = Vector3.down;
            data.Vertices[centerTop] = Vector3.up; data.Normals[centerTop] = Vector3.up;
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector3 normal = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                int a = i * 4;
                data.Vertices[a] = data.Vertices[a + 2] = normal * .5f + Vector3.down;
                data.Vertices[a + 1] = data.Vertices[a + 3] = normal * .5f + Vector3.up;
                data.Normals[a] = data.Normals[a + 1] = normal;
                data.Normals[a + 2] = Vector3.down; data.Normals[a + 3] = Vector3.up;
                for (int j = 0; j < 4; j++) data.Uv[a + j] = new Vector2(i / (float)segments, j % 2);
                if (i == segments) continue;
                int[] faces = { a, a + 1, a + 4, a + 4, a + 1, a + 5,
                    centerBottom, a + 2, a + 6, centerTop, a + 7, a + 3 };
                foreach (int v in faces) data.Triangles[tri++] = v;
            }
            return data;
        }

        public static VisualMeshData Rock()
        {
            VisualMeshData data = RoundBody(false, 12, 8);
            for (int i = 0; i < data.Vertices.Length; i++)
            {
                Vector3 p = data.Vertices[i];
                float contour = 1f + .12f * Mathf.Sin(p.x * 11f + p.z * 7f) * Mathf.Cos(p.y * 9f - p.z * 5f);
                data.Vertices[i] = new Vector3(p.x * contour, p.y * (.91f + .1f * Mathf.Cos(p.x * 9f)), p.z * contour);
            }
            // Rebuild smooth normals from the deformed triangles, then weld UV seams.
            Vector3[] normals = new Vector3[data.Vertices.Length];
            for (int i = 0; i < data.Triangles.Length; i += 3)
            {
                int a = data.Triangles[i], b = data.Triangles[i + 1], c = data.Triangles[i + 2];
                Vector3 face = Vector3.Cross(data.Vertices[b] - data.Vertices[a], data.Vertices[c] - data.Vertices[a]);
                normals[a] += face; normals[b] += face; normals[c] += face;
            }
            for (int row = 0; row <= 8; row++)
            {
                int first = row * 13, last = first + 12;
                Vector3 normal = (normals[first] + normals[last]).normalized;
                normals[first] = normals[last] = normal;
            }
            for (int i = 0; i < normals.Length; i++) data.Normals[i] = normals[i].sqrMagnitude > .000001f ? normals[i].normalized : data.Normals[i];
            return data;
        }
    }
}
