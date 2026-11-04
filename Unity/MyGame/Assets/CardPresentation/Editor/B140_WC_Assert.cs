// B140_WC_Assert.cs — 执行写手 `B140_WC`（2026-11-04）的两件离线活：
//   ① 账 `B124-RETHOLD`：`EventTiming.ReturnHoldOf` 的**回牌库**那一支按【实演】重算
//      （0.9167 + 0.4 = 1.3167，改前 0.55 + 0.4 = 0.95）；
//   ② 自检-X5「延后生成 / 若证实则修」：`Battle/Label.TryApplyPendingWrap` 那一刀**收敛化**
//      + `OnEnable` 之后补一次 `ForceMeshUpdate`。
//
// 直接跑（类在**全局命名空间**、类名 == 文件名、不依赖任何宿主场景）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B140_WC_Assert.Run -logFile -
//
// 判绿红：按**行首** `B140WC ✓` / `B140WC ✗` 数（⛔ 别 `grep -c ✗` —— `✗` 会出现在文案里）。
//
// ============================ 它盯的是什么 ============================
//  A 组（时序）—— 全部期望值都写成**别处的量**，⛔ 不抄一个字面量当期望：
//    · 回手 = `ReturnCardAnimHold + ReturnHandRelayoutHold`（另一支不变）；
//    · 回牌库 = `CardFeel.HandToBoardEnd + EventTiming.MoveToDeckTotal`（= 第一段**实演** 0.9167 + 第二段 0.4）；
//    · `teleport` 两档 = 上面各 + `ReturnCardAnimHold`（叠加项一个字没动）；
//    · `Slot < 0` 三档（墓地取回 / `disappearFromHand` / `ephemeral`）= 0.3 / 0.3 / 0；
//    · `DurationOf(EvtKind.Return)`（**拿不到事件**那条重载）= 回手基准 0.75 —— 它没跟着变，
//      证明这次只动了「按目的地分档」那一半（不把别处的 `EvtKind.Return` 一并推后）。
//  B 组（标签）—— 受控夹具（`Label.Create` + `SetAutoFitBox`，框小、文本长 ⇒ 必须二分缩字号）：
//    · 前提：单跑一遍 `TmpFont.GenerateLayout`（= 改前那条路）**排不出自适应档** ⇒ `textBounds` 不是测量值；
//    · 收敛化之后（走真实那条 `OnEnable`）⇒ 量到真尺寸（`_tmpW` 不再是 `1e-4`）、且没记「不收敛」；
//    · 🧨 灭自证：打开 `Label.DisableWrapConvergenceForTest`（退回单跑一遍）⇒ 同一套夹具**必须**量不到。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//  · `ReturnHoldOf` 把回牌库改回 `ReturnCardAnimHold + MoveToDeckTotal`（0.95）⇒ A3 红（A3 明写「不许是 0.95」）。
//  · 反过来把 0.55 与 0.9167 **一起加**（1.8667，多推后 0.55）⇒ A3 红（A3 断的是**等式**，不是大小）。
//  · 顺手把回手支也改成 0.9167 + 0.2 ⇒ A1 红（A1 断回手 = 0.55 + 0.2，逐项相等）。
//  · 把 `MoveToDeckTotal` 改短（例如塞进 0.95 的余量 ⇒ 0.0333）⇒ A4 红（A4 断第二段仍是 0.4）。
//  · 把 `Label.ApplyPendingWrapConverged` 改回「单跑一遍」⇒ B4/B5 红（前提项与灭自证项结构上不可能同时满足）。
//  · 把 `OnEnable` 里那句 `ForceMeshUpdateOnce()` 删掉 ⇒ 本件**不红**（它是网格那一半，批处理里量不到画面）
//    —— 如实写明：这一条只能靠真机/Unity 渲图验收，⛔ 别假装断言覆盖了它。
//
// ⚠️ 本件**不产生持久副作用**：两个夹具根节点当场 `DestroyImmediate`；两个静态钩子 / 测试开关
//   在 `finally` 里逐字复原。
using System;
using TMPro;
using UnityEngine;
using CardPresentation;
using RuleEngine;

