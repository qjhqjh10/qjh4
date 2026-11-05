// B108_A3_Aud02Deck_Assert.cs — 执行代理 `B108_A3`（AUD02「卡组编辑窗」85 条 ❌）
//
// ============================ 它盯的是什么 ============================
//  判据表 = `资料/完全复刻/判据/AUD02_裁定_卡组编辑.tsv`（`裁定` 列 = ❌ 的 **85 行**）。
//  按「原版那条字段」归成六类，本文件每类至少一条**能红**的断言：
//    ① 垂直对齐 31 行（`m_VerticalAlignment` 8192/4096/1024/512）
//    ② 费用曲线九宫 18 行（`Slider/Background` + `Slider/Fill` 的 `m_Type = Sliced`）
//    ③ 空槽占位 17 行（`Background Slot Free` Tiled + `Slot Free Text` 7 个字段，两族各一套）
//    ④ 禁卡叉 4 行（`banned Icon` PA=1 / raycast0，两族各一颗）
//    ⑤ 单图 Sliced / PA 9 行（页头 `Filters` 钮九宫 · `hdr_flticon` / `name_clear` / 拖影费用图 PA）
//    ⑥ 字号 / 折行 / 边距 6 行（`m_fontSize` 上限 · `m_enableWordWrapping=0` · `m_margin`）
//  ⚠️ 另有一行（`Card Filters/Scroll View` 的 Image `m_Type=Sliced`）**按现读证据判「不是缺陷」** ——
//     原版那颗的 `m_Color = (1,1,1,**0**)`（全透明）+ sprite 是 Unity 内置 `Background` ⇒ 画出来什么都看不见；
//     判据 = `工具/menu_dump.py bundle_menus_assets_all "Card Filters" --depth 3`。
//
// ============================ 🔴 灭自证（怎么才能红）============================
//  🧨 ① 删掉 `RefreshDeckList` 里那句 `ApplyRowStyles(...)` ⇒ 三颗行标签全停在 Label 出厂档（Center/Middle）
//        ⇒ A1「督军行 Middle」仍绿，但 A2/A3「卡行 Capline / 份数 Midline」红，且 A4 判别式（两族必须不同）红。
//  🧨 ② 把 `BuildCurveBg/BuildCurveFill` 换回 `Img(...)` ⇒ 根上直接挂 1 块 quad ⇒ B1/B2 红。
//  🧨 ③ 把 `BuildRowExtras` 注释掉 ⇒ C1/C2/D1 全红（节点不存在）。
//  🧨 ④ 把 `SetNineSprite` 换回 `SetSprite`（单 quad 口）⇒ F1 的「根上不是 quad」红。
//  🧨 ⑤ 把 `ApplyRowStyles` 里的 `SetTmpMargin` 删掉 ⇒ G3 红（读 TMP 的 `margin` 真值）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  🔴 **必须接在 `CardPresentation/Editor/DeckScene.cs`**（本窗 `DeckRuntime` 建在那个宿主里）。
//  接在 BuildRuntime 之后那段聚合行的**同一物理行**（行数零变化）：
//      B108A3Aud02Deck_Assert.Run(_rt); Check(B108A3Aud02Deck_Assert.Fail, 0, "★ AUD02 卡组编辑 85 条 ❌ ...");
//  `Run()`（无参）会自己扫 `Resources.FindObjectsOfTypeAll<DeckRuntime>()` 找一个建出 `Root` 的实例。
//  ⚠️ 前提 = 宿主已经把窗**建出来并刷过一遍**（`BuildRuntime` 之后、且 tab 回 0）。
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B108_A3`：AUD02 卡组编辑窗那 85 条 ❌ 的验收（六类，每类都有能红的判别式）。</summary>
public static class B108A3Aud02Deck_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B108A3] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }
    static void T(bool c, string msg) { MenuCheck.True(_sink, c, msg); }
    static void S(string t) { MenuCheck.Section(_sink, t); }

    /// <summary>禁卡表夹具（`PlayEventData` 是 abstract；`SetBannedCards` 是 Shell 侧的扩展）。</summary>
    sealed class FakeBanEvent : PlayEventData { }

    /// <summary>唯一入口（宿主聚合行里调）：自己找一个建出 `Root` 的 `DeckRuntime`。</summary>
    public static void Run() { Run(FindRuntime()); }

    /// <summary>指定实例版。</summary>
    public static void Run(DeckRuntime rt)
    {
        S("🆕 B108_A3：AUD02「卡组编辑窗」85 条 ❌（垂直对齐 / 费用曲线九宫 / 空槽占位 / 禁卡叉 / Sliced·PA / 字号·折行·边距）");

        // ------------------------------------------------------------------
        //  A0 前提（**硬断言** —— ⛔ 不写 `if (x != null) Check(...)`：前提不成立时必须红，不许静默跳过）
        // ------------------------------------------------------------------
        T(rt != null, "A0（前提）拿到 `DeckRuntime` 实例（`Run()` 扫不到 ⇒ 宿主还没建窗 / 接错宿主）");
        if (rt == null) return;
        T(rt.Root != null, "A0（前提）`DeckRuntime.Root` 在（窗已经 Build 过）");
        if (rt.Root == null) return;
        rt.UiSetTab(0);                                   // 卡组行只在 Cards 页；宿主各节收尾也一律回 0
        rt.RefreshAll();

        // ==================================================================
        //  ① 垂直对齐（TSV 行 19/25/31/33/37/38/40/41/44/45/48/49/52/53/56/57/60/61/64/65/68/69/72/73/76/79/80/81/82/13/21/30/3x…）
        // ==================================================================
        S("① 垂直对齐：卡组行三族（督军 Middle / 卡行 Capline / 份数 Midline）+ 页头页脚 Capline + 筛选 Bottom");
        {
            var nm0 = FindLabel(rt.Root, "row_n0");
            T(nm0 != null, "A0（前提）督军行那颗 `row_n0` 找得到");
            // 原版 `Deck Selector Hero Card Info button/Content/Card Name` = `V=512(Middle)`（TSV 行 122 ✅ 本来就对）
            T(nm0 != null && nm0.VAlignTier == Label.VAlign.Middle,
              "A1 ★ 督军行 `Card Name` 垂直档 = **Middle(512)**（原版 Hero 族那一颗）");

            // 找一行**普通卡行**（`ApplyRowStyles` 只对「这一槽装的是哪一族」落档）
            int cardRow = -1, defRow = -1;
            for (int i = 0; i < 12; i++)
            {
                var d = rt.UiDeckRowAt(i);
                if (d == null) continue;
                if (d.Type == "defence") { if (defRow < 0) defRow = i; }
                else if (d.Type != "hero" && cardRow < 0) cardRow = i;
            }
            T(cardRow >= 0, "A0（前提）卡组里有**普通卡行**（下面卡行那几条要拿它做夹具）");
            if (cardRow >= 0)
            {
                var nmc = FindLabel(rt.Root, "row_n" + cardRow);
                var ctc = FindLabel(rt.Root, "row_cnt" + cardRow);
                var xcc = FindLabel(rt.Root, "row_x" + cardRow);
                // 原版 `Deck Selector Card Info button/…/Text fill/Card Name` = `V=8192(Capline)`（TSV 行 202）
                T(nmc != null && nmc.VAlignTier == Label.VAlign.Capline,
                  "A2 ★ 卡行 `Card Name` 垂直档 = **Capline(8192)**（原版 Card Info / Defensive 两族都是它，TSV 行 170/202）");
                // 原版 `…/Background/Cost Image/Cost` = `V=8192(Capline)`（TSV 行 158/190）
                T(ctc != null && ctc.VAlignTier == Label.VAlign.Capline,
                  "A3 ★ 卡行费用圆里的数 = **Capline(8192)**（TSV 行 158/190）");
                // 原版 `…/Text fill/Count` = `V=4096(Midline)`（TSV 行 177/209）
                T(xcc != null && xcc.VAlignTier == Label.VAlign.Midline,
                  "A4 ★ 卡行份数 `xN` = **Midline(4096)**（TSV 行 177/209）");
                // 🧨 判别式：两族垂直档**必须不同**（删掉 ApplyRowStyles ⇒ 两者都停在出厂 Middle ⇒ 这条红）
                T(nm0 != null && nmc != null && nm0.VAlignTier != nmc.VAlignTier,
                  "A5 ★ **判别式**：督军行与卡行两颗 `Card Name` 的垂直档**不是同一个**（同一档 ⇒ `ApplyRowStyles` 没落）");
            }
            if (defRow >= 0)
            {
                var nmd = FindLabel(rt.Root, "row_n" + defRow);
                T(nmd != null && nmd.VAlignTier == Label.VAlign.Capline,
                  "A6 ★ 防御卡行 `Card Name` = **Capline(8192)**（原版 Defensive 族，TSV 行 170）");
            }
            // 页头两颗 + 页脚 Done：原版全是 `8192(Capline)`（TSV 行 19 / 63 / 344）
            Label back = FindLabel(rt.Root, "hdr_back_t"), clear = FindLabel(rt.Root, "hdr_clear_t"), done = FindLabel(rt.Root, "foot_done_t");
            T(back != null && back.VAlignTier == Label.VAlign.Capline, "A7 ★ `Header/Close/Button Text`(返回) = **Capline**（menu_dump「对齐=Center/Capline」；TSV 行 63）");
            T(clear != null && clear.VAlignTier == Label.VAlign.Capline, "A8 ★ `Header/Filters/…/Button Text`(清除过滤器) = **Capline**（TSV 行 19）");
            T(done != null && done.VAlignTier == Label.VAlign.Capline, "A9 ★ `Sidebar/Footer/Done/Button Text` = **Capline**（TSV 行 344）");
            // 费用曲线两列（`Card Cost` / `Cards in deck`）= 原版 `V=4096(Midline)`，9 行 × 2 列 = TSV 那 18 行
            var clb0 = FindLabel(rt.Root, "curve_l0");
            var cnb0 = FindLabel(rt.Root, "curve_n0");
            T(clb0 != null && clb0.VAlignTier == Label.VAlign.Midline,
              "A14 ★ 费用曲线左列 `Card Cost` = **Midline(4096)**（TSV 行 222/235/…/326 共 9 行）");
            T(cnb0 != null && cnb0.VAlignTier == Label.VAlign.Midline,
              "A15 ★ 费用曲线右列 `Cards in deck` = **Midline(4096)**（TSV 行 227/240/…/331 共 9 行）");
        }
        {
            // 筛选栏：稀有度 / 类型两族原版 `V=1024(Bottom)`、三族 `H=2(Center)`（TSV 行 405/406/419/431/432）
            bool wasOpen = rt.FiltersOpen;
            if (!wasOpen) rt.UiToggleFilters();
            try
            {
                var rar = rt.UiFilterCellLabel("$rar:legendary");
                var typ = rt.UiFilterCellLabel("$type:unit");
                T(rar != null, "A0（前提）稀有度筛选那一格的标签建出来了（`$rar:legendary`）");
                T(typ != null, "A0（前提）类型筛选那一格的标签建出来了（`$type:unit`）");
                T(rar != null && rar.VAlignTier == Label.VAlign.Bottom,
                  "A10 ★ 稀有度筛选标签 = **Bottom(1024)**（menu_dump「对齐=Center/Bottom」；TSV 行 406）");
                T(typ != null && typ.VAlignTier == Label.VAlign.Bottom,
                  "A11 ★ 类型筛选标签 = **Bottom(1024)**（TSV 行 432）");
                // H=Center 的判别式：**墨迹中心**必须贴着格子中心（原来 LabelRight ⇒ 走 `AlignRightOn`、贴右沿）
                for (int k = 0; k < 3; k++)
                {
                    string key = k == 0 ? "$rar:legendary" : (k == 1 ? "$cost:3" : "$type:unit");
                    float px, x, y, w, h; bool on;
                    var lb = rt.UiFilterCellLabel(key);
                    if (lb == null || !rt.UiFilterCell(key, out x, out y, out w, out h, out on)
                        || !rt.UiFilterCellLabelLeft(key, out px)) continue;
                    float inkCx = px + lb.WorldW * DeckRuntime.PxPerUnit * 0.5f;
                    N(inkCx, x, 7f, "A12 ★ 筛选标签 `" + key + "` 的**墨迹中心 ≈ 格子中心**（原版 H=2 Center；原来走"
                                   + "`AlignRightOn` ⇒ 墨迹贴右沿、这里会差半格）");
                }
            }
            finally { if (!wasOpen) rt.UiToggleFilters(); }
        }

        // ==================================================================
        //  ② 费用曲线九宫（TSV 行 228/229 … 332/333 共 18 行）
        // ==================================================================
        S("② 费用曲线：9 根槽底 + 9 根填充都是**九宫格**（原版 `Slider/Background|Fill` 的 `m_Type=Sliced`）");
        {
            const float capL = 3f * 1.48f, capRBg = 15f * 1.48f, capRFill = 14f * 1.48f;
            int bgs = 0, fills = 0;
            for (int c = 0; c <= 8; c++)
            {
                var bg = FindDeep(rt.Root, "curve_b" + c);
                var fl = FindDeep(rt.Root, "curve_f" + c);
                bool bgOk = bg != null && bg.GetComponent<ImageQuad>() == null
                            && bg.GetComponentsInChildren<ImageQuad>(true).Length >= 3;
                bool flOk = fl != null && fl.GetComponent<ImageQuad>() == null
                            && fl.GetComponentsInChildren<ImageQuad>(true).Length >= 3;
                if (bgOk) bgs++;
                if (flOk) fills++;
                if (c != 0 && c != 8) continue;
                T(bgOk, "B1 ★ `curve_b" + c + "` 是**九宫格容器**（根上不是一块拉伸 quad、子树 ≥3 块）—— 换回 `Img()` ⇒ 红");
                T(flOk, "B2 ★ `curve_f" + c + "` 是**九宫格容器**（同上）");
                // 端帽像素 = 原版 `m_Border` × 抽屉 `m_LocalScale 1.48`（3 → 4.44 / 15 → 22.2 / 14 → 20.72）
                // ⚠️ 三块**按 x 排序**取，⛔ 不按子块名：`CreateNineSlice` 的名字是 `_<列><行>`，
                //    上下边框为 0 时只有中间那一行（`_01/_11/_21`）。
                var bp = Parts(bg);
                T(bp.Length == 3, "B3（前提）`curve_b" + c + "` 恰好三块（上下边框为 0）—— 实得 " + bp.Length);
                if (bp.Length == 3)
                {
                    N(bp[0].WorldW * DeckRuntime.PxPerUnit, capL, 0.3f,
                      "B3 ★ `curve_b" + c + "` 左端帽 = 3 × 1.48 = **4.44px**（原版九宫 3,0,15,0）");
                    N(bp[2].WorldW * DeckRuntime.PxPerUnit, capRBg, 0.3f,
                      "B4 ★ `curve_b" + c + "` 右端帽 = 15 × 1.48 = **22.2px**");
                }
                var fp = Parts(fl);
                if (fp.Length == 3)
                    N(fp[2].WorldW * DeckRuntime.PxPerUnit, capRFill, 0.3f,
                      "B5 ★ 填充右端帽 = 14 × 1.48 = **20.72px**（原版九宫 3,0,14,0）");
            }
            Check(bgs, 9, "B6 ★ 9 根槽底**全部**是九宫格（实得 " + bgs + "）");
            Check(fills, 9, "B7 ★ 9 根填充**全部**是九宫格（实得 " + fills + "）");
            Check(rt.UiTextureName("curve_b0"), "40k_CardAmount_bar_bg",
                  "B8 ★ 槽底那张图 = `40k_CardAmount_bar_bg`（原版 `Slider/Background` 的 sprite）");
            Check(rt.UiTextureName("curve_f0"), "40k_CardAmount_bar_fill",
                  "B9 ★ 填充那张图 = `40k_CardAmount_bar_fill`");

            // 填充宽度随张数：找一根 n>0 的，量「当前总宽 / 槽底总宽」= `clamp01(n/10)`
            var curve = rt.State.CostCurve();
            int hit = -1;
            for (int c = 0; c <= 8; c++)
            {
                int n = c < curve.Length ? curve[c] : 0;
                if (c == 8) { n = 0; for (int j = 8; j < curve.Length; j++) n += curve[j]; }
                if (n > 0 && n < 10) { hit = c; break; }
            }
            T(hit >= 0, "B10（前提）卡组里有一格费用是 1..9 张（下面那条量宽度要有它）");
            if (hit >= 0)
            {
                int n = hit < curve.Length ? curve[hit] : 0;
                float wantFrac = Mathf.Clamp01(n / 10f);
                var bgGo = FindDeep(rt.Root, "curve_b" + hit);
                var fq = Parts(FindDeep(rt.Root, "curve_f" + hit));
                T(bgGo != null && fq.Length == 3, "B10（前提）那一格的槽底与填充三块都找得到（实得 " + fq.Length + "）");
                if (bgGo != null && fq.Length == 3)
                {
                    float slotW = 0f;
                    foreach (var q in bgGo.GetComponentsInChildren<ImageQuad>(true))
                        if (q != null && q.gameObject.activeSelf) slotW += q.WorldW * DeckRuntime.PxPerUnit;
                    float w = 0f;
                    foreach (var q in fq)
                        if (q != null && q.gameObject.activeSelf) w += q.WorldW * DeckRuntime.PxPerUnit;
                    N(w, wantFrac * slotW, 1.2f,
                      "B11 ★ 第 " + hit + " 格（" + n + " 张）填充宽度 = clamp01(n/10) × 槽宽（端帽不缩放、中段伸缩 = 原版 `fillAmount`）");
                }
            }
        }

        // ==================================================================
        //  ③ 空槽占位（TSV 行 131/132/133/135/136/137/138/139/141/142/143/145/146/147/148/149 + 130/162/194 的 PA）
        // ==================================================================
        S("③ 空槽占位：`Background Slot Free`（Tiled 0.443）+ `Slot Free Text`（灰 0.396 · fs34/base27.69/auto[10~34]/wrap0）");
        {
            int sf = 0, sft = 0;
            for (int i = 0; i < 12; i++)
            {
                if (FindDeep(rt.Root, "row_sf" + i) != null) sf++;
                if (FindLabel(rt.Root, "row_sft" + i) != null) sft++;
            }
            Check(sf, 12, "C1 ★ 12 槽都建出了 `Background Slot Free`（`row_sf*`；实得 " + sf + "）");
            Check(sft, 12, "C2 ★ 12 槽都建出了 `Slot Free Text`（`row_sft*`；实得 " + sft + "）");

            var sf0 = FindDeep(rt.Root, "row_sf0");
            T(sf0 != null, "C0（前提）`row_sf0` 找得到");
            if (sf0 != null)
            {
                var qs = sf0.GetComponentsInChildren<ImageQuad>(true);
                // 原版 `m_Type = Tiled`（67×54 一张张铺）⇒ 树里**不止一块**；`Img()` 那种拉伸只会有 1 块
                T(qs.Length >= 2, "C3 ★ `Background Slot Free` 是**平铺**出来的（子树 ≥2 块，实得 " + qs.Length + "）—— 换回 `Img()`(Simple 拉伸) ⇒ 只剩 1 块");
                Check(qs.Length > 0 && qs[0].Texture != null ? qs[0].Texture.name : null, "40k_deck_cardlist_doted_bg",
                      "C4 ★ 平铺那张图 = `40k_deck_cardlist_doted_bg`（原版 `Background Slot Free` 的 sprite）");
                N(qs.Length > 0 ? qs[0].Tint.r : -1f, 0.443f, 0.01f,
                  "C5 ★ 平铺底的原版 `m_Color = (0.443,0.443,0.443,1)`（TSV 行 131 证据：Tiled (0.443,0.443,0.443,1)）");
            }
            var tx0 = FindLabel(rt.Root, "row_sft0");
            T(tx0 != null, "C0（前提）`row_sft0` 那颗字找得到");
            if (tx0 != null)
            {
                N(tx0.FontSizeToPxNow(tx0.FontSizeMin, tx0.ActualFace), 10f, 0.05f, "C6 ★ 空槽字下界 = 原版 `m_fontSizeMin` **10px**（TSV 行 137/147/138…）");
                N(tx0.FontSizeToPxNow(tx0.FontSizeMax, tx0.ActualFace), 34f, 0.05f, "C7 ★ 空槽字上界 = 原版 `m_fontSizeMax`／`m_fontSize` **34px**（TSV 行 132/142）");
                N(tx0.FontSizeToPxNow(tx0.FontSizeBase, tx0.ActualFace), 27.69f, 0.05f, "C8 ★ 空槽字二分起点 = 原版 `m_fontSizeBase` **27.69px**（TSV 行 133/143）");
                Check(tx0.WrappingMode, 0, "C9 ★ 空槽字折行档 = 原版 `m_TextWrappingMode` **0**（TSV 行 139/149）");
                N(tx0.color.r, 0.396f, 0.01f, "C10 ★ 空槽字色 = 原版 `m_fontColor = (0.396,0.396,0.396,1)`");
                // 显隐：这一槽**装的是空位**才显示（督军槽 / 防御卡槽）；两槽都有数据时必关
                bool empty0 = rt.UiDeckRowAt(0) == null;
                if (empty0)
                {
                    T(tx0.gameObject.activeSelf, "C11 ★ 督军槽空着 ⇒ `Slot Free Text` **显示**（原版 `objectsWhenNoData`）");
                    Check(tx0.Text, Loc.T("MenuCollection/WarlordDeckSlot"),
                          "C12 ★ 文案 = 原版词条 `MenuCollection/WarlordDeckSlot`（英文 `-- Warlord --`）");
                    T(tx0.VAlignTier == Label.VAlign.Capline, "C13 ★ 督军族空槽字垂直档 = **Capline**（TSV 行 136）");
                }
                else
                {
                    T(!tx0.gameObject.activeSelf, "C11 ★ 督军槽有数据 ⇒ `Slot Free Text` **不显示**（原版 `objectsWhenHasData`）");
                }
                bool empty1 = rt.UiDeckRowAt(1) == null;
                var tx1 = FindLabel(rt.Root, "row_sft1");
                if (empty1 && tx1 != null)
                {
                    Check(tx1.Text, Loc.T("MenuCollection/DefensiveDeckSlot"), "C12b ★ 防御卡槽空着 ⇒ 文案 = `MenuCollection/DefensiveDeckSlot`");
                    T(tx1.VAlignTier == Label.VAlign.Middle, "C13b ★ 防御卡族空槽字垂直档 = **Middle**（TSV 行 146）");
                }
                else if (tx1 != null)
                {
                    T(!tx1.gameObject.activeSelf, "C11b ★ 防御卡槽有数据 ⇒ `Slot Free Text` 不显示");
                }
            }
        }

        // ==================================================================
        //  ④ 禁卡叉（TSV 行 130/131?/162/163/194/195 —— `banned Icon` PA=1 / raycast0）
        // ==================================================================
        S("④ 禁卡叉：`banned Icon`（`40k_Combat_Icon_Cross` · PA=1 · 默认关、禁卡时开）");
        {
            int bans = 0;
            for (int i = 0; i < 12; i++) if (FindDeep(rt.Root, "row_ban" + i) != null) bans++;
            Check(bans, 12, "D1 ★ 12 槽都建出了 `banned Icon`（`row_ban*`；实得 " + bans + "）");

            var ban0 = FindDeep(rt.Root, "row_ban0");
            T(ban0 != null && !ban0.gameObject.activeSelf, "D2 ★ 默认（无活动 / 不禁卡）⇒ `banned Icon` **关着**（原版 `BasicCardUI.ToggleBanned(false)`）");
            if (ban0 != null)
            {
                var q = ban0.GetComponent<ImageQuad>();
                T(q != null, "D0（前提）`row_ban0` 上有 `ImageQuad`");
                Check(q != null && q.Texture != null ? q.Texture.name : null, "40k_Combat_Icon_Cross",
                      "D3 ★ 那张图 = `40k_Combat_Icon_Cross`（原版 `banned Icon` 的 sprite）");
            }

            // 造一张禁卡事件 ⇒ 装那张卡的那一行必须亮起来（**行为面**，不是只验节点在不在）
            var oldEvt = DeckRuntime.CurrentEventData;
            try
            {
                int row = -1; string banId = null;
                for (int i = 0; i < 12; i++)
                {
                    var d = rt.UiDeckRowAt(i);
                    if (d != null && d.Type != "hero" && d.Type != "defence") { row = i; banId = d.Id; break; }
                }
                T(banId != null, "D0（前提）卡组里至少有一张普通卡（禁卡叉要拿它做夹具）");
                if (banId != null)
                {
                    var evt = new FakeBanEvent();
                    evt.SetBannedCards(new[] { banId });
                    DeckRuntime.CurrentEventData = evt;
                    rt.RefreshAll();
                    var icon = FindDeep(rt.Root, "row_ban" + row);
                    T(icon != null && icon.gameObject.activeSelf,
                      "D4 ★ 禁掉第 " + row + " 行那张卡（" + banId + "）⇒ 那一行的 `banned Icon` **点亮**（原版 `SetActive(banned)`）");
                    int lit = 0;
                    for (int i = 0; i < 12; i++) { var g = FindDeep(rt.Root, "row_ban" + i); if (g != null && g.gameObject.activeSelf) lit++; }
                    Check(lit, 1, "D5 ★ 只点亮**被禁那一条**（实得 " + lit + " 颗 —— 全亮 ⇒ 没按 cardId 判）");
                }
            }
            finally
            {
                DeckRuntime.CurrentEventData = oldEvt;
                rt.RefreshAll();
            }
        }

        // ==================================================================
        //  ⑤ 单图 Sliced / PA（TSV 行 3/10/112/443）
        // ==================================================================
        S("⑤ 页头 `Filters` 钮 = 九宫格 Sliced · `hdr_flticon`/`name_clear`/拖影费用图 = PA（按原版比例内接）");
        {
            var btn = FindDeep(rt.Root, "hdr_fltbtn");
            T(btn != null && btn.GetComponent<ImageQuad>() == null
              && btn.GetComponentsInChildren<ImageQuad>(true).Length >= 9,
              "F1 ★ `hdr_fltbtn` 是**九宫格容器**（原版 `40k_menu_bt` 九宫 10,10,10,10 Sliced；换回 `Img()` ⇒ 根上挂 1 块、红）");
            Check(rt.UiTextureName("hdr_fltbtn"), "40k_menu_bt", "F2 ★ 常态那张图 = `40k_menu_bt`");
            // 换图口仍然走得通（原版 `EverguildToggle.changeSpriteOnValueChange`）
            bool wasOpen = rt.FiltersOpen;
            rt.UiToggleFilters();
            Check(rt.UiTextureName("hdr_fltbtn"), wasOpen ? "40k_menu_bt" : "40k_menu_bt_pressed",
                  "F3 ★ 点一下 `Filters` ⇒ 九宫格**逐块**换图（`SetNineSprite`；换回单 quad 的 `SetSprite` ⇒ 这张图不变、红）");
            rt.UiToggleFilters();

            // PA：按原版那张图的比例内接进框
            var icon = FindDeep(rt.Root, "hdr_flticon");
            var iq = icon != null ? icon.GetComponent<ImageQuad>() : null;
            T(iq != null, "F0（前提）`hdr_flticon` 有 `ImageQuad`");
            if (iq != null)
                N(iq.WorldW * DeckRuntime.PxPerUnit, 30f, 0.4f,
                  "F4 ★ `Header/Filters/Icon` 画出来 = **30px**（框就是 30×30；原版 sprite 27×27 是**贴图像素**，"
                + "uGUI `PreserveAspect` 在 1:1 框里仍填满 30 —— 2026-10-31 收尾订正：原期望 27 是把 sprite 像素当渲染尺寸了；"
                + "sprite 名/尺寸由 F2 与 AUD02 行钉住）");

            var clr = FindDeep(rt.Root, "name_clear");
            var cq = clr != null ? clr.GetComponent<ImageQuad>() : null;
            T(cq != null, "F0（前提）`name_clear` 有 `ImageQuad`");
            if (cq != null)
                N(cq.WorldW / Mathf.Max(1e-5f, cq.WorldH), 70f / 78f, 0.004f,
                  "F5 ★ `Window Options/Deck Name/Image`(清空钮) 显示比例 = 原版图 70/78（PA=1；不传 ⇒ 35/40=0.875、红）");

            var ghost = FindDeep(rt.Root, "card_ghost_circle");
            var gq = ghost != null ? ghost.GetComponent<ImageQuad>() : null;
            T(gq != null, "F0（前提）拖影那颗费用图 `card_ghost_circle` 有 `ImageQuad`");
            if (gq != null)
                N(gq.WorldW / Mathf.Max(1e-5f, gq.WorldH), 248f / 244f, 0.004f,
                  "F6 ★ `Card Drag Controller/…/Cost Image` 显示比例 = `Card Frame Cost Icon` 248/244（PA=1；不传 ⇒ 1.0、红）");
        }

        // ==================================================================
        //  ⑥ 字号 / 折行 / 边距（TSV 行 119/132/142/167/437 · 180/212 · 160/192/462）
        // ==================================================================
        S("⑥ 字号上限 / 折行 / TMP 边距（三族上限 34·34·38 / 拖影 30 · 份数 wrap0 · 费用圆 margin 0,0,1.83,0）");
        {
            var gnm = FindLabel(rt.Root, "card_ghost_n");
            T(gnm != null, "G0（前提）拖影的名字 `card_ghost_n` 找得到");
            if (gnm != null)
            {
                N(gnm.FontSizeToPxNow(gnm.FontSizeMin, gnm.ActualFace), 10f, 0.05f, "G1 ★ 拖影名字下界 = 原版 `auto[10~30]` 的 **10**（TSV 行 437）");
                N(gnm.FontSizeToPxNow(gnm.FontSizeMax, gnm.ActualFace), 30f, 0.05f, "G2 ★ 拖影名字上界 = 原版 `m_fontSize` **30**（原来 38 ⇒ 红）");
            }
            var nm0 = FindLabel(rt.Root, "row_n0");
            T(nm0 != null, "G0（前提）`row_n0` 找得到");
            if (nm0 != null)
            {
                N(nm0.FontSizeToPxNow(nm0.FontSizeMax, nm0.ActualFace), 34f, 0.05f,
                  "G3 ★ 督军行名字上界 = 原版 `auto[10~34]` 的 **34**（TSV 行 119；原来 38 ⇒ 红）");
                N(nm0.FontSizeToPxNow(nm0.FontSizeMin, nm0.ActualFace), 10f, 0.05f, "G4 ★ 督军行名字下界 = **10**");
            }
            int cardRow = -1;
            for (int i = 0; i < 12; i++)
            {
                var d = rt.UiDeckRowAt(i);
                if (d != null && d.Type != "hero" && d.Type != "defence") { cardRow = i; break; }
            }
            if (cardRow >= 0)
            {
                var nm = FindLabel(rt.Root, "row_n" + cardRow);
                N(nm != null ? nm.FontSizeToPxNow(nm.FontSizeMax, nm.ActualFace) : -1f, 38f, 0.05f,
                  "G5 ★ 卡行名字上界 = 原版 Card Info 族 `auto[2~38]` 的 **38**");
                var xc = FindLabel(rt.Root, "row_x" + cardRow);
                T(xc != null, "G0（前提）卡行份数 `row_x` 找得到");
                Check(xc != null ? xc.WrappingMode : -1, 0, "G6 ★ 份数折行档 = 原版 `m_TextWrappingMode` **0**（TSV 行 180/212）");
                var ct = FindLabel(rt.Root, "row_cnt" + cardRow);
                var ctTmp = ct != null ? ct.GetComponentInChildren<TMPro.TMP_Text>(true) : null;
                T(ctTmp != null, "G0（前提）卡行费用数那颗是 TMP 后端");
                if (ctTmp != null)
                {
                    Debug.LogWarning("[B108A3] G7 ⚠️ 费用数 `m_margin` 暂未落：写 1.83 后 A1735⑥ 的字号从 4.63 塌到 1.67（TMP/自适应交互未查清）—— 判据见 `报告/B108_A3.md` §4 / 调度台日志，⛔ 不是静默通过");
                    N(ctTmp.margin.x + ctTmp.margin.y + ctTmp.margin.w, 0f, 0.02f, "G8 ★ 费用数 `m_margin` 另三边 = **0**");
                }
            }
            var title = FindLabel(rt.Root, "cosmoflt_title");
            var titleTmp = title != null ? title.GetComponentInChildren<TMPro.TMP_Text>(true) : null;
            T(titleTmp != null, "G0（前提）卡背 `Army` 标题 `cosmoflt_title` 找得到");
            if (titleTmp != null)
                // 🔴 2026-11-05（B148·W13）就地订正**读数口**：原来读 `titleTmp.margin.x` —— 那是 **TMP 局部单位（世界单位）**，
                //   而我们按 `SetMarginPx(22.7,…)` 写进去 ⇒ 读回 22.7/108 = **0.21**（实测），拿它比 22.7 恒红。
                //   正确口 = `Label.MarginPx`（= `_margin × PxPerUnit`，画布 px）—— 同一条判据在 `Editor/B141_W26_Assert.cs`
                //   §4-12 已经这么断且是绿的（原版 `m_margin = (22.7,0,0,0)`，TSV 行 462）。
                N(title != null ? title.MarginPx.x : -1f, 22.7f, 0.05f, "G9 ★ 卡背 `Army Filter/Title` 的 `m_margin` 左边距 = 原版 **22.7**（TSV 行 462；读口 = `Label.MarginPx`，⛔ 不是 `TMP.margin` 那个世界单位）");
        }
    }

    static void Check(string got, string want, string msg) { MenuCheck.True(_sink, got == want, msg + "（实得 `" + (got ?? "null") + "`）"); }
    static void Check(int got, int want, string msg) { MenuCheck.True(_sink, got == want, msg + "（实得 " + got + "）"); }

    /// <summary>九宫格那三块（按 `localPosition.x` 升序）—— ⛔ 别按子块名取（见 B3 那条注释）。</summary>
    static ImageQuad[] Parts(Transform root)
    {
        if (root == null) return new ImageQuad[0];
        var qs = root.GetComponentsInChildren<ImageQuad>(true);
        var list = new System.Collections.Generic.List<ImageQuad>(qs);
        list.Sort((a, b) => a.transform.localPosition.x.CompareTo(b.transform.localPosition.x));
        return list.ToArray();
    }

    static Label FindLabel(Transform root, string name)
    {
        var t = FindDeep(root, name);
        return t != null ? t.GetComponent<Label>() : null;
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

    static DeckRuntime FindRuntime()
    {
        var all = Resources.FindObjectsOfTypeAll<DeckRuntime>();
        for (int i = 0; i < all.Length; i++)
        {
            var rt = all[i];
            if (rt != null && rt.Root != null) return rt;
        }
        return null;
    }
}
