// B99A4_Assert.cs — 第九十九会话 · 反向项「去掉原版没有的 `--`」（执行代理 `B99_A4`）的自检断言集。
//
// 判据（第一权威，逐条实读）：`d:/2/tools/decomp_full/TimerDisplay__UpdateTimer.c` 的**第一条**：
//     if (*(char *)(param_1 + 0x78) == '\0') { return; }
// ⇒ 原版「取不到目标时刻」那一支是 **静默 `return;`**：**不写字、不动 GameObject**（既不 `SetActive`、也不清字）。
//   `--` 是**我们做的、原版没有**的占位（反向清单 → `判据/B99_R3b_施工单_其余按文件.md` §五 #1）⇒ 本批删掉。
//
// 三处改动（本断言集逐处覆盖）：
//   ① `Shell/TimerDisplay.cs` —— `UpdateTimer()` 与静态口 `TextFor(...)` 的 `target == null` 支不再印 `--`
//      （`TextFor` 走 `Compose(prefix, "")`：`{0}` 填空、模板照印，⛔ 不编日期）。
//   ② `Shell/RankedEventWindow.cs` —— `Ranked Division Info/Content/Timer` 那一行由类尾 `RankTimerDeadline`
//      做闸：**没有截止时刻 ⇒ 整棵不建**（先例 = `B99_W2` 的 `SkirmishEventWindow.BuildClassicTimer`）。
//   ③ `Shell/EnergySinglePlayerOnlyEventWindow.cs` —— `Timer` 那一行由类尾 `EnergyTimerDeadline` 做闸，同上。
//
// 跑法（宿主由调度台接线，形状同 `B94B4Assert` / `B97_MINI2_Assert`）：
//     Debug.Log(P + "B99A4Assert 跑 " + B99A4Assert.Run((ok, msg) => CheckTrue(ok, msg)) + " 条断言");
//
// 🔴 每条**先断前提、再断结论**（⛔ 不用 `if (前提) Check(结论)` —— 前提不成立时会**静默跳过**）。
// 🔴 每条都配**灭自证**：把闸写死恒假 / 让 `TimerDisplay` 无目标时照旧印 `--` ⇒ 必须至少有一条变红。
// 🔴 期望值一律取**原版判据**（`return;` 静默 / prefab 节点名 / 官方词条），⛔ 不取被测实现里的常量。
using System;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>`B99_A4` 的断言集。返回**实际执行的断言条数**（宿主打印它 = 自证真被跑到）。</summary>
    public static class B99A4Assert
    {
        /// <summary>跑全部断言。`check(条件, 断言文案)` 由宿主提供。返回执行的断言条数。</summary>
        public static int Run(Action<bool, string> check)
        {
            if (check == null) { Debug.LogError("[B99A4] `B99A4Assert.Run(null)` ⇒ 不跑"); return 0; }
            int n = 0;
            Action<bool, string> C = delegate (bool ok, string msg) { n++; check(ok, msg); };

            TimerNullIsSilent(C);
            TextForNullHasNoDashes(C);
            RankedTimerGate(C);
            EnergyTimerGate(C);
            return n;
        }

        // ============================================================ ① `TimerDisplay`：无目标 ⇒ 一个字都不写
        /// <summary>
        /// 判据：`TimerDisplay__UpdateTimer.c` 第一条 `if (*(char*)(this+0x78) == '\0') return;`。
        /// <para>⚠️ 这一条测的是**组件本身**（不依赖任何窗口）：建一颗 `Label` 带上自己的出厂串，再 `Attach` 一个
        /// **恒 null** 的目标 ⇒ 文本必须**逐字不变**（= 原版「一个字都不写」），且不出现 `--`。</para>
        /// </summary>
        static void TimerNullIsSilent(Action<bool, string> C)
        {
            var go = new GameObject("B99A4_TimerProbe");
            try
            {
                // ---- 无目标 ----
                var lbl = Label.Create(go.transform, "BUILD", Vector3.zero, 4, Color.white, new Vector2(0.5f, 0.5f), "ProbeNull");
                C(lbl != null, "①（前提·不静默）`Label.Create` 建出了一颗探针文字 —— ⛔ 拿不到就不往下断");
                if (lbl == null) return;
                string before = lbl.Text;
                C(!string.IsNullOrEmpty(before), "①（前提）探针文字的出厂串非空（实得「" + before + "」）");

                var td = TimerDisplay.Attach(lbl, () => (System.DateTime?)null, "P: ", "B99A4 无目标探针", "探针注入：目标恒 null");
                C(td != null, "①（前提）`TimerDisplay.Attach` 挂上了（返回非 null）");

                C(lbl.Text == before,
                  "★ ①a 取不到目标时刻 ⇒ **一个字都不写**（原版 `TimerDisplay__UpdateTimer.c` 第一条就是 `return;`）"
                + "｜实得「" + lbl.Text + "」（出厂「" + before + "」）"
                + "｜🧨 改回 `ShowLabel(true)` + `SetText(Compose(prefix, \"--\"))` ⇒ 本条红");
                C((lbl.Text ?? "").IndexOf("--", StringComparison.Ordinal) < 0,
                  "★ ①b 取不到目标时刻 ⇒ **不出现 `--`**（`--` 是我们做的、原版没有的占位；反向清单 §五 #1）"
                + "｜实得「" + lbl.Text + "」");
                C(lbl.gameObject.activeSelf,
                  "★ ①c 取不到目标时刻 ⇒ **不动 GameObject**（原版这一支在 `SetActive` 之前就 `return` 了）");

                // ---- 有目标（灭自证那一半：闸/静默写死 ⇒ 这一条红）----
                var lbl2 = Label.Create(go.transform, "BUILD", Vector3.zero, 4, Color.white, new Vector2(0.5f, 0.5f), "ProbeHit");
                C(lbl2 != null, "①（前提·不静默）第二颗探针文字建出来了");
                if (lbl2 == null) return;
                TimerDisplay.Attach(lbl2, () => System.DateTime.Now.AddHours(2), "P: ", "B99A4 有目标探针", null);
                C(lbl2.Text != "BUILD" && (lbl2.Text ?? "").StartsWith("P: ", StringComparison.Ordinal),
                  "★ ①d 有目标时刻 ⇒ **正常印**（描述串 + 时间串）"
                + "｜实得「" + lbl2.Text + "」"
                + "｜🧨 把那一支改成恒返回 / 恒不写 ⇒ 本条红（与 ①a 合起来两个方向都能分辨）");
                C(System.Text.RegularExpressions.Regex.IsMatch(lbl2.Text ?? "", "[0-9]"),
                  "★ ①e 有目标时刻 ⇒ 串里**带数字**（时间那一格真的算进去了）｜实得「" + lbl2.Text + "」");
            }
            finally
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
        }

        // ============================================================ ② 静态口 `TextFor`
        /// <summary>判据同上：`target == null` 那一支不再印 `--`（改成 `Compose(prefix, "")` —— `{0}` 填空、模板照印）。</summary>
        static void TextForNullHasNoDashes(Action<bool, string> C)
        {
            const string Tmpl = "Ready to collect in {0}";     // 形状 = 官方 `Missions/CollectCounter`（值带 `{0}`）
            string none = TimerDisplay.TextFor(null, Tmpl, "B99A4 TextFor 无目标", "探针注入：目标 null");
            C((none ?? "").IndexOf("--", StringComparison.Ordinal) < 0,
              "★ ②a `TextFor(null, …)` ⇒ **不印 `--`**（原版那一支是 `return;`，⛔ 不编日期）｜实得「" + none + "」");
            C(none == TimerDisplay.Compose(Tmpl, ""),
              "★ ②b `TextFor(null, …)` = **模板填空**（`Compose(模板, \"\")`，`{0}` 那一格空、模板照印）"
            + "｜实得「" + none + "」"
            + "｜🧨 改回 `Compose(prefix, \"--\")` ⇒ ②a 与 ②b 一起红");

            string hit = TimerDisplay.TextFor(System.DateTime.Now.AddHours(2), Tmpl, "B99A4 TextFor 有目标", null);
            C(hit != none && System.Text.RegularExpressions.Regex.IsMatch(hit ?? "", "[0-9]"),
              "★ ②c `TextFor(有目标, …)` ⇒ **印真时间**（串里有数字、且与无目标那串不同）"
            + "｜实得「" + hit + "」｜🧨 把分支写死 ⇒ 本条红");
        }

        // ============================================================ ③ 排位窗：`Content/Timer` 的闸
        /// <summary>
        /// 判据：原版 `RankingDisplay__Initialize.c` 尾部 —— `if (this.timerDisplay != null &amp;&amp; dateTime.HasValue)
        /// TimerDisplay.Initialize(dateTime, …)`；而 `TimerDisplay__UpdateTimer.c` 取不到目标时刻是 `return;`（静默）。
        /// ⇒ 本地恒无截止时刻 ⇒ **`Ranked Division Info/Content/Timer` 整棵不建**（建了只会把 prefab 占位留在屏上 = 假倒计时）。
        /// </summary>
        static void RankedTimerGate(Action<bool, string> C)
        {
            var mgr = WindowsManager.Instance;
            var before = RankedEventWindow.RankTimerDeadline;
            RankedEventWindow w1 = null, w2 = null;
            try
            {
                // ---- 无截止时刻（本地现况）----
                RankedEventWindow.RankTimerDeadline = null;
                w1 = RankedEventWindow.Create(mgr);
                C(w1 != null, "③（前提·不静默）`RankedEventWindow.Create` 建出了一扇");
                if (w1 == null) return;
                w1.Open();                                  // 原版 `Open` → `Build()`（`BuildLeftColumn` 里那道闸就在这条路上）
                var col1 = w1.transform.Find("Ranked Division Info");
                C(col1 != null, "③（前提）`Ranked Division Info` 建出来了（`BuildLeftColumn` 跑过）");
                var content1 = col1 != null ? col1.Find("Content") : null;
                C(content1 != null, "③（前提）`Ranked Division Info/Content` 建出来了（`RankedDivisionInfo.Build` 的产出）");
                C(content1 != null && content1.Find("Timer") == null,
                  "★ ③a 无截止时刻 ⇒ **`Content/Timer` 整棵不建**（原版那一支是 `return;` 静默；建了 = 印假倒计时）"
                + "｜🧨 把闸写死恒真（照旧建）⇒ 本条红；改回 `Compose(prefix, \"--\")` ⇒ 屏上又出现 `--`");

                // ---- 有截止时刻（灭自证：闸写死恒假 ⇒ 下面三条红）----
                RankedEventWindow.RankTimerDeadline = System.DateTime.Now.AddHours(3);
                w2 = RankedEventWindow.Create(mgr);
                C(w2 != null, "③（前提·不静默）第二扇（带截止时刻）建出来了");
                if (w2 == null) return;
                w2.Open();
                var col2 = w2.transform.Find("Ranked Division Info");
                var content2 = col2 != null ? col2.Find("Content") : null;
                C(content2 != null, "③（前提）带截止时刻那一扇的 `Content` 建出来了");
                var timer2 = content2 != null ? content2.Find("Timer") : null;
                C(timer2 != null, "★ ③b 有截止时刻 ⇒ **`Content/Timer` 建出来**（§2·4「留开关，不硬删」的恢复路）"
                + "｜🧨 把闸写死恒假 ⇒ 本条红（与 ③a 合起来两个方向都能分辨）");
                C(timer2 != null && timer2.Find("Timer Icon") != null,
                  "★ ③c 有截止时刻 ⇒ `Timer/Timer Icon` 也建了（跟原版父链 `Content ▸ Timer ▸ Timer Icon`）");
                var tx2 = timer2 != null ? timer2.Find("Timer") : null;
                var lb2 = tx2 != null ? tx2.GetComponent<Label>() : null;
                C(lb2 != null, "③（前提·不静默）带截止时刻那一颗的 `Label` 拿得到");
                if (lb2 != null)
                    C((lb2.Text ?? "").IndexOf("--", StringComparison.Ordinal) < 0
                      && System.Text.RegularExpressions.Regex.IsMatch(lb2.Text ?? "", "[0-9]"),
                      "★ ③d 有截止时刻 ⇒ 印**时间**（带数字、且⛔ 不是 `--`）｜实得「" + lb2.Text + "」");
            }
            catch (Exception e)
            {
                C(false, "③ 建 / 开排位窗抛异常：" + e.GetType().Name + "：" + e.Message);
            }
            finally
            {
                RankedEventWindow.RankTimerDeadline = before;      // 还原（⛔ 别把测试态漏给后面的节）
                DestroyWin(w1);
                DestroyWin(w2);
            }
        }

        // ============================================================ ④ 单机能源活动窗：`Timer` 的闸
        /// <summary>
        /// 判据：原版 `SinglePlayerOnlyEnergyEventWindow__TryOpen.c` —— `activationType != 0` 才
        /// `timerDisplay.Initialize(事件.GetEndDate())`（`Permanent` ⇒ 整件 `SetActive(false)`），
        /// 而 `TimerDisplay__UpdateTimer.c` 取不到目标时刻是 `return;`（静默）⇒ 本地恒无截止时刻 ⇒ **整棵不建**。
        /// </summary>
        static void EnergyTimerGate(Action<bool, string> C)
        {
            var mgr = WindowsManager.Instance;
            var before = EnergySinglePlayerOnlyEventWindow.EnergyTimerDeadline;
            EnergySinglePlayerOnlyEventWindow w1 = null, w2 = null;
            try
            {
                EnergySinglePlayerOnlyEventWindow.EnergyTimerDeadline = null;
                w1 = EnergySinglePlayerOnlyEventWindow.Create(mgr);
                C(w1 != null, "④（前提·不静默）`EnergySinglePlayerOnlyEventWindow.Create` 建出了一扇");
                if (w1 == null) return;
                C(w1.transform.Find("Timer") == null,
                  "★ ④a 无截止时刻 ⇒ **`Timer` 整棵不建**（原版那一支是 `return;` 静默；建了 = 印假倒计时）"
                + "｜🧨 把闸写死恒真（照旧建）⇒ 本条红");
                C(w1.TimerText == null, "★ ④b 无截止时刻 ⇒ 自检读口 `TimerText` 也是 `null`（节点没建，没有半截状态）");

                // ---- 有截止时刻（灭自证）----
                EnergySinglePlayerOnlyEventWindow.EnergyTimerDeadline = System.DateTime.Now.AddHours(3);
                w2 = EnergySinglePlayerOnlyEventWindow.Create(mgr);
                C(w2 != null, "④（前提·不静默）第二扇（带截止时刻）建出来了");
                if (w2 == null) return;
                var timer2 = w2.transform.Find("Timer");
                C(timer2 != null, "★ ④c 有截止时刻 ⇒ **`Timer` 建出来**（§2·4「留开关，不硬删」的恢复路）"
                + "｜🧨 把闸写死恒假 ⇒ 本条红（与 ④a 合起来两个方向都能分辨）");
                C(timer2 != null && timer2.Find("Timer Icon") != null,
                  "★ ④d 有截止时刻 ⇒ `Timer/Timer Icon` 也建了（原版父链 `Timer ▸ Timer Icon`）");
                var tx2 = timer2 != null ? timer2.Find("Timer") : null;
                var lb2 = tx2 != null ? tx2.GetComponent<Label>() : null;
                C(lb2 != null, "④（前提·不静默）带截止时刻那一颗的 `Label` 拿得到");
                if (lb2 != null)
                    C((lb2.Text ?? "").IndexOf("--", StringComparison.Ordinal) < 0
                      && System.Text.RegularExpressions.Regex.IsMatch(lb2.Text ?? "", "[0-9]"),
                      "★ ④e 有截止时刻 ⇒ 印**时间**（带数字、且⛔ 不是 `--`）｜实得「" + lb2.Text + "」");
            }
            catch (Exception e)
            {
                C(false, "④ 建单机能源活动窗抛异常：" + e.GetType().Name + "：" + e.Message);
            }
            finally
            {
                EnergySinglePlayerOnlyEventWindow.EnergyTimerDeadline = before;   // 还原
                DestroyWin(w1);
                DestroyWin(w2);
            }
        }

        // ============================================================ 清理（追加在类尾 = 零行号漂移）
        /// <summary>把断言里造出来的窗口拆掉（⛔ 别把测试窗漏给后面的节 —— 同 `MainMenuAssertB93D2` 的 `finally`）。</summary>
        static void DestroyWin(Component win)
        {
            if (win == null || win.gameObject == null) return;
            var gw = win as GameWindow;
            if (gw != null) { try { gw.Close(); } catch (Exception) { } }   // 先走关窗径：`WindowsManager` 的缓存 / 监听 / 导航面板都按原版那条路撤（⛔ 只 `DestroyImmediate` 会留脏条目）
            var go = win.gameObject;
#if UNITY_EDITOR
            if (!Application.isPlaying) { UnityEngine.Object.DestroyImmediate(go); return; }
#endif
            UnityEngine.Object.Destroy(go);
        }
    }
}
