// B97W3_Assert.cs — 第九十七会话 · 执行代理 `B97_W3`（2026-10-29 · 波 3）
//
// ============================ 它盯的是什么 ============================
// 本代理在 `CardPresentation/Battle/BattleDriver.cs` 落地的**唯一一件行为改动**：
//
//   · **账 `战斗逻辑驱动:483`**（`BattleManager.RemoveSelfBuffsFromQueues`）的**表现层接线** ——
//     原版**唯一**调用点 = `BattleManager._ResolveTransformIntoRemnant_d__554.MoveNext.c:39`
//     （在 `CardScript.TransformIntoRemnant` **之前**那一句）。我们原来 `RemoveSelfBuffsFromQueues`
//     **0 调用点**（`B97_BD` 2026-10-29 复核过），现在接在「**刚翻面成残骸**」那一拍 =
//     `BattleDriver.PlayRemnantSfx` 的 `now.IsRemnant` 支（引擎已 `Board[slot] = rem` 之后才发 `Death`）。
//
//   残余（**不在本件白名单**，⛔ 别在这里假装实现）：原版转调的
//   `CancelEffectsFromDisappearingUnit(this, card, card, shouldIgnoreDestroy: 1)` 那一层
//   （三条泳道逐条撤 + 「自施 buff 不当作被销毁」）—— 引擎侧同一处缺口 = `RuleEngine/Core/ActionPump.cs:2084`。
//
//   判据（⛔ 别在这里抄第二份）→ `资料/完全复刻/报告/B97_W3.md` §一 · `判据/B97_战斗剩余_按文件施工单.md` §1·5。
//
// ============================ ⛔ 本件**故意**没有的断言 ============================
//   · 账 `:1121`（`MinionManager.GetNextSlotPos` ⇒ `BattleDriver.EnsureMinionSpotAnchor` 接线）**停手写反证**：
//     引擎半 `RuleEngine/Core/BoardSlots.NextSlotPos` 的**循环上界**用错了对象（判据 → 报告 §三），
//     接线会**换回错值 + 每次 LogError**。给它写一条断言只能是「已知会红」的断言，⛔ 不写。
//     修好之后（`BoardSlots` 归 `B97_W4`）再加断言，施工单见报告 §三。
//   · 账 `:255` ① / `:292`：已由 `B97_BD` 落地并有 `Editor/B97_BD_Assert.cs`（13 条）⇒ ⛔ 不重复。
//   · 账 `:1004`：真读点在 `CardPresentation/Shell/ProfileData.cs:666`（不在本件白名单）⇒ 给调度台的补丁，见报告 §四。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//   `B97W3_Assert.CheckB97W3(driver);`
//   · **本件对宿主状态没有要求**（除了「这局已经打起来」= `Ctx != null`）：起始先撤干净那两格、
//     把撤掉的条数记成 `pre` / `preOther`，之后每条期望值都用「本次排了几条」算 ⇒ 接在哪儿都对得上。
//   · **前置硬检查只有 W0 / W0b**（取不到 `driver` / 没开局）。⛔ 别改成 `if (…) Check(…)` 跳过。
//     （红线：`if (x != null) Check(...)` = 前提不成立时**静默跳过**。）
//   · 本件**跑完会把排进去的 VFX cue 全部撤干净**；唯一留下的是那条**空操作 `cueAction`**
//     （它不可撤，`ScheduleCueForTest` 只为证明「⛔ 不许顺手撤 cueAction」）—— 它会把时间线推 **0.4 s**，
//     所以**要接在最后一条断言之后**（宿主里已接在 `BattleScene.Run()` 方法尾的聚合行）。
//   · ⚠️ 它会顺带撤掉宿主遗留在 `(side=1, slot=8/7)` 上的待播 cue（条数打进日志）—— 接在方法尾 ⇒ 之后没有别的断言。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//   · 把 `RemoveSelfBuffsFromQueues` 实现成「只加计数器、不动队列」⇒ **W3** 红（`before-after != removed`）。
//   · 把它实现成「禁播标」（撤过一次之后再排同样的东西也不播）⇒ **W4** 红（紧接着再撤必须 0 条）。
//   · 把它顺手扩到 `cueAction`（撤事件/回调）⇒ **W7** 红（那一格里只有 `cueAction`，必须 0 条）。
//   · 把「按格命中」放宽成「按 side 命中」（不看 slot）⇒ **W2** 红（同一 side 另一格那条也被撤 ⇒ 撤了 3 条）。
//   · 把 `PlayRemnantSfx` 那一句删掉 ⇒ 本件**不红**（这里断的是被调方法的语义，不是调用点是否还在）——
//     调用点的钉法 = `grep -c "RemoveSelfBuffsFromQueues(e.Player, e.Slot)" CardPresentation/Battle/BattleDriver.cs` = 1，
//     那一条**只能靠人工/审查**，如实记在这里（⛔ 不假装断言盖住了它）。
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B97_W3`：账 `战斗逻辑驱动:483` 的**表现层接线**读数口
/// （`RemoveSelfBuffsFromQueues` = 原版 `_ResolveTransformIntoRemnant` 那一句）。
/// <para>⛔ 期望值全是**字面量**（1 / 2 / 0），不从被测实现里反算。</para></summary>
public static class B97W3_Assert
{
    static readonly CheckSink _sink = new CheckSink("[W3] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    /// <summary>本件唯一入口（宿主 `Run()` 里用一个 `{ … }` 块调；⛔ 不用 `Test` 前缀，同 `B97_BD_Assert` 的理由）。</summary>
    public static void CheckB97W3(BattleDriver drv = null)
    {
        MenuCheck.Section(_sink, "W3：账 483 —— `RemoveSelfBuffsFromQueues` 接在「变残骸」那一拍（表现层）");
        Check483RemoveSelfBuffsQueues(drv);
        Debug.Log(_sink.P + "W3 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ==================================================================
    //  账 483：`RemoveSelfBuffsFromQueues` = 「按卡撤掉待播的那一族 `playAnim`」
    //    判据 = `BattleManager__CancelAnimFromUnitFromQueue.c:25-81`（从队尾往前扫、只认 `actionType == 0xB(playAnim)`）
    //         + `BattleManager__RemoveSelfBuffsFromQueues.c:5`（转调 `CancelEffectsFromDisappearingUnit(…, shouldIgnoreDestroy:1)`）
    //    我们的等价物：`_timeline` 里 `cueVfx != null` 那一族就是原版 `playAnim`（见 `CancelPendingAnimsOf` 的 doc）
    // ==================================================================
    static void Check483RemoveSelfBuffsQueues(BattleDriver drv)
    {
        if (drv == null) drv = Object.FindFirstObjectByType<BattleDriver>();
        Tr(drv != null, "W0（前提）取得到 `BattleDriver`（⛔ 取不到就红，不静默跳过）");
        if (drv == null) return;
        Tr(drv.Ctx != null, "W0b（前提）这局已经打起来（`Ctx != null`）—— 本段要读/写 `_timeline`");
        if (drv.Ctx == null) return;

        // 🔴 **不依赖宿主遗留状态**：先把这两格上「宿主自己排的」待播 cue 撤干净，记下各撤掉几条（`pre` / `preOther`），
        //    之后**所有期望值都用「本次排了几条」算**（⛔ 别写死 `t0 + 2` —— 宿主在任何位置接入都对得上）。
        //    这也顺带保证本件结束时时间线上没有本件排的 VFX cue（只剩那条空操作 `cueAction`）。
        const int Side = 1, Slot = 8, OtherSlot = 7;
        int t0 = drv.TimelinePending;
        int pre = drv.RemoveSelfBuffsFromQueues(Side, Slot);
        int preOther = drv.RemoveSelfBuffsFromQueues(Side, OtherSlot);
        int swept = t0 - pre - preOther;                            // 撤完之后的时间线长度（= 「既有的别的东西」）
        C(swept, Mathf.Max(0, swept), "W0c（前提）`swept = t0 − pre − preOther` 必须 ≥ 0：t0 " + t0 + " − pre " + pre
                    + " − preOther " + preOther + " = " + swept + " 条（= 宿主遗留的待播条数，后面每条期望值都从它算）"
                    + "｜🧨 `CancelPendingAnimsOf` 把「撤掉」记成负数 / 重复计数 / 撤了不记账 ⇒ 这里先红");

        // ---- W6/W7：只认 `cueVfx`（= `playAnim`），⛔ 不许顺手撤 `cueAction` / 事件 -------------
        drv.ScheduleCueForTest(0.4f, () => { });                    // = `ScheduleAction`：`cueVfx == null`
        C(drv.TimelinePending - swept, 1, "W6（前提）`ScheduleCueForTest` 把一条 **`cueAction`** 排进了时间线（+1）");
        int rmAction = drv.RemoveSelfBuffsFromQueues(Side, Slot);
        C(rmAction, 0, "W7 ★ 那一格里只有 `cueAction` ⇒ 撤 **0** 条（原版 `CancelAnimFromUnitFromQueue` 只认 `actionType == playAnim(11)`）");
        C(drv.TimelinePending, swept + 1, "W7b ★ 撤那一次之后时间线**一条不少**（灭「顺手撤 cueAction / 事件」那种实现）");

        // ---- W1：造三条 `playAnim` 等价 cue（两条同格、一条另一格）------------------------------
        int a1 = drv.ScheduleAnimCueForTest(Side, Slot, "B97W3_CueA");
        int a2 = drv.ScheduleAnimCueForTest(Side, Slot, "B97W3_CueB");
        int a3 = drv.ScheduleAnimCueForTest(Side, OtherSlot, "B97W3_CueOther");
        C(a1 - (swept + 1), 1, "W1a（前提）排第 1 条 `playAnim` 等价 cue ⇒ 时间线 +1（`TimelinePending` 读的是 `_timeline.Count`）");
        C(a2 - a1, 1, "W1b（前提）第 2 条（**同一格**）⇒ +1");
        C(a3 - a2, 1, "W1c（前提）第 3 条（**另一格**）⇒ +1");

        // ---- W2/W3：按**格**撤，且「撤掉的条数 == 时间线实际减少数」----------------------------
        int before = drv.TimelinePending;
        int removed = drv.RemoveSelfBuffsFromQueues(Side, Slot);
        int after = drv.TimelinePending;
        C(removed, 2, "W2 ★ `RemoveSelfBuffsFromQueues(1, 8)` 只撤**本件排的那 2 条**（放宽成「按 side」⇒ 会变 3，连另一格那条一起撤）");
        C(before - after, 2, "W3 ★ 时间线**实际少了 2 条**（灭「计数对、队列没动」）");
        C(after - before, -removed, "W3b ★ `Δ TimelinePending == -removed`（两条口径必须同时成立）");
        C(after, swept + 2, "W3c ★ 撤完只剩「那条 `cueAction` + 另一格那条」+ 宿主遗留（`swept + 2`）");

        // ---- W4：紧接着再撤必须 0 条（灭「实现成禁播标」）---------------------------------------
        int again = drv.RemoveSelfBuffsFromQueues(Side, Slot);
        C(again, 0, "W4 ★ 紧接着再撤 ⇒ **0** 条（原版是「撤队列」不是「禁播」）");
        C(drv.TimelinePending, after, "W4b 那一次之后时间线**不变**");

        // ---- W5 + 收尾：另一格那条还在（顺手撤掉，别把 cue 留到宿主后面去播）--------------------
        int otherRemoved = drv.RemoveSelfBuffsFromQueues(Side, OtherSlot);
        C(otherRemoved, 1, "W5 ★ 另一格 `(1, 7)` 那条**还在**，撤得掉本件排的 1 条（灭「第一次撤把整条时间线清空」）");
        C(drv.TimelinePending, swept + 1, "W5b 收尾：时间线回到「宿主遗留 + 那条空操作 `cueAction`」（本件排的 VFX cue 全部撤净）");
    }
}
