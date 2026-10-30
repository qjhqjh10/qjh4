// B102RAID_Assert.cs — 执行代理 `B102_RAID`（第一百零二会话 · 日常/奖励线）的断言集
//
// ============================ 它盯的是什么（两条真残差）============================
//   账 `日常_奖励:621 / :622 / :623 / :624`（`RaidOrArmyReleaseMenuContainer` 的四条方法）在**宿主渲染那一半**的两条残差：
//     残差 ①  `Scoring Icon Holder` 只建空节点、不画图（原版那颗 `Image.m_Sprite` = `40k_campaign_Expansion Pass`）
//     残差 ②  `Background` / `Outline` / `Fill` 三张进度条图被当**单块拉伸**（原版是 `Image.Type = Sliced` 九宫）
//   判据文档 = `资料/完全复刻/判据/B102_R2_日常奖励B类五条.md` §三（prefab 原始 JSON 逐件现读）。
//   生产实现 = `Shell/MainMenuRuntime.cs` 的 `BuildEventProgression`（调用点）+ 类尾新增的 `EpNine`。
//   ⛔ 别在这里抄第二份几何；要改几何先改判据文档 + 生产实现。
//
// ============================ 🔴 灭自证（怎么才能红）============================
//   · `NineSliceStructure`：把 `EpNine(...)` 改回 `EpImage(...)`（= 单块拉伸）⇒ 每张图的 `Image` 根下只剩 **1** 颗 quad
//     （九宫是「根 + 每格一颗」，`m_Border` 的 y=0 ⇒ **3 块**）⇒ 「块数 = 3」那三条全红。
//   · `CornerWidths`：角块宽是**原版 `m_Border` 的值**（20/20/10），而**不随整条宽度变**。
//     `FillTwoLevels` 用 0.5 与 1.0 两档做「结构上不可能同时满足」的判别：
//     单块拉伸下 0.5 档的「左角块」会是 96.7、1.0 档会是 193.4 —— 两档都得 **10** 这一点**不可能**与拉伸同时成立。
//   · `Icon`：把 `EpImage(sihGo, "40k_campaign_Expansion_Pass", …)` 删掉（或换回 `40k_DeckSelection_icon_SpaceWolves`
//     那张占位图）⇒ sprite 名 / 矩形两条红。
//
// ============================ 怎么用（宿主）============================
//   宿主 = `Assets/CardPresentation/Editor/MainMenuScene.cs` 的 `Run()`。
//   在**已有的一行**（今天 = `:2536`，行尾已经挂着 `B99E7_Assert.Run(_sink);`）末尾追加 ` B102RAID_Assert.Run(_sink);`
//   （同一行 ⇒ 行号零漂移；先例见那一行自己的注释）。⚠️ 本批白名单不含宿主 ⇒ 这一行要**调度台**加；
//   加之前本文件一条都不跑（如实标）。
//   ⚠️ 本文件**自建探针**（一棵裸 `MainMenuRuntime` + 一张裸卡节点 535×848.8），不依赖宿主已建好的菜单；
//     也不建资产、不写盘、不跑 Unity 批处理。
//   🔴 **依赖**：`Icon` 那一组要 `Resources/Art/ui_menu/40k_campaign_Expansion_Pass.png` **已经导进来**
//     （`工具/import_original_art.py` 的 `MENU_IMAGES` 那一行 → 跑一次导入器）。**导之前那两条恒红**（这是有意的：
//     资源缺就是缺，⛔ 不许写成「量不到就跳过」）。

using UnityEngine;
using CardPresentation;

/// <summary>B102_RAID 的全部断言（宿主：`MainMenuScene.Run` 里 `B102RAID_Assert.Run(_sink);`）。</summary>
public static class B102RAID_Assert
{
    // 判据里的常数（`B102_R2` §三·①·b 那张表 · prefab 原始 JSON 现读）—— **只此一处**，下面全引用它。
    const float CardW = 535f, CardH = 848.8f;                 // 卡 `Base Game Mode Container 1x2 Raid or Army Release`（535 × 848.8）
    const float EpW = 386.781f, EpH = 59.39f, EpApY = 122f;    // `Event Progression`（ap (0,122) · size 386.781×59.39）
    const float EpX1 = (CardW - EpW) * 0.5f;                   // 74.1095
    const float EpY1 = CardH - EpApY - EpH;                    // 667.41（相对卡顶）
    const float BandY1 = CardH - EpApY - 0.8f * EpH;           // 679.288（中带 = Background/Outline/Fill 的上边）
    const float BandY2 = CardH - EpApY - 0.2f * EpH;           // 714.922（下边）
    const float BandH = BandY2 - BandY1;                       // 35.634
    const float IconSize = 102.307f;                           // `Scoring Icon Holder`（sizeDelta 102.307²）
    const float IconApX = 13.7845f, IconApY = 29.6906f;        // ap (13.7845, −29.6906) ⇒ 中心在 EP 左上角 + (13.7845, 29.6906)

