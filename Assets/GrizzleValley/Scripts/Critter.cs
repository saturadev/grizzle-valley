using UnityEngine;

namespace GrizzleValley
{
    public enum CritterKind { Puffhop, Antlerkin }

    /// <summary>
    /// Small meadow animals. Puffhops are round bunnies that hop between spots, Antlerkins are
    /// little deer that wander and graze. Shot ones burst into cubes and score.
    /// </summary>
    public class Critter : MonoBehaviour
    {
        public CritterKind kind;
        public int points;
        public bool dead;
        public Vector3 aimOffset;
        Transform body, head, earL, earR;
        Vector3 target;
        float timer, hopT, hopDur, baseY, grazeT;
        Vector3 hopFrom;
        bool hopping;
        float phase;

        public Vector3 AimPoint => transform.position + transform.rotation * aimOffset;

        public static Critter Spawn(CritterKind kind, Vector3 pos, Transform parent)
        {
            var go = new GameObject(kind.ToString());
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            var c = go.AddComponent<Critter>();
            c.kind = kind;
            c.phase = Random.value * 10f;
            if (kind == CritterKind.Puffhop) c.BuildPuffhop(); else c.BuildAntlerkin();
            c.PickTarget();
            return c;
        }

        void BuildPuffhop()
        {
            points = 200;
            Color fur = LP.Hex("#D9A15E"), belly = LP.Hex("#FFF4E2"), pink = LP.Hex("#F4A3B5");
            body = new GameObject("Body").transform; body.SetParent(transform, false);
            LP.Part("Torso", body, LP.IcoMesh(fur), new Vector3(0, 0.36f, 0), new Vector3(0.62f, 0.56f, 0.72f));
            LP.Part("Belly", body, LP.IcoMesh(belly), new Vector3(0, 0.3f, 0.14f), new Vector3(0.48f, 0.44f, 0.5f));
            LP.Part("Tail", body, LP.IcoMesh(Color.white, 0), new Vector3(0, 0.42f, -0.38f), Vector3.one * 0.24f);
            head = new GameObject("Head").transform; head.SetParent(body, false); head.localPosition = new Vector3(0, 0.66f, 0.26f);
            LP.Part("Skull", head, LP.IcoMesh(fur), Vector3.zero, new Vector3(0.46f, 0.42f, 0.44f));
            LP.Part("Muzzle", head, LP.IcoMesh(belly, 0), new Vector3(0, -0.06f, 0.18f), new Vector3(0.22f, 0.16f, 0.14f));
            LP.Part("Nose", head, LP.IcoMesh(pink, 0), new Vector3(0, -0.01f, 0.25f), Vector3.one * 0.07f);
            LP.Part("EyeL", head, LP.IcoMesh(LP.Hex("#1D1D1D"), 0), new Vector3(-0.13f, 0.06f, 0.17f), Vector3.one * 0.08f);
            LP.Part("EyeR", head, LP.IcoMesh(LP.Hex("#1D1D1D"), 0), new Vector3(0.13f, 0.06f, 0.17f), Vector3.one * 0.08f);
            earL = Ear(new Vector3(-0.1f, 0.16f, -0.02f), -12f, fur, pink);
            earR = Ear(new Vector3(0.1f, 0.16f, -0.02f), 12f, fur, pink);
            foreach (int s in new[] { -1, 1 })
                LP.Part("Foot", body, LP.IcoMesh(fur, 0), new Vector3(s * 0.2f, 0.07f, 0.05f), new Vector3(0.16f, 0.12f, 0.32f));
            aimOffset = new Vector3(0, 0.45f, 0.05f);
            var col = gameObject.AddComponent<SphereCollider>();
            col.center = new Vector3(0, 0.45f, 0.05f); col.radius = 0.48f;
        }

