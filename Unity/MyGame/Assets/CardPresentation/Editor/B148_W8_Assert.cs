// B148_W8_Assert.cs — 执行写手 `B148_W8`（战斗表现域 · 2026-11-05）
//
// ============================ 盯的是什么（三件）============================
//  ① `S3-MOVETOPOS-145` + `CREATEATPOS-18`：造牌进牌库的**新纯表现事件** `EvtKind.CardCreatedIntoDeck`
//     （发出口 3 处 · 走 `ctx.Signals` · 消费端 = `BattleDriver.PlayCardCreatedIntoDeckFeel`）。
//     判据（逐跳）→ `判据/B148_战斗表现施工单.md` §1·2。
//  ② `战-T09` S3/S4：两台相机各一颗 `Physics2DRaycaster` + 卡上 8 个指针接口的转发件
//     + 「送达到 EventSystem、语义仍走既有轮询」的**唯一共存开关** `BattlePointerRouter.Available`。
//     判据 → `判据/B148_战斗表现施工单.md` §2·1 / §2·2。
//  ③ `PlayerHand.FinishMulligan` 的面板收掉时机（判据 → `判据/B148_账级核_L1战斗逻辑.md` §5 的降 C 理由②）。
//
// ============================ 怎么接（⛔ 本文件不接线）============================
//  `public static void Run()`：**零前提**（不建相机、不读存档、不写 PlayerPrefs；只做反射 + 读源文件）。
//  建议宿主（由调度台定）：`Editor/BattleScene.cs` 的聚合行**同一物理行**追加：
//      B148_W8_Assert.Run(); _sink.Pass += B148_W8_Assert.Pass; _sink.Fail += B148_W8_Assert.Fail;
//  判绿红：按行首 `B148W8 ✓` / `B148W8 ✗` 数（⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A-1：把 `BattleEvent.CreateAtPos/MoveToPos` 的初值从 `-1` 改成 `0` ⇒ 红（`0` 是合法档号 = self，
//    会让「没有数据」被当成「贴自己身上」，正是本条要挡的静默错）。
//  · A-2：把 `CardCreatedIntoDeck` 登记成 `ZeroDurationKinds`（或删掉 `DurationOf` 的 case）⇒ 红。
//  · A-3：删掉任意一处 `EmitCardCreatedIntoDeck(ctx, …)` 调用（三个造牌落点少发一处 = 某张卡的演出静默消失）⇒ 红。
//  · A-4：把那句 `ctx.Signals.Add(` 改成 `ctx.Emit(` ⇒ 红（本档刻意**不进** `ctx.Events`/动作日志）。
//  · B-1：`CardPointerRelay` 少实现一个指针接口（原版 8 个，`CardCollider.cs:7`）⇒ 红。
//  · B-4：把 `HandCardPhysicsLayer` 改成 5、或把战场那一颗的掩码写成 32 ⇒ 红。
//  · C-1：把 `BeginSettleHold()` 换回 `_mulligan.Close()`（= 面板又比牌先收）⇒ 红。
//
// ============================ 已知偏离（如实记，⛔ 不是没查）============================
//  · 手牌卡在我们这边是**层 0**（原版 `2DCard` = 层 5 ⇒ 原版掩码 32）；我们用 `1<<0`。语义等价，
//    一比一回到「层 5 + 32」要连带改两台相机 cullingMask —— 那是另一条任务（见 `BattleEventSystem.HandCardPhysicsLayer` 的 doc）。
//  · 换牌收尾的「新牌淡入」这一层**本批未做**（原版那一段受 `fadeMulliganCards` 闸、默认 false；
//    现调用点未传真 ⇒ 按原版不实现，见 `BattleDriver` 换牌那一段的块注释）。

using System;
using System.IO;
using System.Reflection;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行写手 `B148-W8` 的验收点（造牌进牌库演出 · 卡格迁 EventSystem · 换牌面板收掉的时机）。</summary>
public static class B148_W8_Assert
{
    const string Tag = "B148W8";
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static string ReadSource(string rel)
    {
        var p = Path.Combine(Application.dataPath, rel);
        try { return File.Exists(p) ? File.ReadAllText(p) : null; }
        catch (Exception e) { Debug.LogError(Tag + " 读源码失败: " + p + " (" + e.GetType().Name + ")"); return null; }
    }

