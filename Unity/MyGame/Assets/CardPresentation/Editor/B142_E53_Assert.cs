// B142_E53_Assert.cs — 执行写手 `B142-E53`（第一百二十九会话 · 2026-11-04 · 波次 B142）
//   账行 = `菜单_外壳 L106`（原版 `DragAndDropItem.OnBeginDrag`）
//
// ============================ 结论：两条带子、两个函数，⛔ 不能合并 ============================
//  判据现读（2026-11-04，逐行读 `.c` + `工具/read_literal.py` 读常量）：
//
//  ① `SupportMethods__IsScrollDragThreshold.c:45-57`
//       `cos = Clamp(dot/(|a||b|), −1, 1)` → `acos(cos) × 57.29578`（`0x1834b2e98` = Rad2Deg）
//       → **`60 < deg < 120`**（`0x1834b30c0` = **60** · `0x1834b3268` = **120**，两值现读）
//       ⇒ 语义 = `Vector2.Angle(位移, Vector2.right)` 的**开区间 (60°,120°) 判成「滚动」**。
//     **谁在调它**：`DeckEditingWindow__CheckCardDrag.c:30` 与 `DeckEditingWindow__CheckCosmeticDrag.c:28`
//       —— 卡组编辑窗那两条拖拽链（卡池卡 / 卡背）**都调这一个函数**。
//     **我们** = `Deck/DeckRuntime.cs:3976 IsScrollDragThreshold`（`deg > 60f && deg < 120f`）= **正确，本轮不改**。
//
//  ② `DragAndDropItem__OnBeginDrag.c:30-32`
//       `ang = |atan2(eventData.delta.y, eventData.delta.x)| × 57.29578`（`FUN_180486da0` = `Mathf.Atan2`；
//       `& 0x7FFFFFFF`（`0x1834b2e60`）取绝对值）→ **`if (ang < 30 || 150 < ang)` 才起拖**
//       （`0x1834b3168` = **30** · `0x1834b2d90` = **150**，两值现读）⇒ **30 ≤ ang ≤ 150 转交 `ScrollRect`**（`:38`）。
//     **我们** = `Shell/PointerLayer.cs:2032 IsScrollDragAngle`（B141/W32 已落，`CollectionScene` 已接线）。
//
//  ⇒ **本条账的正确处置 = 零代码改动**：`DeckRuntime` 那处 60/120 属于 ①，而 30/150 那一处**早已在 ②**。
//    `判据/B142_R15_CHK05块3块4.md` §4·4 的施工建议（「把 `DeckRuntime` 的 60/120 换成 30/150」）**会把 ① 弄错**；
//    本条断言正是钉住「两条带子必须同时存在、且不相等」。
//
// ============================ 灭自证（🧨 结构上不可能同时满足）============================
//  🧨 A1/A2：把 `DeckRuntime` 的带子改成 30/150（= 照施工建议做）⇒ A2 的 45° 那一档翻 ⇒ 红。
//  🧨 B1/B2：把 `PointerLayer` 的带子改成 60/120（= 两条搞混）⇒ B2 的 45°/135° 翻 ⇒ 红。
//  🧨 C1/C2：**同一条 45°/135° 位移上，两个门必须给相反答案**（60/120 窄带：不是滚动；30/150 宽带：是滚动）
//        ⇒ 把任一函数改成另一条带子，必然**只有一边**翻 ⇒ 红。两边不能一起绿。
//  🧨 D1/D2：源码字面量（60/120 开区间 · 30/150 闭区间）—— 改值不改行为（例如把 `>` 改成 `>=`）也红。
//  🧨 D3/D4：`DeckRuntime` 里不许出现 30/150 的写法、`PointerLayer` 里不许出现 60/120 的写法 ⇒ 合并/互抄 ⇒ 红。
//  🧨 E1：`PointerLayer.IsScrollDragAngle` 至少被**两条输入路**（生产 `Update` / 自检 `MoveTo`）调用 ⇒ 删成死件 ⇒ 红。
//
// ============================ 怎么接 ============================
//  `public static void Run()`：**零前提**（不建场景、不读存档、不写 PlayerPrefs；只读两个源文件做结构检查）。
//  挂法（调度台决定；本代理不动宿主）：宿主聚合行**同一物理行**、且落在该行第一个 `//` **之前**追加
//      B142_E53_Assert.Run(); _sink.Pass += B142_E53_Assert.Pass; _sink.Fail += B142_E53_Assert.Fail;
//  宿主建议 = `Editor/DeckScene.cs`（60/120 那一支的宿主）或 `Editor/CollectionScene.cs`（30/150 那一支）。
//  判绿红：按行首 `B142E53 ✓` / `B142E53 ✗` 数（⛔ 别 `grep -c ✗`），合计行 = `B142E53 合计 …`。