    /// <summary>本批唯一入口。</summary>
    public static void Run(CheckSink s)
    {
        MenuCheck.Section(s, "B102_RAID —— 日常_奖励:621–624（B102_R2 §三）`Event Progression`：Scoring Icon Holder 占位图 + 三张进度条九宫");
        Bars(s);
        Icon(s);
        FillTwoLevels(s);
    }

    // ==============================================================================================
    //  A · 三条进度条 = 九宫（块数 / 角块宽 / 整条宽），`end` 仍是单块 Simple 拉伸
    // ==============================================================================================
    static void Bars(CheckSink s)
    {
        GameObject probeGo, cardGo;
        var rt = NewProbe(out probeGo);
        var card = NewCard(out cardGo);
        try
        {
            rt.BuildEventProgressionForTest(card, 0f, 0f, CardW, CardH, "3", "120/200", 0.5f, null);

            var ep = MenuCheck.FindChild(card, "Event Progression");
            var pb = ep != null ? MenuCheck.FindChild(ep, "ProgressBar") : null;
            var bg = pb != null ? MenuCheck.FindChild(pb, "Background") : null;
            var ol = pb != null ? MenuCheck.FindChild(pb, "Outline") : null;
            var fa = bg != null ? MenuCheck.FindChild(bg, "Fill Area") : null;
            var fill = fa != null ? MenuCheck.FindChild(fa, "Fill") : null;
            var end = fill != null ? MenuCheck.FindChild(fill, "end") : null;

            // ---- (前提) 三张图与 `end` 都建出来了。⛔ 不用 `if (前提) Check(结论)`（那是静默跳过）----
            MenuCheck.True(s, bg != null, "(前提) `Event Progression/ProgressBar/Background` 在（不在 ⇒ 下面 Background 那几条空转）");
            MenuCheck.True(s, ol != null, "(前提) `Event Progression/ProgressBar/Outline` 在（不在 ⇒ 下面 Outline 那几条空转）");
            MenuCheck.True(s, fill != null, "(前提) `Background/Fill Area/Fill` 在（fill01=0.5 ⇒ 应该建；不在 ⇒ 下面 Fill 那几条空转）");
            MenuCheck.True(s, end != null, "(前提) `Fill/end` 端帽在");

            var bgB = SliceBlocks(bg);
            var olB = SliceBlocks(ol);
            var fillB = SliceBlocks(fill);

            // ---- A0 ★ 结构：九宫根下 **3 块**（`m_Border` 的 y = 0 ⇒ 只有一行；横向 左/中/右 三列）----
            //      🧨 改回 `EpImage`（单块拉伸）⇒ 这三条全红（那时 `Image` 根下只有 1 颗 quad）
            MenuCheck.Check(s, bgB.Length, 3, "★ `Background` 是**九宫**（`Image` 根下 3 块 = 左/中/右）"
                        + "（🧨 改回 `EpImage` 单块拉伸 ⇒ 只剩 1 块）");
            MenuCheck.Check(s, olB.Length, 3, "★ `Outline` 是**九宫**（3 块）");
            MenuCheck.Check(s, fillB.Length, 3, "★ `Fill` 是**九宫**（3 块）");

            // ---- A1 ★ 角块宽 = 原版 `m_Border`（`ppuMul` 缺省 1.0 ⇒ 就是 20/20/10）----
            CornerWidths(s, "Background", bgB, 20f, 20f, EpX1, EpX1 + EpW);
            CornerWidths(s, "Outline", olB, 20f, 20f, EpX1, EpX1 + EpW);
            CornerWidths(s, "Fill", fillB, 10f, 10f, EpX1, EpX1 + 0.5f * EpW);

            // ---- A1 ★ 纵向：整条就是那 35.634（(0,0.2)-(1,0.8) 那条中带），九宫**没有**上下边（border y=0）----
            float bx1, by1, bx2, by2;
            bool ok = UnionOf(bgB, out bx1, out by1, out bx2, out by2);
            MenuCheck.True(s, ok, "(前提) 量到 `Background` 九宫块的并集矩形");
            MenuCheck.Near(s, by1, BandY1, 1.0f, "★ `Background` 上边 = 679.288（`ProgressBar` 底 726.8 − 0.8×59.39）");
            MenuCheck.Near(s, by2, BandY2, 1.0f, "★ `Background` 下边 = 714.922（高 35.634）");

            // ---- A1 ★ `end` 端帽**仍是单块**（原版 `m_Border` 全 0 = `Image.Type.Simple`）—— ⛔ 别顺手改成九宫 ----
            int endQuads = end != null ? end.GetComponentsInChildren<ImageQuad>(true).Length : 0;
            MenuCheck.Check(s, endQuads, 1, "★ `end` 端帽**仍然是单块**（原版 `40k_campaign_bar_end`：`m_Border` 全 0 · `m_Type = 0 Simple`）"
                        + "（🧨 顺手把 `end` 也改成九宫 ⇒ 本行红 —— 那不是原版行为，见 `B102_R2` §三·② D3）");
            float ex1 = 0f, ey1 = 0f, ex2 = 0f, ey2 = 0f;   // ⚠️ 必须给初值：下面 `&&` 短路时 out 不会执行 ⇒ 否则 CS0165
            bool eok = end != null && MenuDraw.QuadRectPx(OwnQuad(end), out ex1, out ey1, out ex2, out ey2);
            MenuCheck.True(s, eok, "(前提) 量到 `end` 的渲染矩形");
            if (eok)
            {
                MenuCheck.Near(s, ex2 - ex1, 22f, 0.6f, "★ `end` 宽 = 22（`sizeDelta.x`）");
                MenuCheck.Near(s, ey2 - ey1, 27.946f, 0.6f, "★ `end` 高 = 27.946（原版 y 方向 ×1.55 的 Simple 拉伸 —— 照抄，不是缺陷）");
            }
        }
        finally
        {
            Object.DestroyImmediate(cardGo);
            Object.DestroyImmediate(probeGo);
        }
    }

