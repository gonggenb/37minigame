using UnityEngine;
using WuxiaRoguelite.Map;

namespace WuxiaRoguelite.Runtime
{
    public sealed class TutorialLesson
    {
        public readonly string Title;
        public readonly string Body;
        public readonly string Action;

        public TutorialLesson(string title, string body, string action)
        {
            Title = title;
            Body = body;
            Action = action;
        }
    }

    // Only the tutorial flow requests these descriptions. Values come from the touched object.
    public static class TutorialLessonCatalog
    {
        public static readonly TutorialLesson Opening = new TutorialLesson(
            "你只有30秒!",
            "滑屏或摇杆移动，键盘用 WASD / 方向键。\n靠近目标互动，30 秒后挑战守关人。",
            "开始探索");

        public static readonly TutorialLesson Boss = new TutorialLesson(
            $"守关人 · {GameTextCatalog.TutorialBossName}",
            "回满气血，自动战斗；守关战独立计时。\n击败守关人即可通关。",
            "开始挑战");

        public static readonly TutorialLesson MartialArtChoice = new TutorialLesson(
            "升级选武学",
            "修为满后升级，武学三选一，自动生效。\n重复选择可升重，选择时暂停计时。",
            "选择武学");

        public static TutorialLesson ForEncounter(EncounterTrigger encounter)
        {
            switch (encounter.encounterType)
            {
                case EncounterType.NormalEnemy:
                case EncounterType.EliteEnemy:
                    return new TutorialLesson("自动战斗",
                        $"击败敌人可得 {encounter.cultivationReward} 修为、{encounter.copperReward} 铜钱。\n自动攻击，战斗中主倒计时继续。",
                        "开始战斗");
                case EncounterType.HiddenCave:
                    return new TutorialLesson("隐藏洞穴",
                        "洞内主倒计时暂停，靠近目标互动。\n走回出口，点击返回按钮离开，恢复计时。",
                        "进入山洞");
                case EncounterType.Treasure:
                    return new TutorialLesson("宝箱",
                        $"获得装备 ×1、修为 {encounter.cultivationReward}、铜钱 {encounter.copperReward}。\n装备可在角色页查看和穿戴。",
                        "打开宝箱");
                case EncounterType.Herb:
                    string effect = encounter.herbEffect switch
                    {
                        HerbEffectType.Attack => $"本局攻击提高 {Mathf.RoundToInt(encounter.herbBuffValue * 100f)}%",
                        HerbEffectType.Defense => $"本局防御增加 {CombatNumberDisplay.Format(encounter.herbBuffValue)}",
                        HerbEffectType.MoveSpeed => $"本局移速提高 {Mathf.RoundToInt(encounter.herbBuffValue * 100f)}%",
                        _ => $"恢复最大气血的 {Mathf.RoundToInt(encounter.healRatio * 100f)}%"
                    };
                    return new TutorialLesson("药草",
                        $"{effect}。\n采集后立即生效。",
                        "采集药草");
                default:
                    return null;
            }
        }
    }
}