public static class B140_WC_Assert
{
    const string Tag = "B140WC";

    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static void Section(string t) { Debug.Log("--- " + Tag + " · " + t + " ---"); }

    // ==================================================================
    //  入口
    // ==================================================================
    public static void Run()
    {
        _pass = _fail = 0;
        Func<BattleEvent, int> prevDest = EventTiming.ReturnDestOf;
        Func<string, string, bool> prevTrait = EventTiming.HasTraitOf;
        bool prevDisable = Label.DisableWrapConvergenceForTest;
        try
        {
            Debug.Log("=== " + Tag + " 开始（B124-RETHOLD 时序 + 自检-X5 标签收敛）===");
            Timing();
            LabelConvergence();
        }
        finally
        {
            EventTiming.ReturnDestOf = prevDest;
            EventTiming.HasTraitOf = prevTrait;
            Label.DisableWrapConvergenceForTest = prevDisable;
            Debug.Log("=== " + Tag + " 结束：通过 " + _pass + " · 失败 " + _fail + " ===");
        }
    }

    // ==================================================================
    //  A 组：`ReturnHoldOf`（B124-RETHOLD）
    // ==================================================================
    static BattleEvent E(string cardId, int slot = 4, string effect = null)
    {
        return new BattleEvent { Kind = EvtKind.Return, Player = 0, Slot = slot, CardId = cardId, Effect = effect };
    }

