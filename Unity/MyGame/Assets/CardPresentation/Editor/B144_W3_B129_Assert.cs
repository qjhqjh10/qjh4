// B144_W3_B129_Assert.cs — `B144-W3` · **B129 审计**里本轮修掉/复核过的参数断言（只读、零前提）
//
// ============================ 盯的是什么 ============================
//  ① 选牌面板**卡行槽位间距**的 `n≥8` 那一档（B129 二·#9）：
//     原版 `CardsHorizontalLayout__GetPosition.c:149,201-210`：`spacing = min(lossyScale.x*(+0x48), 可用宽/n)`，
//     可用宽由 n=4..7 的实测反推 = **1882.4** ⇒ n≥8 = 1882.4/n（n=8 ⇒ 235.3）。
//     我们改前 `SpacingPx(n≥7) 恒 268.9`（外推）⇒ 本条盯住 n=8/9/10 与单调性。
//     落点 = `CardPresentation/Battle/CardChoicePanel.cs` 的 `SpacingPx`（✅ 已复刻 2026-11-05 · B144-W3）。
//  ② 两条**已修**项的回归钉子（B129 一·★11 / 三·★19）：
//     `TapThresholdPx` 必须 = `PopDragThresholdPx` = **5 px**（原版 `Intro.unity:5429` 的 `m_DragThreshold: 5`）。
//
// ============================ 怎么接 ============================
//  `B144_W3_B129_Assert.Run();`（静态类 + 无参 `Run()`；全局命名空间、类名 == 文件名）
//  ⚠️ 本文件**零前提**：只调纯函数 / 读常量，不建窗口、不碰存档、不跑引擎。
//  ⚠️ 宿主接线**不在本写手白名单**（`Editor/*Scene.cs` 与 `RuleEngineTest.cs` 都不可改）⇒ 待调度台接线。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A2/A3/A4：把 `SpacingPx` 的 `n >= 8` 分支删掉（回 `return 268.9f`）⇒ 三条全红（268.9 ≠ 235.3）。
//   · A5：把 n=8 那一档写成**大于** n=7（或把公式改成 `1882.4×(n-7)`）⇒ 单调性红。
//   · B1/B2：把 `PopDragThresholdPx` 改回 10（旧值）⇒ B1 红；把 `TapThresholdPx` 写死 10 ⇒ B2 红。
using System;
using System.Collections.Generic;
using UnityEngine;
using CardPresentation;

