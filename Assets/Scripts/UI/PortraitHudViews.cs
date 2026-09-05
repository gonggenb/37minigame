using System.Linq;
using UnityEngine;
using WuxiaRoguelite.Audio;
using WuxiaRoguelite.GameFlow;
using WuxiaRoguelite.MartialArts;
using WuxiaRoguelite.Player;
using WuxiaRoguelite.Runtime;

namespace WuxiaRoguelite.UI
{
    public partial class PrototypeHUDController
    {
        private string portraitSelectedArt;
        private EquipmentItem portraitSelectedEquipment;
        private Vector2 resultBuildScroll;
        private Vector2 portraitChoiceScroll;
        private bool explorationNoticeStarted;
        private float levelNoticeRemaining;
        private int observedMomentumRank;
        private float momentumNoticeRemaining;

        // Presentation-only clocks advance while exploration is visible, never the run clock.
        private void UpdateExplorationNotices()
        {
            if (gameFlow == null || playerStats == null) return;
            if (gameFlow.CurrentPhase == GamePhase.Ready)
            {
                explorationNoticeStarted = false;
                levelNoticeRemaining = momentumNoticeRemaining = 0f;
                observedMomentumRank = 0;
                return;
            }
            if (playerStats.combatMomentumRank > observedMomentumRank)
                momentumNoticeRemaining = 3f;
            observedMomentumRank = playerStats.combatMomentumRank;
            if (gameFlow.CurrentPhase != GamePhase.MainMapRunning || settingsOpen ||
                characterPanelOpen || gameFlow.IsTutorialNoticeActive || !ResponsiveGui.IsPortrait) return;
            if (!explorationNoticeStarted)
            {
                explorationNoticeStarted = true;
                levelNoticeRemaining = 3f;
            }
            if (momentumNoticeRemaining > 0f)
                momentumNoticeRemaining = Mathf.Max(0f, momentumNoticeRemaining - Time.deltaTime);
            else
                levelNoticeRemaining = Mathf.Max(0f, levelNoticeRemaining - Time.deltaTime);
        }

        private void DrawPortraitExploration()
        {
            Rect s = ResponsiveGui.SafeArea;
            float leftWidth = s.width * 0.5f - 66f;
            Rect player = new Rect(s.x + 12, s.y + 12, leftWidth, 96);
            WuxiaUiTheme.DrawCompactSurface(player, Ink, Gold);
            Rect portrait = new Rect(player.x + 4, player.y + 4, 46, 46);
            if (playerPortrait != null) GUI.DrawTexture(portrait, playerPortrait, ScaleMode.ScaleToFit, true);
            WuxiaUiComponents.Text(new Rect(portrait.xMax + 6, player.y + 4, player.width - 60, 24),
                $"等级 {playerStats.level}", 16);
            Rect coin = new Rect(portrait.xMax + 6, player.y + 30, 20, 20);
            if (copperHudIcon != null) GUI.DrawTexture(coin, copperHudIcon, ScaleMode.ScaleToFit, true);
            WuxiaUiComponents.Text(new Rect(coin.xMax + 4, coin.y, player.xMax - coin.xMax - 12, 20),
                playerStats.copper.ToString(), 14, Gold);
            DrawHealthBar(new Rect(player.x + 8, player.y + 55, player.width - 16, 10), playerStats.runtimeStats.HealthRatio);
            WuxiaUiComponents.Text(new Rect(player.x + 8, player.y + 67, player.width - 16, 20),
                $"{CombatNumberDisplay.Format(playerStats.runtimeStats.currentHealth)} / {CombatNumberDisplay.Format(playerStats.runtimeStats.maxHealth)}", 14);
            WuxiaUiComponents.Timer(new Rect(s.center.x - 51, s.y + 6, 102, 102),
                gameFlow.mainTimeRemaining, gameFlow.mainTimeLimit, gameFlow.CurrentPhase == GamePhase.LevelUpPaused);

            Rect xp = new Rect(player.x + 8, player.yMax - 6, player.width - 16, 3);
            FillRect(xp, PanelLight);
            FillRect(new Rect(xp.x, xp.y, xp.width * Mathf.Clamp01((float)playerStats.cultivation /
                Mathf.Max(1, playerStats.NextLevelRequirement)), xp.height), Jade);
            DrawExplorationTimedBuffs(new Vector2(player.x, player.yMax + 8), s.width - 88);
            if (gameFlow.CurrentPhase == GamePhase.MainMapRunning && !characterPanelOpen &&
                (momentumNoticeRemaining > 0f || levelNoticeRemaining > 0f))
            {
                string notice = momentumNoticeRemaining > 0f
                    ? $"连战磨砺 {observedMomentumRank}/{PlayerStats.MaxCombatMomentumRank} · 战力提升"
                    : gameFlow.CurrentLevelDisplayName;
                Rect toast = new Rect(s.center.x - 164, s.yMax - 124, 328, 32);
                WuxiaUiTheme.DrawCompactSurface(toast, Ink, Gold);
                WuxiaUiComponents.Text(toast, notice, 16, Paper, TextAnchor.MiddleCenter);
            }
            Rect message = new Rect(s.x + 20, s.yMax - 82, s.width - 40, 42);
            WuxiaUiTheme.DrawCompactSurface(message, new Color(0.04f, 0.05f, 0.045f, 0.88f), Jade);
            WuxiaUiComponents.Text(new Rect(message.x + 12, message.y + 4, message.width - 24, 34),
                gameFlow.statusMessage, 14, Paper, TextAnchor.MiddleLeft, true);
            WuxiaUiComponents.Text(new Rect(s.x, s.yMax - 34, s.width, 22),
                "滑动屏幕移动", 14, Muted, TextAnchor.MiddleCenter);
        }

