using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace GrizzleValley
{
    /// <summary>
    /// The first-person hunter: mouse look, a bolt rifle in the lower right with sway and recoil,
    /// a scope on the right mouse button and hitscan shots with a tracer.
    /// </summary>
    public class Hunter : MonoBehaviour
    {
        public Camera cam;
        public Transform gun, muzzle;
        public float yaw, pitch;
        public bool scoped;
        public bool bot;
        public bool lookLocked;
        public float fireCooldown = 0.42f;
        public const float BaseFov = 68f, ScopeFov = 24f;

        float lastShot = -10f;
        Vector3 gunRest = new Vector3(0.24f, -0.23f, 0.4f);
        Quaternion gunRestRot = Quaternion.Euler(-2f, -1f, 0);
        Vector3 recoilPos; float recoilRot;
        Vector2 sway;
        float yawVel, pitchVel;
        float scopeT;
        bool botScope;

        public static Hunter Create(Transform parent)
        {
            var go = new GameObject("Hunter");
            go.transform.SetParent(parent, false);
            var h = go.AddComponent<Hunter>();
            h.Build();
            return h;
        }

        void Build()
        {
            transform.position = new Vector3(0, World.Height(0, 0) + 1.65f, 0);
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(transform, false);
            cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = BaseFov;
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 700f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = World.SkyHorizon;
            camGo.AddComponent<AudioListener>();
            var data = camGo.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

            gun = new GameObject("Rifle").transform;
            gun.SetParent(camGo.transform, false);
            gun.localPosition = gunRest;
            gun.localRotation = gunRestRot;
            Color wood = LP.Hex("#9A5B2E"), woodDark = LP.Hex("#6E3F1F"), metal = LP.Hex("#2B2F36"), metalLight = LP.Hex("#4A5059");
            var b = new MeshBuilder();
            // Stock and fore-end
            b.Box(LP.TRS(new Vector3(0, -0.035f, -0.2f), new Vector3(8f, 0, 0), new Vector3(0.05f, 0.075f, 0.26f)), wood);
            b.Box(LP.TRS(new Vector3(0, -0.07f, -0.3f), new Vector3(-6f, 0, 0), new Vector3(0.048f, 0.1f, 0.12f)), woodDark);
            b.Box(LP.TRS(new Vector3(0, -0.012f, 0.13f), Vector3.zero, new Vector3(0.046f, 0.05f, 0.34f)), wood);
            b.Box(LP.TRS(new Vector3(0, -0.05f, -0.07f), new Vector3(-20f, 0, 0), new Vector3(0.04f, 0.08f, 0.05f)), woodDark);
            // Receiver, barrel, bolt
            b.Box(LP.TRS(new Vector3(0, 0.02f, -0.02f), Vector3.zero, new Vector3(0.045f, 0.045f, 0.2f)), metal);
            b.Box(LP.TRS(new Vector3(0, 0.022f, 0.36f), Vector3.zero, new Vector3(0.022f, 0.022f, 0.6f)), metal);
            b.Box(LP.TRS(new Vector3(0, 0.022f, 0.66f), Vector3.zero, new Vector3(0.03f, 0.03f, 0.04f)), metalLight);
            b.Box(LP.TRS(new Vector3(0.04f, 0.025f, -0.06f), new Vector3(0, 0, -30f), new Vector3(0.05f, 0.012f, 0.012f)), metalLight);
            b.Ico(LP.TRS(new Vector3(0.065f, 0.012f, -0.06f), Vector3.zero, Vector3.one * 0.022f), metalLight, 0);
            // Scope
            b.Cone(LP.TRS(new Vector3(0, 0.07f, -0.1f), new Vector3(90f, 0, 0), Vector3.one), metal, 8, 0.024f, 0.024f, 0.26f);
            b.Cone(LP.TRS(new Vector3(0, 0.07f, 0.16f), new Vector3(90f, 0, 0), Vector3.one), metal, 8, 0.024f, 0.032f, 0.06f);
            b.Cone(LP.TRS(new Vector3(0, 0.07f, -0.14f), new Vector3(90f, 0, 0), Vector3.one), metal, 8, 0.03f, 0.024f, 0.05f);
            b.Box(LP.TRS(new Vector3(0, 0.045f, -0.04f), Vector3.zero, new Vector3(0.02f, 0.03f, 0.02f)), metalLight);
            b.Box(LP.TRS(new Vector3(0, 0.045f, 0.08f), Vector3.zero, new Vector3(0.02f, 0.03f, 0.02f)), metalLight);
            var mesh = b.Build("Rifle");
            var model = LP.MeshObject("Model", gun, mesh, LP.Lit, false);
            model.layer = 2;
            muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(gun, false);
            muzzle.localPosition = new Vector3(0, 0.022f, 0.69f);

            Fx.Root = transform.parent;
        }

        public void ResetLook()
        {
            yaw = 0; pitch = 2f; yawVel = pitchVel = 0;
            scoped = false; botScope = false; scopeT = 0;
            lookLocked = false;
            Apply();
        }

        void Update()
        {
            var mouse = Mouse.current;
            if (!bot && !lookLocked && mouse != null)
            {
                Vector2 d = mouse.delta.ReadValue() * 0.07f * (cam.fieldOfView / BaseFov);
                yaw += d.x; pitch -= d.y;
                pitch = Mathf.Clamp(pitch, -35f, 50f);
                sway = Vector2.Lerp(sway, d, Time.deltaTime * 10f);
            }
            else sway = Vector2.Lerp(sway, Vector2.zero, Time.deltaTime * 6f);

            bool wantScope = bot ? botScope : (mouse != null && mouse.rightButton.isPressed);
            if (Game.I != null && !Game.I.CanScope) wantScope = false;
            scoped = wantScope;
            scopeT = Mathf.MoveTowards(scopeT, scoped ? 1f : 0f, Time.deltaTime * 6f);

            if (!bot && mouse != null && mouse.leftButton.wasPressedThisFrame) Game.I?.OnFirePressed();

            Apply();
        }

        void Apply()
        {
            transform.rotation = Quaternion.Euler(pitch, yaw, 0);
            float shake = Fx.Shake;
            Fx.Shake = Mathf.MoveTowards(Fx.Shake, 0, Time.deltaTime * 2.2f);
            Vector3 sh = new Vector3(Mathf.PerlinNoise(Time.time * 25f, 1f) - 0.5f, Mathf.PerlinNoise(2f, Time.time * 25f) - 0.5f, 0) * shake * 0.25f;
            float breathe = Mathf.Sin(Time.time * 1.3f) * 0.004f;
            cam.transform.localPosition = sh + new Vector3(0, breathe, 0);
            cam.transform.localRotation = Quaternion.Euler(-recoilRot * 0.35f + sh.y * 6f, sh.x * 6f, 0);
            cam.fieldOfView = Mathf.Lerp(BaseFov, ScopeFov, Mathf.SmoothStep(0, 1, scopeT));

            // Recoil recovers by lerping back to rest every frame (as in llamacademy's gun system, MIT).
            recoilPos = Vector3.Lerp(recoilPos, Vector3.zero, Time.deltaTime * 9f);
            recoilRot = Mathf.Lerp(recoilRot, 0, Time.deltaTime * 9f);
            Vector3 swayOff = new Vector3(-sway.x, -sway.y, 0) * 0.004f;
            Vector3 lowered = new Vector3(0, -0.25f, -0.1f) * Mathf.SmoothStep(0, 1, scopeT);
            gun.localPosition = gunRest + recoilPos + swayOff + lowered + new Vector3(0, breathe * 1.5f, 0);
            gun.localRotation = gunRestRot * Quaternion.Euler(-recoilRot, sway.x * 0.6f, -sway.x * 0.8f);
            gun.gameObject.SetActive(scopeT < 0.85f);
        }

        public bool CanFire => Time.time - lastShot >= fireCooldown;
        public float ScopeAmount => scopeT;

        /// <summary>Fires along the view. Returns the hit (collider may be null).</summary>
        public RaycastHit Fire(out bool fired)
        {
            fired = false;
            var hit = new RaycastHit();
            if (!CanFire) return hit;
            fired = true;
            lastShot = Time.time;
            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            Vector3 end;
            if (Physics.Raycast(ray, out hit, 400f, ~(1 << 2), QueryTriggerInteraction.Ignore)) end = hit.point;
            else end = ray.origin + ray.direction * 300f;
            Vector3 from = scopeT > 0.5f ? cam.transform.position + cam.transform.forward * 0.5f - cam.transform.up * 0.15f : muzzle.position;
            Fx.Tracer(this, from, end);
            if (scopeT < 0.5f) Fx.MuzzleFlash(muzzle);
            recoilPos += new Vector3(0, 0.01f, -0.09f);
            recoilRot += 14f;
            Fx.Shake = Mathf.Max(Fx.Shake, 0.35f);
            Hud.I?.Flash(scopeT > 0.5f ? 0.15f : 0.35f);
            if (hit.collider == null) { }
            else if (hit.collider.GetComponentInParent<Critter>() == null && hit.collider.GetComponentInParent<Bear>() == null) Fx.Dust(hit.point);
            return hit;
        }

        // --- Bot helpers ---
        public float AngleTo(Vector3 world)
        {
            Vector3 d = world - cam.transform.position;
            return Vector3.Angle(transform.forward, d);
        }

        public void AimAt(Vector3 world, float smooth)
        {
            Vector3 d = world - transform.position;
            float ty = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            float tp = -Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg;
            yaw = Mathf.SmoothDampAngle(yaw, ty, ref yawVel, smooth);
            pitch = Mathf.SmoothDampAngle(pitch, tp, ref pitchVel, smooth);
        }

        public void SetBotScope(bool on) { botScope = on; }
    }
}
