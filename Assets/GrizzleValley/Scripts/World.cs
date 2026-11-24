using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GrizzleValley
{
    /// <summary>
    /// Builds the valley from code: rolling meadow, dirt path, a river with a plank bridge,
    /// pines and round trees, tall swaying grass, rocks, a fence, grey mountains, clouds and the sky.
    /// The hunter stands at the origin looking down +Z.
    /// </summary>
    public static class World
    {
        public static readonly Color Grass = LP.Hex("#8ED13C");
        public static readonly Color GrassDark = LP.Hex("#5FA82C");
        public static readonly Color GrassTip = LP.Hex("#C8EE5A");
        public static readonly Color Dirt = LP.Hex("#D98A3A");
        public static readonly Color Water = LP.Hex("#58C8F2");
        public static readonly Color Wood = LP.Hex("#8A5A33");
        public static readonly Color Rock = LP.Hex("#8E959C");
        public static readonly Color PineCol = LP.Hex("#3E9A3A");
        public static readonly Color PineLight = LP.Hex("#56B547");
        public static readonly Color Leaf = LP.Hex("#7CC63D");
        public static readonly Color LeafYellow = LP.Hex("#B9D23A");
        public static readonly Color Mountain = LP.Hex("#9AA1A9");
        public static readonly Color SkyTop = LP.Hex("#5DB8F0");
        public static readonly Color SkyHorizon = LP.Hex("#D9F0FF");
        public static readonly Color Fog = LP.Hex("#CBE6F6");

        public static float RiverZ(float x) => 58f + 7f * Mathf.Sin(x / 17f) + 3f * Mathf.Sin(x / 7f + 1f);
        public static float PathX(float z) => 2.2f + 6.5f * Mathf.Sin(z / 24f) - 0.03f * Mathf.Min(z, 0f) * 0f;
        const float PathHalfWidth = 1.5f;
        const float RiverHalfWidth = 3.4f;

        public static float DistToPath(float x, float z)
        {
            if (z < -12f || z > RiverZ(PathX(z)) - 2f) return 999f;
            return Mathf.Abs(x - PathX(z));
        }

        public static float DistToRiver(float x, float z) => Mathf.Abs(z - RiverZ(x));

        public static float Height(float x, float z)
        {
            float r = Mathf.Sqrt(x * x + (z - 10f) * (z - 10f));
            float bumps = (Mathf.PerlinNoise(x * 0.06f + 30f, z * 0.06f + 11f) - 0.5f) * 1.0f;
            float hills = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(70f, 140f, r)) * (6f + 22f * Mathf.PerlinNoise(x * 0.012f + 5f, z * 0.012f + 3f));
            // Behind the hunter the ground rises a little so the view never shows an edge.
            float behind = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-15f, -60f, z)) * 6f;
            float river = -1.4f * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(RiverHalfWidth - 1f, RiverHalfWidth + 3f, DistToRiver(x, z))));
            return bumps + hills + behind + river;
        }

        public static GameObject Build(Transform parent, int seed)
        {
            var root = new GameObject("World");
            root.transform.SetParent(parent, false);
            var rnd = new System.Random(seed);

            BuildTerrain(root.transform, rnd);
            BuildPath(root.transform);
            BuildRiver(root.transform);
            BuildTrees(root.transform, rnd);
            BuildGrass(root.transform, rnd);
            BuildProps(root.transform, rnd);
            BuildMountains(root.transform, rnd);
            BuildClouds(root.transform, rnd);
            return root;
        }

        static void BuildTerrain(Transform parent, System.Random rnd)
        {
            var b = new MeshBuilder();
            const float cell = 3f;
            int nx = 100, nz = 96;
            float x0 = -150f, z0 = -60f;
            for (int ix = 0; ix < nx; ix++)
            for (int iz = 0; iz < nz; iz++)
            {
                float xa = x0 + ix * cell, za = z0 + iz * cell;
                var p00 = P(xa, za); var p10 = P(xa + cell, za); var p01 = P(xa, za + cell); var p11 = P(xa + cell, za + cell);
                b.Tri(p00, p01, p11, GroundColor((p00 + p01 + p11) / 3f, rnd));
                b.Tri(p00, p11, p10, GroundColor((p00 + p11 + p10) / 3f, rnd));
            }
            var mesh = b.Build("Terrain");
            var go = LP.MeshObject("Terrain", parent, mesh, LP.Lit);
            go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        static Vector3 P(float x, float z) => new Vector3(x, Height(x, z), z);

        static Color GroundColor(Vector3 c, System.Random rnd)
        {
            float dr = DistToRiver(c.x, c.z);
            if (dr < RiverHalfWidth + 0.8f) return LP.Vary(LP.Hex("#6FA9B8"), rnd, 0.05f);
            if (dr < RiverHalfWidth + 2.2f) return LP.Vary(LP.Hex("#C9C08A"), rnd, 0.05f);
            float hillT = Mathf.InverseLerp(4f, 20f, c.y);
            Color g = Color.Lerp(Grass, GrassDark, hillT * 0.8f);
            return LP.Vary(g, rnd, 0.06f);
        }

        static void BuildPath(Transform parent)
        {
            var b = new MeshBuilder();
            float zEnd = RiverZ(PathX(55f)) - RiverHalfWidth - 0.5f;
            Color c = Dirt, c2 = LP.Hex("#C97B30");
            int i = 0;
            for (float z = -14f; z < zEnd; z += 1f, i++)
            {
                float z1 = Mathf.Min(z + 1f, zEnd);
                Vector3 d0 = Side(z), d1 = Side(z1);
                Vector3 a = PathPt(z, -1, d0), bb = PathPt(z, 1, d0), cc = PathPt(z1, 1, d1), dd = PathPt(z1, -1, d1);
                Color col = (i % 3 == 0) ? c2 : c;
                b.Tri(a, dd, cc, col); b.Tri(a, cc, bb, col);
            }
            LP.MeshObject("Path", parent, b.Build("Path"), LP.Lit, false);

            // Plank bridge across the river where the path ends.
            float bz = RiverZ(PathX(zEnd));
            float bx = PathX(zEnd);
            var br = new MeshBuilder();
            float span = RiverHalfWidth * 2f + 3f;
            float y = Mathf.Max(Height(bx, zEnd), Height(bx, bz + RiverHalfWidth + 1.5f)) + 0.25f;
            for (float t = 0; t < span; t += 0.55f)
                br.Box(LP.TRS(new Vector3(bx, y, zEnd - 0.5f + t), new Vector3(0, (t * 37f) % 6f - 3f, 0), new Vector3(3.2f, 0.16f, 0.45f)), LP.Vary(Wood, new System.Random((int)(t * 100)), 0.12f));
            for (int s = -1; s <= 1; s += 2)
            {
                br.Box(LP.TRS(new Vector3(bx + s * 1.5f, y + 0.6f, zEnd - 0.5f + span / 2f), Vector3.zero, new Vector3(0.15f, 0.12f, span)), Wood);
                for (float t = 0; t <= span; t += span / 3f)
                    br.Box(LP.TRS(new Vector3(bx + s * 1.5f, y + 0.1f, zEnd - 0.5f + t), Vector3.zero, new Vector3(0.18f, 1.6f, 0.18f)), Wood);
            }
            LP.MeshObject("Bridge", parent, br.Build("Bridge"), LP.Lit);
        }

        static Vector3 Side(float z)
        {
            float dx = PathX(z + 0.5f) - PathX(z - 0.5f);
            return new Vector3(1f, 0, -dx).normalized;
        }

        static Vector3 PathPt(float z, int s, Vector3 side)
        {
            Vector3 p = new Vector3(PathX(z), 0, z) + side * (s * PathHalfWidth);
            p.y = Height(p.x, p.z) + 0.14f;
            return p;
        }

        static void BuildRiver(Transform parent)
        {
            var b = new MeshBuilder();
            float wy = -0.55f;
            Color deep = Water, foam = LP.Hex("#E8FAFF");
            for (float x = -160f; x < 160f; x += 2f)
            {
                float za0 = RiverZ(x), za1 = RiverZ(x + 2f);
                Vector3 l0 = new Vector3(x, wy, za0 - RiverHalfWidth - 0.6f), r0 = new Vector3(x, wy, za0 + RiverHalfWidth + 0.6f);
                Vector3 l1 = new Vector3(x + 2f, wy, za1 - RiverHalfWidth - 0.6f), r1 = new Vector3(x + 2f, wy, za1 + RiverHalfWidth + 0.6f);
                Vector3 m0 = (l0 + r0) / 2f, m1 = (l1 + r1) / 2f;
                b.Tri(l0, m0, m1, foam, deep, deep); b.Tri(l0, m1, l1, foam, deep, foam);
                b.Tri(m0, r0, r1, deep, foam, foam); b.Tri(m0, r1, m1, deep, foam, deep);
            }
            var go = LP.MeshObject("River", parent, b.Build("River"), Game.WaterMat != null ? Game.WaterMat : LP.Lit, false);
        }

        static bool Blocked(float x, float z, float pathClear, float riverClear)
        {
            return DistToPath(x, z) < pathClear || DistToRiver(x, z) < riverClear;
        }

        public static void Pine(MeshBuilder b, Vector3 p, float h, System.Random rnd)
        {
            b.Cone(LP.TRS(p + Vector3.down * 0.3f, Vector3.zero, Vector3.one), LP.Vary(Wood, rnd, 0.1f), 6, 0.22f * h / 6f + 0.12f, 0.12f, h * 0.35f);
            Color g = LP.Vary(rnd.NextDouble() < 0.5 ? PineCol : PineLight, rnd, 0.08f);
            float yaw = (float)rnd.NextDouble() * 360f;
            for (int k = 0; k < 3; k++)
            {
                float y = h * (0.22f + k * 0.22f);
                float r = h * (0.36f - k * 0.08f);
                b.Cone(LP.TRS(p + Vector3.up * y, new Vector3(0, yaw + k * 20f, 0), Vector3.one), g, 7, r, 0f, h * 0.42f, true, Color.Lerp(g, Color.white, 0.12f));
            }
        }

        public static void RoundTree(MeshBuilder b, Vector3 p, float h, System.Random rnd)
        {
            b.Cone(LP.TRS(p + Vector3.down * 0.3f, Vector3.zero, Vector3.one), LP.Vary(Wood, rnd, 0.1f), 6, 0.2f, 0.12f, h * 0.55f);
            Color g = LP.Vary(rnd.NextDouble() < 0.25 ? LeafYellow : Leaf, rnd, 0.08f);
            int blobs = 2 + rnd.Next(2);
            for (int k = 0; k < blobs; k++)
            {
                Vector3 o = new Vector3(((float)rnd.NextDouble() - 0.5f) * h * 0.35f, h * (0.6f + 0.18f * k), ((float)rnd.NextDouble() - 0.5f) * h * 0.35f);
                float s = h * (0.62f - 0.1f * k);
                b.Ico(LP.TRS(p + o, new Vector3(0, rnd.Next(360), 0), Vector3.one * s), g, 1, 0.18f, rnd.Next(), 0.06f);
            }
        }

        static void BuildTrees(Transform parent, System.Random rnd)
        {
            var b = new MeshBuilder();
            int placed = 0, tries = 0;
            while (placed < 520 && tries < 6000)
            {
                tries++;
                float ang = Mathf.Lerp(-170f, 170f, (float)rnd.NextDouble()) * Mathf.Deg2Rad;
                float dist = Mathf.Lerp(14f, 140f, Mathf.Pow((float)rnd.NextDouble(), 0.8f));
                float x = Mathf.Sin(ang) * dist, z = Mathf.Cos(ang) * dist + 6f;
                if (Blocked(x, z, 3.5f, RiverHalfWidth + 2.5f)) continue;
                // Keep the hunting meadow open, with a few trees for depth.
                bool meadow = Mathf.Abs(x) < 20f && z > 4f && z < 46f;
                if (meadow && rnd.NextDouble() < 0.88) continue;
                if (new Vector2(x, z).magnitude < 9f) continue;
                Vector3 p = new Vector3(x, Height(x, z), z);
                float h = Mathf.Lerp(6f, 13f, (float)rnd.NextDouble()) * (dist > 90 ? 1.2f : 1f);
                if (rnd.NextDouble() < 0.6) Pine(b, p, h, rnd); else RoundTree(b, p, h * 0.8f, rnd);
                placed++;
                if (b.VertexCount > 500000) break;
            }
            LP.MeshObject("Trees", parent, b.Build("Trees"), LP.Lit);
        }

        static void BuildGrass(Transform parent, System.Random rnd)
        {
            var b = new MeshBuilder();
            int clumps = 0, tries = 0;
            while (clumps < 6500 && tries < 30000)
            {
                tries++;
                float x = Mathf.Lerp(-38f, 38f, (float)rnd.NextDouble());
                float z = Mathf.Lerp(1.2f, 62f, Mathf.Pow((float)rnd.NextDouble(), 1.5f));
                if (Blocked(x, z, PathHalfWidth + 0.2f, RiverHalfWidth + 1.0f)) continue;
                if (Mathf.Abs(x) > z * 1.1f + 4f) continue; // outside the view cone
                float y = Height(x, z);
                bool near = z < 5f;
                int blades = near ? 5 : 4;
                for (int k = 0; k < blades; k++)
                {
                    Vector3 root = new Vector3(x + ((float)rnd.NextDouble() - 0.5f) * 0.5f, y - 0.05f, z + ((float)rnd.NextDouble() - 0.5f) * 0.5f);
                    float h = near ? Mathf.Lerp(0.8f, 1.25f, (float)rnd.NextDouble()) : Mathf.Lerp(0.45f, 0.95f, (float)rnd.NextDouble());
                    Color bc = LP.Vary(GrassDark, rnd, 0.08f);
                    Color tc = LP.Vary(rnd.NextDouble() < 0.5 ? GrassTip : Grass, rnd, 0.06f);
                    b.Blade(root, (float)rnd.NextDouble() * 360f, ((float)rnd.NextDouble() - 0.5f) * 36f, h, near ? 0.16f : 0.13f, bc, tc);
                }
                clumps++;
            }
            // Yellow and white flowers among the grass.
            for (int i = 0; i < 260; i++)
            {
                float x = Mathf.Lerp(-30f, 30f, (float)rnd.NextDouble());
                float z = Mathf.Lerp(3f, 50f, (float)rnd.NextDouble());
                if (Blocked(x, z, PathHalfWidth + 0.3f, RiverHalfWidth + 1f)) continue;
                Color fc = rnd.NextDouble() < 0.7 ? LP.Hex("#FFD93B") : Color.white;
                fc.a = 1;
                b.Ico(LP.TRS(new Vector3(x, Height(x, z) + 0.45f, z), Vector3.zero, new Vector3(0.16f, 0.06f, 0.16f)), fc, 0);
            }
            var go = LP.MeshObject("Grass", parent, b.Build("Grass"), LP.Lit, false);
        }

        static void BuildProps(Transform parent, System.Random rnd)
        {
            var b = new MeshBuilder();
            // Rocks
            for (int i = 0; i < 60; i++)
            {
                float x = Mathf.Lerp(-45f, 45f, (float)rnd.NextDouble());
                float z = Mathf.Lerp(4f, 70f, (float)rnd.NextDouble());
                if (Blocked(x, z, PathHalfWidth + 0.6f, RiverHalfWidth - 1f)) continue;
                float s = Mathf.Lerp(0.5f, 1.6f, (float)rnd.NextDouble());
                b.Ico(LP.TRS(new Vector3(x, Height(x, z) + s * 0.15f, z), new Vector3(0, rnd.Next(360), 0), new Vector3(s * 1.3f, s * 0.8f, s)), LP.Vary(Rock, rnd, 0.08f), 0, 0.25f, rnd.Next(), 0.1f);
            }
            // Bushes
            for (int i = 0; i < 70; i++)
            {
                float x = Mathf.Lerp(-50f, 50f, (float)rnd.NextDouble());
                float z = Mathf.Lerp(8f, 75f, (float)rnd.NextDouble());
                if (Blocked(x, z, PathHalfWidth + 1f, RiverHalfWidth + 1.5f)) continue;
                float s = Mathf.Lerp(1.0f, 2.2f, (float)rnd.NextDouble());
                b.Ico(LP.TRS(new Vector3(x, Height(x, z) + s * 0.25f, z), new Vector3(0, rnd.Next(360), 0), new Vector3(s * 1.2f, s * 0.8f, s)), LP.Vary(LP.Hex("#5DB33A"), rnd, 0.1f), 1, 0.2f, rnd.Next(), 0.05f);
            }
            // Fence along the left of the path
            for (float z = 6f; z < 34f; z += 2.6f)
            {
                Vector3 p = new Vector3(PathX(z) - PathHalfWidth - 0.8f, 0, z);
                p.y = Height(p.x, p.z);
                b.Box(LP.TRS(p + Vector3.up * 0.55f, new Vector3(0, 0, (z * 13f) % 6f - 3f), new Vector3(0.18f, 1.2f, 0.18f)), LP.Vary(Wood, rnd, 0.1f));
                Vector3 q = new Vector3(PathX(z + 2.6f) - PathHalfWidth - 0.8f, 0, z + 2.6f);
                q.y = Height(q.x, q.z);
                if (z + 2.6f < 34f)
                {
                    for (int r = 0; r < 2; r++)
                    {
                        Vector3 a = p + Vector3.up * (0.45f + r * 0.45f), c = q + Vector3.up * (0.45f + r * 0.45f);
                        Vector3 mid = (a + c) / 2f;
                        Quaternion rot = Quaternion.LookRotation(c - a);
                        b.Box(Matrix4x4.TRS(mid, rot, new Vector3(0.08f, 0.14f, (c - a).magnitude + 0.1f)), LP.Vary(Wood, rnd, 0.1f));
                    }
                }
            }
            LP.MeshObject("Props", parent, b.Build("Props"), LP.Lit);
        }

        static void BuildMountains(Transform parent, System.Random rnd)
        {
            var b = new MeshBuilder();
            for (int i = 0; i < 22; i++)
            {
                float ang = Mathf.Lerp(-110f, 110f, i / 21f) + ((float)rnd.NextDouble() - 0.5f) * 8f;
                float dist = Mathf.Lerp(190f, 260f, (float)rnd.NextDouble());
                float x = Mathf.Sin(ang * Mathf.Deg2Rad) * dist, z = Mathf.Cos(ang * Mathf.Deg2Rad) * dist;
                float h = Mathf.Lerp(55f, 115f, (float)rnd.NextDouble());
                float r = h * Mathf.Lerp(0.9f, 1.3f, (float)rnd.NextDouble());
                Color c = LP.Vary(Mountain, rnd, 0.06f);
                // A ridge: a low-poly rough cone squashed along one axis.
                b.Ico(LP.TRS(new Vector3(x, -h * 0.15f, z), new Vector3(0, rnd.Next(360), 0), new Vector3(r * 2.2f, h * 2f, r * 1.6f)), c, 1, 0.22f, rnd.Next(), 0.12f);
            }
            LP.MeshObject("Mountains", parent, b.Build("Mountains"), LP.Lit, false);
        }

        static void BuildClouds(Transform parent, System.Random rnd)
        {
            var b = new MeshBuilder();
            for (int i = 0; i < 12; i++)
            {
                float ang = Mathf.Lerp(-80f, 80f, (float)rnd.NextDouble());
                float dist = Mathf.Lerp(220f, 320f, (float)rnd.NextDouble());
                Vector3 c = new Vector3(Mathf.Sin(ang * Mathf.Deg2Rad) * dist, Mathf.Lerp(95f, 150f, (float)rnd.NextDouble()), Mathf.Cos(ang * Mathf.Deg2Rad) * dist);
                int puffs = 4 + rnd.Next(4);
                for (int k = 0; k < puffs; k++)
                {
                    Vector3 o = new Vector3((k - puffs / 2f) * 9f, ((float)rnd.NextDouble()) * 5f, ((float)rnd.NextDouble() - 0.5f) * 8f);
                    float s = Mathf.Lerp(12f, 22f, (float)rnd.NextDouble());
                    b.Ico(LP.TRS(c + o, new Vector3(0, rnd.Next(360), 0), new Vector3(s * 1.3f, s * 0.75f, s)), Color.white, 1, 0.1f, rnd.Next(), 0.03f);
                }
            }
            var go = LP.MeshObject("Clouds", parent, b.Build("Clouds"), Game.CloudMat != null ? Game.CloudMat : LP.Lit, false);
        }

        /// <summary>A gradient sky dome that follows the camera.</summary>
        public static GameObject BuildSky(Transform cam)
        {
            var b = new MeshBuilder();
            int seg = 24, rings = 8;
            for (int r = 0; r < rings; r++)
            {
                float t0 = r / (float)rings, t1 = (r + 1) / (float)rings;
                float e0 = Mathf.Lerp(-0.25f, 1f, t0) * Mathf.PI / 2f, e1 = Mathf.Lerp(-0.25f, 1f, t1) * Mathf.PI / 2f;
                Color c0 = Color.Lerp(SkyHorizon, SkyTop, Mathf.Pow(Mathf.Clamp01(Mathf.Sin(e0)), 0.6f));
                Color c1 = Color.Lerp(SkyHorizon, SkyTop, Mathf.Pow(Mathf.Clamp01(Mathf.Sin(e1)), 0.6f));
                for (int s = 0; s < seg; s++)
                {
                    float a0 = s * Mathf.PI * 2f / seg, a1 = (s + 1) * Mathf.PI * 2f / seg;
                    Vector3 p00 = Dir(a0, e0), p10 = Dir(a1, e0), p01 = Dir(a0, e1), p11 = Dir(a1, e1);
                    b.Tri(p00, p01, p11, c0, c1, c1); b.Tri(p00, p11, p10, c0, c1, c0);
                }
            }
            var mesh = b.Build("Sky");
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 2000f);
            var go = LP.MeshObject("Sky", null, mesh, Game.SkyMat, false);
            go.transform.localScale = Vector3.one * 480f;
            go.AddComponent<FollowPosition>().target = cam;
            return go;
        }

        static Vector3 Dir(float a, float e) => new Vector3(Mathf.Cos(a) * Mathf.Cos(e), Mathf.Sin(e), Mathf.Sin(a) * Mathf.Cos(e));
    }

    public class FollowPosition : MonoBehaviour
    {
        public Transform target;
        void LateUpdate() { if (target) transform.position = target.position; }
    }
}