        private void DrawExplorationTimedBuffs(Vector2 origin, float width)
        {
            playerStats.GetTimedBuffSnapshots(timedBuffBuffer);
            // No empty slots or backing strip; each real effect owns only its small badge.
            int columns = Mathf.Max(1, Mathf.FloorToInt((width + 8) / 204));
            for (int i = 0; i < timedBuffBuffer.Count; i++)
            {
                PlayerStats.TimedBuffSnapshot buff = timedBuffBuffer[i];
                Rect row = new Rect(origin.x + i % columns * 204, origin.y + i / columns * 52,
                    Mathf.Min(196, width), 44);
                WuxiaUiTheme.DrawCompactSurface(row, Ink, Jade);
                DrawTimedBuffSlot(new Rect(row.x, row.y, 44, 44), buff);
                WuxiaUiComponents.Text(new Rect(row.x + 52, row.y, row.width - 56, 22), buff.displayName, 14);
                WuxiaUiComponents.Text(new Rect(row.x + 52, row.y + 22, row.width - 56, 22), buff.effectSummary, 14, Jade);
            }
        }

        private void PortraitBackdrop()
        {
            FillRect(new Rect(0, 0, ResponsiveGui.Width, ResponsiveGui.Height),
                new Color(0.025f, 0.032f, 0.03f, 0.90f));
        }

