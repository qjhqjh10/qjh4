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
// 🆕 **2026-10-11（A370）例外**：**每日骷髅卡**那五档阈值（`3/10/25/50/100`）与那一格奖励
//    （**活动点 ×200**）现在**照原版实拍**，不再是我们挑的 —— 见 `_skullsSteps` 与「骷髅卡」两段。
//
// ✅ **接线点（2026-10-11 · A375 接上）**：进度的真正来源 = 战斗结果 —— 落点 = `Battle/BattleDriver.cs`
//    **结算那一处**的 `DailyData.OnBattleEnd(…)`（原版也是「打完一局回来任务就动了」）。
//    ⚠️ 上面那句「这是**我们自建**的」仍然成立 —— 但**只管【任务内容 / 奖励表 / 数值】**；
//    **「一局给几个骷髅」不在其内**：那一条是**照原版**的（判据写在 `OnBattleEnd` 上面那一大段里）。
//    `Collect*` 只管「领了 → 变已领取」。
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

        /// <summary>出厂三行的**内容快照**（`Desc`/`Target`/`RewardArt`/`RewardText` 各拷一份）。
        /// 🔴 **为什么要有它**：`RerollDaily` 是**就地改** `_daily[i]` 那几个字段的那一条（原版是服务端换新的）；
        /// 自检把重摇池临时换掉、跑完**只还原了池子**，被顶替那一行的文案/目标/奖励就**留在工作区**了
        /// （2026-10-04 A36-M6 审查挑出的**状态残留**：后面的每一节都吃这个残留，下一节若读它就咬人）。
        /// `ResetMissionsForTest` 拿这份快照把三行还原 ⇒ 自检之间**不再靠隐式状态**。
        /// ⚠️ 静态字段初始化按**声明顺序**执行 ⇒ 这份快照必须写在 `_daily` **之后**（写前面会拷到空数组）。</summary>
        static readonly Task[] _dailyFactory = SnapshotOf(_daily);

        static Task[] SnapshotOf(Task[] src)
        {
            var copy = new Task[src.Length];
            for (int i = 0; i < src.Length; i++)
                copy[i] = new Task { Desc = src[i].Desc, Progress = src[i].Progress, Target = src[i].Target,
                                     RewardArt = src[i].RewardArt, RewardText = src[i].RewardText, St = src[i].St };
            return copy;
        }

        const int WeeklyTarget = 15;
        static int _weeklyProgress = 13;
        static State _weeklyState = State.InProgress;
        static readonly bool[] _weeklySteps = { true, true, false, false };

        // ---- 每日骷髅卡：五档里程碑的**阈值** ----
        // 🔴 **2026-10-11（批次 · A370）就地订正（铁律 5）**：这里原来只有**一个** `const int SkullsTarget = 200`，
        //    五档由它**等分**出来（`SkullsTarget * (i + 1) / 5` ⇒ 40 / 80 / 120 / 160 / 200），
        //    且注明是**我们挑的**（原版那条 daily 挑战由服务端下发、本地拿不到 —— 那半句仍然成立）。
        // ✅ **现在有真值了**：判据 = **用户提供的原版实拍**
        //    （`C:\Users\qjh36\Desktop\奖励—布道所（每日任务）参考图.png` 左栏「每日骷髅头」卡）
        //    —— 卡上**五个方框里逐格印着** `3` `10` `25` `50` `100`。
        //    ⇒ 五档**不是等分**；`200` 也不再是「总目标」，它是**每档给的活动点数**（见「骷髅卡奖励」那一段）。
        // ⚠️ 实拍上那 5 个数是**画在方框里的**（= 原版 `MissionMilestoneStep.text` 那个 TMP 的运行时值，
        //    出厂占位串是 `'1'` —— 判据见 `SkullsStepTarget` 的注释）。
        static readonly int[] _skullsSteps = { 3, 10, 25, 50, 100 };

        /// <summary>五档里程碑的**格数**（= 实拍上那 5 个方框；卡上也真的挂着 5 个 `MissionMilestoneStep`）。</summary>
        public const int SkullsStepCount = 5;

        /// <summary>当日**骷髅计数**（原版 `MissionChallengeProgress.currentValue`）。
        /// 🔴 **2026-10-11（批次 · A375）就地订正（铁律 5）**：这里原来写的是 **`= 160`** ——
        /// 一个**出厂 mock 常量**，全工程**没有一个地方往它上面加过东西** ⇒ 画面恒 `x160`、五档恒全亮。
        /// ✅ **现在 = 0**，与**原版实拍**逐字一致：实拍那张「每日骷髅头」卡上印的是 **`x0` + 五个方框全灭**
        /// （判据 = `C:\Users\qjh36\Desktop\奖励—布道所（每日任务）参考图.png`；
        ///  差异清单第 4 条 → `资料/普查产出_1011/R1_每日骷髅与登录卡.md` §六 #4）。
        /// 🔴 推进它的**唯一**入口 = `OnBattleEnd(…)`（一局结算一次；原版 = `SkullsCount.OnBattleEnd`）——
        /// ⛔ 别在别处直接写这个字段；自检要造非零态走 `ForceSkullsCountForTest`（**测试口，不在出厂路径上**）。</summary>
        static int _skullsCount = 0;
        static State _skullsState = State.InProgress;

        const int LoginTarget = 7;
        static int _loginDay = 3;
        static State _loginState = State.InProgress;

        // ============================================================ 任务页

        public static string DailyDesc(int i) { return At(i).Desc; }
        public static string DailyCounter(int i) { var t = At(i); return t.Progress + "/" + t.Target; }
        public static float DailyProgress01(int i) { var t = At(i); return t.Target <= 0 ? 0f : Mathf.Clamp01(t.Progress / (float)t.Target); }
        public static string DailyTimer(int i) { return "Available in 64h"; }   // ⚠️ 我们挑的（原版是本地化词条 + 服务端到期时间）

        /// <summary>🆕 **A63**：画在 `progress` 那一节点上的**最终文本** —— 原版
        /// `MissionCounterDisplay__Setup` 就是**两支**（`d:/2/tools/decomp_full/MissionCounterDisplay__Setup.c:23-79`，
        /// 逐句亲读；MB `3476392019656054992` 逐字段实读）：
        /// <code>
        /// cur = challenge.currentValue;  max = challenge.MaxValue;
        /// if (cur &lt; max || displayCompletedMessage == 0)  text = string.Format(progressTextFormat, cur, max);
        /// else                                             text = GetTranslation(completedMessage);
        /// </code>
        /// 出厂字段值：`progressTextFormat = "{0}/{1}"` · **`displayCompletedMessage = 1`** ·
        /// **`completedMessage = "Missions/Completed"`**（`displayRule = 1`，即**未领取才可见**）。
        /// 🔴 **`completedMessage` 存的是 I2 词条【键】，不是文案** —— 原版在这一句外面套了
        /// `I2_Loc_LocalizationManager.GetTranslation`（`.c:74`），而**本地没有语言表**
        /// （远端 CCD；全仓多次实测）⇒ **「照抄原版英文」这条路在这一条上办不到**。
        /// 照本仓先例（`MissionRerollPopup` 的出厂原文 `Discard this mission…`、登录卡的 `Ruta Gratuita`）
        /// **照抄 prefab 里那个串本身**，并在**第一次**用到时 `Debug.Log` **出声**
        /// —— 不自己编一句英文冒充原版（铁律 3）。
        /// ⚠️ **判据只此一处**：全工程只有这一个方法决定「到顶换不换文案」，`MissionsTab` 只负责画。
        /// ⚠️ 到顶 = **纯数值比较 `Progress >= Target`**（照原版的 `cur >= max`），**不是** `St == Collectable`：
        /// 原版那一句从不碰领取域；两者在我们的数据模型里通常同真，但**判据照原版**。
        /// ⚠️ 「已领取」那一行**整件不建**（`displayRule = 1` ⇒ `!IsComplete` 才 `SetActive(true)`）⇒
        /// 这里不必管它（`MissionsTab` 只在 `!claimed` 那一支里调本方法）。</summary>
        public static string DailyCounterText(int i)
        {
            var t = At(i);
            if (t.Progress < t.Target) return t.Progress + "/" + t.Target;
            SayCompletedKeyOnce();
            return CompletedMessage;
        }

        /// <summary>原版 `MissionCounterDisplay.completedMessage` 的**出厂原文**
        /// （真包 MB `3476392019656054992` 实读）。
        /// 🔴 **它是原版的 I2 词条【键】，不是给人看的文案** —— 别把它当成原版的英文。</summary>
        public const string CompletedMessage = "Missions/Completed";

        /// <summary>「我们正在显示一个词条键」这件事**只说一次**（每次重建都吼会淹掉日志，
        /// 而这条信息只需要传递一次：**这一处的文案是原版词条键、不是我们编的**）。</summary>
        static bool _saidCompletedKey;
        static void SayCompletedKeyOnce()
        {
            if (_saidCompletedKey) return;
            _saidCompletedKey = true;
            Debug.Log("[Daily] `progress` **到顶**（`Progress >= Target`）⇒ 照原版显示 `completedMessage`，"
                      + "而它存的是 **I2 词条【键】** `" + CompletedMessage + "` —— 🔴 原版在这一句外面套了 "
                      + "`I2_Loc_LocalizationManager.GetTranslation`，**本地没有语言表** ⇒ 我们**照抄这个键本身**"
                      + "（不自己编英文冒充原版文案）。判据：MB `3476392019656054992` `displayCompletedMessage=1`"
                      + " + `MissionCounterDisplay__Setup.c:24-26,68-79`。");
        }

        /// <summary>这条任务**原版 `IsComplete()`** 那一态 = **奖励已领取 / 已结算** —— **不是**「进度到顶」。
        /// 判据（2026-10-04 两条**互相独立**的证据链，结论一致）：
        /// ① `MissionChallengeProgress__IsComplete.c:18-26` 的整条算式**只读领取域**：
        ///    `Count(AvailableRewards()) <= *(int*)(this + 0x10)`，而 `+0x10` = 基类
        ///    `ChallengeProgress.collectedRewards`（字段序坐实）—— **从不碰 `currentValue` / `MaxValue`**；
        ///    `AvailableRewards` 的迭代器筛的是「下标 ≥ 已领数**且那条里程碑还有没领的奖励**」。
        /// ② 🔴 **决定性**：每日任务行的 `progress` 节点 = `MissionCounterDisplay`（`displayRule = 1` ⇒
        ///    只在 `!IsComplete` 时可见），而它的 MB 实读是 `displayCompletedMessage = 1` ·
        ///    `completedMessage = "Missions/Completed"`，`MissionCounterDisplay__Setup.c:24-26,68-79`
        ///    恰在 `currentValue >= MaxValue` 时显示那句「Completed」
        ///    ⇒ **「到顶未领取」这一态必须可见** ⇒ 那时 `IsComplete` 必为 `false`。
        /// ⇒ 用我们三态里的 **`State.Claimed`**（`CollectDaily` 置的那一态）。
        /// ⚠️ 「进度到顶」（= 我们的 `State.Collectable`）**表达不了「已领取」** ——
        ///    `CollectDaily` 只改 `St`、**不动 `Progress`**（所以两个谓词不可互推）。
        /// 🔴 **反向也踩过**：1.0 版这里取「进度到顶」，于是「10/10 未领取」那一行把垃圾桶/timer 都算成「已完成」——
        ///    方向与真相**相反**（还删掉了原版的一个能力：到顶未领那一行**是可以重摇的**）。</summary>
        public static bool DailyClaimed(int i) { return At(i).St == State.Claimed; }

        /// <summary>登录卡那一态是不是**已领取**（= 原版 `IsComplete()` 那一态，谓词同 `DailyClaimed`）。
        /// 🔴 **这是 A75② 的判据**：登录卡的 `Timer` 原版是 **`displayRule = 2 (WhenComplete)` ⇒ 已领取才显示**。
        /// 判据（真包实读，2026-10-05 复核）：登录卡 `MissionContainer` MB `7589217681052316345` 的
        /// `infoDisplays` 第 2 项 = MB **`3730529517176468153`**（`MissionTimerDisplay`）的 **`displayRule = 2`**，
        /// 其 `m_GameObject` → `GameObject/Timer_-4321384230747458887.json`；
        /// 规则本体 = `DF:MissionInfoDisplay__Initialize.c:10-27`
        /// （`show = (WhenComplete && IsComplete) || (WhenActive && !IsComplete)`，见 `BuildDailyRow` 那段注释）。
        /// ⚠️ **我们原来恒画它**（`Resets in 12h 34 m`）—— 与每日行 `description`/`timer` 互斥是**同一条规则**。</summary>
        public static bool LoginClaimed() { return _loginState == State.Claimed; }

        /// <summary>骷髅卡是否**已领取**（谓词同 `DailyClaimed`）。
        /// ⚠️ 它**不管任何件的显隐** —— 三张单例卡的 `infoDisplays` 里**根本没有 `Collect` 那一件**、
        /// `dr` 几乎全是 `-1`（`[Flags]` 全位置 1 ⇒ 恒真），那三颗 `Collect` 的显隐走
        /// `interactable = CanCollect()`。**别把 A44 甲「同吃 `displayRule`」那条口径推广到这里**。</summary>
        public static bool SkullsClaimed() { return _skullsState == State.Claimed; }

        /// <summary>周常是否**已领取**（同上，只管状态、不管显隐）。</summary>
        public static bool WeeklyClaimed() { return _weeklyState == State.Claimed; }

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
        /// <summary>第 `i` 档（0 基）的**阈值** = `3 / 10 / 25 / 50 / 100`。
        /// 判据 = **用户提供的原版实拍**（卡上那五个方框里逐格印着的数，见 `_skullsSteps` 那段注释）；
        /// ⛔ 它不是「`200` 五等分」那套（那套是我们挑的，2026-10-11 A370 已删）。
        /// <para>⚠️ **它也是「方框里那个数」的唯一数据源** —— 原版那五格各挂一个 `MissionMilestoneStep`，
        /// 其 `text`（TMP，出厂占位串 `'1'`、40×40、`fs 42.2`、`auto[10~50]`、`Center/Midline`、
        /// `EverguildTextController`）由 `text.text = value.ToString()` 填成**阈值本身**：
        /// `d:/2/tools/decomp_full/MissionMilestoneStep__Setup.c` 头两句 =
        /// `uVar5 = System_Int32__ToString(local_res10)` → `(**(*plVar1 + 0x558))(plVar1, uVar5, …)`
        /// （`0x558` = `TMP_Text.set_text`），而 `local_res10` = 该方法的**第 1 个 int 形参**；
        /// 调用点 `MissionMilestonesDisplay__Setup.c` 传的是 `*(lVar2 + 0x14)` = **那条里程碑自己的阈值**
        /// （第 2 个 int 形参 `*(lVar3 + 0x40)` 才是 `currentValue`，用来算 `bVar2 = 阈值 &lt;= 当前值`）。
        /// 实读（`工具/menu_dump.py bundle_menus_assets_all "Daily Skulls Mission Container Small"`）：
        /// 5 格的 `…/holder/text` 都是 40×40、占位 `'1'`、fs 42.2 —— 占位值，运行时被覆盖。</para></summary>
        public static int SkullsStepTarget(int i)
        { return _skullsSteps[Mathf.Clamp(i, 0, _skullsSteps.Length - 1)]; }

        /// <summary>第 `i` 档达成了没有（`_skullsCount >= 阈值`）。
        /// ⚠️ **只管「方框亮不亮」，不管「能不能领」** —— 可点性见 `CanCollectSkulls`。</summary>
        public static bool SkullsStepDone(int i) { return _skullsCount >= SkullsStepTarget(i); }

        // ============================================================ 卡面的奖励格（🆕 2026-10-05 · **B4**）
        //
        // 🔴 这里原来是一张**按下标**的固定表 `RewardIcon(i)` / `RewardCount(i)`（`i == 1 ? "20" : "150"`），
        //    而**领取**走的是**任务那一份**（`CollectDaily` → `ParseCount(t.RewardText)`）⇒ 两张表在第 3 行
        //    对不上：卡面画「`40K_missions_icon_Daily_skulls` ×150」、实发 `_daily[2]` 的**金块 ×200**
        //    = **图标 / 数量 / 发放三者不一致**；而且重摇换了任务之后，格子里那个数**不会跟着变**
        //    （它按下标取，跟任务是两回事）。
        // ✅ **B4 起的新规矩（别再退回去）**：**卡面那一格画的，必须就是 `CollectXxx` 要发的那一份** ——
        //    · 每日任务行 → 读**那条任务**（`DailyRewardArt` / `DailyRewardText`，与 `CollectDaily` 同源）；
        //    · 登录卡 / 骷髅卡 → 各读**自己那一份**（`LoginReward*` / `SkullsReward*`）。
        //    ⛔ **不要再加一张「按下标查」的公共表** —— 那就是 B4 的病根（登录卡与骷髅卡会跟着一起错）。
        //
        // ============================================================ 「格数」的数据源：原版查清了，**本地没有**（块6 · 件②）
        //
        // 🔴 原版那几格**不是 prefab 写死的，是数据驱动的**（`MissionRewardsDisplay__Setup.c:46-167`）：
        //    `DestroyAllChildren(transform)` → 取 `MissionChallengeProgress.AvailableRewards()` →
        //    **一个 `ChallengeMilestone` 一格**（组内 1 条取 `First`、多条取 `Sum` 合成一个 `RewardInfo`，`:123-150`）
        //    → 逐格 `Instantiate(RewardsPrefab /*+0x28*/)` + `MissionRewardItem__Setup`（`:157-162`）。
        // 数据链（逐环亲读，**全部在客户端之外**）：
        //    `MissionChallengeProvider__GetAssets.c` → `Everguild.LiveOps.Config.ConfigManager.GetConfig<>()`
        //      → `ConfigManager.Unpack(Dictionary<string,string> titleData)`（`.c` 逐条调 `Initialize(titleData)`；
        //        调用点 = `PlayerDataManager._UnpackUserInfo_d__522__MoveNext.c`，那个 `userInfo` 是
        //        PlayFab 的 `GetPlayerCombinedInfoResultPayload`）
        //      → 承载类 = **`MissionsConfig : IConfigData`**（`Assembly-CSharp/MissionsConfig.cs`：
        //        `LoadableReference<MissionChallenge> dailyLoginRef` · `List<MissionChallenge> challenges` ·
        //        `List<ChallengeList> challengeLists` · `MissionConfig{ MissionType type; int maxAmount; }[] configs`）
        //      → PlayFab **Title Data** 的键 = `TitleDataKey.Challenges = 17` / `ChallengesNew = 18`
        //        （`Assembly-CSharp/TitleDataKey.cs`）。
        // ⇒ **拿不到，缺的就是它**：PlayFab Title Data 的 `Challenges` / `ChallengesNew` 那两份 JSON
        //    （= `MissionsConfig` 的内容，含 `dailyLoginRef` 指的那条 `MissionChallenge`）。
        //    本地实测：84 个 bundle 里没有任何 `MissionChallenge` 资产（`MissionChallenge` 是 `[Serializable]`
        //    纯数据类，**不是 SO/MonoBehaviour**，只由上面的 config 下发）；`d:/2` 全盘按 `*.json/*.txt/*.csv`
        //    搜 `challengeType`/`ChallengeMilestone` **零命中**；服务器已关 ⇒ 下不到。
        // ⇒ 我们这边的「格数 / 币种 / 数量」**只能是我们挑的**（= 下面这张表的长度），
        //    仍然必须满足「**卡面画的 = `CollectXxx` 发的**」那条自洽要求（B4）。

        // ---- 登录卡（`Daily Login Bonus Container`）：**两格** ----
        // 🔴 **2026-10-05（块6）依据【已被独立审查证伪】⇒ 按铁律 3 降级为「这是我们挑的，不是原版的做法」**。
        //    这里原来写的是「判据 = 原版 prefab §3·3 #12/#13 的 `count` 字面量」—— **站不住**：
        //    ① **`count` 不是数值依据**：`d:/2/tools/decomp_full/MissionRewardsDisplay__Setup.c:46` 头一句就是
        //       `SupportMethods__DestroyAllChildren(transform)`；随后逐条 `Instantiate(RewardsPrefab)` +
        //       `MissionRewardItem__Setup`，而后者（`MissionRewardItem__Setup.c:19-33`）是
        //       `amountText.text = info.amount.ToString()`、`DestroyAllChildren(drawerHolder)` 再
        //       `ItemDrawer.Draw(drawerHolder, item, amount, 10)`
        //       ⇒ **格数 / 图标 / 数量三样全是【运行期】从服务端 `RewardInfo` 填的**，prefab 里那串 `count`
        //       与那几个子件**都会被删掉重建**。他证：每日行那一格的 `count` 也是 `'100'`（每日任务奖励不可能
        //       是 100），同族还有一格是 `'?'`（谁也不会把 `?` 当真值）。
        //    ② **占位图也不是 gold**：`Daily Login Bonus Container/footer/Rewards/Reward Display Mission/
        //       drawerHolder/Currency/Content/Image` 的 sprite 实测 = **`40k_topmarquee_currency_crystal`**
        //       （90×90）—— 原来这里写「金块」是**读错**（实读见 `bundle_menus_assets_all`，`menu_dump` 打的就是它）。
        //    ⇒ 下面这两个数（**币种 + 数量**）**都是我们挑的**：第 1 格 `40k_topmarquee_currency_gold` ×100、
        //      第 2 格 `40k_Achievements_icon_seal_points` ×20。
        //    ✅ **仍然成立的那一条是「自洽」**（B4 的原文要求）：**卡面这两格画的，必须就是 `CollectLogin` 发的那两份**
        //      —— 这条与「对不对得上原版」无关，别因为依据被证伪就把它一起退掉。
        //    📌 出厂值本身读得没错（`count` = `'100'` / `'?'`、`CurrencyDrawer` / `CampaignPointDrawer`），
        //      出处 = `资料/日常_原版规格.md` §3·3 #12/#13；**错的是把它当数值依据**。
        static readonly string[] _loginRewardArt =
        { "40k_topmarquee_currency_gold", "40k_Achievements_icon_seal_points" };
        static readonly int[] _loginRewardCount = { 100, 20 };

        /// <summary>登录卡奖励格的**格数** —— = 我们这张表的长度（本表 2 格）。
        /// 🔴 **2026-10-05（块6）更正**：原来这里写「**原版 prefab 是 2 格**」—— 那句**不是**格数的依据。
        /// 原版的格数是**数据驱动**的：`MissionRewardsDisplay__Setup` 先 `DestroyAllChildren`，再按
        /// `MissionChallengeProgress.AvailableRewards()`（= `MissionChallenge.rewards` 的 `ChallengeMilestone[]`）
        /// **一条一格** `Instantiate(RewardsPrefab)`；prefab 出厂那几个孩子**只是占位、会被删掉**。
        /// 那份 `MissionChallenge` 数据**本地没有**（来自 PlayFab Title Data 的 `MissionsConfig`，
        /// 见本文件上面「「格数」的数据源」那一段）⇒ 我们只能按**自己这份 mock 奖励表**定格数（铁律 3：这是我们挑的）。</summary>
        public static int LoginRewardCells { get { return _loginRewardArt.Length; } }
        /// <summary>登录卡第 `i` 格要画的图 —— 与 `CollectLogin` 发的是**同一份**（B4）。
        /// ⚠️ **币种也是我们挑的**（原来写「第 1 格 = 金块」是**判错**：prefab 那一格的占位图实测是
        /// `40k_topmarquee_currency_crystal`，不是 gold —— 见上面「登录卡」那一段）。</summary>
        public static string LoginRewardArt(int i) { return _loginRewardArt[LI(i)]; }
        /// <summary>登录卡第 `i` 格要画的数量 —— 与 `CollectLogin` 发的是**同一份**（B4）。
        /// ⚠️ **两格的数量都是我们挑的**（原版走运行期 `RewardInfo.amount`，本地没有那份数据）。</summary>
        public static int LoginRewardCount(int i) { return _loginRewardCount[LI(i)]; }
        static int LI(int i) { return Mathf.Clamp(i, 0, _loginRewardArt.Length - 1); }

        // ---- 骷髅卡（`Daily Skulls Mission Container`）：**一格** ----
        // 🔴 **2026-10-05（块6）依据【已被独立审查证伪】⇒ 按铁律 3 降级为「这是我们挑的，不是原版的做法」**。
        //    这里原来写「图 = 骷髅、数量 = **200**（**这两个都是原版的**）」—— **两半都是判错**：
        //    · **数量**：那个 `count` 出厂 `'200'` 只是**占位**，运行期会被 `DestroyAllChildren` +
        //      `MissionRewardItem__Setup` 重建（判据链同登录卡那一段）；
        //    · **图**：**prefab 里根本没有骷髅这张图** —— 实读 `Daily Skulls Mission Container Small` →
        //      `footer/Rewards/Reward Display Mission/drawerHolder/Icon Campaign Points Drawer Variant/Content/Image`
        //      的 **`m_Sprite = 0`（没图**，`menu_dump` 打的字面就是 `<无图>`），同级那件是**同族通用的**
        //      `Campaign Glow` = `40K_genearl_icon_Campaign points_big` ⇒ 真值由 `CampaignPointDrawer`
        //      **运行期**画。原来把「Drawer 的类名」当成了「图 = 骷髅」的依据。
        //    ⇒ 「骷髅 ×200」= **我们挑的**（走「卡面画的 = `CollectSkulls` 发的」那条自洽要求）。
        //
        // ============================ 🆕 2026-10-11（批次 · A370）**有真值了** ============================
        // 🔴 **铁律 5 更正**：上面那句「图 = 骷髅」**方向就是错的** —— 骷髅是**进度的【输入】**
        //    （玩家在战斗里打出来的），**不是奖励产出**；照那么发等于「自己给自己发进度」。
        // ✅ **判据 = 用户提供的原版实拍**
        //    （`C:\Users\qjh36\Desktop\奖励—布道所（每日任务）参考图.png` 左栏「每日骷髅头」卡）：
        //    `x0` 下面那**一颗图** + `× 200` —— 那颗图是**蓝色漩涡 = 活动点（Campaign Points）图标**，
        //    与①同桌「布道所」三条任务各给的 `300`、②顶栏那颗 `300/2000` **是同一张图**
        //    ⇒ 它是个**通用货币**（用户 2026-09-17 拍的边界「不做真实经济、资源固定 9999」⇒
        //      `Wallet` 就是个记账本，加一个 key 而已）。
        // 📌 **图名 = `40K_genearl_icon_Campaign_points_big`**（⚠️ 原版拼写就是 `genearl`）—— 两条独立证据：
        //    ① **实拍**：那颗蓝色漩涡；
        //    ② **这条格子自己的 prefab**：`Icon Campaign Points Drawer Variant/Content/` 下**唯一一张有图**的
        //       子件 `Campaign Glow` 的 sprite 实测 = **`40K_genearl_icon_Campaign points_big`**
        //       （`menu_dump.py bundle_menus_assets_all "Daily Skulls Mission Container Small"` 实读；
        //        同级的 `Image` 是 `<无图>` —— 它由 `CampaignPointDrawer.DrawForArmy` 运行期填
        //        **阵营徽记**，而 `army == Neutral` 时那张图为空 ⇒ 看到的就只有这颗漩涡）。
        //       ⚠️ 我们工程里落盘名是把**切片名里的空格换成下划线**
        //       （`CardArt.MenuUi` 的命名约定；导出器 `工具/import_original_art.py:463`）⇒
        //       `40K_genearl_icon_Campaign points_big` → `40K_genearl_icon_Campaign_points_big`
        //       （已在 `Resources/Art/ui_menu/`，`Shell/CampaignTab.cs:892` 也在用同一张）。
        // 📌 **数量 200**：实拍上写的就是 `200` ⇒ 与 prefab 那个出厂占位 `'200'` **两处一致**
        //    （占位值本身不是依据，这条是「实拍 + 占位恰好同值」）。
        // ⚠️ 别和卡上那个 `counter text`（`x160`）弄混：那是**进度**（`_skullsCount`），这是**这一格的奖励**。
        /// <summary>骷髅卡那一格要画的图 —— 与 `CollectSkulls` 发的是**同一份**（B4）。
        /// ✅ **2026-10-11（A370）起这是「活动点」图标**（判据 = 用户提供的原版实拍 + 本格 prefab 的
        /// `Campaign Glow`，见上面那一段）。**改之前画的是骷髅** —— 那等于**自己给自己发进度**。</summary>
        public static string SkullsRewardArt() { return "40K_genearl_icon_Campaign_points_big"; }
        /// <summary>骷髅卡那一格要画的数量 —— 与 `CollectSkulls` 发的是**同一份**（B4）。
        /// ✅ **200 是实拍上的数**（`× 200` 活动点）；⚠️ 它**不是**「200 个骷髅」
        /// （2026-10-11 A370 之前那个口径是错的 —— 数量没变，**含义变了**）。</summary>
        public static int SkullsRewardCount() { return 200; }

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
        /// <summary>**当前**用来挑候选的那一份池子 —— 出厂 = `_rerollPool`；**只有自检**会换它
        /// （`SetRerollPoolForTest`，为了把「池子不够」那条兜底逼出来）。运行期别动。</summary>
        static Task[] _rerollPoolCur = _rerollPool;
        /// <summary>池子全在列表里时的轮换游标（**只在池子不够时用**，会出声）。</summary>
        static int _rerollRound;
        /// <summary>累计重摇了几次（自检用：`Cancel`/点窗外**不该**让它变大，只有 `Confirm` 会）。</summary>
        public static int RerollCount { get; private set; }

        /// <summary>自检用：把重摇池**临时换成**指定的几条（传 `null` 还原出厂池）。
        /// 🔴 **为什么要有这个口**：出厂池 **5 条 > 列表 3 条** ⇒ 出厂状态下
        /// 「候选为空 ⇒ 走兜底」那一条**永远到不了**，而它正是 2026-10-04 审查挑出来的那一条
        /// （当时还带两个 bug：可能**摇回同一条**、`Debug.LogWarning` 的**下标差 1**）。
        /// 「走不到的分支」不配断言就永远修不实 ⇒ 换一个**两条都已在列表里**的小池子逼它出来。</summary>
        public static void SetRerollPoolForTest(Task[] pool) { _rerollPoolCur = pool ?? _rerollPool; }

        /// <summary>自检用：把第 i 条的进度**定死**（`St` 按「到顶 ⇒ 可领取」重算；目标值不动）。
        /// `ResetMissionsForTest` 只给一套固定初值（三条**都不到顶**）⇒ 单靠它造不出「到顶」那一态。
        /// ⚠️ 它置的是 **`Collectable`（到顶未领）**，**不是 `Claimed`** —— 要「已领取」得再调
        /// `ForceDailyClaimedForTest(i, true)`（原版那两个件的显示条件吃的是**已领取**那一态，见 `DailyClaimed`）。</summary>
        public static void ForceDailyProgressForTest(int i, int progress)
        {
            var t = At(i);
            t.Progress = Mathf.Clamp(progress, 0, t.Target);
            t.St = t.Progress >= t.Target ? State.Collectable : State.InProgress;
        }

        /// <summary>自检用：把第 i 条置成**已领取 / 未领取**（= 原版 `IsComplete()` 那一态，判据见 `DailyClaimed`）。
        /// **进度一个数都不动** ⇒ 这样「**到顶未领取**」与「**已领取**」两种状态可以分别造出来
        /// （两者的 `Progress` 一样、只有 `St` 不同），自检正是靠这个把谓词的两支都逼出来。
        /// 传 `false` 回到 `InProgress`（要「可领取」那一态请用 `ForceDailyProgressForTest`）。</summary>
        public static void ForceDailyClaimedForTest(int i, bool claimed)
        {
            At(i).St = claimed ? State.Claimed : State.InProgress;
        }

        /// <summary>自检用：把**登录卡 / 骷髅卡 / 周常**三张单例卡的状态定死（A75 那三条断言要两态对比）。
        /// ⚠️ 传 `Collectable` 才领得到（`CollectSkulls` / `CollectWeekly` 的守卫）；
        /// **登录卡是单机口径**——「没领过就能领」（`CollectLogin` 只挡 `Claimed`），所以它用不着 `Collectable`。</summary>
        public static void ForceLoginStateForTest(State st) { _loginState = st; }
        /// <summary>自检用：骷髅卡的状态（`Collectable` 才领得到）。
        /// ⚠️ **2026-10-11（A370）起「领得到」还要再过一关**：`CanCollectSkulls` 里多了一条
        /// `SkullsStepDone(0)`（= 至少过第 1 档）。光置这一态、计数不够时仍然领不到。</summary>
        public static void ForceSkullsStateForTest(State st) { _skullsState = st; }

        /// <summary>🆕 **2026-10-11（A370）自检用**：把骷髅计数**定死**（`SkullsStepDone` / `SkullsCounter` 都读它）。
        /// 🔴 **为什么必须有个口**：五档阈值是**两态**判据（`3` 达成 / `2` 未达成），
        /// 而 `_skullsCount` 是文件私有、出厂恒 `160` ⇒ 不注入就只能断到一个状态
        /// （本仓那条「**弱断言分不出两种状态**」的坑就是这么踩的）。</summary>
        public static void ForceSkullsCountForTest(int n) { _skullsCount = Mathf.Max(0, n); }

        /// <summary>🆕 **2026-10-11（A370）自检用**：现在这个计数（自检要**还原**它 ——
        /// 「另一条研究正在跑怎么打出骷髅」，那个值不许被本节改完之后留在工作区）。</summary>
        public static int SkullsCountValue() { return _skullsCount; }
        /// <summary>自检用：周常的状态（`Collectable` 才领得到）。</summary>
        public static void ForceWeeklyStateForTest(State st) { _weeklyState = st; }

        /// <summary>把第 `i` 条**换掉**（原版：`Confirm` → 服务端 `RerollChallenge`）。
        /// 返回**新任务的描述**（自检要拿它比）。⚠️ 旧任务**直接丢了**（原版服务端也是换一条新的）。
        /// ✅ 自检要比「换前/换后」的那两个字段走 `DailyRewardText` / `DailyRewardArt` ——
        /// **卡面那一格画的就是这一份**（B4，2026-10-05 起；重摇之后格子里那个数**会跟着变**）。</summary>
        public static string RerollDaily(int i)
        {
            var t = At(i);
            string oldDesc = t.Desc;

            // 候选 = 池里**当前列表里没有的**（自己那条也算「在列表里」⇒ 不会被选中）
            var cand = new System.Collections.Generic.List<Task>();
            for (int k = 0; k < _rerollPoolCur.Length; k++)
            {
                bool used = false;
                for (int j = 0; j < _daily.Length && !used; j++)
                    if (_daily[j].Desc == _rerollPoolCur[k].Desc) used = true;
                if (!used) cand.Add(_rerollPoolCur[k]);
            }
            Task src;
            if (cand.Count > 0) src = cand[_rerollRng.Next(cand.Count)];
            else
            {
                // 🔴 **兜底：池子不够（5 条池全被列表占了才走到）** —— 2026-10-04 审查在这里挑出两个 bug，
                //    两个都改了（旧写法留着的话，「真跑到」时**摇回同一条**= 白点一次 Confirm）：
                //      ① 旧：`src = _rerollPool[_rerollRound % Length]` —— **不排除「要顶替的这一条自己」**
                //         ⇒ 可能取回**同一条**（描述一个字没变，玩家以为坏了）。
                //         新：从游标起**挑第一条 `Desc != oldDesc`** 的 ⇒ **保证真换了**。
                //      ② 旧：`Debug.LogWarning` 打在 `_rerollRound++` **之后** ⇒ 报的是**下一条**的下标（差 1）。
                //         新：打的是**真正取用的那一条**的下标。
                //    ⚠️ 出厂池 5 条 > 列表 3 条 ⇒ 出厂状态下这条**到不了**（自检用
                //    `SetRerollPoolForTest` 换个小池子把它逼出来，`Editor/RewardsScene.cs` 有断言守着）。
                // 🔴 **空池要先挡**（2026-10-04 A36-M6）：池长 0 时下面那句 `_rerollRound % Length`
                //    会**除零崩**（只有自检口 `SetRerollPoolForTest(new Task[0])` 造得出来，出厂池到不了）。
                if (_rerollPoolCur.Length == 0)
                {
                    Debug.LogWarning("[Daily] 重摇：候选池**是空的**（`SetRerollPoolForTest(new Task[0])` 才造得出来）"
                                     + " ⇒ **这一条换不了**，保持原样（红线：不许静默失败，也不许假装换了）");
                    return oldDesc;
                }
                int start = _rerollRound % _rerollPoolCur.Length;
                int picked = -1;
                src = null;
                for (int k = 0; k < _rerollPoolCur.Length; k++)
                {
                    int idx = (start + k) % _rerollPoolCur.Length;
                    if (_rerollPoolCur[idx].Desc != oldDesc) { src = _rerollPoolCur[idx]; picked = idx; break; }
                }
                _rerollRound++;        // ⚠️ **游标照走**（它是「下次从哪条起找」的游标，**不是计数**）
                if (src == null)
                {
                    // 池子里**每一条都与被顶替的那条同名**（池只有一条时会这样）⇒ **真的换不了**：
                    // 如实出声、**任务一个字都不改**、**`RerollCount` 不加**（它在下面 `return` 之后那一段，
                    // 这里早退 ⇒ 不会计数）。⚠️ 但**上面那句 `_rerollRound++` 是执行了的**（游标，不是计数）——
                    // 1.0 版把「不计数」写在它旁边，容易被读成「这一行没跑」（2026-10-04 A36-M6 改清楚）。
                    Debug.LogWarning("[Daily] 重摇：池里**没有一条**与「" + oldDesc + "」不同的任务 ⇒ "
                                     + "**这一条换不了**，保持原样（原版这一步是服务端换一条新的，我们没有更大的池子）");
                    return oldDesc;
                }
                Debug.LogWarning("[Daily] 重摇的任务池**已经全在列表里**了 ⇒ 退回**轮换**取池第 " + picked
                                 + " 条「" + src.Desc + "」（⚠️ 已保证 **≠** 被顶替的那一条「" + oldDesc + "」；"
                                 + "原版这一步是服务端换一条新的，我们没有更大的池子 —— 如实说，不假装它是新任务）");
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

        /// <summary>四态。**公式照原版**（`GetCurrentState`）：普通那条 + Premium 那条。
        /// <para>🔴 **2026-10-11（批次1 · W1）就地修一个**真缺陷**（铁律 5·b/11）**：这一行原来只判
        /// `i &lt; RewardsCollected`（那个常量是「开机时已经领到第几天」，恒为 2），
        /// **完全没读 `_rewardClaimed[]`** —— 结果是「本局领过的格子**看不出来**、而且**能无限领**」：
        /// `CollectReward` 的守卫问的就是 `RewardStateOf != Unlocked`，而它在领完之后**照样回 `Unlocked`**
        /// ⇒ 每点一下就再发一次奖励、抽屉上的 `Claimed` / `colider` 状态也永远不变。
        /// 原版那一侧 `collectedRewards` 是**服务端下发的「已领到第几天」**（我们这边就是这两项之和：
        /// 开机进度 + 本局领过）。判据 → `DailyRewardItemContainer__GetCurrentState.c:18-46`
        /// （`index &lt; collectedRewards ? Collected : …`）。</para></summary>
        public static RewardState RewardStateOf(int day, bool premium)
        {
            int i = RI(day);
            int normal = (i < RewardsCollected || _rewardClaimed[i])
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

        /// <summary>领一天的奖励。**只有 `Unlocked` 那一态能领**（原版的 `colider` 也只有那一态可点）。
        /// 🆕 **2026-10-11（批次1 · W1 · A309）起返回「这一下是不是真的领到了」**（与 `CollectDaily` 同一条口径）
        /// —— 领到了才弹那扇 `Reward Window`（见下面「领奖窗」那一段）。老调用点当语句用，照样编得过。</summary>
        public static bool CollectReward(int day, bool premium)
        {
            var st = RewardStateOf(day, premium);
            if (st != RewardState.Unlocked)
            { Say($"第 {day + 1} 天{(premium ? " Premium" : "")}奖励现在是 `{st}`，领不了"); return false; }
            _rewardClaimed[RI(day)] = true;
            string art = RewardIconOf(day, premium);
            int n = RewardAmount(day, premium);
            Wallet.Grant(art, n);
            Say($"第 {day + 1} 天{(premium ? " Premium" : "")}奖励已领取");
            ShowCollectedWindow(art, n, premium);
            return true;
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

        public static bool CollectStreak(int i)
        {
            if (!StreakRewardUnlocked(i)) { Say($"连登第 {i + 1} 格现在领不了"); return false; }
            _streakClaimed[SI(i)] = true;
            string art = StreakRewardIcon(i);
            int n = _streakAmount[SI(i)];
            Wallet.Grant(art, n);
            Say($"连登第 {i + 1} 格已领取");
            ShowCollectedWindow(art, n, false);       // 连登轨只有一条，没有 Premium 档 ⇒ `TierBasic`
            return true;
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

        // ---- 🆕 2026-10-05（B4）：**三颗（+ 登录那颗）`Collect` 的可点性判据 —— 原版 `CanCollect()`** ----
        //
        // 🔴 **判据链（逐环亲读，三环齐全）**：
        // ① 挂载点：`MissionContainer.collectButton`（`dump.cs` 字段 `0x48`）。
        // ② `DF:MissionContainer__SetChallenge.c` 里那一下：
        //    `uVar3 = (**(code **)(*challenge + 0x198))(challenge, …);  Selectable.set_interactable(collectButton, uVar3);`
        //    —— 虚表 `0x198` 那一槽 = `MissionChallengeProgress.CanCollect`
        //    （`d:/2/tools/il2cpp_out/dump.cs` 实读 **`Slot: 6`**，与派活单上那个槽号对得上）
        //    ⇒ **可点性 = `CanCollect()`，不是 `displayRule`**（后者只管显隐）。
        // ③ `DF:MissionChallengeProgress__CanCollect.c` 本体：
        //    `if (!this.canCollect /*0x48*/) return false;` → 拿 `challengeId` 去 `AssetLocator` 取 `MissionChallenge`
        //    → 若它某个标志位（`+0x2d`）为真 ⇒ `true`；否则 `RewardsToCollect().All(m => m.<0x18>.All(r => r.CanCollect()))`。
        //    ⇒ 语义 = **「这条挑战真的可以领了」**（服务端下发的 `canCollect` + 每个奖励都可领）。
        //
        // 🔴 **我们这一侧的对应物**：我们**没有**一个叫 `CanCollect` 的方法，但**同一份布尔**本来就写着 ——
        //    它就是 `CollectXxx` 里那一句**三态守卫**（`St == Collectable` / `_loginState != Claimed`）。
        //    ⇒ **就地提成谓词**（不是新造一条判据），`CollectXxx` 与按钮**共用同一个** ——
        //    照本仓那条「两处写同一条规则 = 迟早不一致」的规矩，**判据只有一份**。
        //    ⚠️ 登录卡那一颗是**单机口径**：「没领过就能领」（`CollectLogin` 只挡 `Claimed`），
        //      所以它用不着 `Collectable` —— 别把另外三颗的 `Collectable` 抄过去。
        //    ⛔ **别把 A44 甲那条「四个件同吃 `displayRule`」推广到这里**：三张单例卡的 `infoDisplays` 里
        //      **根本没有 `Collect` 那一件**（`dr` 几乎全是 `-1`）⇒ 那三颗的显隐**不由 `displayRule` 管**。
        //
        // 🔴 **`interactable = false` 在原版是「又灰又点不动」**（我们两半都接）：
        //    灰 = `EverguildButton.DoStateTransition(4 Disabled)` → `SetToStateActiveOrDisabled` → 材质换
        //    `Everguild/UI/Greyscale`；点不动 = `Selectable.OnPointerClick` 头一句返回。
        //    **前提已核**：真包 `bundle_menus_assets_all` 里这 **12 颗 `Collect`（`Generic UI Button`）的
        //    `colorTintGreyOnDisable` 全是 1**（如每日行 MB `-4755074315463813377`、周常 MB `3762440415779886847`）
        //    —— 不为真时原版那一下**根本不灰**，所以这一条必须核过才能照做。

        /// <summary>原版 `MissionChallengeProgress.CanCollect()`（`Slot: 6`）在我们这一侧的**同一份布尔**：
        /// 这条每日任务现在点得动吗（= `CollectDaily` 那句三态守卫）。见上面那一段判据链。</summary>
        public static bool CanCollectDaily(int i) { return At(i).St == State.Collectable; }
        /// <summary>周常那颗 `Collect` 可不可点（守卫原文见 `CollectWeekly`）。</summary>
        public static bool CanCollectWeekly() { return _weeklyState == State.Collectable; }
        /// <summary>骷髅卡那颗 `Collect` 可不可点（守卫原文见 `CollectSkulls`）。
        /// 🔴 **2026-10-11（A370）加了第二条**：`SkullsStepDone(0)` —— **至少过第 1 档**（现在第 1 档是 **3**，
        /// 不是 A370 之前的 40）。理由：原版那颗钮的可点性 = `MissionChallengeProgress.CanCollect()`
        /// （= 「这条挑战**真的可以领了**」），而骷髅卡这一条「可以领」的前件就是**里程碑至少亮了一格**；
        /// 只判 `_skullsState == Collectable` 的话，**「一档都没过却点了领」也放行**。
        /// ⚠️ 与 `CollectSkulls` 的那句守卫**是同一份布尔**（本仓规矩：两处写同一条规则 = 迟早不一致）。</summary>
        public static bool CanCollectSkulls() { return _skullsState == State.Collectable && SkullsStepDone(0); }
        /// <summary>登录卡那颗 `Collect` 可不可点。⚠️ **单机口径**：只挡「已领过」（见上面那段）。</summary>
        public static bool CanCollectLogin() { return _loginState != State.Claimed; }

        /// <summary>领第 `i` 条每日任务的奖。🆕 **A64 起返回「这一下是不是真的领到了」**：
        /// `true` = 兑现了（`St` 已置 `Claimed`）；`false` = **没领成**（未达成 / 已领过），**什么都没发生**。
        /// 三态守卫与原来的行为**逐字一致**，只是多了个返回值（老调用点当语句用，照样编得过）。
        /// 🔴 **为什么必须知道领没领成**：原版的「重建整页」发生在**领取成功之后** ——
        /// `Missions.CollectChallenge(mission, challenge, onComplete)` 的 `onComplete` =
        /// `MissionContainer.OnCollect`（六环链见 `资料/普查产出_1004/X2审查_A44甲.md` §一·附）；
        /// 而**没达成时那颗钮原版根本点不动**（`MissionContainer.SetChallenge.c` 里
        /// `UnityEngine_UI_Selectable__set_interactable(collectButton, CanCollect(challenge))`）
        /// ⇒ 不能「点了就重建」。
        /// 🆕 **B4 起守卫改调 `CanCollectDaily`** —— 按钮那一边（`MissionsTab`）用的是**同一个**方法。</summary>
        public static bool CollectDaily(int i)
        {
            var t = At(i);
            if (!CanCollectDaily(i)) { Say("每日任务 " + (i + 1) + " 还没达成，领不了"); return false; }
            t.St = State.Claimed;
            Wallet.Grant(t.RewardArt, ParseCount(t.RewardText));
            Say("每日任务 " + (i + 1) + " 已领取");
            return true;
        }

        /// <summary>🆕 **A75① 起返回「这一下是不是真的领到了」**（与 `CollectDaily` 同一条口径）：
        /// 那三张单例卡的 `Collect` 原版走**同一条** `OnCollect` 链 ⇒ 领到之后**重建整页**
        /// （判据见 `MissionsTab.CollectThenRebuild`）⇒ 调用方必须知道领没领成，
        /// ⛔ 不能「点了就重建」。
        /// ⚠️ 周常没有「奖励格」这一件（原版 `Rewards` 出厂 `activeSelf = false`，§3·5 #8）
        /// ⇒ 它这一份奖励**只有这里一处**（500 金块是**我们挑的**）。</summary>
        public static bool CollectWeekly()
        {
            if (!CanCollectWeekly()) { Say("周常还没达成，领不了"); return false; }
            _weeklyState = State.Claimed;
            Wallet.Grant("40k_topmarquee_currency_gold", 500);
            Say("周常已领取");
            return true;
        }

        /// <summary>🆕 **B4**：发的东西改成读**卡面那一格画的同一份**（`SkullsRewardArt/Count`）。
        /// 原来这里发的是 `("40K_missions_icon_Daily_skulls", 0)` —— **0 个**，而卡面那一格画的是
        /// 「封印点 ×20」（按下标表）⇒ **图标 / 数量 / 发放三者全对不上**。
        /// ✅ **2026-10-11（A370）起：图 = `40K_genearl_icon_Campaign_points_big`、数量 = 200**
        /// ⇒ 发的是 **200 活动点**（判据 = 用户提供的原版实拍；**改之前发的是 200 个骷髅**，
        /// 那等于**自己给自己发进度**，见上面「骷髅卡」那一段全文）。</summary>
        public static bool CollectSkulls()
        {
            if (!CanCollectSkulls()) { Say("每日骷髅还没达成，领不了"); return false; }
            _skullsState = State.Claimed;
            Wallet.Grant(SkullsRewardArt(), SkullsRewardCount());
            Say("每日骷髅已领取：" + SkullsRewardCount() + " **活动点**（`" + SkullsRewardArt() + "`）");
            return true;
        }

        /// <summary>🆕 **B4**：发的改成**卡面那两格画的同一份**（`_loginRewardArt/_loginRewardCount`，逐格发）。
        /// 原来这里发的是写死的**金块 ×100**，而卡面画的是「金块 ×150 + 封印点 ×20」（按下标表）
        /// ⇒ 两者对不上。现在两边同源：第 1 格 ×100、第 2 格 ×20 ——
        /// ⚠️ **币种与数量四个值全是我们挑的**（原版那两格走运行期 `RewardInfo`、占位图也不是金块；
        /// 依据 2026-10-05 被证伪 ⇒ 按铁律 3 降级，判据见上面「登录卡」那一段）。</summary>
        public static bool CollectLogin()
        {
            if (!CanCollectLogin()) { Say("今天的登录奖励已经领过了"); return false; }
            _loginState = State.Claimed;
            for (int i = 0; i < _loginRewardArt.Length; i++)
                Wallet.Grant(_loginRewardArt[i], _loginRewardCount[i]);
            Say("登录奖励（Day " + _loginDay + "/" + LoginTarget + "）已领取："
                + _loginRewardCount[0] + " + " + _loginRewardCount[1] + "（**逐格发卡面上画的那两份**）");
            return true;
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
        //
        // 🆕 **2026-10-11（批次 · A375）：骷髅也在这条链上**（在此之前**只有**三条每日任务接上了，
        //   骷髅卡从来没有「一局结束 → 累加」这条路 ⇒ 那边只能挂一个出厂 mock）。
        // 原版那条链**逐环亲读**（`d:/2/tools/decomp_full/`，第一权威；全文 → `资料/普查产出_1011/R1_每日骷髅与登录卡.md` §二）：
        //   ① `ChallengeLogMgr__LogMatchEnd.c`：`BattleEndSignal___ctor(signal, matchData, gameMode,
        //      **BattleScoreManager__GetSkullCount(manager + 0xF8)**, isWin)` —— 那个 int 落在
        //      `BattleEndSignal.SkullsCount`（`d:/2/tools/il2cpp_out/dump.cs:26914`，偏移 `@0x18`）；
        //   ② `SkullsCount__OnBattleEnd.c` 末句 = `MissionChallenge__UpdateProgress(this,
        //      *(undefined4 *)(param_2 + 0x18), **0**, 0)` —— 第 3 个实参 = `shouldOverride = 0`；
        //   ③ `MissionChallenge.__c__DisplayClass35_0___UpdateProgress_b__0.c`：
        //      `shouldOverride == false ⇒ currentValue = value + currentValue`。
        //   ⇒ **累加**（跨**当天所有**战斗之和），**不是**「取最好那一局」、也不是「按胜负给」。
        //   ⇒ 重置是另一条线（`SkullsCount.OnReset` ← `MissionResetSignal`），**本件不做**（如实记在 §五）。
        // ⚠️ **别在这里再数一遍档位**（铁律 6）：`skulls` 由调用方传 —— `BattleDriver` 传的是它那个
        //   `_foeSkullCount`（= 原版 `GetSkullCount()` 的**同一格字段**，与 HUD 的 `x N`、结算面板同源），
        //   而它本来就是用 `DeckRules.SkullsFor` 算出来的（**判据只此一处**）。

        /// <summary>第 i 条的当前进度值（自检用；别拿 `DailyCounter` 那个字符串去解析）。</summary>
        public static int DailyProgressValue(int i) { return At(i).Progress; }
        /// <summary>第 i 条的状态（自检用）。</summary>
        public static State DailyState(int i) { return At(i).St; }
        /// <summary>第 i 条**任务自己**的奖励数量/图标。
        /// ✅ **2026-10-05（B4）起：卡面那一格画的就是这一份**（`MissionsTab.BuildDailyRow` →
        /// `BuildRewardCell(…, DailyRewardArt(index), DailyRewardText(index), …)`），
        /// 与 `CollectDaily` → `ParseCount(t.RewardText)` **同源** ⇒ 图标 / 数量 / 发放三者一致，
        /// 重摇换了任务之后格子里那个数**会跟着变**。
        /// 🔴 **订正（铁律 5）**：这里原来写着「卡面上画的是 `RewardIcon(i)`/`RewardCount(i)` 那张
        /// **按下标**的表、**本件不动它**」—— 那张表**已经删了**（它就是 B4 的病根：
        /// 第 3 行画骷髅 ×150、实发金块 ×200）。**两份口径不许再分开**。</summary>
        public static string DailyRewardText(int i) { return At(i).RewardText; }
        /// <summary>第 i 条**任务自己**的奖励图标（口径见 `DailyRewardText`）。</summary>
        public static string DailyRewardArt(int i) { return At(i).RewardArt; }

        /// <summary>原版 `MatchData.GetMilestones()` 的**模式那一半** —— 这个 `PlayModes` 下，
        /// 一局的里程碑数组是**非空**还是**空**。空 ⇒ `BattleScoreManager.GetSkullCount()` 一颗都数不出来
        /// ⇒ **这一局 0 个骷髅**（不是因为打得好不好，是**这个模式根本不给**）。
        /// <para>判据 = `d:/2/tools/decomp_full/MatchData__GetMilestones.c`（逐 `case` 亲读，2026-10-11）——
        /// `switch (*(undefined4 *)(matchData + 0x18))`，而 `matchData.playMode` 就在 **`0x18`**
        /// （`dump.cs` `MatchData` 字段序）：</para>
        /// <list type="bullet">
        /// <item>`case 0,3,6,7,0xa,0xb,0xc,0xd,0xe` ⇒ `break`：往下建 **3 档**（值 `0x14`=**20** / **10** / **0**）</item>
        /// <item>`case 1,2,4,5,8,9` ⇒ 直接 `System_Array__Empty<T>()` **返回空数组**（一档都不建）</item>
        /// <item>`default` ⇒ 原版 `System_NotImplementedException___ctor` + **不返回**（抛）</item>
        /// </list>
        /// <para>取值名字（`d:/2/tools/il2cpp_out/dump.cs:46188` `enum PlayModes`）：
        /// **给骷髅** = `Classic 0` · `Dungeon 3` · `OfflinePractice 6` · `ClosedDeck 7` · `Replay 10` ·
        /// `RankedFriendly 11` · `OwnDeckTraining 12` · `Skirmish 13` · `Battle4Warpforge 14`；
        /// **一颗都不给** = `Duel 1` · `PracticeLodge 2` · `Tutorial 4` · `CutScene 5` · `Campaign 8` ·
        /// `TutorialReplay 9`。</para>
        /// <para>⚠️ `default` 那一支我们**不抛**（抛一下会把玩家正在打的那一局崩掉，批处理里也会把整条自检带走）——
        /// 改成**出声 + 当作「不给」**；结果与「空里程碑 ⇒ 0 颗」同值，而且**不静默**（红线）。</para>
        /// 🔴 **本工程今天只会传 0 与 13 进来**（对局侧的模式就是 `GameplayVariables` 的两套实例：
        /// `Ctx.Vars.IsSkirmish` ⇒ 13，否则 0 —— 见 `BattleDriver.cs` 结算处那段注释），**两个都在「给」那一组**
        /// ⇒ 这条闸**今天不改变任何一局的产出**。它的作用是把**零档那 6 个模式**的口径钉死
        /// （`Editor/RewardsScene.cs` 逐模式核过），将来做教程 / 战役页时一接就对上。
        /// <para>**改坏法**：把 `case 1/2/4/5/8/9` 并进返回 `true` 那支（或整条删掉恒 `return true`）⇒
        /// `RewardsScene` 那 6 条「一颗都不给」的断言全红。</para>
        /// </summary>
        public static bool ModeGivesSkulls(int playMode)
        {
            switch (playMode)
            {
                case 0: case 3: case 6: case 7: case 10: case 11: case 12: case 13: case 14: return true;
                case 1: case 2: case 4: case 5: case 8: case 9: return false;
                default:
                    Debug.LogWarning("[Daily] 未知 `PlayModes` = " + playMode + " ⇒ **这一局不给骷髅**"
                                     + "（原版 `MatchData.GetMilestones` 在 `default` 支是 `throw NotImplementedException` ——"
                                     + "我们**不抛**，改成出声 + 当作空里程碑。如实说，不静默）");
                    return false;
            }
        }

        /// <summary>把这一局的骷髅**累加**进当日计数（原版 `SkullsCount.OnBattleEnd` →
        /// `MissionChallenge.UpdateProgress(SkullsCount, shouldOverride: false)`，判据见本节开头那一段）。
        /// 返回**真的加进去几个**（模式不给 / 本局一颗没拿到 ⇒ 0），调用方拿它写日志。
        /// 🔴 语义是**累加**（`shouldOverride = false`）—— ⛔ 别改成赋值、也别改成取 `max`；
        /// 两条都是「取最好那一局」，与原版相反。
        /// 🔴 闸的顺序：**先判「本局有没有拿到」、再判模式** —— 两句都返回 0，顺序只影响日志，但先判数值
        /// 可以让「模式不给」那条日志只在**本来该给**的时候才打（不刷屏）。</summary>
        static int AddSkulls(int skulls, int playMode)
        {
            if (skulls <= 0) return 0;
            if (!ModeGivesSkulls(playMode)) return 0;
            _skullsCount += skulls;
            return skulls;
        }

        /// <summary>一局结束 —— 把**这一局的战果**推进日常进度（原版那一条 `BattleEndSignal` 的我们这一侧）。
        /// <para>`skulls` = 本局拿到的骷髅数（= 原版 `BattleScoreManager.GetSkullCount()` = 把**敌方督军**生命
        /// 削到 ≤20 / ≤10 / ≤0 **各 1 个**的**已达成档数**）；`playMode` = 本局模式的 `PlayModes` 号
        /// （只有 `{0,3,6,7,10,11,12,13,14}` 给骷髅，见 `ModeGivesSkulls`）。</para>
        /// ⚠️ **两个实参都不能自己造**：`skulls` 直接传 `BattleDriver._foeSkullCount`（它已经是
        /// `DeckRules.SkullsFor` 算出来的**同一格字段**）；`playMode` 传 `Ctx.Vars` 那一份对应的枚举值。
        /// ⛔ 别在这里、也别在 `BattleDriver` 里再数一遍档位（铁律 6：判据只写一处）。
        /// <para>**改坏法**：① 把 `AddSkulls` 那两行删掉 ⇒ 骷髅卡恒 `x0`（`RewardsScene` / `BattleScene`
        /// 里「累加」那几条全红）；② 把 `+=` 改成 `=` ⇒ 「第二局 +2」那条会得 2 而不是 3。</para>
        /// </summary>
        public static void OnBattleEnd(bool win, int damageToEnemy, int troopsPlayed, int skulls, int playMode)
        {
            // 三张每日任务卡：0 = Deal 500 damage to enemy units · 1 = Play 10 troops · 2 = Win 3 battles
            Advance(0, damageToEnemy);
            Advance(1, troopsPlayed);
            if (win) Advance(2, 1);
            int got = AddSkulls(skulls, playMode);          // 🆕 A375：本局 0~3 个（累加，不是覆盖）
            Say($"本局战果进了任务进度：对敌伤害 +{damageToEnemy} · 打出部队 +{troopsPlayed} · 胜 {(win ? 1 : 0)}"
                + $" · **骷髅 +{got}**（当日累计 {_skullsCount}）");
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
        /// 断言「推进了多少」会恒为 0（2026-09-23 第一版就栽在这）。
        /// 🔴 **2026-10-04（A36-M6）：连【内容】一起还原**（`Desc`/`Target`/`RewardArt`/`RewardText` 走
        /// `_dailyFactory` 那份出厂快照）—— 原来只还原 `Progress`/`St`，于是上一节里
        /// `RerollDaily` 换掉的那条任务（**文案/目标/奖励都变了**）会**泄漏到后面每一节**
        /// （复现：自检把重摇池换成两条已在列表里的、跑完只还原池子 ⇒ 第 0 条永久变成注入的那一条）。</summary>
        public static void ResetMissionsForTest()
        {
            for (int i = 0; i < _daily.Length; i++)
            {
                _daily[i].Desc = _dailyFactory[i].Desc;                  // ⚠️ **内容**也要还原（见上）
                _daily[i].Target = _dailyFactory[i].Target;
                _daily[i].RewardArt = _dailyFactory[i].RewardArt;
                _daily[i].RewardText = _dailyFactory[i].RewardText;
            }
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

        // ============================================================ 领奖窗（= 原版 `RewardService.Collect` 的开窗那一半）
        //
        // 🆕 **2026-10-11（批次1 · W1 · A309）**：「领到奖 ⇒ 弹一扇全屏 `Reward Window`」这件事，
        // 在我们这一侧**只挂在一个出口上** —— `Wallet.Grant` 那一条路（全工程唯一发奖口，
        // 6 个调用点全在本文件）。判据 = `d:/2/tools/decomp_full/RewardService__Collect.c`：
        // `Collect(..., showAnimation: true, onCollected, …)` 在**发完奖之后**
        // `new RewardWindowContext(rewards, isPremiumLocked, onCollect: **null**, onClose: onCollected,
        //  …, isPreview: **0**)` → `WindowsManager.OpenWindow<RewardWindow>` —— 即**「Rewards claimed」那一态**
        // （没有 `Collect` 按钮），关窗时把 `onCollected` 发出去（我们拿它重建还开着的日常两扇窗）。
        // ⚠️ **只接了两条**（每日奖励抽屉 · 连登奖格 —— 都走本文件那两个 `Collect*`）。
        // 原版同样走这个出口的还有：开包（`ContainerService.OpenContainer`）/ 锻造 / 战役节点 / **每日任务卡** /
        // **周常** / **骷髅** / **登录** / 试用卡升级 …。其中**发奖口也在本文件**的是后四条
        // （`CollectDaily` / `CollectWeekly` / `CollectSkulls` / `CollectLogin`，对应的原版调用点 =
        // `Missions.<CollectChallenge>g__OnCollectSuccess_1` 与 `PlayerDataManager.ProcessNewLoginResult`）
        // —— **接线本身与已做的两条逐字相同**，卡的是**自检夹具**：`Editor/RewardsScene.cs` 里有**六处**
        // 点在 `Collect` 钮上（`:1457` `:1538` `:1569` `:1597` `:1616` `:1626`）+ `:4361` 那条 `CollectDaily(0)`，
        // 接上之后每一下都会弹出一扇窗，而它们**正排在四张「只有壳」的截图之前** ⇒ 会**静默盖住**那几张
        // ⇒ 要同批给那几处补 `wm2.CloseAllWindows()`。**要做**（不是「不做」），见报告 §四·⑤。
        // 其余的（开包 / 锻造 / 战役节点 …）宿主窗不在本件白名单 ⇒ 留待那些窗口的批次。

        /// <summary>一条奖励（图名 + 数量）⇒ 弹 `Reward Window`。
        /// <para>🔴 **传进去的 `Id` 就是那张菜单图名** —— 日常线的奖励**没有服务端 item id**
        /// （我们的记账口签名是 `Wallet.Grant(string art, int n)`）。`RewardWindow.ArtOf` 为此补了一条兜底
        /// （id 认不出时再认「id 本身就是一张菜单图名」），见那边的注释。
        /// `Tier` 由**这一格的轨**决定：高级轨 = `TierPremium(10)`（= 原版 `RewardTier` 的取值）。</para></summary>
        static void ShowCollectedWindow(string art, int n, bool premium)
        {
            RewardWindow.ShowCollected(
                new[] { new CampaignData.RewardSpec(art, n, premium ? CampaignData.TierPremium : CampaignData.TierBasic) },
                RefreshOpenDailyWindows);
        }

        /// <summary>领奖窗关掉之后，把**还开着的**日常两扇窗重建一遍
        /// （= 原版 `onCollected` 回调 + liveops handler 那次 refresh：抽屉要从 `Unlocked` 变 `Collected`）。
        /// 两扇窗的内容都是**数据驱动 + 每次 `Build()` 现算**的（`RewardStateOf` / `StreakRewardClaimed`）
        /// ⇒ 重建即刷新。⚠️ 关掉的窗**跳过**（`WindowState.Closed` 的留在 `openWindows` 里是合法状态 ——
        /// `WindowsManager.HideAllWindows` 藏起来的就是这样，别去动它）。</summary>
        static void RefreshOpenDailyWindows(CampaignData.RewardSpec[] rewards)
        {
            var wm = WindowsManager.Instance;
            if (wm == null) return;
            for (int i = wm.openWindows.Count - 1; i >= 0; i--)
            {
                var w = wm.openWindows[i];
                if (w == null || w.CurrentState == WindowState.Closed) continue;
                var dw = w as DailyRewardPopup;
                if (dw != null) { dw.Build(); continue; }
                var sw = w as DailyStreakPopup;
                if (sw != null) { sw.Build(); continue; }
            }
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
