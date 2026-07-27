using UnityEngine;
using WuxiaRoguelite.Battle;
using WuxiaRoguelite.GameFlow;
using WuxiaRoguelite.Map;
using WuxiaRoguelite.Player;
using WuxiaRoguelite.Runtime;
using WuxiaRoguelite.UI;
using WuxiaRoguelite.Visual;

namespace WuxiaRoguelite.Cave
{
    public class CaveRoomController : MonoBehaviour
    {
        public GameFlowController gameFlow;
        public PlayerStats playerStats;
        public BattleManager battleManager;
        public Sprite[] playerIdleFrames;
        public Sprite[] playerRunFrames;
        public Sprite[] enemyIdleFrames;
        public Texture2D merchantTexture;
        public Texture2D treasureTexture;
        [Min(0.1f)] public float caveMoveSpeed = 0.52f;
        [Min(0.5f)] public float playerSpriteScale = ActorVisualScale.Medium;
        [Header("山洞角色展示")]
        [Tooltip("统一放大山洞探索中的玩家、守洞人、商人和宝箱，不影响碰撞或移动。")]
        [Range(1f, 2f)] public float caveActorScale = 1.55f;
        [Header("随机洞穴内容权重")]
        [Min(0f)] public float enemyWeight = 45f;
        [Min(0f)] public float merchantWeight = 25f;
        [Min(0f)] public float treasureWeight = 30f;

        public bool IsRoomActive { get; private set; }
        public CaveContentType CurrentContent { get; private set; }
        public bool CanUseExitAction =>
            IsRoomActive &&
            !merchantOpen &&
            gameFlow != null &&
            gameFlow.CurrentPhase == GamePhase.CaveRunning &&
            (battleManager == null || !battleManager.IsBattleActive) &&
            Vector2.Distance(playerPosition, exitPosition) < ExitInteractionDistance;

        private EncounterTrigger entrance;
        private Vector2 playerPosition;
        private readonly Vector2 eventPosition = new Vector2(0.73f, 0.56f);
        private readonly Vector2 exitPosition = new Vector2(0.13f, 0.22f);
        private const float ExitInteractionDistance = 0.12f;
        private bool eventStarted;
        private bool eventCompleted;
        private bool merchantOpen;
        private bool facingLeft;
        private Vector2 currentMoveInput;
        private string roomMessage = string.Empty;
        private readonly bool[] purchasedOffers = new bool[3];

        private GUIStyle titleStyle;
        private GUIStyle headingStyle;
        private GUIStyle bodyStyle;
        private GUIStyle centeredStyle;
        private GUIStyle hintStyle;
        private GUIStyle buttonStyle;

        private static readonly Color CaveBlack = new Color(0.025f, 0.03f, 0.035f, 1f);
        private static readonly Color Wall = new Color(0.11f, 0.13f, 0.14f, 1f);
        private static readonly Color Floor = new Color(0.20f, 0.18f, 0.15f, 1f);
        private static readonly Color FloorLight = new Color(0.27f, 0.24f, 0.19f, 1f);
        private static readonly Color Gold = new Color(0.83f, 0.65f, 0.29f, 1f);
        private static readonly Color Jade = new Color(0.32f, 0.68f, 0.52f, 1f);

        public void EnterCave(EncounterTrigger source, CaveContentType content)
        {
            entrance = source;
            CurrentContent = source != null
                ? source.ResolveCaveContent(SelectRandomContent)
                : content == CaveContentType.Random
                    ? SelectRandomContent()
                    : content;
            playerPosition = new Vector2(0.16f, 0.72f);
            eventStarted = false;
            eventCompleted = false;
            merchantOpen = false;
            facingLeft = false;
            roomMessage = ObjectiveText();
            for (int i = 0; i < purchasedOffers.Length; i++)
            {
                purchasedOffers[i] = false;
            }

            IsRoomActive = true;
        }

        public void ResetRoom()
        {
            IsRoomActive = false;
            entrance = null;
            merchantOpen = false;
            eventStarted = false;
            eventCompleted = false;
        }

        private void Update()
        {
            if (PrototypeHUDController.BlocksGameplayEscape)
            {
                return;
            }

            if (!IsRoomActive || gameFlow == null || gameFlow.CurrentPhase != GamePhase.CaveRunning ||
                battleManager == null || battleManager.IsBattleActive)
            {
                return;
            }

            if (merchantOpen)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    FinishMerchantEvent();
                }
                return;
            }