        private void DrawPortraitLevelUp()
        {
            PortraitBackdrop();
            Rect p = PortraitUiLayout.Modal(700);
            DrawPanel(p, Ink, Gold);
            WuxiaUiComponents.Text(new Rect(p.x + 24, p.y + 20, p.width - 48, 40), "修为突破", 28);
            WuxiaUiComponents.Text(new Rect(p.x + 24, p.y + 65, p.width - 48, 24),
                "选择一门武学 · 选择期间暂停", 16, WuxiaUiTheme.Paused);
            if (!gameFlow.currentChoices.Contains(portraitSelectedArt))
                portraitSelectedArt = gameFlow.currentChoices.FirstOrDefault();
            const float cardHeight = 140;
            Rect choiceView = new Rect(p.x + 24, p.y + 106, p.width - 48, p.height - 248);
            float choicesHeight = gameFlow.currentChoices.Count * (cardHeight + 10) - 10;
            bool scrollChoices = choicesHeight > choiceView.height;
            portraitChoiceScroll = GUI.BeginScrollView(choiceView, portraitChoiceScroll,
                new Rect(0, 0, choiceView.width - (scrollChoices ? 20 : 0), Mathf.Max(choiceView.height, choicesHeight)));
            for (int i = 0; i < gameFlow.currentChoices.Count; i++)
            {
                string id = gameFlow.currentChoices[i];
                MartialArtDefinition art = MartialArtCatalog.Get(id);
                Rect card = new Rect(0, i * (cardHeight + 10), choiceView.width - (scrollChoices ? 20 : 0), cardHeight);
                bool selected = portraitSelectedArt == id;
                if (GUI.Button(card, GUIContent.none, selected ? activeTabStyle : actionButtonStyle)) portraitSelectedArt = id;
                if (selected) WuxiaUiTheme.DrawOutline(new Rect(card.x + 3, card.y + 3, card.width - 6, card.height - 6), Gold, 2);
                DrawIcon(new Rect(card.x + 12, card.y + 14, 60, 60), FindMartialArtIcon(id), MartialArtIconRenderer.Accent(id));
                WuxiaUiComponents.Text(new Rect(card.x + 86, card.y + 10, card.width - 100, 30), id, 22);
                int current = playerStats.GetMartialArtRank(id);
                WuxiaUiComponents.Text(new Rect(card.x + 86, card.y + 42, card.width - 100, 24),
                    $"{(current == 0 ? "未习得" : RankName(current))} → {RankName(current + 1)}", 16, Gold);
                WuxiaUiComponents.Text(new Rect(card.x + 14, card.y + 78, card.width - 28, card.height - 84),
                    art?.GetEffectSummary(current + 1) ?? string.Empty, 14, Paper, TextAnchor.UpperLeft, true);
            }
            GUI.EndScrollView();
            if (GUI.Button(new Rect(p.x + 24, p.yMax - 126, p.width - 48, 50), "领悟此诀", mainMenuButtonStyle))
            {
                int index = gameFlow.currentChoices.IndexOf(portraitSelectedArt);
                portraitSelectedArt = null;
                if (index >= 0) gameFlow.ChooseMartialArt(index);
                return;
            }
            GUI.enabled = gameFlow.martialArtRerollsRemaining > 0;
            if (GUI.Button(new Rect(p.x + 24, p.yMax - 66, p.width - 48, 44),
                $"重观残页 · 剩余 {gameFlow.martialArtRerollsRemaining}", WuxiaUiComponents.TouchButton()))
            {
                portraitSelectedArt = null;
                gameFlow.RerollMartialArtChoices();
            }
            GUI.enabled = true;
        }

        private void DrawPortraitSettings()
        {
            PortraitBackdrop();
            Rect p = PortraitUiLayout.Modal(560, 456);
            DrawPanel(p, Ink, Gold);
            WuxiaUiComponents.Text(new Rect(p.x + 24, p.y + 24, p.width - 48, 40), "暂停", 30, Paper, TextAnchor.MiddleCenter);
            WuxiaUiComponents.Text(new Rect(p.x + 24, p.y + 70, p.width - 48, 28),
                gameFlow.CurrentLevelDisplayName, 16, Muted, TextAnchor.MiddleCenter);
            if (GUI.Button(new Rect(p.x + 24, p.y + 118, p.width - 48, 54), "继续游戏", mainMenuButtonStyle)) SetSettingsOpen(false);
            Rect music = new Rect(p.x + 24, p.y + 194, p.width - 48, 64);
            WuxiaUiTheme.DrawCompactSurface(music, Panel, Gold);
            WuxiaUiComponents.Text(new Rect(music.x + 14, music.y, music.width - 132, 64), "背景音乐", 18);
            bool on = musicController == null || musicController.MusicEnabled;
            if (GUI.Button(new Rect(music.xMax - 112, music.y + 10, 98, 44), on ? "已开启" : "已关闭", WuxiaUiComponents.TouchButton()))
            {
                musicController ??= FindAnyObjectByType<MainMapMusicController>();
                musicController?.SetMusicEnabled(!on);
            }
            Rect orientation = new Rect(music.x, music.yMax + 16, music.width, 64);
            WuxiaUiTheme.DrawCompactSurface(orientation, Panel, Gold);
            WuxiaUiComponents.Text(new Rect(orientation.x + 14, orientation.y, 120, 64), "画面方向", 18);
            if (GUI.Button(new Rect(orientation.xMax - 178, orientation.y + 10, 76, 44), "竖屏", MobileDisplaySettings.PrefersPortrait ? activeTabStyle : tabStyle)) MobileDisplaySettings.SetPortrait(true);
            if (GUI.Button(new Rect(orientation.xMax - 92, orientation.y + 10, 76, 44), "横屏", !MobileDisplaySettings.PrefersPortrait ? activeTabStyle : tabStyle)) MobileDisplaySettings.SetPortrait(false);
            WuxiaUiComponents.Text(new Rect(p.x + 24, orientation.yMax + 12, p.width - 48, 48),
                "布局跟随实际画面方向\n滑动移动 · 自动战斗", 14, Muted, TextAnchor.MiddleCenter, true);
            if (GUI.Button(new Rect(p.x + 24, p.yMax - 92, p.width - 48, 50), "返回主页", WuxiaUiComponents.TouchButton()))
            {
                SetSettingsOpen(false);
                gameFlow.ReturnToMainMenu();
            }
        }