    // ==============================================================================================
    //  B · `Scoring Icon Holder` 的占位图（残差 ①）
    // ==============================================================================================
    static void Icon(CheckSink s)
    {
        GameObject probeGo, cardGo;
        var rt = NewProbe(out probeGo);
        var card = NewCard(out cardGo);
        try
        {
            rt.BuildEventProgressionForTest(card, 0f, 0f, CardW, CardH, null, null, -1f, null);

            var ep = MenuCheck.FindChild(card, "Event Progression");
            var sih = ep != null ? MenuCheck.FindChild(ep, "Scoring Icon Holder") : null;
            var sihQ = sih != null ? OwnQuad(sih) : null;

            MenuCheck.True(s, sih != null, "(前提) `Event Progression/Scoring Icon Holder` 在");
            // ⛔ 不用「量不到就跳过」：sprite 名取不到就给 `"<null>"`，让它自己红。
            string sprite = sihQ != null && sihQ.Texture != null ? sihQ.Texture.name : "<null>";
            MenuCheck.Check(s, sprite, "40k_campaign_Expansion_Pass",
                "★ `Scoring Icon Holder` 上画的是原版那颗 `40k_campaign_Expansion Pass`"
              + "（🧨 删掉 `BuildEventProgression` 里那句 `EpImage(sihGo, \"40k_campaign_Expansion_Pass\", …)`、"
              + "或换回占位图 `40k_DeckSelection_icon_SpaceWolves` ⇒ 本行红）"
              + "  ⚠️ 本条要 `Resources/Art/ui_menu/40k_campaign_Expansion_Pass.png` 已导入（`工具/import_original_art.py` 的 `MENU_IMAGES`）");

            float ix1 = 0f, iy1 = 0f, ix2 = 0f, iy2 = 0f;   // 同上：`&&` 短路 ⇒ 必须给初值
            bool ok = sihQ != null && MenuDraw.QuadRectPx(sihQ, out ix1, out iy1, out ix2, out iy2);
            MenuCheck.True(s, ok, "(前提) 量到 `Scoring Icon Holder` 自己那颗 quad 的矩形");
            if (ok)
            {
                MenuCheck.Near(s, ix2 - ix1, IconSize, 1.0f, "★ 图标宽 = 102.307（`Scoring Icon Holder` 的 `sizeDelta.x`，Simple 拉伸到插槽）");
                MenuCheck.Near(s, iy2 - iy1, IconSize, 1.0f, "★ 图标高 = 102.307");
                MenuCheck.Near(s, (ix1 + ix2) * 0.5f, EpX1 + IconApX, 1.0f,
                    "★ 图标中心 x = EP 左边 74.11 + ap.x 13.7845（anchor (0,1) · pivot (0.5,0.5)）");
                MenuCheck.Near(s, (iy1 + iy2) * 0.5f, EpY1 + IconApY, 1.0f,
                    "★ 图标中心 y = EP 顶边 667.41 + 29.6906（ap.y = −29.6906 在 Unity 里是**向下**）"
                  + "（🧨 把 `siH` 的 ap.y 符号写反 ⇒ 图标跑到 EP 上面 29.7 ⇒ 本行红）");
                MenuCheck.Near(s, iy2, 748.254f, 1.2f, "★ 图标下边 = 748.254（= 697.10 + 51.15；仍在卡内 < 848.8）");
            }
        }
        finally
        {
            Object.DestroyImmediate(cardGo);
            Object.DestroyImmediate(probeGo);
        }
    }