    static int CountOf(string text, string needle)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(needle)) return 0;
        int n = 0, at = 0;
        while ((at = text.IndexOf(needle, at, StringComparison.Ordinal)) >= 0) { n++; at += needle.Length; }
        return n;
    }

    /// <summary>取 `name` 那一段源码（从声明起、到下一个 `\n        void ` / `\n        bool ` / `\n        public ` 之前）。</summary>
    static string MethodSlice(string text, string decl, int maxChars)
    {
        if (string.IsNullOrEmpty(text)) return null;
        int i = text.IndexOf(decl, StringComparison.Ordinal);
        if (i < 0) return null;
        int len = Math.Min(maxChars, text.Length - i);
        return text.Substring(i, len);
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;

        // ------------------------------------------------------------------
        //  A 组：`EvtKind.CardCreatedIntoDeck`（纯表现事件 + 时长登记 + 三处发出口）
        // ------------------------------------------------------------------
        bool hasKind = Enum.IsDefined(typeof(EvtKind), "CardCreatedIntoDeck");
        Check(hasKind, "A-1a（前提）`EvtKind.CardCreatedIntoDeck` 存在"
                    + "｜🧨 删掉那个枚举成员 ⇒ 红（发出口与消费口都会编不过）");
        if (hasKind)
        {
            var kind = (EvtKind)Enum.Parse(typeof(EvtKind), "CardCreatedIntoDeck");
            var ev = new BattleEvent();
            var fCap = typeof(BattleEvent).GetField("CreateAtPos");
            var fMv = typeof(BattleEvent).GetField("MoveToPos");
            Check(fCap != null && fMv != null, "A-1b（前提）`BattleEvent` 有 `CreateAtPos` / `MoveToPos` 两个字段");
            if (fCap != null && fMv != null)
            {
                Check((int)fCap.GetValue(ev) == -1 && (int)fMv.GetValue(ev) == -1,
                      "A-1c ★ 两个档号的**缺省值 = -1**（= 数据里没有）"
                    + "（实得 " + fCap.GetValue(ev) + " / " + fMv.GetValue(ev) + "）"
                    + "｜🧨 改成 `0` ⇒ 红（`0` 是合法档号 `self`，会把「没数据」静默演成「贴自己身上」）");
            }
            Check(EventTiming.IsTimedKind(kind) && !EventTiming.IsZeroDurationKind(kind),
                  "A-2a ★ `CardCreatedIntoDeck` 登记成**有时间**（原版是动作队列里的一拍 `AddAutoActionToQueue`）"
                + "｜🧨 登记进 `ZeroDurationKinds` ⇒ 红（下一拍会在卡还在半空时开场）");
            float want = CardFeel.CreateDeckCardFlySeconds + CardFeel.CreateDeckCardToPileSeconds;
            float got = EventTiming.DurationOf(kind);
            Check(Mathf.Abs(got - want) < 1e-4f && Mathf.Abs(want - 1.2f) < 1e-4f,
                  "A-2b ★ `DurationOf` = 0.9（`timeToDraw 0.3 × 3.0`）+ 0.3（`MoveToDeck` 默认档）= **1.2 s**"
                + "（实得 " + got.ToString("F3") + "）｜🧨 删掉 `DurationOf` 的 case ⇒ 红");
            Check(!string.IsNullOrEmpty(EventTiming.SourceOf(kind)) && EventTiming.SourceOf(kind).IndexOf("MoveToDeck", StringComparison.Ordinal) >= 0,
                  "A-2c `SourceOf` 有这条的判据文案（且引到 `MoveToDeck` 那一跳）｜🧨 删掉 `SourceOf` 的 case ⇒ 红");
        }

        var resolver = ReadSource("RuleEngine/Core/EffectResolver.cs");
        Check(resolver != null, "A-3（前提）读得到 `EffectResolver.cs`");
        if (resolver != null)
        {
            int calls = CountOf(resolver, "EmitCardCreatedIntoDeck(ctx, owner,");
            Check(calls == 3, "A-3 ★ 三个「造牌进牌库」落点**各发一条**（定义 0 次调用形参名不同 ⇒ 只数 `(ctx, owner,`）"
                            + "（实得 " + calls + " 处）"
                            + "｜🧨 删掉任一处 ⇒ 那张卡的演出静默消失 ⇒ 红");
            var body = MethodSlice(resolver, "static void EmitCardCreatedIntoDeck(", 4000);
            Check(body != null && body.IndexOf("ctx.Signals.Add(", StringComparison.Ordinal) >= 0,
                  "A-4a ★ 发出口走 `ctx.Signals.Add(`（**纯表现**，不进动作日志）｜🧨 改成 `ctx.Emit(` ⇒ 红");
            Check(body != null && body.IndexOf("ctx.Emit(", StringComparison.Ordinal) < 0,
                  "A-4b 发出口**没有**用 `ctx.Emit(`（同一条的两面：Emit 会顺手 `AppendLog`）");
        }

        var driver = ReadSource("CardPresentation/Battle/BattleDriver.cs");
        Check(driver != null, "A-5（前提）读得到 `BattleDriver.cs`");
        if (driver != null)
        {
            Check(CountOf(driver, "EvtKind.CardCreatedIntoDeck && animateFeel") == 1,
                  "A-5 ★ 消费口接上了（`PlaySignal` 里那一句 `else if`）"
                + "｜🧨 删掉那句 ⇒ 事件发了没人播 ⇒ 红");
            Check(CountOf(driver, "PlayCardCreatedIntoDeckFeel(e);") >= 2,
                  "A-6 消费方法定义 + 调用各一处（定义 + `PlaySignal`）");
        }

        // ------------------------------------------------------------------
        //  B 组：`战-T09` S3/S4 —— 两台相机各一颗射线器 + 8 个指针接口的转发件
        // ------------------------------------------------------------------
        var ifaces = typeof(CardPointerRelay).GetInterfaces();
        string[] want8 =
        {
            "IPointerEnterHandler", "IPointerExitHandler", "IPointerDownHandler", "IPointerUpHandler",
            "IPointerClickHandler", "IBeginDragHandler", "IDragHandler", "IEndDragHandler"
        };
        int have = 0;
        foreach (var w in want8)
            foreach (var i in ifaces)
                if (i.Name == w) { have++; break; }
        Check(have == 8, "B-1 ★ `CardPointerRelay` 实现了原版 `CardCollider` 的**全部 8 个**指针接口"
                       + "（`CardCollider.cs:7`）（实得 " + have + "/8）"
                       + "｜🧨 少实现一个 ⇒ 那一类事件永远送不到 ⇒ 红");

        Check(BattleEventSystem.HandCardPhysicsLayer == 0,
              "B-2 ★ 手牌卡物理层 = **0**（我们的层；原版 `2DCard` = 5 ⇒ 原版掩码 32）—— 这是**如实记的偏离**"
            + "｜🧨 改成 5 ⇒ 掩码打不到手牌（它们在层 0）⇒ 红");
        Check((1 << ArenaSlots.BoardCardLayer) == 1024,
              "B-3 ★ 战场相机那一颗的掩码 = `1<<BoardCardLayer` = **1024**（= 原版 `m_EventMask: 1024` **等值**）"
            + "｜🧨 写成 32 ⇒ 红");

        int added = BattleEventSystem.EnsurePhysicsRaycasters(null, null);
        Check(added == 0, "B-4 ★ 两台相机都为 null 时 `EnsurePhysicsRaycasters` 答 0 且不抛（2D 兜底档）"
                        + "（实得 " + added + "）");

        BattlePointerRouter.Reset();
        Check(!BattlePointerRouter.Available && BattlePointerRouter.HoverCard == null,
              "B-5 ★★ `BattlePointerRouter.Reset()` 之后 `Available == false`（= 消费端照旧轮询 `Frontmost`/`HitSlot`）"
            + " —— 这就是「uGUI 送达」与「世界坐标轮询」的**唯一共存开关**"
            + "｜🧨 把 `Available` 的初值写成 true ⇒ 批处理/兜底档会去读一个永远为空的 `HoverCard` ⇒ 红");
        BattlePointerRouter.Note(null, BattlePointerRouter.Edge.Enter, null);
        Check(BattlePointerRouter.HoverCard == null, "B-6 送 `null` 卡进路由不会把 `HoverCard` 写脏");

        var cardView = ReadSource("CardPresentation/Core/CardView.cs");
        Check(cardView != null && CountOf(cardView, "CardPointerRelay.Attach(v)") == 1,
              "B-7 ★ 建卡口（`CardView.Create`）挂上了转发件，且**只挂一处**（原版每张卡一只 `CardCollider`）"
            + "｜🧨 删掉那一句 ⇒ `EventsDeliveredForTest` 恒 0 ⇒ 红");

        var interaction = ReadSource("CardPresentation/Hand/CardInteraction.cs");
        Check(interaction != null && interaction.IndexOf("RoutedHover() ?? Frontmost(_cards, world)", StringComparison.Ordinal) >= 0,
              "B-8 ★ `CardInteraction.HitTest` 先问 uGUI 送达、问不到才退回 `Frontmost`（兜底档逐位不变）"
            + "｜🧨 把 `RoutedHover()` 去掉 ⇒ 送达白接 ⇒ 红");

        var mulligan = ReadSource("CardPresentation/Battle/MulliganPanel.cs");
        Check(mulligan != null && mulligan.IndexOf("BattlePointerRouter.HoverCard", StringComparison.Ordinal) >= 0,
              "B-9 ★ 换牌区 `CardAt` 也走同一开关（且只认 `_cards` 里那几张）");

        // ------------------------------------------------------------------
        //  C 组：`PlayerHand.FinishMulligan` 的面板收掉时机
        // ------------------------------------------------------------------
        if (driver != null)
        {
            var begin = MethodSlice(driver, "bool BeginMulliganSettle(List<CardView> views)", 1600);
            Check(begin != null && begin.IndexOf("BeginSettleHold()", StringComparison.Ordinal) >= 0,
                  "C-1 ★ `BeginMulliganSettle` 用 `BeginSettleHold()`（只收可点件、⛔ 不还位姿、**不关面板**）"
                + "｜🧨 换回 `_mulligan.Close()` ⇒ 面板又比牌先收 ⇒ 红");
            Check(begin != null && begin.IndexOf("_mulligan.Close()", StringComparison.Ordinal) < 0,
                  "C-1b 同一段里**没有** `_mulligan.Close()`（面板收掉的时机已在 `FinishMulliganSettle`）");
            var fin = MethodSlice(driver, "void FinishMulliganSettle()", 1200);
            Check(fin != null && fin.IndexOf("_mulligan.Close()", StringComparison.Ordinal) >= 0,
                  "C-2 ★ `FinishMulliganSettle`（全部飞完 + 尾拍之后）才 `Close()` —— 与原版 `CompleteMulliganPhase` 同序");
        }
        if (mulligan != null)
        {
            Check(mulligan.IndexOf("public void BeginSettleHold()", StringComparison.Ordinal) >= 0
               && mulligan.IndexOf("ClearCardButtons(restorePoses: false)", StringComparison.Ordinal) >= 0,
                  "C-3 ★ `MulliganPanel.BeginSettleHold()` 走 `restorePoses: false`（⛔ 不把正飞回牌堆的那几张拉回原位）");
        }

        var ruleCore = ReadSource("RuleEngine/Core/RuleCore.cs");
        Check(ruleCore != null && ruleCore.IndexOf("for (int i = 0; i < idx.Count; i++)", StringComparison.Ordinal) >= 0,
              "C-4 ★ 引擎侧换牌**已是逐张**（`RuleCore.Mulligan` 的循环：摘一张 → 回库 → 补一张 → 原位放回，2026-11-02 `B117-WMULL`）"
            + "｜🧨 改回「整批换完再一次性补抽」⇒ 账级核降 C 的理由① 复活 ⇒ 红");

        Debug.Log(Tag + " —— 合计 通过 " + _pass + " · 失败 " + _fail);
    }
}