        private void DrawPortraitEquipment(Rect rect)
        {
            PlayerEquipment equipment = playerStats.equipment;
            if (equipment == null) return;
            EquipmentSlot[] slots = { EquipmentSlot.Weapon, EquipmentSlot.Armor, EquipmentSlot.Accessory };
            for (int i = 0; i < slots.Length; i++)
            {
                Rect row = new Rect(rect.x, rect.y + i * 60, rect.width, 54);
                WuxiaUiTheme.DrawCompactSurface(row, Ink, Gold);
                EquipmentItem item = equipment.GetEquipped(slots[i]);
                WuxiaUiComponents.Text(new Rect(row.x + 10, row.y, 46, row.height), SlotName(slots[i]), 14, Muted);
                DrawIcon(new Rect(row.x + 58, row.y + 6, 42, 42), item == null ? null : FindEquipmentIcon(item.id), Gold);
                WuxiaUiComponents.Text(new Rect(row.x + 112, row.y, row.width - 196, row.height), item?.displayName ?? "未装备", 16);
                if (item != null && GUI.Button(new Rect(row.xMax - 78, row.y + 5, 70, 44), "卸下", WuxiaUiComponents.TouchButton())) equipment.Unequip(slots[i]);
            }
            if (portraitSelectedEquipment == null || !equipment.inventory.Contains(portraitSelectedEquipment))
                portraitSelectedEquipment = equipment.inventory.FirstOrDefault();
            float detailHeight = 176;
            Rect viewport = new Rect(rect.x, rect.y + 194, rect.width, Mathf.Max(72, rect.height - 194 - detailHeight - 12));
            inventoryScroll = GUI.BeginScrollView(viewport, inventoryScroll,
                new Rect(0, 0, viewport.width - 20, equipment.inventory.Count * 68));
            for (int i = 0; i < equipment.inventory.Count; i++)
            {
                EquipmentItem item = equipment.inventory[i];
                Rect row = new Rect(0, i * 68, viewport.width - 24, 60);
                if (GUI.Button(row, GUIContent.none, item == portraitSelectedEquipment ? activeTabStyle : actionButtonStyle)) portraitSelectedEquipment = item;
                DrawIcon(new Rect(8, row.y + 8, 44, 44), FindEquipmentIcon(item.id), RarityColor(item.rarity));
                WuxiaUiComponents.Text(new Rect(64, row.y + 4, row.width - 150, 28), item.displayName, 18);
                WuxiaUiComponents.Text(new Rect(64, row.y + 33, row.width - 74, 22), SlotName(item.slot), 14, Muted);
                if (equipment.IsEquipped(item)) WuxiaUiComponents.Text(new Rect(row.xMax - 80, row.y + 6, 70, 24), "已装备", 14, Jade);
            }
            GUI.EndScrollView();
            EquipmentItem selected = portraitSelectedEquipment;
            if (selected == null) return;
            Rect detail = new Rect(rect.x, rect.yMax - detailHeight, rect.width, detailHeight);
            DrawPanel(detail, Ink, Gold);
            WuxiaUiComponents.Text(new Rect(detail.x + 14, detail.y + 8, detail.width - 28, 28), selected.displayName, 20);
            WuxiaUiComponents.Text(new Rect(detail.x + 14, detail.y + 40, detail.width - 28, 52),
                selected.BonusSummary, 14, Paper, TextAnchor.UpperLeft, true);
            EquipmentItem old = equipment.GetEquipped(selected.slot);
            float attackDelta = selected.attackBonus - (old?.attackBonus ?? 0);
            float defenseDelta = selected.defenseBonus - (old?.defenseBonus ?? 0);
            WuxiaUiComponents.Text(new Rect(detail.x + 14, detail.y + 92, detail.width - 28, 24),
                $"装备差值  攻击 {CombatNumberDisplay.FormatSigned(attackDelta)}  ·  防御 {CombatNumberDisplay.FormatSigned(defenseDelta)}", 14, Gold);
            GUI.enabled = !equipment.IsEquipped(selected);
            if (GUI.Button(new Rect(detail.x + 14, detail.yMax - 52, detail.width - 28, 44),
                equipment.IsEquipped(selected) ? "已装备" : "装备", mainMenuButtonStyle)) equipment.Equip(selected);
            GUI.enabled = true;
        }

