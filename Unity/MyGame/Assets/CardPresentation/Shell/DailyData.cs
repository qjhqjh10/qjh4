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

        /// <summary>这条任务**达成没有**。**照原版语义用**：`MissionInfoDisplay.DisplayRule` 是
        /// `[Flags]{ WhenActive=1, WhenComplete=2 }`，实现在 `DF:MissionInfoDisplay__Initialize.c` ——
        /// `show = (IsComplete() && WhenComplete) || (!IsComplete() && WhenActive)`。
        /// 每日任务行的 `description` 是 **1**、`timer` 是 **2** ⇒ **两者互斥**（它们矩形本来就重叠）。
        /// ⚠️ **还没查清**：`MissionChallengeProgress.IsComplete()` 是否含「已领取」那一态 —— 这里取「进度到顶」。</summary>
        public static bool DailyDone(int i) { var t = At(i); return t.Target > 0 && t.Progress >= t.Target; }

        /// <summary>整卡底色。**照原版两套值**：普通/已领取 = `normalColor`，可领取 = `collectableColor`。</summary>
        public static Color RowTint(int i)
            => At(i).St == State.Collectable ? new Color(1.0f, 0.6667f, 0.3451f, 1f)
                                             : new Color(0.4941f, 0.5686f, 0.9176f, 1f);

        public static string RefillText() { return "0 Disponible"; }            // 原版的占位串（正本 §三·6）

        public static string LoginTitle() { return "Daily Login Bonus"; }
        public static string SkullsTitle() { return "Daily Skulls"; }
        public static string SkullsCounter() { return "x" + _skullsCount; }

        /// <summary>🆕 2026-10-03：每日骷髅任务那条 `counter/icons/Army` 格子要挂的**阵营**
        /// （`null` = **Neutral**）。判据 = `d:/2/tools/decomp_full/MissionCounterDisplay__Setup.c:51-63`：
        /// 图 = `ArmyUtilities.GetArmyIcon(challenge.Army)`，**`army == Neutral(0)` 时那一格整格 `SetActive(false)`**；
        /// `Army` 来自挑战字段（`GamesPlayed.cs:14-25` / `SkullsCount.cs:12-21`）。
        /// 🔴 **原版当天那条任务挂哪个阵营本地查不到**（daily 没有资产、服务端下发；
        /// `grep -rl anyArmy assets_full` 只命中静态成就）⇒ **我们这份 mock 恒 `null` = Neutral**，
        /// 也就是**那一格不建、也不留 60px**（`项目任务.md` §三 第 29 条 B1）。
        /// ⚠️ 将来若给 mock 任务补上阵营，这里返回它即可 —— 画法在 `MissionsTab` 里已经写好了。</summary>
        public static string SkullsArmy() { return null; }
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

        // ============================================================ 重摇任务（`MissionReRollButton` → `MissionReRollPopup`）
        //
        // 🔴 **那颗「垃圾桶」不是删除任务，是【重摇任务】**（2026-10-04 查实）：
        //    原版节点 `Trash mission` 挂的是 `MissionReRollButton`（字段 `button`/`displayRule`/`reRollPopup`），
        //    点它 ⇒ `WindowsManager.OpenWindow(MissionReRollPopup, ctx)`（反编译出处：
        //    `d:/2/tools/decomp_full/MissionReRollButton.__c__DisplayClass3_0___Setup_b__0.c`），
        //    弹窗里 `Confirm` ⇒ `MissionEvent…RerollChallenge`（`PlayFab CloudScript`）⇒ **服务端换一条**。
        //    判据正本 = `资料/待办判据_阶段二与联机.md` §A23。
        //
        // ❌ **我们挑的**：**单机没有服务器** ⇒ 换一条的活儿在这里本地做，池子与规则由我们定：
        //    · **换一条新任务** —— 从下面那个池里取一条**当前列表里没有的**顶替它；池子不够（全在列表里）就**轮换**；
        //    · 用**定种子的 `System.Random`**（照 `BoosterPackOpenWindow.RollPack` 那条先例）⇒ **自检可复现**；
        //    · 新任务从 `InProgress` 起（进度 0）—— 原版服务端也是给一条全新的进度。
        // ⚠️ **不扣任何东西**（用户 2026-09-17 拍板「不做真实经济、资源固定 9999」）——
        //    弹窗里那个价钱格照原版外观建、`300,00` 是 prefab 出厂占位（**不是真价钱**，见 `MissionRerollPopup`）。

        /// <summary>重摇用的种子。**定死** ⇒ 同一串调用每次得到同一条（自检可复现）。</summary>
        public const int RerollSeed = 20261004;

        /// <summary>可用来顶替的任务池（**文案/数值都是我们挑的** —— 与原版一样，任务内容本来就由服务端下发）。
        /// 🔴 **池里每一条的 `Desc`/`Target`/`RewardText` 都与 `_daily` 那三条全不同** ——
        /// 这样「换完逐项不同」这条断言才有意义（拿同一条顶替等于没换）。</summary>
        static readonly Task[] _rerollPool =
        {
            new Task { Desc = "Deal 300 damage with ranged units",  Progress = 0, Target = 300,
                       RewardArt = "40k_topmarquee_currency_gold",      RewardText = "120" },
            new Task { Desc = "Win 1 battle with a Warlord alive",  Progress = 0, Target = 1,
                       RewardArt = "40k_Achievements_icon_seal_points", RewardText = "80" },
            new Task { Desc = "Deploy 15 troops",                   Progress = 0, Target = 15,
                       RewardArt = "40K_missions_icon_Daily_skulls",    RewardText = "180" },
            new Task { Desc = "Destroy 8 enemy units",              Progress = 0, Target = 8,
                       RewardArt = "40k_topmarquee_currency_gold",      RewardText = "90" },
            new Task { Desc = "Play 5 tactic cards",                Progress = 0, Target = 5,
                       RewardArt = "40k_Achievements_icon_seal_points", RewardText = "110" },
        };

        static readonly System.Random _rerollRng = new System.Random(RerollSeed);
        /// <summary>池子全在列表里时的轮换游标（**只在池子不够时用**，会出声）。</summary>
        static int _rerollRound;
        /// <summary>累计重摇了几次（自检用：`Cancel`/点窗外**不该**让它变大，只有 `Confirm` 会）。</summary>
        public static int RerollCount { get; private set; }

        /// <summary>把第 `i` 条**换掉**（原版：`Confirm` → 服务端 `RerollChallenge`）。
        /// 返回**新任务的描述**（自检要拿它比）。⚠️ 旧任务**直接丢了**（原版服务端也是换一条新的）。
        /// ⚠️ 自检要比「换前/换后」的那两个字段走 `DailyRewardText` / `DailyRewardArt`
        /// （卡面那一格画的是 `RewardCount(i)`，那是**按下标**的格内容、不会跟着任务走 —— 见那两个访问器的注释）。</summary>
        public static string RerollDaily(int i)
        {
            var t = At(i);
            string oldDesc = t.Desc;

            // 候选 = 池里**当前列表里没有的**（自己那条也算「在列表里」⇒ 不会被选中）
            var cand = new System.Collections.Generic.List<Task>();
            for (int k = 0; k < _rerollPool.Length; k++)
            {
                bool used = false;
                for (int j = 0; j < _daily.Length && !used; j++)
                    if (_daily[j].Desc == _rerollPool[k].Desc) used = true;
                if (!used) cand.Add(_rerollPool[k]);
            }
            Task src;
            if (cand.Count > 0) src = cand[_rerollRng.Next(cand.Count)];
            else
            {
                // 池子不够（5 条池全被占了才走到这）⇒ 轮换，并且**出声**（红线：不许静默）
                src = _rerollPool[_rerollRound % _rerollPool.Length];
                _rerollRound++;
                Debug.LogWarning("[Daily] 重摇的任务池**已经全在列表里**了 ⇒ 退回**轮换**取第 "
                                 + (_rerollRound % _rerollPool.Length) + " 条（原版这一步是服务端换一条新的，"
                                 + "我们没有更大的池子 —— 如实说，不假装它是新任务）");
            }

            t.Desc = src.Desc;
            t.Progress = 0;
            t.Target = src.Target;
            t.RewardArt = src.RewardArt;
            t.RewardText = src.RewardText;
            t.St = State.InProgress;                       // 新任务从「未完成」起
            RerollCount++;
            Say("第 " + (i + 1) + " 条任务已**重摇**：「" + oldDesc + "」⇒「" + t.Desc + "」"
                + "（⚠️ **不扣任何资源** —— 用户 2026-09-17 拍板「不做真实经济」；"
                + "原版这一步走服务端 `RerollChallenge` 并扣 `RerollPrice`）");
            return t.Desc;
        }

        // ============================================================ 周常

        public static string WeeklyCounter() { return _weeklyProgress + "/" + WeeklyTarget; }
        public static float WeeklyProgress01() { return Mathf.Clamp01(_weeklyProgress / (float)WeeklyTarget); }
        public static bool WeeklyStepDone(int i) { return _weeklySteps[i]; }
        public static string WeeklyEndsIn() { return "Ends in 12h 34 m"; }       // ⚠️ 我们挑的
        public static string ResetIn() { return "Resets in 12h 34 m"; }          // ⚠️ 我们挑的

        // ============================================================ 每日奖励（`Daily Reward Popup`）
        //
        // 结构**照原版**：一条轨上 4 个 `Entry`（一天一格），每格两个抽屉（`NormalReward` / `Premium Reward`），
        // 每个抽屉**各自**持一个四态 `RewardState`。状态怎么算照原版
        // （`DF:DailyRewardItemContainer__GetCurrentState.c:18-46`，见 `资料/日常_调用链_DailyRewardPopup.md` §D）。
        // ❌ **我们挑的**：天数、奖励内容与数量、里程碑目标值、有没有买 Premium。

        public const int RewardDays = 4;
        /// <summary>⚠️ 我们挑的：已领到第几天（`index < 它` ⇒ 该格 `Collected`）。</summary>
        const int RewardsCollected = 2;
        /// <summary>⚠️ 我们挑的：今天这条 mission 的进度（决定后面几格 `Unlocked` 还是 `Locked`）。</summary>
        const int RewardCurrentValue = 3;
        static readonly int[] _rewardTarget = { 1, 2, 3, 4 };
        static readonly string[] _rewardFreeName = { "150 Gold", "1 Booster pack", "200 Gold", "20 Skulls" };
        static readonly string[] _rewardPremName = { "300 Gold", "2 Booster packs", "400 Gold", "40 Skulls" };
        static readonly string[] _rewardFreeIcon = { "40k_topmarquee_currency_gold", "40k_main_bt_rewards",
                                                     "40k_topmarquee_currency_gold", "40K_missions_icon_Daily_skulls" };
        static readonly string[] _rewardPremIcon = { "40k_topmarquee_currency_gold", "40k_main_bt_rewards",
                                                     "40k_topmarquee_currency_gold", "40K_missions_icon_Daily_skulls" };
        static readonly int[] _rewardFreeCount = { 150, 20, 200, 160 };
        static readonly int[] _rewardPremCount = { 300, 40, 400, 320 };
        static readonly bool[] _rewardClaimed = new bool[RewardDays];
        /// <summary>⚠️ 我们挑的：**单机不卖 Premium**（边界②「不做真实经济」）⇒ Premium 抽屉恒为 `PremiumLocked`。</summary>
        static readonly bool _premiumOwned = false;

        static int RI(int day) { return Mathf.Clamp(day, 0, RewardDays - 1); }

        public static string RewardName(int day, bool premium)
        { return (premium ? _rewardPremName : _rewardFreeName)[RI(day)]; }

        public static string RewardIconOf(int day, bool premium)
        { return (premium ? _rewardPremIcon : _rewardFreeIcon)[RI(day)]; }

        public static int RewardAmount(int day, bool premium)
        { return (premium ? _rewardPremCount : _rewardFreeCount)[RI(day)]; }

        /// <summary>`Personal Progression` 的 `Counter` 文本。</summary>
        public static string RewardDayCounter(int day) { return (RI(day) + 1) + "/" + RewardDays; }

        /// <summary>四态。**公式照原版**（`GetCurrentState`）：普通那条 + Premium 那条。</summary>
        public static RewardState RewardStateOf(int day, bool premium)
        {
            int i = RI(day);
            int normal = (i < RewardsCollected)
                ? (int)RewardState.Collected
                : (_rewardTarget[i] <= RewardCurrentValue ? (int)RewardState.Unlocked : (int)RewardState.Locked);
            if (!premium) return (RewardState)normal;
            if (!_premiumOwned) return RewardState.PremiumLocked;
            return (normal == (int)RewardState.Collected) ? RewardState.Unlocked : (RewardState)normal;
        }

        /// <summary>`Gacha Reward Claimed` 上的 `Claimed Tex`。
        /// ⚠️ 原版是本地化词条（prefab 里的占位串是 `'Recogido'` = 西语「已领取」）⇒ **我们按英文写**。</summary>
        public static string RewardClaimedText() { return "Claimed"; }

        // ⚠️ 用 **prefab 自带的占位串**（原版运行时按 I2 词条本地化，本地没有语言表）——
        //    两条 TMP 实测 `H=2 (Center)`、fs=36，框只有 200 宽；自己写更长的英文会**溢出被切**（渲染图实证）。
        public static string FreeTrackTitle() { return "Ruta Gratuita"; }
        public static string PremiumTrackTitle() { return "Ruta Premium"; }
        public static string PremiumTrackPrice() { return "9999"; }             // ⚠️ 我们挑的（边界②：不卖）

        // 顶栏（阵营）—— ⚠️ 原版这里是 `ArmyUtilities.Instance.GetArmyIcon(army)`，**单机没有那套清单** ⇒ 写死一个
        public static string HeaderArmyIcon() { return "40k_DeckSelection_icon_FactionOrks"; }
        public static string HeaderArmyName() { return "Orks"; }                // 原版 prefab 占位串就是 'Orks'
        public static string HeaderArmySubTitle() { return "Daily Rewards"; }   // ⚠️ 我们挑的（原版是本地化词条）

        public static string RewardTimerText() { return ResetIn(); }            // 原版由 `TimerDisplay` 运行时填

        /// <summary>领一天的奖励。**只有 `Unlocked` 那一态能领**（原版的 `colider` 也只有那一态可点）。</summary>
        public static void CollectReward(int day, bool premium)
        {
            var st = RewardStateOf(day, premium);
            if (st != RewardState.Unlocked)
            { Say($"第 {day + 1} 天{(premium ? " Premium" : "")}奖励现在是 `{st}`，领不了"); return; }
            _rewardClaimed[RI(day)] = true;
            Wallet.Grant(RewardIconOf(day, premium), RewardAmount(day, premium));
            Say($"第 {day + 1} 天{(premium ? " Premium" : "")}奖励已领取");
        }

        // ============================================================ 收件箱（`Inbox Menu`）
        //
        // ⚠️ **单机没有服务器** ⇒ 没有消息可列。原版 `InboxWindow__Open` 在「一条消息都没有」时走的正是
        // **空态**（开 `noNewsWarning`、关 `MessageDisplay`）⇒ 我们做的就是这个状态，**不是省略**。
        // ❌ **我们挑的**：`Message Display` 的标题文案。

        /// <summary>消息条数。**单机恒 0**（原版从 LiveOps handler 取；条目 prefab 由服务端事件数据决定）。</summary>
        public static int InboxCount { get { return 0; } }
        public static string InboxTitle() { return "Inbox"; }                       // 原版 prefab 占位串
        public static string InboxNoNewsText() { return "Game announcements will be displayed here"; }  // 原版 prefab 占位串
        public static string InboxMessageDisplayTitle() { return "Message"; }       // ⚠️ 我们挑的
        /// <summary>收件箱红点该不该亮 —— 原版 `Inbox.CheckNotification` = **未读条数 > 0**。</summary>
        public static bool InboxHasBadge { get { return InboxCount > 0; } }

        // ============================================================ 每日连登（`Daily Streak Popup`）
        //
        // 两态**照原版**：驱动字段是任务对象的 `HasFailed`/`FailedValue`，靠**两个面板互斥**。
        // ⚠️ 原版**出厂亮着的是「断了」那一态**；我们按单机口径默认 `HasFailed = false`（看连胜态）。
        // ❌ **我们挑的**：连了几天、奖品格数、奖励内容、文案。

        /// <summary>有没有「可领取」的每日任务 —— **奖励窗左栏 Missions 键上的红点判据**。
        /// 原版是 `Missions.CheckNotification`（`INotificationProvider<MissionsBadge>`），
        /// 显隐走 `UiBadgeNotification` 的 **alpha 补间**（`Show()`→1.0 / `Hide()`→0），**不是 `SetActive`**。</summary>
        public static bool RewardsHasBadge
        {
            get { for (int i = 0; i < _daily.Length; i++) if (_daily[i].St == State.Collectable) return true; return false; }
        }

        /// <summary>⚠️ 我们挑的：单机默认**不断签**（这样默认看到的是 `Streak Successful`）。</summary>
        static bool _streakFailed = false;
        /// <summary>⚠️ 我们挑的：当前连了几天（原版由服务端 `currentValue` 给）。</summary>
        const int StreakCurrent = 5;
        /// <summary>一条连登轨上有几个奖格（原版由 `challenges` 长度定）。</summary>
        public const int StreakDays = 7;
        /// <summary>`i < 它` ⇒ 这一格**已领**；`i == 它` ⇒ **可领**（也是 `scaleMultiplierFirstElement` 作用的那一格）。</summary>
        public const int StreakCollected = 5;
        /// <summary>⚠️ 我们挑的：断签时掉的层数（原版是 `MainMenuMission.FailedValue`）。</summary>
        const int StreakLostValue = 10;

        static readonly string[] _streakName =
        { "1 Booster Pack", "150 Gold", "20 Skulls", "2 Booster Packs", "200 Gold", "300 Gold", "40 Skulls" };
        static readonly string[] _streakIcon =
        { "40k_main_bt_rewards", "40k_topmarquee_currency_gold", "40K_missions_icon_Daily_skulls",
          "40k_main_bt_rewards", "40k_topmarquee_currency_gold", "40k_topmarquee_currency_gold",
          "40K_missions_icon_Daily_skulls" };
        static readonly int[] _streakAmount = { 1, 150, 20, 2, 200, 300, 40 };
        static readonly bool[] _streakClaimed = new bool[StreakDays];

        public static bool StreakFailed() { return _streakFailed; }
        public static string StreakWindowTitle() { return "Daily Streak"; }
        public static string StreakCurrentLabel() { return "Current streak:"; }      // ⚠️ 我们挑的
        public static string StreakCurrentValue() { return StreakCurrent.ToString(); }
        public static string StreakNextRewardsText() { return "More Rewards In"; }   // 原版 prefab 占位串
        public static string StreakTimerText() { return "19h 23m"; }                 // 原版 prefab 占位串
        public static string StreakBrokenText() { return "STREAK BROKEN"; }          // 原版 prefab 占位串
        public static string StreakLostText() { return "Streak lost: " + StreakLostValue; }
        public static string ResetStreakText() { return "Reset Streak"; }            // 原版 prefab 占位串
        public static string StreakClaimText() { return "Claim"; }                   // 原版 prefab 占位串
        public static string StreakInfoText()
        { return "Log in every day to keep your streak going."; }                    // ⚠️ 我们挑的

        public static string StreakRewardName(int i) { return _streakName[SI(i)]; }
        public static string StreakRewardIcon(int i) { return _streakIcon[SI(i)]; }
        public static bool StreakRewardClaimed(int i) { return _streakClaimed[SI(i)] || SI(i) < StreakCollected; }
        public static bool StreakRewardUnlocked(int i) { return SI(i) == StreakCollected; }
        static int SI(int i) { return Mathf.Clamp(i, 0, StreakDays - 1); }

        public static void CollectStreak(int i)
        {
            if (!StreakRewardUnlocked(i)) { Say($"连登第 {i + 1} 格现在领不了"); return; }
            _streakClaimed[SI(i)] = true;
            Wallet.Grant(StreakRewardIcon(i), _streakAmount[SI(i)]);
            Say($"连登第 {i + 1} 格已领取");
        }

        /// <summary>原版 `ResetStreakAfterFail`：**只换画面** —— 不写 `HasFailed`、也不减 `currentValue`。</summary>
        public static void ResetStreak()
        {
            _streakFailed = false;
            Say("连登已重置（⚠️ 原版这一步**不发 PlayFab、也不改数值**，只是把画面切回连胜态）");
        }

        /// <summary>关窗/返回时原版会先收一遍（`Close()` = `LiveOp.TryCollect(() => base.Close())`）。</summary>
        public static void StreakAutoCollect()
        {
            if (_streakClaimed[SI(StreakCollected)]) return;
            Say("关窗时自动收取可领的那一格（原版 `Close()` 里就是 `TryCollect`）");
        }

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

        // ============================================================ 战果 → 任务进度（原版的接线点）
        //
        // 原版：**打完一局回来任务就动了** —— 进度由服务端在 `MissionChallengeProgress` 上累加。
        // 单机没有服务端 ⇒ **我们自己累加**（判据只此一处：`Advance`）。
        // ⚠️ 战果本身**由引擎记**（`BattleContext.DamageToEnemy` / `TroopsPlayed`，2026-09-23 加），
        //    这里只负责**消费**它 —— 引擎不认识「日常任务」这回事。

        /// <summary>第 i 条的当前进度值（自检用；别拿 `DailyCounter` 那个字符串去解析）。</summary>
        public static int DailyProgressValue(int i) { return At(i).Progress; }
        /// <summary>第 i 条的状态（自检用）。</summary>
        public static State DailyState(int i) { return At(i).St; }
        /// <summary>第 i 条**任务自己**的奖励数量/图标（自检用）。
        /// 🔴 **注意别跟卡面那一格弄混**：卡面上画的 `RewardIcon(i)` / `RewardCount(i)` 是**按下标**的
        /// 「格内容」表（登录卡与骷髅卡也在用同一份），**不跟着任务走** —— 重摇之后格子里那个数
        /// **不会变**。这两条口径不一致是我们这份 mock 的既有状态（领取走的是**任务**这一份：
        /// `CollectDaily` → `ParseCount(t.RewardText)`），已记在报告里，**本件不动它**。</summary>
        public static string DailyRewardText(int i) { return At(i).RewardText; }
        /// <summary>第 i 条**任务自己**的奖励图标（自检用；口径见 `DailyRewardText`）。</summary>
        public static string DailyRewardArt(int i) { return At(i).RewardArt; }

        public static void OnBattleEnd(bool win, int damageToEnemy, int troopsPlayed)
        {
            // 三张每日任务卡：0 = Deal 500 damage to enemy units · 1 = Play 10 troops · 2 = Win 3 battles
            Advance(0, damageToEnemy);
            Advance(1, troopsPlayed);
            if (win) Advance(2, 1);
            Say($"本局战果进了任务进度：对敌伤害 +{damageToEnemy} · 打出部队 +{troopsPlayed} · 胜 {(win ? 1 : 0)}");
        }

        /// <summary>把第 i 条的进度往前推 n。**到顶就变「可领取」**（原版 `MissionBackgroundHighlighter` 那一态）。</summary>
        static void Advance(int i, int n)
        {
            if (n <= 0) return;
            var t = At(i);
            if (t.St == State.Claimed) return;                 // 领过的不再加
            t.Progress = Mathf.Min(t.Target, t.Progress + n);
            if (t.Progress >= t.Target) t.St = State.Collectable;
        }

        /// <summary>自检用：把三张任务卡恢复到**确定的初值**（**只给自检**，运行时别调）。
        /// ⚠️ 用**确定的数**而不是「出厂值」—— 出厂的 task1 就是 10/10（已在 target），
        /// 断言「推进了多少」会恒为 0（2026-09-23 第一版就栽在这）。</summary>
        public static void ResetMissionsForTest()
        {
            _daily[0].Progress = 52; _daily[0].St = State.InProgress;    // /500
            _daily[1].Progress = 4;  _daily[1].St = State.InProgress;    // /10
            _daily[2].Progress = 1;  _daily[2].St = State.InProgress;    // /3
        }

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
