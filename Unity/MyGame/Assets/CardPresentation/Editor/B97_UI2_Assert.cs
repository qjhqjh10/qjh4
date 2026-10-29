// B97_UI2_Assert.cs — 执行代理 `B97_UI2`（第九十七会话 · 2026-10-29）
//
// ============================ 它盯的是什么 ============================
// 账 `战斗逻辑驱动:381`（原版 `BattleManager.GetTargetInPos(Vector3 worldSpaceMousePosition)`）的
// **落点重锚**（判据正本 → `资料/完全复刻/判据/B97_结构差与休眠_再裁.md` #23 ＋ §三·2）。
//
// 原版（`decomp_full/BattleManager__GetTargetInPos.c` 全文 271 行现读）：
//   · `:104` 2D 支 = 从 `+0xa0` 那个组件的位置出发的 `Physics2D.GetRayIntersectionNonAlloc(ray, buf, **-1f**)`
//     ⇒ **负距离 ⇒ 恒 0 命中**（`-1f` 已由 `read_literal` 实读）⇒ **死支**；它里面有**三档**产物
//     （`ActiveSkillDisplay` / `ActiveWarlordAbility` / 裸 `CardScript`）与一道 `+0x290 == 10 / 0xd ⇒ null`
//     的状态闸 —— 那一整段**永远到不了**。
//   · `:204-265` **真生效支** = `boardCamera.ScreenPointToRay(TouchInputManager.TouchPosition)`
//     → `Physics.RaycastNonAlloc(方向 × 200f, hits, 1 << LayerMask.NameToLayer("BoardElements"))`
//     → 取 `hits[0]`（`RaycastNonAlloc` 不排序）→ `GetComponentInParent<CardScript>()`
//     ⇒ 返回**棋盘**元素（`null` = 没打中）。
//   ⇒ 我们的对应物 = `BattleDriver.HitSlot`（**棋盘**命中的唯一口：`Update` 的按下 / 悬停 / 等目标三条都走它），
//     ⛔ **不是** `CardInteraction.Frontmost`（那是**手牌 / 换牌区**那套，判据 `A1668`；账原来指的正是它 = 错配）。
//
// 本文件钉的就是「拾取产品确实是**棋盘**元素」这件事（⛔ 不改手牌那套、⛔ 不改 `HitSlot` 的行为）：
//   A 前提 → B 指针压在棋盘单位上 ⇒ 拾取产品 = 那张**棋盘视图**（棋盘表里有、手牌表里没有）
//   C 负例：点在空处 ⇒ −1 / 悬停清空 · D 藏起来的卡不参与命中（原版 `RaycastNonAlloc` 同样打不到 inactive collider）
//
// ============================ 🔴 灭自证（什么改动会让哪一条红）============================
//   · 棋盘命中改回 `CardInteraction.Frontmost`（手牌那套的卡表 / 坐标系）⇒ B3 红（悬停拿不到棋盘卡）。
//   · `HitSlot` 把棋盘卡挪进手牌表（或产品换成手牌视图）⇒ B4 红。
//   · `CardHit` 删掉 `activeInHierarchy` 那道闸 ⇒ D1 / D2 红。
//   · 把「点空处」也判成命中（容差放大到满屏之类）⇒ C1 红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//   宿主建议 = `CardPresentation/Editor/BattleScene.cs`（那里 `driver` 活着、`use3DBoard` 有 `boardCam`）。
//   调用：`B97_UI2_Assert.CheckB97UI2TargetInPos(driver);`（不传参也行 —— 它会 `FindFirstObjectByType`）。
//   ⚠️ 本件**自己会 `Begin` 一局**（与 `A964` 同一颗种子 `20260915`）⇒ 插在 `A964` 段
//      `drv964.Begin("Ultramarines", "Goff", 20260915);` **之前**（那段紧接着也 `Begin`，会把本件的状态盖掉）；
//      ⛔ 别插在 `DestroyImmediate(driver)` 之后。
//   · **不跑 Unity 批处理**、不建持久资产：视图由驱动自己的 `Begin`/`RefreshAll` 重建，本件只摆一个单位。
//   · 收尾把指针挪到空处（`TickBoardHover` 会成对派发 exit）⇒ 不留悬停态给后面的断言。
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B97_UI2`：**棋盘拾取的锚点**（原版 `BattleManager.GetTargetInPos` 真生效支 ⇒ `BattleDriver.HitSlot`）。
/// <para>⛔ 期望值全是**字面量 / 独立算出来的量**（槽号来自夹具自己的摆放、命中数来自卡表），不从被测实现里反算。</para></summary>
public static class B97_UI2_Assert
{
    static readonly CheckSink _sink = new CheckSink("[UI2] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ============================================================ 入口
    /// <summary>本件唯一入口。`drv` 为空时按场景里那一个 `BattleDriver` 取（取不到 ⇒ **出声红**，⛔ 不静默跳过）。</summary>
    public static void CheckB97UI2TargetInPos(BattleDriver drv = null)
    {
        MenuCheck.Section(_sink, "UI2 棋盘拾取 = `BattleManager.GetTargetInPos` 真生效支（账 战斗逻辑驱动:381）");

        if (drv == null) drv = Object.FindFirstObjectByType<BattleDriver>();
        Tr(drv != null, "A0（前提）拿到一个活着的 `BattleDriver` —— 没有它本件一条都验不了");
        if (drv == null)
        {
            Debug.Log(_sink.P + "UI2 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败（前提不成立，后面全部没跑）");
            return;
        }

        var go = (GameObject)null;
        try
        {
            // 换一局**干净的非教程局**（与 `BattleScene` 的 `A964` 段同一颗种子）—— 教程局会藏 HUD/藏单位，
            // 局与局之间还会留着上一段的效应 ⇒ 用新局保证「棋盘上只有我摆的那一个单位」。
            drv.Begin("Ultramarines", "Goff", 20260915);
            if (drv.InMulligan) drv.SimulateMulliganDone();
            Tr(drv.Ctx != null && !drv.Ctx.IsOver, "A1（前提）一局进行中的战斗（`Ctx` 在、没结束）");
            if (drv.Ctx == null) return;

            int side = drv.MySideForTest;
            var def = FindCard(StarterCards.Tide(), "Tide Minion");
            Tr(def != null, "A2（前提）`Tide Minion` 在 `StarterCards.Tide()` 里（夹具用的就是这张测试卡）");
            int slot = SimpleAI.FirstFreeSlot(drv.Ctx.Players[side]);
            Tr(slot >= 0 && BoardSpec.IsDeployable(slot), "A3（前提）我方棋盘上有一个合法部署格能摆探针单位");
            if (def == null || slot < 0) return;

            drv.Ctx.Players[side].Board[slot] = new UnitState(def, false) { Exhausted = false };
            drv.RefreshAll();

            CardView v;
            bool haveView = drv.MyUnits.TryGetValue(slot, out v) && v != null;
            Tr(haveView, "A4（前提）那个单位建出了视图（槽 " + slot + "）");
            if (!haveView) return;
            go = v.gameObject;

            // ------------------------------------------------------------ B 指针压在棋盘单位上
            Vector3 ptr = drv.PointerAtWorldForTest(v.transform.position);
            C(drv.HitUnitSlotForTest(side, ptr), slot,
                "B1 ★ 指针压在**我方棋盘单位**上 ⇒ 棋盘拾取判回它自己（`BattleDriver.HitSlot` = 原版 "
              + "`GetTargetInPos` 真生效支「`boardCamera` 射线 → `BoardElements` 层 → `GetComponentInParent<CardScript>`」的对应物）");
            Tr(drv.HitSlotHitCountForTest(side, ptr) >= 1,
                "B2（前提）这确实是「真 collider」那一条命中（`CardHit` / `CardView.HitByCollider`），不是退回手算矩形");

            drv.TickBoardHoverForTest(ptr);
            Tr(ReferenceEquals(drv.HoverBoardCardView, v),
                "B3 ★ 「每帧指针压着哪张棋盘卡」= **同一张棋盘视图**（这正是原版 `BattleManager.Update` → "
              + "`GetTargetInPos` 那条链的消费者）｜🧨 把落点换回 `CardInteraction.Frontmost`（手牌那套卡表）⇒ 本条红");
            Tr(drv.HandIndex(v) < 0,
                "B4 ★ 拾取产品是**棋盘**元素 —— 它在棋盘表里、**不在手牌表里**（`HandIndex == −1`）"
              + "｜🧨 把棋盘卡混进手牌表 / 产品换成手牌视图 ⇒ 本条红");

            // ------------------------------------------------------------ C 负例：点空处
            Vector3 far = new Vector3(-99f, -99f, 0f);
            C(drv.HitUnitSlotForTest(side, far), -1,
                "C1（负例）点在空处 ⇒ 棋盘拾取 −1（原版 `RaycastNonAlloc` 命中数 < 1 ⇒ `return null`）");
            drv.TickBoardHoverForTest(far);
            Tr(drv.HoverBoardCardView == null, "C2（负例）点空处 ⇒ 悬停清空（原版 `OnTouchExit` 那一侧）");

            // ------------------------------------------------------------ D 藏起来的卡不参与命中
            go.SetActive(false);
            C(drv.HitUnitSlotForTest(side, ptr), -1,
                "D1 ★ **藏起来的卡不参与命中**（原版 `Physics.RaycastNonAlloc` 打不到 inactive 对象上的 collider；"
              + "我们 = `CardHit` 的 `activeInHierarchy` 闸）");
            drv.TickBoardHoverForTest(ptr);
            Tr(drv.HoverBoardCardView == null, "D2 …悬停也不该停在它身上");
            go.SetActive(true);
            C(drv.HitUnitSlotForTest(side, ptr), slot,
                "D3 ★ 放回来 ⇒ 又能命中（证明 D1 不是别的成因 —— 灭自证：同一点、只改 `activeSelf`）");
        }
        finally
        {
            if (go != null && !go.activeSelf) go.SetActive(true);
            // 收尾：把指针挪到空处，让 `TickBoardHover` 成对派发 exit ⇒ 不留悬停态给后面的断言
            if (drv != null && drv.Ctx != null) drv.TickBoardHoverForTest(new Vector3(-99f, -99f, 0f));
        }

        Debug.Log(_sink.P + "UI2 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ 局部小工具（⛔ 不是判据）
    static CardDef FindCard(List<CardDef> pool, string name)
    {
        if (pool == null) return null;
        for (int i = 0; i < pool.Count; i++)
            if (pool[i] != null && pool[i].Name == name) return pool[i];
        return null;
    }
}