public static class B144_W3_B129_Assert
{
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    const string Tag = "[B144W3B129]";

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static void Near(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol, msg + "（实得 " + got.ToString("F4") + " / 期望 " + want.ToString("F4") + "）");
    }

    public static void Run()
    {
        _pass = _fail = 0;

        // ---------------- ① 卡行槽位间距（B129 二·#9） ----------------
        // 前四档 = B129 实测值（n≤3 / 4 / 5 / 6 / 7），本轮**一个字没动**，先钉住它们证明「只动了 n≥8」。
        Near(CardChoicePanel.SpacingPx(3), 502.2f, 0.01f, "A1 `SpacingPx(3)` = 502.2（实测档，未动）");
        Near(CardChoicePanel.SpacingPx(4), 470.6f, 0.01f, "A1b `SpacingPx(4)` = 470.6（实测档，未动）");
        Near(CardChoicePanel.SpacingPx(5), 376.5f, 0.01f, "A1c `SpacingPx(5)` = 376.5（实测档，未动）");
        Near(CardChoicePanel.SpacingPx(6), 313.7f, 0.01f, "A1d `SpacingPx(6)` = 313.7（实测档，未动）");
        Near(CardChoicePanel.SpacingPx(7), 268.9f, 0.01f, "A1e `SpacingPx(7)` = 268.9（实测档，未动）");

        // n≥8：原版通用式 `可用宽/n`（可用宽 = 1882.4）。
        Near(CardChoicePanel.SpacingPx(8), 1882.4f / 8f, 0.01f,
             "A2 ★ `SpacingPx(8)` = **1882.4/8 = 235.3**（原版 `CardsHorizontalLayout__GetPosition.c:201-210` 的 `可用宽/n`）"
           + "｜🧨 回 `return 268.9f`（n≥8 恒 268.9 的旧外推）⇒ 本条红");
        Near(CardChoicePanel.SpacingPx(9), 1882.4f / 9f, 0.01f,
             "A3 ★ `SpacingPx(9)` = 1882.4/9 ≈ 209.16｜🧨 同上 ⇒ 红");
        Near(CardChoicePanel.SpacingPx(10), 1882.4f / 10f, 0.01f,
             "A4 ★ `SpacingPx(10)` = 188.24｜🧨 同上 ⇒ 红");

        // 单调性：n 越大间距不能变大（原版是 min(常数, 可用宽/n)）。
        bool mono = true;
        for (int n = 7; n < 12; n++)
            if (CardChoicePanel.SpacingPx(n + 1) > CardChoicePanel.SpacingPx(n) + 1e-3f) mono = false;
        Check(mono, "A5 ★ n=7..12 间距**单调不增**（`min(常数, 可用宽/n)` 的形状）"
                  + "｜🧨 把 n=8 写成大于 n=7（或改成 `1882.4×(n-7)`）⇒ 本条红");

        // ---------------- ② 两条已修项的回归钉子（B129 一·★11 / 三·★19） ----------------
        Near(AttackSelector.PopDragThresholdPx, 5f, 1e-6f,
             "B1 `PopDragThresholdPx` = **5 px**（B129 一·★11 已修；原版 `Intro.unity:5429` 的 `m_DragThreshold: 5`）"
           + "｜🧨 改回 10 ⇒ 红");
        Near(CardInteraction.TapThresholdPx, AttackSelector.PopDragThresholdPx, 1e-6f,
             "B2 ★ `TapThresholdPx` == `PopDragThresholdPx`（手牌轻点/拖拽阈值**同一份判据**，B129 一·★11 的收口）"
           + "｜🧨 把 `TapThresholdPx` 写死回 10 ⇒ 红");

        // ---------------- ③ 多卡展示窗：点卡片 ≠ 点背景（B129 二·★33） ----------------
        //  判据：原版卡片挂 `IPointerClickHandler`（`CardScript.OnTouchUpAsButton`）+ `EverguildPhysicsRaycaster`
        //  ⇒ 射线先被卡片接住、`BackgroundCloseButton` 收不到 ⇒ **点卡片不关窗**。
        //  落点：`MultiCardDisplay.ShouldCloseOnPointer` / `AnyCardHit`（✅ 已复刻 2026-11-05 · B144-W3）。
        Check(!MultiCardDisplay.ShouldCloseOnPointer(overCard: true, dragged: false),
              "C1 ★ 点**卡片**（`overCard=true`）⇒ **不关窗**（原版卡片自己吃点击）"
            + "｜🧨 把判据改成只看 `dragged` ⇒ 本条红");
        Check(MultiCardDisplay.ShouldCloseOnPointer(overCard: false, dragged: false),
              "C2 点**背景** ⇒ 关窗");
        Check(!MultiCardDisplay.ShouldCloseOnPointer(overCard: false, dragged: true),
              "C3 **拖拽抬手** ⇒ 不关窗（原行为，B129 二·★33 之前就这样）");

        {
            var root = new GameObject("B144W3B129 root");
            try
            {
                var data = new CardData
                {
                    id = "B144W3CARD", title = "B144W3CARD", cost = 1, melee = 1, ranged = 1,
                    health = 3, armor = 0, isUnit = true, type = "unit", faction = "Test",
                };
                var cv = CardView.Create(root.transform, data, "probe");
                Check(cv != null, "C4（前提）夹具卡建出来了（`CardView.Create`）");
                if (cv != null)
                {
                    cv.transform.position = new Vector3(2f, 1f, 0f);
                    var list = new List<CardView> { cv };
                    Check(MultiCardDisplay.AnyCardHit(list, new Vector3(2f, 1f, 0f)),
                          "C5 ★ 指针落在卡上 ⇒ `AnyCardHit` = true（= 原版「卡片吃掉点击」的等价物；"
                        + "走的是 `CardView.HitByCollider`，与战斗侧 `CardHit` 同一颗 `BoxCollider2D`）"
                        + "｜🧨 把 `AnyCardHit` 写成恒 false（或把卡片的 collider 关掉）⇒ 红");
                    Check(!MultiCardDisplay.AnyCardHit(list, new Vector3(30f, 1f, 0f)),
                          "C6 指针在远处 ⇒ `AnyCardHit` = false｜🧨 恒 true（那样点背景永远关不了窗）⇒ 红");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        // ---------------- ④ 安全窗：离开整条后重进同一颗（B129 一·#15） ----------------
        //  判据：原版 `…OnPointerExit.c:12-37` 只对**兄弟钮** `DisableTemporary`；被离开那颗自己的
        //  `+0x90`（sendInput）保持 1 ⇒ 重进同一颗立即可发；换到别的格才重开 0.1 s。
        Check(!AttackSelector.ShouldRearmSafeWindow(AttackKind.Melee, AttackKind.None, AttackKind.Melee),
              "D1 离开整条（→None）⇒ **不重开**安全窗（原版自己那颗 `+0x90` 保持 1）"
            + "｜🧨 把判据写回「任何变化都重开」（旧行为）⇒ D1/D2 红");
        Check(!AttackSelector.ShouldRearmSafeWindow(AttackKind.None, AttackKind.Melee, AttackKind.Melee),
              "D2 ★ 离开后**重进同一颗** ⇒ 不重开（立即可发；原版该格 `+0x90` 一直为 1）");
        Check(AttackSelector.ShouldRearmSafeWindow(AttackKind.Melee, AttackKind.Ranged, AttackKind.Melee),
              "D3 换到**别的格** ⇒ 重开 0.1 s（原版 `OnPointerExit` → 兄弟钮 `DisableTemporary`）");
        Check(AttackSelector.ShouldRearmSafeWindow(AttackKind.None, AttackKind.Ranged, AttackKind.Melee),
              "D4 离开整条后进**另一颗** ⇒ 重开（那颗被兄弟钮那一跳临时禁用了）");

        Debug.Log(Tag + " 合计：" + _pass + " 通过 / " + _fail + " 失败");
    }
}
