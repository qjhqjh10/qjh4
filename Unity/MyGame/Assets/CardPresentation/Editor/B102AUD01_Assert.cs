// B102AUD01_Assert.cs — 执行代理 `B102_AUD01`（`AUD02` 普查 · 序列化参数 `m_FillCenter` 那一条）
//
// ============================ 它盯的是什么 ============================
//  `Deck Editing Menu ▸ Content Area ▸ Sidebar ▸ Footer ▸ Done ▸ Done Highlight`
//  原版那颗 `Image`（UnityPy 直读真包 `assets_full/bundle_menus_assets_all`）：
//    · sprite = `FX Square UI SDF`（**128 × 128**）
//    · `m_Type = Sliced` · `m_Border = (19, 19, 19, 19)` · `m_PixelsPerUnitMultiplier = 1`
//    · **`m_FillCenter = 0`** ⇒ 中间那块**不画**，只画 8 块边角（= **空心**框）
//  改前：`Deck/DeckRuntime.cs:2477` 的 `Img("foot_hl", "FX_Square_UI_SDF", …)` = 把整张 128²
//  拉成的**实心 quad**（`ImageQuad.Create` 不切九宫）。
//  改后：`DeckRuntime.BuildFootHl()` → `MenuDraw.Nine(…, fillCenter: FootHlFillCenter=false)`。
//  判据全文 → `资料/完全复刻/报告/B101_P2_AUD02序列化非视觉参数.md` §4-3 / 附录；
//  逐条数据 → `资料/完全复刻/判据/AUD02_serialized_params.tsv`（`Deck Editing Menu … Done Highlight / m_FillCenter` 那一行）。
//
// ============================ 🔴 灭自证（怎么才能红）============================
//  🧨 改法 ①：`DeckRuntime.FootHlFillCenter` 改回 `true` ⇒ `CreateNineSlice` 不再跳过正中那块
//            ⇒ 树里出现 `foot_hl_11`、四块边中块的矩形**盖住节点几何中心** ⇒ **A1 的「8 块 / 无 `_11`」
//            与 A2 的「中心没被覆盖」同时红**。
//  🧨 改法 ②：把 `BuildFootHl()` 换回 `Img(…)` ⇒ 根上直接挂 1 块 `ImageQuad` ⇒ **A1 第一条（根上不是 quad）红**，
//            且 A1 的「8 块」变成 1 块。
//  🧨 改法 ③：`FootHlBorderPx` 从 19 改成别的 ⇒ 角块像素尺寸与 uv 四条一起红（A1 后半）。
//  ⚠️ A1 与 A2 是**两个独立的面**：A1 按名字/块数读结构，A2 只读几何矩形 ⇒ 只改一处不改另一处照样红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  🔴 **必须接在 `CardPresentation/Editor/DeckScene.cs`**（本窗 `DeckRuntime` 建在那个宿主里），
//     ⛔ **别接 `ShellScene` / `MainMenuScene`** —— 那两个宿主根本不建卡组编辑窗，A0 前提会红。
//  接在该方法尾的**聚合行**（同一物理行追加，**行数零变化**）：
//      B102AUD01_Assert.Run(); pass += B102AUD01_Assert.Pass; fail += B102AUD01_Assert.Fail;
//  `Run()` 自己**按 `DeckRuntime.Root` 找 `foot_hl`**（扫全部 `DeckRuntime` 实例、取第一个建出该节点的），
//  不需要宿主传 `_rt`；要指定实例也可以走重载 `Run(Transform footHl)`。
//  ⚠️ 前提是**宿主已经把窗建出来了**（`DeckScene` 在 BuildRuntime 之后的那一段）—— 建窗之前调必然红。
//
// ============================ 判据 / 报告 ============================
//  · 原版：`bundle_menus_assets_all` 的 `Deck Editing Menu ▸ … ▸ Done Highlight` 那颗 `Image`
//          （`m_Type` / `m_Border` / `m_FillCenter` / `m_PixelsPerUnitMultiplier` 逐字段）
//  · 报告：`资料/完全复刻/报告/B102_AUD01_DoneHighlight空心框.md`（若调度台另有落点，以账为准）
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B102_AUD01`：`Done Highlight` 必须是**空心九宫格**（原版 `Sliced` + `m_FillCenter=0`）。</summary>
public static class B102AUD01_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B102AUD01] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }
    static void T(bool c, string msg) { MenuCheck.True(_sink, c, msg); }
    static void S(string t) { MenuCheck.Section(_sink, t); }

    /// <summary>唯一入口（宿主聚合行里调）：自己去找建出来的 `foot_hl`。</summary>
    public static void Run() { Run(FindFootHl()); }

    /// <summary>指定实例版（宿主已经有 `_rt` 时可用）。</summary>
    public static void Run(Transform footHl)
    {
        S("🆕 B102_AUD01：`Done Highlight` 必须是**空心**九宫格（原版 `Sliced` · `m_Border (19,19,19,19)` · `m_FillCenter=0` · sprite 128²）");

        // ------------------------------------------------------------------
        //  A0 前提（**硬断言** —— ⛔ 不写 `if (x != null) Check(...)`：前提不成立时必须红，不许静默跳过）
        // ------------------------------------------------------------------
        T(footHl != null,
          "A0（前提）`Deck Editing Menu ▸ Footer ▸ Done ▸ Done Highlight`（节点名 `foot_hl`）建出来了 —— 前提不成立时 A1/A2 全部无从谈起");
        var quads = footHl != null ? footHl.GetComponentsInChildren<ImageQuad>(true) : new ImageQuad[0];
        T(quads.Length > 0, "A0（前提）这棵子树里有 `ImageQuad`（A1/A2 读的就是它们）—— 实得 " + quads.Length.ToString());

        // ------------------------------------------------------------------
        //  A1 ★ 空心框真的空心（**结构**判据：块数 / 名字 / 角块尺寸与 uv）
        // ------------------------------------------------------------------
        //  🧨 改法 ②：换回 `Img(…)` ⇒ `ImageQuad.Create` 把 quad 挂在**根**上 ⇒ 这一条红。
        T(footHl != null && footHl.GetComponent<ImageQuad>() == null,
          "A1 ★ `foot_hl` 的**根上不是一块 quad**（`MenuDraw.Nine` 的容器根；改前 `Img()` 会在根上直接挂那 1 块实心 quad）");
        //  🧨 改法 ①：`fillCenter:true` ⇒ 9 块（下面两条一起红）。
        T(quads.Length == 8,
          "A1 ★ 只有**八块边角**（`3×3` 去掉正中那块）—— 实得 " + quads.Length.ToString());
        bool hasCenter = false;
        for (int i = 0; i < quads.Length; i++)
            if (quads[i] != null && quads[i].name == "foot_hl_11") hasCenter = true;
        T(!hasCenter, "A1 ★ 正中那块 `foot_hl_11` **不存在**（原版 `m_FillCenter = 0`）");

        ImageQuad corner = null;
        for (int i = 0; i < quads.Length; i++)
            if (quads[i] != null && quads[i].name == "foot_hl_00") corner = quads[i];
        T(corner != null, "A1 ★ 左上角块 `foot_hl_00` 在");
        if (corner != null)
        {
            //  🧨 改法 ③：`FootHlBorderPx` ≠ 19 ⇒ 这四条一起红（uv 是 19/128；画出来是 19px，ppuMul = 1）。
            var r = MenuDraw.QuadRectPx(corner);
            N(r.W, 19f, 0.25f, "A1 ★ 角块画出来宽 = **19** px（原版 `m_Border.x` · `m_PixelsPerUnitMultiplier = 1`）");
            N(r.H, 19f, 0.25f, "A1 ★ 角块画出来高 = **19** px（原版 `m_Border.y`）");
            var uv = corner.UvRect;
            N(uv.width, 19f / 128f, 0.003f, "A1 ★ 角块 uv 宽 = **19/128**（=`m_Border.x` ÷ sprite 宽 128）");
            N(uv.height, 19f / 128f, 0.003f, "A1 ★ 角块 uv 高 = **19/128**（=`m_Border.y` ÷ sprite 高 128）");
        }

        // ------------------------------------------------------------------
        //  A2 灭自证（**几何**判据：与 A1 的「按名字数」互相独立 —— 只读矩形，不读名字）
        // ------------------------------------------------------------------
        //  🧨 改法 ①：`FootHlFillCenter` 改回 `true` ⇒ 正中那块 `foot_hl_11` 的矩形**盖住节点几何中心** ⇒ 红。
        float cx = DeckRuntime.DoneHlX + DeckRuntime.DoneHlW * 0.5f;      // 107.90
        float cy = DeckRuntime.DoneHlY + DeckRuntime.DoneHlH * 0.5f;      // 1045.10
        int cover = 0;
        float ux1 = float.MaxValue, uy1 = float.MaxValue, ux2 = float.MinValue, uy2 = float.MinValue;
        for (int i = 0; i < quads.Length; i++)
        {
            if (quads[i] == null) continue;
            var r = MenuDraw.QuadRectPx(quads[i]);
            if (cx >= r.x1 - 0.01f && cx <= r.x2 + 0.01f && cy >= r.y1 - 0.01f && cy <= r.y2 + 0.01f) cover++;
            if (r.x1 < ux1) ux1 = r.x1;
            if (r.y1 < uy1) uy1 = r.y1;
            if (r.x2 > ux2) ux2 = r.x2;
            if (r.y2 > uy2) uy2 = r.y2;
        }
        N(cx, 107.90f, 0.05f, "A2（前提）节点几何中心 x = **107.90**（`DoneHlX + DoneHlW/2` = −3.7 + 111.6）");
        N(cy, 1045.10f, 0.05f, "A2（前提）节点几何中心 y = **1045.10**（`DoneHlY + DoneHlH/2` = 1004.4 + 40.7）");
        T(cover == 0,
          "A2 ★ **空心本身**：节点几何中心没有任何一块 quad 覆盖（实得覆盖块数 " + cover.ToString() + "）—— 实心 quad / 补了中块都会 ≥ 1");

        // 八块并集必须**仍是整块** 223.2 × 81.4（⛔ 不许因为切九宫把框画小/画偏）
        N(ux2 - ux1, DeckRuntime.DoneHlW, 0.4f, "A2 ★ 八块并集宽 = **223.2**（= `DoneHlW`，⛔ 不是被缩小的框）");
        N(uy2 - uy1, DeckRuntime.DoneHlH, 0.4f, "A2 ★ 八块并集高 = **81.4**（= `DoneHlH`）");
        N(ux1, DeckRuntime.DoneHlX, 0.4f, "A2 ★ 并集左沿 = **−3.7**（= `DoneHlX`）");
        N(uy1, DeckRuntime.DoneHlY, 0.4f, "A2 ★ 并集上沿 = **1004.4**（= `DoneHlY`）");
    }

    // ==================================================================
    //  找件（**不依赖宿主传参**）：扫全部 `DeckRuntime` 实例，取第一个建出 `foot_hl` 的
    //  ⚠️ `DeckScene` 里会造好几个 `DeckRuntime`（探针/冒烟用的那几个不建页脚）⇒ 不能用
    //      `DeckRuntime.Instance`（编辑 / 批处理下 `Awake` 未必跑，且它是「最后一个」）。
    // ==================================================================
    static Transform FindFootHl()
    {
        var all = Resources.FindObjectsOfTypeAll<DeckRuntime>();
        for (int i = 0; i < all.Length; i++)
        {
            var rt = all[i];
            if (rt == null || rt.Root == null) continue;
            var t = FindDeep(rt.Root, "foot_hl");
            if (t != null) return t;
        }
        return null;
    }

    static Transform FindDeep(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var t = FindDeep(root.GetChild(i), name);
            if (t != null) return t;
        }
        return null;
    }
}
