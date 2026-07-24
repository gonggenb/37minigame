using System;
using System.Linq;
using UnityEngine;
using WuxiaRoguelite.Battle;
using WuxiaRoguelite.Cave;
using WuxiaRoguelite.GameFlow;
using WuxiaRoguelite.MartialArts;
using WuxiaRoguelite.Player;

namespace WuxiaRoguelite.UI
{
    public class PrototypeHUDController : MonoBehaviour
    {
        [Serializable]
        public class IconEntry
        {
            public string id;
            public Texture2D icon;
        }

        private enum CharacterView
        {
            Status,
            Equipment
        }

        public GameFlowController gameFlow;
        public PlayerStats playerStats;
        public BattleManager battleManager;
        public Texture2D statusIcon;
        public Texture2D equipmentIcon;
        public Texture2D healthBarBase;
        public Texture2D healthBarFill;
        public IconEntry[] martialArtIcons;
        public IconEntry[] equipmentItemIcons;

        private GUIStyle titleStyle;
        private GUIStyle headingStyle;
        private GUIStyle bodyStyle;
        private GUIStyle mutedStyle;
        private GUIStyle centeredStyle;
        private GUIStyle iconButtonStyle;
        private GUIStyle tabStyle;
        private GUIStyle activeTabStyle;
        private GUIStyle actionButtonStyle;
        private GUIStyle tooltipEffectStyle;
        private bool characterPanelOpen;
        private bool debugVisible;
        private CharacterView currentView;
        private Vector2 statusScroll;
        private Vector2 inventoryScroll;

        private static readonly Color Ink = new Color(0.055f, 0.065f, 0.07f, 0.94f);
        private static readonly Color Panel = new Color(0.09f, 0.105f, 0.105f, 0.97f);
        private static readonly Color PanelLight = new Color(0.14f, 0.16f, 0.15f, 0.98f);
        private static readonly Color Jade = new Color(0.27f, 0.68f, 0.53f, 1f);
        private static readonly Color Gold = new Color(0.86f, 0.68f, 0.32f, 1f);
        private static readonly Color Paper = new Color(0.92f, 0.88f, 0.74f, 1f);
        private static readonly Color Muted = new Color(0.66f, 0.70f, 0.67f, 1f);

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                debugVisible = !debugVisible;
            }

            if (gameFlow != null && gameFlow.CurrentPhase == GamePhase.MainMapRunning)
            {
                if (characterPanelOpen && Input.GetKeyDown(KeyCode.Escape))
                {
                    SetCharacterScreenOpen(false);
                }
                else if (Input.GetKeyDown(KeyCode.C))
                {
                    ToggleCharacterPanel(CharacterView.Status);
                }
                else if (Input.GetKeyDown(KeyCode.B))
                {
                    ToggleCharacterPanel(CharacterView.Equipment);
                }
            }

