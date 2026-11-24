using System.Collections;
using TMPro;
using UnityEngine;

namespace GrizzleValley
{
    /// <summary>Hit bursts, tracers, muzzle flashes, score popups and camera shake.</summary>
    public static class Fx
    {
        public static Transform Root;
        public static float Shake;
        static Mesh flashMesh;

        public static void Burst(Vector3 at, Vector3 dir, int count, Color[] colors)
        {
            for (int i = 0; i < count; i++)
            {
                Color c = colors[i % colors.Length];
                float s = Random.Range(0.07f, 0.2f);
                var go = LP.Part("Chunk", Root, LP.BoxMesh(c), at + Random.insideUnitSphere * 0.25f, Vector3.one * s, Random.rotation.eulerAngles);
                go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var rb = go.AddComponent<Rigidbody>();
                go.AddComponent<BoxCollider>();
                rb.mass = 0.05f;
                Vector3 v = (Random.onUnitSphere + Vector3.up * 1.2f + dir.normalized * 0.8f).normalized * Random.Range(2.5f, 6.5f);
                rb.linearVelocity = v;
                rb.angularVelocity = Random.insideUnitSphere * 15f;
                go.AddComponent<FxLife>().Init(Random.Range(1.1f, 1.8f), true);
            }
        }

        /// <summary>A quick pop of big red cubes at the hit point.</summary>
        public static void Splat(Vector3 at, float size)
        {
            Color[] reds = { LP.Hex("#E3262E"), LP.Hex("#C0141F"), LP.Hex("#FF4B4B") };
            for (int i = 0; i < 9; i++)
            {
                var go = LP.Part("Splat", Root, LP.BoxMesh(reds[i % 3]), at + Random.insideUnitSphere * 0.35f * size, Vector3.one * 0.01f, Random.rotation.eulerAngles);
                go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                go.AddComponent<FxPop>().Init(Random.Range(0.18f, 0.38f) * size, Random.Range(0.25f, 0.45f));
            }
        }

        public static void Dust(Vector3 at)
        {
            Color[] c = { LP.Hex("#C9A06A"), LP.Hex("#E0C08F"), LP.Hex("#8ED13C") };
            for (int i = 0; i < 6; i++)
            {
                var go = LP.Part("Dust", Root, LP.BoxMesh(c[i % 3]), at + Random.insideUnitSphere * 0.2f, Vector3.one * 0.01f, Random.rotation.eulerAngles);
                go.AddComponent<FxPop>().Init(Random.Range(0.1f, 0.22f), Random.Range(0.25f, 0.4f));
            }
        }

        public static void Tracer(MonoBehaviour host, Vector3 from, Vector3 to)
        {
            host.StartCoroutine(TracerRoutine(from, to));
        }

