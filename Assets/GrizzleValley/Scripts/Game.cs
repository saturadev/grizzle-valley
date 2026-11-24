using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR && SATURA_SPACE
using SaturaSpace;
#endif

namespace GrizzleValley
{
    /// <summary>
    /// Grizzle Valley. Hunt critters in a bright meadow through your rifle scope. After a few
    /// kills Big Mumbles the bear stumbles in, rears up and roars, and turns out to be a
    /// timing game: click when the shrinking ring closes on the orange target. Five misses
    /// and he hugs you. Six hits and he sits down, defeated and happy.
    /// </summary>
    public class Game : MonoBehaviour
    {
        public enum State { Hunt, BearIn, Rings, Won, Lost }

        public static Game I;
        public static Material LitMat, FxMat, WaterMat, CloudMat, SkyMat;
        public static TMP_FontAsset Font;
        public static Material FontOutline;

        public const int KillsForBear = 4;
        public const int HitsNeeded = 6;
        public const int MissLimit = 5;

        public State state = State.Hunt;
        public int score, kills, hits, misses;
        public bool botMode;
        public bool CanScope => state == State.Hunt;

        Hunter hunter;
        Hud hud;
        Bear bear;
        Transform runtime;
        readonly List<Critter> critters = new List<Critter>();
        float respawnT, stateT, hintShown, lastKill;

        // rings
        bool ringActive; float ringU, ringGap; Transform ringPoint; int lastRing = -1;
        float ringDur => Mathf.Lerp(1.3f, 1.0f, hits / (float)HitsNeeded);
        bool botMissed;

        static void LoadAssets()
        {
            LitMat = Resources.Load<Material>("GV_Lit");
            FxMat = Resources.Load<Material>("GV_Fx");
            WaterMat = Resources.Load<Material>("GV_Water");
            CloudMat = Resources.Load<Material>("GV_Cloud");
            SkyMat = Resources.Load<Material>("GV_Sky");
            Font = Resources.Load<TMP_FontAsset>("TitanOne SDF");
            FontOutline = Resources.Load<Material>("TitanOne Outline");
            LP.Lit = LitMat; LP.Fx = FxMat;
        }

        void Awake()
        {
            I = this;
            LoadAssets();
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = World.Fog;
            RenderSettings.fogStartDistance = 130f;
            RenderSettings.fogEndDistance = 420f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.54f, 0.64f);

            runtime = new GameObject("Runtime").transform;
            runtime.SetParent(transform, false);
            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(runtime, false);
            sunGo.transform.rotation = Quaternion.Euler(48f, -35f, 0);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional; sun.color = new Color(1f, 0.96f, 0.84f); sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.75f;
            RenderSettings.sun = sun;

