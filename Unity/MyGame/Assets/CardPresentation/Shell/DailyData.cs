// DailyData.cs — 「日常」这一层的**本地数据源**（单机版）
//
// ============================ 这是**我们自建**的，不是原版的做法 ============================
// 🔴 原版这一整套是**服务端**的：反编译实证 `Missions.CollectChallenge` 会
//    `PlayFab CloudScript 0x38E(910)`（`DF:Missions__CollectChallenge.c:74`），
//    任务内容/奖励/进度全在 `LiveOpsManager` + 远端配置里；**单机没有服务器**。
//    `资料/日常_原版规格.md` §八 已定：**任务内容、奖励表、进度来源全部由我们自建**，
//    并在这里**逐条标明「我们挑的」**（铁律 3：查不到的部分要写清是我们挑的，不能冒充原版）。
//
// ✅ **照原版的**：底层的**状态机语义** —— 三态（未完成 / 可领取 / 已领取）、
//    进度用整数对（`{0}/{1}`，原版 `MissionCounterDisplay('{0}/{1}')`）、
//    已达成才变色（原版 `MissionBackgroundHighlighter.collectableColor`）。
// ❌ **我们挑的**：任务文案、目标数值、奖励内容与数量、倒计时口径、重抽次数。
//
// ⏭ **接线点**：进度的真正来源是战斗结果 —— 接 `Battle/EndPanel.cs`（原版也是「打完一局回来任务动了」）。
//    现在先由 `DailyData` 自己维护计数，`Collect*` 只管「领了 → 变已领取」。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>单机版的「日常」状态。**纯内存 + 可复现**（不用 `UnityEngine.Random`；要随机就用定种子的 `System.Random`）。</summary>
    public static class DailyData
    {
        // ---- 原版三态（照原版语义；`Collectable` 那一态换了底色，见 `RowTint`）----
        public enum State { InProgress = 0, Collectable = 1, Claimed = 2 }

        public sealed class Task
        {
            public string Desc;          // ⚠️ 我们挑的文案
            public int Progress, Target; // 照原版：显示成 `{Progress}/{Target}`
            public string RewardArt, RewardText;
            public State St;
            public bool Step1
            { get { return St != State.InProgress; } }
        }

        static readonly Task[] _daily =
        {
            new Task { Desc = "Deal 500 damage to enemy units", Progress = 52,  Target = 500,
                       RewardArt = "40k_topmarquee_currency_gold",     RewardText = "150" },
            new Task { Desc = "Play 10 troops",                 Progress = 10,  Target = 10,
                       RewardArt = "40k_Achievements_icon_seal_points", RewardText = "20" },
            new Task { Desc = "Win 3 battles",                  Progress = 1,   Target = 3,
                       RewardArt = "40k_topmarquee_currency_gold",     RewardText = "200" },
        };

        const int WeeklyTarget = 15;
        static int _weeklyProgress = 13;
        static State _weeklyState = State.InProgress;
        static readonly bool[] _weeklySteps = { true, true, false, false };

        const int SkullsTarget = 200;
        static int _skullsCount = 160;
        static State _skullsState = State.InProgress;

        const int LoginTarget = 7;
        static int _loginDay = 3;
        static State _loginState = State.InProgress;

        // ============================================================ 任务页

        public static string DailyDesc(int i) { return At(i).Desc; }
        public static string DailyCounter(int i) { var t = At(i); return t.Progress + "/" + t.Target; }
        public static float DailyProgress01(int i) { var t = At(i); return t.Target <= 0 ? 0f : Mathf.Clamp01(t.Progress / (float)t.Target); }
        public static string DailyTimer(int i) { return "Available in 64h"; }   // ⚠️ 我们挑的（原版是本地化词条 + 服务端到期时间）

        /// <summary>整卡底色。**照原版两套值**：普通/已领取 = `normalColor`，可领取 = `collectableColor`。</summary>
        public static Color RowTint(int i)
            => At(i).St == State.Collectable ? new Color(1.0f, 0.6667f, 0.3451f, 1f)
                                             : new Color(0.4941f, 0.5686f, 0.9176f, 1f);

        public static string RefillText() { return "0 Disponible"; }            // 原版的占位串（正本 §三·6）

        public static string LoginTitle() { return "Daily Login Bonus"; }
        public static string SkullsTitle() { return "Daily Skulls"; }
        public static string SkullsCounter() { return "x" + _skullsCount; }
        public static bool SkullsStepDone(int i) { return _skullsCount >= SkullsTarget * (i + 1) / 5; }

        public static string RewardIcon(int i)
        {
            switch (i)
            {
                case 0: return "40k_topmarquee_currency_gold";
                case 1: return "40k_Achievements_icon_seal_points";
                case 2: return "40K_missions_icon_Daily_skulls";
                default: return "40k_main_bt_rewards";
            }
        }
        public static string RewardCount(int i) { return i == 1 ? "20" : "150"; }   // ⚠️ 我们挑的

        // ============================================================ 周常

        public static string WeeklyCounter() { return _weeklyProgress + "/" + WeeklyTarget; }
        public static float WeeklyProgress01() { return Mathf.Clamp01(_weeklyProgress / (float)WeeklyTarget); }
        public static bool WeeklyStepDone(int i) { return _weeklySteps[i]; }
        public static string WeeklyEndsIn() { return "Ends in 12h 34 m"; }       // ⚠️ 我们挑的
        public static string ResetIn() { return "Resets in 12h 34 m"; }          // ⚠️ 我们挑的

        // ============================================================ 领奖（**点击必须有反应**，红线）
        //
        // 原版这里会 `ShowPopUp("Missions/CollectingRewards")` → 云脚本 → `RewardService.Collect`。
        // 单机没有云脚本 ⇒ **本地直接兑现**（进 `Wallet`），并把这一条置成「已领取」。

        public static void CollectDaily(int i)
        {
            var t = At(i);
            if (t.St != State.Collectable) { Say("每日任务 " + (i + 1) + " 还没达成，领不了"); return; }
            t.St = State.Claimed;
            Wallet.Grant(t.RewardArt, ParseCount(t.RewardText));
            Say("每日任务 " + (i + 1) + " 已领取");
        }

        public static void CollectWeekly()
        {
            if (_weeklyState != State.Collectable) { Say("周常还没达成，领不了"); return; }
            _weeklyState = State.Claimed;
            Wallet.Grant("40k_topmarquee_currency_gold", 500);
            Say("周常已领取");
        }

        public static void CollectSkulls()
        {
            if (_skullsState != State.Collectable) { Say("每日骷髅还没达成，领不了"); return; }
            _skullsState = State.Claimed;
            Wallet.Grant("40K_missions_icon_Daily_skulls", 0);
            Say("每日骷髅已领取");
        }

        public static void CollectLogin()
        {
            if (_loginState == State.Claimed) { Say("今天的登录奖励已经领过了"); return; }
            _loginState = State.Claimed;
            Wallet.Grant("40k_topmarquee_currency_gold", 100);
            Say("登录奖励（Day " + _loginDay + "/" + LoginTarget + "）已领取");
        }

        static int ParseCount(string s) { int v; return int.TryParse(s, out v) ? v : 0; }

        static void Say(string what)
        {
            // 红线：不许静默。**每次点击都要在日志里留一句**（现在还没有 toast 那套）。
            Debug.Log("[Daily] " + what + "（**单机本地兑现** —— 原版这一步走 PlayFab 云脚本，没有服务器）");
        }

        static Task At(int i) { return _daily[Mathf.Clamp(i, 0, _daily.Length - 1)]; }

        // ---- 自检用：把状态推到一个可断言的值（**别影响运行时默认**）----
        public static void ForceCollectableForTest()
        {
            _daily[0].St = State.Collectable;
            _daily[1].St = State.Collectable;
            _weeklyState = State.Collectable;
        }
    }

    /// <summary>
    /// 「资源固定 9999」那条边界的落点（用户 2026-09-17 拍板：**不做真实经济**）。
    /// 现在只做记账 + 日志；HUD 上的货币条是后面的事。
    /// </summary>
    public static class Wallet
    {
        public const int StartAmount = 9999;        // 🔴 用户定的固定值
        public static readonly System.Collections.Generic.Dictionary<string, int> Gold =
            new System.Collections.Generic.Dictionary<string, int>();

        /// <summary>记账。**基准是 `StartAmount`（9999）** —— 用户定的「资源固定 9999」。
        /// ⚠️ 第一版从 **0** 起记，于是「领了 150 之后余额是 150」（比 9999 还少），
        ///    自检那条 `>= before` 当场变红 —— 判据没写错，是基准写错了。</summary>
        public static void Grant(string art, int n)
        {
            if (string.IsNullOrEmpty(art)) return;
            Gold[art] = Of(art) + n;
        }

        public static int Of(string art)
        {
            int cur;
            return Gold.TryGetValue(art, out cur) ? cur : StartAmount;
        }
    }
}
