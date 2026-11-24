using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GrizzleValley
{
    /// <summary>
    /// Builds flat-shaded, vertex-coloured meshes from simple shapes. Every triangle has its own
    /// vertices so normals come out faceted. Colour alpha is the wind weight (grass tips).
    /// </summary>
    public class MeshBuilder
    {
        readonly List<Vector3> v = new List<Vector3>();
        readonly List<Color> c = new List<Color>();
        readonly List<int> t = new List<int>();

        public int VertexCount => v.Count;

        public void Tri(Vector3 a, Vector3 b, Vector3 d, Color col) { Tri(a, b, d, col, col, col); }

        public void Tri(Vector3 a, Vector3 b, Vector3 d, Color ca, Color cb, Color cd)
        {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(d);
            c.Add(ca); c.Add(cb); c.Add(cd);
            t.Add(i); t.Add(i + 1); t.Add(i + 2);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 d, Vector3 e, Color col)
        {
            Tri(a, b, d, col); Tri(a, d, e, col);
        }

        static readonly Vector3[] boxCorners =
        {
            new Vector3(-.5f,-.5f,-.5f), new Vector3(.5f,-.5f,-.5f), new Vector3(.5f,.5f,-.5f), new Vector3(-.5f,.5f,-.5f),
            new Vector3(-.5f,-.5f,.5f), new Vector3(.5f,-.5f,.5f), new Vector3(.5f,.5f,.5f), new Vector3(-.5f,.5f,.5f)
        };
        static readonly int[] boxFaces = { 0,3,2,1, 1,2,6,5, 5,6,7,4, 4,7,3,0, 3,7,6,2, 4,0,1,5 };

        public void Box(Matrix4x4 m, Color col)
        {
            for (int f = 0; f < 6; f++)
            {
                Quad(m.MultiplyPoint3x4(boxCorners[boxFaces[f * 4]]), m.MultiplyPoint3x4(boxCorners[boxFaces[f * 4 + 1]]),
                     m.MultiplyPoint3x4(boxCorners[boxFaces[f * 4 + 2]]), m.MultiplyPoint3x4(boxCorners[boxFaces[f * 4 + 3]]), col);
            }
        }

        /// <summary>Unit sphere (radius 0.5) as an icosphere. Jitter roughens it for rocks and bushes.</summary>
        public void Ico(Matrix4x4 m, Color col, int subdiv = 1, float jitter = 0f, int seed = 0, float shade = 0f)
        {
            var tris = Icosphere.Get(subdiv);
            var rnd = new System.Random(seed);
            Dictionary<Vector3, Vector3> moved = null;
            if (jitter > 0) moved = new Dictionary<Vector3, Vector3>();
            System.Func<Vector3, Vector3> J = p =>
            {
                if (moved == null) return p * 0.5f;
                if (!moved.TryGetValue(p, out var q))
                {
                    q = p * (0.5f * (1f + jitter * ((float)rnd.NextDouble() * 2f - 1f)));
                    moved[p] = q;
                }
                return q;
            };
            for (int i = 0; i < tris.Length; i += 3)
            {
                var a = J(tris[i]); var b = J(tris[i + 1]); var d = J(tris[i + 2]);
                Color cc = col;
                if (shade > 0) { float k = 1f + shade * ((float)rnd.NextDouble() * 2f - 1f); cc = new Color(col.r * k, col.g * k, col.b * k, col.a); }
                Tri(m.MultiplyPoint3x4(a), m.MultiplyPoint3x4(b), m.MultiplyPoint3x4(d), cc);
            }
        }

        /// <summary>Cone or frustum along +Y from y=0 to y=h.</summary>
        public void Cone(Matrix4x4 m, Color col, int seg, float rBottom, float rTop, float h, bool cap = true, Color? topCol = null)
        {
            Color tc = topCol ?? col;
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * Mathf.PI * 2f / seg, a1 = (i + 1) * Mathf.PI * 2f / seg;
                Vector3 b0 = new Vector3(Mathf.Cos(a0) * rBottom, 0, Mathf.Sin(a0) * rBottom);
                Vector3 b1 = new Vector3(Mathf.Cos(a1) * rBottom, 0, Mathf.Sin(a1) * rBottom);
                Vector3 t0 = new Vector3(Mathf.Cos(a0) * rTop, h, Mathf.Sin(a0) * rTop);
                Vector3 t1 = new Vector3(Mathf.Cos(a1) * rTop, h, Mathf.Sin(a1) * rTop);
                b0 = m.MultiplyPoint3x4(b0); b1 = m.MultiplyPoint3x4(b1); t0 = m.MultiplyPoint3x4(t0); t1 = m.MultiplyPoint3x4(t1);
                if (rTop <= 0.0001f) Tri(b0, t0, b1, col, tc, col);
                else { Tri(b0, t0, b1, col, tc, col); Tri(b1, t0, t1, col, tc, tc); }
                if (cap)
                {
                    Tri(b1, m.MultiplyPoint3x4(Vector3.zero), b0, col);
                    if (rTop > 0.0001f) Tri(t0, m.MultiplyPoint3x4(new Vector3(0, h, 0)), t1, tc);
                }
            }
        }

        /// <summary>A grass blade: a thin double-sided triangle whose tip sways.</summary>
        public void Blade(Vector3 root, float yaw, float lean, float height, float width, Color baseCol, Color tipCol)
        {
            Quaternion q = Quaternion.Euler(lean, yaw, 0);
            Vector3 a = root + q * new Vector3(-width * .5f, 0, 0);
            Vector3 b = root + q * new Vector3(width * .5f, 0, 0);
            Vector3 tip = root + q * new Vector3(0, height, 0);
            Color bc = baseCol; bc.a = 0; Color tc = tipCol; tc.a = 1;
            Tri(a, tip, b, bc, tc, bc);
            Tri(b, tip, a, bc, tc, bc);
        }

        public void Append(MeshBuilder other, Matrix4x4 m)
        {
            int baseIndex = v.Count;
            for (int i = 0; i < other.v.Count; i++) { v.Add(m.MultiplyPoint3x4(other.v[i])); c.Add(other.c[i]); }
            for (int i = 0; i < other.t.Count; i++) t.Add(other.t[i] + baseIndex);
        }

        public Mesh Build(string name)
        {
            var mesh = new Mesh { name = name };
            if (v.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(v);
            mesh.SetColors(c);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }

    static class Icosphere
    {
        static readonly Dictionary<int, Vector3[]> cache = new Dictionary<int, Vector3[]>();

        public static Vector3[] Get(int subdiv)
        {
            if (cache.TryGetValue(subdiv, out var r)) return r;
            float p = (1f + Mathf.Sqrt(5f)) / 2f;
            Vector3[] vs =
            {
                new Vector3(-1,p,0), new Vector3(1,p,0), new Vector3(-1,-p,0), new Vector3(1,-p,0),
                new Vector3(0,-1,p), new Vector3(0,1,p), new Vector3(0,-1,-p), new Vector3(0,1,-p),
                new Vector3(p,0,-1), new Vector3(p,0,1), new Vector3(-p,0,-1), new Vector3(-p,0,1)
            };
            int[] f =
            {
                0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
                3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1
            };
            var list = new List<Vector3>();
            foreach (int i in f) list.Add(vs[i].normalized);
            for (int s = 0; s < subdiv; s++)
            {
                var next = new List<Vector3>();
                for (int i = 0; i < list.Count; i += 3)
                {
                    Vector3 a = list[i], b = list[i + 1], c = list[i + 2];
                    Vector3 ab = (a + b).normalized, bc = (b + c).normalized, ca = (c + a).normalized;
                    next.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }
                list = next;
            }
            // Unity is clockwise front-facing: flip the winding.
            for (int i = 0; i < list.Count; i += 3) { var tmp = list[i + 1]; list[i + 1] = list[i + 2]; list[i + 2] = tmp; }
            r = list.ToArray();
            cache[subdiv] = r;
            return r;
        }
    }

    /// <summary>Shared materials and helpers for making coloured parts.</summary>
    public static class LP
    {
        public static Material Lit;
        public static Material Fx;
        static readonly Dictionary<string, Mesh> meshCache = new Dictionary<string, Mesh>();

        public static Matrix4x4 TRS(Vector3 p, Vector3 euler, Vector3 s) => Matrix4x4.TRS(p, Quaternion.Euler(euler), s);

        public static Mesh BoxMesh(Color c) => Cached("box" + c, b => b.Box(Matrix4x4.identity, c));
        public static Mesh IcoMesh(Color c, int sub = 1) => Cached("ico" + sub + c, b => b.Ico(Matrix4x4.identity, c, sub));
        public static Mesh ConeMesh(Color c, int seg, float rTop) => Cached("cone" + seg + "_" + rTop + c, b => b.Cone(Matrix4x4.TRS(new Vector3(0, -0.5f, 0), Quaternion.identity, Vector3.one), c, seg, 0.5f, rTop * 0.5f, 1f));

        static Mesh Cached(string key, System.Action<MeshBuilder> make)
        {
            if (meshCache.TryGetValue(key, out var m) && m != null) return m;
            var b = new MeshBuilder();
            make(b);
            m = b.Build(key);
            meshCache[key] = m;
            return m;
        }

        public static GameObject Part(string name, Transform parent, Mesh mesh, Vector3 pos, Vector3 scale, Vector3 euler = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Lit;
            return go;
        }

        public static GameObject MeshObject(string name, Transform parent, Mesh mesh, Material mat, bool shadows = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return go;
        }

        public static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return QualitySettings.activeColorSpace == ColorSpace.Linear ? c.linear : c; }

        public static Color Vary(Color c, System.Random r, float amount)
        {
            float k = 1f + amount * ((float)r.NextDouble() * 2f - 1f);
            return new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), c.a);
        }

        public static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform) SetLayerRecursive(t.gameObject, layer);
        }
    }
}