            Fx.Root = runtime;
            World.Build(runtime, 7);
            hunter = Hunter.Create(runtime);
            hud = Hud.Create(runtime);
            World.BuildSky(hunter.cam.transform);
            Fx.Root = runtime;
        }

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Restart(false);
        }

        public void Restart(bool bot)
        {
            botMode = bot;
            StopAllCoroutines();
            foreach (var c in critters) if (c) Destroy(c.gameObject);
            critters.Clear();
            if (bear) Destroy(bear.gameObject);
            for (int i = runtime.childCount - 1; i >= 0; i--)
            {
                var n = runtime.GetChild(i).name;
                if (n == "Chunk" || n == "Splat" || n == "Dust" || n == "Tracer" || n == "Popup") Destroy(runtime.GetChild(i).gameObject);
            }
            hunter.bot = bot;
            hunter.ResetLook();
            Random.InitState(bot ? 11 : System.Environment.TickCount);
            state = State.Hunt; score = kills = hits = misses = 0;
            ringActive = false; botMissed = false; stateT = 0; respawnT = 0;
            hud.HideRing(); hud.SetScore(0); hud.SetStatus("HUNT  0/" + KillsForBear);
            hud.ShowScope(0);
            Fx.Shake = 0;
            // The opening: a far bunny for the scope, others around the meadow.
            Spawn(CritterKind.Puffhop, new Vector3(-3.5f, 0, 27f));
            Spawn(CritterKind.Puffhop, new Vector3(5.5f, 0, 17f));
            Spawn(CritterKind.Antlerkin, new Vector3(-8f, 0, 31f));
            Spawn(CritterKind.Antlerkin, new Vector3(9f, 0, 24f));
            hud.Hint("MOUSE: AIM   CLICK: SHOOT\nRIGHT CLICK: SCOPE", 6f);
        }

        void Spawn(CritterKind kind, Vector3 pos)
        {
            pos.y = World.Height(pos.x, pos.z);
            critters.Add(Critter.Spawn(kind, pos, runtime));
        }

        void SpawnRandom()
        {
            var kind = Random.value < 0.6f ? CritterKind.Puffhop : CritterKind.Antlerkin;
            Spawn(kind, new Vector3(Random.Range(-11f, 11f), 0, Random.Range(15f, 34f)));
        }

        public void OnFirePressed() { TryFire(); }

        public void TryFire()
        {
            if (state == State.Won || state == State.Lost) return;
            var hit = hunter.Fire(out bool fired);
            if (!fired) return;
            if (state == State.Hunt)
            {
                if (hit.collider == null) return;
                var c = hit.collider.GetComponentInParent<Critter>();
                if (c != null && !c.dead)
                {
                    score += c.points; kills++; lastKill = Time.time;
                    hud.SetScore(score);
                    hud.SetStatus("HUNT  " + Mathf.Min(kills, KillsForBear) + "/" + KillsForBear);
                    c.Kill(hunter.cam.transform.forward);
                    critters.Remove(c);
                    respawnT = 1.0f;
                    if (kills >= KillsForBear) StartBear();
                }
            }
            else if (state == State.Rings && ringActive)
            {
                if (!AimedAtRing()) return;
                if (ringU >= 0.78f && ringU <= 1.2f) RingHit(ringU >= 0.9f && ringU <= 1.1f);
                else if (ringU < 0.78f) RingMiss();
            }
        }

        // ---------- bear encounter ----------
        void StartBear()
        {
            state = State.BearIn; stateT = 0;
            foreach (var c in critters) if (c) c.GetComponent<Collider>().enabled = false;
            Vector3 f = hunter.transform.forward; f.y = 0; f.Normalize();
            Vector3 from = f * 36f + Vector3.Cross(Vector3.up, f) * 6f;
            Vector3 to = f * 8f;
            from.y = World.Height(from.x, from.z); to.y = World.Height(to.x, to.z);
            bear = Bear.Spawn(from, to, runtime);
            hud.SetStatus("BIG MUMBLES!");
            hud.Message("BIG MUMBLES!", "HE LOOKS SCARY...", 2.4f, Hud.Orange);
            Fx.Shake = 0.2f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            stateT += dt;
            hud.ShowScope(hunter.ScopeAmount);
            if (state == State.Hunt)
            {
                critters.RemoveAll(c => c == null || c.dead);
                respawnT -= dt;
                if (critters.Count < 4 && respawnT <= 0 && kills < KillsForBear) { SpawnRandom(); respawnT = 0.8f; }
            }
            else if (state == State.BearIn)
            {
                if (bear.Arrived)
                {
                    Vector3 d = hunter.transform.position - bear.transform.position; d.y = 0;
                    bear.transform.rotation = Quaternion.LookRotation(d);
                    bear.mode = Bear.Mode.Stand;
                    bear.Roar();
                    Fx.Shake = 0.6f;
                    state = State.Rings; stateT = 0; ringActive = false; ringGap = 2.2f;
                    hud.SetStatus("MISS 0/" + MissLimit);
                    hud.Hint("CLICK WHEN THE RING\nCLOSES ON THE TARGET", 4f);
                }
            }
            else if (state == State.Rings)
            {
                UpdateRings(dt);
            }
            else if (state == State.Won || state == State.Lost)
            {
                if (!botMode && stateT > 6f) Restart(false);
            }

            if (botMode) Bot(dt);
        }

        void UpdateRings(float dt)
        {
            if (!ringActive)
            {
                ringGap -= dt;
                if (ringGap <= 0)
                {
                    int idx;
                    do { idx = Random.Range(0, bear.ringPoints.Length); } while (idx == lastRing);
                    lastRing = idx; ringPoint = bear.ringPoints[idx];
                    ringActive = true; ringU = 0;
                }
                hud.HideRing();
                return;
            }
            ringU += dt / ringDur;
            Vector3 wp = ringPoint.position;
            Vector3 sp = hunter.cam.WorldToScreenPoint(wp);
            Vector2 c = hud.ScreenToCanvas(sp);
            float scale = Mathf.Lerp(2.6f, 1f, ringU);
            if (sp.z < 0) c = new Vector2(0, -300);
            Vector2 half = hud.canvasRect.rect.size * 0.5f - Vector2.one * 90f;
            c = new Vector2(Mathf.Clamp(c.x, -half.x, half.x), Mathf.Clamp(c.y, -half.y, half.y));
            hud.ShowRing(c, Mathf.Max(0.5f, scale), Mathf.Clamp01(ringU * 5f) * (ringU > 1.1f ? Mathf.Clamp01((1.2f - ringU) * 10f) : 1f));
            if (ringU > 1.2f) RingMiss();
        }

        bool AimedAtRing()
        {
            if (ringPoint == null) return false;
            Vector3 sp = hunter.cam.WorldToScreenPoint(ringPoint.position);
            if (sp.z < 0) return false;
            Vector2 c = hud.ScreenToCanvas(sp);
            return c.magnitude < hud.TargetRadius * 1.9f;
        }

        void EndRing() { ringActive = false; ringGap = Mathf.Lerp(0.7f, 0.35f, hits / (float)HitsNeeded); hud.HideRing(); }

        void RingHit(bool perfect)
        {
            hits++;
            int pts = perfect ? 500 : 300;
            score += pts; hud.SetScore(score);
            Vector3 at = ringPoint.position;
            Fx.Popup(at + hunter.transform.right * 0.6f + Vector3.up * 0.3f, perfect ? "PERFECT" : "GOOD", perfect ? Hud.Gold : Hud.Cream, 0.9f);
            Fx.Popup(at + Vector3.up * 1.0f, "+" + pts, Hud.Orange, 0.8f);
            bear.Hit();
            Fx.Shake = 0.25f; hud.Flash(0.25f);
            EndRing();
            if (hits >= HitsNeeded) Win();
        }

        void RingMiss()
        {
            misses++;
            hud.SetStatus("MISS " + misses + "/" + MissLimit);
            hud.Hurt(); Fx.Shake = 0.7f;
            bear.Swipe();
            Fx.Popup(ringPoint.position + Vector3.up * 0.3f, "MISS", new Color(1f, 0.4f, 0.35f), 0.9f);
            EndRing();
            if (misses >= MissLimit) Lose();
        }

        void Win()
        {
            state = State.Won; stateT = 0;
            score += 2000; hud.SetScore(score);
            bear.SitDown();
            hud.HideRing();
            hud.SetStatus("BEAR BEATEN");
            hud.Message("NO BEEF!", "HE JUST WANTED SNACKS", 5f, Hud.Gold);
        }

        void Lose()
        {
            state = State.Lost; stateT = 0;
            bear.HugLunge();
            hud.HideRing();
            hud.Message("BIG HUG!", "HE JUST WANTED CUDDLES", 5f, Hud.Orange);
        }

        // ---------- the bot that plays it ----------
        void Bot(float dt)
        {
            if (state == State.Hunt)
            {
                Critter best = null; float bestA = 999f;
                foreach (var c in critters)
                {
                    if (c == null || c.dead) continue;
                    float a = hunter.AngleTo(c.AimPoint);
                    if (a < bestA) { bestA = a; best = c; }
                }
                if (best == null) { hunter.SetBotScope(false); return; }
                float dist = Vector3.Distance(hunter.transform.position, best.transform.position);
                bool scope = dist > 20f;
                hunter.SetBotScope(scope);
                Vector3 aim = best.AimPoint + best.transform.forward * (best.kind == CritterKind.Puffhop ? 0.1f : 0f);
                hunter.AimAt(aim, scope ? 0.34f : 0.26f);
                bool ready = !scope || hunter.ScopeAmount > 0.9f;
                if (ready && stateT > 0.9f && Time.time - lastKill > 0.9f && hunter.AngleTo(best.AimPoint) < (scope ? 0.7f : 1.4f) && hunter.CanFire) TryFire();
            }
            else if (state == State.BearIn || (state == State.Rings && !ringActive))
            {
                hunter.SetBotScope(false);
                if (bear) hunter.AimAt(bear.ringPoints[1].position + Vector3.up * 0.8f, 0.3f);
            }
            else if (state == State.Rings && ringActive)
            {
                hunter.SetBotScope(false);
                hunter.AimAt(ringPoint.position, 0.1f);
                if (hits == 2 && !botMissed && ringU > 0.4f && AimedAtRing()) { botMissed = true; TryFire(); }
                else if (ringU > 0.97f && ringU < 1.07f && AimedAtRing() && hunter.CanFire) TryFire();
            }
            else hunter.SetBotScope(false);
        }

        // ---------- handles support ----------
        public void StartDemo() { Restart(true); }
        public void StartHuman() { Restart(false); }
        public void SkipToBear() { Restart(botMode); score = 600; kills = KillsForBear - 1; }
        public string Describe() => $"state={state} score={score} kills={kills} hits={hits} misses={misses} bot={botMode}";
    }

#if UNITY_EDITOR && SATURA_SPACE
    public static class GameHandles
    {
        [SaturaHandle("Fresh run that the bot plays by itself: hunt three critters, then the bear")]
        public static void DemoRun() { Game.I.StartDemo(); }

        [SaturaHandle("Fresh run for a human player")]
        public static void StartRun() { Game.I.StartHuman(); }

        [SaturaHandle("Reads state, score, kills, ring hits and misses")]
        public static string GetState() => Game.I.Describe();

        [SaturaHandle("Waits until the run is won or lost, or the time runs out. Yields the state")]
        public static IEnumerator WaitForEnd(float seconds)
        {
            float t = 0;
            while (t < seconds && Game.I.state != Game.State.Won && Game.I.state != Game.State.Lost) { t += Time.deltaTime; yield return null; }
            yield return Game.I.Describe();
        }

        [SaturaHandle("Waits until the bear stands in front of the player. Yields the state")]
        public static IEnumerator WaitForBear(float seconds)
        {
            float t = 0;
            while (t < seconds && Game.I.state != Game.State.Rings) { t += Time.deltaTime; yield return null; }
            yield return Game.I.Describe();
        }
    }
#endif
}
