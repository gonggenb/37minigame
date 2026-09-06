using UnityEngine;
using WuxiaRoguelite.Runtime;

namespace WuxiaRoguelite.UI
{
    public partial class BattleScreenController
    {
        private Texture2D openingHero;
        private Texture2D openingFox;
        private Texture2D openingBackdrop;
        private bool openingResourcesLoaded;
        private GUIStyle dialogueKickerStyle;
        private GUIStyle dialogueSpeakerStyle;
        private GUIStyle dialogueBodyStyle;
        private GUIStyle dialogueHintStyle;
        private GUIStyle dialogueContinueStyle;
        private int presentedOpeningIndex = -1;
        private int openingInputFrame = -1;
        private string openingFullText = string.Empty;
        private string openingVisibleText = string.Empty;
        private float openingRevealTime;
        private int openingVisibleCharacters;
        private const float OpeningCharactersPerSecond = 28f;

        private bool OpeningLineRevealed => openingVisibleCharacters >= openingFullText.Length;

        private void EnsureOpeningStyles()
        {
            dialogueKickerStyle = CreateStyle(14, FontStyle.Bold, TextAnchor.MiddleLeft,
                WuxiaUiTheme.TextPrimary);
            dialogueSpeakerStyle = CreateStyle(20, FontStyle.Bold, TextAnchor.MiddleCenter,
                WuxiaUiTheme.TextPrimary);
            dialogueBodyStyle = CreateStyle(20, FontStyle.Normal, TextAnchor.UpperLeft,
                WuxiaUiTheme.TextPrimary);
            dialogueBodyStyle.wordWrap = true;
            dialogueBodyStyle.richText = false;
            dialogueHintStyle = CreateStyle(12, FontStyle.Normal, TextAnchor.MiddleLeft,
                WuxiaUiTheme.TextSecondary);
            dialogueContinueStyle = CreateStyle(14, FontStyle.Bold, TextAnchor.MiddleRight,
                WuxiaUiTheme.Brass);
        }

