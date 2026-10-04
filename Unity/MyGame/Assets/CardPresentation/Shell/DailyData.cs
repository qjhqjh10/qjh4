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
        public static bool SkullsStepDone(int i) { return _skullsCount >= SkullsTarget * (i + 1) / 5; }

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
        // ⚠️ 别和卡上那个 `counter text`（`x160`）弄混：那是**进度**（`_skullsCount`），这是**这一格的奖励**。
        /// <summary>骷髅卡那一格要画的图 —— 与 `CollectSkulls` 发的是**同一份**（B4）。
        /// ⚠️ **图是我们挑的**（原版 prefab 那一格的 Image `m_Sprite = 0`；见上面「骷髅卡」那一段）。</summary>
        public static string SkullsRewardArt() { return "40K_missions_icon_Daily_skulls"; }
        /// <summary>骷髅卡那一格要画的数量 —— 与 `CollectSkulls` 发的是**同一份**（B4）。
        /// ⚠️ **200 是我们挑的**（原版走运行期 `RewardInfo.amount`）。</summary>
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
        /// <summary>自检用：骷髅卡的状态（`Collectable` 才领得到）。</summary>
        public static void ForceSkullsStateForTest(State st) { _skullsState = st; }
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
        /// <summary>骷髅卡那颗 `Collect` 可不可点（守卫原文见 `CollectSkulls`）。</summary>
        public static bool CanCollectSkulls() { return _skullsState == State.Collectable; }
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
        /// 现在：图 = 骷髅、数量 = **200** —— ⚠️ **这两个都是我们挑的**（原版那一格的 Image `m_Sprite = 0`、
        /// `count` 是运行期填的；依据 2026-10-05 被证伪 ⇒ 按铁律 3 降级，判据见上面「骷髅卡」那一段）。</summary>
        public static bool CollectSkulls()
        {
            if (!CanCollectSkulls()) { Say("每日骷髅还没达成，领不了"); return false; }
            _skullsState = State.Claimed;
            Wallet.Grant(SkullsRewardArt(), SkullsRewardCount());
            Say("每日骷髅已领取：" + SkullsRewardCount() + " 个（`" + SkullsRewardArt() + "`）");
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
