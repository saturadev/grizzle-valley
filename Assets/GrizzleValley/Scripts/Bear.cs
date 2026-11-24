using UnityEngine;

namespace GrizzleValley
{
    /// <summary>
    /// Big Mumbles: a huge, goofy, googly-eyed bear. Walks in on all fours, rears up and roars,
    /// flinches comically when a ring is hit, swipes on a miss, sits down happy when he loses,
    /// and lunges in for a hug when he wins. All poses are procedural.
    /// </summary>
    public class Bear : MonoBehaviour
    {
        public enum Mode { Walk, Stand, Sit, Hug }
        public Mode mode = Mode.Walk;
        public Vector3 walkTarget;
        public float walkSpeed = 6.5f;

        Transform hips, chest, head, jaw, armL, armR, legL, legR, pupilL, pupilR, eyeL, eyeR, browL, browR, tummy;
        public Transform[] ringPoints;

        float rear;            // 0 on all fours, 1 standing
        float roar;            // jaw open and arms up
        float flinch;          // impulse after a hit
        float swipe;           // impulse after a miss
        float swipeSide = 1;
        float sit, hug, happy;
        float walkPhase;
        Vector2 pupilOff, pupilVel;
        float spin;

        public bool Arrived => (transform.position - walkTarget).sqrMagnitude < 0.04f;

        public static Bear Spawn(Vector3 pos, Vector3 target, Transform parent)
        {
            var go = new GameObject("Big Mumbles");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var b = go.AddComponent<Bear>();
            b.walkTarget = target; go.transform.localScale = Vector3.one * 1.25f;
            Vector3 d = target - pos; d.y = 0;
            go.transform.rotation = Quaternion.LookRotation(d);
            b.Build();
            return b;
        }

