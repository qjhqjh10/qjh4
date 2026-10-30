// B103F1_Assert.cs — 执行代理 `B103_F1` 的断言集
//
// ============================ 它盯的是什么（两条，缺一不可）============================
//   ① **静态信号打到「已销毁」的 `DeckRuntime` 上，不许抛**。
//      批处理（`Application.isPlaying == false`）里 `DestroyImmediate` **不派 `OnDestroy`**
//      （判据 = `资料/普查产出_1012/D9_Battle六红诊断.md:14` · `资料/普查产出_1017/D4_联机诊断.md:123`：
//       本项目两次实测；CLAUDE.md 记录的是「编辑模式不派生命周期」，⛔ 不是「OnDestroy 单独实证过」），
//      而 `Inventories.Wildcards.Updated` 是**静态**事件 ⇒ 已销毁的 `DeckRuntime` 会一直挂在上面。
//      2026-10-30 现场：`DeckRuntime.OnWildcardsUpdated` 里新加的 `RefreshHeader()`（=`SetCountersText` 那半）
//      在一具尸体上取 `_deckNameHint.gameObject` ⇒ `MissingReferenceException` ⇒
//      `DeckScene.Run` **整条宿主崩**（`_tmp_view/deck.log:40090-40101`）、`CollectionScene` 1 红（`collection.log:116263`）。
//   ② **活的实例仍要刷**（⛔ 别把修法做成「干脆不刷」）：`UpdateCounters` 的 `SetCountersText` 那半必须还在。
//
// ============================ 🔴 灭自证（每条「什么改动能让它红」）============================
//   · ① 把 `DeckRuntime.OnWildcardsUpdated` 第一句那道 `Root == null` 闸删掉（= 改前的行为）⇒ 第 1 组直接红（抛异常）。
//   · ① 只把 `RefreshHeader()` 那句删掉（= 改前的行为，B103_R1 之前的样子）⇒ 第 2 组红（四格数字不跟着库存走）。
//   · ①② 结构上不可能被同一个错法一起满足：「恒不刷」命中断言 2、「恒刷（含尸体）」命中断言 1。
//
// ============================ 怎么接（⚠️ 本代理不改宿主）============================
//   宿主 = `Assets/CardPresentation/Editor/DeckScene.cs` 的 `RunCore()`，**接在 `:825` 那条聚合行之后**即可
//   （那一行现在已经在跑 `B99W1Assert` / `B99W5_Assert` / … ；**建议排在 `B99W5_Assert.Run(...)` 之后**，
//    因为本件第 1 组正是把「已销毁实例仍挂在静态事件上」这件事单独复现一遍）：
//
//       B103F1_Assert.Run((tag, ok, detail) => CheckTrue(ok, tag + " —— " + detail));
//
//   `Editor/DeckScene.cs` 归调度台 ⇒ 这一行要**调度台**加；加之前本文件一条都不跑（如实标）。
//
//   ⚠️ 本文件**不落盘**：进门前先 `CardProgress.ResetForTest()`（关掉卡牌进度落盘，⛔ 绝不改玩家存档 ——
//      与 `B99W1_Assert.cs` / `B99W5_Assert.cs` 同一口径），卡组库用**内存里的空库**（`new DeckLibrary()`）。
using System;
using UnityEngine;
using RuleEngine;
using CardPresentation;

/// <summary>B103_F1：静态 `Wildcards.Updated` 上的「已销毁 DeckRuntime」那一族回归。</summary>
public static class B103F1_Assert
{
    /// <summary>跑一遍；返回失败条数（0 = 全绿）。</summary>
    public static int Run(Action<string, bool, string> check)
    {
        int bad = 0;
        if (check == null) return 0;
        Action<string, bool, string> C = (tag, ok, detail) => { check(tag, ok, detail); if (!ok) bad++; };

        // 前提 0：关掉卡牌进度落盘 —— 本文件只读库存、只摆内存值，⛔ 一个字节都不写盘。
        CardProgress.ResetForTest();

        // ============================================================ ① 已销毁的实例不许把信号打断
        var dead = new GameObject("B103F1_DeadDeckRuntime");
        bool hookedBeforeKill = false;
        try
        {
            var deadRt = dead.AddComponent<DeckRuntime>();
            deadRt.Build(new DeckLibrary());
            hookedBeforeKill = deadRt.WcSignalHookedForTest;   // = 它真的订在静态事件上（否则下面那条等于没查）
        }
        finally
        {
            // ⚠️ 故意**不**手工退订：本条的正是「批处理里 `DestroyImmediate` 不派 `OnDestroy` ⇒ 退订没发生」这一态。
            //   （Play 模式下这里会走 `OnDestroy` 正常退订 —— 那样下面那条同样该绿，只是少了鉴别力。）
            UnityEngine.Object.DestroyImmediate(dead);
        }
        C("B103F1·前提：第 1 组那具 `DeckRuntime` 在被销毁**之前**确实订着 `Wildcards.Updated`（否则第 1 组空转）",
          hookedBeforeKill, "WcSignalHookedForTest=" + hookedBeforeKill);

        Exception caught = null;
        try
        {
            // = 原版 `Signal.Raise(WildcardUpdateSignal)`；改前这里会在一具尸体上抛 `MissingReferenceException`
            Inventories.Wildcards.UpdateItemCollection();
        }
        catch (Exception ex) { caught = ex; }
        C("B103F1·★ ① 库存信号打到**已销毁**的 `DeckRuntime` 上**不抛**（批处理里 `DestroyImmediate` 不派 `OnDestroy` ⇒ 静态事件会留尸体；"
        + "`OnWildcardsUpdated` 那道 `Root == null` 闸要当场退订并出声）",
          caught == null,
          caught == null ? "没有抛（= 闸生效）" : (caught.GetType().Name + " · " + caught.Message));

        // ============================================================ ② 活的实例照旧要刷（⛔ 别把修法做成「干脆不刷」）
        var live = new GameObject("B103F1_LiveDeckRuntime");
        string army = null;
        int rareWas = 0;
        try
        {
            var rt = live.AddComponent<DeckRuntime>();
            rt.Build(new DeckLibrary());
            army = DeckRuntime.DeckArmyName(rt.State);
            C("B103F1·前提：拿得到这副卡组的阵营名（空 ⇒ 第 2 组空转）",
              !string.IsNullOrEmpty(army), "army=" + (army ?? "null"));

            rareWas = CardProgress.Wildcards(army, "rare");
            CardProgress.SetWildcardsForTest(CardProgress.WildKey(army, "rare"), 7);
            Inventories.Wildcards.UpdateItemCollection();      // = 原版 `Signal.Raise(WildcardUpdateSignal)`

            // 期望值 = **注入的字面量** 7（⛔ 不从被测实现里读回来）
            C("B103F1·★ ② 活的 `DeckRuntime` 收到信号后四格照旧刷（`SetCountersText` 那半仍在；`Rare` = 注入的 7）",
              rt.UiWcText(1) == "7",
              "UiWcText(1)=" + (rt.UiWcText(1) ?? "null") + "（应为 \"7\"；\"99\" ⇒ `RefreshHeader()` 那一跳被删了）");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(live);
            if (!string.IsNullOrEmpty(army)) CardProgress.SetWildcardsForTest(CardProgress.WildKey(army, "rare"), rareWas);
        }

        return bad;
    }
}
