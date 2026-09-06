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
            "滑动屏幕或拖动摇杆移动；键盘可用方向键或 WASD。\n\n探索、碰怪、收集补给，提升本局实力。30 秒后挑战一个简单的守关对手，击败他完成第一关。首次遇见会说明用途，阅读不消耗教学时间。",
            "开始探索");

        public static readonly TutorialLesson Boss = new TutorialLesson(
            $"新手守关 · {GameTextCatalog.TutorialBossName}",
            "30 秒探索结束，来试试刚才积累的实力！\n\n入场时气血回满，双方自动交锋。这个对手伤害很低，只会普通攻击，你获得的武学和装备会继续生效。\n\n守关战独立计时，不再倒数。击败他即可完成第一关。",
            "准备好了，开始试炼");

        public static readonly TutorialLesson MartialArtChoice = new TutorialLesson(
            "修为突破 · 选择武学",
            "修为积满就会升级，并从三门武学中选择一门。\n\n武学自动生效，无需手动释放；再次选择已有武学可以提升重数。留意效果搭配，让本局构筑更强。\n\n选择期间时间暂停，选好后继续。",
            "查看武学选择");

        public static TutorialLesson ForEncounter(EncounterTrigger encounter)
        {
            switch (encounter.encounterType)
            {
                case EncounterType.NormalEnemy:
                case EncounterType.EliteEnemy:
                    return new TutorialLesson("遭遇敌人 · 自动战斗",
                        $"碰到敌人后会自动交锋，无需连续点击攻击。击败这个敌人可获得 {encounter.cultivationReward} 修为、{encounter.copperReward} 铜钱，修为积满后可选择武学。\n\n普通战斗中主地图倒计时继续流逝。留意气血，衡量战斗收益与耗时。",
                        "明白了，开始交锋");
                case EncounterType.HiddenCave:
                    return new TutorialLesson("隐藏洞穴 · 暂停主时间",
                        "山洞提供额外的战斗、宝箱或交易机会，进入后主地图倒计时暂停。\n\n在洞内靠近目标进行互动；结束后走回出口，点击返回按钮离开。返回主地图后继续计时。",
                        "明白了，进入山洞");
                case EncounterType.Treasure:
                    return new TutorialLesson("宝箱 · 装备与成长",
                        $"这个宝箱提供一件装备、{encounter.cultivationReward} 修为和 {encounter.copperReward} 铜钱，无需战斗。\n\n获得装备后可在角色的装备页查看、穿戴和比较效果。铜钱可以在洞穴商人处购买补给。",
                        "明白了，打开宝箱");
                case EncounterType.Herb:
                    string effect = encounter.herbEffect switch
                    {
                        HerbEffectType.Attack => $"本局攻击提高 {Mathf.RoundToInt(encounter.herbBuffValue * 100f)}%",
                        HerbEffectType.Defense => $"本局防御增加 {CombatNumberDisplay.Format(encounter.herbBuffValue)}",
                        HerbEffectType.MoveSpeed => $"本局移速提高 {Mathf.RoundToInt(encounter.herbBuffValue * 100f)}%",
                        _ => $"恢复最大气血的 {Mathf.RoundToInt(encounter.healRatio * 100f)}%，不会超过气血上限"
                    };
                    return new TutorialLesson("药草 · 立即生效的补给",
                        $"这株药草可以{effect}。\n\n触碰采集后立即生效，不会放进背包。恢复类药草在受伤后采集更有价值，留意路线上的补给。",
                        "明白了，采集药草");
                default:
                    return null;
            }
        }
    }
}