using System;
using System.IO;
using System.Reflection;
using CardPresentation;
using UnityEngine;

/// <summary>`B142-E53`：钉住「两条方向门带子」——`SupportMethods.IsScrollDragThreshold` 的 **60/120（开区间）**
/// 与 `DragAndDropItem.OnBeginDrag` 的 **30/150（闭区间）**必须**同时存在且不相等**。</summary>
public static class B142_E53_Assert
{
    static int _pass, _fail;
    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("B142E53 ✓ " + msg); }
        else { _fail++; Debug.LogError("B142E53 ✗ " + msg); }
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        A_SupportMethodsBand();     // 60/120（SupportMethods 链）
        B_DragAndDropItemBand();    // 30/150（DragAndDropItem 链）
        C_TwoBandsAreNotEqual();    // 配对：同一位移上两个门给相反答案
        D_SourceLiterals();         // 源码字面量（开/闭区间）+ 不许互抄
        E_NotDeadCode();            // 30/150 那条门真的接在两条输入路上
        Debug.Log("B142E53 合计 " + _pass + " 过 / " + _fail + " 红");
    }

    // ============================================================
    //  工具
    // ============================================================
    /// <summary>方向向量：`Dir(90)` = 正上方（与屏幕 y 向上一致）。角度 = 与 +x 轴的夹角（0~180 取绝对）。</summary>
    static Vector2 Dir(float deg)
    {
        float r = deg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(r), Mathf.Sin(r));
    }

    /// <summary>`DeckRuntime.IsScrollDragThreshold` 是**私有静态**（原版 `SupportMethods` 的等价物）。
    /// 用反射直接调 —— 好处 = **不需要 `DeckRuntime` 实例**（`B141_W29_Assert` §A4/A6 在没实例时会整段 SKIP，
    /// 本条正好补上那段空档）。</summary>
    static MethodInfo _deckGate;
    static bool _deckGateLooked;

    static MethodInfo DeckGate
    {
        get
        {
            if (!_deckGateLooked)
            {
                _deckGateLooked = true;
                _deckGate = typeof(DeckRuntime).GetMethod("IsScrollDragThreshold",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            }
            return _deckGate;
        }
    }

    /// <summary>`null` = 调不动（方法与断言前提不成立），调用方必须先断前提再断行为。</summary>
    static bool? DeckScroll(float deg)
    {
        var m = DeckGate;
        if (m == null) return null;
        try { return (bool)m.Invoke(null, new object[] { Dir(deg) }); }
        catch (Exception) { return null; }
    }

    static string ReadSrc(string rel)
    {
        try
        {
            string p = Path.Combine(Application.dataPath, rel);
            return File.Exists(p) ? File.ReadAllText(p) : null;
        }
        catch (Exception) { return null; }
    }

    /// <summary>按**代码行**数出现次数（跳过以 `//` / `///` 开头的整行注释）。
    /// 返回 -1 = 源文件读不到（调用方须先断「读到了」这条前提）。</summary>
    static int CountOfCode(string src, string needle)
    {
        if (src == null) return -1;
        int n = 0;
        foreach (var ln in src.Split('\n'))
        {
            string t = ln.TrimStart();
            if (t.StartsWith("//", StringComparison.Ordinal)) continue;
            int i = 0;
            while ((i = ln.IndexOf(needle, i, StringComparison.Ordinal)) >= 0) { n++; i += needle.Length; }
        }
        return n;
    }

    const string SrcDeckRuntime = "CardPresentation/Deck/DeckRuntime.cs";
    const string SrcPointerLayer = "CardPresentation/Shell/PointerLayer.cs";

    // ============================================================
    //  A：`SupportMethods.IsScrollDragThreshold` 的带子 = **(60°, 120°) 开区间**
    //     判据 = `decomp_full/SupportMethods__IsScrollDragThreshold.c:45-57`（`0x1834b30c0`=60 · `0x1834b3268`=120）
    //     调用方 = `DeckEditingWindow__CheckCardDrag.c:30` · `DeckEditingWindow__CheckCosmeticDrag.c:28`
    // ============================================================
    static void A_SupportMethodsBand()
    {
        Check(DeckGate != null,
            "A0 ★（前提）`DeckRuntime.IsScrollDragThreshold` 存在（私有静态方法，反射拿到）"
          + "｜🧨 改签名/改名 ⇒ 本节整段失效 —— 所以先断这一条");

        bool? a45 = DeckScroll(45f), a90 = DeckScroll(90f), a0 = DeckScroll(0f);
        Check(a45.HasValue && a45.Value == false,
            "A1 ★★ **45° 不算滚动**（原版 `60 < deg` ⇒ 窄带之外）｜🧨 把带子改成 30/150 ⇒ 45° 变真 ⇒ 红");
        Check(a90.HasValue && a90.Value == true,
            "A2 ★★ **90°（纯竖向）判成滚动**（两条带子在这点上一致 —— 它不区分两条带子的差别）");
        Check(a0.HasValue && a0.Value == false,
            "A3 ★ **0°（纯横向）不算滚动**（两条带子在这点上也一致）");

        bool? a595 = DeckScroll(59.5f), a605 = DeckScroll(60.5f);
        bool? a1195 = DeckScroll(119.5f), a1205 = DeckScroll(120.5f);
        Check(a595.HasValue && a595.Value == false,
            "A4 ★★ 边界**开区间**下界：59.5° 不算滚动（原版 `60 < deg`，取到 60 也不含）");
        Check(a605.HasValue && a605.Value == true,
            "A5 ★★ 60.5° 算滚动｜A4×A5 一起钉住「下界就在 60 附近」");
        Check(a1195.HasValue && a1195.Value == true,
            "A6 ★★ 上界**开区间**：119.5° 仍算滚动");
        Check(a1205.HasValue && a1205.Value == false,
            "A7 ★★ 120.5° 不算滚动｜A6×A7 一起钉住「上界就在 120 附近」"
          + "｜🧨 把上界挪到 150（= 换成另一条带子）⇒ A7 变真 ⇒ 红");

        bool? a135 = DeckScroll(135f);
        Check(a135.HasValue && a135.Value == false,
            "A8 ★★ **135° 不算滚动**（60/120 窄带外；30/150 宽带内 —— 这是两条带子的**判别点**之一）"
          + "｜🧨 照 `判据/B142_R15_CHK05块3块4.md` §4·4 施工建议换成 30/150 ⇒ 红");

        bool? azeroVec = DeckScroll(0f);   // Dir(0) = (1,0)，不是零向量
        Check(azeroVec.HasValue && azeroVec.Value == false,
            "A9（不回归）纯右向位移（1,0）不算滚动 —— 卡池起拖仍然起得来");
        var mZero = DeckGate;
        bool zeroGuard = false;
        if (mZero != null)
        {
            try { zeroGuard = !(bool)mZero.Invoke(null, new object[] { Vector2.zero }); }
            catch (Exception) { zeroGuard = false; }
        }
        Check(mZero != null && zeroGuard,
            "A10 ★ 零位移 ⇒ **不算滚动**（原版那道 `|a||b| < kEpsilonNormalSqrt` 守卫返 0 ⇒ 0° 不在 (60,120) 内；"
          + "`Vector2.Angle` 自带它，我们的实现直接复用它）");
    }

    // ============================================================
    //  B：`DragAndDropItem.OnBeginDrag` 的带子 = **30 ≤ ang ≤ 150（闭区间）**
    //     判据 = `decomp_full/DragAndDropItem__OnBeginDrag.c:30-38`（`0x1834b3168`=30 · `0x1834b2d90`=150）
    // ============================================================
    static void B_DragAndDropItemBand()
    {
        Check(PointerLayer.IsScrollDragAngle(new Vector2(0f, 10f)),
            "B0 ★★ 90°（纯竖向）⇒ 归滚动区（原版 `30 ≤ ang ≤ 150` ⇒ 转交 `ScrollRect.OnBeginDrag`，`.c:38`）");
        Check(PointerLayer.IsScrollDragAngle(new Vector2(0f, -10f)),
            "B0b ★★ **正上方（−90°）也归滚动**（原版 `& 0x7FFFFFFF` 取绝对值 ⇒ 与正下方同值；"
          + "`B141_W32_Assert` D2 同一条）｜🧨 把 `Mathf.Abs` 去掉 ⇒ −90° 落带外 ⇒ 红");

        Check(PointerLayer.IsScrollDragAngle(Dir(45f)),
            "B1 ★★ **45° 归滚动**（30/150 宽带内）｜🧨 把带子改成 60/120 ⇒ 红（= 两条带子搞混）");
        Check(PointerLayer.IsScrollDragAngle(Dir(135f)),
            "B2 ★★ **135° 归滚动**｜🧨 同样，改成 60/120 ⇒ 红");

        Check(!PointerLayer.IsScrollDragAngle(Dir(29.5f)),
            "B3 ★★ 下界外侧：29.5° ⇒ 不归滚动（原版 `ang < 30` 才起拖）");
        Check(PointerLayer.IsScrollDragAngle(Dir(30.5f)),
            "B4 ★★ 下界内侧：30.5° ⇒ 归滚动｜B3×B4 钉住「下界就在 30 附近」");
        Check(PointerLayer.IsScrollDragAngle(Dir(149.5f)),
            "B5 ★★ 上界内侧：149.5° ⇒ 归滚动");
        Check(!PointerLayer.IsScrollDragAngle(Dir(150.5f)),
            "B6 ★★ 上界外侧：150.5° ⇒ 不归滚动｜B5×B6 钉住「上界就在 150 附近」"
          + "｜🧨 把带子改成 60/120 ⇒ B6 变真 ⇒ 红");

        Check(!PointerLayer.IsScrollDragAngle(Dir(0f)) && !PointerLayer.IsScrollDragAngle(Dir(180f)),
            "B7 ★ 两个纯水平方向（0° / 180°）都**不**归滚动 ⇒ 真的起拖（= 卡组格能横向重排）");
        Check(!PointerLayer.IsScrollDragAngle(Vector2.zero),
            "B8 ★ 零位移 ⇒ false（原版没有这道守卫，它是我们 `sqrMagnitude <= 0` 的显式前置；只影响噪声，**不改方向语义**）");
    }

    // ============================================================
    //  C：配对（灭自证的核心）—— 同一位移上，两条带子必须给**相反**答案
    // ============================================================
    static void C_TwoBandsAreNotEqual()
    {
        bool? a45 = DeckScroll(45f);
        bool b45 = PointerLayer.IsScrollDragAngle(Dir(45f));
        Check(a45.HasValue && a45.Value == false && b45,
            "C1 ★★★ **45°：60/120 说不（不是滚动）、30/150 说是（归滚动）** —— 两条带子必须**相反**"
          + "｜🧨 把任一函数改成另一条带子 ⇒ 两边同值 ⇒ 红（这条是「不许合并」的判别式）");

        bool? a135 = DeckScroll(135f);
        bool b135 = PointerLayer.IsScrollDragAngle(Dir(135f));
        Check(a135.HasValue && a135.Value == false && b135,
            "C2 ★★★ **135°：同上，必须相反**（与 C1 是镜像点 —— 只改一端的修复会被另一端点破）");

        bool? a90 = DeckScroll(90f);
        bool b90 = PointerLayer.IsScrollDragAngle(Dir(90f));
        Check(a90.HasValue && a90.Value && b90,
            "C3 ★ 90°：两条带子**都**判成滚动（共同点；⛔ 它不能用来区分两条带子）");

        bool? a0 = DeckScroll(0f);
        bool b0 = PointerLayer.IsScrollDragAngle(Dir(0f));
        Check(a0.HasValue && a0.Value == false && !b0,
            "C4 ★ 0°：两条带子**都**不是滚动（共同点；同理不区分）");
    }

    // ============================================================
    //  D：源码字面量 —— 带子必须**逐字**是那两条（改值/改开闭区间都红）+ 不许互抄
    // ============================================================
    static void D_SourceLiterals()
    {
        string deck = ReadSrc(SrcDeckRuntime);
        string ptr = ReadSrc(SrcPointerLayer);
        Check(deck != null, "D0a（前提）读到了 `" + SrcDeckRuntime + "`｜🧨 读不到 ⇒ D1/D3 恒假，所以先断它");
        Check(ptr != null, "D0b（前提）读到了 `" + SrcPointerLayer + "`｜同上");

        const string DeckBand = "return deg > 60f && deg < 120f;";
        const string PtrBand = "return ang >= 30f && ang <= 150f;";

        Check(CountOfCode(deck, DeckBand) == 1,
            "D1 ★★ `DeckRuntime` 的带子**逐字** = `" + DeckBand + "`（开区间；原版 `60 < deg < 120`）"
          + "｜🧨 把 `>` 改成 `>=` / 把 60 改成 30 ⇒ 红｜实际 " + CountOfCode(deck, DeckBand) + " 处");

        Check(CountOfCode(ptr, PtrBand) == 1,
            "D2 ★★ `PointerLayer` 的带子**逐字** = `" + PtrBand + "`（闭区间；原版 `ang < 30 || 150 < ang` 才起拖 ⇒ 30/150 含在带内）"
          + "｜🧨 改成 60/120 ⇒ 红｜实际 " + CountOfCode(ptr, PtrBand) + " 处");

        Check(CountOfCode(deck, "return ang >= 30f && ang <= 150f;") == 0
              && CountOfCode(deck, "Mathf.Atan2(") == 0,
            "D3 ★★ `DeckRuntime` 里**没有** 30/150 那条写法（也**没有** `Mathf.Atan2`）—— ⛔ 两条带子不许混进同一个门"
          + "｜实际 带子写法 " + CountOfCode(deck, "return ang >= 30f && ang <= 150f;")
          + " 处 / `Mathf.Atan2(` " + CountOfCode(deck, "Mathf.Atan2(") + " 处");

        Check(CountOfCode(ptr, "return deg > 60f && deg < 120f;") == 0
              && CountOfCode(ptr, "Vector2.Angle(") == 0,
            "D4 ★★ `PointerLayer` 里**没有** 60/120 那条写法（也**没有** `Vector2.Angle`）—— 反向同理"
          + "｜实际 带子写法 " + CountOfCode(ptr, "return deg > 60f && deg < 120f;")
          + " 处 / `Vector2.Angle(` " + CountOfCode(ptr, "Vector2.Angle(") + " 处");

        Check(CountOfCode(deck, "Vector2.Angle(dragInput, Vector2.right)") == 1,
            "D5 ★ `DeckRuntime` 用的是原版那条算式 `Vector2.Angle(位移, Vector2.right)`"
          + "（`SupportMethods__IsScrollDragThreshold.c` 的 `acos(dot/(|a||b|)) × Rad2Deg`，逐行等价）");
        Check(CountOfCode(ptr, "Mathf.Atan2(delta.y, delta.x)") == 1 && CountOfCode(ptr, "Mathf.Abs(") >= 1,
            "D6 ★ `PointerLayer` 用的是原版另一条算式 `|atan2(dy,dx)| × Rad2Deg`"
          + "（`FUN_180486da0` = `Mathf.Atan2`，`& 0x7FFFFFFF` = `Mathf.Abs`；**不是** `Vector2.Angle`）");
    }

    // ============================================================
    //  E：30/150 那条门不是死件 —— 两条输入路都调它
    // ============================================================
    static void E_NotDeadCode()
    {
        string ptr = ReadSrc(SrcPointerLayer);
        Check(ptr != null, "E0（前提）读到了 `" + SrcPointerLayer + "`（否则 E1 恒假）");
        int calls = CountOfCode(ptr, "IsScrollDragAngle(");
        // 1 处是**定义行**（`public static bool IsScrollDragAngle(Vector2 delta)`），其余是调用点
        Check(calls >= 3,
            "E1 ★★ `IsScrollDragAngle` 至少被 **2 条输入路**调（生产 `Update` + 自检 `MoveTo`；合计出现 >= 3 = 定义 1 + 调用 2）"
          + "｜🧨 只剩定义（= 死件）⇒ 红｜实际 " + calls + " 处");
        Check(CountOfCode(ptr, "IsScrollDragAngle(px - _pressPx)") >= 1
              && CountOfCode(ptr, "IsScrollDragAngle(p - _pressPx)") >= 1,
            "E2 ★★ 生产路（`px - _pressPx`）与自检直调路（`p - _pressPx`）**各有一道**"
          + "｜🧨 只留一条 ⇒ 批处理绿/真机不对 ⇒ 红");
    }
}
