using System;
using System.Collections;
using System.Collections.Generic;
using WuxiaRoguelite.Visual;
using WuxiaRoguelite.Runtime;
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
        private Camera transitionCamera;
        private bool failed;
        private bool finished;
        private const string TransitionSceneName = "LevelTransition";
        public static readonly List<string> LastStages = new List<string>();

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
            SceneManager.sceneLoaded += instance.ActivateDestination;
            Time.timeScale = 0f;
            instance.StartCoroutine(instance.LoadScene(scene));
            return true;
        }

        // Catch failures from nested coroutines too; never leave the screen frozen after unload.
        private IEnumerator LoadScene(string scene)
        {
            LastStages.Clear();
            var stack = new Stack<IEnumerator>();
            stack.Push(Transition(scene));
            Exception failure = null;
            while (stack.Count > 0)
            {
                object next = null;
                bool moved = false;
                try { moved = stack.Peek().MoveNext(); if (moved) next = stack.Peek().Current; }
                catch (Exception error) { failure = error; break; }
                if (!moved) { stack.Pop(); continue; }
                if (next is IEnumerator nested) stack.Push(nested);
                else yield return next;
            }
            if (failure == null) yield break;
            Debug.LogException(failure);
            LevelSequence.CancelPendingRequest();
            AsyncOperation recovery = TryLoad(LevelSequence.MenuSceneName);
            if (recovery != null)
            {
                yield return recovery;
                var menu = SceneManager.GetSceneByName(LevelSequence.MenuSceneName);
                if (menu.IsValid() && menu.isLoaded)
                {
                    SceneManager.SetActiveScene(menu);
                    yield return RemoveTransitionScene();
                    Finish();
                    yield break;
                }
            }
            failed = true;
            destinationTitle = "关卡加载失败，请重试。";
            Time.timeScale = previousTimeScale;
        }

        private IEnumerator Transition(string scene)
        {
            yield return null; // Draw the overlay before asset work starts.
            float startedAt = Time.realtimeSinceStartup;
            Stage("before-unload");
            var previousScenes = new List<Scene>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene loaded = SceneManager.GetSceneAt(i);
                if (loaded.name != TransitionSceneName) previousScenes.Add(loaded);
            }
            Scene transit = SceneManager.GetSceneByName(TransitionSceneName);
            if (!transit.IsValid()) transit = SceneManager.CreateScene(TransitionSceneName);
            SceneManager.SetActiveScene(transit);
            var cameraObject = new GameObject("Transition Camera");
            cameraObject.transform.SetParent(transform);
            transitionCamera = cameraObject.AddComponent<Camera>();
            transitionCamera.clearFlags = CameraClearFlags.SolidColor;
            transitionCamera.backgroundColor = WuxiaUiTheme.BackgroundInk;
            transitionCamera.cullingMask = 0;
            transitionCamera.depth = 100;
            foreach (Scene old in previousScenes)
            {
                if (!old.IsValid() || !old.isLoaded) continue;
                AsyncOperation unload = SceneManager.UnloadSceneAsync(old);
                if (unload != null) yield return unload;
            }
            Time.timeScale = 0f; // Old settings panels can restore their previous scale during OnDisable.
            // All scene-owned references are now gone. Shared UI/fonts remain intentionally resident.
            MartialArtIconRenderer.ClearCache();
            if (scene == LevelSequence.MenuSceneName)
            {
                HeroDirectionalArt.ReleaseCache();
                HeroAttackArt.ReleaseCache();
            }
            yield return null; // Complete deferred Destroy calls before the resource sweep.
            yield return Resources.UnloadUnusedAssets();
            Progress = 0.2f;
            Stage("old-scenes-released");
            AsyncOperation operation = TryLoad(scene);
            if (operation == null) throw new InvalidOperationException("Scene load did not start: " + scene);
            // Activation is allowed immediately. The overlay, not an old level, owns the minimum delay.
            while (!operation.isDone)
            {
                Progress = Mathf.Max(Progress, 0.2f + operation.progress * 0.7f);
                yield return null;
            }
            Scene destination = SceneManager.GetSceneByName(scene);
            if (!destination.IsValid() || !destination.isLoaded)
                throw new InvalidOperationException("Destination scene was not loaded: " + scene);
            SceneManager.SetActiveScene(destination);
            yield return null; // Destination Start consumes its one-shot intent before reveal.
            // Main actors are shared and frequently used. Prepare them before gameplay time starts.
            if (scene != LevelSequence.MenuSceneName) { _ = HeroDirectionalArt.Available; }
            Stage("destination-ready");
            yield return RemoveTransitionScene();
            while (Time.realtimeSinceStartup - startedAt < MinimumDuration)
            {
                Progress = Mathf.Max(Progress, Mathf.Lerp(0.9f, 0.99f,
                    (Time.realtimeSinceStartup - startedAt) / MinimumDuration));
                yield return null;
            }
            Progress = 1f;
            yield return new WaitForSecondsRealtime(0.15f);
            Finish();
        }

        private static AsyncOperation TryLoad(string scene)
        {
            try
            {
                // Additive keeps the tiny transition scene alive while the old gameplay scene is absent.
                return SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
            }
            catch (Exception error) { Debug.LogException(error); return null; }
        }

        private void ActivateDestination(Scene loaded, LoadSceneMode mode)
        {
            // sceneLoaded runs before Start. LevelSequence reads the active scene in Start.
            if (loaded.name != TransitionSceneName) SceneManager.SetActiveScene(loaded);
            Time.timeScale = 0f;
        }

        private void Update()
        {
            if (!failed && !finished) Time.timeScale = 0f;
        }

        private static IEnumerator RemoveTransitionScene()
        {
            Scene transit = SceneManager.GetSceneByName(TransitionSceneName);
            if (transit.IsValid() && transit.isLoaded)
                yield return SceneManager.UnloadSceneAsync(transit);
        }

        private static void Stage(string stage)
        {
            LastStages.Add(stage);
            WebMemoryDiagnostics.Capture(stage);
        }

        private void Finish()
        {
            finished = true;
            if (transitionCamera != null) transitionCamera.enabled = false;
            Time.timeScale = 1f;
            Stage("revealed");
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (instance != this) return;
            SceneManager.sceneLoaded -= ActivateDestination;
            if (!finished) Time.timeScale = previousTimeScale;
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
                if (failed)
                {
                    if (GUI.Button(new Rect(panel.x + 24, panel.y + 136, width - 48, 48),
                        "返回主页", WuxiaUiComponents.TouchButton()))
                    {
                        failed = false;
                        Time.timeScale = 0f;
                        StartCoroutine(LoadScene(LevelSequence.MenuSceneName));
                    }
                    return;
                }
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