    static void Timing()
    {
        Section("A 时序 · `EventTiming.ReturnHoldOf`（账 `B124-RETHOLD`）");

        // 本组要临时接管两个静态钩子 —— 进来先存一份，出去逐字复原（`Run()` 那一层也会复原，双保险）
        Func<BattleEvent, int> prevDest = EventTiming.ReturnDestOf;
        Func<string, string, bool> prevTrait = EventTiming.HasTraitOf;

        // 判据链上那三个「别处的量」—— 期望值一律从它们推，⛔ 不另抄一份数
        float firstSegReal = CardFeel.HandToBoardEnd;          // 倒放整条 clip 的实演长度
        float firstSegQueue = EventTiming.ReturnCardAnimHold;   // 原版队列等的那个 AnimationEvent 时刻（回手支仍用它）
        float secondSeg = EventTiming.MoveToDeckTotal;          // 第二段（`CardScript.MoveToDeck` 的返回值）
        float handTail = EventTiming.ReturnHandRelayoutHold;

        Check(Mathf.Abs(firstSegReal - 0.9167f) < 1e-4f,
              "A0（前提）第一段实演 = `CardFeel.HandToBoardEnd` = 0.9167（实得 " + firstSegReal.ToString("F4") + "）");
        Check(Mathf.Abs(firstSegQueue - 0.55f) < 1e-4f,
              "A0b（前提）第一段「队列值」= `ReturnCardAnimHold` = 0.55（实得 " + firstSegQueue.ToString("F4") + "）");
        Check(Mathf.Abs(secondSeg - 0.4f) < 1e-4f,
              "A0c（前提）第二段 = `MoveToDeckTotal` = 0.4（实得 " + secondSeg.ToString("F4") + "）");

        try
        {
            // 目的地 0 = 手牌；1 = 牌库。`*TP` 那两张额外带 `teleport`。
            EventTiming.ReturnDestOf = e => e != null && (e.CardId == "DECK" || e.CardId == "DECKTP") ? 1 : 0;
            EventTiming.HasTraitOf = (cardId, kw) => kw == KeywordTable.Teleport
                                                     && (cardId == "DECKTP" || cardId == "HANDTP");

            // ---- ① 回手那条【别的分支】一个字不动 ----
            float dHand = EventTiming.ReturnHoldOf(E("HAND"));
            Check(Mathf.Abs(dHand - (firstSegQueue + handTail)) < 1e-4f,
                  "A1 ★ 回手 = `ReturnCardAnimHold` + `ReturnHandRelayoutHold` = " + (firstSegQueue + handTail).ToString("F4")
                + "（实得 " + dHand.ToString("F4") + "）｜🧨 顺手把回手也改成实演 0.9167 ⇒ 红");

            float dHandTp = EventTiming.ReturnHoldOf(E("HANDTP"));
            Check(Mathf.Abs(dHandTp - (dHand + firstSegQueue)) < 1e-4f,
                  "A2 ★ 回手 + `teleport` = 回手 + 0.55 = " + (dHand + firstSegQueue).ToString("F4")
                + "（实得 " + dHandTp.ToString("F4") + "）｜🧨 漏掉 teleport 叠加 ⇒ 红");

            // ---- ② 回牌库 = 第一段【实演】0.9167 + 第二段 0.4（本次要改的那一格）----
            float dDeck = EventTiming.ReturnHoldOf(E("DECK"));
            float wantDeck = firstSegReal + secondSeg;
            Check(Mathf.Abs(dDeck - wantDeck) < 1e-4f,
                  "A3 ★ 回牌库 = 第一段实演 " + firstSegReal.ToString("F4") + " + 第二段 " + secondSeg.ToString("F4")
                + " = " + wantDeck.ToString("F4") + "（实得 " + dDeck.ToString("F4") + "）");
            // 🧨 灭自证：旧值（0.55 + 0.4 = 0.95）与「两段相加再叠 0.55」（1.8667）都必须**不等于**上面那个等式
            Check(Mathf.Abs(dDeck - (firstSegQueue + secondSeg)) > 1e-3f,
                  "A3b 🧨 灭自证：回牌库 **不再是** 0.55 + 0.4 = " + (firstSegQueue + secondSeg).ToString("F4")
                + "（改回旧算式 ⇒ 这一条与 A3 一起红；实得 " + dDeck.ToString("F4") + "）");
            Check(dDeck > firstSegQueue + secondSeg,
                  "A3c ★ 回牌库**只比旧值长**（长出来的是「我们没有 AnimationEvent 截断点」那 0.3667 s —— 见 `ReturnHoldOf` 的 doc）");
            Check(Mathf.Abs(dDeck - (firstSegQueue + firstSegReal + secondSeg)) > 1e-3f,
                  "A3d 🧨 灭自证：**不许**把 0.55 与 0.9167 一起加（那会把下一条事件多推后 0.55 s）");

            // ---- ③ 第二段本身没被「缩短」----
            Check(Mathf.Abs(secondSeg - 0.4f) < 1e-4f,
                  "A4 ★ 第二段仍是 `MoveToDeckTotal` = 0.4（⛔ 没有为了塞进旧窗口把它压成 0.0333）");

            float dDeckTp = EventTiming.ReturnHoldOf(E("DECKTP"));
            Check(Mathf.Abs(dDeckTp - (dDeck + firstSegQueue)) < 1e-4f,
                  "A5 ★ 回牌库 + `teleport` = 回牌库 + 0.55 = " + (dDeck + firstSegQueue).ToString("F4")
                + "（实得 " + dDeckTp.ToString("F4") + "）");

            // ---- ④ `Slot < 0` 三档（不在棋盘上）逐档不变 ----
            float dGrave = EventTiming.ReturnHoldOf(E("HAND", slot: -1));
            Check(Mathf.Abs(dGrave - 0.3f) < 1e-4f,
                  "A6 墓地取回 = 0.3（`ReturnGraveHold`；实得 " + dGrave.ToString("F4") + "）");
            float dDissolve = EventTiming.ReturnHoldOf(E("HAND", slot: -1, effect: "disappearFromHand"));
            Check(Mathf.Abs(dDissolve - 0.3f) < 1e-4f,
                  "A7 `disappearFromHand` = 0.3（实得 " + dDissolve.ToString("F4") + "）");
            float dEph = EventTiming.ReturnHoldOf(E("HAND", slot: -1, effect: "ephemeral"));
            Check(dEph == 0f, "A8 `ephemeral` = 0（实得 " + dEph.ToString("F4") + "）");

            // ---- ⑤ 拿不到事件那条重载**没跟着变**（证明只动了「按目的地分档」那一半）----
            float dKind = EventTiming.DurationOf(EvtKind.Return);
            Check(Mathf.Abs(dKind - (firstSegQueue + handTail)) < 1e-4f,
                  "A9 ★ `DurationOf(EvtKind.Return)` 仍是回手基准 " + (firstSegQueue + handTail).ToString("F4")
                + "（实得 " + dKind.ToString("F4") + "）｜⛔ 别把没有事件的那条路也推后");
        }
        finally
        {
            EventTiming.ReturnDestOf = prevDest;
            EventTiming.HasTraitOf = prevTrait;
        }
    }