        private void UpdateOpeningPresentation()
        {
            if (gameFlow == null || !gameFlow.IsOpeningIntroActive)
            {
                presentedOpeningIndex = -1;
                return;
            }

            SyncOpeningLine();
            if (PrototypeHUDController.IsSettingsOpen) return;
            openingRevealTime += Time.unscaledDeltaTime;
            SetOpeningVisibleCharacters(Mathf.FloorToInt(openingRevealTime * OpeningCharactersPerSecond));
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter))
                AdvanceOpeningPresentation();
        }

        private void SyncOpeningLine()
        {
            if (presentedOpeningIndex == gameFlow.OpeningDialogueIndex) return;
            presentedOpeningIndex = gameFlow.OpeningDialogueIndex;
            openingFullText = gameFlow.CurrentOpeningDialogue;
            openingRevealTime = 0f;
            openingVisibleCharacters = 0;
            openingVisibleText = string.Empty;
        }

        private void SetOpeningVisibleCharacters(int count)
        {
            count = Mathf.Clamp(count, 0, openingFullText.Length);
            if (count == openingVisibleCharacters) return;
            openingVisibleCharacters = count;
            openingVisibleText = openingFullText.Substring(0, count);
        }

        private void AdvanceOpeningPresentation()
        {
            if (gameFlow == null || !gameFlow.IsOpeningIntroActive ||
                PrototypeHUDController.IsSettingsOpen || openingInputFrame == Time.frameCount) return;
            openingInputFrame = Time.frameCount;
            SyncOpeningLine();
            if (!OpeningLineRevealed)
            {
                openingRevealTime = openingFullText.Length / OpeningCharactersPerSecond;
                SetOpeningVisibleCharacters(openingFullText.Length);
                return;
            }

            gameFlow.AdvanceOpeningIntro();
            if (gameFlow.IsOpeningIntroActive) SyncOpeningLine();
        }

        private void LoadOpeningArt()
        {
            if (openingResourcesLoaded) return;
            openingResourcesLoaded = true;
            openingHero = Resources.Load<Texture2D>("OpeningDialogue/portrait_hero_v01");
            openingFox = Resources.Load<Texture2D>("OpeningDialogue/portrait_fox_v01");
            openingBackdrop = Resources.Load<Texture2D>("OpeningDialogue/temple_dusk_v01");
        }

        private void DrawOpeningIntro()
        {
            LoadOpeningArt();
            SyncOpeningLine();
            float scale = ResponsiveGui.Scale;
            float width = Screen.width / scale;
            float height = Screen.height / scale;
            bool portrait = ResponsiveGui.IsPortrait;
            Rect safe = ResponsiveGui.SafeArea;
            Matrix4x4 previousMatrix = ResponsiveGui.ApplyScale(scale);
            try
            {
                Texture2D background = openingBackdrop != null ? openingBackdrop : bossBattleBackground;
                if (background != null)
                    GUI.DrawTexture(new Rect(0f, 0f, width, height), background, ScaleMode.ScaleAndCrop, false);
                else
                    DrawBackdrop(width, height);

                GUI.Label(new Rect(safe.x + 24f, safe.y + 14f, safe.width - 100f, 30f),
                    OpeningDialogueCatalog.Title, dialogueKickerStyle);

                float margin = portrait ? 16f : 32f;
                float panelWidth = safe.width - margin * 2f;
                dialogueBodyStyle.fontSize = portrait ? 20 : 18;
                float textHeight = dialogueBodyStyle.CalcHeight(new GUIContent(openingFullText), panelWidth - 48f);
                float panelHeight = Mathf.Max(portrait ? 214f : 146f, textHeight + 98f);
                Rect panel = new Rect(safe.x + margin, safe.yMax - margin - panelHeight,
                    panelWidth, panelHeight);

                DrawOpeningPortraits(safe, panel, portrait);
                WuxiaUiTheme.DrawPanel(panel, WuxiaUiTheme.BackgroundInk,
                    WuxiaUiTheme.Brass, WuxiaPanelKind.Default);
                Rect nameplate = new Rect(panel.x + 12f, panel.y - 24f,
                    Mathf.Min(184f, panel.width - 24f), 44f);
                WuxiaUiTheme.DrawPanel(nameplate, WuxiaUiTheme.SurfaceWood,
                    WuxiaUiTheme.Brass, WuxiaPanelKind.Default);
                GUI.Label(nameplate, gameFlow.CurrentOpeningSpeaker, dialogueSpeakerStyle);
                GUI.Label(new Rect(panel.x + 24f, panel.y + 40f, panel.width - 48f, textHeight + 6f),
                    openingVisibleText, dialogueBodyStyle);

                string hint = OpeningLineRevealed ? "点击对话框继续" : "点击显示完整对白";
                string next = gameFlow.OpeningDialogueIndex == gameFlow.OpeningDialogueCount - 1
                    ? gameFlow.OpeningContinueLabel : "继续";
                GUI.Label(new Rect(panel.x + 24f, panel.yMax - 44f, panel.width - 170f, 32f),
                    hint, dialogueHintStyle);
                GUI.Label(new Rect(panel.xMax - 156f, panel.yMax - 44f, 132f, 32f),
                    OpeningLineRevealed ? next + "  ▾" : "···", dialogueContinueStyle);
                bool enabled = GUI.enabled;
                GUI.enabled = enabled && !PrototypeHUDController.IsSettingsOpen;
                if (GUI.Button(panel, GUIContent.none, GUIStyle.none)) AdvanceOpeningPresentation();
                GUI.enabled = enabled;
            }
            finally
            {
                GUI.matrix = previousMatrix;
            }
        }

        private void DrawOpeningPortraits(Rect safe, Rect panel, bool portrait)
        {
            OpeningPortrait speaker = gameFlow.CurrentOpeningPortrait;
            bool heroSpeaking = speaker == OpeningPortrait.Player;
            bool foxSpeaking = speaker == OpeningPortrait.Fox;
            bool foxVisible = gameFlow.OpeningDialogueIndex >= 2 && gameFlow.OpeningDialogueIndex < 9;
            float top = safe.y + (portrait ? 76f : 44f);
            float bottom = panel.y + (portrait ? 118f : 94f);
            float availableHeight = Mathf.Max(140f, bottom - top);
            float heroHeight = availableHeight * (heroSpeaking ? 1f : 0.84f);
            float foxHeight = availableHeight * (foxSpeaking ? 1f : 0.84f);
            float heroCenter = safe.x + safe.width * (portrait ? (heroSpeaking ? 0.34f : 0.10f) : 0.25f);
            float foxCenter = safe.x + safe.width * (portrait ? (foxSpeaking ? 0.64f : 0.90f) : 0.75f);
            if (speaker == OpeningPortrait.Narrator)
            {
                heroCenter = safe.x + safe.width * 0.26f;
                foxCenter = safe.x + safe.width * 0.77f;
            }

            float heroLight = heroSpeaking ? 1f : speaker == OpeningPortrait.Narrator ? 0.72f : 0.48f;
            float foxLight = foxSpeaking ? 1f : speaker == OpeningPortrait.Narrator ? 0.66f : 0.44f;
            // Draw the listener first, so overlapping hair/tails never obscure the speaker's face.
            if (heroSpeaking)
            {
                if (foxVisible) DrawOpeningPortrait(openingFox, foxCenter, bottom, foxHeight, foxLight, true);
                DrawOpeningPortrait(openingHero, heroCenter, bottom, heroHeight, heroLight, false);
            }
            else
            {
                DrawOpeningPortrait(openingHero, heroCenter, bottom, heroHeight, heroLight, false);
                if (foxVisible) DrawOpeningPortrait(openingFox, foxCenter, bottom, foxHeight, foxLight, true);
            }
        }

        private void DrawOpeningPortrait(Texture2D texture, float center, float bottom,
            float height, float brightness, bool isFox)
        {
            if (texture == null)
            {
                // Keep the previous in-game art available until a valid alpha portrait is delivered.
                EnemyVisualProfile profile = isFox ? FindEnemyVisualProfile(GameTextCatalog.FinalBossVisualId) : null;
                Sprite frame = GetFrame(isFox ? profile?.idleFrames : playerIdleFrames, false, 0f);
                if (frame == null) return;
                float size = height * 0.56f;
                Color prior = GUI.color;
                GUI.color = new Color(brightness, brightness, brightness, 1f);
                DrawSprite(new Rect(center - size * 0.5f, bottom - size - 64f, size, size),
                    frame, isFox && profile != null && profile.flipHorizontally);
                GUI.color = prior;
                return;
            }
            float width = height * texture.width / texture.height;
            Color previous = GUI.color;
            // Lower RGB rather than alpha: listeners remain solid instead of showing scenery through faces.
            GUI.color = new Color(brightness, brightness, brightness, 1f);
            GUI.DrawTexture(new Rect(center - width * 0.5f, bottom - height, width, height),
                texture, ScaleMode.StretchToFill, true);
            GUI.color = previous;
        }
    }
}