            Vector2 keyboardInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            Vector2 movementInput = MobileInputController.MoveInput.sqrMagnitude > 0.01f
                ? MobileInputController.MoveInput
                : keyboardInput;
            Vector2 input = new Vector2(movementInput.x, -movementInput.y);
            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }
            currentMoveInput = input;

            if (Mathf.Abs(input.x) > 0.01f)
            {
                facingLeft = input.x < 0f;
            }

            playerPosition += input * (caveMoveSpeed * Time.unscaledDeltaTime);
            playerPosition.x = Mathf.Clamp(playerPosition.x, 0.09f, 0.91f);
            playerPosition.y = Mathf.Clamp(playerPosition.y, 0.18f, 0.84f);

            if (!eventCompleted && !eventStarted && Vector2.Distance(playerPosition, eventPosition) < 0.115f)
            {
                BeginEvent();
            }

            if ((CanUseExitAction && Input.GetKeyDown(KeyCode.E)) || Input.GetKeyDown(KeyCode.Escape))
            {
                LeaveCave();
            }
        }

        public bool TryUseExitAction()
        {
            if (!CanUseExitAction)
            {
                return false;
            }

            LeaveCave();
            return true;
        }

        private void LeaveCave()
        {
            bool completed = eventCompleted;
            EncounterTrigger source = entrance;
            ResetRoom();
            gameFlow.ExitHiddenCave(completed);
            if (!completed && source != null)
            {
                source.ResetEncounter(true);
            }
        }

        private void BeginEvent()
        {
            eventStarted = true;
            switch (CurrentContent)
            {
                case CaveContentType.Enemy:
                    roomMessage = "守洞人逼近，进入自动战斗。";
                    CombatantStats enemy = entrance != null ? entrance.CreateEnemyStats() : null;
                    if (!IsUsableCaveEnemy(enemy))
                    {
                        enemy = CreateDefaultCaveEnemy();
                    }
                    gameFlow.BeginCaveBattle(enemy, entrance != null ? entrance.cultivationReward : 35,
                        entrance != null ? entrance.copperReward : 12, OnCaveBattleFinished);
                    break;
                case CaveContentType.Merchant:
                    merchantOpen = true;
                    roomMessage = "游商展开货箱，铜钱在这里可以换成实力。";
                    break;
                case CaveContentType.Treasure:
                    ResolveTreasure();
                    break;
            }
        }

        private void OnCaveBattleFinished(bool playerWon)
        {
            if (!playerWon)
            {
                return;
            }

            eventCompleted = true;
            roomMessage = "守洞人已败。前往左下方石门返回江湖。";
        }

        private void ResolveTreasure()
        {
            string reward = gameFlow.GrantCaveTreasure();
            eventCompleted = true;
            roomMessage = $"古匣开启：{reward}。前往左下方石门返回江湖。";
        }

        private void OnGUI()
        {
            RuntimeChineseFont.PrepareSkin();

            if (!IsRoomActive || gameFlow == null || gameFlow.CurrentPhase != GamePhase.CaveRunning ||
                battleManager == null || battleManager.IsBattleActive)
            {
                return;
            }

            GUI.depth = -900;
            EnsureStyles();
            Matrix4x4 originalGuiMatrix = ResponsiveGui.ApplyScale(ResponsiveGui.Scale);
            try
            {
                DrawRoom();
                if (merchantOpen)
                {
                    DrawMerchantPanel();
                }
            }
            finally
            {
                GUI.matrix = originalGuiMatrix;
            }
        }

        private void DrawRoom()
        {
            float width = ResponsiveGui.Width;
            float height = ResponsiveGui.Height;
            FillRect(new Rect(0f, 0f, width, height), CaveBlack);
            ResponsiveGui.DrawSingleLineLabel(new Rect(0f, 7f, width, 32f),
                "隐窟 · " + ContentName(), titleStyle, 16);
            ResponsiveGui.DrawSingleLineLabel(new Rect(0f, 37f, width, 22f),
                $"主地图倒数已暂停  {gameFlow.mainTimeRemaining:0.0}s", hintStyle, 10);

            Rect room = new Rect(14f, 66f, width - 28f, height - 80f);
            FillRect(room, Wall);
            Rect floor = new Rect(room.x + 18f, room.y + 18f, room.width - 36f, room.height - 36f);
            FillRect(floor, Floor);
            DrawFloorPattern(floor);

            float actorSize = Mathf.Clamp(Mathf.Min(width * 0.15f, floor.height * 0.32f), 62f, 128f) *
                              caveActorScale;
            Vector2 playerCenter = RoomPoint(floor, playerPosition);
            Vector2 targetCenter = RoomPoint(floor, eventPosition);
            Vector2 exitCenter = RoomPoint(floor, exitPosition);
            bool moving = currentMoveInput.sqrMagnitude > 0.01f;
            DrawExit(exitCenter, actorSize * 0.75f);
            float playerActorSize = actorSize * playerSpriteScale;
            DrawSpriteCentered(playerCenter, playerActorSize, moving ? playerRunFrames : playerIdleFrames, facingLeft);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(playerCenter.x - 60f, playerCenter.y + playerActorSize * 0.43f, 120f, 22f),
                "无名少侠", centeredStyle, 10);

            if (!eventCompleted)
            {
                DrawEventTarget(targetCenter, actorSize);
            }

            DrawExitActionButton();

            float preferredMessageWidth =
                ResponsiveGui.PreferredSingleLineWidth(roomMessage, bodyStyle, 30f);
            float messageWidth = Mathf.Clamp(preferredMessageWidth, width * 0.58f, width - 28f);
            Rect safe = ResponsiveGui.SafeArea;
            float messageX = (width - messageWidth) * 0.5f;
            float messageY = height - 112f;
            if (ResponsiveGui.IsPortrait)
            {
                const float edgePadding = 18f;
                const float exitButtonWidth = 156f;
                const float columnGap = 12f;
                messageWidth = Mathf.Min(
                    messageWidth,
                    Mathf.Max(180f, safe.width - exitButtonWidth - edgePadding * 2f - columnGap));
                messageX = safe.x + edgePadding;
                messageY = safe.yMax - 52f;
            }

            Rect message = new Rect(messageX, messageY, messageWidth, 34f);
            FillRect(message, new Color(0f, 0f, 0f, 0.82f));
            FillRect(new Rect(message.x, message.y, message.width, 2f), Gold);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(message.x + 12f, message.y + 2f, message.width - 24f, message.height - 4f),
                roomMessage, bodyStyle, 9);

            if (CanUseExitAction)
            {
                string exitHint = eventCompleted ? "可返回江湖" : "可撤离洞穴";
                ResponsiveGui.DrawSingleLineLabel(
                    new Rect(exitCenter.x - 80f, exitCenter.y - actorSize * 0.78f, 160f, 24f),
                    exitHint, hintStyle, 10);
            }
        }

        private void DrawExitActionButton()
        {
            Rect safe = ResponsiveGui.SafeArea;
            float buttonWidth = ResponsiveGui.IsPortrait ? 156f : 148f;
            const float buttonHeight = 52f;
            const float edgePadding = 18f;
            Rect buttonRect = new Rect(
                safe.xMax - buttonWidth - edgePadding,
                safe.yMax - buttonHeight - edgePadding,
                buttonWidth,
                buttonHeight);

            bool wasEnabled = GUI.enabled;
            GUI.enabled = CanUseExitAction;
            string label;
            if (!CanUseExitAction)
            {
                label = "走近左下石门";
            }
            else
            {
                label = eventCompleted ? "返回江湖" : "撤离洞穴";
            }

            if (GUI.Button(buttonRect, label, buttonStyle))
            {
                TryUseExitAction();
            }
            GUI.enabled = wasEnabled;
        }

        private void DrawFloorPattern(Rect floor)
        {
            const int columns = 8;
            const int rows = 5;
            float cellWidth = floor.width / columns;
            float cellHeight = floor.height / rows;
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    if ((x + y) % 2 == 0)
                    {
                        FillRect(new Rect(floor.x + x * cellWidth, floor.y + y * cellHeight, cellWidth, cellHeight), FloorLight);
                    }
                }
            }
        }

        private void DrawEventTarget(Vector2 center, float size)
        {
            switch (CurrentContent)
            {
                case CaveContentType.Enemy:
                    DrawSpriteCentered(center, size, enemyIdleFrames, true);
                    break;
                case CaveContentType.Merchant:
                    DrawTextureCentered(center, size * 0.72f, merchantTexture, new Color(0.36f, 0.62f, 0.72f));
                    break;
                case CaveContentType.Treasure:
                    DrawTextureCentered(center, size * 0.68f, treasureTexture, Gold);
                    break;
            }

            ResponsiveGui.DrawSingleLineLabel(
                new Rect(center.x - 90f, center.y + size * 0.42f, 180f, 24f),
                ContentName(), centeredStyle, 10);
        }

        private void DrawExit(Vector2 center, float size)
        {
            Color color = eventCompleted ? Jade : Gold;
            FillRect(new Rect(center.x - size * 0.36f, center.y - size * 0.48f, size * 0.72f, size * 0.96f), color);
            FillRect(new Rect(center.x - size * 0.22f, center.y - size * 0.34f, size * 0.44f, size * 0.82f), CaveBlack);
        }

        private void DrawMerchantPanel()
        {
            FillRect(new Rect(0f, 0f, ResponsiveGui.Width, ResponsiveGui.Height), new Color(0f, 0f, 0f, 0.66f));
            float panelWidth = Mathf.Min(530f, ResponsiveGui.Width - 28f);
            float panelHeight = Mathf.Min(292f, ResponsiveGui.Height - 28f);
            Rect panel = new Rect((ResponsiveGui.Width - panelWidth) * 0.5f,
                (ResponsiveGui.Height - panelHeight) * 0.5f, panelWidth, panelHeight);
            FillRect(panel, new Color(0.075f, 0.085f, 0.08f, 1f));
            FillRect(new Rect(panel.x, panel.y, 4f, panel.height), Gold);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(panel.x + 18f, panel.y + 10f, panel.width - 36f, 30f),
                "云游商人", headingStyle, 12);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(panel.x + 18f, panel.y + 40f, panel.width - 36f, 24f),
                $"现有铜钱  {playerStats.copper}", bodyStyle, 10);

            DrawOffer(panel, 0, "金疮药", "恢复 45% 气血", 6, panel.y + 72f);
            DrawOffer(panel, 1, "江湖秘器", "获得一件稀有装备", 10, panel.y + 122f);
            DrawOffer(panel, 2, "残页心得", "随机习得一门功法", 14, panel.y + 172f);

            if (GUI.Button(new Rect(panel.x + 18f, panel.yMax - 44f, panel.width - 36f, 30f), "结束交易", buttonStyle))
            {
                FinishMerchantEvent();
            }
        }

        private void FinishMerchantEvent()
        {
            merchantOpen = false;
            eventCompleted = true;
            roomMessage = "交易结束。前往左下方石门返回江湖。";
        }

        private void DrawOffer(Rect panel, int index, string name, string description, int price, float y)
        {
            Rect row = new Rect(panel.x + 18f, y, panel.width - 36f, 42f);
            FillRect(row, new Color(0.13f, 0.145f, 0.135f, 1f));
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(row.x + 10f, row.y + 1f, row.width - 106f, 21f),
                name, headingStyle, 10);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(row.x + 10f, row.y + 20f, row.width - 106f, 19f),
                description, bodyStyle, 9);
            GUI.enabled = !purchasedOffers[index] && playerStats.copper >= price;
            string buttonText = purchasedOffers[index] ? "已购" : $"{price} 铜钱";
            if (GUI.Button(new Rect(row.xMax - 90f, row.y + 6f, 80f, 30f), buttonText, buttonStyle))
            {
                PurchaseOffer(index, price);
            }
            GUI.enabled = true;
        }

        private void PurchaseOffer(int index, int price)
        {
            if (purchasedOffers[index] || !playerStats.TrySpendCopper(price))
            {
                return;
            }

            purchasedOffers[index] = true;
            switch (index)
            {
                case 0:
                    playerStats.HealPercent(0.45f);
                    roomMessage = "购得金疮药，气血已经恢复。";
                    break;
                case 1:
                    string equipmentName = playerStats.GrantTreasureEquipment();
                    roomMessage = string.IsNullOrEmpty(equipmentName) ? "秘器已经售罄。" : $"购得 {equipmentName}。";
                    break;
                default:
                    string art = gameFlow.GrantRandomMartialArt();
                    roomMessage = $"参悟残页，习得 {art}。";
                    break;
            }
        }

        private string ObjectiveText()
        {
            switch (CurrentContent)
            {
                case CaveContentType.Enemy:
                    return "深入洞穴，靠近守洞人后自动开战。";
                case CaveContentType.Merchant:
                    return "洞中似有人声，靠近云游商人查看货物。";
                default:
                    return "石室深处有一只古匣，靠近即可开启。";
            }
        }

        private string ContentName()
        {
            switch (CurrentContent)
            {
                case CaveContentType.Enemy:
                    return "守洞武人";
                case CaveContentType.Merchant:
                    return "云游商人";
                default:
                    return "秘藏古匣";
            }
        }

        private CaveContentType SelectRandomContent()
        {
            float safeEnemyWeight = Mathf.Max(0f, enemyWeight);
            float safeMerchantWeight = Mathf.Max(0f, merchantWeight);
            float safeTreasureWeight = Mathf.Max(0f, treasureWeight);
            float totalWeight = safeEnemyWeight + safeMerchantWeight + safeTreasureWeight;
            if (totalWeight <= 0f)
            {
                return CaveContentType.Enemy;
            }

            float roll = Random.value * totalWeight;
            if (roll < safeEnemyWeight)
            {
                return CaveContentType.Enemy;
            }

            roll -= safeEnemyWeight;
            return roll < safeMerchantWeight
                ? CaveContentType.Merchant
                : CaveContentType.Treasure;
        }

        private static bool IsUsableCaveEnemy(CombatantStats enemy)
        {
            return enemy != null && enemy.maxHealth > 1f && enemy.attack > 0f;
        }

        private static CombatantStats CreateDefaultCaveEnemy()
        {
            return new CombatantStats
            {
                displayName = "守洞武人",
                visualId = "orc_cave_guardian",
                maxHealth = 160f,
                currentHealth = 160f,
                attack = 14f,
                defense = 4f,
                attackSpeed = 0.85f,
                critChance = 0.05f,
                critMultiplier = 1.5f
            };
        }

        private static Vector2 RoomPoint(Rect room, Vector2 normalized)
        {
            return new Vector2(room.x + room.width * normalized.x, room.y + room.height * normalized.y);
        }

        private void DrawSpriteCentered(Vector2 center, float size, Sprite[] frames, bool flip)
        {
            if (frames == null || frames.Length == 0)
            {
                DrawTextureCentered(center, size, null, Color.white);
                return;
            }

            Sprite sprite = frames[Mathf.FloorToInt(Time.unscaledTime * 9f) % frames.Length];
            Rect rect = new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size);
            // Keep the full sliced frame so transparent padding remains consistent
            // across animation frames instead of stretching each tight mesh bounds.
            Rect textureRect = sprite.rect;
            Rect uv = new Rect(textureRect.x / sprite.texture.width, textureRect.y / sprite.texture.height,
                textureRect.width / sprite.texture.width, textureRect.height / sprite.texture.height);
            if (flip)
            {
                uv.x += uv.width;
                uv.width = -uv.width;
            }
            GUI.DrawTextureWithTexCoords(rect, sprite.texture, uv, true);
        }

        private static void DrawTextureCentered(Vector2 center, float size, Texture2D texture, Color fallback)
        {
            Rect rect = new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size);
            Color previous = GUI.color;
            GUI.color = fallback;
            GUI.DrawTexture(rect, texture != null ? texture : Texture2D.whiteTexture, ScaleMode.ScaleToFit, true);
            GUI.color = previous;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = Style(24, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            headingStyle = Style(17, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            bodyStyle = Style(14, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white);
            centeredStyle = Style(13, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            hintStyle = Style(14, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.92f, 0.79f, 0.48f));
            buttonStyle = RuntimeChineseFont.Apply(new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            });
        }

        private static GUIStyle Style(int size, FontStyle fontStyle, TextAnchor alignment, Color color)
        {
            return RuntimeChineseFont.Apply(new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                fontStyle = fontStyle,
                alignment = alignment,
                wordWrap = true,
                normal = { textColor = color }
            });
        }

        private static void FillRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
