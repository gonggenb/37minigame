using UnityEngine;
using WuxiaRoguelite.Battle;
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
        }
    }

    /// <summary>
    /// Touch-first virtual joystick for the main map and cave exploration.
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
        [Range(52f, 84f)] public float joystickRadius = 68f;

        public static Vector2 MoveInput { get; private set; }
        public static bool IsDragging { get; private set; }

        private int activeFingerId = -1;
        private bool mouseCaptured;
        private Vector2 centerScreen;
        private float radiusScreen;
        private Texture2D baseTexture;
        private Texture2D knobTexture;

        private bool ShouldShow =>
            gameFlow != null &&
            !PrototypeHUDController.IsSettingsOpen &&
            !gameFlow.IsCharacterMenuPaused &&
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

            baseTexture = CreateCircleTexture(128, new Color(0.07f, 0.09f, 0.085f, 0.48f),
                new Color(0.86f, 0.68f, 0.32f, 0.82f), 0.78f);
            knobTexture = CreateCircleTexture(96, new Color(0.27f, 0.68f, 0.53f, 0.88f),
                new Color(0.92f, 0.88f, 0.74f, 0.92f), 0.82f);
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
            if (!ShouldShow)
            {
                ResetInput();
                return;
            }

            HandleTouches();
            HandleEditorMouse();
        }

        private void HandleTouches()
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began && activeFingerId < 0 &&
                    Vector2.Distance(touch.position, centerScreen) <= radiusScreen * 1.35f)
                {
                    activeFingerId = touch.fingerId;
                    UpdateMoveInput(touch.position);
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
                    UpdateMoveInput(touch.position);
                }
            }
        }

        private void HandleEditorMouse()
        {
            if (!Application.isEditor || !showInEditor || Input.touchCount > 0)
            {
                return;
            }

            Vector2 mousePosition = Input.mousePosition;
            if (Input.GetMouseButtonDown(0) &&
                Vector2.Distance(mousePosition, centerScreen) <= radiusScreen * 1.35f)
            {
                mouseCaptured = true;
                UpdateMoveInput(mousePosition);
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
                UpdateMoveInput(mousePosition);
            }
        }

        private void UpdateMoveInput(Vector2 pointerPosition)
        {
            Vector2 offset = (pointerPosition - centerScreen) / Mathf.Max(1f, radiusScreen);
            MoveInput = Vector2.ClampMagnitude(offset, 1f);
            IsDragging = true;
        }

        private void UpdateGeometry()
        {
            float scale = ResponsiveGui.Scale;
            Rect safeArea = Screen.safeArea;
            radiusScreen = joystickRadius * scale;
            float edgePadding = 18f * scale;
            float centerX = ResponsiveGui.IsPortrait
                ? safeArea.center.x
                : safeArea.xMin + radiusScreen + edgePadding;
            centerScreen = new Vector2(
                centerX,
                safeArea.yMin + radiusScreen + edgePadding);
        }

        private void OnGUI()
        {
            if (!ShouldShow || baseTexture == null || knobTexture == null)
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
            MoveInput = Vector2.zero;
            IsDragging = false;
        }
    }
}
