namespace WuxiaRoguelite.Runtime
{
    public enum OpeningPortrait
    {
        Narrator,
        Player,
        Fox
    }

    /// <summary>Short linear prologue. Tutorial and formal-run exits retain their own promises.</summary>
    public static class OpeningDialogueCatalog
    {
        public const string Title = "序章 · 狐火初现";
        public const int StoryLineCount = 10;

        public static int Count(bool tutorial) => StoryLineCount + (tutorial ? 0 : 1);

        public static OpeningPortrait Portrait(int index) => index switch
        {
            1 or 3 or 5 or 8 => OpeningPortrait.Player,
            2 or 4 or 6 or 7 => OpeningPortrait.Fox,
            _ => OpeningPortrait.Narrator
        };

        public static string Speaker(int index, string playerName, string bossName) =>
            index >= StoryLineCount ? "出发提示" : Portrait(index) switch
            {
                OpeningPortrait.Player => playerName,
                OpeningPortrait.Fox => bossName,
                _ => "旁白"
            };

        public static string Text(int index, bool tutorial) => index switch
        {
            0 => "暮色压下山道。村口的寻人纸被风卷起，尽头却亮着一簇不肯熄灭的狐火。",
            1 => "山下失踪的人，最后都来过这里。这一路的狐火，是你布下的？",
            2 => "追了这么远，竟只为几个素不相识的人？",
            3 => "他们还活着？",
            4 => $"想知道，就亲自来{GameTextCatalog.FinalBossTempleName}问我。",
            5 => "你既肯现身，又何必躲在傀儡后面？",
            6 => "山门不是谁都能过的。莫让你那点侠气，先折在半路。",
            7 => tutorial
                ? "先过山道，再来寻我。可别连守路的家伙都应付不了。"
                : "给你六十息准备。找些趁手的本事，再来叩我的山门。",
            8 => "路我会走，人我会找。到了古刹，你最好有个答案。",
            9 => "狐火散入山雾，绯红的身影随之淡去。你收拢衣襟，踏上了通往山门的旧路。",
            _ => "确认起手武学后开始六十息探索。普通战斗继续计时；隐藏洞穴暂停主时间。循路牌寻找适合自己的成长路线。"
        };
    }
}