        Transform Ear(Vector3 pos, float tilt, Color fur, Color pink)
        {
            var e = new GameObject("Ear").transform; e.SetParent(head, false);
            e.localPosition = pos; e.localRotation = Quaternion.Euler(-10f, 0, tilt);
            LP.Part("Outer", e, LP.IcoMesh(fur, 0), new Vector3(0, 0.22f, 0), new Vector3(0.12f, 0.48f, 0.07f));
            LP.Part("Inner", e, LP.IcoMesh(pink, 0), new Vector3(0, 0.22f, 0.025f), new Vector3(0.07f, 0.36f, 0.04f));
            return e;
        }

        void BuildAntlerkin()
        {
            points = 300;
            Color fur = LP.Hex("#C97D3C"), light = LP.Hex("#F6E2C4"), dark = LP.Hex("#3A2A1E"), antler = LP.Hex("#EDE3CF");
            body = new GameObject("Body").transform; body.SetParent(transform, false);
            LP.Part("Torso", body, LP.IcoMesh(fur), new Vector3(0, 0.95f, 0), new Vector3(0.5f, 0.5f, 1.0f));
            LP.Part("Rump", body, LP.IcoMesh(light, 0), new Vector3(0, 1.0f, -0.46f), new Vector3(0.3f, 0.3f, 0.12f));
            foreach (var p in new[] { new Vector3(-0.15f, 0, 0.32f), new Vector3(0.15f, 0, 0.32f), new Vector3(-0.15f, 0, -0.32f), new Vector3(0.15f, 0, -0.32f) })
            {
                LP.Part("Leg", body, LP.BoxMesh(fur), p + new Vector3(0, 0.38f, 0), new Vector3(0.09f, 0.78f, 0.09f));
                LP.Part("Hoof", body, LP.BoxMesh(dark), p + new Vector3(0, 0.03f, 0.01f), new Vector3(0.1f, 0.08f, 0.12f));
            }
            head = new GameObject("Neck").transform; head.SetParent(body, false); head.localPosition = new Vector3(0, 1.1f, 0.38f);
            LP.Part("NeckPart", head, LP.BoxMesh(fur), new Vector3(0, 0.22f, 0.06f), new Vector3(0.18f, 0.5f, 0.2f), new Vector3(20f, 0, 0));
            var skull = LP.Part("Skull", head, LP.IcoMesh(fur), new Vector3(0, 0.5f, 0.2f), new Vector3(0.28f, 0.26f, 0.42f)).transform;
            LP.Part("Snout", head, LP.IcoMesh(light, 0), new Vector3(0, 0.45f, 0.38f), new Vector3(0.16f, 0.13f, 0.16f));
            LP.Part("Nose", head, LP.IcoMesh(dark, 0), new Vector3(0, 0.47f, 0.46f), Vector3.one * 0.06f);
            LP.Part("EyeL", head, LP.IcoMesh(dark, 0), new Vector3(-0.12f, 0.56f, 0.28f), Vector3.one * 0.06f);
            LP.Part("EyeR", head, LP.IcoMesh(dark, 0), new Vector3(0.12f, 0.56f, 0.28f), Vector3.one * 0.06f);
            foreach (int s in new[] { -1, 1 })
            {
                LP.Part("Ear", head, LP.IcoMesh(fur, 0), new Vector3(s * 0.17f, 0.62f, 0.12f), new Vector3(0.2f, 0.08f, 0.1f), new Vector3(0, 0, s * 25f));
                LP.Part("Antler", head, LP.BoxMesh(antler), new Vector3(s * 0.08f, 0.78f, 0.14f), new Vector3(0.05f, 0.3f, 0.05f), new Vector3(0, 0, -s * 18f));
                LP.Part("Tine", head, LP.BoxMesh(antler), new Vector3(s * 0.15f, 0.86f, 0.18f), new Vector3(0.04f, 0.16f, 0.04f), new Vector3(30f, 0, -s * 50f));
            }
            earL = earR = null;
            aimOffset = new Vector3(0, 1.0f, 0.1f);
            var col = gameObject.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 1.05f, 0.1f); col.radius = 0.42f; col.height = 1.6f; col.direction = 2;
        }