        void Build()
        {
            Color fur = LP.Hex("#8B5A2E"), furDark = LP.Hex("#6A4122"), belly = LP.Hex("#E7C9A0"), snout = LP.Hex("#F0D7B0");
            Color black = LP.Hex("#1A1414"), mouth = LP.Hex("#7A1E28"), tongue = LP.Hex("#F07C8C"), claw = LP.Hex("#F4F1E8");

            legL = Pivot("LegL", transform, new Vector3(-0.55f, 1.05f, 0));
            legR = Pivot("LegR", transform, new Vector3(0.55f, 1.05f, 0));
            foreach (var leg in new[] { legL, legR })
            {
                LP.Part("Thigh", leg, LP.IcoMesh(fur), new Vector3(0, -0.45f, 0), new Vector3(0.75f, 1.15f, 0.8f));
                LP.Part("Foot", leg, LP.IcoMesh(furDark), new Vector3(0, -0.98f, 0.18f), new Vector3(0.6f, 0.3f, 0.85f));
            }

            hips = Pivot("Hips", transform, new Vector3(0, 1.1f, 0));
            LP.Part("Torso", hips, LP.IcoMesh(fur), new Vector3(0, 0.95f, 0), new Vector3(1.75f, 2.2f, 1.45f));
            tummy = LP.Part("Belly", hips, LP.IcoMesh(belly), new Vector3(0, 0.85f, 0.48f), new Vector3(1.3f, 1.7f, 0.7f)).transform;
            LP.Part("Tail", hips, LP.IcoMesh(furDark, 0), new Vector3(0, 0.1f, -0.7f), Vector3.one * 0.3f);
            chest = Pivot("Chest", hips, new Vector3(0, 1.75f, 0.1f));

            head = Pivot("Head", chest, new Vector3(0, 0.45f, 0.12f));
            LP.Part("Skull", head, LP.IcoMesh(fur), new Vector3(0, 0.1f, 0), new Vector3(1.25f, 1.08f, 1.08f));
            LP.Part("Cheeks", head, LP.IcoMesh(fur), new Vector3(0, -0.12f, 0.12f), new Vector3(1.35f, 0.7f, 0.9f));
            LP.Part("Snout", head, LP.IcoMesh(snout), new Vector3(0, -0.12f, 0.52f), new Vector3(0.62f, 0.42f, 0.5f));
            LP.Part("Nose", head, LP.IcoMesh(black), new Vector3(0, 0.0f, 0.78f), new Vector3(0.3f, 0.18f, 0.18f));
            foreach (int s in new[] { -1, 1 })
            {
                var ear = Pivot(s < 0 ? "EarL" : "EarR", head, new Vector3(s * 0.48f, 0.55f, -0.05f));
                LP.Part("Outer", ear, LP.IcoMesh(furDark), Vector3.zero, new Vector3(0.38f, 0.36f, 0.2f));
                LP.Part("Inner", ear, LP.IcoMesh(snout, 0), new Vector3(0, -0.02f, 0.06f), new Vector3(0.22f, 0.2f, 0.1f));
            }
            // Googly eyes
            eyeL = LP.Part("EyeL", head, LP.IcoMesh(Color.white), new Vector3(-0.22f, 0.32f, 0.43f), new Vector3(0.36f, 0.4f, 0.24f)).transform;
            eyeR = LP.Part("EyeR", head, LP.IcoMesh(Color.white), new Vector3(0.24f, 0.34f, 0.43f), new Vector3(0.4f, 0.44f, 0.24f)).transform;
            pupilL = LP.Part("PupilL", head, LP.IcoMesh(black, 0), new Vector3(-0.22f, 0.3f, 0.56f), Vector3.one * 0.13f).transform;
            pupilR = LP.Part("PupilR", head, LP.IcoMesh(black, 0), new Vector3(0.24f, 0.32f, 0.56f), Vector3.one * 0.14f).transform;
            browL = LP.Part("BrowL", head, LP.BoxMesh(furDark), new Vector3(-0.22f, 0.58f, 0.44f), new Vector3(0.3f, 0.07f, 0.1f), new Vector3(0, 0, -15f)).transform;
            browR = LP.Part("BrowR", head, LP.BoxMesh(furDark), new Vector3(0.24f, 0.6f, 0.44f), new Vector3(0.3f, 0.07f, 0.1f), new Vector3(0, 0, 15f)).transform;
            // Mouth: the open part is a dark box behind the jaw.
            LP.Part("MouthInside", head, LP.IcoMesh(mouth, 0), new Vector3(0, -0.3f, 0.5f), new Vector3(0.5f, 0.22f, 0.3f));
            jaw = Pivot("Jaw", head, new Vector3(0, -0.28f, 0.28f));
            LP.Part("LowerJaw", jaw, LP.IcoMesh(snout), new Vector3(0, -0.06f, 0.24f), new Vector3(0.5f, 0.18f, 0.42f));
            LP.Part("Tongue", jaw, LP.IcoMesh(tongue, 0), new Vector3(0, 0.02f, 0.24f), new Vector3(0.3f, 0.08f, 0.3f));
            foreach (int s in new[] { -1, 1 })
            {
                LP.Part("Fang", head, LP.ConeMesh(claw, 4, 0), new Vector3(s * 0.15f, -0.27f, 0.68f), new Vector3(0.07f, 0.12f, 0.07f), new Vector3(180f, 0, 0));
                LP.Part("LowFang", jaw, LP.ConeMesh(claw, 4, 0), new Vector3(s * 0.13f, 0.04f, 0.4f), new Vector3(0.06f, 0.1f, 0.06f));
            }

            armL = Arm("ArmL", -1, fur, furDark, claw);
            armR = Arm("ArmR", 1, fur, furDark, claw);

            // Ring targets: face, belly, both paws, nose.
            ringPoints = new[]
            {
                Marker("RingFace", head, new Vector3(0.05f, 0.25f, 0.6f)),
                Marker("RingBelly", hips, new Vector3(0, 0.85f, 0.85f)),
                Marker("RingPawL", armL, new Vector3(0, -1.25f, 0.3f)),
                Marker("RingPawR", armR, new Vector3(0, -1.25f, 0.3f)),
                Marker("RingNose", head, new Vector3(0, -0.05f, 0.85f)),
                Marker("RingChest", chest, new Vector3(-0.35f, -0.2f, 0.7f)),
            };

            var col = gameObject.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 1.8f, 0.1f); col.radius = 1.0f; col.height = 3.8f;
        }

        Transform Pivot(string name, Transform parent, Vector3 pos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            return t;
        }

        Transform Marker(string name, Transform parent, Vector3 pos) => Pivot(name, parent, pos);

