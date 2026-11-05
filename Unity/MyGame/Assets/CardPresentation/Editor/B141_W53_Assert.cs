// B141_W53_Assert.cs — 执行写手 `B141-W53`（2026-11-04 · 第一百二十八会话 · 波次 `W129`）
//   = `B138 §4` 的 **4-01（字体面）** 里**属于 `Core/TmpFont.cs` 的那一半**（判据包 `W23`）。
//
// ============================ 它盯的是什么 ============================
//  原版 1314 份 prefab 里**主字体不是 Noto**：Pragati **4,431** 颗 · Asar **548** 颗 · Noto **0** · Roboto **0**
//  （2026-11-03 `B131_A6`）。而我们的**缺省面仍是 `Face.Cjk`**（Noto，行盒 **1.2933 em** vs 原版 Pragati
//  **0.9474 em**）⇒ 同框自适应收敛值系统性偏小（已知症状：卡组左栏费用数字原版停在 50、我们被压到 3.05）。
//
//  `Core/TmpFont.cs` 本次新增的**唯一东西** = 一张**「站点 → 面」表**（`TmpFont.SiteFaces` / `FaceForSite` /
//  `TryFaceForSite`）。**它不改任何缺省值、不改渲染逻辑**；每个值都是**原版 prefab 的 `m_fontAsset` 实读**。
//
// ============================ 本文件断什么（六组）============================
//  A 表形：8 行 · 站点键唯一 · 每行都有「原版资产名 + 判据出处」。
//  B 绑定：**每一行的 `Face` 解析出来的资产，名字必须就是他记的那份原版资产**
//          （`LatinCardBody → Pragati-Regular SDF` · `LatinUi → Asar-Regular SDF` · `LatinNarrow → RobotoCondensed-Regular SDF`）
//          ⇒ 换枚举序 / 换 `FaceResource` 绑定 / 抄错行 ⇒ 红。
//  C 缺省与兜底没被动过：**`Label` 出厂面仍是 `Cjk`**（本表不改缺省值）· 未知站点回 `Cjk` ·
//          **文本里有汉字 ⇒ `ActualFace` 回 `Cjk`**（三份拉丁资产没有汉字字形，拿它们渲汉字会出一排 `.notdef` 方框）。
//  D 灭自证（与 A/B 不同源）：表里**必须同时存在**两种面（Asar 与 Pragati），且**同一 prefab 内**的两族
//          （`card.name` vs `card.body`）**必须不同面** ⇒ 「一把全改成同一个面」那种假绿过不了。
//  E 卡组窗（`Run(DeckRuntime)`）：6 颗**必建**站点按名钉 `LatinCardBody` + 整窗计数（≥12 且占多数）。
//  F 收藏窗（`Run(CollectionWindow)`）：`Title Header` 按名钉 + 整窗计数（≥6 且占多数）。
//
// ============================ 接线（两窗）= 【2026-11-04 已落地】============================
//  · `Deck/DeckRuntime.cs`：`Txt` / `TxtPx` 加 `TmpFont.Face face = TmpFont.Face.Cjk`（缺省已于 2026-11-05 `B148-W6` 改为 Pragati），
//    **27 个调用点**逐一传 `face: TmpFont.Face.LatinCardBody`（本窗原版 `Deck Editing Menu.prefab` **64/64 = Pragati**）。
//  · `Shell/CollectionWindow.cs`：**19 个 `Text(` 站点**传同一面（原版 `Collection Menu Variant.prefab` **57/57 = Pragati**），
//    另 2 个站点（`Title Header` / `Event Title`）**本来就传了**同一面。
//  · ⚠️ **2026-11-05 订正（B148-W3/W6）**：缺省面已改 —— `MenuDraw.Text` / `TmpFont.NewText` = Pragati，`Label` 出厂仍是 `Cjk`；汉字仍由 `Label.ActualFace` 回 CJK。
//
// ============================ 怎么接线（宿主）============================
//  宿主在白名单外（`Editor/DeckScene.cs` / `Editor/CollectionScene.cs`），本批没接。宿主里加两行即可：
//      DeckScene：        B141_W53_Assert.Run(_rt); pass += B141_W53_Assert.Pass; fail += B141_W53_Assert.Fail;
//      CollectionScene：  B141_W53_Assert.Run(win); pass += B141_W53_Assert.Pass; fail += B141_W53_Assert.Fail;
//  只要表那一段（不建窗）：`B141_W53_Assert.Run();`
//  ⚠️ 它**不改场景**，只读常量 + 建一颗临时 `GameObject` 量 `Label`（读完立刻 `DestroyImmediate`）。
using CardPresentation;
using UnityEngine;

