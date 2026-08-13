using UnityEngine;
using WuxiaRoguelite.Battle;
using WuxiaRoguelite.Cave;
using WuxiaRoguelite.GameFlow;

namespace WuxiaRoguelite.UI
{
    /// <summary>
    /// Stores the player's preferred mobile orientation and applies it before the scene starts.
    /// The actual responsive layout always follows the current screen aspect ratio.
    /// </summary>
    public static class MobileDisplaySettings
    {
        private const string OrientationPreference = "Settings.PortraitOrientation";

        public static bool PrefersPortrait =>
            PlayerPrefs.GetInt(OrientationPreference, Screen.height >= Screen.width ? 1 : 0) == 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplySavedOrientation()
        {
            ApplyOrientation(PlayerPrefs.GetInt(OrientationPreference, 1) == 1, false);
        }

        public static void SetPortrait(bool portrait)
        {
            ApplyOrientation(portrait, true);
        }

        private static void ApplyOrientation(bool portrait, bool save)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // Browser layout follows the responsive canvas. Calling Screen.orientation
            // on desktop WebGL invokes screen.orientation.lock(), which is unavailable
            // in common desktop browsers and produces a rejected promise.
            if (save)
            {
                PlayerPrefs.SetInt(OrientationPreference, portrait ? 1 : 0);
                PlayerPrefs.Save();
            }
            return;
#else
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.orientation = portrait
                ? ScreenOrientation.Portrait
                : ScreenOrientation.LandscapeLeft;

            if (!save)
            {
                return;
            }

