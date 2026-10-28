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
// 🆕 **2026-10-12（A372）例外**：**周常**那**六**档阈值（`5/10/15/20/25/30`）与**格数 6**
//    同样**照原版实拍**（原来是我们按 prefab 作者预览建的 4 格 / 终值 15）—— 见 `_weeklySteps` 一段。
//    ⚠️ 但**周常的当前进度**仍是我们挑的（`_weeklyProgress = 13`）：实拍那个 `25/30` 是玩家数据。
// 🆕 **2026-10-17（A2070③）例外**：**三条每日任务**的文案 / 目标 / 奖励（活动点 ×300）现在**照用户实拍**——
//    见 `_daily` 那一段。⚠️ 那是**一帧实拍**、不是原版数据表（原版任务内容由服务端下发）。
//
// ✅ **接线点（2026-10-11 · A375 接上）**：进度的真正来源 = 战斗结果 —— 落点 = `Battle/BattleDriver.cs`
//    **结算那一处**的 `DailyData.OnBattleEnd(…)`（原版也是「打完一局回来任务就动了」）。
//    ⚠️ 上面那句「这是**我们自建**的」仍然成立 —— 但**只管【任务内容 / 奖励表 / 数值】**；
//    **「一局给几个骷髅」不在其内**：那一条是**照原版**的（判据写在 `OnBattleEnd` 上面那一大段里）。
//    `Collect*` 只管「领了 → 变已领取」。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>单机版的「日常」状态。**可复现**（不用 `UnityEngine.Random`；要随机就用定种子的 `System.Random`）。
    /// <para>🔴 **2026-10-13（A429）就地订正（铁律 5）**：这里原来写「**纯内存** + 可复现」——
    /// **「纯内存」已经不对了**：**当日那一族会落盘**（用户 2026-10-13 裁定「落盘（独立文件）」）。
    /// 「纯内存」只对**其余**成立（周常 · 连登 · 每日奖励抽屉 · 商店 —— 各有各的周期，都不落盘）。
    /// 落盘的形状 / 为什么现在敢落 / 自检怎么隔离 → 「每日重置（A384）」那一节末尾的
    /// 「每日状态的**落盘**」一段（⛔ 别只看这一句）。</para></summary>
    public static class DailyData
    {
        // ---- 原版三态（照原版语义；`Collectable` 那一态换了底色，见 `RowTint`）----
        public enum State { InProgress = 0, Collectable = 1, Claimed = 2 }

        public sealed class Task
        {
            public string Desc;          // 英文原文/兜底文案（`Term` 为空时直接用它）
            /// <summary>🆕 **2026-10-18（第五十一会话）**：官方词条键（含 `{0}` 的那一族）。
            /// 有它 ⇒ 显示文案 = `Loc.T(Term)` 按 `Arg` 填参 + `CardIcons.RewriteMarks`（`[[ranged]]` 这类记号）。
            /// 值逐字来自官方表（见 `Loc.cs` 里那几条 `Missions/Daily_*`）。</summary>
            public string Term;
            public int Arg;              // 填进 `{0}` 的那个数（进度目标）
            public int Progress, Target; // 照原版：显示成 `{Progress}/{Target}`
            public string RewardArt, RewardText;
            public State St;
            public bool Step1
            { get { return St != State.InProgress; } }
        }

        // 🔴🆕 **2026-10-17（A2070③）：这三条的【文案 / 目标 / 奖励图 / 数量】照用户实拍一帧改。**
        //   参考图（**桌面，不在仓库**）：`C:/Users/qjh36/Desktop/warpforge参考图/奖励—布道所（每日任务）参考图.png`
        //   —— 右栏「每日布道所」三条：**对敌方战将造成 45 伤害（0/45，300）· 攻击单位 15 次（0/15，300）·
        //   远程攻击 15 次（0/15，300）**，三条的奖励格都是**同一颗「活动点」图 + `300`**。
        //   ⚠️ **来源标注（铁律 2/3）**：这是**用户实拍一帧**，**不是**原版数据表 —— 原版任务内容由服务端下发
        //   （见本文件头），⛔ **别把这句读成「原版就是这三条」**。进度一律 `0`（实拍那一帧就是 `0/45`）。
        //   ⚠️ **推进逻辑仍是旧的三个口径**（`OnBattleEnd` 的 `damageToEnemy / troopsPlayed / 胜`）——
        //   与「远程攻击次数」这一条**对不上**（引擎边界上没有那个计数），如实记在报告里、⛔ 不在这里编。
        static readonly Task[] _daily =
        {
            new Task { Desc = "Deal 45 damage to enemy warlords", Progress = 0, Target = 45,
                       Term = "Missions/Daily_5",         Arg = 45,
                       RewardArt = "40K_genearl_icon_Campaign_points_big", RewardText = "300" },
            new Task { Desc = "Attack with units 15 times",       Progress = 0, Target = 15,
                       Term = "Missions/Daily_Generic_8", Arg = 15,
                       RewardArt = "40K_genearl_icon_Campaign_points_big", RewardText = "300" },
            new Task { Desc = "Attack with ranged units 15 times", Progress = 0, Target = 15,
                       Term = "Missions/Daily_10",        Arg = 15,
                       RewardArt = "40K_genearl_icon_Campaign_points_big", RewardText = "300" },
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
                                     RewardArt = src[i].RewardArt, RewardText = src[i].RewardText, St = src[i].St,
                                     // 🔴 **2026-10-18（Rewards 四条红 #4）**：`Term`/`Arg` 也要拷 ——
                                     //   原来漏了这两个，`ResetMissionsForTest` 还原内容时**没还原词条键**
                                     //   ⇒ 被重摇过的那一行会「文案回到出厂、词条仍指旧任务」。
                                     Term = src[i].Term, Arg = src[i].Arg };
            return copy;
        }

        // ---- 周常：**六档**里程碑的**阈值** ----
        // 🔴 **2026-10-12（A372）就地订正（铁律 5）**：这里原来是
        //    `const int WeeklyTarget = 15;` + `static readonly bool[] _weeklySteps = { true, true, false, false };`
        //    —— **4 格、手写布尔、终值 15**；格数当场取自**独立 prefab 的作者预览**，那是**出厂占位**。
        // ✅ **现在照原版实拍**：判据 = **用户提供的原版实拍**
        //    （`C:\Users\qjh36\Desktop\奖励—布道所（每日任务）参考图.png` 的「每周挑战」一行）
        //    —— 实拍上是 **6 个宝箱、下方逐格印着** `5` `10` `15` `20` `25` `30`，
        //    右上那颗计数写着 **`25/30`** ⇒ **终值 = 30**（不是 15）。
        // 🔴 **根因与骷髅卡同源**：`d:/2/tools/decomp_full/MissionMilestonesDisplay__Setup.c:28-31` 头两句
        //    就是 `SupportMethods__DestroyAllChildren(transform)`，随后按 `AvailableRewards()`
        //    **一条一格 `Instantiate`**（`.c:37-77`，每格传 `*(里程碑 + 0x14)` = 它自己的阈值）
        //    ⇒ **格数是数据驱动的**；prefab 里那 4 格只是**作者预览**
        //    （`AF/bundle_menus_assets_all/GameObject/Weekly Mission Container.json` 里那 4 个同名实例
        //      `Weekly Mission Milestones Step (3)` 的 x = 245.30 / 578.11 / 910.92 / 1243.73
        //      = 间距 332.81 = 70 + **262.81**；而 `4×70 + 3×262.81 = 1068.43` **恰好 = 容器宽**
        //      ⇒ 262.81 是**按 4 格填满容器**配的占位值 —— 见 `MissionsTab.BuildWeekly` 的间距那一段）。
        // ⚠️ 实拍上那 6 个数**印在宝箱的下方**（= 原版 `…/holder/text` 那个 TMP 的运行时值，
        //    出厂占位串是 `'5'` —— 判据见 `WeeklyStepTarget` 的注释）。
        static readonly int[] _weeklySteps = { 5, 10, 15, 20, 25, 30 };

        /// <summary>周常里程碑的**格数**（= 实拍上那 6 个宝箱；A372 之前是 4）。
        /// ⚠️ 与 `_weeklySteps.Length` **同一条口径**（格数 = 阈值数组长度），下面有断言钉住。</summary>
        public const int WeeklyStepCount = 6;

        /// <summary>周常的**总目标** = **最后一档**（实拍计数写 `25/30`；A372 之前是 `15`）。
        /// 🔴 **这个数只此一处**：`WeeklyCounter` / `WeeklyProgress01` 与进度条都从它取
        /// （⛔ 别再写第二个 `30`）。</summary>
        const int WeeklyTarget = 30;
        /// 🔶 自己设计（非原版）：周常 mock 进度 13 · 实拍那个 25/30 是服务端下发的玩家进度，不是写死值 · TODO(谁定的)｜原注：<summary>⚠️ **我们挑的** mock 进度。实拍那张图的 `25/30` 是**那个玩家当时的进度**（服务端下发），
        /// 不是原版写死的值 ⇒ 我们照旧填一个我们挑的数（保留 A372 之前的 `13`：
        /// 在 `5/10/15/20/25/30` 这套阈值下它 **亮第 1、2 档**，其余四档灭 —— 视觉上与原版那张图同构）。
        /// 自检要造别的态走 `ForceWeeklyProgressForTest`（**测试口，不在出厂路径上**）。</summary>
        static int _weeklyProgress = 13;
        static State _weeklyState = State.InProgress;

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

        public static string DailyDesc(int i)
        {
            var t = At(i);
            // 🆕 **2026-10-18（第五十一会话 · 修「中文档里日常文案是英文」）**：
            //   有官方键 ⇒ `Loc.T(键)` 按 `Arg` 填 `{0}`，再把 `[[ranged]]` 这类记号换掉
            //   （`CardIcons.RewriteMarks` = 卡面同一条渲染路，⛔ 别另写替换）。
            if (!string.IsNullOrEmpty(t.Term))
            {
                string s = Loc.T(t.Term);
                if (!string.IsNullOrEmpty(s) && s != t.Term)
                {
                    s = s.Replace("{0}", t.Arg.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    // 🔴 **2026-10-18**：官方那两条带 `[[ranged]]` / `[[melee]]` 记号（原版卡面渲染成图标）。
                    //   任务行不是卡面、图标资源不保证在 ⇒ 先换成**当前语言的词**（`Card_Trait/*` 那一族），
                    //   剩下的记号再交给 `RewriteMarks`（它认不出的会原样留下 = 「ranged」那种半英文）。
                    s = s.Replace("[[ranged]]", CardText.Keyword("ranged"))
                         .Replace("[[melee]]", CardText.Keyword("melee"));
                    return CardIcons.RewriteMarks(s, CardText.Zh);
                }
            }
            return t.Desc;
        }
        public static string DailyCounter(int i) { var t = At(i); return t.Progress + "/" + t.Target; }
        public static float DailyProgress01(int i) { var t = At(i); return t.Target <= 0 ? 0f : Mathf.Clamp01(t.Progress / (float)t.Target); }
        /// <summary>`timer` 那一格（原版是 `MissionTimerDisplay` · 文案由 `Missions/CollectCounter` /
        /// `Missions/CompleteCounter` 词条在运行期拼）。🆕 **2026-10-17（W_A_TimerDisplay）：真倒计时** ——
        /// 目标 = 我们自建的**日界** `NextResetAt`（本机时间；我们没有服务器 —— 本项目允许的偏离）。
        /// ⚠️ 原版这一格的目标是**每条挑战自己的 refill/end 时刻**（`MissionTimerDisplay__Update.c`），
        /// 我们的三条任务数据里没有这个字段 ⇒ 用「我们的日界」代替（**我们挑的**，已在报告记账）。
        /// 前缀保留 prefab 出厂串 `Available in `（原版运行期按词条本地化，本地 `Loc` 没有那两条键）。</summary>
        public static string DailyTimer(int i)
        { return TimerDisplay.TextFor(NextResetAt, Loc.T("Missions/CollectCounter"), "每日任务行 Timer", null); }

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
        /// `I2_Loc_LocalizationManager.GetTranslation`（`.c:74`）。
        /// ⚠️ **2026-10-18 更正（铁律 5）**：这一段原来写「**本地没有语言表**（远端 CCD；全仓多次实测）
        /// ⇒ 照本仓先例**照抄 prefab 里那个串本身**，并在第一次用到时 `Debug.Log` 出声」
        /// —— **已过期，而且那不是「办不到」、是我们的缺陷**：
        ///   · 原版**原生**的英文列确实拿不到（远端 CCD）—— 这一半**仍然成立**；
        ///   · 但**键 `Missions/Completed` 早就在我们自己那份表里**（`Core/Loc.cs` 的
        ///     `("已完成", "Completed")`，ZH/EN 两列都自拟、写在 `⑦ 任务页「进度到顶」那一行` 那一节）
        ///     ⇒ 一行 `Loc.T` 就能把屏上那串从**键名**翻成**人话**。
        ///   · **错因 = 显示点没接表**：本方法当时**直接把键名当文案 `return`**，而屏上那条路
        ///     （`MissionsTab` → `MenuWindowBase.Text`）**从头到尾一次表都没查**（那三处 `grep 'Loc\.'` 零命中）
        ///     ⇒ 中文档印的是 `Missions/Completed`（**不是**「已完成」）、英文档也是键名。
        ///   ⇒ 现在**返回 `Loc.T(键)`**（见下面那一行）。⚠️ 键缺了也不静默：`Loc.T` 回**键名本身**，
        ///     屏上会明明白白印出 `Missions/Completed`（而不是悄悄换别的串）。
        /// ⚠️ 本仓那条**先例本身没被推翻**（`MissionRerollPopup` 的出厂原文 `Discard this mission…`、
        ///   登录卡的 `Ruta Gratuita` ⇒ **照抄 prefab 里那个串本身**、**不自己编一句英文冒充原版**，铁律 3）——
        ///   错的只是**把它套在一条「我们自己有文案的 I2 键」上**。判据：**先查 `Loc.HasEntry`，
        ///   是键就接表；不是键（prefab 里写死的文案）才照抄**。
        /// ⚠️ **判据只此一处**：全工程只有这一个方法决定「到顶换不换文案」，`MissionsTab` 只负责画。
        /// ⚠️ 到顶 = **纯数值比较 `Progress >= Target`**（照原版的 `cur >= max`），**不是** `St == Collectable`：
        /// 原版那一句从不碰领取域；两者在我们的数据模型里通常同真，但**判据照原版**。
        /// ⚠️ 「已领取」那一行**整件不建**（`displayRule = 1` ⇒ `!IsComplete` 才 `SetActive(true)`）⇒
        /// 这里不必管它（`MissionsTab` 只在 `!claimed` 那一支里调本方法）。</summary>
        public static string DailyCounterText(int i)
        {
            // 🆕 **2026-10-28（`B93_E10` · 账 `:471`）**：本体改成**转调原版同名件**
            //   <see cref="MissionCounterDisplay.Text"/>（= `MissionCounterDisplay.Setup` 里那两段：
            //   `currentValue >= MaxValue &amp;&amp; displayCompletedMessage` → `completedMessage` 词条；
            //   否则 `string.Format(progressTextFormat, currentValue, MaxValue)`）。
            //   🔴 **格式串现在是参数**（原版**每实例一个**）：每日行实例 = `"{0}/{1}"`（MB `3476392019656054992`）
            //   ⇒ 用 `MissionCounterDisplay.FormatDaily`，⛔ 别再在这里手写拼接。
            var p = ProgressView(i);
            if (p.CurrentValue >= p.MaxValue) SayCompletedKeyOnce();
            return MissionCounterDisplay.Text(p, MissionCounterDisplay.FormatDaily, true, CompletedMessage);
        }

        /// <summary>原版 `MissionCounterDisplay.completedMessage` 的**出厂原文**
        /// （真包 MB `3476392019656054992` 实读）。
        /// 🔴 **它是原版的 I2 词条【键】，不是给人看的文案** —— 别把它当成原版的英文。</summary>
        public const string CompletedMessage = "Missions/Completed";

        /// <summary>「到顶那一行显示的是原版那条 `completedMessage` 词条」这件事**只说一次**
        /// （每次重建都吼会淹掉日志，而这条信息只需要传递一次）。
        /// ⚠️ **2026-10-18 更正（铁律 5）**：原来这里写「**我们正在显示一个词条键**……
        /// 这一处的文案是原版词条键、不是我们编的」—— **已过期**：接上表之后屏上印的是**词条的文案**
        /// （中文档「已完成」/ 英文档 `Completed`），不再印键名。</summary>
        static bool _saidCompletedKey;
        static void SayCompletedKeyOnce()
        {
            if (_saidCompletedKey) return;
            _saidCompletedKey = true;
            Debug.Log("[Daily] `progress` **到顶**（`Progress >= Target`）⇒ 照原版显示 `completedMessage` 那条词条；"
                      + "它存的是 **I2 词条【键】** `" + CompletedMessage + "`，原版在这一句外面套 "
                      + "`I2_Loc_LocalizationManager.GetTranslation` —— 我们走 `Loc.T` 接**自己那份表**"
                      + "（ZH「已完成」/ EN `Completed`；两列都是**我们自拟**，原版那两列在远端 CCD、本地拿不到）。"
                      + "判据：MB `3476392019656054992` `displayCompletedMessage=1`"
                      + " + `MissionCounterDisplay__Setup.c:24-26,68-79`。");
        }

        /// <summary>这条任务**原版 `IsComplete()`** 那一态 = **奖励已领取 / 已结算** —— **不是**「进度到顶」。
        /// 判据（2026-10-04 两条**互相独立**的证据链，结论一致）：
        /// ① `MissionChallengeProgress__IsComplete.c:18-26` 的整条算式**只读领取域**：
        ///    `Count(AvailableRewards()) &lt;= *(int*)(this + 0x10)`，而 `+0x10` = 基类
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
        /// （`show = (WhenComplete &amp;&amp; IsComplete) || (WhenActive &amp;&amp; !IsComplete)`，见 `BuildDailyRow` 那段注释）。
        /// ⚠️ **我们原来恒画它**（`Resets in 12h 34 m`）—— 与每日行 `description`/`timer` 互斥是**同一条规则**。</summary>
        public static bool LoginClaimed() { return _loginState == State.Claimed; }

        /// <summary>骷髅卡是否**已领取**（谓词同 `DailyClaimed`）。
        /// ⚠️ 它**不管任何件的显隐** —— 三张单例卡的 `infoDisplays` 里**根本没有 `Collect` 那一件**、
        /// `dr` 几乎全是 `-1`（`[Flags]` 全位置 1 ⇒ 恒真），那三颗 `Collect` 的显隐走
        /// `interactable = CanCollect()`。**别把 A44 甲「同吃 `displayRule`」那条口径推广到这里**。</summary>
        public static bool SkullsClaimed() { return _skullsState == State.Claimed; }

        /// <summary>周常是否**已领取**（同上，只管状态、不管显隐）。</summary>
        public static bool WeeklyClaimed() { return _weeklyState == State.Claimed; }

        /// <summary>整卡底色。**照原版两套值**：普通/已领取 = `normalColor`，可领取 = `collectableColor`。
        /// 🆕 **2026-10-28（`B93_E10` · 账 `:438`）**：本体改成**转调原版同名件**
        /// <see cref="MissionBackgroundHighlighter.ColorFor"/>（= `MissionBackgroundHighlighter.Setup` 里那
        /// 一句三元），颜色常量从此**只有一处**（那个类里，prefab 全精度值）。
        /// ⚠️ **方法名保留**：30+ 处引用/断言在读 `RowTint`（⛔ 别删）；它现在只是**入口**。</summary>
        public static Color RowTint(int i) { return MissionBackgroundHighlighter.ColorFor(ProgressView(i)); }

        public static string RefillText()
        {
            // 🔴 **2026-10-18（第五十一会话 · 对参考图）**：这里是「**N 可用**」= 当前还能做、还没领的日常条数
            //   （参考图 `奖励—布道所（每日任务）参考图.png`：三条都没领 ⇒ 印 **3 可用**）。原来写死 `0`。
            int n = RefillCountValue();          // 🆕 2026-10-26（B9 · P9 #530）：计数收成一处，页头显隐读同一个口
            return Loc.T("Missions/RefillCount").Replace("{0}", n.ToString());
        }   // 原版占位串是 `0 Disponible`（葡语残留）；有官方键 `Missions/RefillCount` ⇒ 按词条印

        /// <summary>登录卡卡头标题。🆕 **2026-10-28（`B93_E10` · 账 `:492`）**：改成转调原版同名件
        /// <see cref="MissionHeaderDisplay.TitleText"/>（= `MissionHeaderDisplay.Setup` 里
        /// `title.text = LiveOpsAssetUtility.GetLabel(mission, EventLabelReferenceType.Title)`）。
        /// ⚠️ **原版这一支的 `tooltip.Setup(GetLabel(…Description))` 我们没有落点** ——
        /// 我们卡头右边那颗 ⓘ 是**另一件**（`AddInfoTooltip`），理由写在那颗类里。</summary>
        public static string LoginTitle() { return MissionHeaderDisplay.TitleText("Missions/Title/LoginBonus"); }
        /// <summary>骷髅卡卡头标题（同 `LoginTitle`，账 `:492`）。</summary>
        public static string SkullsTitle() { return MissionHeaderDisplay.TitleText("Missions/Title/DailySkulls"); }
        /// <summary>骷髅卡 `progress` 那一格的文本。🆕 **2026-10-28（`B93_E10` · 账 `:471`）**：改成转调
        /// <see cref="MissionCounterDisplay.Text"/>，格式串 = **骷髅卡实例自己那一份** `"x{0}"` +
        /// `displayCompletedMessage = false`（MB `-3349471971615803659` 实读）⇒ **到顶也不换词条**
        /// （这一条以前只是「行为碰巧对」，现在是**同一个判据的两个参数**）。</summary>
        public static string SkullsCounter()
        { return MissionCounterDisplay.Text(SkullsProgressView(), MissionCounterDisplay.FormatSkulls, false, CompletedMessage); }

        /// <summary>🆕 2026-10-03：每日骷髅任务那条 `counter/icons/Army` 格子要挂的**阵营**
        /// （`null` = **Neutral**）。判据 = `d:/2/tools/decomp_full/MissionCounterDisplay__Setup.c:51-63`：
        /// 图 = `ArmyUtilities.GetArmyIcon(challenge.Army)`，**`army == Neutral(0)` 时那一格整格 `SetActive(false)`**；
        /// `Army` 来自挑战字段（`GamesPlayed.cs:14-25` / `SkullsCount.cs:12-21`）。
        /// 🔴 **原版当天那条任务挂哪个阵营本地查不到**（daily 没有资产、服务端下发；
        /// `grep -rl anyArmy assets_full` 只命中静态成就）⇒ **我们这份 mock 恒 `null` = Neutral**，
        /// 也就是**那一格不建、也不留 60px**（`项目任务.md` §三 第 29 条 B1）。
        /// ✅ **2026-10-12（A377）这一条的【观感】已核对：`null` 与原版一致 —— 不再是「我们挑的」**。
        /// 判据有两份，**互相独立**：① 上引那条反编译分支（`army == Neutral(0)` ⇒ 整格 `SetActive(false)`）；
        /// ② **用户提供的原版实拍**（`C:\Users\qjh36\Desktop\奖励—布道所（每日任务）参考图.png`
        /// 左栏「每日骷髅头」卡）：`counter` 那一条上**只有一个骷髅图标 + `x0`，没有阵营徽记、左边也没有空槽**
        /// ⇒ 实拍落的就是 `Neutral` 那一支。
        /// ⚠️ **「一致」指的是「我们这条 mock 的观感 = 原版 Neutral 那一支」**，⛔ **不是**说
        /// 「原版那天那条任务真的没阵营」—— 那一条仍然**查不到**（服务端数据）；将来若给 mock 补上阵营，
        /// 这里返回它即可 —— 画法在 `MissionsTab` 里已经写好了。</summary>
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
        /// ⚠️ **只管「方框亮不亮」，不管「能不能领」** —— 可点性见 `CanCollectSkulls`。
        /// 🆕 **2026-10-28（`B93_E10` · 账 `:497`）**：那个 `>=` 现在**只有一处** —— 转调原版同名件
        /// <see cref="MissionMilestoneStep.Reached"/>（= `MissionMilestoneStep__Setup.c` 里那个
        /// `value &lt;= currentValue`，也 = `MissionChallengeProgress.&lt;CurrentMilestone&gt;b__15_0` 的取反）。</summary>
        public static bool SkullsStepDone(int i) { return MissionMilestoneStep.Reached(SkullsStepTarget(i), _skullsCount); }

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
        //    ⇒ 它是个**通用货币**（用户 2026-09-17 拍的边界「不做真实经济、资源固定 2000」
        //      —— 固定值 2026-10-24 由 9999 改为 2000 ⇒
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
        //       （已在 `Resources/Art/ui_menu/`，`Shell/CampaignTab.cs` 里用 `40K_genearl_icon_Campaign_points_big` 那一处 也在用同一张）。
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
        // ⚠️ **不扣任何东西**（用户 2026-09-17 拍板「不做真实经济、资源固定 2000」；
        //    固定值 2026-10-24 由 9999 改为 2000）——
        //    弹窗里那个价钱格照原版外观建、`300,00` 是 prefab 出厂占位（**不是真价钱**，见 `MissionRerollPopup`）。

        /// <summary>重摇用的种子。**定死** ⇒ 同一串调用每次得到同一条（自检可复现）。</summary>
        public const int RerollSeed = 20261004;

        /// 🔶 自己设计（非原版）：顶替任务池的文案与数值 · 原版任务内容由服务端下发 · TODO(谁定的)｜原注：<summary>可用来顶替的任务池（**文案/数值都是我们挑的** —— 与原版一样，任务内容本来就由服务端下发）。
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
        /// 而 `_skullsCount` 是文件私有、出厂恒 **`0`**（⚠️ **2026-10-13 更正**：这里原来写「出厂恒 `160`」——
        /// 那是 A375 之前的出厂 mock 值；错因 = 2026-10-11 那次订正只改了 `_skullsCount` 字段那一处、**漏了本处**，
        /// 即本文件同一个错数被抄过三处，本处是第三处）⇒ 不注入就只能断到一个状态
        /// （本仓那条「**弱断言分不出两种状态**」的坑就是这么踩的）。</summary>
        public static void ForceSkullsCountForTest(int n) { _skullsCount = Mathf.Max(0, n); }

        /// <summary>🆕 **2026-10-11（A370）自检用**：现在这个计数（自检要**还原**它 ——
        /// 「另一条研究正在跑怎么打出骷髅」，那个值不许被本节改完之后留在工作区）。</summary>
        public static int SkullsCountValue() { return _skullsCount; }
        /// <summary>自检用：周常的状态（`Collectable` 才领得到）。</summary>
        public static void ForceWeeklyStateForTest(State st) { _weeklyState = st; }

        /// <summary>🆕 **2026-10-12（A372）自检用**：把周常进度**定死**（六档亮不亮全由它算）。
        /// 🔴 **为什么必须有个口**：六档阈值是**两态**判据（`4` 未达成 / `5` 达成），
        /// 而 `_weeklyProgress` 是文件私有、出厂是我们挑的 `13` ⇒ 不注入就只能断到一个状态
        /// （本仓那条「**弱断言分不出两种状态**」的坑就是这么踩的；同 `ForceSkullsCountForTest`）。</summary>
        public static void ForceWeeklyProgressForTest(int n) { _weeklyProgress = Mathf.Max(0, n); }

        /// <summary>🆕 **2026-10-12（A372）自检用**：现在这个进度（自检要**还原**它）。</summary>
        public static int WeeklyProgressValue() { return _weeklyProgress; }

        /// <summary>🆕 **2026-10-12（A316）自检用**：把连登第 `i` 格的「本局领过」那一位定死
        /// （`StreakRewardUnlocked` 那条守卫读的就是它）。
        /// 🔴 **为什么必须有个口**：`_streakClaimed[]` 是文件私有、出厂全 `false`，
        /// 而 A316 那两条是**两态**判据（没领过 ⇒ 领得到 / 领过 ⇒ 领不到）—— 不注入就只能断到一个状态
        /// （本仓那条「**弱断言分不出两种状态**」的坑就是这么踩的；同 `ForceSkullsCountForTest`）。
        /// ⚠️ 它是**静态**的 ⇒ 自检跑完**必须还原**，否则后面每一节都吃这个残留。</summary>
        public static void ForceStreakClaimedForTest(int i, bool claimed) { _streakClaimed[SI(i)] = claimed; }
        /// <summary>🆕 **2026-10-12（A316）自检用**：可领的那一格是第几格（= `StreakCollected` 的读口，
        /// 自检拿它去点 / 去还原，⛔ 别在自检里再抄一个 `5`）。</summary>
        public static int StreakClaimableDay { get { return StreakCollected; } }

        /// <summary>🆕 **2026-10-13（A498 · 调度台裁定）自检用**：把**每日奖励抽屉**第 `day` 格的
        /// 「本局领过」那一位定死（`RewardStateOf(day, …)` 读的就是它）。
        /// 🔴 **为什么必须有个口**：`_rewardClaimed[]` 是文件私有、出厂全 `false`，而全自检**只有一格**
        /// 进得了 `Unlocked`（`RewardsCollected = 2` · `RewardCurrentValue = 3` · `_rewardTarget = {1,2,3,4}`
        /// ⇒ 只有下标 2），而 §九(a) 那条既有覆盖**真点过一次抽屉、把它收掉了**
        /// ⇒ 没有写口就**再也造不出「可领」那一态**：A479 的**正例**（关窗 ⇒ 真收到了）因此落不进来
        /// （H42 §三 逐条记过那两条落法都会撞红）。
        /// ⚠️ 与**连登轨**那个 `ForceStreakClaimedForTest` 是**两条轨**（两个数组），⛔ 别互相顶替。
        /// ⚠️ 它是**静态**的 ⇒ 自检跑完**必须还原**，否则后面每一节都吃这个残留。
        /// <para>🔴 **H38 §⑭ 那句「本件不为奖励抽屉加任何读口/写口」在本裁定下【作废】**，理由写在这里：
        /// 它当年成立的前提是「那一格一旦被收就回不去 ⇒ 正例没有可领的格可收」——
        /// 而调度台 2026-10-13 裁定「**加写口**」（同族先例 = `ForceSkullsCountForTest` /
        /// `ForceWeeklyProgressForTest` / `ForceStreakClaimedForTest`，都是「**两态判据不许只断得到一态**」）。
        /// 按「两处写同一条规则 = 迟早不一致」，本口与 `RewardStateOf` 共用**同一份** `_rewardClaimed[]`，
        /// ⛔ 不另立一份状态。</para>
        /// <para>**改坏法**：把这个口删掉（或让它写别的数组）⇒ A496 的 ①–⑥ 与 ⑦ 的前提当场红
        /// （那一格回不到 `Unlocked`）。</para></summary>
        public static void ForceRewardClaimedForTest(int day, bool claimed) { _rewardClaimed[RI(day)] = claimed; }

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
            // 🔴 **2026-10-18（Rewards 四条红 #4 · 真缺陷）**：`Term`/`Arg` **必须跟着换**。
            //   原来只拷上面四个字段 ⇒ `DailyDesc` 见 `Term` 非空就仍走**旧任务的官方键 + 旧 Arg**
            //   （实测：目标已变 0/300、描述还印「对敌方战将造成 45 伤害」）。
            //   池条目目前没有 Term（文案是我们自拟的）⇒ 拷过来就是 `null` / 0，`DailyDesc` 自然回 `Desc`。
            t.Term = src.Term;
            t.Arg = src.Arg;
            t.St = State.InProgress;                       // 新任务从「未完成」起
            RerollCount++;
            Say("第 " + (i + 1) + " 条任务已**重摇**：「" + oldDesc + "」⇒「" + t.Desc + "」"
                + "（⚠️ **不扣任何资源** —— 用户 2026-09-17 拍板「不做真实经济」；"
                + "原版这一步走服务端 `RerollChallenge` 并扣 `RerollPrice`）");
            // 🆕 **A429**：这一行换了内容（`Desc`/`Target`/`RewardArt`/`RewardText` 都动了）+ 进度归零
            // ⇒ 落一次盘（**连内容一起存**，见「每日状态的落盘」那段里 `RowDto.rerolled` 的理由：
            //    只存进度的话，重启后这一行会**文案退回出厂、进度却是重摇后的**）。
            Persist("重摇第 " + (i + 1) + " 条");
            return t.Desc;
        }

        // ============================================================ 周常

        public static string WeeklyCounter() { return _weeklyProgress + "/" + WeeklyTarget; }
        public static float WeeklyProgress01() { return Mathf.Clamp01(_weeklyProgress / (float)WeeklyTarget); }

        /// <summary>第 `i` 档（0 基）的**阈值** = `5 / 10 / 15 / 20 / 25 / 30`。
        /// 判据 = **用户提供的原版实拍**（「每周挑战」一行那 6 个宝箱下面逐格印着的数）。
        /// <para>⚠️ **它也是「格子里那个数」的唯一数据源** —— 原版每格挂一个 `WeeklyMissionMilestone`，
        /// 其 `holder/text`（TMP，出厂占位串 `'5'`、fs **50**、auto[10~50]、白）由
        /// `text.text = 阈值.ToString()` 填成**阈值本身**：`DF:MissionMilestoneStep__Setup.c` 头两句 =
        /// `System_Int32__ToString(第 1 个 int 形参)` → `TMP_Text.set_text`（`+0x558`），而调用点
        /// `DF:MissionMilestonesDisplay__Setup.c:76` 传的正是 `*(里程碑 + 0x14)` = **那条里程碑自己的阈值**
        /// （第 2 个 int 形参 `*(challenge + 0x40)` 才是 `currentValue`）。
        /// `WeeklyMissionMilestone__Setup.c` 第一句就转调 `MissionMilestoneStep__Setup()` ⇒ **同一条链**。
        /// 实读（`工具/menu_dump.py bundle_menus_assets_all "Weekly Mission Milestones Step (3)"`）：
        /// `holder/text` 框 **142.95×56**（比 70² 的格子宽、**且向下伸出格子 29.53**）、占位 `'5'`、fs 50。</para></summary>
        public static int WeeklyStepTarget(int i)
        { return _weeklySteps[Mathf.Clamp(i, 0, _weeklySteps.Length - 1)]; }

        /// <summary>第 `i` 档达成了没有（`_weeklyProgress &gt;= 阈值`）。
        /// 🔴 **2026-10-12（A372）改了口径**：这里原来读一个**手写的 `bool[]`** —— 与 `_weeklyProgress`
        /// **两处各说各话**（格亮不亮和计数 `13/15` 可以互相矛盾，加档还要改两处）。
        /// 现在与骷髅卡（`SkullsStepDone`）**同一条口径**：**唯一数据源 = 阈值数组**，亮不亮由计数算出来。</summary>
        /// <summary>周常第 `i` 档达成了没有。
        /// 🆕 **2026-10-28（`B93_E10` · 账 `:460` / `:497`）**：那个 `>=` 改成转调
        /// <see cref="MissionMilestoneStep.Reached"/>（它内部用
        /// <see cref="MissionChallengeProgress.CurrentMilestonePredicate"/> 的取反 =
        /// `MissionChallengeProgress.&lt;CurrentMilestone&gt;b__15_0` 那个闭包）⇒ **判定只有一处**。</summary>
        public static bool WeeklyStepDone(int i) { return MissionMilestoneStep.Reached(WeeklyStepTarget(i), _weeklyProgress); }
        /// <summary>`Weekly Mission / TimerHolder / Timer`（prefab 占位 `Ends in 12h 34 m`）。
        /// 🆕 **2026-10-17（W_A_TimerDisplay）**：目标时刻 = **未知** —— 我们的数据模型里**没有周常刷新时刻**
        /// （`TryDailyReset` 明确「只清当日那一族 ⇒ 周常不在内」，全仓也没有 `weeklyReset` 之类的字段；
        /// 原版由服务端下发）⇒ 按用户口径**不编日期**：出声一次 + 稳定显示 `Ends in --`。**2026-10-29（`B94_B4`）就地订正**：描述串原来写死 `Missions/CollectCounter`，而原版 `MissionTimerDisplay.Update` 按 `IsComplete` 分档（已领 ⇒ `Missions/CompleteCounter`）⇒ 周常领完后仍印「Ready to collect in」，另外三张卡却已分档（同一条规则两处口径）⇒ 改走 `MissionTimerDisplay.Description(false, WeeklyClaimed())`。</summary>
        public static string WeeklyEndsIn()
        {
            return TimerDisplay.TextFor(null, MissionTimerDisplay.Description(false, WeeklyClaimed()), "周常 Timer",
                                        "周常刷新时刻在本地数据模型里不存在（原版由服务端下发；`TryDailyReset` 不碰周常）");
        }
        /// <summary>`Timer`（`Resets in 12h 34 m` 那一族：每日行 / 登录卡 / 骷髅卡 / 每日奖励窗）。
        /// 🆕 **2026-10-17（W_A_TimerDisplay）**：真倒计时，目标 = 自建日界 `NextResetAt`（本机时间，无服务器）。</summary>
        public static string ResetIn()
        { return TimerDisplay.TextFor(NextResetAt, Loc.T("Missions/CollectCounter"), "日常 Timer", null); }

        // ============================================================ 每日奖励（`Daily Reward Popup`）
        //
        // 结构**照原版**：一条轨上 4 个 `Entry`（一天一格），每格两个抽屉（`NormalReward` / `Premium Reward`），
        // 每个抽屉**各自**持一个四态 `RewardState`。状态怎么算照原版
        // （`DF:DailyRewardItemContainer__GetCurrentState.c:18-46`，见 `资料/日常_调用链_三窗.md` §一·D；⚠️ 更正：原来指 `资料/日常_调用链_DailyRewardPopup.md`，2026-10-10 已并入）。
        // 🔶 自己设计（非原版）：每日奖励的天数 / 奖励内容与数量 / 里程碑目标值 · 原版由服务端下发 · TODO(谁定的)｜原注：❌ **我们挑的**：天数、奖励内容与数量、里程碑目标值、有没有买 Premium。

        public const int RewardDays = 4;
        /// <summary>⚠️ 我们挑的：已领到第几天（`index &lt; 它` ⇒ 该格 `Collected`）。</summary>
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
        /// 🔶 自己设计（非原版）：本地模拟「买 Premium 轨」（`_premiumOwned` 可写） · 原版走真商店 `PremiumOffer.TryPurchaseOffer`、服务器已关服 ⇒ 判据在远端 · 调度台 2026-10-28 裁定｜原注：⚠️ 我们挑的：**单机不卖 Premium** ⇒ 出厂 `false`、Premium 抽屉恒 `PremiumLocked`；买过了才转可领（见 `PurchasePremiumLocal`）。
        static bool _premiumOwned;

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
            return normal != (int)RewardState.Collected ? (RewardState)normal : (_rewardPremClaimed[i] ? RewardState.Collected : RewardState.Unlocked);
        }

        /// <summary>`Gacha Reward Claimed` 上的 `Claimed Tex`。
        /// ⚠️ 原版是本地化词条（prefab 里的占位串是 `'Recogido'` = 西语「已领取」）⇒ **我们按英文写**。</summary>
        public static string RewardClaimedText() { return "Claimed"; }

        // ⚠️ 用 **prefab 自带的占位串**（原版运行时按 I2 词条本地化，本地没有语言表）——
        //    两条 TMP 实测 `H=2 (Center)`、fs=36，框只有 200 宽；自己写更长的英文会**溢出被切**（渲染图实证）。
        public static string FreeTrackTitle() { return "Ruta Gratuita"; }
        public static string PremiumTrackTitle() { return "Ruta Premium"; }
        public static string PremiumTrackPrice() { return "9999"; }             // ⚠️ 我们挑的（边界②：不卖）

        // 顶栏（阵营）—— 原版这里是 `ArmyUtilities.Instance.GetArmyIcon(army)` + `GetLocalizedArmyName(army)`，
        // 单机没有「当前是哪个阵营的日常事件」那份数据（原版 = LiveOps 配的 `MainMenuMission.army`）⇒
        // **阵营固定取 `Goff`(20)**（prefab 出厂占位串就是 `'Orks'`，与 Goff 的英文显示名一致）。
        // 🔴 **2026-10-19（U5）**：图名/显示名**不再写死**，走 `Core/ArmyIcons`
        //   （= 原版 `IconsByArmySO` 逐字节解出的唯一一份映射；判据链在那个文件头）。
        public static string HeaderArmyIcon() { return ArmyIcons.IconName(HeaderArmyValue); }
        public static string HeaderArmyName() { return ArmyIcons.LocalizedName(HeaderArmyValue) ?? "Orks"; }   // 原版 prefab 占位串就是 'Orks'
        public static string HeaderArmySubTitle() { return "Daily Rewards"; }   // 🔶 自己设计（非原版）：顶栏副标题 `Daily Rewards` · 原版是本地化词条、本地无 value（原版判据拿不到） · TODO(谁定的)｜原注：⚠️ 我们挑的（原版是本地化词条）
        /// <summary>本窗顶栏固定展示的阵营（见上一条注释；`CardArmy.Goff = 20`）。</summary>
        public const int HeaderArmy = 20;

        /// <summary>每日奖励窗右下那颗 `Timer Text`。🆕 **2026-10-17（W_A_TimerDisplay）**：真倒计时 ——
        /// 与日常那族同一份目标（自建日界 `NextResetAt`，本机时间）。原版由 `TimerDisplay` 运行时填。
        /// ⚠️ 本窗（`DailyRewardPopup.cs`）**不在本批白名单** ⇒ 只给了「建窗那一刻」的值，没有挂每秒 tick 的组件。</summary>
        public static string RewardTimerText() { return ResetIn(); }

        /// <summary>领一天的奖励。**只有 `Unlocked` 那一态能领**（原版的 `colider` 也只有那一态可点）。
        /// 🆕 **2026-10-11（批次1 · W1 · A309）起返回「这一下是不是真的领到了」**（与 `CollectDaily` 同一条口径）
        /// —— 领到了才弹那扇 `Reward Window`（见下面「领奖窗」那一段）。老调用点当语句用，照样编得过。</summary>
        public static bool CollectReward(int day, bool premium)
        {
            var st = RewardStateOf(day, premium);
            if (st != RewardState.Unlocked)
            { Say($"第 {day + 1} 天{(premium ? " Premium" : "")}奖励现在是 `{st}`，领不了"); return false; }
            if (premium) _rewardPremClaimed[RI(day)] = true; else _rewardClaimed[RI(day)] = true;
            string art = RewardIconOf(day, premium);
            int n = RewardAmount(day, premium);
            Wallet.Grant(art, n);
            Say($"第 {day + 1} 天{(premium ? " Premium" : "")}奖励已领取");
            ShowCollectedWindow(art, n, premium);
            return true;
        }

        /// <summary>🆕 **2026-10-12（A479/A480）**：`Daily Reward Popup` 的**关窗那一拍**先收一遍
        /// （原版 `DailyRewardPopup.Close()` = `LiveOp.TryCollect(() => base.Close())`，与连登窗
        /// `DailyStreakWindow.Close()` **是同一个调用** —— 判据逐环见下）。
        /// <para>🔴 **判据（逐环实读，⛔ 不是推的）**：
        /// ① `d:/2/tools/decomp_full/DailyRewardPopup__Close.c`：取 `this.LiveOp`（`+0x70` ——
        ///    `DailyRewardPopup : LiveOpsEventWindow&lt;MainMenuMission&gt;` 自己的字段从 `0x78` 起，
        ///    见 `d:/2/tools/il2cpp_out/dump.cs` 的类声明）→ 造一个到 `&lt;Close&gt;b__11_0` 的委托
        ///    （宿主方法体 = `DailyRewardPopup___Close_b__11_0.c` = `LiveOpsEventWindow&lt;object&gt;.Close`）
        ///    → `FUN_18102f400(LiveOp, 委托)`；
        /// ② 那个被调地址 **`0x18102F400` = `MissionEvent&lt;object,object&gt;.TryCollect(Action)`** ——
        ///    与连登窗那两处（`DailyStreakWindow__Close.c` / `__CollectRewardClicked.c`）**逐位相同**
        ///    （`dump.cs` 的 `GenericInstMethod` 表读法见 `Shell/DailyData.cs` 的 `StreakAutoCollect` 注释）；
        /// ③ `TryCollect` 本体（`decomp_full` 里没有这份 `.c` ⇒ 按 `dump.cs` 的 RVA 走
        ///    `工具/disasm_va.py` 实读）= **`CurrentChallenges.FirstOrDefault(x =&gt; x.canCollect)`**：
        ///    取到 ⇒ 尾调 `Collect(challenge, onComplete)`；取不到 ⇒ **直接调 `onComplete()`**。
        /// ④ ⚠️ **H29 §七·1 把另一个调用点记成「`&lt;Start&gt;` 一处 `TryCollect(null)`」—— 那是它的形状、
        ///    不是它的时机**（本件就地核过，铁律 5）：`&lt;Start&gt;b__6_0`（`dump.cs` 里签名 =
        ///    `private void &lt;Start&gt;b__6_0(ChallengeMilestone milestone)`）**不是开窗时收**，而是
        ///    **选择器那颗奖励的点按处理器** —— `DailyRewardPopup__Start.c` 那句
        ///    `Delegate.Combine`（`UIGenericEventCatcher/SourceDelegate` 那个形状）写回的正是
        ///    `rewardsTrack(DailyRewardSelector, +0x88) 的 **`OnCollect`**（`+0x58`，类型
        ///    `Action&lt;ChallengeMilestone&gt;`，见 `dump.cs` 的 `DailyRewardSelector` 字段表），
        ///    而 `DailyRewardSelector__CollectRewardClicked.c` 就是**调 `+0x58` 这个委托**。
        ///    ⇒ **原版这扇窗【没有】开窗自动收**；它只有两处 `TryCollect`：**关窗**（续作 = 关窗）
        ///    与**点奖励**（续作 = null）。⛔ 别在 `Open()`/`Build()` 里加自动收（那是我们发明的时点）。</para>
        /// <para>🔴 **A480 · 如实标注（调度台裁定：维持本件落地）**：原版那一下 `TryCollect` 收的是
        /// **`CurrentChallenges` 里的【第一条 `canCollect`】** —— **不止本窗那一格**（理论上可能是某条每日任务，
        /// 或连登那一条）。而「第一条是谁」由**原版那个列表的顺序**决定，那份序是**服务端下发的、本地查不到**
        /// ⇒ 我们按**本窗那一格**收（`RewardStateOf(...) == Unlocked`），并在这里如实写明这是**我们的落地**、
        /// 不是原版那一条。判据：H29 报告 §六·1（「本地查不到」那一格）。</para>
        /// <para>**返回值**（与 `CollectReward` / `StreakAutoCollect` 同口径）：`true` = 真收到了；
        /// `false` = 没有可领的（= 原版 `FirstOrDefault` 取不到、直接走续作那一支，**什么都不发**）。</para>
        /// <para>**改坏法**：① 把 `DailyRewardPopup` 关钮那两句里的本调用删掉 ⇒ 自检「关窗 ⇒ 真收到了」红；
        /// ② 把 `RewardStateOf != Unlocked` 那道守卫（= 转调的 `CollectReward` 自己的守卫）绕过 ⇒
        /// 「已领过再关一次 ⇒ 一份都没再发」红。</para></summary>
        public static bool DailyRewardAutoCollect()
        {
            // 本窗那一格 = **第一格可领的抽屉**，次序照本窗自己的摆法（一天里 `NormalReward` 在
            // `Premium Reward` 前 —— `DailyRewardPopup.BuildDrawer` 那两句的次序），天号升序。
            for (int day = 0; day < RewardDays; day++)
            {
                if (RewardStateOf(day, false) == RewardState.Unlocked)
                {
                    Say($"关窗时自动收取可领的那一格：第 {day + 1} 天（普通轨）"
                        + "（原版 `Close()` = `LiveOp.TryCollect(() => base.Close())`）");
                    return CollectReward(day, false);      // ⛔ 别在这里另写一份发奖（同一条路 = 同一份守卫/记账/开窗）
                }
                if (RewardStateOf(day, true) == RewardState.Unlocked)
                {
                    Say($"关窗时自动收取可领的那一格：第 {day + 1} 天（Premium 轨）"
                        + "（原版 `Close()` = `LiveOp.TryCollect(() => base.Close())`）");
                    return CollectReward(day, true);
                }
            }
            // 原版那一支：`FirstOrDefault(canCollect)` 取不到 ⇒ 什么都不做，直接走续作。
            Say("关窗时这一窗没有可领的（原版 `TryCollect` 取不到可领的挑战 ⇒ 直接走续作，什么都不发）");
            return false;
        }

        // ============================================================ 收件箱（`Inbox Menu`）
        //
        // ⚠️ **单机没有服务器** ⇒ 原版由 LiveOps handler 下发的消息表由**本地等价物** `Shell/InboxData.cs` 提供
        //    （2026-10-25 N17 开线后新增；判据与边界写在那份文件的头注释里）。本类只留**读口**。
        // 🔴 **2026-10-25（铁律 5 就地订正）**：下面这一行原来写 `InboxCount { get { return 0; } }`、
        //    注释说「单机恒 0」—— **已过期**。现在它是**未读条数**（原版 `Inbox.CheckNotification` =
        //    `Count(x => !x.IsRead())`，`Inbox.cs:745-778`）⇒ 转发 `InboxData.UnreadCount()`；
        //    `InboxNotificationProvider`（`BadgeNotifications.cs:366`）读它 ⇒「选中消息 → 标已读 → 红点灭」自动通。

        /// <summary>**未读**消息条数（原版 `Inbox.CheckNotification()` 的计数）。</summary>
        public static int InboxCount { get { return InboxData.UnreadCount(); } }
        public static string InboxTitle() { return "Inbox"; }                       // 原版 prefab 占位串
        public static string InboxNoNewsText() { return "Game announcements will be displayed here"; }  // 原版 prefab 占位串
        /// <summary>`Message Display/Title` 的**出厂占位串**。🔴 2026-10-25 订正：原来写的是 `"Message"`
        /// （标注「我们挑的」）—— 原版 prefab 那颗 TMP 的原文是 **'WELCOME TO WARPFORGE CLOSED ALPHA!'**
        /// （判据 = `menu_dump … "Inbox Menu" --depth 9 --md --no-sprite` 里那一行的 `'…'`）。
        /// 运行时它会被 `MessageDisplay.Initialize` 写成本条消息的 `GetTitle()`（`InboxWindow.ShowDetail`）。</summary>
        public static string InboxMessageDisplayTitle() { return "WELCOME TO WARPFORGE CLOSED ALPHA!"; }
        /// <summary>收件箱红点该不该亮 —— 原版 `Inbox.CheckNotification` = **未读条数 > 0**。</summary>
        public static bool InboxHasBadge { get { return InboxCount > 0; } }

        // ============================================================ 每日连登（`Daily Streak Popup`）
        //
        // 两态**照原版**：驱动字段是任务对象的 `HasFailed`/`FailedValue`，靠**两个面板互斥**。
        // ⚠️ 原版**出厂亮着的是「断了」那一态**；我们按单机口径默认 `HasFailed = false`（看连胜态）。
        // ❌ **我们挑的**：连了几天、奖品格数、奖励内容、文案。

        /// <summary>有没有「可领取」的每日任务 —— **奖励窗左栏 Missions 键上的红点判据**。
        /// 原版是 `Missions.CheckNotification`（`INotificationProvider&lt;MissionsBadge>`），
        /// 显隐走 `UiBadgeNotification` 的 **alpha 补间**（`Show()`→1.0 / `Hide()`→0），**不是 `SetActive`**。</summary>
        public static bool RewardsHasBadge { get { return CollectableMissionCount() > 0; } }

        /// <summary>🆕 **2026-10-25（N17）**：**当前可领的任务条数** —— 红点的唯一计数口。
        /// <para>判据 = 原版 `Missions.CheckNotification()`：`OfType&lt;IMission&gt;()` → 逐条
        /// `MissionChallengeProgress.CanCollect()` → `Repeat(badge, 可领条数)`（`Missions.cs` 的
        /// `SelectMany` + `Enumerable.Repeat`）。⇒ **四张卡都算**：三条每日 + 周常 + 骷髅 + 登录。</para>
        /// <para>⚠️ 登录卡那一条是**单机口径**（`CanCollectLogin` = `_loginState != Claimed`）：原版那条
        /// `DailyLoginChallenge` 在服务端完成登录时就算「可领」，没领之前一直可领 ⇒ 语义相同。</para>
        /// <para>🔴 **红点 provider 与 `RewardsHasBadge` 共用这一份**（⛔ 别在 `BadgeNotifications` 里再写一遍
        /// `for + CanCollectDaily` —— 两处写同一条规则 = 迟早不一致）。</para></summary>
        public static int CollectableMissionCount()
        {
            int n = 0;
            for (int i = 0; i < _daily.Length; i++) if (CanCollectDaily(i)) n++;
            if (CanCollectWeekly()) n++;
            if (CanCollectSkulls()) n++;
            if (CanCollectLogin()) n++;
            return n;
        }

        /// <summary>三条每日任务的条数（自检/红点 provider 用；⛔ 别在别处再写死 `3`）。</summary>
        public static int DailyTaskCount { get { return _daily.Length; } }

        /// 🔶 自己设计（非原版）：单机默认不断签（`_streakFailed = false`） · 原版由服务端 `currentValue` 给 · TODO(谁定的)｜原注：<summary>⚠️ 我们挑的：单机默认**不断签**（这样默认看到的是 `Streak Successful`）。</summary>
        static bool _streakFailed = false;
        /// <summary>⚠️ 我们挑的：当前连了几天（原版由服务端 `currentValue` 给）。</summary>
        const int StreakCurrent = 5;

        // ---------------------------------------------------------- 🆕 本地累计连登（A815 · 用户 2026-10-15 拍板）
        //
        // 🔴 **为什么另开一份、不去改上面那个 `StreakCurrent`**（铁律 5·c / 5·b）：
        //    `StreakCurrent = 5` 是**连登弹窗那条奖格轨的夹具值**（「我们挑的」，与 `StreakCollected = 5`
        //    同一套自检夹具 —— 弹窗的 A509 / A316 那一族断言全建在它上面）。它是**画面内容**，
        //    回答的是「弹窗该画第几格可领」，**不是**「玩家真的连着登了几天」。
        //    A815 这一件要的恰是**后者** ⇒ 两者语义不同，合并会把弹窗那一族断言一起改口径。
        //    📌 **还欠一件**（已记进 `资料/普查产出_1016/W1_A815第4页签.md`）：弹窗那个数要不要也换成读本计数
        //      —— 原版那个数是**服务端下发的 `currentValue`**（本地拿不到），所以只有「统不统一到本计数」这一个选择，
        //      而统一它 = 改 `DailyStreakPopup` 的夹具口径（不在 A815 这一件里）。
        //
        // ⚠️ **口径 = 我们挑的**：原版连登由服务器给（`MainMenuMission.customData['lastUpdate']` / `FailedValue`，
        //    见本类落盘那一节的注释），**跨进程 / 跨设备的行为本地查不到**（服务器已关、CCD 不可达）
        //    ⇒ 我们照 `TryDailyReset()` 那一套用**本机时间的「日」**（与它同一条口径 ——
        //      它里面对应的那句是 `var now = System.DateTime.Now;`，紧挨着 `EnsureNextReset(now)`）。

        /// <summary>🆕 **本地累计的连续登录天数**（跨天 +1 · 断天归 1）。**A815 那个页签读的就是它**。</summary>
        public static int StreakLoggedDays { get { return _streakDays; } }

        /// <summary>连着登了几天（`0` = 本机从没记过）。</summary>
        static int _streakDays;
        /// <summary>最近一次**记过的那一天**（本机时间的 `.Date.Ticks`；`0` = 从没记过）。</summary>
        static long _streakDayTicks;

        /// <summary>🆕 **2026-10-25（`X2`）**：**累计登录天数**（只增不减；`Ach_17` 的计数源）。
        /// `0` = 本机从没记过。口径与「为什么是我们挑的」→ <see cref="StoreDto.totalLoginDays"/>。</summary>
        public static int TotalLoginDays { get { return _totalLoginDays; } }
        static int _totalLoginDays;

        /// <summary>**记一次登录**（A815 那一页的计数源头）。
        /// <para>🔴 **挂在哪一拍**：`TryDailyReset()` 里、它那句 `if (now &lt; _nextReset) return false;` **之前**
        /// —— 而 `TryDailyReset()` 是 `MainMenuRuntime.Build()` 调的（`Shell/MainMenuRuntime.cs` 的 `TryDailyReset()`）
        /// ⇒ 也就是**进壳 / 回主菜单那一拍**。⚠️ **不改 `TryDailyReset` 的返回值 / 判据 / 诊断量**
        /// （那一支的语义全在 `DailyResetChecks` / `DailyResetCount` / `_nextReset` 上，见它自己的 doc）。</para>
        /// <para>**规则**（A815 口径 · 用户 2026-10-15 拍板）：从没记过 ⇒ **1** · 与上次相差**恰好 1 天** ⇒ **+1** ·
        /// 相差 **≥ 2 天**（断天）⇒ **归 1**（今天算第 1 天）· **同一天**进多少次壳 ⇒ **只记一次**（幂等）。</para>
        /// <para>⚠️ **「断几天算断」是我们的口径**（照**自然日差**：隔一天没登 ⇒ 归 1）—— 原版这一拍是
        /// **服务端**做的，本地查不到它按什么时区 / 什么日界算（服务器已关）⇒ 如实标，⛔ 别写成「照原版」。</para>
        /// <para>**改坏法**：① 把 `gap == 1` 写成 `gap >= 1` ⇒ 「断天归 1」那条红；
        /// ② 删掉 `gap == 0` 那一句早退 ⇒ 同一天进几次壳就 +几次（幂等那条红）；
        /// ③ 删掉末尾 `Persist(...)` ⇒ 「重启后连登数还在」那条红（模拟重启读回来的是旧值）。</para></summary>
        static void RecordLogin(System.DateTime now)
        {
            var today = now.Date;
            if (_streakDayTicks != 0)
            {
                var last = new System.DateTime(_streakDayTicks).Date;
                int gap = (int)(today - last).Days;          // 整「日」之差（两边都已取 .Date）
                if (gap == 0 && _streakDays > 0) return;     // 同一天 ⇒ 什么都不做（幂等）
                if (gap == 1) _streakDays++;                 // 跨天 ⇒ +1
                else _streakDays = 1;                        // 断天（≥ 2 天）/ 状态不自洽 ⇒ 归 1
            }
            else _streakDays = 1;                            // 从没记过 ⇒ 今天是第 1 天
            _streakDays = Mathf.Max(1, _streakDays);
            _streakDayTicks = today.Ticks;
            // 🆕 **2026-10-25（`X2`）**：**累计登录天数 +1**（成就 `Ach_17`）。
            //   能走到这儿 ⇒ 今天一定是**新的一天**（同一天在上面 `gap == 0` 那一句已经早退）
            //   ⇒ 首次记 = 1、之后每跨一天 +1。⚠️ 口径是我们挑的（判据 §N5，详见 `StoreDto.totalLoginDays`）。
            _totalLoginDays = Mathf.Max(1, _totalLoginDays + 1);
            Persist("记一次登录（连登 " + _streakDays + " 天 · 累计 " + _totalLoginDays + " 天）");
            // 🆕 2026-10-25（`X2`）：把这两个数接进成就（`Ach_17` 累计 / `Ach_18` 连续）。
            //   ⚠️ 用 **`SetAchievementValueAtLeast`**（不是累加）：这两条记的是**当前状态**，
            //   而**连登断了会归 1** —— 原版存档带 `highestValue` ⇒ 进度**只涨不落**
            //   （`AchievementPersistence { id, value, highestValue, enabled }`，判据 §B-1）。
            ProfileData.SetAchievementValueAtLeast(ProfileData.IdAchLoginTotalDays, _totalLoginDays);
            ProfileData.SetAchievementValueAtLeast(ProfileData.IdAchLoginStreakDays, _streakDays);
        }

        /// <summary>自检用：喂一个「现在」走**同一条实现**（⛔ 不是另写一份规则 —— 两处写同一条 = 迟早不一致）。
        /// ⚠️ 它**会落盘**（走 `RecordLogin` 末尾那一句），所以自检必须先设 `OverrideDir`（见 `StoreOn`）。</summary>
        public static void RecordLoginForTest(System.DateTime now) { RecordLogin(now); }

        /// <summary>自检用：把连登那一格摆成**确定的一态**（⛔ 运行时别调；它**不落盘** —— 要落盘显式调
        /// `SaveNowForTest()`）。`lastDay` 传 `default` ⇒ 造「本机从没记过」那一态。</summary>
        public static void ForceStreakForTest(int days, System.DateTime lastDay)
        {
            _streakDays = Mathf.Max(0, days);
            _streakDayTicks = lastDay == default(System.DateTime) ? 0L : lastDay.Date.Ticks;
        }

        /// <summary>自检用：读「最近一次记过的那一天」（`default` = 从没记过）—— 快照 / 收工还原用。</summary>
        public static System.DateTime StreakLastDayForTest()
        {
            return _streakDayTicks == 0 ? default(System.DateTime) : new System.DateTime(_streakDayTicks);
        }

        /// <summary>自检用：把「累计登录天数」摆成**确定的一态**（⛔ 运行时别调；它**不落盘** ——
        /// 要落盘显式调 `SaveNowForTest()`）。🆕 2026-10-25（`X2`）。</summary>
        public static void ForceTotalLoginDaysForTest(int days) { _totalLoginDays = Mathf.Max(0, days); }

        /// <summary>一条连登轨上有几个奖格（原版由 `challenges` 长度定）。</summary>
        public const int StreakDays = 7;
        /// <summary>`i &lt; 它` ⇒ 这一格**已领**；`i == 它` ⇒ **可领**（也是 `scaleMultiplierFirstElement` 作用的那一格）。</summary>
        public const int StreakCollected = 5;
        /// 🔶 自己设计（非原版）：断签时掉的层数 `StreakLostValue = 10` · 原版是 `MainMenuMission.FailedValue`（服务端） · TODO(谁定的)｜原注：<summary>⚠️ 我们挑的：断签时掉的层数（原版是 `MainMenuMission.FailedValue`）。</summary>
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
        /// <summary>'More Rewards In' —— **奖励窗与连登窗共用同一条本地化词条** ⇒ 口名取**中性**的。
        /// <para>🔴 **判据（原版两窗是同一条词条）**：连登窗 prefab 里那一颗
        /// （`Daily Streak Popup/Streak Successful/Timer/Next Rewards text`）的占位串是英文
        /// `'More Rewards In'`，而奖励窗 prefab 里那一颗
        /// （`Daily Reward Popup/Timer/EverguildTextMeshPro`）**落到了 es 语系** `'Más Recompensas En'`
        /// —— **同一条 I2 词条、两个语言**（铁律 5·c：一个值 ≠ 全部情况）。
        /// 出处 → `资料/普查产出_1013/WD3_领奖弹窗补建.md` §四·3（A516 那一节逐条核过）。</para>
        /// <para>⚠️ **2026-10-13（A643）**：原来只有 `StreakNextRewardsText()`（名字带 `Streak`），
        /// 而 A516 给奖励窗补那颗 `EverguildTextMeshPro` 时**只能复用它** ⇒ 名字会把下一个会话引向
        /// 「这是连登窗专用」。⇒ **两窗一律改用本口**（字面量只留这一处 ——
        /// 「两处写同一条规则 = 迟早不一致」）；旧名保留为**转发**，⛔ 新调用点别再用它。</para></summary>
        public static string MoreRewardsInText() { return "More Rewards In"; }   // 原版 prefab 占位串（连登窗那份；奖励窗是 es 版同词条）
        /// <summary>⚠️ **2026-10-13（A643）起本名只是旧名转发**：两窗共用的那条词条改用中性口
        /// `MoreRewardsInText()`。保留它只为不掐断既有调用点（本仓不许留下第二份字面量）。</summary>
        public static string StreakNextRewardsText() { return MoreRewardsInText(); }
        /// <summary>连登窗 `Timer Text`（prefab 占位 `19h 23m`）。
        /// 🆕 **2026-10-17（W_A_TimerDisplay）**：真倒计时 —— 目标 = 自建日界 `NextResetAt`（本机时间）。
        /// <para>⚠️ **我们挑的**：原版这一格的具体目标时刻（服务端的 `LiveOp` 到期时间）本地查不到；
        /// 连登的语义就是「下一天开始」，日界是数据模型里唯一对得上的时刻 ⇒ 取它。⛔ 不是原版值。</para></summary>
        public static string StreakTimerText()
        { return TimerDisplay.TextFor(NextResetAt, "", "连登 Timer", null); }
        public static string StreakBrokenText() { return "STREAK BROKEN"; }          // 原版 prefab 占位串
        public static string StreakLostText() { return "Streak lost: " + StreakLostValue; }
        public static string ResetStreakText() { return "Reset Streak"; }            // 原版 prefab 占位串
        /// <summary>连登卡的领取钮文案。🔴 **2026-10-17（`N47` 整包普查）**：原版对应词条 =
        /// `ShopOffers/GeneralClaim`（`Daily Streak Reward Popup Entry / Collect / Collect Text` 的
        /// `Localize.mTerm`，enabled=1）。
        /// <para>🔴 **2026-10-19（W57 · UI-6）就地订正（铁律 5）**：本段原来写「该键在本地 13 语言表与 en+zh 表里
        /// 都不存在 ⇒ 属快照缺口」，并因此调 `Loc.BulkOr` —— **两句都不成立**：该键在官方 13 语言表
        /// `Resources/official_loc_13lang.json` 里**有**（现读 en `Claim` / zh `索取`，13 列全非空）；
        /// 而 `Loc.BulkOr` **只查** `Resources/official_names.json` 的 bulk 表（八个数据面族 + `Card_Trait`，
        /// **不含 `ShopOffers/*`**）⇒ 原写法**恒回兜底 `Claim`**（中文档也印英文，且每次打一条缺键警告）。
        /// 错因 = 当时拿「bulk 表里没有」当成了「本地没有」。</para>
        /// <para>改走 <see cref="Loc.T"/>（第三档兜官方全表）⇒ 中/英/其余 10 语言都能取到。
        /// 调用点 = `Shell/DailyStreakPopup.cs` 的 `BuildEntry`（`Collect Text` 那一颗，2026-10-19 同批接回本口
        /// ⇒ **键字面量只留这一处**）。⛔ 别再退回 `BulkOr`，也⛔ 别在调用点另写一份字面量。</para></summary>
        public static string StreakClaimText() { return Loc.T("ShopOffers/GeneralClaim"); }
        /// <summary>连登窗说明句（`Daily Streak Popup/Streak Successful/Info` 与 `Streak Failed/Info` 两态共用）。
        /// 🔴 **2026-10-19（W57 · UI-6）：回原版词条 `Rewards Menu/DailyStreak/Help`** —— 原来是我们自拟的
        /// `Log in every day to keep your streak going.`，**既不是 prefab 出厂文本、也不是官方词条**
        /// （第三种串，铁律 5·b/11）。
        /// <para>判据（`d:/2/新解包资源/assets_full/bundle_menus_assets_all` 现读）：
        /// ① 两颗 TMP 各挂 `Localize`：`mTerm = Rewards Menu/DailyStreak/Help` · `m_Enabled = 1` ·
        ///    `LocalizeOnAwake = 1`（`mLocalizeTargetName = I2.Loc.LocalizeTarget_TextMeshPro_UGUI`）
        ///    ⇒ 运行期显示的是**官方词条值**（MB `-2550844335809879381` / `4744420865844781739`）；
        /// ② prefab 出厂 `m_text` 就是官方英文值（`Log in daily and keep your streak going to unlock bigger rewards.
        ///    Skip a day, and your streak resets.`）；官方 13 列全非空（zh = `每天登录并保持连续登录，以解锁更大的奖励。
        ///    跳过一天，你的连续登录就会重置。`）；
        /// ③ TMP 框 = `Streak Successful/Info` `47.47,830 → 1872.53,880`（**1825.06×50**）·
        ///    `Streak Failed/Info` `47.43,583 → 1872.57,633`（**1825.14×50**）· 字号 36 ·
        ///    `m_TextWrappingMode = 1`（Normal 折行）· `m_margin = (0,0,0,0)` · `m_overflowMode = 0`（溢出照画、不截断）·
        ///    **`m_enableAutoSizing = 0`**（不缩字号）⇒ 调用点按原版框宽传 `wrapPx`（见 `DailyStreakPopup`）。
        /// ⚠️ 本口同时被**自建**「每日连胜」页签使用（`Shell/DailyStreakTab.cs:104`），那一格**没传 `wrapPx`**
        /// （该文件不在本笔白名单）⇒ 长串会超出它那 1050px 的框；已写进报告，等主对话派活补。</para></summary>
        public static string StreakInfoText()
        { return Loc.T("Rewards Menu/DailyStreak/Help"); }

        public static string StreakRewardName(int i) { return _streakName[SI(i)]; }
        public static string StreakRewardIcon(int i) { return _streakIcon[SI(i)]; }
        /// <summary>第 `i` 格**现在读作「已领」**。
        /// 🔴 **2026-10-13（A509）就地改口径（铁律 5·b/11）**：本行原来写
        /// `_streakClaimed[SI(i)] || SI(i) &lt; StreakCollected` —— 那是个**「或」**：
        /// 只要 `SI(i) &lt; StreakCollected` 就**恒 `true`**，**「本局领过」那一位被整段吞掉**。
        /// 今天恰好看不出来（`StreakCollected = 5`、自检只碰第 5 格 ⇒ 那一位单独说了算），
        /// 但 `StreakCollected` 一改（H45 §四·1 点出的那一档：调小/夹具造更小的态之后
        /// `Force(…, false)` **照样读 `true`**）⇒ **读口与守卫说的就不是同一件事**，
        /// 而 `Editor/RewardsScene.cs` 的起点快照 / 收工还原**全建在这个读口上**
        /// ⇒ 那条「弱改坏法」会**反过来咬人**（H45 §四·1 原文）。
        /// <para>⇒ 现在**问守卫真正问的那一位**：可领那一格直接取 `!StreakRewardUnlocked(i)`
        /// （守卫读的就是 `_streakClaimed[SI(i)]`）⇒ 两个读口在这**格上互为补**，不可能再各说各的
        /// （本仓「两处写同一条规则 = 迟早不一致」）。</para>
        /// <para>⚠️ **另两档仍是位置口径**（原版那条 `index &lt; collectedRewards`）：
        /// `i &lt; StreakCollected` ⇒ 已领（引擎口径，与那份布尔无关）；`i &gt; StreakCollected` ⇒ **还没轮到**。
        /// ⛔ 别把整条写成 `!StreakRewardUnlocked(i)` —— 那会把**还没轮到**的格也读成「已领」。</para>
        /// <para>**今天行为逐格不变**：唯一的界面读点 `DailyStreakPopup.cs` 只在
        /// `if (unlocked &amp;&amp; !claimed)` 这个**合取**里用它，而 `unlocked` 为真只在
        /// `i == StreakCollected` 那一格 ⇒ 另两档怎么读都不进画面（改前改后同一张画面）。</para>
        /// <para>**改坏法**：把本方法退回只问位置那一半（删掉 `!StreakRewardUnlocked(k)` 这一支）
        /// ⇒ `Editor/RewardsScene.cs` 的 A509 那两条（两读口互为补 / 两态一起翻面）红。</para></summary>
        public static bool StreakRewardClaimed(int i)
        {
            int k = SI(i);
            if (k == StreakCollected) return !StreakRewardUnlocked(k);   // = 守卫真正问的那一位（`_streakClaimed[k]`）
            return k < StreakCollected;                                  // 其余各格：位置口径（还没轮到 ⇒ false）
        }
        /// <summary>第 `i` 格**现在能不能领**（原版 `DailyStreakItemContainer` 那个「可领取」态）。
        /// 🔴 **2026-10-12（A316）统一了守卫口径**（铁律 5·b/11）：这里原来只写 `SI(i) == StreakCollected`
        /// —— **不读 `_streakClaimed`** ⇒ 与 `CollectReward`（守卫问的是 `RewardStateOf(day,premium)`，
        /// 而那一位**读了 `_rewardClaimed[]`**，见它那一大段注释）**两条口径不一致**。后果与那一处同型：
        /// **已领过的那一格，守卫照样放行** ⇒ `CollectStreak(i)` 连点两次**发两份奖**
        /// （`_streakClaimed[i]` 只写不判）。
        /// <para>判据：原版那一格的可点性/显隐是**一个**「可领取」态 ——
        /// `DailyStreakPopup` 自己在 `Shell/DailyStreakPopup.cs` 里那处 `unlocked &amp;&amp; !claimed` 把 `unlocked` 与 `claimed`
        /// 各算一次、再 `unlocked &amp;&amp; !claimed` 合成一个布尔（画 `Collect` 与 `Highlight` 都用它）。
        /// ⇒ **口径统一到这里**（= 「它是那一格 **且** 还没领过」），于是那个合成式**自动等价**
        /// （第 5 格领完 ⇒ `unlocked` 变假 ⇒ `&amp;&amp; !claimed` 那一半成了冗余，画面**逐帧不变**；
        /// 其余各格 `unlocked` 本来就假）。⛔ 别把 `!_streakClaimed` 那一半删回去。</para>
        /// <para>⚠️ **今天 UI 侧走不到**「连点两次」：领完那一格 `Collect` 就不再画
        /// （`Shell/DailyStreakPopup.cs` 里那句 `if (unlocked &amp;&amp; !claimed)`），
        /// 而且这一下会弹领奖窗、把连登窗压到 `Background`（指针命中不到它）。
        /// **但守卫仍要对** —— 它是「两处共用的同一份布尔」那一半，`CollectStreak` 可以被别处直调
        /// （自检就是），口径不一致**迟早**会在某个新入口上现形（本仓「两处写同一条规则 = 迟早不一致」）。</para></summary>
        public static bool StreakRewardUnlocked(int i) { return SI(i) == StreakCollected && !_streakClaimed[SI(i)]; }
        static int SI(int i) { return Mathf.Clamp(i, 0, StreakDays - 1); }

        public static bool CollectStreak(int i)
        {
            if (!StreakRewardUnlocked(i)) { Say($"连登第 {i + 1} 格现在领不了（守卫问的是同一份布尔）"); return false; }
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

        /// <summary>关窗/返回时原版会先收一遍（`Close()` = `LiveOp.TryCollect(() => base.Close())`）。
        /// <para>🔴 **2026-10-12（A400）就地订正（铁律 5·b / 11）：这一条原来是个【空壳】**
        /// —— `if (…) return;` 之后**只写一句 `Say`**，**既不置 `_streakClaimed`、也不发奖**
        /// ⇒「关窗自动收」这条原版行为在我们这儿**一次都没发生过**，而且**不出声**（静默失败）。
        /// 现在补成**真收**。</para>
        /// <para>🔴 **判据（逐环实读，⛔ 不是推的）**：
        /// ① `d:/2/tools/decomp_full/DailyStreakWindow__Close.c` 末段 = 取 `this.LiveOp`（`+0x70`）→ 造一个
        ///    到 `&lt;Close&gt;b__23_0` 的委托（宿主方法体 = `DailyStreakWindow___Close_b__23_0.c`，
        ///    本体 `LiveOpsEventWindow&lt;object&gt;.Close`）→ `LiveOp.&lt;某方法&gt;(委托)`；
        /// ② 那个被调地址 `0x18102F400` = **`MissionEvent&lt;object,object&gt;.TryCollect(Action)`** ——
        ///    `d:/2/tools/il2cpp_out/dump.cs` 的 `GenericInstMethod` 表里它写着
        ///    `-RVA: 0x102F400 Offset: 0x102DE00 VA: 0x18102F400`，与 `.c` 里那个常量**逐位相同**
        ///    （`VA = 0x180000000 + RVA`，读法见 `资料/全量反编译_入口与用法.md`）；
        /// ③ `TryCollect` 本体（`decomp_full` 里**没有**这个 `.c` ⇒ 按 `dump.cs` 的 RVA 走
        ///    `工具/disasm_va.py` 实读）=
        ///    **`CurrentChallenges.FirstOrDefault(x =&gt; x.canCollect)` → 取到就把
        ///    `(challenge, onComplete)` 尾调给 `Collect(...)`；取不到就【直接调 `onComplete()`】**。
        ///    · 谓词实据 = `MissionEvent.__c_object__object____TryCollect_b__21_0.c` 末句
        ///      `return *(undefined1 *)(param_2 + 0x48);`（`canCollect` 就在 `+0x48` ——
        ///      与 `MissionChallengeProgress__CanCollect.c` 头一句读的是同一个偏移）；
        ///    · 「扫的那个列表 = `CurrentChallenges`」实据 = `MissionEvent&lt;object,object&gt;.get_CurrentChallenges`
        ///      （`dump.cs`：`VA: 0x1810306B0`）反汇编出来只有三句，末句就是 `return this.@0x20.@0x48;`
        ///      —— **与 `TryCollect` 里读的那个表达式逐字相同**（同一份数据）。
        /// ④ 🔴 **点 `Claim` 与关窗在原版是【同一个调用】**：`DailyStreakWindow__CollectRewardClicked.c`
        ///    末句**也是** `0x18102F400`（两次只差续作委托：`DAT_1842b3720` vs `DAT_1842b3420`）
        ///    ⇒ 原版「关窗自动收」收的**就是「点 `Claim` 收的那一份」**。
        ///    ⇒ 我们这一侧 = **转调 `CollectStreak`**（同一条路 ⇒ 同一份守卫 / 同一份记账 / 同一扇开窗 /
        ///    同一句 `Say`），⛔ **别在本方法里再写一份发奖**（本仓「两处写同一条规则 = 迟早不一致」）。</para>
        /// <para>🔴 **今天只收「本窗那一格」，如实标注**：原版 `TryCollect` 取的其实是 `CurrentChallenges` 里的
        /// **第一条** `canCollect` —— 「第一条是谁」由**原版那个列表的顺序**决定，**本地查不到**（见报告
        /// §顺手发现）。我们这一侧只有这一格有「可领」这个概念（`StreakRewardUnlocked`），所以按本窗那一格收。
        /// 旁证（说明连登奖格确实在 `TryCollect` 扫的那个集合里）：`DailyStreakWindow__RefreshRewards.c`
        /// 正是拿 **`CurrentChallenges`**（同一个 getter）去 `FirstOrDefault` 出**连登那一条进度**。</para>
        /// <para>**返回值**（与 `CollectDaily` / `CollectStreak` 那一族同口径）：
        /// `true` = 这一下**真收到了**（已置位 + 进 `Wallet` + 弹领奖窗）；`false` = 没有可领的
        /// （= 原版 `FirstOrDefault` 取不到、直接走续作的 `onComplete()` 那一支，**什么都不发**）。
        /// 两个老调用点都当语句用（`Shell/DailyStreakPopup.cs` 里那两处 `DailyData.CollectStreak(…)` 调用点 / `CardPresentation/Shell/DailyStreakPopup.cs`），换成 `bool` 照样编得过。</para>
        /// <para>**改坏法**：① 把末尾那句 `CollectStreak` 删掉（退回空壳）⇒ 自检「关窗 ⇒ 真收到了」那条红；
        /// ② 把 `StreakRewardUnlocked` 那半句删掉改成恒收 ⇒ 「已领过再关一次 ⇒ 一份都没再发」那条红；
        /// ③ 在这里另写一份 `Wallet.Grant` 而不转调 `CollectStreak` ⇒ 「窗里那一条 = 格子里画的那张图」那条红
        /// （开窗那一步会缺）。</para></summary>
        public static bool StreakAutoCollect()
        {
            if (!StreakRewardUnlocked(StreakCollected))
            {
                // 原版那一支：`FirstOrDefault(canCollect)` 取不到 ⇒ **什么都不做**，直接走 `onComplete()`。
                Say("关窗时那一格没有可领的（原版 `TryCollect` 取不到可领的挑战 ⇒ 直接走续作，什么都不发）");
                return false;
            }
            Say("关窗时自动收取可领的那一格（原版 `Close()` = `LiveOp.TryCollect(() => base.Close())`）");
            return CollectStreak(StreakCollected);   // ⛔ 别在这里另写一份发奖（同一条路 = 同一份守卫/记账/开窗）
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
            string art = t.RewardArt;
            int n = ParseCount(t.RewardText);
            Wallet.Grant(art, n);
            Say("每日任务 " + (i + 1) + " 已领取");
            ShowCollectedWindow(art, n, false);     // 🆕 A313：原版 `Missions.<CollectChallenge>g__OnCollectSuccess_1`
            Persist("领每日任务 " + (i + 1));       // 🆕 A429：领取态是落盘的那一族（重启后不能又变回「可领取」）
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
            // 🆕 **2026-10-26（B9 · P9）**：这两个字面量已上移到类尾的 `WeeklyRewardArt` / `WeeklyRewardCount`
            //   —— 预览（`WeeklyRewardSpecs`）与本处必须**同一份**（原来只在这里一处，预览要读它就得分叉）。
            string art = WeeklyRewardArt;
            int n = WeeklyRewardCount;
            Wallet.Grant(art, n);
            Say("周常已领取");
            ShowCollectedWindow(art, n, false);     // 🆕 A313：原版同走 `Missions.<CollectChallenge>g__OnCollectSuccess_1`
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
            string art = SkullsRewardArt();
            int n = SkullsRewardCount();
            Wallet.Grant(art, n);
            Say("每日骷髅已领取：" + n + " **活动点**（`" + art + "`）");
            ShowCollectedWindow(art, n, false);     // 🆕 A313：原版同走 `Missions.<CollectChallenge>g__OnCollectSuccess_1`
            Persist("领每日骷髅");                   // 🆕 A429（`_skullsState` 是落盘的那一族）
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
            // 🆕 A313：原版 `PlayerDataManager.ProcessNewLoginResult` 走的是同一条 `RewardService.Collect`，
            // 而那个入口收的本来就是一个**列表** ⇒ 两格装进**同一扇窗**（⛔ 不是各弹一扇、也不是只弹第一格）。
            var specs = new CampaignData.RewardSpec[_loginRewardArt.Length];
            for (int i = 0; i < specs.Length; i++)
                specs[i] = new CampaignData.RewardSpec(_loginRewardArt[i], _loginRewardCount[i], CampaignData.TierBasic);
            ShowCollectedWindow(specs);
            Persist("领登录奖励");                   // 🆕 A429（`_loginState` 是落盘的那一族）
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
        /// <item>`case 1,2,4,5,8,9` ⇒ 直接 `System_Array__Empty&lt;T>()` **返回空数组**（一档都不建）</item>
        /// <item>`default` ⇒ 原版 `System_NotImplementedException___ctor` + **不返回**（抛）</item>
        /// </list>
        /// <para>取值名字（`d:/2/tools/il2cpp_out/dump.cs:46188` `enum PlayModes`）：
        /// **给骷髅** = `Classic 0` · `Dungeon 3` · `OfflinePractice 6` · `ClosedDeck 7` · `Replay 10` ·
        /// `RankedFriendly 11` · `OwnDeckTraining 12` · `Skirmish 13` · `Battle4Warpforge 14`；
        /// **一颗都不给** = `Duel 1` · `PracticeLodge 2` · `Tutorial 4` · `CutScene 5` · `Campaign 8` ·
        /// `TutorialReplay 9`。</para>
        /// <para>⚠️ `default` 那一支我们**不抛**（抛一下会把玩家正在打的那一局崩掉，批处理里也会把整条自检带走）——
        /// 改成**出声 + 当作「不给」**；结果与「空里程碑 ⇒ 0 颗」同值，而且**不静默**（红线）。</para>
        /// 🔴 **2026-10-15（A383）订正**：这里原写「本工程今天只会传 0 与 13 进来（`Ctx.Vars.IsSkirmish`
        /// ⇒ 13，否则 0）」—— **已过期**：对局侧现在传的是**入口窗真正声明的那一档**
        /// （`Ctx.PlayMode`，`BattleDriver.cs` 结算处那段注释），可达的是
        /// `Classic 0`（排位）/ `OfflinePractice 6`（练习窗）/ `OwnDeckTraining 12`（`Practice Deck`）/
        /// `Skirmish 13`（遭遇战）**四档**，再加联机与回放带进来的（≤ 15 档）。
        /// **这四档全在「给」那一组** ⇒ 这条闸**仍然不改变任何一局的产出**（改的是「传得对不对」）。
        /// 它的作用是把**零档那 6 个模式**的口径钉死
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
            int before = _skullsCount;
            _skullsCount += skulls;
            // 🆕 **2026-10-25（N17）：到这里那颗 `Collect` 才可能亮。**
            //   判据 = 原版 `MissionChallengeProgress.CanCollect()`（= 「这条挑战真的可以领了」）——
            //   骷髅卡可领的前件就是**里程碑至少过一档**（`SkullsStepDone(0)`，阈值 3）。
            //   🔴 **改之前这里什么都不写** ⇒ `_skullsState` 在全仓**只有出厂 / 读档 / 领取**三处，
            //     `CanCollectSkulls()` 恒 false ⇒ 那颗钮**永久灰色、永远领不到**（N17 分诊挑出的 P0）。
            //   ⚠️ **单态简化（如实标）**：原版是**逐档**可领（`AvailableRewards()` 每档一条），
            //     我们只有一态 ⇒ 第一次跨过任意阈值就变 `Collectable`，领完即 `Claimed`（终态）；
            //     方框亮不亮仍由 `SkullsStepDone(i)` 逐档算（那半不受影响）。
            if (_skullsState == State.InProgress && CrossedStep(before, _skullsCount, _skullsSteps))
                _skullsState = State.Collectable;
            return skulls;
        }

        /// <summary>`before → after` 之间**有没有跨过**这张阈值表里的任意一档
        /// （`before &lt; t &lt;= after`）。判据 = 原版「`challenge.CurrentValue` 涨到某档 ⇒ 那档的
        /// `AvailableRewards()` 非空」那一条（`MissionChallengeProgress.CanCollect.c`）。
        /// ⛔ 别改成「`after &gt;= 第一档`」—— 那会让**已经过了几档的老进度**每局都重新变可领。</summary>
        static bool CrossedStep(int before, int after, int[] steps)
        {
            if (steps == null || after <= before) return false;
            for (int i = 0; i < steps.Length; i++)
                if (before < steps[i] && steps[i] <= after) return true;
            return false;
        }

        /// <summary>🆕 **2026-10-25（N17）**：周常进度往前推（**每局 +1**）。
        /// 🔴 **改之前周常进度根本没有推进点**（`_weeklyProgress` 恒 13、只在自检里被改）⇒ 周常既不长、也永不
        /// 可领（N17 分诊 P0 的另一半）。
        /// <para>⚠️ **「每局 +1」是我们挑的**（原版周常任务的变量类型由服务端下发，本地拿不到 ——
        /// 与 `_weeklySteps` 那六个阈值同一条口径）。闸门用 `ModeGivesSkulls`（同一份「这一局算不算数」），
        /// 免得回放 / 教程把周常刷上去。</para>
        /// <para>**可领判据** = 这一局**跨过了一个新阈值**（`CrossedStep`）—— ⛔ 不是「进度 ≥ 第一档」
        /// （出厂 13 已经过了 5/10，那样一开局就会变可领，与「开局不可领」的既有断言打架、也不合原版语义）。</para></summary>
        static void AdvanceWeekly(int n)
        {
            if (n <= 0) return;
            if (_weeklyState == State.Claimed) return;
            int before = _weeklyProgress;
            _weeklyProgress = Mathf.Min(WeeklyTarget, _weeklyProgress + n);
            if (_weeklyState == State.InProgress && CrossedStep(before, _weeklyProgress, _weeklySteps))
                _weeklyState = State.Collectable;
        }

        /// <summary>一局结束 —— 把**这一局的战果**推进日常进度（原版那一条 `BattleEndSignal` 的我们这一侧）。
        /// <para>`skulls` = 本局拿到的骷髅数（= 原版 `BattleScoreManager.GetSkullCount()` = 把**敌方督军**生命
        /// 削到 ≤20 / ≤10 / ≤0 **各 1 个**的**已达成档数**）；`playMode` = 本局模式的 `PlayModes` 号
        /// （只有 `{0,3,6,7,10,11,12,13,14}` 给骷髅，见 `ModeGivesSkulls`）。</para>
        /// ⚠️ **两个实参都不能自己造**：`skulls` 直接传 `BattleDriver._foeSkullCount`（它已经是
        /// `DeckRules.SkullsFor` 算出来的**同一格字段**）；`playMode` 传 `Ctx.Vars` 那一份对应的枚举值。
        /// ⛔ 别在这里、也别在 `BattleDriver` 里再数一遍档位（铁律 6：判据只写一处）。
        /// <para>**改坏法**：① 把 `AddSkulls` 那两行删掉 ⇒ 骷髅卡恒 `x0`（`RewardsScene` / `BattleScene`
        /// 里「累加」那几条全红）；② 把 `+=` 改成 `=` ⇒ 「第二局 +2」那条会得 2 而不是 3。
        /// 🆕 ③（A429）删掉末尾那句 `Persist("一局结算")` ⇒ 「重启后进度还在」那条红
        /// （本局推进的进度没落盘 ⇒ 重启即回出厂）。</para>
        /// </summary>
        public static void OnBattleEnd(bool win, int damageToEnemy, int troopsPlayed, int skulls, int playMode,
                                       int damageToEnemyWarlord = -1, int attacksDeclared = -1, int rangedAttacksDeclared = -1)
        {
            // 🆕 **2026-10-26（B9 · P9 #518）**：先存「**这一局之前**哪几条已是可领」—— 差分判据。
            //   原版那一条链（`BattleManager__FinishedProcessingMatchResult` → `Missions.UpdateMissionProgress`
            //   → 它的异步回调 `<>c__DisplayClass14_0__<UpdateMissionProgress>b__0` → `TryShowToastNotification`）
            //   拿的正是**更新后**那份 `missions` + `progress`；飘窗只为「刚完成」的那几条弹
            //   ⇒ 我们在这里把「之前可领」记下来，末尾做差即得「这一局刚完成」。
            var wasCollectable = new bool[_daily.Length];
            for (int i = 0; i < _daily.Length; i++) wasCollectable[i] = CanCollectDaily(i);

            // 三张每日任务卡（🆕 2026-10-17 A2070③ 文案已照用户实拍改）：
            //   0 = Deal 45 damage to enemy warlords · 1 = Attack with units 15 times ·
            //   2 = Attack with ranged units 15 times
            // 🔴 **2026-10-17（`N32` 收口）**：真口径计数已在引擎侧补上 ——
            //   `BattleContext.DamageToEnemyWarlord / AttacksDeclared / RangedAttacksDeclared`
            //   （落点：`RuleCore.ApplyDamage` 的督军那一格 + `RuleCore.DeclareAttack` 过完三道闸之后）。
            //   **生产调用点（`BattleDriver`）传这三格** ⇒ 三条任务按实拍口径推进。
            // ⚠️ **缺省 `-1` = 旧口径兜底**：只为**既有的自检夹具**（它们只关心「推到没到顶」，
            //   不想为三条任务各造一份计数）—— 生产路径**必须**传三个新参（`BattleDriver` 已传）。
            if (damageToEnemyWarlord >= 0 || attacksDeclared >= 0 || rangedAttacksDeclared >= 0)
            {
                Advance(0, Mathf.Max(0, damageToEnemyWarlord));
                Advance(1, Mathf.Max(0, attacksDeclared));
                Advance(2, Mathf.Max(0, rangedAttacksDeclared));
            }
            else
            {
                // ===== 旧口径（**只给自检夹具**；生产不走这里）=====
                Advance(0, damageToEnemy);
                Advance(1, troopsPlayed);
                if (win) Advance(2, 1);
            }
            int got = AddSkulls(skulls, playMode);          // 🆕 A375：本局 0~3 个（累加，不是覆盖）
            // 🆕 **2026-10-25（N17）**：周常 +1（同一道「这局算不算」的闸；判据见 `AdvanceWeekly`）
            if (ModeGivesSkulls(playMode)) AdvanceWeekly(1);
            Say($"本局战果进了任务进度：对督军伤害 +{Mathf.Max(0, damageToEnemyWarlord)} · 攻击 +{Mathf.Max(0, attacksDeclared)}"
                + $" · 远程攻击 +{Mathf.Max(0, rangedAttacksDeclared)}（旧口径兜底时这三格为 −1）"
                + $" · **骷髅 +{got}**（当日累计 {_skullsCount}）");
            // 🆕 **A429**：这一族是**落盘**的 ⇒ 一局结算这一拍是**最主要的一个写入点**，落一次
            // （⛔ 别挪进 `Advance`/`AddSkulls` —— 那会让一局写三次盘，而且「三张卡 + 骷髅」本来就是一拍改完的）。
            Persist("一局结算");

            // 🆕 **2026-10-26（B9 · P9 #518）**：**任务完成飘窗** —— 这一局**刚变成可领**的每日任务各弹一条
            //   （≡ 原版 `Missions.TryShowToastNotification`；见 `TryShowMissionToast` 的判据）。
            // 🔴 **为什么只做每日三条**：`ToastNotifications.ShowMission(int)` 的数据源是
            //   `DailyData` 的每日三条（`DailyDesc` / `DailyProgress01` / `DailyCounter` / `DailyRewardArt`
            //   / `DailyRewardText`）。周常 / 骷髅 / 登录那三张卡**原版也是 `IMission`**，
            //   但它们每一档的奖励由服务端下发（`AvailableRewards()`），本地**没有那一格数据**
            //   ⇒ 如实不弹（⛔ 不拿卡级奖励冒充档级奖励），已记进报告。
            for (int i = 0; i < _daily.Length; i++)
                if (!wasCollectable[i] && CanCollectDaily(i)) TryShowMissionToast(i);
        }

        // ============================================================ 每日重置（A384）
        //
        // 🆕 **2026-10-13（A429）**：这一段的状态**现在会落盘**（跨进程 / 重启保持，重启后**不重复重置**、
        //    「关着过了日界再开」也**会**重置一次）—— 形状 / 判据 / 用户裁定 / 自检怎么隔离，
        //    全在本节末尾「每日状态的**落盘**」那一段（⛔ 别只看这一句）。
        //
        // 🔴 **原版这一拍是【后端的】，客户端只有【登记方】**（判据全文 → `资料/普查产出_1012/V8_判据补查.md` §A384）：
        //   · 信号 `MissionResetSignal` **全客户端没有任何地方发** —— 不是「没找到」，是**泛型实例全表的硬否定**：
        //     `d:/2/tools/il2cpp_out/script.json` 的 `Addresses` 段（= 二进制里**全部**泛型实例）里
        //     `Signal.Raise<T>()` 共 37 个 T，**没有 `MissionResetSignal`**；而
        //     `Signal.Register<MissionResetSignal>()` **在**（`script.json:2049611`）⇒ **发点在后端**。
        //   · 客户端只有登记方 `SkullsCount.OnReset()`（`d:/2/tools/decomp_full/SkullsCount__OnReset.c` 全文 = 一句 `Register`）。
        //   · 「每一天」那一拍由**服务器时间的日界**决定：`Missions.ScheduleMissionRefresh` 取
        //     `DailyLoginChallenge.GetNextUpdate()` 与 `PlayerDataManager.CurrentServerDateTime()` 比，
        //     未过期才 `ActionScheduler.Schedule(那一刻, cb)`，而 `cb` = **PlayFab 云脚本 `0x401`**
        //     （`decomp_full/Missions.__c__DisplayClass9_0___ScheduleMissionRefresh_b__0.c`）。
        //     日界值（`decomp_full/DailyLoginChallenge__GetNextUpdate.c`）：进度未满那一支返回 **`serverNow.Date`（当天 0 点）**；
        //     否则 `progress.lastUpdate` → `.Date` → `AddDays(那个 double 常量)`，`0x1834b2f60` **实读 = 1.0** ⇒ **+1 天**。
        //   · 「重置」在客户端表现为 **换一份新的 mission persistence**
        //     （`MissionEvent.DisableChallenge/InitializeChallenge` → `MissionChallenge.DisableChallenge/Initialize`）。
        //
        // ⚠️ **我们这一侧 = 「我们挑的」，如实标注（铁律 3）**：
        //   **我们没有服务器时间**（原版那个 `TimeHelper.ServerDateTime` 是服务端给的，同 `DailyTimer` 那一族）
        //   ⇒ 用**本机时间** `System.DateTime.Now`。形状照原版那一句「**先算出下一次刷新的时刻、到点再比**」
        //   （= 调度台裁的 **口径 (a) 进壳/回主菜单判一次 + 形状 ②**）：存「下一次刷新的时刻」，
        //   每次进壳比对；`Now >= 存的时刻` ⇒ 跨天了 ⇒ 清进度 + 重新算下一次。
        //   🔴 **只覆盖「本机时间」这一种情况**（铁律 5·c）：换时区 / 玩家改系统表**不在覆盖内**
        //      （原版那两件事全由服务器兜）。
        //      🆕 **2026-10-13（A429）就地订正（铁律 5）**：这一句原来还写着「/ **跨进程重启**都不在覆盖内」——
        //      **那一条已经不成立了**：整族现在**会落盘**（见本节末尾），重启后进度还在、日界也照旧档算。
        //      ✅ **2026-10-13 订正（铁律 5）：那句话已经改掉了** —— `Editor/MainMenuScene.cs` 那一份由写手 W-E1 一并改完
        //      （**现读在约 `:7742`**，不是这里原来写的 `:7602`）⇒ **本指针已结清，⛔ 别再派一次**。
        //      （原文：「⚠️ 同一句话在 `Editor/MainMenuScene.cs` 也有一份 ⇒ 调度台要把它一起改掉」。）
        //   ⛔ **后端那条云脚本 `0x401` 具体重置了什么 —— 本地查不到**（反编译全域 + `dump.cs` + `script.json`
        //      三处都只有上面那 3 行）⇒ **不编**；我们清的就是「客户端侧看得见的那一族」（见 `ResetDailyProgress`）。
        //
        // ⚠️ **重置哪些量**（照 `SkullsCount` / `DailyLoginChallenge` 那族**现有的**字段，⛔ 别扩大范围）：
        //   只有「**当日**」那一族 —— `SkullsCount`（`_skullsCount`/`_skullsState`）· 三条每日任务（`_daily[]`）·
        //   `DailyLoginChallenge` 的**当天领取态**（`_loginState`）。
        //   ⛔ **周常 / 连登 / 每日奖励抽屉 / 商店 / Inbox 一律不动**（各有各的周期，不在这一族里）。
        //   ⛔ **`_loginDay` 也不动** —— 那是 7 天周期里的第几天，不是「当日进度」。
        //   ⛔ **别把这个重置接到 `_skullsCount` 的累加入口上**（A375 已定：**唯一入口** = `OnBattleEnd`）。

        /// <summary>「下一次刷新的时刻」（**本机时间**）—— 照原版 `DailyLoginChallenge.GetNextUpdate()`
        /// 那句 `lastUpdate.Date + 1 天` 的形状算出来、**存起来**的值。
        /// <para>🔴 **2026-10-13（A429）就地订正（铁律 5）**：这里原来写着「⚠️ 内存里存**是有意的**：本类整体就是
        /// 「纯内存 + 可复现」（见类头），别偷偷加 `PlayerPrefs`/写盘 —— **那会让自检碰玩家的真存档**
        /// （同 `CollectionScene` 用临时存档那条纪律）」。**那条顾虑现在不存在了** —— 用户 2026-10-13 裁定
        /// 「**落盘（独立文件）**」，而落盘这件事**两件一起**解决掉了原来那个理由：
        /// ① **独立文件**（`persistentDataPath/WarpforgeDaily/daily.json`，跟 `ReplayStore` 一条路子）
        ///    —— ⛔ 不是 `PlayerPrefs`（那个在 Windows 上写注册表、而且全工程共用一个键空间，坏了会连累别人）；
        /// ② **自检那扇门是关着的**（`StoreOn`）：`-batchmode` 下一律**不碰真档**，
        ///    自检要走盘上那一份必须**显式注入**临时目录（`OverrideDir` = 「一律走临时档」的落点）
        ///    + `ReloadForTest()`。⇒ 所谓「自检会碰玩家真存档」**结构上不成立了**（不靠每个宿主自觉）。
        /// ⇒ 因此**此处确实会落盘**：`_nextReset` / `_nextResetSet` 的每一个写入点都跟一句 `Persist`
        /// （出厂不在其内 —— 出厂那一刻还没有任何「当日进度」可存；跨天重排与自检口都落）。
        /// 详细形状 → 本节末尾「每日状态的**落盘**」那一段。</para></summary>
        static System.DateTime _nextReset;
        static bool _nextResetSet;

        /// <summary>真的重置了几次（自检断它**带电** —— 差分判据，别拿期望值自证）。</summary>
        public static int DailyResetCount { get; private set; }
        /// <summary>「进壳/回主菜单那一拍」**判了几次**（跨天的判据不是它、它是**接线**判据：
        /// `MainMenuRuntime.Build()` 里那一句被删掉之后，这条计数就不涨了）。</summary>
        public static int DailyResetChecks { get; private set; }
        /// <summary>最近一次真重置时**打出去的那句话**（`null` = 一次都没重置过）。
        /// 存在的理由 = 「不许静默失败」这条红线要**可断言**：跨天却没出声 ⇒ 这里是空的。</summary>
        public static string LastResetMessage { get; private set; }

        /// <summary>下次刷新的时刻（本机时间）—— 自检读它做差分/造态用。</summary>
        public static System.DateTime NextResetAt { get { EnsureNextReset(System.DateTime.Now); return _nextReset; } }

        /// <summary>自检用：把「下一次刷新的时刻」**定死**。调成**过去** = 造出「跨天」那一态；
        /// 调成**将来** = 造出「同一天」那一态（幂等那条要它）。⛔ 运行时别调。</summary>
        public static void ForceNextResetForTest(System.DateTime when)
        {
            EnsureNextReset(System.DateTime.Now);
            _nextReset = when;
        }

        static void EnsureNextReset(System.DateTime now)
        {
            if (_nextResetSet) return;
            // `lastUpdate.Date + 1 天`：**出厂就是「今天这一档」**（`lastUpdate` 取当天）⇒ 下一次 = 明天 0 点。
            _nextReset = now.Date.AddDays(1);
            _nextResetSet = true;
        }

        /// <summary>进壳 / 回主菜单那一拍：**跨天了就换一份新的 mission persistence**（进度清空）。
        /// <para>🔴 **幂等**：同一天进多少次都只重置一次 —— 判据是**存的「下一次刷新时刻」**，不是「上次重置的日期串」
        /// （形状 ② 与 `MissionResetSignal` 那条「到点才动」同构）。</para>
        /// <para>🔴 **出声**：真重置了打一行 `[Daily]`；没跨天时**不刷屏**（但 `DailyResetChecks` 照涨 ⇒ 接线坏了看得见）。</para>
        /// <para>**改坏法**：① 删掉 `MainMenuRuntime.Build()` 里那一句 ⇒ `DailyResetChecks` 不涨
        /// （`Editor/MainMenuScene.cs` 的「接线」那条红）；② 删掉 `if (now &lt; _nextReset) return false;` ⇒
        /// 每进一次壳都重置一次（「同一天只重置一次」那条红）；③ 不重算 `_nextReset` ⇒ 同上。
        /// 🆕 ④（A429）删掉末尾那句 `Persist("跨天重置")` ⇒ 「重启后还在 / 跨天重启恰好重置一次」那几条红
        /// （重排出来的「下一次刷新时刻」没落盘 ⇒ 重启后又按出厂算，跨的那一天**被跳过**）。</para></summary>
        public static bool TryDailyReset()
        {
            DailyResetChecks++;
            var now = System.DateTime.Now;            // ⚠️ **本机时间**（我们挑的，见本节开头那一大段）
            EnsureNextReset(now);
            // 🆕 **A815（用户 2026-10-15 拍板）**：**进壳那一拍记一次登录**（连登天数那一格）。
            //    放在**这一行**（早退之前）而不是重置那一支里 —— 连登是**每一天**都记，与「跨没跨天」无关；
            //    它自己按「最近记过的那一天」判同一天（幂等，`RecordLogin` 里那句 `gap == 0` 早退）。
            //    ⚠️ 它**不改本方法的返回值**（上面那句 doc 写的三条改坏法逐条照旧成立）。
            RecordLogin(now);
            if (now < _nextReset) return false;       // 没跨天 ⇒ 什么都不做（幂等）
            ResetDailyProgress();                     // 跨天 ⇒ 换一份新的 mission persistence
            _nextReset = now.Date.AddDays(1);         // 重算「下一次刷新的时刻」（原版那一拍之后也重排）
            DailyResetCount++;
            LastResetMessage = $"跨天（本机时间 {now:yyyy-MM-dd HH:mm}）⇒ **换了一份新的 mission persistence**："
                + "三条每日任务的进度与领取态 · 每日骷髅计数 · 登录卡的当天领取态 —— **都清空了**。"
                + "⚠️ 这一拍原版是**服务器日界**下发的（`MissionResetSignal` 全客户端只登记不发，发点在后端云脚本 `0x401`，"
                + "它具体重置了什么**本地查不到**）—— 我们**没有服务器时间**，判据用的是**本机时间**（= 我们挑的）。";
            Debug.Log("[Daily] " + LastResetMessage);
            Persist("跨天重置");                       // 🆕 A429：清完立刻落盘（不然重启一次就白清了）
            return true;
        }

        /// <summary>「换一份新的 mission persistence」（原版 `MissionEvent.DisableChallenge/InitializeChallenge`
        /// → `MissionChallenge.DisableChallenge/Initialize` 那一句的我们这一侧）—— **只清【当日】那一族**
        /// （范围与理由见本节开头那段；⛔ 周常/连登/奖励抽屉不在内）。</summary>
        static void ResetDailyProgress()
        {
            for (int i = 0; i < _daily.Length; i++) { _daily[i].Progress = 0; _daily[i].St = State.InProgress; }
            // 原版**唯一**登记了 `MissionResetSignal` 的就是它（`SkullsCount.OnReset`）⇒ 计数归零、回到未领取。
            _skullsCount = 0;
            _skullsState = State.InProgress;
            // `DailyLoginChallenge` 的**当天领取态**（⛔ `_loginDay` 不动 —— 那是 7 天周期里的第几天）。
            _loginState = State.InProgress;
        }

        // ============================================================ 每日状态的**落盘**（A429）
        //
        // 🔴 **为什么要落盘**：原版这一族**跨进程 / 跨设备都由服务端兜** ——
        //    `DailyLoginChallenge.GetNextUpdate()` 那句 `lastUpdate` 是**从服务端下发的挑战数据里取的**
        //    （`d:/2/tools/decomp_full/DailyLoginChallenge__GetNextUpdate.c:38-72`：`param_2[4]` 那个 JToken →
        //     `JToken.ToObject<long>` → `MillisToDateTime`；取不到才退 `TimeHelper.ServerDateTime`），
        //    而 `OnInitialize` / `<Execute>b__3_0` 那种「新的一天自己 +1 / 断签重置」也全建在那份
        //    `customData['lastUpdate']` 上。⇒ 原版**杀掉进程再开，当日进度还在、日界照着昨天那份算**。
        //    🔴 **我们这一侧（2026-10-13 之前）**：整族是**纯内存**（`_nextResetSet` 是进程内静态）
        //    ⇒ ① 重启 = 全部打回出厂（玩家看到的进度**倒退**）；
        //       ② **「关着过了一夜再开」那一档永远不触发重置**（该清的日进度不清 —— 那次重启会把
        //          `_nextReset` 重算成「明天 0 点」，于是那一天**被整个跳过**）。
        //    ⚠️ 这一句「原版跨重启长什么样」**是从反编译推出来的，不是实拍**（服务器已关、CCD 不可达，
        //       与 `资料/真Play待验清单.md` 那一族同性质）—— 如实标注，⛔ 别当它验过。
        //
        // 🔴 **用户 2026-10-13 裁定：落盘（独立文件）**。做法 = **跟回放一条路子**
        //    （`Battle/ReplayStore.cs`：`Application.persistentDataPath` 下一个自己的目录 + 一个给自检用的
        //    `OverrideDir`；⛔ **别自创**）。写盘那一拍照 `RuleEngine/Data/DeckStore.cs:SaveAll` 的
        //    「先写 `.tmp` 再换名」（写一半崩了不至于把旧档毁掉），⛔ **不是 `PlayerPrefs`**。
        //
        // 🔴 **原来那句「别偷偷加 `PlayerPrefs`/写盘」为什么不成立了**（那一句已经订正，
        //    见上面 `_nextReset` 的 doc）：它真正的顾虑只有一条 ——「**自检会碰玩家的真存档**」。
        //    现在两件一起解决：① **独立文件**（不是注册表、不是全工程共用的 `PlayerPrefs` 键空间，
        //    坏了只坏这一族）；② **门是关着的**（下面 `StoreOn`）：**只有真在一局里**
        //    （`Application.isPlaying && !Application.isBatchMode`）才读写玩家真档；
        //    **11 条自检全跑在 `-batchmode`**（`工具/_run_8_checks.sh` 那一行就是 `-batchmode -quit`）
        //    ⇒ 不注入临时档时**一个字节都不碰**（手调入口 `ShellScene.Play` 会 `EnterPlaymode`，
        //    但按 `CLAUDE.md` §二 的跑法它同样带 `-batchmode` ⇒ 仍然关着）。
        //    ⇒ **「自检一律走临时档、绝不碰玩家真存档」是【结构上】成立的**，
        //      不靠每个自检宿主自觉去设 `OverrideDir`（现读 `grep -rl "DailyData\."` = **17 个 `.cs`**：
        //      **4 个自检宿主**（`BattleScene` / `CollectionScene` / `MainMenuScene` / `RewardsScene`）
        //      + 12 个运行时文件 ⇒ 靠自觉一定会漏几个）。
        //
        // ⚠️ **落盘哪些量 = 重置清哪些量 + 「下一次刷新时刻」本身**（⛔ 别扩大范围，理由同上面那一段）：
        //    `_daily[]` 的 `Progress`/`St`（外加**被重摇过那一行的内容**，见下）· `_skullsCount`/`_skullsState`
        //    · `_loginState` · `_nextReset`/`_nextResetSet`
        //    · 🆕 **A815：`_streakDays`/`_streakDayTicks`（本地累计连登）** —— 它**天然是跨天的**
        //      （「跨天 +1 / 断天归 1」两半都要拿上一次记过的那一天来判）⇒ **不落盘就永远只有 1 天**，
        //      这条与「重置清哪些量」并列（它也**不在** `ResetDailyProgress()` 的清空范围内：**跨天不归零**，
        //      归零的是断天 —— 那一拍在 `RecordLogin` 里自己判）。
        //      ⚠️ **别与下面那个「连登」看混**：下面说的是**连登弹窗那条奖格轨**（`_streakClaimed` 一族，
        //      仍在内存里）；这里说的是**「连着登了几天」这个计数**，两个是不同的东西（见本节 `StreakLoggedDays` 的注释）。
        //    ⛔ **不落盘**：`_loginDay`（7 天周期的第几天，本来就不在重置范围内）· 周常 / **连登弹窗的奖格轨**
        //    / 每日奖励抽屉 / 商店（各有各的周期）· `DailyResetCount`/`DailyResetChecks`/`LastResetMessage`（进程内诊断量，
        //    自检拿它们做**差分**基线）· `RerollCount`/`_rerollRound`（进程内游标）。
        //    ⛔ 也**不动 `Wallet`**（那是记账，不是「当日进度」）。
        //
        // ⚠️ **`RerollDaily` 那一行要连【内容】一起存**：它是**就地换掉** `Desc`/`Target`/`RewardArt`/`RewardText`
        //    （原版这一步是服务端换一条新的）⇒ 只存进度的话，重启后那一行会**文案退回出厂、进度却是重摇后的**
        //    （自相矛盾）。做法 = 存内容 **+ 「这一行被摇过」的判据**，读回时**只对被摇过的行**覆盖内容
        //    ⇒ **没摇过的行永远跟代码里的出厂内容走**（改了 `_daily` 的文案不会被旧档静默盖住 —— 本仓踩过那族坑）。
        //    「摇过没有」是**现算**的（跟 `_dailyFactory` 比），不另立标志位 ⇒ 不会与内容脱节。
        //
        // ⚠️ **读盘的唯一入口 = 本类的静态构造函数**（下面 `static DailyData()`）—— C# 保证它在本类**任何成员
        //    被碰到之前**跑完 ⇒ 不必在每个读口前面各写一句 `EnsureLoaded()`（那样迟早漏一个）。要重读只有
        //    `ReloadForTest()`。

        /// <summary>落盘目录名（`persistentDataPath` 下，跟 `ReplayStore.DirName` 同理）。</summary>
        public const string StoreDirName = "WarpforgeDaily";
        /// <summary>落盘文件名。⚠️ **文件名里不带路径分隔符**（`ReplayStore` 那条纪律）。</summary>
        public const string StoreFileName = "daily.json";

        /// <summary>自检用的**目录改写口**：设了就用它，不设就走玩家目录 —— 与 `ReplayStore.OverrideDir`
        /// / `DeckStore.OverridePath` 同一套路、同一个理由（**自检不该动玩家的真存档**）。
        /// ⚠️ 自检**一律**要设它（见 `StoreOn`）；用之前先 `DeleteStoreForTest()` 清掉上一趟的残留。</summary>
        public static string OverrideDir;

        static string StoreDir
        {
            get
            {
                return string.IsNullOrEmpty(OverrideDir)
                     ? System.IO.Path.Combine(Application.persistentDataPath, StoreDirName)
                     : OverrideDir;
            }
        }

        /// <summary>存档文件的**完整路径**（日志里要打出来 —— 编辑器里它在 `AppData` 深处，不好找；
        /// 同 `ReplayStore` 那条纪律）。</summary>
        public static string StorePath { get { return System.IO.Path.Combine(StoreDir, StoreFileName); } }

        /// <summary>🔴 **门：这一趟允许碰盘吗。**
        /// <para>① `OverrideDir` 设了 ⇒ **允许**（自检注入的临时档 —— 这就是「**自检一律走临时档**」的落点）。</para>
        /// <para>② 否则**只有真在一局里**才允许（`Application.isPlaying &amp;&amp; !Application.isBatchMode`
        /// = editor 里按 Play / 打出来的 player）。**11 条自检全在 `-batchmode`**（含会 `EnterPlaymode`
        /// 的 `ShellScene.Play`）⇒ **一律落在这条之外**（= 「**绝不碰玩家真存档**」的落点，结构上保证）。</para></summary>
        static bool StoreOn
        {
            get { return !string.IsNullOrEmpty(OverrideDir) || (Application.isPlaying && !Application.isBatchMode); }
        }

        /// <summary>最近一次落盘去的文件（`null` = 本进程一次都没落过）。自检读它做「真的写下去了」的判据。</summary>
        public static string LastStorePath { get; private set; }
        /// <summary>最近一次落盘的字数（📏 别拍脑袋估 —— 这一份很小，整数百字节）。</summary>
        public static long LastStoreBytes { get; private set; }

        /// <summary>盘上那一份的**一行任务**（`desc/target/reward*` 只在 `rerolled` 为真时作数）。</summary>
        [System.Serializable]
        class RowDto
        {
            public int progress;
            public int st;              // 0/1/2 = `State`
            public bool rerolled;       // 这一行被**重摇过** ⇒ 下面四格才覆盖代码里的出厂内容
            public string desc;
            public int target;
            public string rewardArt;
            public string rewardText;
        }

        /// <summary>盘上那一份（**只放要落盘的那几个量**；字段名就是 JSON 键 ⇒ 改名 = 换版本）。</summary>
        [System.Serializable]
        class StoreDto
        {
            /// <summary>格式版本。**将来改结构就 +1**；读到不认识的版本 ⇒ **出声 + 不读它**（按出厂值走），
            /// ⛔ **别硬解** —— 缺字段会被 `JsonUtility` 静默读成 0，那是「拿默认值冒充真值」（`ReplayStore.Load` 同一条口径）。
            /// ⚠️ 拿 `new StoreDto().version` 比，⛔ 别写死一个数（`ReplayStore` 那边踩过）。</summary>
            public int version = 1;
            /// <summary>「下一次刷新的时刻」（`DateTime.Ticks`，**本机时间**）。</summary>
            public long nextResetTicks;
            /// <summary>这一份存过 `_nextReset` 没有（出厂那一刻没存过 ⇒ `false`）。</summary>
            public bool nextSet;
            public RowDto[] rows;
            public int skullsCount;
            public int skullsState;
            public int loginState;
            /// <summary>🆕 **A815：本地累计连登天数**（与 `streakDayTicks` 一起落盘 —— 缺哪一半都读不出「跨没跨天」）。
            /// <para>🔴 **为什么 `version` 不 +1**（铁律 5·c）：这两个字段是**纯增量**的，而且 `0` 有**确定含义**
            /// = 「本机从没记过」（正是出厂态）⇒ 老档读进来 `streakDayTicks == 0` ⇒ 下一次进壳照「第 1 天」走，
            /// **不是「拿默认值冒充真值」**。反过来把 `version` 抬到 2 会把玩家**已有的当日任务进度整份作废**
            /// （`EnsureLoaded` 对不认识版本的处理是「按出厂值走」，见那一段）⇒ 那是**静默破坏**，不做。</para></summary>
            public int streakDays;
            /// <summary>🆕 A815：最近一次记过的那一天（`.Date.Ticks`，本机时间；`0` = 从没记过）。</summary>
            public long streakDayTicks;
            /// <summary>🆕 **2026-10-25（执行代理 `X2`）**：**累计登录天数**（成就 `Ach_17` = `Log in counter` 的计数源）。
            /// <para>与 <see cref="streakDays"/> 的区别：那个是**连续**（断签归 1），这个是**累计**（只增不减）。
            /// 判据 = 成就定义 SO 的 `ConstantChallenge` + 阈值 `30/60/90/180/365`（英文 `Log in {0} days`，
            /// ⚠️ 中文与 `Ach_18` 撞同一句 —— 判据 §F3，别按中文分）。</para>
            /// <para>⚠️ **口径 = 我们挑的**：原版这一拍是**服务端**算的（`ConstantChallenge__UpdateProgress.c`
            /// 只有一个 `int` 入参，抬升点的方法体没解出来 —— 判据 §N5）⇒ 我们按「**每个自然日 +1**」
            /// （与 <see cref="streakDays"/> 同一个日界口径），**如实标为「我们挑的」**。</para>
            /// <para>同 `streakDays`：`version` **不 +1**（`0` 有确定含义 = 本机从没记过）。</para></summary>
            public int totalLoginDays;
            /// <summary>🆕 **2026-10-25（N17）**：**周常**那一格（进度 / 三态）。之前**根本没进存档**
            /// ⇒ 周常既不涨、重启也不保持（N17 分诊挑出的 P0 缺陷之一，判据 = `MissionEvent.GetBaseSave` /
            /// `UpdateSaveGame`：原版每一份任务进度都在事件存档里）。
            /// <para>`weeklySet` = 「这一份存过没有」—— 同 `nextSet` 的形状：`0` 是**合法进度**，
            /// 不能拿它当「没存过」⇒ 必须另有一个标志位（⛔ 别学「用 0 当哨兵」那种写法）。</para></summary>
            public bool weeklySet;
            public int weeklyProgress;
            public int weeklyState;
        }

        static bool _loaded;          // 本进程读过盘了没有（**读失败也算读过** —— 不然每次进壳都重试、刷屏）
                                      // ⚠️ 这条依赖「进 Play 会重载 domain」（现读 `ProjectSettings/EditorSettings.asset`
                                      //    `m_EnterPlayModeOptionsEnabled: 0` = 默认、快进 Play 没开）—— 若将来开了
                                      //    「不重载 domain」，**编辑期**那一趟留下的 `_loaded=true` 会带进 Play ⇒ 要改成按 `StoreOn` 重判。
        static bool _storeOffSaid;    // 「门关着」那句话只出声一次

        /// <summary>🔴 **本类任何成员被碰到之前，它一定先跑完**（C# 的静态构造函数语义）——
        /// 这就是**读盘的唯一入口**（理由见上面那段）。⚠️ 它的整个身子都在 `EnsureLoaded()` 里、
        /// 而那一支**自己吞掉所有异常** ⇒ 不会把本类变成一碰就炸的 `TypeInitializationException`。</summary>
        static DailyData() { InitializeHandler(); }

        /// <summary>读一次盘（**只能读一次**；要重读走 `ReloadForTest()`）。
        /// ⚠️ 盘上没有 / 读不出 / 版本不认识 ⇒ 都**按出厂值走**，并且**一律出声**（红线：不许静默失败
        /// —— 尤其「以为进度还在、其实没了」这种）。</summary>
        static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;                       // ⚠️ **先置位**（读失败也不重试）
            if (!StoreOn)
            {
                if (!_storeOffSaid)
                {
                    _storeOffSaid = true;
                    Debug.Log("[Daily] 落盘**关着**（没设 `OverrideDir`，且这一趟不在真的一局里）⇒ 本进程"
                            + "**一个字节都不碰玩家真档**，当日状态只活在内存里。⚠️ 自检要走盘上那一份："
                            + "先 `DailyData.OverrideDir = <临时目录>` 再 `DeleteStoreForTest()` + `ReloadForTest()`。");
                }
                return;
            }
            var path = StorePath;
            // 🆕 2026-10-25（执行代理 R4 · 存档 Step 4「双写期」过渡读法）：**先中央档、再旧档**。
            // `ReadThroughLegacy` 内部判 `SaveCenter.StoreOn` ⇒ 本进程落盘关着时它只读旧档那一份，
            // 不会读到玩家真中央档（上面的 `if (!StoreOn)` 也已经先返回了）。⛔ 别删旧读。
            string text = SaveCenter.ReadThroughLegacy(SaveKeys.DailyCounters, path);
            if (text == null)
            {
                Debug.Log("[Daily] 还没有存档（`" + path + "`）⇒ 按出厂值走");   // 第一次跑
                return;
            }
            try
            {
                // 🆕 **2026-10-28（`B93_E10` · 账 `:500`）**：老格式迁移 —— 原版 `MissionPersistence.OnBeforeRead`
                //   在**反序列化之前**把 `challenges` 从「单个对象」包成一元数组；我们这里同一条路（见 `OnBeforeRead`）。
                text = OnBeforeRead(text);
                var dto = JsonUtility.FromJson<StoreDto>(text);
                if (dto == null)
                {
                    Debug.LogWarning("[Daily] 存档解不出内容（`" + path + "`）⇒ 按出厂值走，"
                                   + "下一拍落盘会**盖掉**它");
                    return;
                }
                if (dto.version != new StoreDto().version)
                {
                    Debug.LogWarning("[Daily] 存档版本 " + dto.version + " 不认识（本代码只认 "
                                   + new StoreDto().version + "）⇒ 按出厂值走，下一拍落盘会**盖掉**它"
                                   + "（`" + path + "`）");
                    return;
                }
                ApplyDto(dto);
                Debug.Log("[Daily] 已从 `" + path + "` 读回当日状态：" + BriefState());
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Daily] 读盘失败：" + e.Message + "（`" + path + "`）⇒ **本进程按出厂值走**"
                             + "（红线：不许静默失败 —— 这一句就是告诉人「进度看起来没了」的原因）");
            }
        }

        /// <summary>把盘上那一份盖到内存上（**只覆盖落盘的那几个量**）。</summary>
        static void ApplyDto(StoreDto dto)
        {
            if (dto.nextSet && dto.nextResetTicks > 0 && dto.nextResetTicks <= System.DateTime.MaxValue.Ticks)
            {
                _nextReset = new System.DateTime(dto.nextResetTicks, System.DateTimeKind.Local);
                _nextResetSet = true;
            }
            if (dto.rows != null && dto.rows.Length == _daily.Length)
            {
                for (int i = 0; i < _daily.Length; i++)
                {
                    var r = dto.rows[i];
                    if (r == null) continue;
                    if (r.rerolled)
                    {
                        // 只有**被重摇过**的那一行才吃盘上的内容（没摇过的行跟代码里的出厂内容走）
                        if (!string.IsNullOrEmpty(r.desc)) _daily[i].Desc = r.desc;
                        if (r.target > 0) _daily[i].Target = r.target;
                        if (r.rewardArt != null) _daily[i].RewardArt = r.rewardArt;
                        if (r.rewardText != null) _daily[i].RewardText = r.rewardText;
                        // 🔴 **2026-10-18（Rewards 四条红 #4）**：盘上的重摇行**没有 `Term`/`Arg` 列**
                        //   ⇒ 这里必须**清掉**出厂那两个，否则 `DailyDesc` 会拿旧任务的官方键覆盖盘上的新描述
                        //   （重启后「文案退回出厂、进度却是重摇后的」那一类残留）。
                        //   ⚠️ 将来的重摇池若带 Term，要先把 term/arg 加进 `RowDto` 再存这里。
                        _daily[i].Term = null;
                        _daily[i].Arg = 0;
                    }
                    _daily[i].Progress = Mathf.Clamp(r.progress, 0, _daily[i].Target);   // 夹到**这一行真正的**目标值上
                    _daily[i].St = (State)Mathf.Clamp(r.st, (int)State.InProgress, (int)State.Claimed);
                }
            }
            else if (dto.rows != null)
            {
                Debug.LogWarning("[Daily] 存档里的任务条数（" + dto.rows.Length + "）与当前代码（" + _daily.Length
                               + "）对不上 ⇒ 任务进度这一块**按出厂值走**（骷髅 / 登录那份照读）");
            }
            _skullsCount = Mathf.Max(0, dto.skullsCount);
            _skullsState = (State)Mathf.Clamp(dto.skullsState, (int)State.InProgress, (int)State.Claimed);
            _loginState = (State)Mathf.Clamp(dto.loginState, (int)State.InProgress, (int)State.Claimed);
            // 🆕 A815：连登那一格。**两半都要有效才吃**（只有天数、没有「记到哪一天」⇒ 判不出跨天/断天 ⇒ 宁可当没记过）
            if (dto.streakDays > 0 && dto.streakDayTicks > 0
                && dto.streakDayTicks <= System.DateTime.MaxValue.Ticks)
            {
                _streakDays = dto.streakDays;
                _streakDayTicks = dto.streakDayTicks;
            }
            // 🆕 2026-10-25（`X2`）：累计登录天数（与连登那一格**互不依赖** —— 它不是「两半都要有效」那一族）
            _totalLoginDays = Mathf.Max(0, dto.totalLoginDays);
            // 🆕 **2026-10-25（N17）**：周常进度/态（`weeklySet` 才吃 —— 见 `StoreDto` 那条注释）
            if (dto.weeklySet)
            {
                _weeklyProgress = Mathf.Clamp(dto.weeklyProgress, 0, WeeklyTarget);
                _weeklyState = (State)Mathf.Clamp(dto.weeklyState, (int)State.InProgress, (int)State.Claimed);
            }
        }

        /// <summary>把内存里那一份整理成要写下去的形状。</summary>
        static StoreDto MakeDto()
        {
            EnsureNextReset(System.DateTime.Now);      // 落盘的一定是**完整的一份**（出厂那一刻还没算过就现在算）
            var dto = new StoreDto();
            dto.nextResetTicks = _nextReset.Ticks;
            dto.nextSet = _nextResetSet;
            dto.rows = new RowDto[_daily.Length];
            for (int i = 0; i < _daily.Length; i++)
            {
                var t = _daily[i];
                var f = _dailyFactory[i];
                // 「摇过没有」= **现算**（跟出厂快照逐格比）⇒ 不另立标志位、不会与内容脱节
                bool rerolled = t.Desc != f.Desc || t.Target != f.Target
                             || t.RewardArt != f.RewardArt || t.RewardText != f.RewardText;
                dto.rows[i] = new RowDto { progress = t.Progress, st = (int)t.St, rerolled = rerolled,
                                           desc = t.Desc, target = t.Target,
                                           rewardArt = t.RewardArt, rewardText = t.RewardText };
            }
            dto.skullsCount = _skullsCount;
            dto.skullsState = (int)_skullsState;
            dto.loginState = (int)_loginState;
            dto.streakDays = _streakDays;                // 🆕 A815
            dto.streakDayTicks = _streakDayTicks;        // 🆕 A815
            dto.totalLoginDays = _totalLoginDays;        // 🆕 2026-10-25（`X2`）
            // 🆕 **2026-10-25（N17）**：周常那一格（判据同 `StoreDto.weeklySet`）
            dto.weeklySet = true;
            dto.weeklyProgress = _weeklyProgress;
            dto.weeklyState = (int)_weeklyState;
            return dto;
        }

        /// <summary>把当前状态**写下去**。`why` = **谁写的**（日志里要一眼看出是哪一拍落的盘）。
        /// 🔴 **失败一律出声**（红线）：写不进去时玩家会以为进度存住了、其实没有。</summary>
        static void Persist(string why)
        {
            if (!StoreOn) return;                      // 门关着 ⇒ 一个字节都不碰（自检的常态）
            var path = StorePath;
            try
            {
                string json = JsonUtility.ToJson(MakeDto());       // 紧凑（这一份很小）
                // 🆕 2026-10-25（执行代理 R4 · 存档 Step 4「双写期」）：原来的「先写 `.tmp` → 删旧 → 换名」
                // 由 `SaveCenter.SaveThroughLegacy` 取代 —— 它内部是 `tmp → File.Replace`（NTFS 上原子），
                // 失败才退回两步法**并出声**（`SaveCenter.AtomicWrite` 的注释里写了）。
                // 🔴 **「原子写」这条性质必须保住**（切换单 C4 的硬要求）：
                //   · 落盘开着 ⇒ 中央档 `DailyCounters` + 旧档 `WarpforgeDaily/daily.json` 各一份；
                //   · 只注入了 `OverrideDir`（自检常态）⇒ 只写旧档，**内容与切换前逐字节同**，换名照样走原子路。
                string werr;
                if (!SaveCenter.SaveThroughLegacy(SaveKeys.DailyCounters, path, json, out werr))
                {
                    Debug.LogError("[Daily] 落盘失败（" + why + "）：" + werr + "（目录 " + StoreDir + "）⇒ "
                                 + "这一份状态**只在内存里**，重启会丢（红线：不许静默失败）");
                    return;
                }
                LastStorePath = path;
                LastStoreBytes = new System.Text.UTF8Encoding(false).GetByteCount(json);
                Debug.Log("[Daily] 落盘（" + why + "）：`" + path + "`（" + LastStoreBytes + " B）—— " + BriefState()
                        + "。⚠️ 编辑器里这个路径在 `AppData/LocalLow` 深处（`Application.persistentDataPath`），不好找。");
                // 🆕 **2026-10-26（B9 · P9 #431）**：≡ 原版 `MainMenuMission.UpdateSaveGame(save)`
                //   = `base.UpdateSaveGame(save)` **+ 尾调 `TrySchedulePopup()`**
                //   （判据 = `d:/2/tools/decomp_full/MainMenuMission__UpdateSaveGame.c:12` 那个尾调用 `[rax+0x5d8]`）
                //   ⇒ **每次任务存档更新之后重排一次自动弹窗**。我们这一侧「存档更新」= 本方法
                //   （`Persist` 是全部落盘点，见本文件「每日状态的落盘」那段）⇒ 挂在**写成功之后**。
                //   ⚠️ 排队体只在当前窗是 `MainMenuWindow` 时开火 ⇒ 战斗中/别的窗上它只是挂着（与原版同）。
                TrySchedulePopup();
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Daily] 落盘失败（" + why + "）：" + e.Message + "（目录 " + StoreDir + "）⇒ "
                             + "这一份状态**只在内存里**，重启会丢（红线：不许静默失败）");
            }
        }

        /// <summary>日志用的一句话速写（读回 / 落盘都打它，好让日志能直接跟断言对照）。</summary>
        static string BriefState()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < _daily.Length; i++)
                sb.Append(i > 0 ? " · " : "任务 ").Append(_daily[i].Progress).Append('/').Append(_daily[i].Target);
            sb.Append(" · 骷髅 x").Append(_skullsCount);
            sb.Append(" · 登录 ").Append(_loginState == State.Claimed ? "今天已领" : "今天没领");
            sb.Append(" · 连登 ").Append(_streakDays).Append(" 天")        // 🆕 A815
              .Append(_streakDayTicks == 0 ? "（本机从没记过）"
                                           : "（记到 " + new System.DateTime(_streakDayTicks).ToString("yyyy-MM-dd") + "）");
            sb.Append(" · 下一次刷新 ").Append(_nextResetSet ? _nextReset.ToString("yyyy-MM-dd HH:mm") : "（还没算过）");
            return sb.ToString();
        }

        /// <summary>把**落盘那一族**打回出厂值（`ReloadForTest` 用它模拟「进程刚起来、内存什么都没有」）。
        /// ⚠️ 它**不动**进程内的诊断量（`DailyResetCount` / `DailyResetChecks` / `LastResetMessage` /
        /// `RerollCount` / `Wallet`）—— 那些本来就不落盘，而自检拿它们做**差分**基线（抹掉反而看不清）。</summary>
        static void ResetPersistedMemory()
        {
            for (int i = 0; i < _daily.Length; i++)
            {
                _daily[i].Desc = _dailyFactory[i].Desc;
                _daily[i].Target = _dailyFactory[i].Target;
                _daily[i].RewardArt = _dailyFactory[i].RewardArt;
                _daily[i].RewardText = _dailyFactory[i].RewardText;
                _daily[i].Progress = _dailyFactory[i].Progress;
                _daily[i].St = _dailyFactory[i].St;
            }
            _skullsCount = 0;
            _skullsState = State.InProgress;
            _loginState = State.InProgress;
            // 🆕 **2026-10-25（N17）**：周常也属于「打回出厂值」那一族（它落盘了 ⇒ 模拟重启时必须一起清）
            _weeklyProgress = 13;                 // = 文件顶部那个「我们挑的」出厂值（⛔ 别在这里写第二个数）
            _weeklyState = State.InProgress;
            _nextReset = default(System.DateTime);
            _nextResetSet = false;
            _streakDays = 0;                      // 🆕 A815（它落盘 ⇒ 属于「打回出厂值」那一族）
            _streakDayTicks = 0L;
        }

        /// <summary>🔴 **自检用：模拟一次进程重启** —— 把「当日那一族」打回出厂值，**再从盘上读回来**
        /// （真重启就是这个样子：内存全丢 + 只剩盘上那一份）。
        /// <para>⚠️ 用之前**先设 `OverrideDir`**（不然门是关的，读了等于没读 —— 而这时候「没读」正是对的）。</para>
        /// <para>**改坏法**：把这一句里的 `_loaded = false` 去掉 ⇒ 读不回来 ⇒ 所有「重启后还在」的断言红；
        /// 把 `ResetPersistedMemory()` 去掉 ⇒ 模拟不出真重启（内存里的东西没清，断「重启后还在」会**假绿**）。</para></summary>
        public static void ReloadForTest()
        {
            ResetPersistedMemory();
            _loaded = false;
            EnsureLoaded();
        }

        /// <summary>自检用：把当前内存状态**立刻落盘**（= 模拟「上一个进程把状态写下去了」）。
        /// ⚠️ 自检造出来的态（`Force*ForTest` 那一族）**自己不落盘**（⛔ 别让自检口在真档上留痕）——
        /// 要落盘就在造完态之后显式调这一句。</summary>
        public static void SaveNowForTest() { Persist("自检 SaveNowForTest"); }

        /// <summary>自检用：**删掉存档文件**（让本节从「第一次跑、盘上什么都没有」那一态起手；
        /// 与 `ReplayStore.ResetForTest()` 同一件事）。
        /// 🔴 **没设 `OverrideDir` 时它【拒绝执行】** —— 那种情况下它删的就是**玩家的真档**（硬挡，不是靠自觉）。</summary>
        public static void DeleteStoreForTest()
        {
            if (string.IsNullOrEmpty(OverrideDir))
            {
                Debug.LogError("[Daily] 自检：**拒绝删档** —— 没设 `OverrideDir` 时删的就是玩家的真存档。"
                             + "先 `DailyData.OverrideDir = <临时目录>`。");
                return;
            }
            try
            {
                var p = StorePath;
                if (System.IO.File.Exists(p)) { System.IO.File.Delete(p); Debug.Log("[Daily] 自检：已删掉存档 `" + p + "`"); }
                if (System.IO.File.Exists(p + ".tmp")) System.IO.File.Delete(p + ".tmp");
                // 🆕 Step 4 双写期：中央档那一份也一起清 —— 否则「删了旧档再 `ReloadForTest`」会从中央档
                // 把上一趟的状态读回来（读口是先中央后旧档）。走 `SaveCenter.Delete`（**不真删**：改名到 `.del-*`）。
                if (SaveCenter.StoreOn && SaveCenter.Exists(SaveKeys.DailyCounters)) SaveCenter.Delete(SaveKeys.DailyCounters);
            }
            catch (System.Exception e) { Debug.LogError("[Daily] 自检：删存档失败：" + e.Message); }
        }

        /// <summary>🔴 **自检用：从【盘上】读回「骷髅计数」那一格**（⛔ 不是读内存）。
        /// <para>**为什么必须有个「读盘」的口**：断言若只看内存，那么「把 `Persist(...)` 那一句删掉」
        /// 这种改坏法**验不出来**（内存里当然是对的）—— 这个口专门堵那个洞
        /// （= 本仓「**灭自证 / 改哪两处会一起变绿**」那一族）。</para>
        /// 返回 **`int.MinValue`** = 盘上没有档 / 读不出 / 版本不认识（⛔ **别当 0 用**；
        /// 自检拿它判红，用法见 `资料/普查产出_1013/WA429_每日重置落盘.md` §五）。</summary>
        public static int StoredSkullsCountForTest() { return StoredIntForTest(s => s.skullsCount); }

        /// <summary>🆕 **A815 自检用：从【盘上】读回本地累计连登天数**（⛔ 不是读内存；`int.MinValue` 的含义同
        /// `StoredSkullsCountForTest`）。
        /// <para>**为什么非要一个「读盘」的口**：「天数来自本地记录」这句话，若断言只看内存，
        /// 那么「把 `RecordLogin` 末尾那句 `Persist(...)` 删掉」这种改坏法**验不出来**（内存里当然是对的）
        /// —— 这个口专堵那个洞（= 本仓「灭自证 / 改哪两处会一起变绿」那一族，与 A429 那三条同型）。</para>
        /// <para>⚠️ 老档（A815 之前落的盘）里没有这个键 ⇒ `JsonUtility` 给 `0` ⇒ 这里返回 **0**（= 「本机从没记过」，
        /// 是本字段**确定的**一个含义，不是「拿默认值冒充真值」，见 `StoreDto.streakDays`）。</para></summary>
        public static int StoredStreakDaysForTest() { return StoredIntForTest(s => s.streakDays); }

        /// <summary>自检用：从**盘上**读回第 `i` 条每日任务的进度（`int.MinValue` 的含义同上）。</summary>
        public static int StoredDailyProgressForTest(int i)
        {
            return StoredIntForTest(s =>
            {
                if (s.rows == null || s.rows.Length == 0) return int.MinValue;
                int k = Mathf.Clamp(i, 0, s.rows.Length - 1);
                return s.rows[k] == null ? int.MinValue : s.rows[k].progress;
            });
        }

        static int StoredIntForTest(System.Func<StoreDto, int> pick)
        {
            // ⚠️ **门关着就【连读都不读】** —— 「绝不碰玩家真存档」是对**读写两边**说的：
            //    未注入临时档时 `StorePath` 指的是**玩家的真档**，读它一样是碰它。
            if (!StoreOn) return int.MinValue;
            try
            {
                var p = StorePath;
                if (!System.IO.File.Exists(p)) return int.MinValue;
                var dto = JsonUtility.FromJson<StoreDto>(System.IO.File.ReadAllText(p));
                if (dto == null || dto.version != new StoreDto().version) return int.MinValue;
                return pick(dto);
            }
            catch (System.Exception e) { Debug.LogError("[Daily] 自检：读存档失败：" + e.Message); return int.MinValue; }
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
        /// ⚠️ 用**确定的数**而不是「运行到此刻的值」—— 断言「推进了多少」才不会被上一次运行污染。
        /// 🔴 **2026-10-17（`A2070③`/`N32`）就地订正（铁律 5）**：目标已改成 **45 / 15 / 15**，
        ///   旧基准 `52/4/1` 会让第 1 条变成 **`52/45`（进度 > 目标）**（`Advance` 的 `Min(Target, …)`
        ///   只在推进时夹，这里直接赋值不夹）⇒ 改成 **0 / 0 / 0**（= 新出厂值）。
        ///   原文（留痕）：「出厂的 task1 就是 10/10（已在 target）」—— 那是旧 `_daily`（500 伤害）时代的读数。
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
                _daily[i].Term = _dailyFactory[i].Term;                  // 🔴 2026-10-18（#4）：词条键也还原
                _daily[i].Arg = _dailyFactory[i].Arg;
            }
            _daily[0].Progress = 0; _daily[0].St = State.InProgress;    // /45
            _daily[1].Progress = 0; _daily[1].St = State.InProgress;    // /15
            _daily[2].Progress = 0; _daily[2].St = State.InProgress;    // /15
        }

        // ---- 自检用：把状态推到一个可断言的值（**别影响运行时默认**）----
        public static void ForceCollectableForTest()
        {
            _daily[0].St = State.Collectable;
            _daily[1].St = State.Collectable;
            _weeklyState = State.Collectable;
        } /* 🆕 2026-10-26（F3b）：**全量还原口**（三条每日 + 周常 + 骷髅 + 登录）。`ResetMissionsForTest` 只管三条每日 ⇒ 别的宿主拿它当「回到出厂」时，上一次留下的 `_weeklyState` / `_skullsState` / `_loginState` 会**泄漏**过来（最坑的是 `CanCollectLogin()` = `_loginState != Claimed` ⇒ 只要没被显式置成 `Claimed`，「一条可领都没有」这个态**根本造不出来**，红点断言恒 1）。⛔ 别改 `ResetMissionsForTest` 现有行为（别的宿主在用）；「回到出厂」一律用本口。⚠️ 它**不**把登录/骷髅置成可领 —— 出厂就是 `InProgress`（`_loginState`/`_skullsState` 的初值）。 */ public static void ResetAllStatesForTest() { _weeklyState = State.InProgress; _skullsState = State.InProgress; _loginState = State.InProgress; ResetMissionsForTest(); }

        // ============================================================ 领奖窗（= 原版 `RewardService.Collect` 的开窗那一半）
        //
        // 🆕 **2026-10-11（批次1 · W1 · A309）**：「领到奖 ⇒ 弹一扇全屏 `Reward Window`」这件事，
        // 在我们这一侧**只挂在一个出口上** —— `Wallet.Grant` 那一条路（全工程唯一发奖口，
        // 6 个调用点全在本文件）。判据 = `d:/2/tools/decomp_full/RewardService__Collect.c`：
        // `Collect(..., showAnimation: true, onCollected, …)` 在**发完奖之后**
        // `new RewardWindowContext(rewards, isPremiumLocked, onCollect: **null**, onClose: onCollected,
        //  …, isPreview: **0**)` → `WindowsManager.OpenWindow<RewardWindow>` —— 即**「Rewards claimed」那一态**
        // （没有 `Collect` 按钮），关窗时把 `onCollected` 发出去（我们拿它重建还开着的日常两扇窗）。
        // ⚠️ **2026-10-12（A313）就地订正（铁律 5）**：这里原来写着「**只接了两条**（每日奖励抽屉 ·
        // 连登奖格）」+ 一份「要做的四条」清单 —— **那四条已经接完了**（见下面四段 `Collect*` 里各一句
        // `ShowCollectedWindow`），原清单里引的 `:1457/:1538/…` 那六个行号也早已漂掉（A 表行号会漂，
        // 一律以现读为准）。同一句错在 `Editor/RewardsScene.cs` 也有一份，那边一并订正（铁律 5：同一句话
        // 被复制到别处的地方一起改）。
        // ✅ **现在接上的六条**（本文件内全部走 `ShowCollectedWindow`）：
        //   `CollectReward`（每日奖励抽屉）· `CollectStreak`（连登奖格）· `CollectDaily` · `CollectWeekly` ·
        //   `CollectSkulls` · `CollectLogin`（后四条 = A313 本批接的）。
        // 原版同样走这个出口的还有：开包（`ContainerService.OpenContainer`）/ 锻造 / 战役节点 /
        // 试用卡升级 … —— 那些的**宿主窗不在本文件**（也各不在同批各自写手的白名单里）⇒ 留待那些窗口的批次。
        // 🔴 **接上之后夹具必须同批改**（A313 的验收里写了）：`Editor/RewardsScene.cs` 里每一处
        // 「点 `Collect` **且真领到**」的调用点后面都得把那扇窗关掉 —— 领奖窗是**弹窗**，
        // 开着时它底下的窗被 `ToBackground()`、指针命中全归它（详见那边 `ClickCollectAndDismiss` 那段）。

        /// <summary>一条奖励（图名 + 数量）⇒ 弹 `Reward Window`。
        /// <para>🔴 **传进去的 `Id` 就是那张菜单图名** —— 日常线的奖励**没有服务端 item id**
        /// （我们的记账口签名是 `Wallet.Grant(string art, int n)`）。`RewardWindow.ArtOf` 为此补了一条兜底
        /// （id 认不出时再认「id 本身就是一张菜单图名」），见那边的注释。
        /// `Tier` 由**这一格的轨**决定：高级轨 = `TierPremium(10)`（= 原版 `RewardTier` 的取值）。</para>
        /// <para>⚠️ 它与下面那个**数组重载**是**同一条路**（这一条只是「一条奖励」的薄包装）——
        /// 别在两处各写一份开窗（本仓「两处写同一条规则 = 迟早不一致」）。</para></summary>
        static void ShowCollectedWindow(string art, int n, bool premium)
        {
            ShowCollectedWindow(new[]
            { new CampaignData.RewardSpec(art, n, premium ? CampaignData.TierPremium : CampaignData.TierBasic) });
        }

        /// <summary>**多条**奖励 ⇒ 弹**同一扇** `Reward Window`（🆕 **A313**：登录卡那两份就是一扇窗装两条）。
        /// <para>判据：原版 `RewardService.Collect` 的第一个形参本来就是**一个奖励列表**
        /// （`IReadOnlyList&lt;RewardInfo&gt; rewards`，`d:/2/tools/il2cpp_out/dump.cs:94273`），
        /// 而登录卡那个调用点 `PlayerDataManager.ProcessNewLoginResult` 走的就是它 ——
        /// 它并不会按格开好几扇窗。</para>
        /// <para>⚠️ 与 `Preview` 的分工**照原版**：一个格子里装多条时，原版把 `Preview` 挂到**抽屉**上
        /// （`DailyRewardDrawerController__Initialize.c:103-116` 那句 `if (1 &lt; rewards.Count)`）；
        /// 我们这一支是**发完奖之后的 Collect 态**（`isPreview: 0`、`OnCollect = null`）——
        /// 登录卡这两条是**已到手的**，不是「将要得到」。</para></summary>
        static void ShowCollectedWindow(CampaignData.RewardSpec[] rewards, bool showAnimation = true)
        {
            if (showAnimation) RewardWindow.ShowCollected(rewards, RefreshOpenDailyWindows); else RefreshOpenDailyWindows(rewards);   // 原版 RewardService.Collect:170（`RewardService__Collect.c`）showAnimation==false ⇒ 不开窗、仍调 onCollected
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

        // ============================================================ 🆕 2026-10-26（B9 · P9 / U11）
        // **本段是 U11 那 16 条里三件事的数据侧落点**（方法体判据全部亲读
        // `d:/2/tools/decomp_full/`，逐条标在各自的 summary 上）：
        //   ① 进主菜单自动弹窗 —— 原版 `MainMenuMission.TrySchedulePopup()` / `Dispose()`；
        //   ② 任务完成飘窗   —— 原版 `Missions.TryShowToastNotification(...)`；
        //   ③ 奖励预览       —— 原版 `RewardService.Preview(...)`（挂在抽屉 / 里程碑格上）。
        //
        // 🔴 **为什么落在 `DailyData`**：原版那三条的宿主（`MainMenuMission` / `Missions`）在我们这一侧
        //    没有对应类 —— 日常状态机**只有 `DailyData` 一份**（本文件头那段「单机口径」）。新造一个
        //    「任务事件」类只会变成第二份口径（铁律 6：两处写同一条规则 = 迟早不一致）。
        //
        // ⚠️ **不要把 `MenuActions` 当「我们发明的排队」** —— 它是 Q3（2026-10-26）照原版建的等价物
        //    （`Shell/LiveOpsHandler.cs` 的 `MenuActions` 类头有逐条对照）。

        /// <summary>≡ 原版 `MainMenuMission.menuAction`（偏移 **0x38**）—— 排进 `MenuActions` 的那条动作的句柄，
        /// 用来做**幂等**（已在队列里就不再排）与 `Dispose` 时撤销。
        /// <para>🔴 **两个句柄 = 两条 mission 实例**：原版 `MainMenuMission` 是**按任务各实例化一份**的
        /// （`DailyRewardPopup` / `DailyStreakWindow` 都是 `LiveOpsEventWindow&lt;MainMenuMission&gt;`，
        /// 而 `MainMenuMission` **没有子类** —— `dump.cs:60340` 只有这一个具体类），每份实例的
        /// `TrySchedulePopup` 各排自己那条 `Popup(12)`。⇒ 我们按两条轨各留一个句柄
        /// （每日奖励轨 / 连登轨），与「每实例一个 `menuAction`」一一对应。</para></summary>
        static MenuAction _popupActionDaily, _popupActionStreak;

        /// <summary>见 <see cref="TrySchedulePopup"/> 里那段 🔶 —— **编辑期默认不排队**；自检要验这条链时
        /// 显式把它打开（`DailyData.AutoPopupInEditor = true`），验完关掉。
        /// ⚠️ 它只管「自动排程」这一侧；`TrySchedulePopup()` 手调时也受它管。</summary>
        public static bool AutoPopupInEditor;

        /// <summary>「现在允许排自动动作吗」—— 运行时恒真；编辑期只有显式打开才真（理由见那段 🔶）。</summary>
        static bool SchedulingAllowed { get { return Application.isPlaying || AutoPopupInEditor; } }

        /// <summary>排过几次弹窗动作（**自检读它做差分** —— 别拿「一定排过」自证）。</summary>
        public static int PopupScheduleCount { get; private set; }
        /// <summary>排队体真的开出过几次窗（同上，差分判据）。</summary>
        public static int PopupOpenCount { get; private set; }
        /// <summary>现在队列里还挂着几条本类的弹窗动作（`0` / `1` / `2`）。</summary>
        public static int PendingPopupActions
        {
            get
            {
                int n = 0;
                if (MenuActions.Contains(_popupActionDaily)) n++;
                if (MenuActions.Contains(_popupActionStreak)) n++;
                return n;
            }
        }

        /// <summary>每日奖励轨（4 天）**有没有一格可领** —— 原版那一支的 `!challenges.All(p =&gt; !p.CanCollect())`。
        /// <para>判据链：`MainMenuMission__TrySchedulePopup.c:42` 的 `Enumerable.All(challenges, b__11_0)`，
        /// 谓词 `MainMenuMission.__c___TrySchedulePopup_b__11_0.c:8` = **`CanCollect(...) == 0`**
        /// ⇒ 「全部不可领」为真 ⇒ **不排**；为假（至少一条可领）⇒ 排。
        /// 我们这一侧的「挑战」= 每天那一格（`DailyRewardItemContainer.GetCurrentState` 的
        /// `normal == Unlocked` 那一态 = 可领），见 <see cref="RewardStateOf"/>。</para></summary>
        public static bool HasCollectableDailyReward
        {
            get
            {
                for (int d = 0; d < RewardDays; d++)
                    if (RewardStateOf(d, false) == RewardState.Unlocked) return true;
                return false;
            }
        }

        /// <summary>连登轨**那一格可领**（`StreakRewardUnlocked(StreakCollected)`）—— 与
        /// <see cref="HasCollectableDailyReward"/> 同一条判据形状（各自那条 mission 实例）。</summary>
        public static bool HasCollectableStreak { get { return StreakRewardUnlocked(StreakCollected); } }

        /// <summary>≡ 原版 `MainMenuMission.TrySchedulePopup()`（虚方法 Slot 74，被 `Initialize()` 与
        /// `UpdateSaveGame()` **尾调用**；`MainMenuMission__Initialize.c:11-12` / `__UpdateSaveGame.c:12`）。
        /// <para>**逐句对位**（`MainMenuMission__TrySchedulePopup.c`）：
        /// ① 门：`if (!challenges.All(p =&gt; !p.CanCollect()))`（判据见 <see cref="HasCollectableDailyReward"/>）；
        /// ② 幂等：句柄非空且**还在静态 `actions` 列表里** ⇒ 直接 return（`c:44-59` 那段
        ///    `List.Contains` 早退）；
        /// ③ 排队：`MenuActions.QueueAction(cb, 0,0, null, false,false,false, Array.Empty&lt;Type&gt;())`，
        ///    返回值存回句柄（`c:61-68`）。
        /// 🔴 **`Array.Empty&lt;Type&gt;()` 那个空 filter 不是「不筛」**：原版 `QueueAction` 内部把空表
        ///    **改写成 `new[]{ typeof(MainMenuWindow) }`**（disasm 0x18069E413-0x18069E487，见
        ///    `资料/日常_调用链_三窗.md` §一 A-1 第 5 行）⇒ 该动作**只在当前窗恰好是 `MainMenuWindow`
        ///    时开火**。我们这一侧显式传 `new[]{ typeof(MainMenuWindow) }`（本仓 `MenuActions` 没有那条改写，
        ///    传 null = 任何当前窗都行 —— 那会**在战斗里的任一一扇窗上弹日常窗**，与判据反）。
        /// ⚠️ **调用时机**：`MainMenuRuntime.Build()` 末尾（= 主菜单整棵树建完之后，原版「主菜单初始化完成」那一刻）。</para>
        /// <para>**改坏法**：① 删掉 `menuFilter` ⇒ 战斗里也会弹；② 删掉幂等那段 ⇒ 同一轮重建里堆两条动作
        /// （`PendingPopupActions` 会变成 4）；③ 把门去掉 ⇒ 一条可领都没有也弹（验收第 ②' 条）。</para></summary>
        public static bool TrySchedulePopup()
        {
            // = 原版 `Dispose()`（`MainMenuMission__Dispose.c`：`List.Remove(this.menuAction)`）在我们这一侧的
            //   **等价时点** —— 本方法只在「主菜单重建」这一拍被调，那一刻上一轮那份 mission 实例已经没了
            //   ⇒ 先撤旧句柄、再按新状态排（否则旧句柄会被 `Contains` 判成「已排过」而**永远不再弹**）。
            ReleasePopupActions();

            // 🔶 自己设计（非原版）：编辑期默认**只撤句柄、不排队**（`AutoPopupInEditor` 可显式打开） · 原版没有「编辑期」
            // 这个概念、判据为空；但**必须有这道闸** —— 自检宿主（`MainMenuScene.Run` / `ShellScene.Run`）在同一个进程里
            // 反复 `Build()` + 开关窗，而 `MenuActions.actions` 是**静态**的、跨节存活 ⇒ 不闸的话一条「等
            // `MainMenuWindow` 的动作」会在**后面某一节**突然开一扇奖励窗，把不相干的断言打红
            // （同族先例 = `MenuTutorialFlow.Attach` 的 `Application.isPlaying` 闸） · 调度台 2026-10-26
            if (!SchedulingAllowed) return false;

            bool any = false;
            if (HasCollectableDailyReward)
            {
                _popupActionDaily = MenuActions.QueueAction(
                    OpenDailyRewardPopupScheduled, ActionRule.DontBlock, 0, null, false, false, false,
                    new[] { typeof(MainMenuWindow) });
                any = true;
            }
            if (HasCollectableStreak)
            {
                _popupActionStreak = MenuActions.QueueAction(
                    OpenDailyStreakPopupScheduled, ActionRule.DontBlock, 0, null, false, false, false,
                    new[] { typeof(MainMenuWindow) });
                any = true;
            }
            if (any) PopupScheduleCount++;
            return any;
        }

        /// <summary>≡ 原版 `MainMenuMission.Dispose()`（`MainMenuMission__Dispose.c` 末句
        /// `System_Collections_Generic_List.Remove(actions, menuAction)`）—— **按句柄撤销**。
        /// 重复调用安全（`ReleaseAction` 就是 `List.Remove`）。</summary>
        public static void ReleasePopupActions()
        {
            if (_popupActionDaily != null) MenuActions.ReleaseAction(_popupActionDaily);
            if (_popupActionStreak != null) MenuActions.ReleaseAction(_popupActionStreak);
            _popupActionDaily = null;
            _popupActionStreak = null;
        }

        /// <summary>排队体 ≡ 原版 `MainMenuMission.&lt;TrySchedulePopup&gt;b__11_1()`
        /// （`MainMenuMission___TrySchedulePopup_b__11_1.c`）—— **先重检一次**（这次读裸字段，谓词
        /// `__c___TrySchedulePopup_b__11_2.c` = `canCollect == 0`），通过才
        /// `GetComponentReference(mission, 0xC = Popup)` + `WindowsManager.OpenWindow(ref, mission, 0, 0)`。
        /// <para>⚠️ **`0xC` 那一跳我们做不到、如实标注（铁律 3）**：`Popup` 槽存的是**远端 CCD 下发的
        /// GUID**（主菜单任务那几份 SO 不在 `assets_full` 里 —— 全树搜两张窗 prefab 的 GUID
        /// `3d3e5e00…` / `d67d1b71…` **0 命中**，只有 menus 包的容器表里有）⇒ 我们按**窗类直建**
        /// （`WindowsManager.OpenByRef` 用 prefab 根名做缓存键，等价于原版 `automaticallyLoadedWindows`
        /// 命中复用那一套）。</para></summary>
        static void OpenDailyRewardPopupScheduled()
        {
            if (!HasCollectableDailyReward) return;                    // ← 原版那句「再判一次」
            PopupOpenCount++;
            WindowsManager.OpenByRef("Daily Reward Popup",
                                     m => DailyRewardPopup.Create(m), false);   // closeAll = 原版第 3 实参 0
        }

        /// <summary>连登轨那条排队体（与上面同形；窗 = `Daily Streak Popup`）。</summary>
        static void OpenDailyStreakPopupScheduled()
        {
            if (!HasCollectableStreak) return;
            PopupOpenCount++;
            WindowsManager.OpenByRef("Daily Streak Popup",
                                     m => DailyStreakPopup.Create(m), false);
        }

        // ------------------------------------------------------------ ② 任务完成飘窗（#518）

        /// <summary>≡ 原版 `Missions.TryShowToastNotification(missions, missionChallengeProgress)`
        /// （`Missions__TryShowToastNotification.c` 逐句）：
        /// <code>
        /// var d = new DisplayClass { missions, progress };
        /// if (instance.&lt;bool@0x1b0&gt; == 0) ShowToastNotification(d.missions, d.progress, 0);   // 不在战斗 ⇒ 立刻弹
        /// else MenuActions.QueueAction(delegate { ShowToastNotification(d.missions, d.progress, 0); }, …); // 战斗中 ⇒ 排队
        /// </code>
        /// <para>🔴 **两支合成一条**：两支的差别只有「现在弹 / 回主菜单弹」，而我们的
        /// `MenuActions.QueueAction` + `MainMenuWindow` filter **在菜单里会当场 drain、在战斗里会等到
        /// 回主菜单**（判据 = 本仓 `MenuActions.CanTriggerNow` 的「有当前窗且类型在 filter 里」那两句）
        /// ⇒ 传 filter 的那一条**与两支观测等价**，不必再自己判「是不是在战斗」。
        /// ⚠️ 战斗里那一刻 `currentWindow` 到底是谁本地查不到（原版那个 `0x1b0` 字段名也没有产物可读）
        /// —— 这一条是**我们的落地**，写在报告里。</para>
        /// <para>触发点 = <see cref="OnBattleEnd"/>（原版 = `BattleManager__FinishedProcessingMatchResult`
        /// → `Missions.UpdateMissionProgress`，`grep -rl Missions__UpdateMissionProgress` 命中它）。</para></summary>
        static void TryShowMissionToast(int dailyIndex)
        {
            int idx = dailyIndex;
            // 与 `TrySchedulePopup` 同一道闸（理由见那段 🔶：静态 `MenuActions` 会跨自检小节存活）。
            // ⚠️ **出声** —— 红线是不许静默失败，所以「没排」这件事要留一行。
            if (!SchedulingAllowed)
            {
                Debug.Log("[Daily] 任务完成的飘窗**没有排**（编辑期默认不排自动动作；"
                        + "要验这条链先置 `DailyData.AutoPopupInEditor = true`）—— 任务 " + (idx + 1));
                return;
            }
            var a = MenuActions.QueueAction(() => ToastNotifications.ShowMission(idx),
                                            ActionRule.DontBlock, 0, null, false, false, false,
                                            new[] { typeof(MainMenuWindow) });
            // 🆕 **2026-10-26（B9 · P9 #518）**：留句柄只为**自检收尾能撤**（原版那一条是临时动作、
            //   没有撤销口 —— 我们多这一份纯粹是「别把动作漏进后续自检小节」）。
            if (a != null)
            {
                for (int i = _toastActions.Count - 1; i >= 0; i--)
                    if (!MenuActions.Contains(_toastActions[i])) _toastActions.RemoveAt(i);
                _toastActions.Add(a);
                MissionToastQueuedCount++;
            }
        }

        /// <summary>飘窗排过几条（自检差分判据；`ReleaseMissionToastActions()` **不会**把它减回去）。</summary>
        public static int MissionToastQueuedCount { get; private set; }

        /// <summary>还挂着的任务飘窗动作句柄（只为自检收尾 —— 见 `TryShowMissionToast` 末段）。</summary>
        static readonly System.Collections.Generic.List<MenuAction> _toastActions =
            new System.Collections.Generic.List<MenuAction>();

        /// <summary>撤掉所有还挂着的任务飘窗动作（**自检收尾用**）。重复调用安全。</summary>
        public static void ReleaseMissionToastActions()
        {
            for (int i = 0; i < _toastActions.Count; i++) MenuActions.ReleaseAction(_toastActions[i]);
            _toastActions.Clear();
        }

        // ------------------------------------------------------------ ③ 奖励预览（#426/#427/#495/#778）

        /// <summary>≡ 原版 `RewardService.Preview(rewards, …)` —— **「你将得到」那一态**。
        /// <para>判据：`RewardService__Preview.c` 与 `Collect` 共用同一个 `RewardWindowContext` ctor，
        /// 只差 `isPreview = 1`（全文 → `Shell/RewardWindow.cs` 的 `ShowPreview` doc）。
        /// 原版的**三个触发者**：抽屉（`DailyRewardDrawerController.Initialize` 里
        /// `if (1 &lt; rewards.Count)`）· 每日里程碑格（`MissionMilestoneStep.&lt;DisplayCheckmark&gt;b__0`）·
        /// 周常里程碑格（`WeeklyMissionMilestone.&lt;DisplayCheckmark&gt;b__0`）——后两个都是
        /// **同一个 lambda 的 ICF 折叠副本**（字节相同）。</para>
        /// <para>⛔ **不编奖励**：列表为空 ⇒ 出声、不开窗（红线：不许静默失败，也不许假装有奖励）。</para></summary>
        public static RewardWindow PreviewRewards(CampaignData.RewardSpec[] rewards, bool premiumLocked = false)
        {
            if (rewards == null || rewards.Length == 0)
            {
                Debug.LogWarning("[Daily] 奖励预览：**这一格没有奖励数据** ⇒ 不开窗"
                               + "（原版那些奖励由服务端下发；⛔ 不编一条假的顶上）");
                return null;
            }
            return RewardWindow.ShowPreview(rewards, RefreshOpenDailyWindows, premiumLocked);
        }

        /// <summary>登录卡的奖励列表（两格）—— `CollectLogin` 发的那**同一份**
        /// （`_loginRewardArt` / `_loginRewardCount`，⛔ 别在预览里另拼一张表）。
        /// 用于 `LoginBonusRewardDisplay.Setup` 挂的那颗「看本周期可领奖励」按钮（#427）。</summary>
        public static CampaignData.RewardSpec[] LoginRewardSpecs()
        {
            var specs = new CampaignData.RewardSpec[_loginRewardArt.Length];
            for (int i = 0; i < specs.Length; i++)
                specs[i] = new CampaignData.RewardSpec(_loginRewardArt[i], _loginRewardCount[i], CampaignData.TierBasic);
            return specs;
        }

        /// <summary>骷髅卡的奖励（一份）—— `CollectSkulls` 发的同一份。</summary>
        public static CampaignData.RewardSpec[] SkullsRewardSpecs()
        {
            return new[] { new CampaignData.RewardSpec(SkullsRewardArt(), SkullsRewardCount(), CampaignData.TierBasic) };
        }

        /// <summary>周常卡的奖励（一份）—— `CollectWeekly` 发的同一份（⛔ 别在预览里另写一个 500）。</summary>
        public static CampaignData.RewardSpec[] WeeklyRewardSpecs()
        {
            return new[] { new CampaignData.RewardSpec(WeeklyRewardArt, WeeklyRewardCount, CampaignData.TierBasic) };
        }

        /// <summary>第 `i` 条每日任务的奖励（一份）—— `CollectDaily` 发的同一份。</summary>
        public static CampaignData.RewardSpec[] DailyTaskRewardSpecs(int i)
        {
            var t = At(i);
            return new[] { new CampaignData.RewardSpec(t.RewardArt, ParseCount(t.RewardText), CampaignData.TierBasic) };
        }

        /// <summary>每日奖励轨上第 `day` 天、普通那条抽屉的奖励（一份；`Premium` 那条单机恒锁、不预览）。
        /// 与 `CollectReward` 同源。</summary>
        public static CampaignData.RewardSpec[] DailyTrackRewardSpecs(int day, bool premium)
        {
            return new[] { new CampaignData.RewardSpec(RewardIconOf(day, premium), RewardAmount(day, premium),
                                                       premium ? CampaignData.TierPremium : CampaignData.TierBasic) };
        }

        /// <summary>🆕 **2026-10-26（B9 · P9 #530）**：页头 `Refill Counter` 该不该显示 ——
        /// 原版 `MissionsTab.RefreshRefillCount()` 末句 `dailyMissionRefillCounter.enabled = 0 &lt; 合计`
        /// （`MissionsTab__RefreshRefillCount.c`）⇒ **合计为 0 时整件不显示**。
        /// <para>合计 = 当前还能重摇的条数（`InProgress` 那几条）—— 见 <see cref="RefillText"/> 的注释：
        /// 原版数的是「每条任务**剩余重摇次数**之和」，那个字段是**服务端下发的**
        /// （`MissionChallengeProgress` 的剩余重摇数，本地 `assets_full` 0 命中）⇒ 我们这一侧
        /// 「剩余次数」无界，取「还能重摇的条数」等价（≡ 参考图上那 3）。</para></summary>
        public static bool RefillCounterVisible { get { return RefillCountValue() > 0; } }

        /// <summary>`Refill Counter` 那个数（= <see cref="RefillCounterVisible"/> 的判据源，自检也读它）。
        /// ⛔ 别在 `MissionsTab` 里再数一遍（「两处写同一条规则 = 迟早不一致」）。</summary>
        public static int RefillCountValue()
        {
            int n = 0;
            for (int i = 0; i < _daily.Length; i++) if (_daily[i].St == State.InProgress) n++;
            return n;
        }

        /// <summary>🆕 **2026-10-26（B9 · P9 #530）**：周常奖励的**唯一来源**（`CollectWeekly` 与预览共用）。
        /// 🔶 自己设计（非原版）：周常奖励 = 500 金块 · 原版周常卡的 `Rewards` 出厂 `activeSelf = false`、
        /// 那一份奖励由服务端下发（本地判据为空）⇒ 数额是我们挑的 · 用户 2026-09-17（「不做真实经济」）｜
        /// 原注：原来这两个字面量写在 `CollectWeekly` 方法体里，预览接上后必须收成一处。</summary>
        public const string WeeklyRewardArt = "40k_topmarquee_currency_gold";
        /// <summary>见 <see cref="WeeklyRewardArt"/>。</summary>
        public const int WeeklyRewardCount = 500;

        // ------------------------------------------------------------ ④ 顶栏阵营（#91 / #92）

        /// <summary>顶栏当前阵营 —— ≡ 原版 `CampaignHandler.SelectedArmy`
        /// （`DailyRewardPopup__Open.c` 读 `LiveOpsManager.GetHandler&lt;CampaignHandler&gt;().&lt;int@0x7C&gt;`；
        /// `SetArmy` 那一跳写的就是它）。出厂 = `HeaderArmy`（Goff / 20 —— 奖励窗 prefab 的占位串就是 `'Orks'`）。
        /// <para>⚠️ **持久化没做、如实标注（铁律 3）**：原版那是**玩家账户**里的字段（PlayFab 存档），
        /// 本机没有账户存档 ⇒ 只在**本次进程内**保持（重启回 `20`）。要落盘得往 `StoreDto` 加一格，
        /// 而那属于「账户/云存档」那条线（不在本件）。</para></summary>
        static int _headerArmy = HeaderArmy;

        /// <summary>`DailyRewardPopup` 顶栏现在画哪个阵营（`ArmyIcons.Entry.Army` 的取值域）。</summary>
        public static int HeaderArmyValue { get { return _headerArmy; } }

        /// <summary>≡ 原版 `DailyRewardPopup.SetArmy(CardArmy)` + `RefreshOnArmyChange(CardArmy)`
        /// （`DailyRewardPopup__SetArmy.c` / `__RefreshOnArmyChange.c` 两份全文）：
        /// ① `CampaignHandler.SetSelectedArmy(army)` ⇒ 我们写 <see cref="_headerArmy"/>；
        /// ② 顶栏两颗按新阵营改写（`ArmyIcons.LocalizedName` / `IconName` 就是原版
        ///    `ArmyUtilities.GetLocalizedArmyName` / `GetArmyIcon` 的等价物，判据见 `Core/ArmyIcons.cs` 头）；
        /// ③ `rewardsTrack.RefreshData(mission, refreshAll: true)` ⇒ **调用方重建整扇窗**
        ///    （本仓两扇日常窗一律「数据驱动 + `Build()` 现算」，同 `RewardWindow` 关窗后的那一次刷新）。
        /// <para>⚠️ 认不出的阵营**不换**并出声 —— 原版 `armyIcons.GetArmyIcon` 对 `Neutral(0)` 返回 null，
        /// 顶栏会空一块；我们不让那一态发生（选择条只列 `IconsByArmySO` 里那 13 个）。
        /// ⚠️ 同阵营再点 ⇒ **不改**（原版 `ChangeSelectedArmy` 的早返回，`c:30-33`）。</para>
        /// <para>**返回值** = 有没有真的换（`true` = 值变了，调用方才需要重建）。</para></summary>
        public static bool SetHeaderArmy(int army)
        {
            ArmyIcons.Entry e;
            if (!ArmyIcons.TryGet(army, out e))
            {
                Debug.LogWarning("[Daily] 换阵营：`" + army + "` 不在 `IconsByArmySO` 那 13 条里 ⇒ **不换**"
                               + "（原版那一格取不到图、顶栏会空一块）");
                return false;
            }
            if (_headerArmy == army) return false;              // 原版 `ChangeSelectedArmy` 的早返回
            _headerArmy = army;
            Debug.Log("[Daily] 顶栏阵营已切到 `" + e.Faction + "`（" + army + "）"
                    + " —— 原版这一跳 = `CampaignHandler.SetSelectedArmy` + `RefreshOnArmyChange`");
            return true;
        }

        /// <summary>`Army Selector` 的候选列表 —— ≡ 原版 `FeatureConfig.GetValidArmies()`。
        /// 🔶 自己设计（非原版）：候选 = `IconsByArmySO` 那 13 个阵营**全列** · 原版按远端 `FeatureConfig` 的开关筛
        /// （`FeatureConfig__GetValidArmies.c` = `ConfigManager.GetConfig&lt;FeatureConfig&gt;()` + `Enum.GetValues(CardArmy)`
        /// 过滤），那份配置在**远端 CCD**、本地 `assets_full` 0 命中 ⇒ 只能全列 · 调度台 2026-10-26
        /// ⚠️ 它与 `HeaderArmyIcon/Name` 是**同一份数据的两个读口**（同一个 `ArmyIcons` 表），⛔ 别各自维护一份。</summary>
        public static int[] ValidArmies()
        {
            var all = ArmyIcons.All;
            var ids = new int[all.Length];
            for (int i = 0; i < all.Length; i++) ids[i] = all[i].Army;
            return ids;
        }

        // ============================================================ 原版类名的同名入口（2026-10-28 · `B93_E10`）
        //
        // 🔴 **为什么集中放在类尾**：这四条对应的原版成员分别在 `MissionEvent<T,TData>` / `Missions` /
        //    `MissionPersistence` 上，而**我们的等价实现全在本类**（本仓的「日常」= 一套本地数据源，
        //    没有 `MissionEvent` 那个事件对象层面）。账里那几行的缺口写的是「**缺一个与原型同名的方法**」
        //    —— 那正是本节存在的理由（可核性：账的落点能指到一个同名方法）。
        // ⚠️ **铁律 18·b**：新成员一律追加在类尾（⛔ 别往中间插）—— 中间插会让本文件下游几十条账落点整体漂移。
        // ⚠️ **本节四条里有两条是「语义别名」（`RerollChallenge` / `UpdateChallengeProgress`）**：
        //    行为本体在别的方法里，别名只是名字对齐 ⇒ 它们**没有调用方**，如实写在每一条的注释里。

        /// <summary>原版 `MissionEvent<T,TData>.Initialize()`（`decomp_generic_trial/out/…__Initialize__102ee20.c`）：
        /// 「遍历 `Save.challengeProgress` 逐条 `InitializeChallenge`（初始化 + 订阅）」。
        /// <para>我们这一侧的等价物 = <see cref="InitializeHandler"/>（读盘 + 建表）——
        /// **遍历那一半是空的**：我们的三条日常任务**不是** `MissionChallenge` 实例，
        /// 磁盘上也没有 `challengeProgress[]`（见本文件头「这是我们自己设计的」那一段）⇒ 如实为空，⛔ 不编一张假表。</para></summary>
        internal static void Initialize() { InitializeHandler(); }

        /// <summary>原版 `Missions.InitializeHandler()`（`decomp_full/Missions__InitializeHandler.c`）：
        /// 取配置（`MissionChallengeProvider.GetAssets()` → 远端 `MissionsConfig`）+ 建组。
        /// 我们的等价物 = **静态构造那一拍读一次盘**（`EnsureLoaded`）⇒ 本方法是它**唯一**的入口
        /// （`static DailyData()` 调的就是它 —— 所以这一条**有调用方**、不是死代码）。</summary>
        internal static void InitializeHandler() { EnsureLoaded(); }

        /// <summary>原版 `MissionEvent<T,TData>.RerollChallenge(challenge, onComplete)`（`…__RerollChallenge__102f210.c`）：
        /// 「换一条新的（服务端下发）」。
        /// <para>⚠️ **语义别名（0 调用方）**：我们这一侧的实现整体在 <see cref="RerollDaily"/>（就地换掉那一行），
        /// 调用点 = `Shell/MissionRerollPopup.cs:481`（那个文件不在本批白名单）⇒ 名字对齐用，行为**逐字相同**
        /// （它转调 `RerollDaily`）。⛔ 别在这里再写一份换任务的逻辑。</para></summary>
        internal static string RerollChallenge(int i) { return RerollDaily(i); }

        /// <summary>原版 `MissionEvent&lt;T,TData&gt;.UpdateChallengeProgress(payload)`
        /// （`…__UpdateChallengeProgress__102f660.c`，197 行）：服务端推进一次就下发一次。
        /// <para>我们**没有服务器** ⇒ 等价物 = **结算那一拍推本地进度**，本体在 <see cref="OnBattleEnd"/>
        /// （它同时管骷髅计数 / 刚完成提示 / 落盘）。本方法只是**名字对齐的转发**（⛔ 别让两者各推一次 —— 双记）。</para></summary>
        internal static void UpdateChallengeProgress(bool win, int damageToEnemy, int troopsPlayed, int skulls,
                                                    int playMode, int damageToEnemyWarlord = -1,
                                                    int attacksDeclared = -1, int rangedAttacksDeclared = -1)
        {
            OnBattleEnd(win, damageToEnemy, troopsPlayed, skulls, playMode,
                        damageToEnemyWarlord, attacksDeclared, rangedAttacksDeclared);
        }

        /// <summary>原版 `Missions.UpdateMissionProgress(Action onComplete)`（`decomp_full/Missions__UpdateMissionProgress.c`，
        /// 157 行 / 173 语句）：**上传进度 + 本地落盘**。
        /// <para>我们这一侧**只有本地那一半**（上传要服务器）⇒ 本体 = <see cref="Persist"/>。
        /// `onComplete` = 原版上传完的回调（云端受理后刷新界面）；我们没有「等待受理」那一拍
        /// ⇒ **不接回调**（传 null 是唯一诚实的写法，⛔ 别造一个假的异步）。</para></summary>
        internal static void UpdateMissionProgress(System.Action onComplete)
        {
            Persist("Missions.UpdateMissionProgress（本地落盘那一半）");
            // ⚠️ `onComplete` **故意不调**：原版它在**云端受理之后**跑（改界面用）；本地没有「受理」这一步，
            //    调它会让调用方以为「已经和服务器对齐」。要接它必须先补出「受理」这个概念。
            if (onComplete != null)
                Debug.Log("[Daily] `Missions.UpdateMissionProgress` 的 `onComplete` **不会**被调用："
                        + "原版它在云端受理后跑，我们没有服务器（红线：不许静默 —— 这句就是说明）。");
        }

        /// <summary>原版 `MissionPersistence.OnBeforeRead(JObject jo)`（`decomp_full/MissionPersistence__OnBeforeRead.c`）：
        /// 反序列化**之前**做老格式迁移 —— `jo["challenges"]` 若是**单个对象**（老档），
        /// 包成 `new JArray(obj)` 再写回去（这样接下来的读取就当数组处理）。
        /// <para>我们这一侧的同一条路：`StoreDto.rows` 在老档里也是**单个对象**。本方法做纯文本迁移，
        /// 由 <see cref="EnsureLoaded"/> 在 `JsonUtility.FromJson` **之前**调用（顺序就是原版的语义）。</para>
        /// <para>⚠️ 扫描**必须跳过字符串字面量**：`desc` / `rewardText` 里可能有 `{0}` 这类花括号，
        /// 不过那些是**值**里的字符、不在 `rows` 的结构位置上；仍然按引号感知扫，避免把值里的 `}` 当成结构。</para>
        /// <returns>迁移后的 JSON（不需要迁移时**原样返回同一个字符串**）。</returns></summary>
        internal static string OnBeforeRead(string json)
        {
            if (string.IsNullOrEmpty(json)) return json;
            int at = json.IndexOf("\"rows\"", System.StringComparison.Ordinal);
            if (at < 0) return json;
            int i = at + 6;
            while (i < json.Length && (json[i] == ' ' || json[i] == '\t' || json[i] == '\r' || json[i] == '\n')) i++;
            if (i >= json.Length || json[i] != ':') return json;       // 不是我们认得的形状 ⇒ 原样交回（交给版本/异常那条路去出声）
            i++;
            while (i < json.Length && (json[i] == ' ' || json[i] == '\t' || json[i] == '\r' || json[i] == '\n')) i++;
            if (i >= json.Length || json[i] != '{') return json;        // 已经是数组 / null ⇒ 不动
            // 找这个对象的收尾 `}`（引号感知 —— 值里的 `}` 不算结构）
            int depth = 0; bool inStr = false; bool esc = false; int end = -1;
            for (int k = i; k < json.Length; k++)
            {
                char c = json[k];
                if (inStr)
                {
                    if (esc) { esc = false; continue; }
                    if (c == '\\') { esc = true; continue; }
                    if (c == '"') inStr = false;
                    continue;
                }
                if (c == '"') { inStr = true; continue; }
                if (c == '{') depth++;
                else if (c == '}') { depth--; if (depth == 0) { end = k; break; } }
            }
            if (end < 0) return json;                                   // 括号不配 ⇒ 原样交回（不猜）
            Debug.Log("[Daily] `MissionPersistence.OnBeforeRead`：存档里的 `rows` 是**老格式（单个对象）**"
                    + " ⇒ 包成一元数组再读（判据：原版 `MissionPersistence__OnBeforeRead.c` 的 "
                    + "`new JArray(obj)` 后写回）。");
            return json.Substring(0, i) + "[" + json.Substring(i, end - i + 1) + "]" + json.Substring(end + 1);
        }

        // ============================================================ `MissionProgressView` 的四个出口
        //
        // 原版这几个显示件读的是同两个对象（`IMission` + `MissionChallengeProgress`）；
        // 我们的对应物是 `_daily[i]` / `_skullsCount` / `_loginState` / `_weeklyProgress` 四族
        // ⇒ **每族一个只读出口**，字段名照样标出原版读点（判据见 `MissionProgressView` 的 summary）。
        // ⚠️ **`Army` 一律传 0（= `CardArmy.Neutral`）**：原版那条任务的阵营**在服务端**、本地拿不到，
        //    我们这份 mock 恒 Neutral（判据 + 实拍两条证据见 `SkullsArmy()` 那一段）⇒ 图标那一格不建、也不留空位。

        /// <summary>每日第 `i` 行的读口（`CurrentValue/MaxValue` = `Progress/Target`）。</summary>
        internal static MissionProgressView ProgressView(int i)
        {
            var t = At(i);
            return new MissionProgressView(t.Progress, t.Target, CanCollectDaily(i), DailyClaimed(i), 0);
        }

        /// <summary>骷髅卡的读口（`CurrentValue` = `_skullsCount`，`MaxValue` = **最后一档阈值** —— 即原版 `MaxValue`
        /// = `AvailableRewards()` 的**最后一个** `targetValue`，见 `DailyData.SkullsStepTarget`）。</summary>
        internal static MissionProgressView SkullsProgressView()
        {
            return new MissionProgressView(_skullsCount, SkullsStepTarget(SkullsStepCount - 1),
                                           CanCollectSkulls(), SkullsClaimed(), 0);
        }

        /// <summary>登录卡的读口（`CanCollect` = `CanCollectLogin`；`MaxValue` = 1 —— 登录卡是**单件**，
        /// 原版那一条挑战只有一个里程碑 ⇒ 进度那一格恒 `0/1` 那一档，我们这一侧不画它）。</summary>
        internal static MissionProgressView LoginProgressView()
        {
            return new MissionProgressView(_loginState == State.Claimed ? 1 : 0, 1,
                                           CanCollectLogin(), LoginClaimed(), 0);
        }

        /// <summary>周常卡的读口（`CurrentValue` = `_weeklyProgress`，`MaxValue` = 最后一档阈值）。</summary>
        internal static MissionProgressView WeeklyProgressView()
        {
            return new MissionProgressView(_weeklyProgress, WeeklyStepTarget(WeeklyStepCount - 1),
                                           CanCollectWeekly(), WeeklyClaimed(), 0);
        }

        /// <summary>登录卡奖励格的**归并后**清单（原版 `MissionRewardsDisplay.Setup` 里那个 `GroupBy(x =&gt; x.ItemId)`）。
        /// 🆕 **2026-10-28（`B93_E10` · 账 `:508`）**：归并求和那一半落成
        /// <see cref="MissionRewardsDisplay.MergeSameItem"/>；本方法只是把**我们这张表**（`_loginRewardArt` /
        /// `_loginRewardCount`，逐格读）喂进去 ⇒ `MissionsTab.BuildLoginCard` 的建格循环改吃它。
        /// ⚠️ 我们这张表**恒无重复 Id** ⇒ 归并后与逐格直取**逐位同值**（今天的观感零变化）；
        /// 它存在的意义是「将来若某一版表里出现两条同 Id，格子数会自己少一颗、数量会相加」——
        /// 那正是原版 `GroupBy` 的行为。</summary>
        internal static System.Collections.Generic.List<MissionRewardsDisplay.Cell> LoginRewardCellList()
        {
            var src = new System.Collections.Generic.List<MissionRewardsDisplay.Cell>(_loginRewardArt.Length);
            for (int i = 0; i < _loginRewardArt.Length; i++)
                src.Add(new MissionRewardsDisplay.Cell { ItemId = _loginRewardArt[i], Quantity = _loginRewardCount[i] });
            return MissionRewardsDisplay.Setup(src);   // = 原版同名入口（内部转调 `MergeSameItem`；🆕 2026-10-29 B94_S2 把休眠的 `Setup` 接上线，账 508）
        }

        // ============================================================ 每日奖励 · Premium 轨（🆕 2026-10-28 · `B93_N1`）
        // 本节`追加在类尾`（铁律「新成员追加到类尾」）：上面最后一条落点（`DailyData.cs:2750`）之后，零行号漂移。
        /// <summary>本机是否已拥有 Premium 轨 = 原版 `InventoryManager.HasItem(mission.PremiumItem)` 的等价物。
        /// <para>判据两处：① `DailyRewardTrackPanel__Initialize.c:24-46`（`HasItem(premiumItem)` 决定那颗价钱钮的显隐）；
        /// ② `DailyRewardTrackPanel__HandlePurchase.c:26-29`（已拥有 ⇒ 直接 `return`）。</para></summary>
        public static bool PremiumOwned { get { return _premiumOwned; } }

        /// <summary>= 原版 `DailyRewardTrackPanel.HandlePurchase()`（`d:/2/tools/decomp_full/DailyRewardTrackPanel__HandlePurchase.c`
        /// 全 72 行）＋ 它的续作 `&lt;HandlePurchase&gt;b__6_0`（`…___HandlePurchase_b__6_0.c:10-19`）。
        /// <para>原版三句：① `if (InventoryManager.HasItem(premiumItem)) return;` · ② `purchaseButton.Interactable = false;`
        /// · ③ `liveOp.PremiumOffer.TryPurchaseOffer(onComplete)`；续作 = `liveOp.CurrentChallenges.FirstOrDefault()` 的
        /// `canCollect(+0x48) = true`。🔴 **`FirstOrDefault()` 无谓词** —— ILSpy 版
        /// `out_cpp2il_dev_ilspy/DailyRewardTrackPanel.cs:99-108` 逐字给出 `Enumerable.FirstOrDefault(currentChallenges)`；
        /// `.c:12` 里那第二个实参是泛型 MethodInfo（IL2CPP 的泛型调用形状），**不是**谓词。
        /// ⛔ 别与 `MissionEvent.TryCollect` 里那个 `FirstOrDefault(x =&gt; x.canCollect)` 混成一条（两处不同）。</para>
        /// <para>🔶 自己设计（非原版）：**第 ③ 句「买」这一跳** · 原版是真钱商店（`PremiumOffer` 由远端商店目录下发，
        /// 关服后判据在远端、本机永远拿不到） · 调度台 2026-10-28 裁定「本地模拟买」。
        /// ⇒ 本口 = ① 守卫（同原版）＋ 置位。**续作不另造**：原版 `canCollect = true` 的作用是
        /// 「让 Premium 奖励可领」，而在我们的状态机里这一格由 `_premiumOwned` 直接参与
        /// <see cref="RewardStateOf"/> ⇒ 置位即达成。⛔ 别再去改某条每日任务的 `St`：原版改的是**当前挑战**的
        /// `canCollect`，不是任务的进度（改了就变成我们自己发明的一条副作用）。</para>
        /// <para>原版第 ② 句（`Interactable = false`）在这里**没有站立点**：本地模拟买是同步的、不会二次进入，
        /// 而且紧接着调用方就把整颗价钱钮 `SetActive(false)`（= `Initialize` 那一句）⇒ 如实不写。
        /// **返回**：真 = 这一次真买成了（调用方据此刷新抽屉 + 收掉价钱钮）。</para></summary>
        public static bool PurchasePremiumLocal()
        {
            if (_premiumOwned) return false;      // 原版 `HasItem(premiumItem)` ⇒ 已拥有 ⇒ 什么都不做
            _premiumOwned = true;
            Say("Premium 轨已「购买」（🔶 本地模拟：原版走真商店 `PremiumOffer.TryPurchaseOffer`，本项目服务器已关服）");
            return true;
        }

        /// <summary>自检口：把「已拥有 Premium」拨到指定态（**测试口，不在出厂路径上**；同 `ForceRewardClaimedForTest` 一族
        /// —— 自检之间不靠隐式状态）。⛔ 运行时别调。</summary>
        public static void ForcePremiumOwnedForTest(bool owned) { _premiumOwned = owned; }

        // ============================================================ 每日奖励 · Premium 轨的「已领」记账（🆕 2026-10-29 · B94_S2）
        //
        //  🔴 **这是一个真缺陷的修复**（铁律 5·b / 11）：修前 `_rewardClaimed[]` **两条轨共用一格**，
        //  而 `RewardStateOf(premium:true)` 把「普通轨已领」映射成 **`Unlocked`**（见它那一行）——
        //  两者叠加的后果 = **Premium 抽屉可以无限领**：每一次都还是 `Unlocked`，每一领又只把
        //  同一个 `_rewardClaimed[i]` 写成 `true`（它本来就是 `true`）⇒ 状态永不前进。
        //
        //  原版判据 = `DailyRewardItemContainer__GetCurrentState.c:18-46`：**两条轨各一格计数** ——
        //  `param_3[+0x10]`（普通轨已领数）与 `*(int*)((longlong)param_3 + 0x14)`（**Premium 轨已领数**）；
        //  末段 `if (cVar4 == 2 && playerData+0x14 <= index) cVar2 = 1;` ⇒ 「普通轨是 Collected **且**
        //  Premium 轨这一格还没领」才给 `Unlocked(1)`。我们这一侧 **Premium 轨那一格就是本数组**。
        //  ⚠️ 与 `_rewardClaimed` 是**两个数组、两条轨**，⛔ 别互相顶替（同 `_streakClaimed` 那对的纪律）。
        static readonly bool[] _rewardPremClaimed = new bool[RewardDays];

        /// <summary>自检口：Premium 轨第 day 格拨到「已领 / 未领」（同 `ForceRewardClaimedForTest` 一族）。⛔ 运行时别调。</summary>
        public static void ForceRewardPremiumClaimedForTest(int day, bool claimed) { _rewardPremClaimed[RI(day)] = claimed; }
    }

    /// <summary>
    /// 「资源固定 2000」那条边界的落点（用户 2026-09-17 拍板：**不做真实经济**；
    /// 用户 2026-10-24 改口径：固定值 **9999 → 2000**）。
    /// 现在只做记账 + 日志；HUD 上的货币条是后面的事。
    /// </summary>
    public static class Wallet
    {
        // 🔴 铁律 5 留痕：本行原值 **9999**（用户 2026-09-17 定的固定值），
        //    用户 2026-10-24 明确「资源 9999，改为 2000」⇒ 现为 **2000**。
        public const int StartAmount = 2000;        // 🔴 用户定的固定值（2026-10-24 起 = 2000）
        public static readonly System.Collections.Generic.Dictionary<string, int> Gold =
            new System.Collections.Generic.Dictionary<string, int>();

        /// <summary>记账。**基准是 `StartAmount`（2000）** —— 用户定的「资源固定 2000」
        /// （⚠️ 2026-10-24 用户把固定值从 9999 改成 2000）。
        /// ⚠️ 第一版从 **0** 起记，于是「领了 150 之后余额是 150」（比 2000 还少），
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
