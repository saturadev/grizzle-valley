using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GrizzleValley
{
    /// <summary>Screen UI: score, status, crosshair, scope mask, flashes, messages and the bear's timing rings.</summary>
    public class Hud : MonoBehaviour
    {
        public static Hud I;
        public static readonly Color Gold = new Color(1f, 0.84f, 0.2f);
        public static readonly Color Orange = new Color(1f, 0.55f, 0.1f);
        public static readonly Color Cream = new Color(1f, 0.97f, 0.9f);

        public RectTransform canvasRect;
        TextMeshProUGUI score, scoreLabel, status, message, sub, hint;
        Image flash, hurt, crossDot, crossRing;
        GameObject scope;
        RectTransform ringTarget, ringOuter;
        Image ringOuterImg;
        float flashA, hurtA, msgT, msgLife, hintT;
        static Sprite circle, ringSprite, scopeSprite, scratchSprite;

        public static Hud Create(Transform parent)
        {
            var go = new GameObject("HUD");
            go.transform.SetParent(parent, false);
            var h = go.AddComponent<Hud>();
            h.Build();
            I = h;
            return h;
        }

        void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasRect = (RectTransform)transform;
            MakeSprites();

            // Scope mask: a round window with crosshairs, black around it.
            scope = new GameObject("Scope", typeof(RectTransform));
            scope.transform.SetParent(transform, false);
            var srt = (RectTransform)scope.transform; Stretch(srt);
            var win = Img("Window", srt, scopeSprite, Color.white);
            win.rectTransform.sizeDelta = new Vector2(1000, 1000);
            var top = Img("Top", srt, null, Color.black); top.rectTransform.anchorMin = new Vector2(0, 0.5f); top.rectTransform.anchorMax = new Vector2(1, 1); top.rectTransform.offsetMin = new Vector2(0, 498); top.rectTransform.offsetMax = Vector2.zero;
            var bot = Img("Bottom", srt, null, Color.black); bot.rectTransform.anchorMin = new Vector2(0, 0); bot.rectTransform.anchorMax = new Vector2(1, 0.5f); bot.rectTransform.offsetMin = Vector2.zero; bot.rectTransform.offsetMax = new Vector2(0, -498);
            var left = Img("Left", srt, null, Color.black); left.rectTransform.anchorMin = new Vector2(0, 0); left.rectTransform.anchorMax = new Vector2(0.5f, 1); left.rectTransform.offsetMin = Vector2.zero; left.rectTransform.offsetMax = new Vector2(-498, 0);
            var right = Img("Right", srt, null, Color.black); right.rectTransform.anchorMin = new Vector2(0.5f, 0); right.rectTransform.anchorMax = new Vector2(1, 1); right.rectTransform.offsetMin = new Vector2(498, 0); right.rectTransform.offsetMax = Vector2.zero;
            scope.SetActive(false);

            crossRing = Img("CrossRing", (RectTransform)transform, ringSprite, new Color(1, 1, 1, 0.85f)); crossRing.rectTransform.sizeDelta = new Vector2(46, 46);
            crossDot = Img("CrossDot", (RectTransform)transform, circle, Color.white); crossDot.rectTransform.sizeDelta = new Vector2(10, 10);

            // Timing ring for the bear.
            ringOuter = Img("RingOuter", (RectTransform)transform, ringSprite, Color.white).rectTransform;
            ringOuterImg = ringOuter.GetComponent<Image>();
            ringOuter.sizeDelta = new Vector2(150, 150);
            ringTarget = Img("RingTarget", (RectTransform)transform, circle, Orange).rectTransform;
            ringTarget.sizeDelta = new Vector2(118, 118);
            var inner = Img("Dot", ringTarget, circle, Color.white); inner.rectTransform.sizeDelta = new Vector2(16, 16);
            var innerDark = Img("DotCore", inner.rectTransform, circle, new Color(0.15f, 0.1f, 0.05f)); innerDark.rectTransform.sizeDelta = new Vector2(7, 7);
            HideRing();

            hurt = Img("Hurt", (RectTransform)transform, null, new Color(0.55f, 0.12f, 0.08f, 0)); Stretch(hurt.rectTransform);
            for (int k = -1; k <= 1; k++)
            {
                var sc = Img("Scratch", hurt.rectTransform, scratchSprite, new Color(1, 0.9f, 0.85f, 1));
                sc.rectTransform.sizeDelta = new Vector2(90, 1300);
                sc.rectTransform.anchoredPosition = new Vector2(k * 150, 0);
                sc.rectTransform.localRotation = Quaternion.Euler(0, 0, 25f);
            }
            flash = Img("Flash", (RectTransform)transform, null, new Color(1f, 0.93f, 0.6f, 0)); Stretch(flash.rectTransform);

            scoreLabel = Text("ScoreLabel", 44, Cream, new Vector2(0.5f, 1), new Vector2(0, -150));
            score = Text("Score", 120, Gold, new Vector2(0.5f, 1), new Vector2(0, -235));
            status = Text("Status", 56, Cream, new Vector2(0.5f, 1), new Vector2(0, -335));
            message = Text("Message", 104, Cream, new Vector2(0.5f, 0.5f), new Vector2(0, 420));
            sub = Text("Sub", 58, Gold, new Vector2(0.5f, 0.5f), new Vector2(0, 310));
            hint = Text("Hint", 42, Cream, new Vector2(0.5f, 0), new Vector2(0, 260));
            scoreLabel.text = "SCORE";
            message.text = ""; sub.text = ""; hint.text = "";
        }

        static void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }

        Image Img(string name, RectTransform parent, Sprite sprite, Color c)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite; img.color = c; img.raycastTarget = false;
            return img;
        }

        TextMeshProUGUI Text(string name, float size, Color c, Vector2 anchor, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.font = Game.Font;
            t.fontSharedMaterial = Game.FontOutline;
            t.fontSize = size;
            t.color = c;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(940, size * 2.6f);
            rt.anchoredPosition = pos;
            return t;
        }

        static void MakeSprites()
        {
            if (circle != null) return;
            circle = MakeSprite(256, (x, y) => Mathf.Clamp01((0.5f - Mathf.Sqrt(x * x + y * y)) * 256f + 0.5f));
            ringSprite = MakeSprite(256, (x, y) => { float r = Mathf.Sqrt(x * x + y * y); return Mathf.Clamp01((0.06f - Mathf.Abs(r - 0.44f)) * 256f + 0.5f); });
            scratchSprite = MakeSprite(128, (x, y) => Mathf.Clamp01((0.12f - Mathf.Abs(x) - Mathf.Abs(y) * 0.2f) * 60f));
            // Scope window: black outside the circle, a soft dark rim, thin crosshairs with thick outer posts.
            const int n = 512;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Scope" };
            var px = new Color32[n * n];
            for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
            {
                float x = (i + 0.5f) / n - 0.5f, y = (j + 0.5f) / n - 0.5f;
                float r = Mathf.Sqrt(x * x + y * y);
                float a = Mathf.Clamp01((r - 0.47f) * n + 0.5f);          // outside: black
                a = Mathf.Max(a, Mathf.Clamp01((r - 0.38f) / 0.09f) * 0.55f); // vignette rim
                float line = 0;
                float thin = 1.2f / n, thick = 5f / n;
                if (Mathf.Abs(x) < thin || Mathf.Abs(y) < thin) line = 1;
                if ((Mathf.Abs(x) < thick && Mathf.Abs(y) > 0.12f) || (Mathf.Abs(y) < thick && Mathf.Abs(x) > 0.12f)) line = 1;
                a = Mathf.Max(a, line * 0.9f);
                px[j * n + i] = new Color32(0, 0, 0, (byte)(a * 255));
            }
            tex.SetPixels32(px); tex.Apply();
            scopeSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        }

        static Sprite MakeSprite(int n, System.Func<float, float, float> alpha)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
            {
                float x = (i + 0.5f) / n - 0.5f, y = (j + 0.5f) / n - 0.5f;
                px[j * n + i] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alpha(x, y)) * 255));
            }
            tex.SetPixels32(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        }

        public void SetScore(int s) { score.text = s.ToString("N0"); }
        public void SetStatus(string s) { status.text = s; }
        public void Flash(float a) { flashA = Mathf.Max(flashA, a); }
        public void Hurt() { hurtA = 1f; }
        public void Hint(string s, float seconds) { hint.text = s; hintT = seconds; hint.alpha = 1; }

        public void Message(string main, string subText, float seconds, Color? color = null)
        {
            message.text = main; sub.text = subText ?? "";
            message.color = color ?? Cream;
            msgT = 0; msgLife = seconds;
        }

        public void ShowScope(float amount) { scope.SetActive(amount > 0.85f); crossDot.enabled = crossRing.enabled = amount < 0.5f; }

        public Vector2 ScreenToCanvas(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out var local);
            return local;
        }

        public void ShowRing(Vector2 canvasPos, float ringScale, float alpha)
        {
            ringTarget.gameObject.SetActive(true); ringOuter.gameObject.SetActive(true);
            ringTarget.anchoredPosition = canvasPos; ringOuter.anchoredPosition = canvasPos;
            ringOuter.localScale = Vector3.one * ringScale;
            ringTarget.localScale = Vector3.one * (1f + Mathf.Max(0, 1.05f - ringScale) * 0.6f);
            ringOuterImg.color = new Color(1, 1, 1, alpha);
        }

        public void HideRing() { ringTarget.gameObject.SetActive(false); ringOuter.gameObject.SetActive(false); }

        public float TargetRadius => 59f;

        void Update()
        {
            float dt = Time.deltaTime;
            flashA = Mathf.MoveTowards(flashA, 0, dt * 3.5f);
            flash.color = new Color(1f, 0.93f, 0.6f, flashA);
            hurtA = Mathf.MoveTowards(hurtA, 0, dt * 1.6f);
            hurt.color = new Color(0.55f, 0.12f, 0.08f, hurtA * 0.45f);
            foreach (Transform c in hurt.transform) c.GetComponent<Image>().color = new Color(1, 0.92f, 0.88f, hurtA * 0.9f);

            if (msgLife > 0)
            {
                msgT += dt;
                float pop = msgT < 0.15f ? Mathf.Lerp(0.5f, 1.1f, msgT / 0.15f) : Mathf.Lerp(1.1f, 1f, Mathf.Clamp01((msgT - 0.15f) / 0.15f));
                message.rectTransform.localScale = Vector3.one * pop;
                float a = msgT > msgLife ? Mathf.Clamp01(1f - (msgT - msgLife) / 0.3f) : 1f;
                message.alpha = a; sub.alpha = a;
                if (msgT > msgLife + 0.3f) { message.text = ""; sub.text = ""; msgLife = 0; }
            }
            if (hintT > 0)
            {
                hintT -= dt;
                hint.alpha = Mathf.Clamp01(hintT / 0.4f);
                if (hintT <= 0) hint.text = "";
            }
        }
    }
}