    // ==============================================================================================
    //  C · 🔴 灭自证：`Fill` 的两档进度 ⇒ **角块宽恒定、中段宽随进度变**（单块拉伸下不可能同时成立）
    // ==============================================================================================
    static void FillTwoLevels(CheckSink s)
    {
        float halfCorner = 0f, halfMid = 0f, fullCorner = 0f, fullMid = 0f;
        bool halfOk = FillWidths(s, 0.5f, out halfCorner, out halfMid);
        bool fullOk = FillWidths(s, 1.0f, out fullCorner, out fullMid);

        MenuCheck.True(s, halfOk && fullOk, "(前提) 两档（0.5 / 1.0）都量到了 `Fill` 的九宫块");
        MenuCheck.Near(s, halfCorner, 10f, 0.6f, "★ 0.5 档 `Fill` 左角块宽 = 10（原版 `m_Border.x`）");
        MenuCheck.Near(s, fullCorner, 10f, 0.6f, "★ 1.0 档 `Fill` 左角块宽**也** = 10（⛔ 不随整条宽度缩）");
        MenuCheck.Near(s, halfMid, 173.39f, 1.0f, "★ 0.5 档中段 = 193.39 − 10 − 10 = 173.39");
        MenuCheck.Near(s, fullMid, 366.78f, 1.0f, "★ 1.0 档中段 = 386.781 − 10 − 10 = 366.78");
        MenuCheck.True(s, Mathf.Abs(fullMid - halfMid) > 100f && Mathf.Abs(fullCorner - halfCorner) < 0.6f,
            "★★★ 灭自证：**中段随进度变、角块不变** —— 两档同时满足只可能来自九宫"
          + "（🧨 改回单块拉伸 ⇒ 0.5 档的「左角块」会量成 96.7、1.0 档量成 193.4 ⇒ 上面两条 + 本条全红）");
    }

    /// <summary>按指定进度建一次子树，量出 `Fill` 的**左角块宽**与**中段宽**（px）。返回 false = 没量到。</summary>
    static bool FillWidths(CheckSink s, float fill01, out float corner, out float mid)
    {
        corner = mid = 0f;
        GameObject probeGo, cardGo;
        var rt = NewProbe(out probeGo);
        var card = NewCard(out cardGo);
        try
        {
            rt.BuildEventProgressionForTest(card, 0f, 0f, CardW, CardH, null, null, fill01, null);
            var ep = MenuCheck.FindChild(card, "Event Progression");
            var pb = ep != null ? MenuCheck.FindChild(ep, "ProgressBar") : null;
            var bg = pb != null ? MenuCheck.FindChild(pb, "Background") : null;
            var fa = bg != null ? MenuCheck.FindChild(bg, "Fill Area") : null;
            var fill = fa != null ? MenuCheck.FindChild(fa, "Fill") : null;
            var b = SliceBlocks(fill);
            if (b.Length != 3) return false;
            float x1, y1, x2, y2;
            if (!MenuDraw.QuadRectPx(b[0], out x1, out y1, out x2, out y2)) return false;
            corner = x2 - x1;
            if (!MenuDraw.QuadRectPx(b[1], out x1, out y1, out x2, out y2)) return false;
            mid = x2 - x1;
            return true;
        }
        finally
        {
            Object.DestroyImmediate(cardGo);
            Object.DestroyImmediate(probeGo);
        }
    }

    // ==============================================================================================
    //  量法 / 夹具
    // ==============================================================================================