        Transform Arm(string name, int s, Color fur, Color furDark, Color claw)
        {
            var a = Pivot(name, chest, new Vector3(s * 0.85f, -0.05f, 0.1f));
            LP.Part("Upper", a, LP.IcoMesh(fur), new Vector3(0, -0.55f, 0), new Vector3(0.5f, 1.3f, 0.52f));
            LP.Part("Paw", a, LP.IcoMesh(furDark), new Vector3(0, -1.2f, 0.06f), new Vector3(0.55f, 0.42f, 0.5f));
            LP.Part("Pad", a, LP.IcoMesh(LP.Hex("#3B2418"), 0), new Vector3(0, -1.22f, 0.28f), new Vector3(0.3f, 0.24f, 0.08f));
            for (int k = -1; k <= 1; k++)
                LP.Part("Claw", a, LP.ConeMesh(claw, 4, 0), new Vector3(k * 0.14f, -1.45f, 0.18f), new Vector3(0.07f, 0.2f, 0.07f), new Vector3(200f, 0, 0));
            return a;
        }

        public void Roar() { roar = 1.6f; spin = 0; }
        public void Hit() { flinch = 1f; spin = 1f; Fx.Burst(ringPoints[1].position, -transform.forward, 10, new[] { LP.Hex("#8B5A2E"), LP.Hex("#6A4122"), LP.Hex("#E7C9A0") }); }
        public void Swipe() { swipe = 1f; swipeSide = -swipeSide; }
        public void SitDown() { mode = Mode.Sit; }
        public void HugLunge() { mode = Mode.Hug; }

