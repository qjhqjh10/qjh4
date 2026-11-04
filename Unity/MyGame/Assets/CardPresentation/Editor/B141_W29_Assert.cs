// B141_W29_Assert.cs — 执行代理 `B141-W29`（第一百二十八会话 · 波次 `W129`）
//                       卡组编辑「拖拽 / 滚动」族 19 条（`资料/完全复刻/判据/B141_W129_对账_卡组色彩.md` §一）
//
// ============================ 它盯的是什么 ============================
//  全部落在 `Deck/DeckRuntime.cs` 一个文件里。原版判据（逐句现读，⛔ 不是照截图量）：
//   · **3-05/06/07/25 阈值与方向门** —— uGUI `EventSystem.pixelDragThreshold` = **5**（`Intro.unity:5429`）；
//     越过 5 px 才发 `OnBeginDrag`；`SupportMethods.IsScrollDragThreshold` 吃**当帧** `eventData.delta`，
//     `Angle(delta, Vector2.right) ∈ (60°,120°)` ⇒ 判成滚动。我们原来是：按下即起拖 / ±16.2 px 方框 / 拿累计位移判方向。
//   · **3-08/09 判成滚动要真的转给滚动** —— `DeckEditingPanel__CheckCardSlotDrag.c` 的滚动支 = `EventRelay.SetTarget(relay, scrollRect.gameObject)`；
//     `DeckEditingWindow__CheckCardDrag.c` 的滚动支 = `CollectionDisplay.ResetRelayTarget`（不起拖）。
//     我们原来只 `Debug.Log`（什么都不发生），而且整扇窗没有「按住拖动滚动」。
//   · **3-11 边缘自滚** —— `DragAndDropItem.threshold = 50` / `speed = 1.0`（prefab 实读），
//     `CheckScrollDrag` 是 `verticalNormalizedPosition += Δt × speed`（**归一化/秒**）；我们原来是每帧固定 10 px。
//   · **3-12/29 抽屉** —— `DOAnchorPosX` 无 `SetEase` ⇒ DOTween 默认 **OutQuad（6）**；`Toggle` 只 `SetActive + DOAnchorPosX`
//     ⇒ **滑行途中不屏蔽点击**（我们原来是线性 + 屏蔽）。
//   · **3-14/15 字符上限** —— 卡组名 **26**；卡名筛选 **无上限**（我们原来两个共用 24）。
//   · **3-16/17 张数条** —— `DeckEditorCollectionDisplay__DrawCell.c` = `Count(GetLibraryInFull())` + **恒** `{0}/{1}`；
//     我们原来只数 `CardIds`、无督军那档印 `xN`。
//   · **3-19 保存失败模态窗** —— 与横幅同源（`DeckRules.ErrorTextKey` 的**具名族**）；我们原来走数字族 `MenuDeck/Error/{2,4,5}`。
//   · **3-21 空库开编辑器** —— 原版 `CardDeck__GetEmptyDeck` = **空卡组**；我们原来塞一副演示满编（`PlayerDeckForDemo`）。
//   · **3-23 卡池格命中** —— 原版 `UI Collider` = **整格 262.5×384**；我们原来还多判一层卡自己的小矩形。
//   · **3-26/27/32 行拖** —— 落点看 `hovered` 的 `IDropHandler`（Deck Details 面板矩形）；牌在**起拖那一帧**就 `RemoveCard`；
//     `TryRemove` 失败要出声（原版那一支不会失败）。
//
// ============================ 🔴 灭自证（🧨 什么坏法会让哪条红）============================
//  · A1/A2 钉死「**5**」：把阈值改回 16.2（`ArmPxSlop` / `0.15`）⇒ A1/A2 红。
//  · A3 两档**结构上不可能同时成立**：「横向 100 px ⇒ 不是滚动」与「竖向 100 px ⇒ 是滚动」——把方向门删掉（恒 true/恒 false）必红一边。
//  · A4 钉「归一化/秒 = 1.0」且钉「边缘带 50」：把速度改回 10 px/帧 ⇒ A4 红。
//  · A7 直接读源码文本钉**旧写法真的不在了**（`if (!v.Contains(wp))` / `_nameEdit.Length < 24` / `MenuDeckErrorKey(err),` /
//    `_newDeck = PlayerDeckForDemo(NewState());` / `Mathf.MoveTowards(d.Slide` / `DragEdgeSpeedPx`）—— 回退任何一条 ⇒ 对应行红。
//  · B 段是**实例级**（要真建出编辑器）：`UiRowDragTo` 走的是鼠标那条路的**同一段判据**，
//    「拖到面板里 ⇒ 张数不变」与「拖到面板外 ⇒ 张数 −1」是一对，删掉任一半都必红一边。
//  · ⚠️ B 段的前提逐条**先断**（不写 `if (x != null) Check(...)` 那种静默跳过 —— 见 `CLAUDE.md` 那族）；前提不成立 ⇒ C/SKIP 行出声。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `Editor/DeckScene.cs` 里（`_rt` 那一档，与 `B141_W26_Assert.Run(_rt)` 同一行风格）加：
//      B141_W29_Assert.Run(_rt); _sink.Pass += B141_W29_Assert.Pass; _sink.Fail += B141_W29_Assert.Fail;
//  ⚠️ 也可以只跑纯函数那一半：`B141_W29_Assert.Run()`（不建窗、不需要 `_rt`；B 段会打一条 SKIP）。
//  判绿红：按行首 `B141W29 ✓` / `B141W29 ✗` 数（⛔ 别 `grep -c ✗`）；SKIP 行是 `B141W29 – `。