    // ==================================================================
    //  B 组：`Label` 的「延后生成」收敛化（自检-X5）
    // ==================================================================
    const string WrapText =
        "Rites of Battle: this line is deliberately far longer than the tiny fixture box, so the " +
        "auto-sizing path must bisect the point size more than once before Phase II can report bounds.";

    /// <summary>受控夹具：**未激活**父链下建标签 + 开一个**小**自适应框（`SetWrapWidth` 只把「生成」记成待办，
    /// 走的就是 A266 那条路）。返回根节点（调用方负责 `SetActive(true)` 与销毁）。</summary>
    static GameObject BuildWrapFixture(out Label lb, out TMP_Text tmp)
    {
        lb = null; tmp = null;
        var root = new GameObject("B140WC_wrapfix");
        root.SetActive(false);                                   // ← 关键：未激活 ⇒ `SetWrapWidth` 记成待办
        lb = Label.Create(root.transform, WrapText, Vector3.zero, 6, Color.white, new Vector2(0.5f, 0.5f), "b140wc_lb");
        if (lb == null) return root;
        var t = lb.transform.Find("text");
        tmp = t != null ? t.GetComponent<TMP_Text>() : null;
        lb.SetAutoFitBox(1.0f, 0.42f, 8f, 30f);                   // 108×45 px 的框、自适应 8~30 px（长文本必须缩字号 + 折行）
        return root;
    }

    /// <summary>`textBounds` 是不是一个**测量值** —— 与 `Label.UsableBounds` + `PushLayoutInactive` 同一条判据
    /// （有限、|x|&gt;1e-3、且在画布尺度内：世界档上限 100 世界单位）。
    /// ⚠️ 这里**照抄判据**而不是调 `Label` 的私有函数；读数来自**被测对象自己的 `textBounds`**（不是我们的常量）。</summary>
    static bool ReadyBounds(Bounds b)
    {
        float w = Mathf.Abs(b.size.x), h = Mathf.Abs(b.size.y);
        if (float.IsNaN(w) || float.IsInfinity(w) || float.IsNaN(h) || float.IsInfinity(h)) return false;
        if (Mathf.Abs(b.min.x) > 100f || Mathf.Abs(b.min.y) > 100f) return false;
        return w <= 100f && h <= 100f && w > 1e-3f;
    }

    static bool ReadyW(float w)
    {
        return !float.IsNaN(w) && !float.IsInfinity(w) && w > 1e-3f && w < 100f;
    }