        void Update()
        {
            float dt = Time.deltaTime;
            float t = Time.time;
            flinch = Mathf.MoveTowards(flinch, 0, dt * 2.4f);
            swipe = Mathf.MoveTowards(swipe, 0, dt * 2.2f);
            roar = Mathf.MoveTowards(roar, 0, dt);
            spin = Mathf.MoveTowards(spin, 0, dt * 1.4f);

            float targetRear = mode == Mode.Walk ? 0f : 1f;
            rear = Mathf.MoveTowards(rear, targetRear, dt * 1.6f);
            sit = Mathf.MoveTowards(sit, mode == Mode.Sit ? 1f : 0f, dt * 1.8f);
            hug = Mathf.MoveTowards(hug, mode == Mode.Hug ? 1f : 0f, dt * 1.5f);
            happy = Mathf.MoveTowards(happy, mode == Mode.Sit ? 1f : 0f, dt);

            if (mode == Mode.Walk)
            {
                Vector3 to = walkTarget - transform.position; to.y = 0;
                float step = walkSpeed * dt;
                if (to.magnitude > 0.01f)
                {
                    Vector3 p = transform.position + Vector3.ClampMagnitude(to, step);
                    p.y = World.Height(p.x, p.z);
                    transform.position = p;
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to.normalized), dt * 4f);
                    walkPhase += dt * 7.5f;
                }
            }
            if (mode == Mode.Hug)
            {
                Vector3 cam = Camera.main ? Camera.main.transform.position : transform.position;
                Vector3 to = cam - transform.position; to.y = 0;
                if (to.magnitude > 1.6f) transform.position += to.normalized * dt * 3.5f;
            }

            float walkK = 1f - rear;
            float sw = Mathf.Sin(walkPhase);
            // Hips pitch forward on all fours, upright when standing, lean back when sitting.
            float hipPitch = Mathf.Lerp(0f, 72f, walkK) - sit * 16f + flinch * -14f + hug * 22f + Mathf.Sin(t * 1.6f) * 2.5f * rear;
            hips.localPosition = new Vector3(0, 1.1f - sit * 0.75f + Mathf.Abs(sw) * 0.08f * walkK - flinch * 0.12f, -walkK * 0.4f);
            hips.localRotation = Quaternion.Euler(hipPitch, Mathf.Sin(t * 1.1f) * 4f * rear + swipe * 18f * swipeSide, Mathf.Sin(walkPhase * 0.5f) * 4f * walkK + Mathf.Sin(t * 3f) * 3f * happy);
            float squash = 1f - flinch * 0.12f;
            hips.localScale = new Vector3(1f + flinch * 0.08f, squash, 1f + flinch * 0.06f);

            head.localRotation = Quaternion.Euler(-hipPitch * 0.75f - roar * 22f + flinch * 18f + Mathf.Sin(t * 2.2f) * 4f * rear, Mathf.Sin(t * 0.9f) * 8f * rear, Mathf.Sin(t * 2.7f) * 9f * happy + flinch * 12f * swipeSide);
            jaw.localRotation = Quaternion.Euler(Mathf.Clamp01(roar) * 38f + flinch * 30f + happy * (10f + Mathf.Sin(t * 6f) * 8f) + hug * 25f + Mathf.Max(0, Mathf.Sin(t * 3.1f)) * 6f * rear, 0, 0);

            // Arms: legs on all fours, claws up for the roar, swipe on a miss, wave when happy, wide for the hug.
            float roarUp = Mathf.Clamp01(roar);
            for (int s = -1; s <= 1; s += 2)
            {
                Transform a = s < 0 ? armL : armR;
                float walkSwing = sw * 30f * s * walkK;
                float pitchA = -hipPitch * walkK + walkSwing;
                pitchA += rear * (-25f + Mathf.Sin(t * 2f + s) * 8f) - roarUp * 120f;
                float roll = s * (12f + roarUp * 25f + rear * 10f);
                if (swipe > 0 && s == (int)swipeSide) { pitchA += -95f * Mathf.Sin(swipe * Mathf.PI); roll += -s * 40f * Mathf.Sin(swipe * Mathf.PI); }
                if (happy > 0 && s == 1) { pitchA = Mathf.Lerp(pitchA, -150f, happy); roll = Mathf.Lerp(roll, 30f + Mathf.Sin(t * 8f) * 25f, happy); }
                if (happy > 0 && s == -1) { pitchA = Mathf.Lerp(pitchA, -30f, happy); roll = Mathf.Lerp(roll, -20f, happy); }
                pitchA = Mathf.Lerp(pitchA, -80f, hug);
                roll = Mathf.Lerp(roll, s * 55f, hug);
                a.localRotation = Quaternion.Euler(pitchA, 0, roll);
            }
            legL.localRotation = Quaternion.Euler(-sw * 25f * walkK - sit * 80f, 0, -sit * 12f);
            legR.localRotation = Quaternion.Euler(sw * 25f * walkK - sit * 80f, 0, sit * 12f);

            // Googly pupils: springy, they roll around after a hit.
            Vector2 goal = new Vector2(Mathf.Sin(t * 1.3f) * 0.03f, Mathf.Sin(t * 1.7f) * 0.02f);
            if (spin > 0) goal = new Vector2(Mathf.Cos(t * 18f), Mathf.Sin(t * 18f)) * 0.07f * spin;
            Vector2 acc = (goal - pupilOff) * 220f - pupilVel * 9f;
            pupilVel += acc * dt;
            pupilOff += pupilVel * dt;
            pupilOff = Vector2.ClampMagnitude(pupilOff, 0.09f);
            pupilL.localPosition = new Vector3(-0.22f + pupilOff.x, 0.3f + pupilOff.y, 0.56f);
            pupilR.localPosition = new Vector3(0.24f - pupilOff.x * 0.8f, 0.32f + pupilOff.y * 1.2f, 0.56f);
            float eyePop = 1f + flinch * 0.35f + roarUp * 0.15f;
            eyeL.localScale = new Vector3(0.36f, 0.4f, 0.24f) * eyePop;
            eyeR.localScale = new Vector3(0.4f, 0.44f, 0.24f) * eyePop;
            float browAngle = Mathf.Lerp(-15f, 20f, Mathf.Max(happy, flinch)) - roarUp * 10f;
            browL.localRotation = Quaternion.Euler(0, 0, browAngle);
            browR.localRotation = Quaternion.Euler(0, 0, -browAngle);
            browL.localPosition = new Vector3(-0.22f, 0.58f + flinch * 0.08f, 0.44f);
            browR.localPosition = new Vector3(0.24f, 0.6f + flinch * 0.08f, 0.44f);
            tummy.localScale = new Vector3(1.3f, 1.7f, 0.7f) * (1f + Mathf.Sin(t * 2.4f) * 0.02f + flinch * 0.1f);
        }
    }
}