/// <summary>`B141-W53`：4-01 的「站点 → 面」表（`Core/TmpFont.cs` 的 `SiteFaces` / `FaceForSite`）。</summary>
public static class B141_W53_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B141_W53] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }
    /// <summary>失败明细（宿主合并进自己的 `Failures` 用）。</summary>
    public static readonly System.Collections.Generic.List<string> Failures = _sink.Failures;

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void T(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    public static void Run()
    {
        MenuCheck.Section(_sink, "B141_W53 · 4-01「站点 → 面」表（原版 `m_fontAsset` 实读，非猜）");
        SectionA_TableShape();
        SectionB_AssetBinding();
        SectionC_DefaultsAndCjkFallback();
        SectionD_AntiSelfProof();
        SectionG_SharedSites();          // 🆕 2026-11-04（`B141-W64`）：两处共享件的残留站点
    }

    // ============================================================ E/F · 两窗的站点面（原版整窗都是 Pragati）
    /// <summary>卡组编辑窗：原版 `Deck Editing Menu.prefab` **64/64 颗 TMP = Pragati**（含页头 `label`、`Done`/`Back`
    /// 按钮字、四颗通配符计数、全部筛选标题与 `Card Name`/`Count`/`Card Cost`/`Cards in deck`）。</summary>
    public static void Run(CardPresentation.DeckRuntime rt)
    {
        MenuCheck.Section(_sink, "B141_W53 · 卡组编辑窗（`Deck Editing Menu` 64/64 = Pragati）");
        T(rt != null, "E0（前提）`DeckRuntime` 拿得到");
        if (rt == null) return;
        SiteFace(rt.transform, "hdr_back_t", "E1", "页头 `Back` 按钮字");
        SiteFace(rt.transform, "hdr_wct0", "E2", "通配符计数（`WIldcard Counter/Counters/*/Counter`）");
        SiteFace(rt.transform, "tab_tx0", "E3", "侧栏页签（`Window Options/*/Label/Text`）");
        SiteFace(rt.transform, "name_t", "E4", "卡组名输入框（`Deck Name/Text Area/Text`）");
        SiteFace(rt.transform, "foot_done_t", "E5", "页脚 `Done` 按钮字");
        SiteFace(rt.transform, "foot_cnt", "E6", "页脚计数（`Sidebar/Footer/Counter`）");
        SweepOwnLabels(rt.transform, "E7", "Deck Editing Menu", "Deck Name", 12);
    }

    /// <summary>收藏窗：原版 `Collection Menu Variant.prefab` **57/57 颗 TMP = Pragati**（含 4 页签 `TabButtonLabel`、
    /// 页头 `label`、`Clear Filter Button`、`Create`/`Import`/`Unlock`、四颗通配符、`Select Game Mode Deck Tab/Title Header`）。</summary>
    public static void Run(CardPresentation.CollectionWindow win)
    {
        MenuCheck.Section(_sink, "B141_W53 · 收藏窗（`Collection Menu Variant` 57/57 = Pragati）");
        T(win != null, "F0（前提）`CollectionWindow` 拿得到");
        if (win == null) return;
        SiteFace(win.transform, "Title Header", "F1", "`Select Game Mode Deck Tab/Title Header`");
        SweepOwnLabels(win.transform, "F2", "Collection Menu Variant", "Deck Name", 6);
        SectionF_TabButtonLabels(win.transform);
    }

    /// <summary>按名字找一颗 `Label` 断它的面。⚠️ **名字找不到 = 红**（前提断言），⛔ 不写成 `if (x != null) Check(...)`。</summary>
    static void SiteFace(Transform root, string nodeName, string tag, string what)
    {
        var t = MenuCheck.FindChild(root, nodeName);
        var lb = t != null ? t.GetComponent<Label>() : null;
        T(lb != null, $"{tag}（前提）`{nodeName}`（{what}）建出来了 —— 本窗原版那颗 TMP 是 `Pragati-Regular SDF`");
        if (lb == null) return;
        C(lb.Face, TmpFont.Face.LatinCardBody,
          $"{tag} ★ `{nodeName}` 指派面 = `LatinCardBody`（Pragati）｜🧨 把该调用点的 `face:` 去掉 ⇒ 红");
    }

    /// <summary>整窗按**计数**断（不逐名钉 —— 窗里还有**别的写手**建的件，见报告 §五）：本窗自己的 `Label` 里
    /// `LatinCardBody` 的**条数必须够**（= 那 27 / 19 个站点的接线真生效了），且**占多数**（防「全被改回 Cjk」）。
    /// 排除两类（⛔ 不是本窗的活）：① `CardView` 子树（卡面子树自己管面）；
    /// ② `MenuDraw.DeckCell` 建的 `"Deck Name"`（共享件，归 `Shell/MenuDraw.cs`）。
    /// ⚠️ **不用「其它一律为 0」那种严格式**：窗里还有共享件建的件（`MenuDraw` 的壳件等）⇒ 严格式会把**别人的缺口**算到本件头上。
    /// 🔴 **2026-11-04（`B141-W64`）就地订正**：原来这里写「左栏 `Tab Buttons/*/TabButtonLabel` 由
    /// `Shell/MainMenuSubmenuWindow.cs:777` 建、本批白名单外」—— 那两处**已经做完了**（`MainMenuSubmenuWindow.cs:780`
    /// 传 `face: LatinCardBody`；`MenuDraw.DeckCell` 的 `Deck Name` 传 `face: LatinUi`），
    /// 它们现在分别由 <see cref="SectionF_TabButtonLabels"/> 与 <see cref="SectionG_SharedSites"/> **逐颗钉死**。
    /// 本函数**仍保留**两类排除：计数式的口径不变（收紧会把它变成「别人的件也要我负责」）。</summary>
    static void SweepOwnLabels(Transform root, string tag, string windowName, string sharedNodeName,
                               int minLatin)
    {
        var all = root.GetComponentsInChildren<Label>(true);
        int own = 0, latin = 0, other = 0;
        string otherList = "";
        for (int i = 0; i < all.Length; i++)
        {
            var lb = all[i];
            if (lb == null) continue;
            if (lb.GetComponentInParent<CardView>() != null) continue;      // ① 卡面子树
            if (lb.name == sharedNodeName) continue;                        // ② 共享件（MenuDraw.DeckCell）
            own++;
            if (lb.Face == TmpFont.Face.LatinCardBody) { latin++; continue; }
            other++;
            if (otherList.Length < 400) otherList += $"{lb.name}={lb.Face} ";
        }
        T(own >= minLatin, $"{tag}（前提）本窗自己扫到 **{own}** 颗 `Label`（原版 `{windowName}` 整窗 TMP 全是 Pragati）");
        T(latin >= minLatin,
          $"{tag} ★ 本窗 **{latin}/{own}** 颗 `Label` 的字体面 = `LatinCardBody`（Pragati）"
        + $"，下限 **{minLatin}**｜🧨 把这些站点的 `face:` 去掉 ⇒ 只剩少数几颗 ⇒ 本行红");
        T(latin > other,
          $"{tag} ★ 拉丁面**占多数**（{latin} > {other}）｜🧨 整片改回 `Cjk` ⇒ 红"
        + (other > 0 ? $"｜⛔ 仍是别的面的（**只登记不断言**，多为共享件建的件）：{otherList}" : ""));
    }

    // ============================================================ A · 表形
    static void SectionA_TableShape()
    {
        var t = TmpFont.SiteFaces;
        T(t != null, "A0（前提）`TmpFont.SiteFaces` 拿得到");
        if (t == null) return;

        C(t.Length, 8, "A1 表长 = **8** 行（menu.body · window.title · section.title · card.name · card.body · "
                     + "battle.hud.number · battle.hud.title · narrow）");

        int dup = 0, badAsset = 0, badEvidence = 0, badFace = 0;
        var seen = new System.Collections.Generic.HashSet<string>();
        for (int i = 0; i < t.Length; i++)
        {
            if (string.IsNullOrEmpty(t[i].Site) || !seen.Add(t[i].Site)) dup++;
            if (string.IsNullOrEmpty(t[i].Original)) badAsset++;
            if (string.IsNullOrEmpty(t[i].Evidence)) badEvidence++;
            if ((int)t[i].Face < 0 || (int)t[i].Face > (int)TmpFont.Face.LatinNarrow) badFace++;
        }
        C(dup, 0, "A2 站点键**唯一**且非空（⛔ 同一用途不要开两个键）");
        C(badAsset, 0, "A3 每行都写了**原版资产名**（`m_fontAsset` 实读，⛔ 不是按名字猜的）");
        C(badEvidence, 0, "A4 每行都写了**判据出处 / 计数**（下一个人能复查）");
        C(badFace, 0, "A5 每行的 `Face` 都落在枚举里");
    }

    // ============================================================ B · 绑定（面 → 原版那份资产）
    static void SectionB_AssetBinding()
    {
        var t = TmpFont.SiteFaces;
        if (t == null) return;

        int bad = 0; string detail = "";
        for (int i = 0; i < t.Length; i++)
        {
            string want = BaseName(t[i].Original);          // "Pragati" / "Asar" / "RobotoCondensed"
            var a = TmpFont.FaceAsset(t[i].Face);
            string got = a != null ? a.name : "<null>";
            bool ok = a != null && got.Contains(want);
            if (!ok)
            {
                bad++;
                if (detail.Length < 500) detail += $"{t[i].Site}:{t[i].Face}→{got}(应含 {want}) ";
            }
        }
        C(bad, 0, "B1 ★ 每行「面 → 资产」都对得上**它记的那份原版资产**"
                + "（`LatinCardBody=Pragati-Regular SDF` · `LatinUi=Asar-Regular SDF` · `LatinNarrow=RobotoCondensed-Regular SDF`）"
                + "｜🧨 换枚举序 / 换 `FaceResource` 绑定 / 抄错行 ⇒ 本行红"
                + (bad > 0 ? $"｜不对的：{detail}" : ""));

        // 三份拉丁资产各自取得到（缺一份 ⇒ 前面 B1 会以「退回 CJK 那份」的形态红，这里再单钉一次，便于分诊）
        var pragati = TmpFont.FaceAsset(TmpFont.Face.LatinCardBody);
        var asar = TmpFont.FaceAsset(TmpFont.Face.LatinUi);
        var roboto = TmpFont.FaceAsset(TmpFont.Face.LatinNarrow);
        T(pragati != null && pragati.name.Contains("Pragati"),
          "B2（前提）`Resources/Fonts/Pragati-Regular SDF` 取得到｜实得「" + (pragati != null ? pragati.name : "<null>") + "」");
        T(asar != null && asar.name.Contains("Asar"),
          "B3（前提）`Resources/Fonts/Asar-Regular SDF` 取得到｜实得「" + (asar != null ? asar.name : "<null>") + "」");
        T(roboto != null && roboto.name.Contains("RobotoCondensed"),
          "B4（前提）`Resources/Fonts/RobotoCondensed-Regular SDF` 取得到｜实得「" + (roboto != null ? roboto.name : "<null>") + "」");
    }

    /// <summary>`"Pragati-Regular SDF"` → `"Pragati"`（TMP 资产的 `name` 就是文件名，不含 `-Regular SDF`）。</summary>
    static string BaseName(string original)
    {
        if (string.IsNullOrEmpty(original)) return "";
        int i = original.IndexOf('-');
        return i > 0 ? original.Substring(0, i) : original;
    }

    // ============================================================ C · 缺省面与汉字兜底（⛔ 本表不许动这两条）
    static void SectionC_DefaultsAndCjkFallback()
    {
        // C1/C2：未知站点 = 今天的缺省（表不改缺省值，⛔ 别把未知键也塞成拉丁面）
        TmpFont.Face f;
        C(TmpFont.FaceForSite("no.such.site"), TmpFont.Face.Cjk,
          "C1 未知站点 → `Face.Cjk`（= 今天的缺省面；本表**不改缺省值**）");
        T(!TmpFont.TryFaceForSite("no.such.site", out f),
          "C2 未知站点 `TryFaceForSite` 返回 **false**（⛔ 不猜一个面给它）");
        C(TmpFont.FaceForSite(null), TmpFont.Face.Cjk, "C2b `null` 站点 → `Face.Cjk`（空/脏实参不发散）");

        // C3/C4：`Label` 出厂面仍是 Cjk（缺省没被本表改掉）
        var root = new GameObject("B141_W53_Probe");
        try
        {
            var lb = Label.Create(root.transform, "Deck", new Vector3(0f, 0f, 0f), 5, Color.white,
                                  new Vector2(0.5f, 0.5f), "probe");
            T(lb != null, "C3（前提）`Label.Create` 建得出来（字体资产在）");
            if (lb == null) return;

            C(lb.Face, TmpFont.Face.Cjk, "C4 ★ `Label` 出厂面 = `Face.Cjk`（本表⛔ 不改漏斗缺省值）");

            // C5：指派拉丁面、文本里有汉字 ⇒ 实际生效面回 CJK
            lb.SetFace(TmpFont.Face.LatinCardBody);
            lb.SetText("卡组 Deck");
            C(lb.Face, TmpFont.Face.LatinCardBody, "C5（前提）指派的面记下来了（`Label.Face`）");
            C(lb.ActualFace, TmpFont.Face.Cjk,
              "C6 ★ **文本里有汉字 ⇒ `ActualFace` 回 `Cjk`**（三份拉丁资产没有汉字字形，拿它们渲汉字会静默出一排 `.notdef`）");

            // C7：纯拉丁文本 ⇒ 实际生效面就是指派的那一面（证明 C6 不是「恒 CJK」那种假绿）
            lb.SetText("Deck 1");
            C(lb.ActualFace, TmpFont.Face.LatinCardBody,
              "C7 ★ 纯拉丁文本 ⇒ `ActualFace` = 指派面（这条证明 C6 **不是**「`ActualFace` 恒 CJK」）");

            Kill(lb.gameObject);
        }
        finally
        {
            Kill(root);          // 批处理下 `Destroy` 不生效，走 `DestroyImmediate`（见 CLAUDE.md）
        }
    }

    /// <summary>批处理下没有帧循环 ⇒ `Destroy` 不生效。⚠️ 编辑器程序集拿不到 `TmpFont.Kill`（那是 `internal`）。</summary>
    static void Kill(Object o)
    {
        if (o == null) return;
        Object.DestroyImmediate(o);
    }

    // ============================================================ D · 灭自证（与 A/B 不同源）
    static void SectionD_AntiSelfProof()
    {
        var body = TmpFont.FaceForSite(TmpFont.SiteMenuBody);
        var title = TmpFont.FaceForSite(TmpFont.SiteWindowTitle);
        var name = TmpFont.FaceForSite(TmpFont.SiteCardName);
        var cardBody = TmpFont.FaceForSite(TmpFont.SiteCardBody);
        var hudTitle = TmpFont.FaceForSite(TmpFont.SiteBattleHudTitle);
        var narrow = TmpFont.FaceForSite(TmpFont.SiteNarrow);

        C(body, TmpFont.Face.LatinCardBody,
          "D1 ★ 菜单/弹窗正文 = `LatinCardBody`（Deck 64/64 · Collection 57/57 · Settings 102/102 · Tooltip · Toast = Pragati）"
        + "｜🧨 改成 `LatinUi`/`Cjk` ⇒ 红");
        C(title, TmpFont.Face.LatinUi,
          "D2 ★ 窗口标题 `Window Title` = `LatinUi`（`Daily Streak Popup` / `EnergySinglePlayerOnlyEventWindow` 实读 = Asar）");
        T(name != cardBody,
          "D3 ★ **同一颗卡上「卡名」与「正文」不是同一个面**（原版 `NameText*` = Asar、`DescText*`/`UnitType*` = Pragati）"
        + "｜🧨 把整表刷成同一个面 ⇒ 本行红");
        T(body != title,
          "D4 ★ 正文与窗口标题**不是一个面**｜🧨 「全表恒 `LatinCardBody`」那种假绿过不了本行");
        C(hudTitle, TmpFont.Face.LatinUi, "D5 战斗 HUD 标题族（`OVERTIME!`/`END TURN`/称号）= Asar");
        C(narrow, TmpFont.Face.LatinNarrow, "D6 `narrow` 键 → `LatinNarrow`（原版 1314 份 prefab 里 **0 颗**，键保留只为「查得到、别猜」）");

        // D7：整表里至少各有 1 行 Asar / Pragati —— 结构性约束，防「删掉一半行」把 D3/D4 变成空转
        int asarRows = 0, pragatiRows = 0;
        for (int i = 0; i < TmpFont.SiteFaces.Length; i++)
        {
            if (TmpFont.SiteFaces[i].Face == TmpFont.Face.LatinUi) asarRows++;
            else if (TmpFont.SiteFaces[i].Face == TmpFont.Face.LatinCardBody) pragatiRows++;
        }
        T(asarRows >= 3 && pragatiRows >= 3,
          $"D7 ★ 表里两种面各 ≥3 行（现读 Asar **{asarRows}** · Pragati **{pragatiRows}**）"
        + " ⇒ D3/D4 不会因为「行被删光」而空转");
    }

    // ============================================================ F3..F5 · 左栏页签（共享件 `MainMenuSubmenuWindow.BuildTabButton`）
    /// <summary>🆕 2026-11-04（`B141-W64`）：左栏四颗页签的文案由 **`Shell/MainMenuSubmenuWindow.cs` 的 `BuildTabButton`** 一处建出，
    /// 四个窗（奖励 / 商店 / 社交 / 收藏）共用 ⇒ **一处修四窗**。
    /// <para>判据（2026-11-04 现读 `bundle_menus_assets_all`，两源一致）：
    /// 四个窗的 `Content Area/Tab Buttons/*/Label/TabButtonLabel` 的 `m_fontAsset` 全是 **`Pragati-Regular SDF`**
    /// （pathID `3485036404935369831` = `资源/R0_资源覆盖账.tsv:83554`；AssetRipper `Collection Menu Variant.prefab` guid `2548b731…` ⇒
    /// `Pragati-Regular SDF.asset.meta:2`）。`Rewards Base Submenu Variant` 4 颗 · `Collection Menu Variant` 4 颗 ·
    /// `Social Submenu Variant` 2 颗 · `Shop Menu Variant` 1 颗（母版，其余运行期克隆）**逐颗同值**。</para>
    /// <para>⚠️ 我们这边的节点名不是 `TabButtonLabel`：`BuildTabButton` 把文案建成按钮下的 `"Text"`（`Text(..., "Text", ...)`）。
    /// 所以按「名叫 `Text` + 祖先里有 `Tab Buttons`」定位，而不是按原版节点名。</para></summary>
    static void SectionF_TabButtonLabels(Transform root)
    {
        var all = root.GetComponentsInChildren<Label>(true);
        int n = 0, prag = 0, bad = 0, actualBad = 0, cjkSeen = 0, latinSeen = 0;
        string detail = "";
        for (int i = 0; i < all.Length; i++)
        {
            var lb = all[i];
            if (lb == null || lb.name != "Text") continue;
            if (!UnderNamed(lb.transform.parent, "Tab Buttons")) continue;
            n++;
            if (lb.Face == TmpFont.Face.LatinCardBody) prag++;
            else { bad++; if (detail.Length < 300) detail += $"{lb.transform.parent.name}={lb.Face} "; }
            bool hasCjk = Loc.HasCjk(lb.Text);
            var want = (hasCjk && !lb.CjkFallbackFace) ? TmpFont.Face.Cjk : lb.Face;
            if (lb.ActualFace != want)
            {
                actualBad++;
                if (detail.Length < 400) detail += $"[实际]{lb.transform.name}:{lb.ActualFace}≠{want} ";
            }
            if (hasCjk) cjkSeen++; else latinSeen++;
        }
        T(n >= 4, $"F3（前提）左栏页签文案扫到 **{n}** 颗（`Collection Menu Variant` 原版是 4 颗 `TabButtonLabel`）");
        T(bad == 0, $"F4 ★ 四颗页签的**指派面**全是 `LatinCardBody`（Pragati）—— 现读 {prag}/{n}"
                  + "｜🧨 删掉 `MainMenuSubmenuWindow.cs:780` 的 `face: TmpFont.Face.LatinCardBody` ⇒ 本行红"
                  + (bad > 0 ? $"｜不是的：{detail}" : ""));
        T(actualBad == 0, $"F5 ★ **实际生效面**按文本语种走：有汉字 ⇒ `Cjk`（`Pragati` 没有汉字字形）· 纯拉丁 ⇒ 指派面"
                       + $"｜本窗汉字 {cjkSeen} 颗 · 纯拉丁 {latinSeen} 颗"
                       + (actualBad > 0 ? $"｜🧨 兜底被拆掉 / 写死成一面：{detail}" : ""));
    }

    // ============================================================ G · 两处共享件（`B141-W64`：`DeckCell` 的 `Deck Name`）
    /// <summary>🆕 2026-11-04（`B141-W64`）：`MenuDraw.DeckCell` 建的 **`Deck Name`**（卡组格的名字，卡组列表共用）。
    /// <para>🔴 **判据 = `Asar-Regular SDF`（`Face.LatinUi`），不是 Pragati** —— 2026-11-04 两源现读一致：
    /// ① 原始包 `bundle_menus_assets_all` 的 `Collection Deck/content/Deck/Deck Name`（`m_text = 'Deck name'`）
    /// `m_fontAsset` pathID `-8244042478085975641` = `资源/R0_资源覆盖账.tsv:83490` → **`Asar-Regular SDF`**
    /// （同一行也见 `Deck Drawer/Content/Collection Deck/…` 与 `Ranked Deck Selection/Deck Name`，全同值）；
    /// ② AssetRipper `Collection Deck.prefab` 的 `Deck Name` guid `b80ad157b2e63114e94d0e228946d481` = `Asar-Regular SDF.asset.meta:2`。
    /// ⚠️ **易混点**：`Deck Editing Menu/…/Deck Name/Text Area/Text`（卡组编辑侧栏那个**输入框**）才是 Pragati
    /// —— 那不是本件；`W129` 的施工单原写「原版 Pragati」是把这两处同名节点并成一处，已按本条订正。</para></summary>
    static void SectionG_SharedSites()
    {
        MenuCheck.Section(_sink, "B141_W64 · 4-01 残留：`MenuDraw.DeckCell` 的 `Deck Name`（原版 = Asar）");
        T(TmpFont.Face.LatinCardBody != TmpFont.Face.LatinUi,
          "G0 ★ 前提：`LatinCardBody` 与 `LatinUi` 是两个不同的枚举值（否则 G3 无法区分「填对了」与「都填成一个面」）");

        var root = new GameObject("B141_W64_Probe");
        try
        {
            var info = new CollectionData.DeckInfo
            {
                Name = "W64 Deck 1",
                WarlordId = "",
                Faction = "",
                Count = 0,
                CardbackId = "",
                GameMode = 0,
            };
            var cell = MenuDraw.DeckCell(root.transform, "W64Cell", new PxRect(0f, 0f, 225f, 364.5f), info, false,
                                         3000, 3005, MenuDraw.QText, 3015, 3020, null);
            T(cell != null, "G1（前提）`MenuDraw.DeckCell` 建出来了");
            if (cell == null) return;

            var nameT = cell.Find("Deck Name");
            var lb = nameT != null ? nameT.GetComponent<Label>() : null;
            T(lb != null, "G2（前提）格子上有 `Deck Name` 的 `Label`");
            if (lb == null) return;

            C(lb.Face, TmpFont.Face.LatinUi,
              "G3 ★ `Deck Name` 指派面 = `LatinUi`（原版 `Collection Deck/content/Deck/Deck Name` = `Asar-Regular SDF`）"
            + "｜🧨 改回 `Cjk`、或照施工单误填 `LatinCardBody` ⇒ 本行红");

            C(lb.ActualFace, TmpFont.Face.LatinUi,
              "G4 ★ 纯拉丁卡组名 ⇒ `ActualFace` = `LatinUi`（证明 G5 不是「`ActualFace` 恒 `Cjk`」）");

            lb.SetText("卡组 1");
            C(lb.Face, TmpFont.Face.LatinUi, "G5（前提）换中文文本后指派面不变");
            C(lb.ActualFace, TmpFont.Face.Cjk,
              "G6 ★ 中文卡组名 ⇒ `ActualFace` 回 `Cjk`（`Asar` 没有汉字字形，硬渲会出一排 `.notdef`）");

            // 灭自证：同一棵树里再建一颗**没传 `face:`** 的标签 ⇒ 必须是出厂面 Cjk（证明 G3 是「接线」不是「缺省被改」）
            var ctl = MenuDraw.Text(root.transform, new PxRect(240f, 0f, 460f, 60f), "Control", Color.white,
                                    "W64Control", 30f, MenuDraw.QText);
            T(ctl != null, "G7（前提）对照标签建出来了");
            if (ctl != null)
                C(ctl.Face, TmpFont.Face.Cjk,
                  "G8 ★ 对照标签仍是**出厂面 `Cjk`** ⇒ `MenuDraw.Text` 的缺省面一个字没改（🧨 把缺省改成拉丁面 ⇒ 本行红）");
        }
        finally
        {
            Kill(root);
        }
    }

    /// <summary>某节点是不是挂在 <paramref name="ancestorName"/> 这棵子树里（含自己）。</summary>
    static bool UnderNamed(Transform t, string ancestorName)
    {
        int guard = 0;
        var cur = t;
        while (cur != null && guard++ < 64)
        {
            if (cur.name == ancestorName) return true;
            cur = cur.parent;
        }
        return false;
    }
}
