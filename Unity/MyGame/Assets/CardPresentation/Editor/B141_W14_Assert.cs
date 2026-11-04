// B141_W14_Assert.cs — 执行写手 `B141-W14`（第一百二十八会话 · 波次 `W129` · B138 §4 `4-11` / `4-12` / `4-13`）
//
// ============================ 它盯的是什么 ============================
//  `Label` / `MenuDraw` 这一层「TMP 排版参数」的**公共口 + 真实接线**：
//    · **4-11** `m_paragraphSpacing`（段落间距）—— 原版非 0 约 150 颗，我们改前**连 API 都没有**。
//      本波 = `Label.SetParagraphSpacing`（唯一实现）+ `MenuDraw.SetParagraphSpacing`（Shell 侧转发）。
//    · **4-12** `m_margin` —— 入口早就有（`Label.SetMargin` / `SetMarginPx`），本波只验证它**能表达原版那几档值**
//      （逐站点接线不在本包：判据 `B141_W129_对账_UI动画.md` §D `W9` 行「站点接线不归本包」）。
//    · **4-13** 行距 / 字距 / 字体样式 / Truncate —— 公共口够用，**缺的是站点接线**；
//      本包只接**本文件内真实存在**的两颗：`Deck Name`（`m_overflowMode = 3 Truncate`）
//      与 `Banned Text`（`m_FontStyle = 17 = Bold|UpperCase`）。
//
// ============================ 判据（全部本地现读）============================
//  `d:/2/tools/assetripper_out/ExportedProject/Assets/**/*.prefab`（339 份带 TMP 的，2026-11-04 现扫，
//  逐字段汇总表 → `资料/完全复刻/报告/B141_W129_LabelMenuDraw.md` §一）：
//    · 段距非 0：`8×132`（`DescTextTactic*` / `DescTacticSmall` 卡面族）· **`60×10`**（`DescriptionText`，
//      `Base Event Popup` / `WelcomeScreen Alpha` / `Tutorial Message Window PopUp` …）· `20×4`（toast 正文）·
//      `50×2` · `25×2`（设置 `Email Text`）· `22` · `10` · **`0.08×8`**（`Deck Editing Menu` / `Collection Menu Variant`
//      的 `Counter`）。
//    · 边距非 0：**`(0, 0.0077514946, 0, −0.8828125)`×299**（`Button Text` 全族）·
//      `(4.6, 0, 0, 0)`×42（`Новинка!`）· `(18, 0, 18, 0)`×28 · `(10.365997, 9.329266, 10.365906, 7.25613)`×5 …
//    · `Deck Name`（6 份 prefab）：`m_characterSpacing = −2`（既有）· **`m_overflowMode = 3`（Truncate）**。
//    · `Banned Text`（47 份 prefab，**全同值**）：**`m_FontStyle = 17` = `Bold(1) | UpperCase(16)`** · 段距/行距/字距/边距全 0。
//
// ============================ 🧨 灭自证 ============================
//  · A 段：出厂档逐项 == TMP 出厂值 ⇒ **新口不改默认渲染**（把新口默认写成非 0 ⇒ A 段红）。
//  · B 段：原版逐件真值（8 / 60 / 0.08 / 20 / 边距两族）⇒ 入口**真能表达**那些值（把入口写死成 0 ⇒ 红）。
//  · C 段：**真实现场**——`MenuDraw.DeckCell` 建出来的 `Deck Name` 必须 `Truncate`，
//    同一棵树里的**对照标签**必须仍是 `Overflow`（🧨 把 C2 改成「所有标签都 Truncate」⇒ C4 红）。
//  · D 段：🧨 **灭自证闸** —— 打开 `Label.DisableParagraphSpacingWriteForTest` 后同一调用**读回不变**
//    （证明那一口真的写了 TMP，而不是只记我们自己的字段）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  宿主里加：
//      B141_W14_Assert.Run(); _pass += B141_W14_Assert.Pass; _fail += B141_W14_Assert.Fail;
//  推荐宿主 = `Editor/CollectionScene.cs`（C 段要真卡组库做禁卡夹具；它有 14 套临时测试卡组）。
//  若只想跑 A/B/D 三段，任何一个有 TMP 的宿主都行（那三段不读卡组库）。
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B141_W14`：段距（4-11）/ 边距（4-12）/ 行距·字距·样式·Truncate（4-13）的公共口与真实接线。</summary>
public static class B141_W14_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B141_W14] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }
    static void T(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    public static void Run()
    {
        MenuCheck.Section(_sink, "B141_W14 · 4-11 段距 / 4-12 边距 / 4-13 样式·Truncate");
        var root = new GameObject("B141W14_ROOT", typeof(RectTransform));
        try
        {
            SectionA_Defaults(root.transform);
            SectionB_OriginalValues(root.transform);
            SectionC_RealSites(root.transform);
            SectionD_SelfProof(root.transform);
        }
        finally
        {
            Label.DisableParagraphSpacingWriteForTest = false;   // 🧨 开关绝不许留在打开状态（会静默停掉生产写入）
            Object.DestroyImmediate(root);
        }
    }

    // ============================================================ A · 出厂档 == 现状
    static void SectionA_Defaults(Transform root)
    {
        var lb = MenuDraw.Text(root, new PxRect(0f, 0f, 240f, 60f), "Default tier", Color.white, "W14Default", 30f, MenuDraw.QText);
        T(lb != null, "A0 **前提**：`MenuDraw.Text` 建得出标签（拿不到 TMP 字体 ⇒ 本段无从读）");
        if (lb == null) return;
        T(lb.CanRenderChinese, "A0b **前提**：走的是 **TMP 后端**（点阵后端上这些参数一个都不存在 —— 否则本段是假绿）");
        if (!lb.CanRenderChinese) return;

        N(lb.ParagraphSpacingNow, 0f, 1e-4f,
          "A1 出厂段距 = **0**（TMP `paragraphSpacing` 出厂值）⇒ 不调用新口时与改前**逐位相同**｜🧨 把新口默认写成非 0 ⇒ 本行红");
        N(lb.ParagraphSpacingRequested, 0f, 1e-4f, "A2 「我们请求过什么」= 0（同样是不调即不变）");
        C(lb.OverflowNow, TMPro.TextOverflowModes.Overflow, "A3 出厂溢出档 = Overflow（新口不偷改渲染档）");
        C(lb.FontStyleNow, TMPro.FontStyles.Normal, "A4 出厂字体样式 = Normal");
        C(lb.MarginPx, Vector4.zero, "A5 出厂边距 = (0,0,0,0)");

        MenuDraw.SetParagraphSpacing(lb, 0f);   // 显式传 0 = 「等价于不调用」
        N(lb.ParagraphSpacingNow, 0f, 1e-4f, "A6 `MenuDraw.SetParagraphSpacing(lb, 0)` ⇒ 读回仍是 0（Shell 转发口通）");
    }

    // ============================================================ B · 原版逐件真值
    static void SectionB_OriginalValues(Transform root)
    {
        var lb = MenuDraw.Text(root, new PxRect(260f, 0f, 520f, 60f), "Original tiers", Color.white, "W14Orig", 30f, MenuDraw.QText);
        T(lb != null, "B0 **前提**：第二颗标签建出来了");
        if (lb == null) return;
        T(lb.CanRenderChinese, "B0b **前提**：TMP 后端（点阵后端没有段距/边距这回事）");
        if (!lb.CanRenderChinese) return;

        MenuDraw.SetParagraphSpacing(lb, 8f);   // 走 Shell 转发口
        N(lb.ParagraphSpacingNow, 8f, 1e-4f,
          "B1 ★ 段距 **8**（卡面战术卡 `DescTextTactic*` / `DescTacticSmall` 族，132 颗）｜🧨 入口写死 0 ⇒ 红");

        lb.SetParagraphSpacing(60f);           // 走 Label 直口
        N(lb.ParagraphSpacingNow, 60f, 1e-4f,
          "B2 ★ 段距 **60**（事件/欢迎弹窗 `DescriptionText`，10 颗：`Base Event Popup` / `WelcomeScreen Alpha` …）");

        lb.SetParagraphSpacing(0.08f);
        N(lb.ParagraphSpacingNow, 0.08f, 1e-4f, "B3 ★ 段距 **0.08**（`Deck Editing Menu` / `Collection Menu Variant` 的万能卡 `Counter`，8 颗）");

        lb.SetParagraphSpacing(20f);
        N(lb.ParagraphSpacingNow, 20f, 1e-4f, "B4 ★ 段距 **20**（toast 正文 `Toast.prefab` / `Mission Toast Notification`，4 颗）");

        lb.SetMarginPx(4.6f, 0f, 0f, 0f);
        N(lb.MarginPx.x, 4.6f, 1e-3f, "B5 ★ 边距左 **4.6**（`Новинка!` 那颗 `Text`，42 颗）");
        N(lb.MarginPx.y, 0f, 1e-3f, "B6 边距上 = 0（同那一族）");

        lb.SetMarginPx(0f, 0.0077514946f, 0f, -0.8828125f);
        N(lb.MarginPx.y, 0.0077514946f, 1e-4f, "B7 ★ 边距上 **0.00775**（`Button Text` 全族 299 颗 —— 全库最大一族）");
        N(lb.MarginPx.w, -0.8828125f, 1e-4f, "B8 ★ 边距下 **−0.8828**（同那一族）");
        N(lb.MarginPx.x, 0f, 1e-4f, "B9 边距左 = 0（同那一族）");

        // 读回的真值来自 TMP 而不是我们的字段：`SetMargin` 同时写 `_tmp.margin`
        var readBack = lb.MarginPx;
        lb.SetMarginPx(0f, 0f, 0f, 0f);
        C(lb.MarginPx, Vector4.zero, "B10 再写一次 (0,0,0,0) ⇒ 读回全 0（不是「只第一次生效」）");
        N(readBack.w, -0.8828125f, 1e-4f, "B11 上一次读到的值留在本地变量里（B7..B9 不是被 B10 覆盖出来的假值）");
    }

    // ============================================================ C · 真实现场
    static void SectionC_RealSites(Transform root)
    {
        var info = new CollectionData.DeckInfo
        {
            Name = "B141W14 Deck",
            WarlordId = "B141W14Warlord",
            Faction = "",
            Count = 0,
            CardbackId = "",
            GameMode = 0,
        };
        var cell = MenuDraw.DeckCell(root, "W14Cell", new PxRect(0f, 0f, 225f, 364.5f), info, false,
                                     3000, 3005, MenuDraw.QText, 3015, 3020, null);
        T(cell != null, "C0 **前提**：`DeckCell` 建出来了（拿不到 TMP 字体 / 图缺到没法建格 ⇒ 后面读不到）");
        if (cell == null) return;

        var nameT = cell.Find("Deck Name");
        T(nameT != null, "C1 **前提**：格子里有 `Deck Name` 节点");
        var nameLb = nameT != null ? nameT.GetComponent<Label>() : null;
        T(nameLb != null, "C2 **前提**：`Deck Name` 上有 `Label`");
        if (nameLb != null)
            T(nameLb.CanRenderChinese, "C2b **前提**：`Deck Name` 走 TMP 后端（否则 `OverflowNow` 恒报出厂档 = 假绿）");
        if (nameLb != null && nameLb.CanRenderChinese)
        {
            C(nameLb.OverflowNow, TMPro.TextOverflowModes.Truncate,
              "C3 ★ `Deck Name` 溢出档 = **Truncate(3)**（原版 6 份 prefab 逐份同值）"
              + "｜🧨 删掉 `MenuDraw.DeckCell` 里那句 `SetOverflow(...)` ⇒ 本行红");
            N(nameLb.CharSpacing, -2f, 1e-4f, "C4 `Deck Name` 字距 = −2（既有接线，回归）");
        }

        // 对照：同一棵树里另一颗标签必须仍是出厂档 —— 证明 C3 是「接线效果」，不是「全局默认被改」
        var ctl = MenuDraw.Text(root, new PxRect(240f, 0f, 460f, 60f), "Control", Color.white, "W14Control", 30f, MenuDraw.QText);
        T(ctl != null, "C5 **前提**：对照标签建出来了");
        if (ctl != null)
            T(ctl.CanRenderChinese, "C5b **前提**：对照标签走 TMP 后端（点阵后端 `OverflowNow` 恒报 Overflow = 假绿）");
        if (ctl != null && ctl.CanRenderChinese)
            C(ctl.OverflowNow, TMPro.TextOverflowModes.Overflow,
              "C6 对照标签仍是 **Overflow** ⇒ C3 那一档只落在 `Deck Name` 上（🧨 全局默认改成 Truncate ⇒ 本行红）");

        // ---- `Banned Text` 的 `m_FontStyle = 17`：生产路休眠（禁卡表在远端）⇒ 与 `B121_C3` 同一套注入夹具。
        int n = CollectionData.DeckCount();
        T(n > 0, "C7 **前提**：卡组库至少一套（本段要真卡组做禁卡夹具；`CollectionScene` 用临时存档，应有 14 套）");
        if (n <= 0) return;
        var realInfo = CollectionData.DeckAt(0);
        var deck = CollectionData.Raw(0);
        T(deck != null, "C8 **前提**：第 0 套卡组读得到（`CollectionData.Raw(0)`）");
        if (deck == null) return;
        T(!string.IsNullOrEmpty(deck.WarlordId), "C9 **前提**：第 0 套有督军（禁卡谓词第一处就是它）｜实际 `" + deck.WarlordId + "`");
        if (string.IsNullOrEmpty(deck.WarlordId)) return;

        var oldEvt = DeckRuntime.CurrentEventData;
        try
        {
            DeckRuntime.CurrentEventData = Event(deck.WarlordId);
            var bannedCell = MenuDraw.DeckCell(root, "W14Banned", new PxRect(0f, 0f, 225f, 364.5f), realInfo, false,
                                               3000, 3005, MenuDraw.QText, 3015, 3020, null, deckIndex: 0);
            var icon = bannedCell != null ? bannedCell.Find("Ban Icon") : null;
            T(icon != null, "C10 **前提**：禁卡档下 `Ban Icon` 建出来了（Ban 图缺 ⇒ 下面读不到 `Banned Text`）");
            if (icon == null) return;
            var bt = icon.Find("Banned Text");
            var bl = bt != null ? bt.GetComponent<Label>() : null;
            T(bl != null, "C11 **前提**：`Banned Text` 上有 `Label`");
            if (bl != null)
                T(bl.CanRenderChinese, "C11b **前提**：`Banned Text` 走 TMP 后端（否则 `FontStyleNow` 恒报 Normal = 假绿）");
            if (bl != null && bl.CanRenderChinese)
                C(bl.FontStyleNow, TMPro.FontStyles.Bold | TMPro.FontStyles.UpperCase,
                  "C12 ★ `Banned Text` 字体样式 = **Bold|UpperCase = 17**（47 份 prefab 全同值）"
                  + "｜🧨 删掉 `MenuDraw.DeckBan.Apply` 里那句 `SetFontStyle(...)` ⇒ 本行红");
        }
        finally
        {
            DeckRuntime.CurrentEventData = oldEvt;   // 生产路本来就是 null；⛔ 绝不留下注入态
        }
    }

    // ============================================================ D · 灭自证
    static void SectionD_SelfProof(Transform root)
    {
        var lb = MenuDraw.Text(root, new PxRect(480f, 0f, 720f, 60f), "Self proof", Color.white, "W14SelfProof", 30f, MenuDraw.QText);
        T(lb != null, "D0 **前提**：灭自证那颗标签建出来了");
        if (lb == null) return;
        T(lb.CanRenderChinese, "D0b **前提**：TMP 后端（点阵后端上 `SetParagraphSpacing` 压根不写任何东西）");
        if (!lb.CanRenderChinese) return;

        lb.SetParagraphSpacing(60f);
        N(lb.ParagraphSpacingNow, 60f, 1e-4f, "D1 基线：写 60 ⇒ TMP 读回 60");

        Label.DisableParagraphSpacingWriteForTest = true;
        lb.SetParagraphSpacing(8f);
        N(lb.ParagraphSpacingNow, 60f, 1e-4f,
          "D2 🧨 **灭自证**：关掉 TMP 写入后同一调用**读回仍是 60** ⇒ 证明这一口真的在写 TMP，不是只记我们自己的字段");
        N(lb.ParagraphSpacingRequested, 8f, 1e-4f, "D3 同一拍的「请求记录」已变成 8（两边的缝正是 D2 要证明的那一条）");

        Label.DisableParagraphSpacingWriteForTest = false;
        lb.SetParagraphSpacing(8f);
        N(lb.ParagraphSpacingNow, 8f, 1e-4f, "D4 开关复位 ⇒ 同一调用落地（8）");
    }

    // ============================================================ 禁卡夹具（与 `B121_C3_Assert` 同一形状）
    static PlayEventData Event(string bannedId)
    {
        var evt = new TestEvent();
        evt.SetBannedCards(string.IsNullOrEmpty(bannedId) ? null : new List<string> { bannedId });
        return evt;
    }

    /// <summary>禁卡表夹具（`PlayEventData` 是 abstract；`SetBannedCards` 是 Shell 侧的扩展）。</summary>
    sealed class TestEvent : PlayEventData { }
}