            if (gameFlow != null && gameFlow.CurrentPhase != GamePhase.MainMapRunning)
            {
                SetCharacterScreenOpen(false);
            }
        }

        private void OnDisable()
        {
            SetCharacterScreenOpen(false);
        }

        private void OnGUI()
        {
            if (gameFlow == null || playerStats == null || playerStats.runtimeStats == null)
            {
                return;
            }

            if (battleManager != null && battleManager.IsBattleActive)
            {
                return;
            }

            GUI.depth = -500;
            EnsureStyles();
            if (gameFlow.CurrentPhase != GamePhase.Result && gameFlow.CurrentPhase != GamePhase.CaveRunning && !characterPanelOpen)
            {
                DrawCompactHud();
            }

            if (gameFlow.CurrentPhase == GamePhase.MainMapRunning)
            {
                if (characterPanelOpen)
                {
                    DrawCharacterScreen();
                }
                else
                {
                    DrawCharacterButtons();
                }
            }

            if (gameFlow.CurrentPhase == GamePhase.LevelUpPaused)
            {
                DrawLevelUpPanel();
            }
            else if (gameFlow.CurrentPhase == GamePhase.Result)
            {
                DrawResultPanel();
            }

            if (debugVisible && !characterPanelOpen)
            {
                DrawDebugControls();
            }
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = LabelStyle(22, FontStyle.Bold, TextAnchor.MiddleLeft, Paper);
            headingStyle = LabelStyle(16, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            bodyStyle = LabelStyle(14, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white);
            mutedStyle = LabelStyle(12, FontStyle.Normal, TextAnchor.MiddleLeft, Muted);
            centeredStyle = LabelStyle(14, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);

            iconButtonStyle = new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(7, 7, 7, 7),
                fixedWidth = 48f,
                fixedHeight = 48f
            };
            tabStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Muted }
            };
            activeTabStyle = new GUIStyle(tabStyle);
            activeTabStyle.normal.textColor = Paper;
            actionButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            tooltipEffectStyle = LabelStyle(15, FontStyle.Bold, TextAnchor.UpperLeft,
                new Color(1f, 0.80f, 0.35f));
        }

        private void DrawCompactHud()
        {
            Rect hud = new Rect(14f, 14f, 210f, 66f);
            DrawPanel(hud, Ink, Jade);
            GUI.Label(new Rect(hud.x + 12f, hud.y + 4f, 132f, 25f), $"江湖余时  {gameFlow.mainTimeRemaining:0.0}", headingStyle);
            GUI.Label(new Rect(hud.xMax - 60f, hud.y + 5f, 48f, 23f), $"Lv.{playerStats.level}", centeredStyle);

            GUI.Label(new Rect(hud.x + 12f, hud.y + 29f, hud.width - 24f, 16f),
                $"气血  {playerStats.runtimeStats.currentHealth:0}/{playerStats.runtimeStats.maxHealth:0}", mutedStyle);
            Rect healthRect = new Rect(hud.x + 12f, hud.y + 47f, hud.width - 24f, 10f);
            DrawHealthBar(healthRect, playerStats.runtimeStats.HealthRatio);

            Rect resources = new Rect(14f, 86f, 210f, 25f);
            DrawPanel(resources, new Color(0.04f, 0.05f, 0.05f, 0.9f), Gold);
            GUI.Label(new Rect(resources.x + 10f, resources.y + 1f, resources.width - 20f, resources.height - 2f),
                $"修为 {playerStats.cultivation}/{playerStats.NextLevelRequirement}    铜钱 {playerStats.copper}", mutedStyle);

            float statusWidth = Mathf.Min(360f, Screen.width - 28f);
            Rect message = new Rect((Screen.width - statusWidth) * 0.5f, Screen.height - 40f, statusWidth, 26f);
            DrawPanel(message, new Color(0.03f, 0.04f, 0.04f, 0.84f), Gold);
            GUI.Label(new Rect(message.x + 10f, message.y + 2f, message.width - 20f, message.height - 4f), gameFlow.statusMessage, bodyStyle);
        }

        private void DrawCharacterButtons()
        {
            Rect statusRect = new Rect(Screen.width - 58f, 14f, 48f, 48f);
            Rect equipmentRect = new Rect(Screen.width - 58f, 68f, 48f, 48f);
            if (GUI.Button(statusRect, new GUIContent(statusIcon, "角色状态"), iconButtonStyle))
            {
                ToggleCharacterPanel(CharacterView.Status);
            }
            if (GUI.Button(equipmentRect, new GUIContent(equipmentIcon, "装备背包"), iconButtonStyle))
            {
                ToggleCharacterPanel(CharacterView.Equipment);
            }

            if (!string.IsNullOrEmpty(GUI.tooltip))
            {
                Vector2 size = bodyStyle.CalcSize(new GUIContent(GUI.tooltip));
                Rect tooltip = new Rect(Input.mousePosition.x - size.x - 16f, Screen.height - Input.mousePosition.y + 12f, size.x + 12f, 24f);
                FillRect(tooltip, Ink);
                GUI.Label(new Rect(tooltip.x + 6f, tooltip.y, size.x, tooltip.height), GUI.tooltip, bodyStyle);
            }
        }

        private void ToggleCharacterPanel(CharacterView view)
        {
            bool sameOpenView = characterPanelOpen && currentView == view;
            currentView = view;
            SetCharacterScreenOpen(!sameOpenView);
        }

        private void SetCharacterScreenOpen(bool open)
        {
            if (gameFlow == null)
            {
                characterPanelOpen = false;
                return;
            }

            if (open && gameFlow.CurrentPhase != GamePhase.MainMapRunning)
            {
                return;
            }

            characterPanelOpen = open;
            gameFlow.SetCharacterMenuPaused(open);
        }

        private void DrawCharacterScreen()
        {
            FillRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.025f, 0.03f, 0.03f, 0.98f));
            Rect panel = CenteredRect(760f, 460f);
            DrawPanel(panel, Panel, currentView == CharacterView.Status ? Jade : Gold);

            GUI.Label(new Rect(panel.x + 18f, panel.y + 9f, panel.width - 210f, 34f), "侠客档案", titleStyle);
            GUI.Label(new Rect(panel.xMax - 174f, panel.y + 11f, 116f, 28f), "江湖暂停", centeredStyle);
            if (GUI.Button(new Rect(panel.xMax - 44f, panel.y + 10f, 28f, 28f), "×", actionButtonStyle))
            {
                SetCharacterScreenOpen(false);
                return;
            }

            float tabY = panel.y + 50f;
            float tabWidth = Mathf.Min(150f, (panel.width - 36f) * 0.5f);
            if (GUI.Button(new Rect(panel.x + 18f, tabY, tabWidth, 32f), "角色状态", currentView == CharacterView.Status ? activeTabStyle : tabStyle))
            {
                currentView = CharacterView.Status;
            }
            if (GUI.Button(new Rect(panel.x + 22f + tabWidth, tabY, tabWidth, 32f), "装备背包", currentView == CharacterView.Equipment ? activeTabStyle : tabStyle))
            {
                currentView = CharacterView.Equipment;
            }

            Rect content = new Rect(panel.x + 18f, tabY + 44f, panel.width - 36f, panel.height - 108f);
            if (currentView == CharacterView.Status)
            {
                DrawStatus(content);
            }
            else
            {
                DrawEquipment(content);
            }
        }


        private void DrawStatus(Rect rect)
        {
            const float contentHeight = 350f;
            bool needsScroll = rect.height < contentHeight;
            if (needsScroll)
            {
                statusScroll = GUI.BeginScrollView(rect, statusScroll, new Rect(0f, 0f, rect.width - 18f, contentHeight));
                rect = new Rect(0f, 0f, rect.width - 22f, contentHeight);
            }
            if (statusIcon != null)
            {
                GUI.DrawTexture(new Rect(rect.x, rect.y, 58f, 58f), statusIcon, ScaleMode.ScaleToFit, true);
            }
            GUI.Label(new Rect(rect.x + 68f, rect.y, rect.width - 68f, 24f), playerStats.runtimeStats.displayName, headingStyle);
            GUI.Label(new Rect(rect.x + 68f, rect.y + 25f, rect.width - 68f, 18f),
                $"等级 {playerStats.level}  ·  击杀 {playerStats.killCount}  ·  洞穴 {playerStats.caveEntries}", mutedStyle);
            GUI.Label(new Rect(rect.x + 68f, rect.y + 44f, rect.width - 68f, 18f),
                $"修为 {playerStats.cultivation}/{playerStats.NextLevelRequirement}  ·  铜钱 {playerStats.copper}", bodyStyle);

            float y = rect.y + 72f;
            DrawStatRow(new Rect(rect.x, y, rect.width, 27f), "气血", $"{playerStats.runtimeStats.currentHealth:0}/{playerStats.runtimeStats.maxHealth:0}", "攻击", playerStats.runtimeStats.attack.ToString("0"));
            DrawStatRow(new Rect(rect.x, y + 31f, rect.width, 27f), "防御", playerStats.runtimeStats.defense.ToString("0"), "攻速", playerStats.runtimeStats.attackSpeed.ToString("0.00"));
            DrawStatRow(new Rect(rect.x, y + 62f, rect.width, 27f), "暴击", $"{playerStats.runtimeStats.critChance * 100f:0.#}%", "闪避", $"{playerStats.runtimeStats.dodgeChance * 100f:0.#}%");
            DrawStatRow(new Rect(rect.x, y + 93f, rect.width, 27f), "吸血", $"{playerStats.runtimeStats.lifeSteal * 100f:0.#}%", "移速", playerStats.runtimeStats.moveSpeed.ToString("0.0"));

            GUI.Label(new Rect(rect.x, y + 130f, rect.width, 22f), "已习武学", headingStyle);
            if (playerStats.learnedMartialArts.Count == 0)
            {
                GUI.Label(new Rect(rect.x, y + 154f, rect.width, 38f), "尚未习得", bodyStyle);
            }
            else
            {
                string[] learned = playerStats.learnedMartialArts.Take(6).ToArray();
                const float gap = 8f;
                float tileWidth = (rect.width - gap * 2f) / 3f;
                for (int i = 0; i < learned.Length; i++)
                {
                    int column = i % 3;
                    int row = i / 3;
                    Rect tile = new Rect(rect.x + column * (tileWidth + gap),
                        y + 154f + row * 54f, tileWidth, 48f);
                    DrawMartialArtTile(tile, learned[i]);
                }
            }
            if (needsScroll)
            {
                GUI.EndScrollView();
            }
        }

        private void DrawStatRow(Rect rect, string leftLabel, string leftValue, string rightLabel, string rightValue)
        {
            FillRect(rect, PanelLight);
            float half = rect.width * 0.5f;
            GUI.Label(new Rect(rect.x + 10f, rect.y, half - 20f, rect.height), $"{leftLabel}  {leftValue}", bodyStyle);
            GUI.Label(new Rect(rect.x + half + 10f, rect.y, half - 20f, rect.height), $"{rightLabel}  {rightValue}", bodyStyle);
        }

        private void DrawEquipment(Rect rect)
        {
            PlayerEquipment equipment = playerStats.equipment;
            if (equipment == null)
            {
                GUI.Label(rect, "装备系统未连接", centeredStyle);
                return;
            }

            GUI.Label(new Rect(rect.x, rect.y, rect.width, 24f), "当前穿戴", headingStyle);
            const float slotGap = 8f;
            float slotWidth = (rect.width - slotGap * 2f) / 3f;
            DrawEquippedSlot(new Rect(rect.x, rect.y + 27f, slotWidth, 58f), equipment, EquipmentSlot.Weapon);
            DrawEquippedSlot(new Rect(rect.x + slotWidth + slotGap, rect.y + 27f, slotWidth, 58f), equipment, EquipmentSlot.Armor);
            DrawEquippedSlot(new Rect(rect.x + (slotWidth + slotGap) * 2f, rect.y + 27f, slotWidth, 58f), equipment, EquipmentSlot.Accessory);

            float inventoryY = rect.y + 94f;
            GUI.Label(new Rect(rect.x, inventoryY, rect.width, 24f), $"背包  {equipment.inventory.Count}", headingStyle);
            Rect viewport = new Rect(rect.x, inventoryY + 28f, rect.width, Mathf.Max(68f, rect.yMax - inventoryY - 28f));
            float contentHeight = equipment.inventory.Count * 66f;
            inventoryScroll = GUI.BeginScrollView(viewport, inventoryScroll, new Rect(0f, 0f, viewport.width - 18f, contentHeight));
            for (int i = 0; i < equipment.inventory.Count; i++)
            {
                EquipmentItem item = equipment.inventory[i];
                DrawInventoryItem(new Rect(0f, i * 66f, viewport.width - 22f, 60f), equipment, item);
            }
            GUI.EndScrollView();
        }

        private void DrawEquippedSlot(Rect rect, PlayerEquipment equipment, EquipmentSlot slot)
        {
            FillRect(rect, PanelLight);
            EquipmentItem item = equipment.GetEquipped(slot);
            GUI.Label(new Rect(rect.x + 7f, rect.y + 1f, rect.width - 14f, 18f), SlotName(slot), mutedStyle);
            if (item != null)
            {
                DrawIcon(new Rect(rect.x + 7f, rect.y + 20f, 32f, 32f), FindEquipmentIcon(item.id),
                    RarityColor(item.rarity));
            }
            GUI.Label(new Rect(rect.x + 45f, rect.y + 19f, rect.width - 103f, 34f),
                item == null ? "未装备" : item.displayName, bodyStyle);
            if (item != null && GUI.Button(new Rect(rect.xMax - 54f, rect.y + 23f, 47f, 25f), "卸下", actionButtonStyle))
            {
                equipment.Unequip(slot);
            }
        }

        private void DrawInventoryItem(Rect rect, PlayerEquipment equipment, EquipmentItem item)
        {
            FillRect(rect, new Color(0.12f, 0.14f, 0.13f, 1f));
            DrawIcon(new Rect(rect.x + 7f, rect.y + 6f, 48f, 48f), FindEquipmentIcon(item.id),
                RarityColor(item.rarity));
            Color previous = GUI.contentColor;
            GUI.contentColor = RarityColor(item.rarity);
            GUI.Label(new Rect(rect.x + 63f, rect.y + 5f, rect.width - 146f, 23f), item.displayName, headingStyle);
            GUI.contentColor = previous;
            GUI.Label(new Rect(rect.x + 63f, rect.y + 30f, rect.width - 146f, 22f), item.BonusSummary, mutedStyle);

            bool equipped = equipment.IsEquipped(item);
            GUI.enabled = !equipped;
            if (GUI.Button(new Rect(rect.xMax - 72f, rect.y + 15f, 62f, 30f), equipped ? "已装备" : "装备", actionButtonStyle))
            {
                equipment.Equip(item);
            }
            GUI.enabled = true;
        }

        private void DrawLevelUpPanel()
        {
            FillRect(new Rect(0f, 0f, Screen.width, Screen.height),
                new Color(0.02f, 0.025f, 0.025f, 0.72f));
            Rect panel = CenteredRect(640f, 300f);
            DrawPanel(panel, new Color(0.09f, 0.105f, 0.105f, 1f), Gold);
            GUI.Label(new Rect(panel.x + 18f, panel.y + 12f, panel.width - 36f, 32f), "修为突破", titleStyle);

            float detailsWidth = Mathf.Clamp(panel.width * 0.39f, 210f, 244f);
            Rect choicesArea = new Rect(panel.x + 18f, panel.y + 54f,
                panel.width - detailsWidth - 50f, panel.height - 72f);
            Rect detailsArea = new Rect(choicesArea.xMax + 14f, choicesArea.y,
                detailsWidth, choicesArea.height);
            string hoveredArt = null;

            for (int i = 0; i < gameFlow.currentChoices.Count; i++)
            {
                string artId = gameFlow.currentChoices[i];
                Rect card = new Rect(choicesArea.x, choicesArea.y + i * 72f,
                    choicesArea.width, 62f);
                if (card.Contains(Event.current.mousePosition))
                {
                    hoveredArt = artId;
                }

                bool selected = GUI.Button(card, GUIContent.none, actionButtonStyle);
                DrawIcon(new Rect(card.x + 7f, card.y + 7f, 48f, 48f),
                    FindMartialArtIcon(artId), CategoryColor(MartialArtCatalog.Get(artId)?.category));
                GUI.Label(new Rect(card.x + 65f, card.y + 5f, card.width - 74f, 26f),
                    artId, headingStyle);
                MartialArtDefinition definition = MartialArtCatalog.Get(artId);
                GUI.Label(new Rect(card.x + 65f, card.y + 31f, card.width - 74f, 22f),
                    definition?.effectSummary ?? "查看效果", mutedStyle);

                if (selected)
                {
                    gameFlow.ChooseMartialArt(i);
                    return;
                }
            }

            if (hoveredArt == null && gameFlow.currentChoices.Count > 0)
            {
                hoveredArt = gameFlow.currentChoices[0];
            }
            DrawMartialArtTooltip(detailsArea, hoveredArt);
        }

        private void DrawMartialArtTooltip(Rect rect, string artId)
        {
            FillRect(rect, new Color(0.055f, 0.065f, 0.06f, 0.98f));
            FillRect(new Rect(rect.x, rect.y, 3f, rect.height), Gold);
            MartialArtDefinition definition = MartialArtCatalog.Get(artId);
            if (definition == null)
            {
                GUI.Label(new Rect(rect.x + 14f, rect.y + 22f, rect.width - 28f, 28f),
                    "悬停武学查看效果", headingStyle);
                GUI.Label(new Rect(rect.x + 14f, rect.y + 58f, rect.width - 28f, 78f),
                    "选择后会立即获得本局属性加成；突破期间所有时间暂停。", mutedStyle);
                return;
            }

            GUI.Label(new Rect(rect.x + 14f, rect.y + 10f, rect.width - 28f, 28f),
                definition.id, headingStyle);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 40f, rect.width - 28f, 20f),
                definition.category, mutedStyle);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 68f, rect.width - 28f, 44f),
                definition.effectSummary, tooltipEffectStyle);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 116f, rect.width - 28f, rect.height - 126f),
                definition.description, bodyStyle);
        }

        private void DrawMartialArtTile(Rect rect, string artId)
        {
            FillRect(rect, PanelLight);
            MartialArtDefinition definition = MartialArtCatalog.Get(artId);
            DrawIcon(new Rect(rect.x + 5f, rect.y + 5f, 38f, 38f),
                FindMartialArtIcon(artId), CategoryColor(definition?.category));
            GUI.Label(new Rect(rect.x + 49f, rect.y + 2f, rect.width - 54f, 22f),
                artId, bodyStyle);
            GUI.Label(new Rect(rect.x + 49f, rect.y + 23f, rect.width - 54f, 20f),
                definition?.effectSummary ?? string.Empty, mutedStyle);
        }

        private static void DrawIcon(Rect rect, Texture2D icon, Color accent)
        {
            FillRect(rect, new Color(0.035f, 0.04f, 0.04f, 0.95f));
            FillRect(new Rect(rect.x, rect.y, 2f, rect.height), accent);
            if (icon != null)
            {
                GUI.DrawTexture(new Rect(rect.x + 3f, rect.y + 3f, rect.width - 6f, rect.height - 6f),
                    icon, ScaleMode.ScaleToFit, true);
            }
        }

        private Texture2D FindMartialArtIcon(string id)
        {
            return FindIcon(martialArtIcons, id);
        }

        private Texture2D FindEquipmentIcon(string id)
        {
            return FindIcon(equipmentItemIcons, id);
        }

        private static Texture2D FindIcon(IconEntry[] entries, string id)
        {
            if (entries == null || string.IsNullOrEmpty(id))
            {
                return null;
            }

            foreach (IconEntry entry in entries)
            {
                if (entry != null && entry.id == id)
                {
                    return entry.icon;
                }
            }

            return null;
        }

        private static Color CategoryColor(string category)
        {
            switch (category)
            {
                case "内功":
                    return new Color(0.38f, 0.72f, 0.58f);
                case "身法":
                    return new Color(0.36f, 0.64f, 0.86f);
                default:
                    return new Color(0.82f, 0.34f, 0.24f);
            }
        }

        private void DrawResultPanel()
        {
            FillRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.02f, 0.025f, 0.025f, 0.78f));
            Rect panel = CenteredRect(380f, 190f);
            DrawPanel(panel, Panel, gameFlow.bossDefeated ? Jade : new Color(0.72f, 0.25f, 0.20f));
            GUI.Label(new Rect(panel.x + 18f, panel.y + 12f, panel.width - 36f, 36f), gameFlow.bossDefeated ? "闯关功成" : "江湖路断", titleStyle);
            GUI.Label(new Rect(panel.x + 18f, panel.y + 54f, panel.width - 36f, 24f), gameFlow.statusMessage, bodyStyle);
            GUI.Label(new Rect(panel.x + 18f, panel.y + 82f, panel.width - 36f, 22f),
                $"等级 {playerStats.level}  ·  击杀 {playerStats.killCount}  ·  洞穴 {playerStats.caveEntries}", mutedStyle);
            if (GUI.Button(new Rect(panel.x + 18f, panel.yMax - 52f, panel.width - 36f, 36f), "再入江湖", actionButtonStyle))
            {
                gameFlow.StartRun();
            }
        }

        private void DrawDebugControls()
        {
            Rect panel = new Rect(14f, 106f, 180f, 238f);
            DrawPanel(panel, Ink, new Color(0.55f, 0.55f, 0.55f));
            if (GUI.Button(new Rect(panel.x + 8f, panel.y + 8f, panel.width - 16f, 26f), "重新开始")) gameFlow.StartRun();
            if (GUI.Button(new Rect(panel.x + 8f, panel.y + 40f, panel.width - 16f, 26f), "增加修为")) gameFlow.AddDebugCultivation();
            if (GUI.Button(new Rect(panel.x + 8f, panel.y + 72f, panel.width - 16f, 26f), "增加战力")) gameFlow.AddDebugPower();
            if (GUI.Button(new Rect(panel.x + 8f, panel.y + 104f, panel.width - 16f, 26f), "进入 Boss")) gameFlow.ForceEnterBoss();
            if (GUI.Button(new Rect(panel.x + 8f, panel.y + 136f, panel.width - 16f, 26f), "敌人洞穴")) gameFlow.DebugEnterCave(CaveContentType.Enemy);
            if (GUI.Button(new Rect(panel.x + 8f, panel.y + 168f, panel.width - 16f, 26f), "商人洞穴")) gameFlow.DebugEnterCave(CaveContentType.Merchant);
            if (GUI.Button(new Rect(panel.x + 8f, panel.y + 200f, panel.width - 16f, 26f), "宝箱洞穴")) gameFlow.DebugEnterCave(CaveContentType.Treasure);
        }

        private void DrawHealthBar(Rect rect, float ratio)
        {
            if (healthBarBase != null)
            {
                GUI.DrawTexture(rect, healthBarBase, ScaleMode.StretchToFill, true);
            }
            else
            {
                FillRect(rect, new Color(0.12f, 0.08f, 0.07f));
            }

            float border = Mathf.Clamp(rect.height * 0.22f, 2f, 5f);
            Rect fill = new Rect(rect.x + border, rect.y + border, Mathf.Max(0f, (rect.width - border * 2f) * ratio), rect.height - border * 2f);
            if (healthBarFill != null)
            {
                GUI.DrawTexture(fill, healthBarFill, ScaleMode.StretchToFill, true);
            }
            else
            {
                FillRect(fill, new Color(0.72f, 0.18f, 0.16f));
            }
        }

        private static void DrawPanel(Rect rect, Color background, Color accent)
        {
            FillRect(rect, background);
            FillRect(new Rect(rect.x, rect.y, 4f, rect.height), accent);
            FillRect(new Rect(rect.x, rect.y, rect.width, 1f), new Color(1f, 1f, 1f, 0.12f));
        }

        private static GUIStyle LabelStyle(int size, FontStyle fontStyle, TextAnchor alignment, Color color)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                fontStyle = fontStyle,
                alignment = alignment,
                wordWrap = true,
                normal = { textColor = color }
            };
        }

        private static Rect CenteredRect(float width, float height)
        {
            width = Mathf.Min(width, Screen.width - 28f);
            height = Mathf.Min(height, Screen.height - 28f);
            return new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
        }

        private static string SlotName(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.Weapon:
                    return "兵器";
                case EquipmentSlot.Armor:
                    return "护甲";
                default:
                    return "饰物";
            }
        }

        private static Color RarityColor(EquipmentRarity rarity)
        {
            switch (rarity)
            {
                case EquipmentRarity.Rare:
                    return new Color(0.72f, 0.55f, 0.92f);
                case EquipmentRarity.Fine:
                    return new Color(0.35f, 0.76f, 0.63f);
                default:
                    return Color.white;
            }
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
