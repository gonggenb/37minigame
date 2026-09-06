using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using WuxiaRoguelite.GameFlow;

namespace WuxiaRoguelite.UI
{
    /// <summary>Shared cross-scene overlay. Presentation takes at least five real seconds.</summary>
    public sealed class LevelLoadingScreen : MonoBehaviour
    {
        public const float MinimumDuration = 5f;
        public static bool IsLoading => instance != null;
        public float Progress { get; private set; }
        private static LevelLoadingScreen instance;
        private string destinationTitle;
        private string subtitle;
        private float previousTimeScale;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private Texture2D background;

        public static bool Load(string scene, string title, string subtitle = null)
        {
            if (IsLoading) return false;
            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                Debug.LogError($"Cannot load configured level: {scene}");
                if (GameFlowController.Instance != null)
                    GameFlowController.Instance.statusMessage = "关卡加载失败，请重试。";
                return false;
            }

            instance = new GameObject("LevelLoadingScreen").AddComponent<LevelLoadingScreen>();
            DontDestroyOnLoad(instance.gameObject);
            instance.destinationTitle = title;
            instance.subtitle = subtitle;
            instance.previousTimeScale = Time.timeScale;
            instance.background = Resources.Load<Texture2D>("UI/MainMenu/bg_mainmenu_mountain_pass_v02");
            Time.timeScale = 0f;
            instance.StartCoroutine(instance.LoadScene(scene));
            return true;
        }

        private IEnumerator LoadScene(string scene)
        {
            // Let the overlay paint before any asset work begins (also works in WebGL).
            yield return null;
            float startedAt = Time.realtimeSinceStartup;
            AsyncOperation operation = null;
            try
            {
                operation = SceneManager.LoadSceneAsync(scene);
                if (operation == null) throw new InvalidOperationException("Scene load did not start.");
                operation.allowSceneActivation = false;
            }
            catch (Exception error)
            {
                Debug.LogException(error);
            }
            if (operation == null)
            {
                if (GameFlowController.Instance != null)
                    GameFlowController.Instance.statusMessage = "关卡加载失败，请重试。";
                Time.timeScale = previousTimeScale;
                Destroy(gameObject);
                yield break;
            }

            while (Time.realtimeSinceStartup - startedAt < MinimumDuration || operation.progress < 0.9f)
            {
                float presentation = Mathf.Clamp01((Time.realtimeSinceStartup - startedAt) / MinimumDuration);
                Progress = Mathf.Max(Progress, Mathf.Min(presentation, operation.progress / 0.9f) * 0.98f);
                yield return null;
            }

            Progress = 0.99f;
            operation.allowSceneActivation = true;
            yield return operation;
            // Start consumes the one-shot destination request before we reveal the scene.
            yield return null;
            Progress = 1f;
            yield return new WaitForSecondsRealtime(0.15f);
            Time.timeScale = 1f;
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (instance != this) return;
            instance = null;
        }

        private void OnGUI()
        {
            RuntimeChineseFont.PrepareSkin();
            int oldDepth = GUI.depth;
            GUI.depth = -10000;
            Matrix4x4 oldMatrix = ResponsiveGui.ApplyScale(ResponsiveGui.Scale);
            try
            {
                Rect screen = new Rect(0, 0, ResponsiveGui.Width, ResponsiveGui.Height);
                Color ink = WuxiaUiTheme.BackgroundInk;
                ink.a = 1f;
                WuxiaUiTheme.FillRect(screen, ink);
                if (background != null)
                {
                    GUI.DrawTexture(screen, background, ScaleMode.ScaleAndCrop);
                    WuxiaUiTheme.FillRect(screen, WuxiaUiTheme.BackgroundInk * new Color(1, 1, 1, 0.75f));
                }
                EnsureStyles();
                Rect safe = ResponsiveGui.SafeArea;
                float width = Mathf.Min(520f, safe.width - 32f);
                Rect panel = new Rect(safe.center.x - width / 2f, safe.center.y - 132f, width, 264f);
                WuxiaUiTheme.DrawPanel(panel, WuxiaUiTheme.BackgroundBrown, WuxiaUiTheme.Brass);
                GUI.Label(new Rect(panel.x + 24, panel.y + 24, width - 48, 44), destinationTitle, titleStyle);
                GUI.Label(new Rect(panel.x + 24, panel.y + 78, width - 48, 30),
                    string.IsNullOrEmpty(subtitle) ? "正在前往……" : subtitle, bodyStyle);
                Rect track = new Rect(panel.x + 24, panel.y + 136, width - 48, 20);
                WuxiaUiTheme.DrawCompactSurface(track, WuxiaUiTheme.SurfaceIron, WuxiaUiTheme.Brass);
                WuxiaUiTheme.FillRect(new Rect(track.x + 4, track.y + 4,
                    (track.width - 8) * Progress, track.height - 8), WuxiaUiTheme.Brass);
                GUI.Label(new Rect(panel.x + 24, panel.y + 170, width - 48, 32),
                    $"{Mathf.FloorToInt(Progress * 100f)}%", titleStyle);
                GUI.Label(new Rect(panel.x + 24, panel.y + 216, width - 48, 26),
                    Progress >= 1f ? "准备就绪" : "正在加载……", bodyStyle);
                if (Event.current.isMouse || Event.current.isKey || Event.current.type == EventType.ScrollWheel)
                    Event.current.Use();
            }
            finally
            {
                GUI.matrix = oldMatrix;
                GUI.depth = oldDepth;
            }
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = RuntimeChineseFont.Apply(new GUIStyle(GUI.skin.label)
            {
                fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter
            });
            titleStyle.normal.textColor = WuxiaUiTheme.TextPrimary;
            bodyStyle = RuntimeChineseFont.Apply(new GUIStyle(GUI.skin.label)
            {
                fontSize = 16, alignment = TextAnchor.MiddleCenter
            });
            bodyStyle.normal.textColor = WuxiaUiTheme.TextSecondary;
        }
    }
}
