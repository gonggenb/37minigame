using UnityEngine;
using WuxiaRoguelite.Runtime;

namespace WuxiaRoguelite.UI
{
    /// <summary>Once per application session; independent of level selection and gameplay clocks.</summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class StudioSplashScreen : MonoBehaviour
    {
        public const float FadeInDuration = .55f;
        public const float HoldDuration = 1.8f;
        public const float FadeOutDuration = .45f;
        public const float Duration = FadeInDuration + HoldDuration + FadeOutDuration;
        public const string LogoResource = "UI/Branding/logo_hakimi_group_v01";
        public static bool HasPresentedThisSession { get; private set; }
        public static bool IsBlocking => instance != null || Time.frameCount <= releaseFrame;
        public float Elapsed => firstPaintTime < 0 ? 0 : Time.realtimeSinceStartup - firstPaintTime;
        private static StudioSplashScreen instance;
        private static int releaseFrame = -1;
        private Texture2D logo;
        private float firstPaintTime = -1;
        private float skipTime = -1;
        private float previousTimeScale;
        private bool previousAudioPause;
        private bool restored;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            instance = null;
            releaseFrame = -1;
            HasPresentedThisSession = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void PresentAtStartup()
        {
            if (HasPresentedThisSession || Application.isBatchMode) return;
            HasPresentedThisSession = true;
            instance = new GameObject("Hakimi Group Studio Splash").AddComponent<StudioSplashScreen>();
            DontDestroyOnLoad(instance.gameObject);
        }

        private void Awake()
        {
            logo = Resources.Load<Texture2D>(LogoResource);
            previousTimeScale = Time.timeScale;
            previousAudioPause = AudioListener.pause;
            Time.timeScale = 0;
            AudioListener.pause = true;
        }

        public void Skip()
        {
            // Ignore the click that launched/focused the application; never forward a skip to the menu.
            if (Elapsed >= .35f && skipTime < 0) skipTime = Elapsed;
        }

        private float FadeOut => Mathf.SmoothStep(1, 0,
            (Elapsed - (skipTime >= 0 ? skipTime : FadeInDuration + HoldDuration)) / FadeOutDuration);

        private void Update()
        {
            if (firstPaintTime < 0) return;
            if (Input.anyKeyDown || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)) Skip();
            float finishAt = skipTime >= 0 ? skipTime + FadeOutDuration : Duration;
            if (Elapsed < finishAt) return;
            releaseFrame = Time.frameCount + 1;
            Restore();
            Destroy(gameObject);
        }

        private void Restore()
        {
            if (restored) return;
            restored = true;
            Time.timeScale = previousTimeScale;
            AudioListener.pause = previousAudioPause;
        }

        private void OnDestroy()
        {
            Restore();
            if (instance == this) instance = null;
        }

        private void OnGUI()
        {
            RuntimeChineseFont.PrepareSkin();
            if (firstPaintTime < 0 && Event.current.type == EventType.Repaint)
                firstPaintTime = Time.realtimeSinceStartup;
            Color color = GUI.color;
            Matrix4x4 matrix = ResponsiveGui.ApplyScale(ResponsiveGui.Scale);
            GUI.depth = -20000;
            try
            {
                float fade = FadeOut;
                Color ink = WuxiaUiTheme.BackgroundInk;
                ink.a = fade;
                WuxiaUiTheme.FillRect(new Rect(0, 0, ResponsiveGui.Width, ResponsiveGui.Height), ink);
                float opacity = Mathf.SmoothStep(0, 1, Elapsed / FadeInDuration) * fade;
                GUI.color = new Color(1, 1, 1, opacity);
                Rect safe = ResponsiveGui.SafeArea;
                float size = Mathf.Min(ResponsiveGui.IsPortrait ? 280 : 240, Mathf.Min(safe.width - 64, safe.height - 190));
                float top = safe.center.y - (size + 90) * .5f - 10;
                if (logo != null)
                    GUI.DrawTexture(new Rect(safe.center.x - size * .5f, top, size, size), logo, ScaleMode.ScaleToFit, true);
                WuxiaUiComponents.Text(new Rect(safe.x + 24, top + size + 20, safe.width - 48, 44),
                    GameTextCatalog.StudioPresentation, 28, WuxiaUiTheme.TextPrimary, TextAnchor.MiddleCenter);
                GUI.color = new Color(1, 1, 1, Mathf.Clamp01((Elapsed - .55f) / .3f) * fade);
                WuxiaUiComponents.Text(new Rect(safe.x + 24, safe.yMax - 52, safe.width - 48, 28),
                    "点击屏幕或按任意键跳过", 14, WuxiaUiTheme.TextSecondary, TextAnchor.MiddleCenter);
                if (Event.current.isMouse || Event.current.isKey || Event.current.type == EventType.ScrollWheel)
                    Event.current.Use();
            }
            finally
            {
                GUI.color = color;
                GUI.matrix = matrix;
            }
        }
    }
}
