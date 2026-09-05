using System.Collections.Generic;
using UnityEngine;
using WuxiaRoguelite.Battle;
using WuxiaRoguelite.GameFlow;
using WuxiaRoguelite.Map;
using WuxiaRoguelite.MartialArts;
using WuxiaRoguelite.Player;
using WuxiaRoguelite.Runtime;
using WuxiaRoguelite.UI;
using WuxiaRoguelite.Visual;

namespace WuxiaRoguelite.Cave
{
    public class CaveRoomController : MonoBehaviour
    {
        private enum MerchantOfferType
        {
            MartialArt,
            Equipment,
            Relic,
            Consumable,
            UpgradeService,
            TransformService
        }

        private sealed class MerchantOffer
        {
            public MerchantOfferType type;
            public string contentId;
            public string displayName;
            public string description;
            public string iconId;
            public int price;
            public bool discounted;
            public bool sold;
        }

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
        public bool IsModalUiOpen => merchantOpen;
        public CaveContentType CurrentContent { get; private set; }
        public bool CanUseExitAction =>
            IsRoomActive &&
            !merchantOpen &&
            gameFlow != null &&
            gameFlow.CurrentPhase == GamePhase.CaveRunning &&
            (battleManager == null || !battleManager.IsBattleActive) &&
            Vector2.Distance(playerPosition, CurrentExitPosition) < ExitInteractionDistance;

        private EncounterTrigger entrance;
        private Vector2 playerPosition;
        private const float ExitInteractionDistance = 0.12f;
        private const float EventInteractionDistance = 0.115f;
        private const float EventRearmDistance = 0.16f;
        private bool eventStarted;
        private bool eventCompleted;
        private bool merchantOpen;
        private bool merchantAwaitingReapproach;
        private bool facingLeft;
        private Vector2 currentMoveInput;
        private string roomMessage = string.Empty;
        private readonly List<MerchantOffer> merchantOffers = new List<MerchantOffer>();
        private readonly Dictionary<string, Texture2D> caveSceneTextures = new Dictionary<string, Texture2D>();
        private Vector2 merchantScroll;
        private bool merchantRefreshed;

        private Vector2 CurrentEventPosition => ResponsiveGui.IsPortrait
            ? new Vector2(0.59f, 0.25f)
            : new Vector2(0.73f, 0.50f);

        private Vector2 CurrentExitPosition => ResponsiveGui.IsPortrait
            ? new Vector2(0.20f, 0.82f)
            : new Vector2(0.28f, 0.80f);

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
            playerPosition = ResponsiveGui.IsPortrait
                ? new Vector2(0.35f, 0.72f)
                : new Vector2(0.43f, 0.72f);
            eventStarted = false;
            eventCompleted = false;
            merchantOpen = false;
            merchantAwaitingReapproach = false;
            facingLeft = false;
            roomMessage = ObjectiveText();
            merchantRefreshed = false;
            merchantScroll = Vector2.zero;
            if (CurrentContent == CaveContentType.Merchant)
            {
                BuildMerchantStock();
            }

            IsRoomActive = true;
        }