    static void LabelConvergence()
    {
        Section("B 标签 · `TryApplyPendingWrap` 单跑不收敛 → 收敛化（自检-X5）");

        Check(TmpFont.Available, "B0（前提）TMP 字体资产可用（`TmpFont.Available`）—— 否则本组测的是点阵后端、无意义");
        if (!TmpFont.Available) return;

        GameObject root = null, root2 = null;
        try
        {
            // ---------- ① 收敛化那一档 ----------
            Label.DisableWrapConvergenceForTest = false;
            root = BuildWrapFixture(out var lb, out var tmp);
            Check(lb != null && tmp != null, "B1（前提）夹具建出来了（`Label.Create` + TMP 子件 `text`）");
            if (lb == null || tmp == null) return;
            Check(tmp.enableAutoSizing, "B1b（前提）自适应真的开着（`enableAutoSizing` = " + tmp.enableAutoSizing + "）");

            // 前提：**单跑一遍** `GenerateLayout`（= 改前 `TryApplyPendingWrap` 的全部内容）排不出测量值
            TmpFont.GenerateLayout(tmp);
            Bounds once = tmp.textBounds;
            Check(!ReadyBounds(once),
                  "B2 ★ 前提：**单跑一遍** `GenerateLayout` 排不出自适应档 ⇒ 不是测量值"
                + "（`textBounds.size` = " + once.size.x.ToString("F1") + " × " + once.size.y.ToString("F1") + "）");

            // 走真实那条路：激活 → `OnEnable`（批处理下 Awake/OnEnable 不自动跑 ⇒ 显式 SendMessage 走同一个方法）
            root.SetActive(true);
            lb.SendMessage("OnEnable", SendMessageOptions.DontRequireReceiver);

            Check(lb.PendingWrapAppliedCount >= 1,
                  "B3 ★ `OnEnable` 把那条待办兑现了（`PendingWrapAppliedCount` = " + lb.PendingWrapAppliedCount + "）");
            Check(lb.PendingWrapNotConvergedCount == 0,
                  "B4 ★ 收敛化那一刀**量到了**真 `textBounds`（`PendingWrapNotConvergedCount` = "
                + lb.PendingWrapNotConvergedCount + "）");
            Check(ReadyW(lb.WorldW) && ReadyW(lb.WorldH),
                  "B5 ★ 收敛化之后 `WorldW × WorldH` 是真尺寸 = " + lb.WorldW.ToString("F4") + " × "
                + lb.WorldH.ToString("F4") + "（改前单跑那一档停在 1e-4）");
            Check(ReadyBounds(tmp.textBounds),
                  "B5b ★ `textBounds` 落在画布尺度内（`size` = " + tmp.textBounds.size.x.ToString("F1") + " × "
                + tmp.textBounds.size.y.ToString("F1") + "）");

            // ---------- ② 🧨 灭自证：关掉收敛（退回单跑一遍）----------
            Label.DisableWrapConvergenceForTest = true;
            root2 = BuildWrapFixture(out var lb2, out var tmp2);
            Check(lb2 != null && tmp2 != null, "B6（前提）灭自证夹具建出来了");
            if (lb2 != null && tmp2 != null)
            {
                root2.SetActive(true);
                lb2.SendMessage("OnEnable", SendMessageOptions.DontRequireReceiver);
                Check(lb2.PendingWrapAppliedCount >= 1,
                      "B7 🧨 灭自证档也走到了兑现（`PendingWrapAppliedCount` = " + lb2.PendingWrapAppliedCount + "）");
                Check(lb2.PendingWrapNotConvergedCount >= 1,
                      "B8 🧨 灭自证：退回单跑一遍 ⇒ 这一颗记到「没量到」（`PendingWrapNotConvergedCount` = "
                    + lb2.PendingWrapNotConvergedCount + "）｜把 `ApplyPendingWrapConverged` 改回单跑 ⇒ B4 红");
                Check(!ReadyW(lb2.WorldW),
                      "B9 🧨 灭自证：退回单跑一遍 ⇒ `WorldW` 停在初值 1e-4（实得 "
                    + lb2.WorldW.ToString("E2") + "）｜B5 与 B9 结构上不可能同时满足");
            }
        }
        finally
        {
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
            if (root2 != null) UnityEngine.Object.DestroyImmediate(root2);
        }
    }
}
