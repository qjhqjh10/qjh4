// B103F6_Assert.cs — 第一百零三会话 · 执行代理 `B103_F6`（2026-10-30 晚）
//
// ============================ 它盯的是什么 ============================
//  今天（`B103_F5`）接上收藏窗四页的**小屏档**之后，两处**格子内容不跟着缩放**的可见缺陷：
//   ① `MenuDraw.DeckCell` 的内层几何写死「作者 px × `DeckCellK`(0.9)」、锚在 `r.x1/r.y1`
//      ⇒ 小屏格 **281.25×455.625** 里只画出 **225×364.5**（右空 56.25 / 下空 91.125）。
//   ② `CollectionWindow` 卡背页 `Cardback Shadow SDF` 那层的四个偏移写死桌面值
//      ⇒ 小屏格 **300×486** 里光晕只有 **295 宽 < 本体 300 宽**、只包住左上。
//
// ============================ 判据（期望值一律是**字面量**，不从被测代码读回来）============================
//  · 作者系 = **250×405**（卡背页 `_cellWidth/_cellHeight` 原值；卡组页 = 225×364.5 = 250×0.9 × 405×0.9）
//    → `资料/完全复刻/判据/AUD05_state_matrix_收藏窗.md` §1·2 / §2·2。
//  · 小屏系数：卡组 **1.25**（281.25×455.625）· 卡背 **1.2**（300×486）—— 同上 §1·2 那张表（四页四个值）。
//  · 卡组格内层（原版 prefab 作者系，`bundle_menus_assets_all` 的卡组格子树；见 `MenuDraw` 的
//    `DcFrameX/Y/W/H` 一族注释）：`Frame` = `(2,17) 246×368` ⇒ 右沿 248、下沿 385；
//    `Deck Image(mask)` = `(11,26.1) 228×306` ⇒ 右沿 239、下沿 332.1
//    （整张 1024² 纹理那一档的画框 = `(−49.4,6.2) 347.7×346.4`）。
//  · 卡背光晕作者系矩形 = `x −42.5 · y −70.875 · w 337.5 · h 550.8`（右沿 295、下沿 479.925），
//    出处 = `资料/普查产出_0923/A4_装饰页与驱动链.md:91`（锚点 `(-0.17,-0.185)-(1.18,1.175)` 是同一矩形的另一条推导，
//    ⚠️ 锚点是 **y 向上**、dump 的 y 是**向下** ⇒ 换算 `y1 = (1.175−1)×405`、`y2 = (1−(−0.185))×405`）。
//
// ============================ 灭自证（🧨 结构上不可能同时满足）============================
//  ① 把 `DeckCell` 的等比改回写死 `0.9`（= 今天的旧版）⇒ 小屏档那半组：
//     `Frame` 右沿 = `223.2` 而期望 `279.0`、`(Frame.x2 − r.x1)/r.W` = **0.7936** 而期望 **0.992**、
//     右空 = **58.05** 而期望 **2.25** ⇒ 至少 3 条红（且桌面档那半组照旧全绿 ⇒ 差异只可能来自缩放）。
//  ② 反过来只把期望改成「写死 0.9 的值」而不改实现 ⇒ 桌面档那半组红（桌面期望也写成小屏值就会与格子尺寸自相矛盾）。
//  ③ 卡背光晕把等比改回写死 ⇒ 小屏那条 `(sr.x2 − r.x1)/r.W == 1.18` 得 **0.9833**（295/300）⇒ 红；
//     而桌面那条（250 宽）恒 `(295)/250 = 1.18` ⇒ 绿 ⇒ 两档并置才抓得住。
//  ④ 两个比例**分开用**（x 吃 `r.W`、y 吃 `r.H`）：把 y 也写成 `r.W` 的系数 ⇒ 目前两档格子都是**等比**缩放
//     （281.25/250 = 455.625/405 = 1.125），这条**抓不到**；如实记在报告 §④「没查清/抓不到的」里。
//
// ============================ 怎么接（⚠️ 本代理不碰宿主聚合行）============================
//  `Editor/CollectionScene.cs` 的聚合行里（**同一物理行、`//` 之前**）加：
//      B103F6_Assert.Run(); _sink.Pass += B103F6_Assert.Pass; _sink.Fail += B103F6_Assert.Fail;
//  ⚠️ `Run()` 自建夹具（一棵临时 GameObject，`finally` 里 `DestroyImmediate`），**不建窗、不写盘**；
//     小屏开关用 `SmallScreenUI.PersistOverride`（⛔ 不动玩家真设置），收尾放回**进场档**。
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B103_F6`：卡组格内层几何 / 卡背光晕 —— 「桌面逐位不变、小屏撑满格子」两档并置。</summary>
public static class B103F6_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B103F6] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }
    static void S(string t) { MenuCheck.Section(_sink, t); }

    // ---------------------------------------------------------------- 判据字面量（⛔ 不从被测代码读）
    const float AuthorW = 250f, AuthorH = 405f;          // 作者系（AUD05 §1·2 卡背页 `_cellWidth/_cellHeight`）
    const float FrameX1 = 2f, FrameY1 = 17f, FrameX2 = 248f, FrameY2 = 385f;          // `Frame` 作者系
    const float ImgX1 = 11f, ImgY1 = 26.1f, ImgX2 = 239f, ImgY2 = 332.1f;             // `Deck Image(mask)` 作者系
    const float WholeX1 = -49.4f, WholeY1 = 6.2f, WholeX2 = 298.3f, WholeY2 = 352.6f; // 整张 1024² 那一档的画框

    /// <summary>一个**纯节点**（`MenuDraw.Node` 建的：格子根 / `Hit`）的设计矩形（画布 px · 左上原点 · y 向下）。
    /// ⚠️ 读 `RectTransform.sizeDelta`（`ApplyPxRect` 把矩形写在这儿，锚点恒重合、pivot 居中）+ `MenuCheck.DesignPx` 的中心。
    /// ⛔ **别拿它量图** —— `MenuDraw.Rect` / `Nine` 建的节点**不写 `sizeDelta`**（图是 `ImageQuad` 的网格，
    /// 尺寸在 `WorldW/H` 上），那种走 <see cref="QuadRect"/>。</summary>
    static bool NodeRect(Transform t, out float x1, out float y1, out float x2, out float y2)
    {
        x1 = y1 = x2 = y2 = 0f;
        var rt = t as RectTransform;
        if (rt == null) return false;
        const float dp = LayoutSpace.DesignPxH / LayoutSpace.DesignHeight;   // 108 px / 世界单位
        float w = rt.sizeDelta.x * dp, h = rt.sizeDelta.y * dp;
        if (!(w > 0f && h > 0f)) return false;                                // 量不到 ⇒ 调用方必红，⛔ 不静默
        var c = MenuCheck.DesignPx(t);
        if (Mathf.Abs(c.x) > 50000f || Mathf.Abs(c.y) > 50000f) return false; // `DesignPx(null)` 的哨兵
        x1 = c.x - w * 0.5f; x2 = c.x + w * 0.5f;
        y1 = c.y - h * 0.5f; y2 = c.y + h * 0.5f;
        return true;
    }

    /// <summary>一个 `ImageQuad` **画出来**的像素矩形 —— 转调全仓唯一那份 <see cref="MenuDraw.QuadRectPx"/>。
    /// ⚠️ 取的是**这个组件自己**的 `ImageQuad`（`MenuDraw.Rect` 把名字打在这颗上；⛔ 别 `GetComponentInChildren` ——
    /// 软边那一档会在子树里切出 `<名字>_soft<i><j>` 子块）。</summary>
    static bool QuadRect(Transform t, out float x1, out float y1, out float x2, out float y2)
    {
        x1 = y1 = x2 = y2 = 0f;
        var q = t != null ? t.GetComponent<ImageQuad>() : null;
        return MenuDraw.QuadRectPx(q, out x1, out y1, out x2, out y2);
    }

    // ============================================================ 入口

    /// <summary>两处几何的两档自检（桌面 / 小屏各一遍）。</summary>
    public static void Run()
    {
        bool wasPersist = SmallScreenUI.PersistOverride;
        bool wasEnabled = SmallScreenUI.Enabled;
        SmallScreenUI.PersistOverride = true;          // ⛔ 自检不许动玩家的真设置（同 `B103F5_Assert`）
        GameObject host = null;
        try
        {
            S("B103F6 · ① 卡组格内层几何：桌面（225×364.5）逐位不变 ↔ 小屏（281.25×455.625）撑满格子");
            host = new GameObject("B103F6 DeckCell Fixture");
            DeckTier(host.transform, false);
            DeckTier(host.transform, true);

            S("B103F6 · ② 卡背光晕（`Cardback Shadow SDF`）：桌面（250×405）逐位不变 ↔ 小屏（300×486）等比");
            CosmoTier(false);
            CosmoTier(true);
        }
        finally
        {
            if (host != null) Object.DestroyImmediate(host);
            SmallScreenUI.Set(wasEnabled);                 // 放回**进场档**（不是硬写 false）
            SmallScreenUI.PersistOverride = wasPersist;
        }
        Tr(SmallScreenUI.Enabled == wasEnabled, "（收尾）`SmallScreenUI` 已放回入场档");
        Tr(SmallScreenUI.PersistOverride == wasPersist, "（收尾）`PersistOverride` 写盘闸已放回入场档");
        Debug.Log(_sink.P + "B103F6 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ ① 卡组格

    static void DeckTier(Transform parent, bool small)
    {
        string tn = small ? "小屏" : "桌面";
        SmallScreenUI.Set(small);
        float cw = CollectionWindow.DeckCellW, ch = CollectionWindow.DeckCellH;

        // ---- 前提（先断前提，再断结论 —— ⛔ 不许 `if (非空) 断`）
        N(cw, small ? 281.25f : 225f, 1e-3f, $"[{tn}]（前提）`DeckCellW` == {(small ? "281.25" : "225")}（原版 225 × 小屏系数 1.25）");
        N(ch, small ? 455.625f : 364.5f, 1e-3f, $"[{tn}]（前提）`DeckCellH` == {(small ? "455.625" : "364.5")}（原版 364.5 × 1.25）");

        var r = new PxRect(0f, 0f, cw, ch);            // 格子锚在夹具原点 ⇒ 读数直接可比
        var info = new CollectionData.DeckInfo { Name = "B103F6", WarlordId = "0544047df88d2ba4cad9c17ff080c0eb", Faction = "", Count = 0, CardbackId = "", GameMode = 0 };   // `WarlordId` 给一张**真实存在的** `Resources/Art/cards/art_<id>.png`（否则 `Portrait` 返 null、主图层走卡背退路，而 `Faction` 为空时卡背也是 null ⇒ 夹具会凭空少一层）
        var cell = MenuDraw.DeckCell(parent, "B103F6_" + tn, r, info, false, 3010, 3009, 3011, 3013, 3014,
                                     () => { });       // `onClick` 给非 null ⇒ `Hit` 那层会建（下面要用）

        Tr(cell != null, $"[{tn}]（前提）`DeckCell` 建出来了");
        if (cell == null) return;                      // 建不出 ⇒ 上一行已红，后面的读数无意义（⛔ 不是静默跳过）

        float cx1, cy1, cx2, cy2;
        Tr(NodeRect(cell, out cx1, out cy1, out cx2, out cy2), $"[{tn}]（前提）`DeckCell` 根节点的矩形量得出来");
        N(cx2 - r.x1, cw, 0.5f, $"[{tn}]（前提）整格 root 的右沿 == 格子右沿（格子铺满整格）");
        N(cy2 - r.y1, ch, 0.5f, $"[{tn}]（前提）整格 root 的下沿 == 格子下沿");

        // ---- 结论 A：`Frame`（卡框）—— 恒建的层，作者系 (2,17)→(248,385) 按 r 等比
        var frame = cell.Find("Frame");
        Tr(frame != null, $"[{tn}]（前提）`Frame` 层存在（`40K_bt_deck` 在 `Resources/Art/ui_deck/`）");
        float fx1 = -9999f, fy1 = -9999f, fx2 = -9999f, fy2 = -9999f;
        bool fm = frame != null && QuadRect(frame, out fx1, out fy1, out fx2, out fy2);
        Tr(fm, $"[{tn}]（前提）`Frame` 的矩形量得出来");
        if (fm)
        {
            float sx = cw / AuthorW, sy = ch / AuthorH;
            N(fx1 - r.x1, FrameX1 * sx, 0.5f, $"[{tn}] `Frame` 左沿 == 2 × (格宽/250)");
            N(fx2 - r.x1, FrameX2 * sx, 0.5f, $"[{tn}] `Frame` 右沿 == 248 × (格宽/250) —— 改前小屏这里得 223.2（期望 {FrameX2 * sx:F3}）");
            N(fy1 - r.y1, FrameY1 * sy, 0.5f, $"[{tn}] `Frame` 上沿 == 17 × (格高/405)");
            N(fy2 - r.y1, FrameY2 * sy, 0.5f, $"[{tn}] `Frame` 下沿 == 385 × (格高/405) —— 改前小屏这里得 346.5（期望 {FrameY2 * sy:F3}）");

            // 归一化 = 「内层几何占格子的比例」—— 与格子大小无关，两档必须**同值**（这才是「填满」的判据）
            N((fx2 - r.x1) / cw, FrameX2 / AuthorW, 2e-3f, $"[{tn}] （归一）`Frame` 右沿占格宽 == 248/250 = 0.992 —— 改前小屏 = 0.7936");
            N((fy2 - r.y1) / ch, FrameY2 / AuthorH, 2e-3f, $"[{tn}] （归一）`Frame` 下沿占格高 == 385/405 = 0.9506 —— 改前小屏 = 0.7605");

            // 空隙 = 作者系的留白 × 比例（桌面 1.8 / 18.0；小屏 2.25 / 22.5 = 桌面 ×1.25）
            N(cw - (fx2 - r.x1), (AuthorW - FrameX2) * cw / AuthorW, 0.5f, $"[{tn}] 右侧留白 == 10/250 × 格宽（桌面 1.8 / 小屏 2.25；改前小屏 58.05）");
            N(ch - (fy2 - r.y1), (AuthorH - FrameY2) * ch / AuthorH, 0.5f, $"[{tn}] 下侧留白 == 20/405 × 格高（桌面 18.0 / 小屏 22.5；改前小屏 109.125）");
        }

        // ---- 结论 B：主图层（`Deck Image` 或退路 `CardBack`）—— 两种画框都必须是**作者系值 × 比例**
        var img = cell.Find("Deck Image");
        var cb = cell.Find("CardBack");
        Tr(img != null || cb != null, $"[{tn}]（前提）主图层存在（`Deck Image` 或退路 `CardBack` 至少一颗）");
        var body = img != null ? img : cb;
        float bx1 = -9999f, by1 = -9999f, bx2 = -9999f, by2 = -9999f;
        bool bm = body != null && QuadRect(body, out bx1, out by1, out bx2, out by2);
        Tr(bm, $"[{tn}]（前提）主图层的矩形量得出来");
        if (bm)
        {
            float sx = cw / AuthorW, sy = ch / AuthorH;
            // 遮罩格那一档（`art_<id>.png` 已裁到插图）或整张 1024² 那一档 —— 只可能是这两者之一
            bool maskShape = Mathf.Abs((bx1 - r.x1) - ImgX1 * sx) <= 0.5f && Mathf.Abs((bx2 - r.x1) - ImgX2 * sx) <= 0.5f
                          && Mathf.Abs((by1 - r.y1) - ImgY1 * sy) <= 0.5f && Mathf.Abs((by2 - r.y1) - ImgY2 * sy) <= 0.5f;
            bool wholeShape = Mathf.Abs((bx1 - r.x1) - WholeX1 * sx) <= 0.5f && Mathf.Abs((bx2 - r.x1) - WholeX2 * sx) <= 0.5f
                           && Mathf.Abs((by1 - r.y1) - WholeY1 * sy) <= 0.5f && Mathf.Abs((by2 - r.y1) - WholeY2 * sy) <= 0.5f;
            Tr(maskShape || wholeShape, $"[{tn}] 主图层矩形 == 作者系画框 × (格宽/250, 格高/405)"
                + $"（实读 {bx1 - r.x1:F2},{by1 - r.y1:F2} → {bx2 - r.x1:F2},{by2 - r.y1:F2}；"
                + $"遮罩档期望 {ImgX1 * sx:F2},{ImgY1 * sy:F2} → {ImgX2 * sx:F2},{ImgY2 * sy:F2}）");
        }

        // ---- 结论 C：命中区 == 整格（`clip` 为 null ⇒ 不截；这条同时钉住「点击面覆盖整格、与内层缩放无关」）
        var hit = cell.Find("Hit");
        Tr(hit != null, $"[{tn}]（前提）`Hit` 层存在（`onClick` 非 null）");
        float hx1 = -9999f, hy1 = -9999f, hx2 = -9999f, hy2 = -9999f;
        bool hm = hit != null && NodeRect(hit, out hx1, out hy1, out hx2, out hy2);
        Tr(hm, $"[{tn}]（前提）`Hit` 的矩形量得出来");
        if (hm)
        {
            N(hx1 - r.x1, 0f, 0.5f, $"[{tn}] 命中区左沿 == 格子左沿");
            N(hx2 - r.x1, cw, 0.5f, $"[{tn}] 命中区右沿 == 格子右沿");
            N(hy2 - r.y1, ch, 0.5f, $"[{tn}] 命中区下沿 == 格子下沿");
        }
    }

    // ============================================================ ② 卡背光晕

    static void CosmoTier(bool small)
    {
        string tn = small ? "小屏" : "桌面";
        SmallScreenUI.Set(small);
        float cw = CollectionWindow.CosmoCellW, ch = CollectionWindow.CosmoCellH;
        N(cw, small ? 300f : 250f, 1e-3f, $"[{tn}]（前提）`CosmoCellW` == {(small ? "300" : "250")}（原版 250 × 小屏系数 1.2）");
        N(ch, small ? 486f : 405f, 1e-3f, $"[{tn}]（前提）`CosmoCellH` == {(small ? "486" : "405")}（原版 405 × 1.2）");

        // 格子锚在 (0,0) ⇒ 读数直接可比（滚动位移不影响 `r.W/r.H`，这里连位移也一起是 0）
        var r = new PxRect(0f, 0f, cw, ch);
        var sr = CollectionWindow.CosmoSdfRect(r);

        // 结论 A：桌面逐位不变（4 个字面量）
        N(sr.x1, small ? -51f : -42.5f, 0.5f, $"[{tn}] 光晕左沿 == {(small ? "−51（−42.5×1.2）" : "−42.5（原值）")}");
        N(sr.y1, small ? -85.05f : -70.875f, 0.5f, $"[{tn}] 光晕上沿 == {(small ? "−85.05（−70.875×1.2）" : "−70.875（原值）")}");
        N(sr.x2, small ? 354f : 295f, 0.5f, $"[{tn}] 光晕右沿 == {(small ? "354（295×1.2）" : "295（原值）")} —— 改前小屏这里恒 295 < 本体 300");
        N(sr.y2, small ? 575.91f : 479.925f, 0.5f, $"[{tn}] 光晕下沿 == {(small ? "575.91" : "479.925")}");

        // 结论 B：归一化 = 「光晕相对格子的四个比例」（作者系 −42.5/250 = −0.17 · 295/250 = 1.18 ·
        //   −70.875/405 = −0.175 · 479.925/405 = 1.185）—— 两档必须**同值**；改回写死常量时小屏那条必红。
        N((sr.x1 - r.x1) / cw, -0.17f, 2e-3f, $"[{tn}] （归一）左外扩 == −0.17 × 格宽（改前小屏 = −0.1417）");
        N((sr.x2 - r.x1) / cw, 1.18f, 2e-3f, $"[{tn}] （归一）右外扩 == 1.18 × 格宽（改前小屏 = 0.9833 < 1 ⇒ 光晕比本体窄）");
        N((sr.y1 - r.y1) / ch, -0.175f, 2e-3f, $"[{tn}] （归一）上外扩 == −0.175 × 格高");
        N((sr.y2 - r.y1) / ch, 1.185f, 2e-3f, $"[{tn}] （归一）下外扩 == 1.185 × 格高");

        // 结论 C：结构性 —— 光晕必须**四边都包住**本体（宽高比恒定 1.35 / 1.36）
        N(sr.W / cw, 1.35f, 2e-3f, $"[{tn}] 光晕宽 ÷ 格宽 == 337.5/250 = 1.35（改前小屏 = 1.125）");
        N(sr.H / ch, 1.360f, 2e-3f, $"[{tn}] 光晕高 ÷ 格高 == 550.8/405 = 1.36（改前小屏 = 550.8/486 = 1.1333）");
        Tr(sr.x1 < r.x1 && sr.y1 < r.y1 && sr.x2 > r.x2 && sr.y2 > r.y2, $"[{tn}] 光晕四边都在格子之外（原版 `Cardback Shadow SDF` 就是比卡背大一圈）");
    }
}