            PlayerPrefs.SetInt(OrientationPreference, portrait ? 1 : 0);
            PlayerPrefs.Save();
#endif
        }
    }

    /// <summary>
    /// Touch-first movement for the main map and cave exploration.
    /// Portrait uses an invisible swipe gesture, while landscape keeps the virtual joystick.
    /// Keyboard input remains available and is combined by the gameplay controllers.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1200)]
    public class MobileInputController : MonoBehaviour
    {
        public GameFlowController gameFlow;
        public BattleManager battleManager;
        [Tooltip("Shows and enables mouse-driven joystick preview while testing in the Unity Editor.")]
        public bool showInEditor = true;
        [Tooltip("Shows the joystick in touch-capable WebGL and desktop builds.")]
        public bool showOnDesktop = true;
        [Range(52f, 84f)] public float joystickRadius = 60f;
        [Header("Portrait Swipe")]
        [Range(4f, 32f)] public float portraitSwipeDeadZone = 12f;
        [Range(48f, 160f)] public float portraitSwipeFullSpeedDistance = 96f;

        public static Vector2 MoveInput { get; private set; }
        public static bool IsDragging { get; private set; }

        private int activeFingerId = -1;
        private bool mouseCaptured;
        private Vector2 centerScreen;
        private Vector2 gestureStartScreen;
        private float radiusScreen;
        private Texture2D baseTexture;
        private Texture2D knobTexture;
        private CaveRoomController caveRoom;
        private bool? appliedPortraitLayout;

        private bool ShouldShow =>
            gameFlow != null &&
            !PrototypeHUDController.IsSettingsOpen &&
            !gameFlow.IsCharacterMenuPaused &&
            (caveRoom == null || !caveRoom.IsModalUiOpen) &&
            (gameFlow.CurrentPhase == GamePhase.MainMapRunning ||
             gameFlow.CurrentPhase == GamePhase.CaveRunning) &&
            (battleManager == null || !battleManager.IsBattleActive) &&
            (Application.isMobilePlatform || Input.touchSupported || showOnDesktop ||
             (Application.isEditor && showInEditor));

        private void Awake()
        {
            if (gameFlow == null)
            {
                gameFlow = FindAnyObjectByType<GameFlowController>();
            }

            if (battleManager == null)
            {
                battleManager = FindAnyObjectByType<BattleManager>();
            }

            if (caveRoom == null)
            {
                caveRoom = FindAnyObjectByType<CaveRoomController>();
            }

            baseTexture = CreateCircleTexture(128,
                new Color(WuxiaUiTheme.BackgroundInk.r, WuxiaUiTheme.BackgroundInk.g,
                    WuxiaUiTheme.BackgroundInk.b, 0.62f),
                new Color(WuxiaUiTheme.Brass.r, WuxiaUiTheme.Brass.g, WuxiaUiTheme.Brass.b, 0.78f),
                0.82f);
            knobTexture = CreateCircleTexture(96,
                new Color(WuxiaUiTheme.Jade.r, WuxiaUiTheme.Jade.g, WuxiaUiTheme.Jade.b, 0.82f),
                new Color(WuxiaUiTheme.TextPrimary.r, WuxiaUiTheme.TextPrimary.g,
                    WuxiaUiTheme.TextPrimary.b, 0.76f),
                0.84f);
        }

        private void OnDisable()
        {
            ResetInput();
        }

        private void OnDestroy()
        {
            if (baseTexture != null)
            {
                Destroy(baseTexture);
            }

            if (knobTexture != null)
            {
                Destroy(knobTexture);
            }
        }

        private void Update()
        {
            UpdateGeometry();
            bool portrait = ResponsiveGui.IsPortrait;
            if (appliedPortraitLayout.HasValue && appliedPortraitLayout.Value != portrait)
            {
                ResetInput();
            }

            appliedPortraitLayout = portrait;
            if (!ShouldShow)
            {
                ResetInput();
                return;
            }

            HandleTouches(portrait);
            HandleEditorMouse(portrait);
        }

        private void HandleTouches(bool portrait)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began && activeFingerId < 0 &&
                    CanBeginPointer(touch.position, portrait))
                {
                    activeFingerId = touch.fingerId;
                    BeginPointer(touch.position, portrait);
                    continue;
                }

                if (touch.fingerId != activeFingerId)
                {
                    continue;
                }

                if (touch.phase == TouchPhase.Canceled || touch.phase == TouchPhase.Ended)
                {
                    activeFingerId = -1;
                    MoveInput = Vector2.zero;
                    IsDragging = false;
                }
                else
                {
                    UpdatePointerInput(touch.position, portrait);
                }
            }
        }

        private void HandleEditorMouse(bool portrait)
        {
            if (!Application.isEditor || !showInEditor || Input.touchCount > 0)
            {
                return;
            }

            Vector2 mousePosition = Input.mousePosition;
            if (Input.GetMouseButtonDown(0) &&
                CanBeginPointer(mousePosition, portrait))
            {
                mouseCaptured = true;
                BeginPointer(mousePosition, portrait);
            }

            if (!mouseCaptured)
            {
                return;
            }

            if (Input.GetMouseButtonUp(0))
            {
                mouseCaptured = false;
                MoveInput = Vector2.zero;
                IsDragging = false;
            }
            else
            {
                UpdatePointerInput(mousePosition, portrait);
            }
        }

        private bool CanBeginPointer(Vector2 pointerPosition, bool portrait)
        {
            if (!portrait)
            {
                return Vector2.Distance(pointerPosition, centerScreen) <= radiusScreen * 1.35f;
            }

            return !IsPortraitUiControl(pointerPosition);
        }

        private void BeginPointer(Vector2 pointerPosition, bool portrait)
        {
            if (portrait)
            {
                gestureStartScreen = pointerPosition;
                MoveInput = Vector2.zero;
                IsDragging = false;
                return;
            }

            UpdateJoystickInput(pointerPosition);
        }

        private void UpdatePointerInput(Vector2 pointerPosition, bool portrait)
        {
            if (portrait)
            {
                UpdatePortraitSwipeInput(pointerPosition);
                return;
            }

            UpdateJoystickInput(pointerPosition);
        }

        private void UpdateJoystickInput(Vector2 pointerPosition)
        {
            Vector2 offset = (pointerPosition - centerScreen) / Mathf.Max(1f, radiusScreen);
            MoveInput = Vector2.ClampMagnitude(offset, 1f);
            IsDragging = true;
        }

        private void UpdatePortraitSwipeInput(Vector2 pointerPosition)
        {
            float scale = ResponsiveGui.Scale;
            float deadZone = portraitSwipeDeadZone * scale;
            float fullSpeedDistance = Mathf.Max(deadZone + 1f, portraitSwipeFullSpeedDistance * scale);
            Vector2 delta = pointerPosition - gestureStartScreen;
            float distance = delta.magnitude;
            if (distance <= deadZone)
            {
                MoveInput = Vector2.zero;
                IsDragging = false;
                return;
            }

            float strength = Mathf.InverseLerp(deadZone, fullSpeedDistance, distance);
            MoveInput = delta.normalized * strength;
            IsDragging = true;
        }

        private bool IsPortraitUiControl(Vector2 pointerPosition)
        {
            float scale = ResponsiveGui.Scale;
            Vector2 guiPosition = ResponsiveGui.ScreenPointToGui(pointerPosition, scale);
            Rect safe = ResponsiveGui.SafeArea;
            Rect shortcutRail = new Rect(safe.xMax - 76f, safe.y, 76f, 184f);
            if (shortcutRail.Contains(guiPosition))
            {
                return true;
            }

            if (gameFlow.CurrentPhase == GamePhase.CaveRunning)
            {
                Rect exitControl = new Rect(safe.xMax - 182f, safe.yMax - 92f, 182f, 92f);
                return exitControl.Contains(guiPosition);
            }

            return false;
        }

        private void UpdateGeometry()
        {
            float scale = ResponsiveGui.Scale;
            Rect safeArea = Screen.safeArea;
            radiusScreen = joystickRadius * scale;
            float edgePadding = 18f * scale;
            float portraitLift = ResponsiveGui.IsPortrait ? 64f * scale : 0f;
            float centerX = ResponsiveGui.IsPortrait
                ? safeArea.center.x
                : safeArea.xMin + radiusScreen + edgePadding;
            centerScreen = new Vector2(
                centerX,
                safeArea.yMin + radiusScreen + edgePadding + portraitLift);
        }

        private void OnGUI()
        {
            if (!ShouldShow || ResponsiveGui.IsPortrait || baseTexture == null || knobTexture == null)
            {
                return;
            }

            GUI.depth = -950;
            float scale = ResponsiveGui.Scale;
            Matrix4x4 originalMatrix = ResponsiveGui.ApplyScale(scale);
            Color originalColor = GUI.color;
            try
            {
                Vector2 center = new Vector2(
                    centerScreen.x / scale,
                    (Screen.height - centerScreen.y) / scale);
                float radius = radiusScreen / scale;
                Rect baseRect = new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f);
                GUI.color = Color.white;
                GUI.DrawTexture(baseRect, baseTexture, ScaleMode.StretchToFill, true);

                float knobRadius = radius * 0.47f;
                Vector2 knobCenter = center + new Vector2(MoveInput.x, -MoveInput.y) * radius * 0.52f;
                Rect knobRect = new Rect(
                    knobCenter.x - knobRadius,
                    knobCenter.y - knobRadius,
                    knobRadius * 2f,
                    knobRadius * 2f);
                GUI.DrawTexture(knobRect, knobTexture, ScaleMode.StretchToFill, true);
            }
            finally
            {
                GUI.color = originalColor;
                GUI.matrix = originalMatrix;
            }
        }

        private static Texture2D CreateCircleTexture(int size, Color fill, Color rim, float rimStart)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "RuntimeVirtualJoystick",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            Color[] pixels = new Color[size * size];
            Vector2 center = Vector2.one * (size - 1) * 0.5f;
            float radius = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                    if (distance > 1f)
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                    else
                    {
                        float edgeFade = Mathf.Clamp01((1f - distance) * 18f);
                        Color color = distance >= rimStart ? rim : fill;
                        color.a *= edgeFade;
                        pixels[y * size + x] = color;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private void ResetInput()
        {
            activeFingerId = -1;
            mouseCaptured = false;
            gestureStartScreen = Vector2.zero;
            MoveInput = Vector2.zero;
            IsDragging = false;
        }
    }
}