    /// <summary>三条进度条各自的**角块宽**与**整条宽**（角块 + 中段 + 角块 的并集）。</summary>
    static void CornerWidths(CheckSink s, string what, ImageQuad[] blocks, float wantL, float wantR,
                             float wantX1, float wantX2)
    {
        if (blocks.Length != 3)
        {
            // ⛔ 不跳过：块数不对就直接判红（上面 A0 已经报过一条，这里再钉一次「所以角块量不了」）
            MenuCheck.True(s, false, "★ `" + what + "` 的角块宽量不了（`Image` 根下只有 " + blocks.Length + " 块，九宫应为 3 块）");
            return;
        }
        float lx1, ly1, lx2, ly2;
        MenuCheck.True(s, MenuDraw.QuadRectPx(blocks[0], out lx1, out ly1, out lx2, out ly2), "(前提) 量到 `" + what + "` 左角块");
        MenuCheck.Near(s, lx2 - lx1, wantL, 0.6f,
            "★ `" + what + "` **左角块宽 = " + wantL.ToString("F0") + "**（原版 `m_Border.x`；`m_PixelsPerUnitMultiplier` 缺省 1.0 ⇒ 角块 = border 原值）");
        float rx1, ry1, rx2, ry2;
        MenuCheck.True(s, MenuDraw.QuadRectPx(blocks[2], out rx1, out ry1, out rx2, out ry2), "(前提) 量到 `" + what + "` 右角块");
        MenuCheck.Near(s, rx2 - rx1, wantR, 0.6f, "★ `" + what + "` **右角块宽 = " + wantR.ToString("F0") + "**");
        MenuCheck.Near(s, lx1, wantX1, 0.6f, "★ `" + what + "` 左缘 = " + wantX1.ToString("F2") + "（⛔ 角块没把整条撑出边界）");
        MenuCheck.Near(s, rx2, wantX2, 0.6f, "★ `" + what + "` 右缘 = " + wantX2.ToString("F2"));
    }

    static bool UnionOf(ImageQuad[] qs, out float x1, out float y1, out float x2, out float y2)
    {
        x1 = y1 = float.MaxValue; x2 = y2 = float.MinValue;
        int n = 0;
        for (int i = 0; i < qs.Length; i++)
        {
            float a, b, c, d;
            if (qs[i] == null || !MenuDraw.QuadRectPx(qs[i], out a, out b, out c, out d)) continue;
            if (a < x1) x1 = a; if (b < y1) y1 = b; if (c > x2) x2 = c; if (d > y2) y2 = d;
            n++;
        }
        if (n == 0) { x1 = y1 = x2 = y2 = 0f; return false; }
        return true;
    }

    /// <summary>某张进度条图**九宫根**（`Image`）下的全部块，按左缘 x 升序（左/中/右）。
    /// ⚠️ 走 `Image` 这个名字是**有意**的：`EpNine` 把它当九宫根传给 `ImageQuad.CreateNineSlice`，
    /// 块名是 `Image_&lt;i&gt;&lt;j&gt;`；而 `Fill` 的兄弟 `end` 不在这个根里（所以不会被算进 `Fill` 的块）。</summary>
    static ImageQuad[] SliceBlocks(Transform container)
    {
        var img = container != null ? MenuCheck.FindChild(container, "Image") : null;
        if (img == null) return new ImageQuad[0];
        var qs = img.GetComponentsInChildren<ImageQuad>(true);
        System.Array.Sort(qs, delegate (ImageQuad a, ImageQuad b)
        {
            float a1, b1, c1, d1, a2, b2, c2, d2;
            MenuDraw.QuadRectPx(a, out a1, out b1, out c1, out d1);
            MenuDraw.QuadRectPx(b, out a2, out b2, out c2, out d2);
            return a1.CompareTo(a2);
        });
        return qs;
    }

    /// <summary>取某个容器节点**自己那一颗** `ImageQuad`（= 直接子件里的）。</summary>
    static ImageQuad OwnQuad(Transform container)
    {
        if (container == null) return null;
        var qs = container.GetComponentsInChildren<ImageQuad>(true);
        for (int i = 0; i < qs.Length; i++) if (qs[i] != null && qs[i].transform.parent == container) return qs[i];
        return qs.Length > 0 ? qs[0] : null;
    }

    static MainMenuRuntime NewProbe(out GameObject go)
    {
        // ⚠️ 编辑模式下 `AddComponent` **不触发** Awake/Start ⇒ 裸组件即可（`BuildEventProgression` 不读 `_root`）。
        go = new GameObject("B102RAID Probe");
        return go.AddComponent<MainMenuRuntime>();
    }

    static Transform NewCard(out GameObject go)
    {
        go = new GameObject("B102RAID Card", typeof(RectTransform));
        MenuDraw.SetPxSize(go.transform, CardW, CardH);
        return go.transform;
    }
}