        public void ResetRoom()
        {
            IsRoomActive = false;
            entrance = null;
            merchantOpen = false;
            merchantAwaitingReapproach = false;
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
            playerPosition.y = Mathf.Clamp(playerPosition.y, 0.12f, 0.88f);

            float eventDistance = Vector2.Distance(playerPosition, CurrentEventPosition);
            if (merchantAwaitingReapproach && eventDistance >= EventRearmDistance)
            {
                merchantAwaitingReapproach = false;
            }

            if (!eventCompleted && !eventStarted && !merchantAwaitingReapproach &&
                eventDistance < EventInteractionDistance)
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
                case CaveContentType.Trial:
                    roomMessage = "试炼石门落下，守关者的气息远胜寻常。";
                    CombatantStats trialEnemy = entrance != null ? entrance.CreateEnemyStats() : CreateDefaultCaveEnemy();
                    if (!IsUsableCaveEnemy(trialEnemy))
                    {
                        trialEnemy = CreateDefaultCaveEnemy();
                    }
                    trialEnemy.displayName = "隐窟试炼者";
                    trialEnemy.maxHealth *= 1.25f;
                    trialEnemy.currentHealth = trialEnemy.maxHealth;
                    trialEnemy.attack *= 1.12f;
                    gameFlow.BeginCaveBattle(trialEnemy,
                        entrance != null ? entrance.cultivationReward + 12 : 48,
                        entrance != null ? entrance.copperReward + 5 : 17,
                        OnCaveBattleFinished);
                    break;
                case CaveContentType.Merchant:
                    if (merchantOffers.Count == 0)
                    {
                        BuildMerchantStock();
                    }
                    merchantOpen = true;
                    roomMessage = "游商展开货箱，铜钱在这里可以换成实力。";
                    break;
                case CaveContentType.Treasure:
                    ResolveTreasure();
                    break;
                default:
                    ResolveSpecialCaveEvent();
                    break;
            }
        }

        private void ResolveSpecialCaveEvent()
        {
            eventCompleted = true;
            switch (CurrentContent)
            {
                case CaveContentType.Altar:
                    playerStats.ApplyMysteryPoison(0.12f);
                    playerStats.ApplyAttackBuff(0.12f);
                    roomMessage = "血契石坛：损失 12% 最大气血，换得本局攻击 +12%。";
                    break;
                case CaveContentType.Healer:
                    playerStats.HealPercent(0.65f);
                    roomMessage = "药庐残火：恢复 65% 最大气血。";
                    break;
                case CaveContentType.Library:
                    string art = gameFlow.GrantRandomMartialArt();
                    roomMessage = $"残卷石室：参悟《{art}》。";
                    break;
                case CaveContentType.Forge:
                    string equipment = playerStats.GrantTreasureEquipment();
                    roomMessage = string.IsNullOrEmpty(equipment)
                        ? "铸兵台只余精铁，攻击永久提升 6%。"
                        : $"铸兵台重燃：获得 {equipment}。";
                    if (string.IsNullOrEmpty(equipment))
                    {
                        playerStats.ApplyAttackBuff(0.06f);
                    }
                    break;
                case CaveContentType.Gambler:
                    if (playerStats.TrySpendCopper(6))
                    {
                        bool won = Random.value < 0.65f;
                        if (won)
                        {
                            playerStats.GainCopper(15);
                        }
                        roomMessage = won ? "盲匣赌局：投入 6 铜，赢回 15 铜。" : "盲匣赌局：投入的 6 铜化作一声轻响。";
                    }
                    else
                    {
                        roomMessage = "盲匣赌局：至少需要 6 铜钱，赌局已经错过。";
                    }
                    break;
                case CaveContentType.HerbGarden:
                    playerStats.ApplyMoveSpeedBuff(0.10f);
                    playerStats.HealPercent(0.25f);
                    roomMessage = "地脉药圃：恢复 25% 气血，本局移速 +10%。";
                    break;
                case CaveContentType.RelicShrine:
                    string relicId = RandomUnownedRelic();
                    RunRelicDefinition relic = RunContentCatalog.GetRelic(relicId);
                    bool granted = playerStats.GrantRelic(relicId);
                    roomMessage = granted && relic != null
                        ? $"供器神龛：获得遗物「{relic.displayName}」。"
                        : "供器神龛已经沉寂，转化为 12 铜钱。";
                    if (!granted)
                    {
                        playerStats.GainCopper(12);
                    }
                    break;
            }

            roomMessage += " 前往左下石门返回江湖。";
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
            Rect safe = ResponsiveGui.SafeArea;
            FillRect(new Rect(0f, 0f, width, height), CaveBlack);
            bool portraitLayout = ResponsiveGui.IsPortrait;
            Rect caveHeader = new Rect(
                safe.x + 14f,
                safe.y + 7f,
                Mathf.Min(480f, safe.width - 28f),
                portraitLayout ? 116f : 52f);
            WuxiaUiTheme.DrawPanel(caveHeader,
                new Color(0.025f, 0.03f, 0.035f, 0.90f), WuxiaUiTheme.Paused,
                WuxiaPanelKind.Paper);
            if (portraitLayout)
            {
                WuxiaUiComponents.Timer(new Rect(caveHeader.x + 8, caveHeader.y + 10, 96, 96),
                    gameFlow.mainTimeRemaining, gameFlow.mainTimeLimit, true);
                WuxiaUiComponents.Text(new Rect(caveHeader.x + 120, caveHeader.y + 18, caveHeader.width - 144, 34),
                    "隐窟 · " + ContentName(), 20);
                WuxiaUiComponents.Text(new Rect(caveHeader.x + 120, caveHeader.y + 60, caveHeader.width - 144, 34),
                    "主时间暂停", 16, WuxiaUiTheme.Paused);
            }
            else
            {
            ResponsiveGui.DrawSingleLineLabel(new Rect(caveHeader.x, caveHeader.y + 2f, caveHeader.width, 28f),
                "隐窟 · " + ContentName(), titleStyle, 16);
            ResponsiveGui.DrawSingleLineLabel(new Rect(caveHeader.x, caveHeader.y + 28f, caveHeader.width, 19f),
                $"主地图倒数已暂停  {gameFlow.mainTimeRemaining:0.0} 秒", hintStyle, 10);
            }

            float roomTop = portraitLayout ? caveHeader.yMax + 8f : 66f;
            Rect room = new Rect(safe.x + 14f, roomTop, safe.width - 28f, safe.yMax - roomTop - 14f);
            FillRect(room, Wall);
            Rect floor = new Rect(room.x + 18f, room.y + 18f, room.width - 36f, room.height - 36f);
            Texture2D background = GetCaveSceneBackground();
            if (background != null)
            {
                GUI.DrawTexture(floor, background, ScaleMode.ScaleAndCrop, true);
                FillRect(new Rect(floor.x, floor.y, floor.width, 3f), new Color(0.78f, 0.62f, 0.30f, 0.56f));
            }
            else
            {
                FillRect(floor, Floor);
                DrawFloorPattern(floor);
            }

            float actorSize = Mathf.Clamp(Mathf.Min(width * 0.15f, floor.height * 0.32f), 62f, 128f) *
                              caveActorScale;
            Vector2 playerCenter = RoomPoint(floor, playerPosition);
            Vector2 targetCenter = RoomPoint(floor, CurrentEventPosition);
            Vector2 exitCenter = RoomPoint(floor, CurrentExitPosition);
            bool moving = currentMoveInput.sqrMagnitude > 0.01f;
            DrawExit(exitCenter, actorSize * 1.05f);
            float playerActorSize = actorSize * playerSpriteScale;
            DrawSpriteCentered(playerCenter, playerActorSize, moving ? playerRunFrames : playerIdleFrames, facingLeft);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(playerCenter.x - 60f, playerCenter.y + playerActorSize * 0.43f, 120f, 22f),
                "无名少侠", centeredStyle, 10);

            if (!eventCompleted || CurrentContent == CaveContentType.Merchant)
            {
                DrawEventTarget(targetCenter, actorSize);
            }

            DrawExitActionButton();

            float preferredMessageWidth =
                ResponsiveGui.PreferredSingleLineWidth(roomMessage, bodyStyle, 30f);
            float messageWidth = Mathf.Clamp(preferredMessageWidth, width * 0.58f, width - 28f);
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
            WuxiaUiTheme.DrawPanel(message,
                new Color(0f, 0f, 0f, 0.82f), WuxiaUiTheme.Paused,
                WuxiaPanelKind.Default);
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
                default:
                    DrawTextureCentered(center, size * 0.62f,
                        LoadContentIcon(ContentIconCatalog.Cave(CurrentContent)), Gold);
                    break;
            }

            ResponsiveGui.DrawSingleLineLabel(
                new Rect(center.x - 90f, center.y + size * 0.42f, 180f, 24f),
                ContentName(), centeredStyle, 10);
        }

        private void DrawExit(Vector2 center, float size)
        {
            Texture2D exitTexture = LoadCaveSceneTexture("CaveScenes/cave_exit_arch_v01");
            if (exitTexture != null)
            {
                Color previous = GUI.color;
                GUI.color = eventCompleted ? new Color(0.72f, 1f, 0.82f, 1f) : Color.white;
                GUI.DrawTexture(
                    new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size),
                    exitTexture,
                    ScaleMode.ScaleToFit,
                    true);
                GUI.color = previous;
                return;
            }

            Color color = eventCompleted ? Jade : Gold;
            FillRect(new Rect(center.x - size * 0.36f, center.y - size * 0.48f, size * 0.72f, size * 0.96f), color);
            FillRect(new Rect(center.x - size * 0.22f, center.y - size * 0.34f, size * 0.44f, size * 0.82f), CaveBlack);
        }

        private Texture2D GetCaveSceneBackground()
        {
            string theme;
            switch (CurrentContent)
            {
                case CaveContentType.Enemy:
                case CaveContentType.Altar:
                case CaveContentType.Trial:
                    theme = "combat";
                    break;
                case CaveContentType.Merchant:
                case CaveContentType.Healer:
                case CaveContentType.Library:
                    theme = "sanctuary";
                    break;
                case CaveContentType.Treasure:
                case CaveContentType.Forge:
                case CaveContentType.RelicShrine:
                    theme = "vault";
                    break;
                default:
                    theme = "mystic";
                    break;
            }

            string orientation = ResponsiveGui.IsPortrait ? "portrait" : "landscape";
            return LoadCaveSceneTexture($"CaveScenes/bg_cave_{theme}_{orientation}_v01");
        }

        private Texture2D LoadCaveSceneTexture(string resourcePath)
        {
            if (caveSceneTextures.TryGetValue(resourcePath, out Texture2D cached))
            {
                return cached;
            }

            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            caveSceneTextures[resourcePath] = texture;
            return texture;
        }

        private void DrawMerchantPanel()
        {
            if (ResponsiveGui.IsPortrait) { DrawPortraitMerchant(); return; }
            FillRect(new Rect(0f, 0f, ResponsiveGui.Width, ResponsiveGui.Height), new Color(0f, 0f, 0f, 0.66f));
            float panelWidth = Mathf.Min(920f, ResponsiveGui.Width - 24f);
            float panelHeight = Mathf.Min(510f, ResponsiveGui.Height - 24f);
            Rect panel = new Rect((ResponsiveGui.Width - panelWidth) * 0.5f,
                (ResponsiveGui.Height - panelHeight) * 0.5f, panelWidth, panelHeight);
            WuxiaUiTheme.DrawPanel(panel,
                new Color(0.075f, 0.085f, 0.08f, 1f), Gold,
                WuxiaPanelKind.Paper);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(panel.x + 18f, panel.y + 8f, panel.width - 330f, 30f),
                "云游商人 · 固定货架", headingStyle, 12);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(panel.xMax - 290f, panel.y + 8f, 110f, 30f),
                $"铜钱 {playerStats.copper}", hintStyle, 10);

            bool canRefresh = !merchantRefreshed && playerStats.copper >= 5;
            bool previousEnabled = GUI.enabled;
            GUI.enabled = canRefresh;
            // Reserve the top-right rail for the shared settings button.
            if (GUI.Button(new Rect(panel.xMax - 174f, panel.y, 104f, 44f),
                    merchantRefreshed ? "本洞已刷新" : "刷新 5 铜", buttonStyle))
            {
                playerStats.TrySpendCopper(5);
                merchantRefreshed = true;
                BuildMerchantStock();
                roomMessage = "商人重新整理了全部货架。";
            }
            GUI.enabled = previousEnabled;

            Rect viewport = new Rect(panel.x + 14f, panel.y + 48f, panel.width - 28f, panel.height - 98f);
            int columns = ResponsiveGui.IsPortrait ? 2 : 5;
            float gap = 9f;
            float cardWidth = (viewport.width - 18f - gap * (columns - 1)) / columns;
            float cardHeight = ResponsiveGui.IsPortrait ? 164f : 166f;
            int rows = Mathf.CeilToInt(merchantOffers.Count / (float)columns);
            float contentHeight = Mathf.Max(viewport.height - 2f, rows * cardHeight + Mathf.Max(0, rows - 1) * gap);
            merchantScroll = GUI.BeginScrollView(viewport, merchantScroll,
                new Rect(0f, 0f, viewport.width - 18f, contentHeight));
            for (int i = 0; i < merchantOffers.Count; i++)
            {
                int column = i % columns;
                int row = i / columns;
                DrawMerchantCard(
                    new Rect(column * (cardWidth + gap), row * (cardHeight + gap), cardWidth, cardHeight),
                    merchantOffers[i]);
            }
            GUI.EndScrollView();

            ResponsiveGui.DrawSingleLineLabel(
                new Rect(panel.x + 18f, panel.yMax - 42f, panel.width - 174f, 28f),
                "库存不会因购买自动补充；可连续购买不同商品。", bodyStyle, 9);
            if (GUI.Button(new Rect(panel.xMax - 144f, panel.yMax - 42f, 126f, 30f), "结束交易", buttonStyle))
            {
                FinishMerchantEvent();
            }
        }

        private int portraitMerchantSelection;
        private Vector2 portraitMerchantDetailScroll;

        private void DrawPortraitMerchant()
        {
            GUIStyle touchStyle = WuxiaUiComponents.TouchButton();
            FillRect(new Rect(0, 0, ResponsiveGui.Width, ResponsiveGui.Height), new Color(0.02f, 0.025f, 0.023f, 0.94f));
            Rect p = PortraitUiLayout.Modal(812);
            WuxiaUiTheme.DrawPanel(p, WuxiaUiTheme.BackgroundInk, Gold);
            WuxiaUiComponents.Text(new Rect(p.x + 24, p.y + 18, p.width - 160, 38), "云游商人", 28);
            WuxiaUiComponents.Text(new Rect(p.xMax - 138, p.y + 20, 114, 34), $"铜钱 {playerStats.copper}", 18, Gold, TextAnchor.MiddleRight);
            WuxiaUiComponents.Text(new Rect(p.x + 24, p.y + 62, p.width - 48, 28), "主时间暂停 · 云游货架", 16, WuxiaUiTheme.Paused);
            Rect view = new Rect(p.x + 24, p.y + 102, p.width - 48, p.height - 340);
            float cw = (view.width - 28) / 2;
            const float ch = 184;
            int rows = Mathf.CeilToInt(merchantOffers.Count / 2f);
            merchantScroll = GUI.BeginScrollView(view, merchantScroll, new Rect(0, 0, view.width - 18, rows * (ch + 10)));
            for (int i = 0; i < merchantOffers.Count; i++)
            {
                MerchantOffer offer = merchantOffers[i];
                Rect card = new Rect((i % 2) * (cw + 10), (i / 2) * (ch + 10), cw, ch);
                if (GUI.Button(card, GUIContent.none, buttonStyle))
                {
                    portraitMerchantSelection = i;
                    portraitMerchantDetailScroll = Vector2.zero;
                }
                if (portraitMerchantSelection == i) WuxiaUiTheme.DrawOutline(new Rect(card.x + 3, card.y + 3, card.width - 6, card.height - 6), Gold, 2);
                Texture icon = MartialArtIconRenderer.Get(LoadContentIcon(offer.iconId), offer.contentId);
                Rect iconRect = new Rect(card.x + 12, card.y + 12, 46, 46);
                if (icon != null) GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
                WuxiaUiComponents.Text(new Rect(card.x + 66, card.y + 14, card.width - 76, 42), OfferTypeName(offer.type), 14, WuxiaUiTheme.TextSecondary, TextAnchor.MiddleLeft, true);
                WuxiaUiComponents.Text(new Rect(card.x + 12, card.y + 66, card.width - 24, 26), offer.displayName, 18);
                WuxiaUiComponents.Text(new Rect(card.x + 12, card.y + 98, card.width - 24, 48), offer.description, 14, WuxiaUiTheme.TextSecondary, TextAnchor.UpperLeft, true);
                WuxiaUiComponents.Text(new Rect(card.x + 12, card.yMax - 30, card.width - 24, 24), offer.sold ? "已售罄" : $"{offer.price} 铜钱", 16, offer.sold ? WuxiaUiTheme.TextDisabled : Gold);
            }
            GUI.EndScrollView();
            portraitMerchantSelection = Mathf.Clamp(portraitMerchantSelection, 0, Mathf.Max(0, merchantOffers.Count - 1));
            if (merchantOffers.Count > 0)
            {
                MerchantOffer selected = merchantOffers[portraitMerchantSelection];
                Rect detail = new Rect(p.x + 24, p.yMax - 226, p.width - 48, 88);
                WuxiaUiTheme.DrawCompactSurface(detail, WuxiaUiTheme.BackgroundInk, Gold);
                string description = selected.displayName + " · " + selected.description;
                string comparison = EquipmentComparison(selected);
                if (!string.IsNullOrEmpty(comparison)) description += "\n" + comparison;
                Rect detailView = new Rect(detail.x + 12, detail.y + 8, detail.width - 24, detail.height - 16);
                float detailHeight = Mathf.Max(detailView.height, bodyStyle.CalcHeight(new GUIContent(description), detailView.width - 22) + 8);
                portraitMerchantDetailScroll = GUI.BeginScrollView(detailView, portraitMerchantDetailScroll,
                    new Rect(0, 0, detailView.width - 22, detailHeight));
                WuxiaUiComponents.Text(new Rect(0, 0, detailView.width - 22, detailHeight), description,
                    14, WuxiaUiTheme.TextPrimary, TextAnchor.UpperLeft, true);
                GUI.EndScrollView();
                bool oldEnabled = GUI.enabled;
                GUI.enabled = !selected.sold && playerStats.copper >= selected.price;
                string label = selected.sold ? "已售罄" : playerStats.copper < selected.price ? "铜钱不足" : $"购买 {selected.displayName} · {selected.price} 铜";
                if (GUI.Button(new Rect(p.x + 24, p.yMax - 126, p.width - 48, 50), label, WuxiaUiComponents.TouchButton(true))) PurchaseOffer(selected);
                GUI.enabled = oldEnabled;
            }
            float bw = (p.width - 60) / 2;
            GUI.enabled = !merchantRefreshed && playerStats.copper >= 5;
            if (GUI.Button(new Rect(p.x + 24, p.yMax - 64, bw, 44), merchantRefreshed ? "本洞已刷新" : "刷新 · 5 铜钱", touchStyle))
            {
                if (playerStats.TrySpendCopper(5)) { merchantRefreshed = true; BuildMerchantStock(); portraitMerchantSelection = 0; }
            }
            GUI.enabled = true;
            if (GUI.Button(new Rect(p.xMax - 24 - bw, p.yMax - 64, bw, 44), "结束交易", touchStyle)) FinishMerchantEvent();
        }

        private void FinishMerchantEvent()
        {
            merchantOpen = false;
            eventStarted = false;
            eventCompleted = false;
            merchantAwaitingReapproach = true;
            roomMessage = "交易暂歇。离开商人后再次靠近可继续交易，也可从左下石门撤离洞穴。";
        }

        private void DrawMerchantCard(Rect card, MerchantOffer offer)
        {
            Color accent = offer.discounted ? new Color(0.92f, 0.47f, 0.24f) : Gold;
            WuxiaUiTheme.DrawPanel(card,
                offer.sold
                    ? new Color(0.09f, 0.10f, 0.095f, 1f)
                    : new Color(0.13f, 0.145f, 0.135f, 1f),
                accent,
                WuxiaPanelKind.Default);
            Texture icon = MartialArtIconRenderer.Get(LoadContentIcon(offer.iconId), offer.contentId);
            Rect iconRect = new Rect(card.x + 8f, card.y + 10f, 48f, 48f);
            WuxiaUiTheme.DrawSlot(iconRect,
                new Color(0.035f, 0.04f, 0.04f, 0.95f), accent);
            if (icon != null)
            {
                GUI.DrawTexture(new Rect(iconRect.x + 3f, iconRect.y + 3f, 42f, 42f),
                    icon, ScaleMode.ScaleToFit, true);
            }
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(card.x + 62f, card.y + 8f, card.width - 70f, 20f),
                OfferTypeName(offer.type), hintStyle, 8);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(card.x + 62f, card.y + 29f, card.width - 70f, 27f),
                offer.displayName, headingStyle, 9);
            GUI.Label(new Rect(card.x + 9f, card.y + 64f, card.width - 18f, 52f),
                offer.description, bodyStyle);

            string comparison = EquipmentComparison(offer);
            if (!string.IsNullOrEmpty(comparison))
            {
                ResponsiveGui.DrawSingleLineLabel(
                    new Rect(card.x + 9f, card.y + 115f, card.width - 18f, 18f),
                    comparison, hintStyle, 8);
            }

            bool wasEnabled = GUI.enabled;
            GUI.enabled = !offer.sold && playerStats.copper >= offer.price;
            string priceText = offer.sold
                ? "已售"
                : offer.discounted
                    ? $"特价 {offer.price} 铜"
                    : $"{offer.price} 铜";
            if (GUI.Button(new Rect(card.x + 9f, card.yMax - 31f, card.width - 18f, 25f), priceText, buttonStyle))
            {
                PurchaseOffer(offer);
            }
            GUI.enabled = wasEnabled;
        }

        private void PurchaseOffer(MerchantOffer offer)
        {
            if (offer == null || offer.sold || !playerStats.TrySpendCopper(offer.price))
            {
                return;
            }

            bool success = true;
            string result = offer.displayName;
            switch (offer.type)
            {
                case MerchantOfferType.MartialArt:
                    if (!gameFlow.IsMartialArtEligible(offer.contentId))
                    {
                        success = false;
                        break;
                    }
                    int rank = playerStats.ApplyMartialArt(offer.contentId);
                    result = $"《{offer.displayName}》修至 {rank} 重";
                    break;
                case MerchantOfferType.Equipment:
                    result = playerStats.equipment != null
                        ? playerStats.equipment.AddItemById(offer.contentId)
                        : string.Empty;
                    success = !string.IsNullOrEmpty(result);
                    break;
                case MerchantOfferType.Relic:
                    success = playerStats.GrantRelic(offer.contentId);
                    break;
                case MerchantOfferType.Consumable:
                    success = playerStats.ApplyConsumable(offer.contentId);
                    break;
                case MerchantOfferType.UpgradeService:
                    result = playerStats.UpgradeRandomMartialArt();
                    success = !string.IsNullOrEmpty(result);
                    break;
                case MerchantOfferType.TransformService:
                    result = gameFlow.GrantCrossSchoolMartialArt();
                    success = !string.IsNullOrEmpty(result);
                    break;
            }

            if (!success)
            {
                playerStats.GainCopper(offer.price);
                roomMessage = "这件货物当前无法使用，铜钱已经退回。";
                return;
            }

            offer.sold = true;
            roomMessage = $"交易完成：{result}。仍可继续挑选。";
        }

        private void BuildMerchantStock()
        {
            merchantOffers.Clear();

            List<string> arts = gameFlow != null
                ? gameFlow.GetMerchantMartialArtCandidates()
                : new List<string>();
            Shuffle(arts);
            for (int i = 0; i < Mathf.Min(4, arts.Count); i++)
            {
                string artId = arts[i];
                MartialArtDefinition definition = MartialArtCatalog.Get(artId);
                int nextRank = playerStats.GetMartialArtRank(artId) + 1;
                AddMerchantOffer(MerchantOfferType.MartialArt, artId, artId,
                    definition?.GetEffectSummary(nextRank) ?? "获得一门武学",
                    ContentIconCatalog.MartialArt(artId), 12 + Mathf.Clamp(nextRank - 1, 0, 2) * 3);
            }

            List<string> equipmentIds = new List<string>(PlayerEquipment.TreasureItemIds);
            if (playerStats.equipment != null)
            {
                equipmentIds.RemoveAll(playerStats.equipment.HasItem);
            }
            Shuffle(equipmentIds);
            if (equipmentIds.Count > 0 && playerStats.equipment != null)
            {
                EquipmentItem item = playerStats.equipment.GetTemplate(equipmentIds[0]);
                if (item != null)
                {
                    AddMerchantOffer(MerchantOfferType.Equipment, item.id, item.displayName,
                        item.BonusSummary, ContentIconCatalog.Equipment(item.id), 13);
                }
            }

            string relicId = RandomUnownedRelic();
            RunRelicDefinition relic = RunContentCatalog.GetRelic(relicId);
            if (relic != null)
            {
                AddMerchantOffer(MerchantOfferType.Relic, relic.id, relic.displayName,
                    relic.description, relic.iconId, 14);
            }
            else if (equipmentIds.Count > 1 && playerStats.equipment != null)
            {
                EquipmentItem secondItem = playerStats.equipment.GetTemplate(equipmentIds[1]);
                if (secondItem != null)
                {
                    AddMerchantOffer(MerchantOfferType.Equipment, secondItem.id, secondItem.displayName,
                        secondItem.BonusSummary, ContentIconCatalog.Equipment(secondItem.id), 14);
                }
            }

            List<string> consumables = new List<string>(RunContentCatalog.AllConsumableIds);
            Shuffle(consumables);
            for (int i = 0; i < Mathf.Min(2, consumables.Count); i++)
            {
                RunConsumableDefinition consumable = RunContentCatalog.GetConsumable(consumables[i]);
                if (consumable != null)
                {
                    AddMerchantOffer(MerchantOfferType.Consumable, consumable.id, consumable.displayName,
                        consumable.description, consumable.iconId, 6 + i * 2);
                }
            }

            AddMerchantOffer(MerchantOfferType.UpgradeService, "upgrade_service", "灌顶升诀",
                "随机提升一门尚未满重的已学武学", "store_upgrade", 12);
            AddMerchantOffer(MerchantOfferType.TransformService, "transform_service", "散功换诀",
                "获得一门不同于主流派的可学武学", "store_transform", 10);

            if (merchantOffers.Count > 0)
            {
                int discountIndex = Random.Range(0, Mathf.Min(8, merchantOffers.Count));
                merchantOffers[discountIndex].discounted = true;
                merchantOffers[discountIndex].price = Mathf.Max(1,
                    Mathf.CeilToInt(merchantOffers[discountIndex].price * 0.8f));
            }
        }

        private void AddMerchantOffer(MerchantOfferType type, string contentId, string displayName,
            string description, string iconId, int price)
        {
            merchantOffers.Add(new MerchantOffer
            {
                type = type,
                contentId = contentId,
                displayName = displayName,
                description = description,
                iconId = iconId,
                price = Mathf.Max(1, price)
            });
        }

        private string RandomUnownedRelic()
        {
            if (playerStats.relics.Count >= 2)
            {
                return string.Empty;
            }

            List<string> candidates = new List<string>(RunContentCatalog.AllRelicIds);
            candidates.RemoveAll(playerStats.HasRelic);
            return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : string.Empty;
        }

        private string EquipmentComparison(MerchantOffer offer)
        {
            if (offer.type != MerchantOfferType.Equipment || playerStats.equipment == null)
            {
                return offer.type == MerchantOfferType.MartialArt && offer.discounted ? "掌柜今日特价" : string.Empty;
            }

            EquipmentItem candidate = playerStats.equipment.GetTemplate(offer.contentId);
            return candidate == null
                ? string.Empty
                : playerStats.equipment.IsUpgrade(candidate)
                    ? "较当前同槽装备更强"
                    : "侧重不同触发效果";
        }

        private static Texture2D LoadContentIcon(string iconId)
        {
            return string.IsNullOrEmpty(iconId)
                ? null
                : Resources.Load<Texture2D>("Icons/" + iconId);
        }

        private static string OfferTypeName(MerchantOfferType type)
        {
            switch (type)
            {
                case MerchantOfferType.MartialArt: return "武学";
                case MerchantOfferType.Equipment: return "装备";
                case MerchantOfferType.Relic: return "遗物";
                case MerchantOfferType.Consumable: return "药品";
                default: return "服务";
            }
        }

        private static void Shuffle<T>(List<T> values)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int swapIndex = Random.Range(0, i + 1);
                (values[i], values[swapIndex]) = (values[swapIndex], values[i]);
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
                case CaveContentType.Treasure:
                    return "石室深处有一只古匣，靠近即可开启。";
                default:
                    return $"洞窟深处传来异响，靠近{ContentName()}触发事件。";
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
                case CaveContentType.Treasure:
                    return "秘藏古匣";
                case CaveContentType.Altar: return "血契石坛";
                case CaveContentType.Trial: return "隐窟试炼";
                case CaveContentType.Healer: return "药庐残火";
                case CaveContentType.Library: return "残卷石室";
                case CaveContentType.Forge: return "铸兵台";
                case CaveContentType.Gambler: return "盲匣赌局";
                case CaveContentType.HerbGarden: return "地脉药圃";
                default: return "供器神龛";
            }
        }

        private CaveContentType SelectRandomContent()
        {
            if (Random.value < 0.30f)
            {
                CaveContentType[] specialRooms =
                {
                    CaveContentType.Altar, CaveContentType.Trial, CaveContentType.Healer,
                    CaveContentType.Library, CaveContentType.Forge, CaveContentType.Gambler,
                    CaveContentType.HerbGarden, CaveContentType.RelicShrine
                };
                return specialRooms[Random.Range(0, specialRooms.Length)];
            }

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
            buttonStyle = WuxiaUiTheme.CreateButtonStyle(
                13, WuxiaButtonKind.Secondary);
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