        void PickTarget()
        {
            for (int i = 0; i < 20; i++)
            {
                Vector3 t = transform.position + new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-4f, 4f));
                float ang = Mathf.Atan2(t.x, t.z) * Mathf.Rad2Deg;
                if (Mathf.Abs(ang) > 28f || t.z < 12f || t.z > 40f) continue;
                if (World.DistToRiver(t.x, t.z) < 6f) continue;
                target = t; return;
            }
            target = new Vector3(Random.Range(-6f, 6f), 0, Random.Range(15f, 30f));
        }

        void Update()
        {
            if (dead) return;
            float dt = Time.deltaTime;
            float t = Time.time + phase;
            if (kind == CritterKind.Puffhop)
            {
                if (!hopping)
                {
                    timer -= dt;
                    if (earL) { earL.localRotation = Quaternion.Euler(-10f + Mathf.Sin(t * 5f) * 6f, 0, -12f); earR.localRotation = Quaternion.Euler(-10f + Mathf.Sin(t * 5f + 1f) * 6f, 0, 12f); }
                    body.localScale = new Vector3(1f, 1f + Mathf.Sin(t * 6f) * 0.025f, 1f);
                    if (timer <= 0)
                    {
                        if ((transform.position - target).sqrMagnitude < 0.5f) PickTarget();
                        hopping = true; hopT = 0; hopDur = 0.45f; hopFrom = transform.position;
                    }
                }
                else
                {
                    hopT += dt;
                    float k = Mathf.Clamp01(hopT / hopDur);
                    Vector3 to = Vector3.MoveTowards(hopFrom, target, 1.4f);
                    Vector3 p = Vector3.Lerp(hopFrom, to, k);
                    p.y = World.Height(p.x, p.z) + Mathf.Sin(k * Mathf.PI) * 0.55f;
                    transform.position = p;
                    Vector3 flat = to - hopFrom; flat.y = 0;
                    if (flat.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat), dt * 10f);
                    body.localScale = new Vector3(1f - Mathf.Sin(k * Mathf.PI) * 0.1f, 1f + Mathf.Sin(k * Mathf.PI) * 0.18f, 1f);
                    if (k >= 1f) { hopping = false; timer = Random.Range(0.15f, 0.9f); if ((transform.position - target).sqrMagnitude < 0.5f) PickTarget(); }
                }
            }
            else
            {
                Vector3 to = target - transform.position; to.y = 0;
                if (grazeT > 0)
                {
                    grazeT -= dt;
                    head.localRotation = Quaternion.Slerp(head.localRotation, Quaternion.Euler(70f, 0, 0), dt * 4f);
                    if (grazeT <= 0) PickTarget();
                }
                else if (to.magnitude < 0.3f) { grazeT = Random.Range(1.2f, 2.8f); }
                else
                {
                    head.localRotation = Quaternion.Slerp(head.localRotation, Quaternion.Euler(Mathf.Sin(t * 2f) * 5f, 0, 0), dt * 4f);
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), dt * 3f);
                    Vector3 p = transform.position + transform.forward * 1.1f * dt;
                    p.y = World.Height(p.x, p.z);
                    transform.position = p;
                    body.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(t * 7f)) * 0.04f, 0);
                }
            }
        }

        public void Kill(Vector3 hitDir)
        {
            if (dead) return;
            dead = true;
            Color fur = kind == CritterKind.Puffhop ? LP.Hex("#D9A15E") : LP.Hex("#C97D3C");
            Fx.Burst(AimPoint, hitDir, kind == CritterKind.Puffhop ? 22 : 30, new[] { LP.Hex("#D8202A"), LP.Hex("#B3121E"), LP.Hex("#F0343C"), fur, Color.white });
            Fx.Splat(AimPoint, 1.2f);
            Fx.Popup(AimPoint + Vector3.up * 0.5f, points.ToString(), Hud.Gold, 1f);
            Destroy(gameObject);
        }
    }
}
