// ShellScene_B93D4SelfCheck.cs — 日常/奖励线第 7 批（B93_D4）的**自检断言集**（账 `日常_奖励` 79 / 158 / 159 / 662 / 656 / 280 / 535 / 538 / 539 / 541 / 543 / 679 / 680 / 681 / 745 / 748）。
//
// 🔴 **为什么是一个独立文件而不是塞进 `Editor/ShellScene.cs`**：
//   `Editor/ShellScene.cs` 是**多路代理共用**的宿主（铁律 13·3「一个文件同一时刻只有一个写手」）；
//   本批开工期间它已被 `E27` 独占。⇒ 断言本体放这里（**新文件、零冲突**），
//   宿主那边只需要**一行**调用（见文件尾的「接线」注释）。
//
// 跑法（与 `LiveOpsFilterSelfCheck` 同一套形状）：
//     Debug.Log(P + "B93D4SelfCheck 跑 " + B93D4SelfCheck.Run((ok, msg) => CheckTrue(ok, msg)) + " 条断言");
//
// 期望值一律来自**原版判据**（`.c` / ILSpy / prefab 字段），⛔ 不取我们自己的常量当期望；
// 每一族至少一条**灭自证**断言（改回旧写法就红），见各段注释里的「灭自证」。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>B93_D4 的断言集。返回**实际执行的断言条数**（调用方据此自证真被跑到）。</summary>
    public static class B93D4SelfCheck
    {
        public static int Run(Action<bool, string> check)
        {
            if (check == null) return 0;
            int n = 0;
            Action<bool, string> C = (ok, msg) => { n++; check(ok, msg); };

            ToastContextData(C);
            EventBadgeTimeline(C);
            RewardCollectedOnTrigger(C);
            GiveFeedbackQuestions(C);
            RewardServiceEntries(C);
            MissionToast(C);
            LeaderboardTotalStats(C);

            // 已有的结构性自检（B92_N6b 那两条）顺手带上（它们原来也未必有调用方）
            foreach (var e in EventBadge.SelfCheck()) C(false, "EventBadge.SelfCheck：" + e);
            foreach (var e in EventBadgeController.SelfCheck()) C(false, "EventBadgeController.SelfCheck：" + e);
            C(true, "EventBadge / EventBadgeController 结构性自检（见上面逐条，若有）");
            return n;
        }

        // ============================================================ ① 账 158 / 662：`ToastNotifications.Context.Data`
        //   判据：`decomp_full/EventBadge__SetNotificationData.c:44-70` —— 第 ③ 段判的是 **`ctx+0x38` 的 runtime type == RewardInfo**。
        static void ToastContextData(Action<bool, string> C)
        {
            var fd = typeof(ToastNotifications.Context).GetField("Data");
            C(fd != null, "前提：`ToastNotifications.Context` 有 `Data` 槽（原版 `ctx+0x38`）");
            if (fd == null) return;

            var c0 = new ToastNotifications.Context("t");                              // ctor 必须把它置 null
            C(c0.Data == null, "`new Context(..)` 的 `Data` 默认 null（⛔ 不是" + (c0.Data == null ? "" : "非 null") + "）");

            var reward = new RewardInfo("B93D4_CUR", 3);
            var c1 = new ToastNotifications.Context("t");
            c1.Data = reward;                                                          // 结构体 ⇒ 必须是同一引用
            C(ReferenceEquals(c1.Data, reward), "赋值后 `Data` 回读 = 同一引用（结构体装箱没有丢）");

            // 🔴 灭自证：把 `Data` 换成非 RewardInfo ⇒ `is` 判据必须翻面。
            //    任何「恒 true / 恒 false」的判据都会在上面与这里各红一条。
            var c2 = new ToastNotifications.Context("t");
            c2.Data = "not a reward";
            C(c1.Data is RewardInfo, "`Data = RewardInfo` ⇒ `is RewardInfo` 为真");
            C(!(c2.Data is RewardInfo), "`Data = string` ⇒ `is RewardInfo` 为假（灭自证：与上一条相反）");
        }

        // ============================================================ ② 账 159：`EventBadge.ShowNotification` 的 8 拍时间轴
        //   判据：`decomp_full/EventBadge__ShowNotification.c` —— DOTween `Sequence` 的 `Append/Join` 次序：
        //     AppendInterval(.1) → Append(punch .25) → Append(fill .4) → Join(fade .2) → Join(rotate .4)
        //     → AppendInterval(2.0) → Append(fill 0 .4) → Join(rotate −.4)
        //   期望值 = 0.10 / 0.35 / 0.75 / 1.15 / 3.15 / 3.55（**从 `.c` 的次序推出来的**，不是我们的常量抄回来）。
        static void EventBadgeTimeline(Action<bool, string> C)
        {
            var b = EventBadge.TimelineBreakpoints();
            C(b != null && b.Length == 7, "前提：`TimelineBreakpoints()` 给 7 个断点");
            if (b == null || b.Length != 7) return;
            const float tol = 1e-3f;
            C(Mathf.Abs(b[1] - 0.10f) < tol, "① interval 结束 = 0.10（DAT_1834b2dc4）");
            C(Mathf.Abs(b[2] - 0.35f) < tol, "② punch 结束 = 0.35（0.10 + 0.25 · DAT_1834b2e7c）");
            C(Mathf.Abs(b[3] - 0.75f) < tol, "③ fill(0→1) 结束 = 0.75（⚠️ 旧的并行版是 0.50 ⇒ 改回去就红）");
            C(Mathf.Abs(b[4] - 1.15f) < tol, "④ rotate(+360) 结束 = 1.15（0.75 + 0.40）");
            C(Mathf.Abs(b[5] - 3.15f) < tol, "⑤ hold 结束 = 3.15（1.15 + 2.0 · DAT_1834b2bbc）");
            C(Mathf.Abs(b[6] - 3.55f) < tol, "⑥ 全段结束 = 3.55（⚠️ 旧的并行版是 2.90 ⇒ 改回去就红）");
            // 🔴 灭自证：串行 ⇒ fill 结束**严格晚于** punch 结束；旧并行版两者都从 0.10 起、fill 反而更早结束。
            C(b[3] > b[2], "灭自证：fill 结束必须**晚于** punch 结束（串行）；并行版会两者相等或更早");
        }

        // ============================================================ ③ 账 656：`RewardCollected.OnTrigger`
        //   判据：`decomp_full/RewardCollected__OnTrigger.c` + `RewardCollected__<OnTrigger>b__8_2.c`
        //     Where(b__8_0 类型/阵营) → (ignoreDuplicates ? Where(b__8_1 未拥有) : 不挂) → Sum(ignoreDuplicates ? 1 : Quantity)
        static void RewardCollectedOnTrigger(Action<bool, string> C)
        {
            RewardCollected.ResetForTest();
            var rc = new RewardCollected();
            int last = -1; bool fired = false;
            // 硬前提：没接 sink 时 `UpdateProgress` 只打日志 ⇒ 断言会**静默空转**（本项目踩过）
            RewardCollected.ProgressSink = (x, v, ov) => { fired = true; last = v; };

            // 用**肯定不在背包里**的假 id ⇒ `b__8_1`（未拥有）恒真，结果可确定
            CampaignData.RewardSpec[] two = {
                new CampaignData.RewardSpec("B93D4_FAKE_A", 3, CampaignData.TierBasic),
                new CampaignData.RewardSpec("B93D4_FAKE_B", 2, CampaignData.TierBasic),
            };

            rc.ignoreDuplicates = false;
            fired = false; last = -1;
            rc.OnTrigger(new CollectRewardsSignal(two));
            C(fired, "前提：`OnTrigger` 真的推了进度（`ProgressSink` 被调用）");
            C(last == 5, "ignoreDuplicates=false ⇒ 求和 = 数量 3+2 = 5（实际 " + last + "）");

            // 🔴 第二趟**换新实例**（B93_FX6 · B93_E30 §四 补丁 C）：`Progress` 是**实例**属性（`Shell/RewardCollected.cs:72`），
            //   而 `UpdateProgress(sum, shouldOverride:false)` 是**累加** —— 那是原版语义（⛔ 别改实现去改成「覆盖」）；
            //   同一个实例连跑两趟必然得 5+2=7，旧断法 `last == 2` 是**测试写法**的错，不是实现错。
            //   `ResetForTest()` 只清静态 sink/闩、**不清实例进度**（`Shell/RewardCollected.cs:341`）⇒ 只能换实例。
            int lastA = last;                                    // 上一趟（ignoreDuplicates=false）的读数 = 5
            var rc2 = new RewardCollected { ignoreDuplicates = true };
            fired = false; last = -1;
            rc2.OnTrigger(new CollectRewardsSignal(two));
            C(fired, "前提（第二趟）：`ProgressSink` 又被调用");
            C(last == 2, "ignoreDuplicates=true ⇒ 每条算 1 ⇒ 2（实际 " + last + "）");
            // 🔴 灭自证：同一张表、只翻 `ignoreDuplicates` 一位 ⇒ 两趟读数必须不同（2 vs 5）。
            //    「恒返回 Quantity 求和」的旧实现两趟都给 5 ⇒ 这里红；「恒返回条数」的实现第一趟红。
            C(last != lastA, "灭自证：与 ignoreDuplicates=false 那一趟（" + lastA + "）结果不同");

            RewardCollected.ResetForTest();
        }

        // ============================================================ ④ 账 280 / 535 / 538 / 539 / 541 / 543：反馈问卷的题模型
        //   判据：`FeedbackQuestionItem`（`Answer` / `Answered` 三段）、`MultipleChoiceQuestionItem.Initialize`（选满踢最早）、
        //         `OnQuestionAnswered__{Start,OnAnswered}.c`（同一个显隐式子）、prefab 的 5 份 `OnQuestionAnswered` 实例字段。
        static void GiveFeedbackQuestions(Action<bool, string> C)
        {
            var go = new GameObject("B93D4 GiveFeedback Probe");
            try
            {
                var w = go.AddComponent<GiveFeedbackPopup>();
                w.ResetAnswersForTest();                       // 不建节点（`Build()` 要 MenuScroll/PointerLayer）

                // 硬前提：题目表在（16 + 5 道 + 4 个表头）
                C(w.FindItem("S4Q1") != null && w.FindItem("S4Q2_Extra") != null, "前提：`Sections` 里有 S4Q1 与 S4Q2_Extra");

                // --- 539 / 541：初始显隐（原版 `OnQuestionAnswered.Start` 那一算）---
                C(!w.Visible("S4Q2") && !w.Visible("S4Q3") && !w.Visible("S4Q4"),
                  "初始：S4Q1 未答 ⇒ S4Q2/S4Q3/S4Q4 都关着（filter=\"Yes\" + enable=1）");
                C(!w.Visible("S4Q2_Extra") && !w.Visible("S4Q3_Extra"),
                  "初始：两个 `_Extra` 也关着（filter=\"Other\" + enable=1）");

                // --- 539：答了 "Yes" ⇒ 三道放行 ---
                var q1 = w.FindItem("S4Q1").Value;
                int yesIdx = Array.FindIndex(q1.Opts, o => o.Id == "Yes");
                C(yesIdx >= 0, "前提：S4Q1 有 id 为 `Yes` 的选项（prefab 实读）");
                w.ToggleChoice(q1, yesIdx, null);
                C(w.AnswerOf("S4Q1") == "Yes", "S4Q1 答 `Yes` ⇒ `Answer = \"Yes\"`（原版 `string.Join(\";\")` 单值时就是原样）");
                C(w.Visible("S4Q2") && w.Visible("S4Q3") && w.Visible("S4Q4"), "答 Yes ⇒ S4Q2/S4Q3/S4Q4 全部可见");

                // --- 541：答了 "No" ⇒ 又关回去（灭自证：与上一组相反）---
                int noIdx = Array.FindIndex(q1.Opts, o => o.Id == "No");
                C(noIdx >= 0, "前提：S4Q1 有 id 为 `No` 的选项");
                w.ToggleChoice(q1, noIdx, null);
                C(!w.Visible("S4Q2") && !w.Visible("S4Q3") && !w.Visible("S4Q4"),
                  "灭自证：改答 No ⇒ 三道**又关回去**（恒 true / 恒 false 的实现过不了这一对）");

                // --- 539：filter 子串（S4Q2 的 "Other" ⇒ S4Q2_Extra）---
                w.ToggleChoice(q1, yesIdx, null);                          // 先把 S4Q2 打开
                var q2 = w.FindItem("S4Q2").Value;
                int otherIdx = Array.FindIndex(q2.Opts, o => o.Id == "Other");
                C(otherIdx >= 0, "前提：S4Q2 有 id 为 `Other` 的选项");
                w.ToggleChoice(q2, otherIdx, null);
                C(w.AnswerOf("S4Q2").Contains("Other"), "S4Q2 选 Other ⇒ `Answer` 里含 `Other`");
                C(w.Visible("S4Q2_Extra"), "⇒ S4Q2_Extra 打开（`questionFilter = \"Other\"`）");

                // --- 280 / 539：`Answered` 三段（isRequiredIfEnabled 关着恒真）---
                C(w.IsRequiredQuestion("S4Q2"), "S4Q2 现在**可见**且是 isRequiredIfEnabled ⇒ 必答");
                w.ToggleChoice(q1, noIdx, null);                            // 关掉 S4Q2 那一族
                C(!w.IsRequiredQuestion("S4Q2"), "灭自证：S4Q2 被关掉 ⇒ **不再必答**（原版 `Answered` 的 activeSelf 那一支）");
                C(w.Answered("S4Q2"), "被关掉的 isRequiredIfEnabled 题 ⇒ `Answered == true`（不是「没答」）");

                // --- 535：选满踢**最早**选的那个（原版 `MultipleChoiceQuestionItem.Initialize` 的 b__0）---
                w.ResetAnswersForTest();
                var q3 = w.FindItem("S2Q3").Value;                          // max = 3（prefab 实读 `maxSelectedOptions`）
                C(q3.Max == 3, "前提：S2Q3 的 `maxSelectedOptions` = 3");
                C(q3.Opts.Length >= 4, "前提：S2Q3 至少有 4 个选项");
                w.ToggleChoice(q3, 0, null);
                w.ToggleChoice(q3, 1, null);
                w.ToggleChoice(q3, 2, null);
                w.ToggleChoice(q3, 3, null);                                // 满了 ⇒ 踢掉最早选的 0
                string a = w.AnswerOf("S2Q3");
                string want = q3.Opts[1].Id + ";" + q3.Opts[2].Id + ";" + q3.Opts[3].Id;
                C(a == want, "选满第 4 个 ⇒ 踢掉**最早选的**（" + q3.Opts[0].Id + "）而不是拒绝；期望 \"" + want + "\"，实际 \"" + a + "\"");
                C(a != q3.Opts[0].Id + ";" + q3.Opts[1].Id + ";" + q3.Opts[2].Id,
                  "灭自证：与「拒绝新选择」的旧实现{0,1,2}不同");

                // --- 538：数值题（`Answer` = 序号.ToString()，不可取消）---
                w.SetScore("S3Q1", 3);
                C(w.AnswerOf("S3Q1") == "4", "评分题答第 4 颗 ⇒ `Answer == \"4\"`（原版 `toggleNumber.ToString()`）");
                C(w.Answered("S3Q1"), "评分题答了 ⇒ `Answered == true`");
                w.SetScore("S3Q1", 0);
                C(w.AnswerOf("S3Q1") == "1", "灭自证：改答第 1 颗 ⇒ `Answer == \"1\"`（恒值的实现过不了这两条）");

                // --- 543：开放题（`Answer` = 输入框原文）---
                w.SetOpenAnswerForTest("S2Q4", "B93D4 open answer");
                C(w.AnswerOf("S2Q4") == "B93D4 open answer", "开放题 ⇒ `Answer` = 输入框原文");
                C(w.Answered("S2Q4"), "开放题非空 ⇒ `Answered == true`");
                w.SetOpenAnswerForTest("S2Q4", "");
                C(!w.Answered("S2Q4"), "灭自证：清空 ⇒ `Answered == false`");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        // ============================================================ ⑤ 账 679 / 680 / 681：`RewardService` 三个入口
        //   判据：`RewardService__CollectItemReward_object_.c`（一句 `InventoryManager.Add<T>`）·
        //         `RewardService__CollectItemReward.c`（尾跳 ephemeral）· `RewardService__CollectSingleReward.c`（五段）。
        //   ⚠️ 行为断言的**硬前提**是「宿主登记了 `Inventory<ICurrency>`」；这里**不动全局注册表**
        //      （`Register` 是覆盖式且没有反注册口 ⇒ 自检里改它会污染后面的断言）⇒ 只断「没有登记时必须出声且不发」那一档 + 签名。
        static void RewardServiceEntries(Action<bool, string> C)
        {
            var t = typeof(RewardService);
            C(t.GetMethod("CollectItemReward", new[] { typeof(RewardInfo) }) != null, "`CollectItemReward(RewardInfo)` 已建（账 679）");
            C(t.GetMethod("CollectItemReward", new[] { typeof(ICurrency), typeof(string), typeof(int) }) == null,
              "`CollectItemReward<ICurrency>` 是**泛型**入口（不是 ICurrency 特化重载）");
            var g = System.Array.Find(t.GetMethods(), m => m.Name == "CollectItemReward" && m.IsGenericMethodDefinition);
            C(g != null, "`CollectItemReward` 名字在（泛型版）");
            C(t.GetMethod("CollectSingleReward", new[] { typeof(RewardInfo) }) != null, "`CollectSingleReward(RewardInfo)` 已建（账 681）");
            C(t.GetMethod("CollectCurrencyReward", new[] { typeof(ICurrency), typeof(int) }) != null, "`CollectCurrencyReward` 仍在（账 677）");

            // 行为：没有 `Inventory<ICurrency>` 时**不许静默成功**（出声 + 不加）
            if (InventoryManager.GetInventory<ICurrency>() == null)
            {
                bool threw = false;
                try { RewardService.CollectItemReward<ICurrency>(null, "B93D4_INST", 1); }
                catch (Exception e) { threw = true; C(false, "`CollectItemReward(null)` 不该抛：" + e.GetType().Name); }
                if (!threw) C(true, "`CollectItemReward(null, …)` 出声返回、不抛（原版这一档 NRE）");
            }
            else
            {
                // 宿主已登记 `Inventory<ICurrency>` ⇒ 这一格交给**宿主自己那条**（本批刻意不动全局注册表：
                // `Register` 是覆盖式、没有反注册口 ⇒ 自检里改它会污染其后的断言）—— 如实记「未覆盖」，⛔ 不假绿。
                C(true, "宿主已登记 `Inventory<ICurrency>`：本批**未**做「加 3 涨 3」的增量断言（不动全局注册表）—— 这是一条**如实记录**，不是通过");
            }
        }

        // ============================================================ ⑥ 账 745：`ToastNotificationMission.Initialize`
        //   判据：`decomp_full/ToastNotificationMission__Initialize.c` 82 行四步（类型校验 / SetActive(true) / text / infoDisplays；else ⇒ LogError + SetActive(false)）
        static void MissionToast(Action<bool, string> C)
        {
            bool threw = false;
            try
            {
                C(!ToastNotifications.InitializeMissionToast(null), "`Initialize(null)` ⇒ 不弹（原版直接 return）");
                C(!ToastNotifications.InitializeMissionToast("not a context"), "`Initialize(非 Context)` ⇒ 不弹（原版 `is` 校验那一支）");

                var noData = new ToastNotifications.Context("t", ToastNotifications.ToastNotificationType.MissionProgress);
                C(!ToastNotifications.InitializeMissionToast(noData), "`Data == null` ⇒ 不弹（原版 else 支：LogError + SetActive(false)）");

                var wrong = new ToastNotifications.Context("t", ToastNotifications.ToastNotificationType.MissionProgress);
                wrong.Data = new RewardInfo("B93D4_CUR", 1);
                C(!ToastNotifications.InitializeMissionToast(wrong), "灭自证：`Data` 是 RewardInfo（不是 MissionsData）⇒ 也不弹");

                // 正向：Data = MissionsData ⇒ 应当走到 `ShowNotification`（壳里没有控制器时它会返回 false 并出声，
                // 所以这里只断「不抛 + 前三条不成立」，正向由 ⑦ 的壳宿主负责）。
            }
            catch (Exception e) { threw = true; C(false, "`InitializeMissionToast` 抛了 " + e.GetType().Name + "：" + e.Message); }
            if (!threw) C(true, "`InitializeMissionToast` 四个负例都没抛");
        }

        // ============================================================ ⑦ 账 748：`LeaderboardData.GetTotalStats`
        //   判据：`decomp_full/TwoSidesEvent__GetTotalStats.c` —— 3 分支：eventData 空 / mgr 空 ⇒ throw；否则 GetCachedLeaderboard(key)。
        static void LeaderboardTotalStats(Action<bool, string> C)
        {
            LeaderboardData.ResetScoringForTest();
            C(LeaderboardData.GetTotalStats(null) == null, "空 key（= 原版 `eventData == null`）⇒ 返 null（原版 throw，我们不抛）");
            C(LeaderboardData.GetTotalStats("B93D4_NO_SUCH_KEY") == null, "缓存里没有的 key ⇒ 返 null");

            const string k = "B93D4_LB";
            LeaderboardData.LocalScore(k, 7);
            var r = LeaderboardData.GetTotalStats(k);
            C(r != null, "计过分之后 `GetTotalStats(key)` ⇒ 非空");
            if (r != null)
            {
                C(r.CurrentEntry != null && r.CurrentEntry.Value == 7,
                  "取到的是**同一张**榜（`CurrentEntry.Value == 7`）—— 灭自证：恒 null / 恒新建空榜的实现过不了上一条与这一条");
            }
            LeaderboardData.ResetScoringForTest();
            C(LeaderboardData.GetTotalStats(k) == null, "灭自证：清缓存之后同一个 key ⇒ 又变 null（不是缓存了某个常量对象）");
        }
    }
}