        private void DrawPortraitResult()
        {
            PortraitBackdrop();
            Rect p = PortraitUiLayout.Modal(760);
            bool won = gameFlow.IsTutorialCompletionSummary || gameFlow.bossDefeated;
            DrawPanel(p, Ink, won ? Gold : Crimson, WuxiaPanelKind.Boss);
            WuxiaUiComponents.Text(new Rect(p.x + 24, p.y + 18, p.width - 48, 36), "此行战果", 28);
            WuxiaUiComponents.Text(new Rect(p.x + 24, p.y + 62, p.width - 48, 36),
                gameFlow.IsTutorialCompletionSummary ? "教学完成" : won ? $"击败{GameTextCatalog.FinalBossName}" : "江湖路断", 24, won ? Gold : Crimson);
            WuxiaUiComponents.Text(new Rect(p.x + 24, p.y + 104, p.width - 48, 44), gameFlow.statusMessage, 14, Muted, TextAnchor.UpperLeft, true);
            string[] names = { "决战用时", "击杀敌人", "探索洞穴", "连战磨砺" };
            string[] values = { $"{gameFlow.bossBattleTime:0.0} 秒", playerStats.killCount.ToString(), playerStats.caveEntries.ToString(), $"{playerStats.combatMomentumRank} / {PlayerStats.MaxCombatMomentumRank}" };
            for (int i = 0; i < names.Length; i++) WuxiaUiComponents.ReportRow(new Rect(p.x + 24, p.y + 150 + i * 54, p.width - 48, 46), names[i], values[i]);
            WuxiaUiComponents.Text(new Rect(p.x + 24, p.y + 372, p.width - 48, 28), $"本局武学 · 等级 {playerStats.level}", 18);
            Rect v = new Rect(p.x + 24, p.y + 408, p.width - 48, Mathf.Max(48, p.height - 554));
            resultBuildScroll = GUI.BeginScrollView(v, resultBuildScroll, new Rect(0, 0, v.width - 20, Mathf.Max(v.height, playerStats.learnedMartialArts.Count * 48)));
            for (int i = 0; i < playerStats.learnedMartialArts.Count; i++)
            {
                string id = playerStats.learnedMartialArts[i];
                DrawIcon(new Rect(0, i * 48, 40, 40), FindMartialArtIcon(id), MartialArtIconRenderer.Accent(id));
                WuxiaUiComponents.Text(new Rect(54, i * 48, v.width - 84, 40), $"{id} · {RankName(playerStats.GetMartialArtRank(id))}", 16);
            }
            GUI.EndScrollView();
            GUI.enabled = gameFlow.CanContinueToNextLevel;
            if (GUI.Button(new Rect(p.x + 24, p.yMax - 124, p.width - 48, 48), gameFlow.CanContinueToNextLevel ? "下一关" : "下一关尚未开放", mainMenuButtonStyle)) gameFlow.ContinueToNextLevel();
            GUI.enabled = true;
            if (GUI.Button(new Rect(p.x + 24, p.yMax - 64, p.width - 48, 44), "返回主页", WuxiaUiComponents.TouchButton())) gameFlow.ReturnToMainMenu();
        }
    }
}