        // Technique adapted from llamacademy/scriptable-object-based-guns (MIT): a trail that is moved
        // from the muzzle to the hit point at a fixed speed, then left to fade.
        static IEnumerator TracerRoutine(Vector3 from, Vector3 to)
        {
            var go = new GameObject("Tracer");
            go.transform.SetParent(Root, false);
            go.transform.position = from;
            var tr = go.AddComponent<TrailRenderer>();
            tr.sharedMaterial = LP.Fx;
            tr.time = 0.09f;
            tr.minVertexDistance = 0.05f;
            tr.widthCurve = new AnimationCurve(new Keyframe(0, 0.05f), new Keyframe(1, 0.0f));
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.95f, 0.6f), 0), new GradientColorKey(new Color(1f, 0.75f, 0.3f), 1) },
                      new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
            tr.colorGradient = g;
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            yield return null;
            float dist = Vector3.Distance(from, to), remaining = dist;
            const float speed = 260f;
            while (remaining > 0)
            {
                go.transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(1 - remaining / dist));
                remaining -= speed * Time.deltaTime;
                yield return null;
            }
            go.transform.position = to;
            yield return new WaitForSeconds(tr.time + 0.05f);
            Object.Destroy(go);
        }

        public static void MuzzleFlash(Transform muzzle)
        {
            if (flashMesh == null)
            {
                var b = new MeshBuilder();
                Color hot = new Color(1f, 0.97f, 0.7f, 1f), edge = new Color(1f, 0.7f, 0.15f, 0.0f);
                for (int k = 0; k < 8; k++)
                {
                    float a = k * Mathf.PI / 4f;
                    float len = (k % 2 == 0) ? 1f : 0.55f;
                    Vector3 tip = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0.3f) * len;
                    Vector3 s0 = new Vector3(Mathf.Cos(a + 0.35f), Mathf.Sin(a + 0.35f), 0) * 0.25f;
                    Vector3 s1 = new Vector3(Mathf.Cos(a - 0.35f), Mathf.Sin(a - 0.35f), 0) * 0.25f;
                    b.Tri(Vector3.zero, s0, tip, hot, hot, edge);
                    b.Tri(Vector3.zero, tip, s1, hot, edge, hot);
                }
                // A forward cone of flame.
                for (int k = 0; k < 6; k++)
                {
                    float a0 = k * Mathf.PI / 3f, a1 = (k + 1) * Mathf.PI / 3f;
                    Vector3 p0 = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0) * 0.3f, p1 = new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0) * 0.3f;
                    b.Tri(p0, p1, new Vector3(0, 0, 1.4f), hot, hot, edge);
                }
                flashMesh = b.Build("MuzzleFlash");
            }
            var go = LP.MeshObject("Flash", muzzle, flashMesh, LP.Fx, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            go.transform.localScale = Vector3.one * Random.Range(0.09f, 0.13f);
            go.layer = muzzle.gameObject.layer;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point; light.color = new Color(1f, 0.8f, 0.4f); light.range = 8f; light.intensity = 6f;
            go.AddComponent<FxLife>().Init(0.06f, false);
        }

        public static void Popup(Vector3 at, string text, Color color, float size)
        {
            var go = new GameObject("Popup");
            go.transform.SetParent(Root, false);
            go.transform.position = at;
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.font = Game.Font;
            tmp.fontSharedMaterial = Game.FontOutline;
            tmp.text = text;
            tmp.color = color;
            tmp.fontSize = 6f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.rectTransform.sizeDelta = new Vector2(8f, 2f);
            go.AddComponent<FxPopup>().Init(size);
        }
    }

    public class FxLife : MonoBehaviour
    {
        float life, t; bool shrink; Vector3 s0;
        public void Init(float life, bool shrink) { this.life = life; this.shrink = shrink; s0 = transform.localScale; }
        void Update()
        {
            t += Time.deltaTime;
            if (shrink) transform.localScale = s0 * Mathf.Clamp01(1f - Mathf.InverseLerp(life * 0.6f, life, t));
            if (t >= life) Destroy(gameObject);
        }
    }

    public class FxPop : MonoBehaviour
    {
        float size, life, t;
        public void Init(float size, float life) { this.size = size; this.life = life; }
        void Update()
        {
            t += Time.deltaTime;
            float k = t / life;
            float s = k < 0.3f ? Mathf.SmoothStep(0, size, k / 0.3f) : Mathf.Lerp(size, 0, (k - 0.3f) / 0.7f);
            transform.localScale = Vector3.one * Mathf.Max(0.001f, s);
            transform.position += Vector3.up * Time.deltaTime * 0.4f;
            if (t >= life) Destroy(gameObject);
        }
    }

    public class FxPopup : MonoBehaviour
    {
        float size, t; TextMeshPro tmp; Color c0;
        public void Init(float size) { this.size = size; tmp = GetComponent<TextMeshPro>(); c0 = tmp.color; }
        void LateUpdate()
        {
            t += Time.deltaTime;
            var cam = Camera.main;
            if (cam)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position, cam.transform.up);
                float d = Vector3.Distance(cam.transform.position, transform.position);
                float pop = t < 0.12f ? Mathf.Lerp(0.4f, 1.25f, t / 0.12f) : Mathf.Lerp(1.25f, 1f, Mathf.Clamp01((t - 0.12f) / 0.15f));
                transform.localScale = Vector3.one * (d * 0.045f * size * pop * (cam.fieldOfView / 70f));
            }
            transform.position += Vector3.up * Time.deltaTime * 0.6f;
            if (t > 0.8f) { var c = c0; c.a = Mathf.Lerp(1, 0, (t - 0.8f) / 0.35f); tmp.color = c; }
            if (t > 1.15f) Destroy(gameObject);
        }
    }
}