using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B141-W29`：卡组编辑拖拽/滚动族 19 条（阈值 5 px + 方向门 · 转给滚动 · 自滚 Δt·1.0 · OutQuad · 上限 26/∞ · 张数全库 · 具名错误键 · 空卡组 · 整格命中 · 起拖即摘牌）。</summary>
public static class B141_W29_Assert
{
    const string Tag = "B141W29";
    static int _pass, _fail, _skip;

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _fail; } }
    /// <summary>因前提不成立而没跑的条数（**出声**，不是静默跳过）。</summary>
    public static int Skip { get { return _skip; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static void SkipLine(string msg)
    {
        _skip++; Debug.Log(Tag + " – SKIP " + msg);
    }

    static void Near(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol,
              msg + "（" + got.ToString("F4") + " ≈ " + want.ToString("F4") + " ± " + tol.ToString("F4") + "）");
    }

    /// <summary>纯函数那一半（不建窗）。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0; _skip = 0;
        Debug.Log(Tag + " --- 卡组编辑拖拽/滚动族（判据 = `B141_W129_对账_卡组色彩.md` §一）---");
        GroupA_Thresholds(null);
        GroupB_AutoScroll();
        GroupC_TextLimits();
        GroupT_SourceText();
        SkipLine("D 段（实例级：行拖摘牌 / 滚动面命中 / 滚动拖动）没跑 —— 本入口没拿到 `DeckRuntime` 实例（宿主请调 `Run(_rt)`）");
        Debug.Log(Tag + " 合计：✓ " + _pass + " · ✗ " + _fail + " · SKIP " + _skip);
    }

    /// <summary>实例级那一半（宿主把 `_rt` 递进来）。</summary>
    public static void Run(DeckRuntime rt)
    {
        _pass = 0; _fail = 0; _skip = 0;
        Debug.Log(Tag + " --- 卡组编辑拖拽/滚动族（判据 = `B141_W129_对账_卡组色彩.md` §一）---");
        GroupA_Thresholds(rt);
        GroupB_AutoScroll();
        GroupC_TextLimits();
        GroupT_SourceText();
        GroupD_Instance(rt);
        Debug.Log(Tag + " 合计：✓ " + _pass + " · ✗ " + _fail + " · SKIP " + _skip);
    }

    // ==================================================================
    //  A · 阈值与方向门（3-05 / 3-06 / 3-07 / 3-25）
    // ==================================================================
    static void GroupA_Thresholds(DeckRuntime rt)
    {
        Near(DeckRuntime.UiDragStartPx, 5f, 1e-6f,
             "A1 ★ 起拖阈值 = **5 px**（原版 uGUI `EventSystem.pixelDragThreshold`，`Intro.unity:5429`；我们原来是 16.2）");
        Check(!DeckRuntime.UiPassedDragStart(4.9f, 0f) && DeckRuntime.UiPassedDragStart(5.1f, 0f),
              "A2 ★ 过门判定是「距离 > 5」而不是「某个轴的绝对值 > 16.2」：4.9 → 没过、5.1 → 过");
        Check(DeckRuntime.UiPassedDragStart(3.6f, 3.6f),
              "A3 ★ 双轴一起算（3.6,3.6 的模长 5.09 > 5 ⇒ 过门）—— 旧写法 `abs(x)>16.2 || abs(y)>16.2` 在这里是**不过**");

        // A4–A6 走**反射直调私有静态** `DeckRuntime.IsScrollDragThreshold(Vector2)`（原版 `SupportMethods.IsScrollDragThreshold` 的等价物）
        // —— 它**不读任何实例状态**（纯函数）⇒ 不需要 `DeckRuntime` 实例（写法同 `B142_E53_Assert` §A）。
        var gate = ScrollGate;
        Check(gate != null,
              "A4a（前提）反射找得到私有静态 `DeckRuntime.IsScrollDragThreshold(Vector2)`"
            + "｜🧨 改名 / 改成实例方法 ⇒ 本行红（2026-11-04 `B142-E72`：⛔ 不再「拿不到实例就整段 SKIP」）");
        if (gate == null) return;
        Check(ScrollIsScrollDragThreshold(0f, 100f) == true,
              "A4 ★ 纯竖向位移 ⇒ 判成**滚动**（Angle = 90°，落在 (60°,120°)）");
        Check(ScrollIsScrollDragThreshold(100f, 0f) == false,
              "A5 ★ 纯横向位移 ⇒ **不是**滚动（Angle = 0°）—— 与 A4 结构上不可能同时为真（把方向门删成恒真/恒假必红一边）");
        Check(ScrollIsScrollDragThreshold(100f, 173.2f) == false && ScrollIsScrollDragThreshold(100f, 180f) == true,
              "A6 ★ 边界是**开区间**：59.99°(100,173.2) 不算滚动 · 60.9°(100,180) 算滚动（原版 `deg > 60 && deg < 120`）"
            + "｜⚠️ 30/150 那条带子属于**另一个函数** `DragAndDropItem.OnBeginDrag`（我们的等价物 = `PointerLayer.IsScrollDragAngle`，`B141_W32_Assert` §D），⛔ 别把它合并进来（2026-11-04 `B142-E53` 现核；`B142_E53_Assert` §C/§D 钉住两条带子不相等）");
    }

    // ==================================================================
    //  B · 边缘自滚量纲（3-11）
    // ==================================================================
    static void GroupB_AutoScroll()
    {
        Near(DeckRuntime.UiDragEdgeSpeedNormPerSec, 1f, 1e-6f,
             "B1 ★ 自滚速度 = **1.0 归一化/秒**（原版 `DragAndDropItem.speed`，prefab 实读；我们原来是 10 px/帧）");
        Near(DeckRuntime.DragEdgeBandPx, 50f, 1e-6f,
             "B2 ★ 边缘带 = **50 px**（原版 `DragAndDropItem.threshold`，prefab 实读）");
        Check(DeckRuntime.DragEdgeScrollFor(0f) < 0f && DeckRuntime.DragEdgeScrollFor(2000f) > 0f,
              "B3 ★ 判定两档方向相反：贴上沿 ⇒ 上滚（负）· 贴下沿 ⇒ 下滚（正）");
        Check(Mathf.Abs(DeckRuntime.DragEdgeScrollFor(500f) - DeckRuntime.DragEdgeScrollFor(900f)) < 1e-6f,
              "B4 ★ 中间带（500 / 900）两侧都在带外 ⇒ 同一个值（= 不在边缘带里时不滚）");
    }

    // ==================================================================
    //  C · 字符上限（3-14 / 3-15）
    // ==================================================================
    static void GroupC_TextLimits()
    {
        Check(DeckRuntime.UiNameLimitFor(1) == 26,
              "C1 ★ 卡组名上限 = **26**（原版输入框 `characterLimit 26`；我们原来 24）");
        Check(DeckRuntime.UiNameLimitFor(2) == int.MaxValue,
              "C2 ★ 卡名筛选**无上限**（原版 `characterLimit 0`）—— 与 C1 是两个档，⛔ 别共用一个数");
        Near(DeckRuntime.UiOutQuad(0.5f), 0.75f, 1e-5f, "C3 ★ 抽屉缓动 = OutQuad（`t=0.5` ⇒ 0.75；原版 DOTween 默认 6）");
        Near(DeckRuntime.UiOutQuad(0f), 0f, 1e-6f, "C4 ★ OutQuad 端点 0");
        Near(DeckRuntime.UiOutQuad(1f), 1f, 1e-6f, "C5 ★ OutQuad 端点 1");
    }

    // ==================================================================
    //  T · 源码文本：旧写法真的不在了（3-07 / 3-15 / 3-19 / 3-21 / 3-23 / 3-11 / 3-29）
    // ==================================================================
    static void GroupT_SourceText()
    {
        string p = Path.Combine(Application.dataPath, "CardPresentation/Deck/DeckRuntime.cs");
        if (!File.Exists(p)) { Check(false, "T0 ★ 前提：读得到 `DeckRuntime.cs`（" + p + "）"); return; }
        string src = File.ReadAllText(p);
        Check(src.IndexOf("if (!v.Contains(wp)) return false;", System.StringComparison.Ordinal) < 0,
              "T1 ★ 3-23：卡池格**不再**多判卡自己的小矩形（`v.Contains(wp)` 那一条已删 ⇒ 整格 262.5×384 命中）");
        Check(src.IndexOf("_nameEdit.Length < 24", System.StringComparison.Ordinal) < 0,
              "T2 ★ 3-14/15：写死的 24 已删（改走 `NameLimitFor`：卡组名 26 / 卡名筛选无上限）");
        Check(src.IndexOf("MenuDeckErrorKey(err),", System.StringComparison.Ordinal) < 0
              && src.IndexOf("DeckRules.ErrorTextKey(err),", System.StringComparison.Ordinal) >= 0,
              "T3 ★ 3-19：保存失败模态窗**不再**走数字族键 `MenuDeckErrorKey`（改与横幅同源的 `DeckRules.ErrorTextKey`）");
        Check(src.IndexOf("_newDeck = PlayerDeckForDemo(NewState());", System.StringComparison.Ordinal) < 0,
              "T4 ★ 3-21：空库**不再**塞演示满编（改 `CardDeck__GetEmptyDeck` 的真·空卡组）");
        Check(src.IndexOf("Mathf.MoveTowards(d.Slide", System.StringComparison.Ordinal) < 0,
              "T5 ★ 3-12：抽屉**不再**线性 `MoveTowards`（改 OutQuad）");
        Check(src.IndexOf("DragEdgeSpeedPx", System.StringComparison.Ordinal) < 0,
              "T6 ★ 3-11：`DragEdgeSpeedPx`（10 px/帧）已删（改 `DragEdgeSpeedNormPerSec` = 1.0 归一化/秒）");
        Check(src.IndexOf("ArmPxSlop", System.StringComparison.Ordinal) < 0,
              "T7 ★ 3-05/07：`ArmPxSlop`（16.2）已删（改 `DragStartPx` = 5）");
        Check(src.IndexOf("_rowDragArmed", System.StringComparison.Ordinal) >= 0
              && src.IndexOf("BeginRowDragOut()", System.StringComparison.Ordinal) >= 0,
              "T8 ★ 3-06/27：行拖有「越门才起拖」与「起拖即摘牌」两个口（`_rowDragArmed` / `BeginRowDragOut`）");
        Check(src.IndexOf("HandleScrollDragTick", System.StringComparison.Ordinal) >= 0
              && src.IndexOf("BeginScrollDrag", System.StringComparison.Ordinal) >= 0,
              "T9 ★ 3-08/09：判成滚动会**真的转给滚动**（`BeginScrollDrag`）+ 四台滚动面有按住拖动（`HandleScrollDragTick`）");
    }

    // ==================================================================
    //  D · 实例级（3-09 / 3-16 / 3-26 / 3-27 / 3-32）
    // ==================================================================
    static void GroupD_Instance(DeckRuntime rt)
    {
        if (rt == null) { SkipLine("D 段：没拿到 `DeckRuntime` 实例"); return; }
        bool built = rt.State != null && rt.Root != null;
        Check(built, "D0 ★ 前提：编辑器已经 `Build()` 过（`State` / `Root` 都在）");
        if (!built) return;

        // ---- D1–D5：四台滚动面（3-09）----
        if (rt.ActiveTab != 0)
            SkipLine("D1–D5：当前不在 Cards 页（`_tab` = " + rt.ActiveTab + "）⇒ 滚动面/行带那几条按页分档，跳过");
        else
        {
            Check(rt.UiSdKindAt(100f, 500f) == 2,
                  "D1 ★ 左栏列表那一点 ⇒ 第 2 台滚动面（deck list）—— 实得 " + rt.UiSdKindAt(100f, 500f));
            Check(rt.UiSdKindAt(1000f, 500f) == 1,
                  "D2 ★ 卡池那一点 ⇒ 第 1 台滚动面（pool）—— 实得 " + rt.UiSdKindAt(1000f, 500f));
            Check(rt.UiSdKindAt(208f, 60f) == 0,
                  "D3 ★ 页头带（y=60）不属于任何滚动面 —— 实得 " + rt.UiSdKindAt(208f, 60f));
            float maxD = rt.MaxDeckScrollPx;
            if (maxD <= 1f) SkipLine("D4：卡组列表当前不可滚（`MaxDeckScrollPx` = " + maxD.ToString("F1") + "）⇒ 按住拖动无处可滚");
            else
            {
                float before = rt.DeckScrollPx;
                rt.UiDragScroll(2, 100f, 500f, -30f, 3, 0.016f);       // 指针往上拖 90 px ⇒ 看后面的行
                Check(rt.DeckScrollPx > before + 1f,
                      "D4 ★ 按住往上拖 90 px ⇒ 列表滚下去了（" + before.ToString("F1") + " → " + rt.DeckScrollPx.ToString("F1") + "）");
                rt.UiScrollDeck(before - rt.DeckScrollPx);             // 还原到**进这一段之前**的位置（⛔ 不是置 0）
            }
        }

        // ---- D5：张数条分子含督军（3-16）----
        string wl = rt.State.Deck.WarlordId;
        if (string.IsNullOrEmpty(wl)) SkipLine("D5：当前卡组没有督军 ⇒ 张数条「含督军」这一档验不到");
        else Check(rt.UiDeckCountInFull(wl) >= 1,
                   "D5 ★ 张数条分子含**督军**（`GetLibraryInFull` 口径）：战将那张读 " + rt.UiDeckCountInFull(wl) + "（旧写法恒 0）");

        // ---- D6–D8：落点判据（3-26，纯函数、**不动卡组**）----
        Check(DeckRuntime.UiDroppedOnDeckPanel(150f, 700f),
              "D6 ★ 面板里那一点（150,700）⇒ **算在面板上**（松手 = 放回）");
        Check(!DeckRuntime.UiDroppedOnDeckPanel(1500f, 700f),
              "D7 ★ 面板右外（1500,700）⇒ **不算**（松手 = 保持已摘）—— 与 D6 是一对");
        Check(!DeckRuntime.UiDroppedOnDeckPanel(150f, 200f),
              "D8 ★ **只在 y 上出界**（150,200）⇒ 也**不算** —— 这条旧写法（只比 x）**恒错**，是 3-26 的鉴别力所在");

        SkipLine("D9–D11（行拖「起拖即摘牌 / 丢回放回」整条链）：本段**故意不跑** —— 它会改卡组内容与脏标记，"
               + "而宿主这一刻后面还有一批「起手必须干净」的断言；那条链由宿主既有的那一对覆盖："
               + "`UiDragDeckRow(2,150)` 张数不变（丢回面板）/ `UiDragDeckRow(2,1200)` 少一张（丢在面板外），两条都走同一段新代码");
    }

    // ==================================================================
    //  A4–A6 的反射口 —— `DeckRuntime.IsScrollDragThreshold(Vector2)` 是**私有静态**纯函数
    //  （原版 `SupportMethods.IsScrollDragThreshold` 的等价物：`Vector2.Angle(v, Vector2.right) ∈ (60°,120°)`），
    //  不读任何实例状态 ⇒ 直调它**不需要** `DeckRuntime` 实例。
    //  🔴 **2026-11-04（`B142-E72`）**：本文件 A4/A6 原来「拿不到 `DeckRuntime` 实例 ⇒ 整段 SKIP」，
    //  而它本来就是纯函数（旧注释把它写成了「实例方法」，那是 `UiIsScrollDragThreshold` 那层**包装**，不是判据本体）
    //  ⇒ 改成与 `B142_E53_Assert` §A 同一种反射写法，⛔ 不再 SKIP（前提不成立改由 A4a 硬断，⛔ 不静默跳过）。
    //  （放在类尾：本文件不被账按 `文件:行号` 引用，但仍按「新成员追加到类尾」的纪律走。）
    // ==================================================================

    static System.Reflection.MethodInfo _scrollGate;
    static bool _scrollGateLooked;

    /// <summary>私有静态门的反射句柄（找不到 ⇒ `null` ⇒ A4a 红）。</summary>
    static System.Reflection.MethodInfo ScrollGate
    {
        get
        {
            if (!_scrollGateLooked)
            {
                _scrollGateLooked = true;
                _scrollGate = typeof(DeckRuntime).GetMethod("IsScrollDragThreshold",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic
                    | System.Reflection.BindingFlags.Public);
            }
            return _scrollGate;
        }
    }

    /// <summary>调那个门。`null` = 调不动（前提不成立）—— 调用方必须先断 <see cref="ScrollGate"/> 再断行为。</summary>
    static bool? ScrollIsScrollDragThreshold(float dx, float dy)
    {
        var m = ScrollGate;
        if (m == null) return null;
        try { return (bool)m.Invoke(null, new object[] { new Vector2(dx, dy) }); }
        catch (System.Exception) { return null; }
    }
}
