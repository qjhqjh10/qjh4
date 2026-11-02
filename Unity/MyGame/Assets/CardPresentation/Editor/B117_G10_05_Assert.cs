// B117_G10_05_Assert.cs — 执行代理 `B117-G10-05`（2026-11-02）· 战斗域第 5 部分「玩家/对手名片与联盟窗」的两条修复
//
// ============================ 本件盯的两件 ============================
//  ① **联盟徽章托盘 `Alliance Badge Drawer/{Frame,Badge}` 的画框**：
//     · 原版两层都 `m_PreserveAspect = 1`、`Frame` pivot **(0,0)** / `Badge` pivot **(0,0.5)**
//       ⇒ uGUI `Image.PreserveSpriteAspectRatio` 收窄之后**按 pivot 偏移**（贴左边），⛔ 不是恒居中；
//       贴图全是 1024×1024（4×TIER + 16 张阵营徽章 + Default，22/22 实测），抽屉 139.705994×131.104004
//       ⇒ 画框 = **131.104 × 131.104**、画心 x = **−(139.705994−131.104004)/2 = −4.301**。
//     · 另外画布档的 `Img(parent, …, parentR)` 里 `parentR` 原来传的是 **`Alliance Group` 的 rect**，
//       而父节点是**抽屉**（`LocalIn` = 「相对父中心」）⇒ 又把抽屉那 (−197.45,+8.97) 叠了一次，
//       徽章整块飞出抽屉。两处一起修（同一批）。
//  ② **四颗静态文案**（`Name:` / `Title:` / `Alliance:` / `NotInaAllianceText`）：
//     原版前三颗挂 `Localize`（换语言由 I2 自动换词）、第四颗在 `BattleAlliancePanel__Open.c` 的
//     「无联盟」支里**每次都** `GetTranslation(...)`；本扇窗只 `Build` 一次 ⇒ 战斗内换语言后重开面板
//     必须跟着换（`Apply` 现在每次调 `RefreshStaticLabels`）。
//
// ============================ 判据出处（逐条可复查，⛔ 不读我们自己的常量当期望值）============================
//  · `d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1/`：
//      `RectTransform` 3503（`Alliance Badge Drawer`）`m_SizeDelta = (139.705994, 131.104004)` ·
//      `m_AnchoredPosition = (-197.459930, 8.970734)` · `m_LocalScale = 1`；
//      `Frame` / `Badge` 两层：`anchorMin/Max (0,0)-(1,1)` · `sizeDelta (0,0)` ·
//      pivot 分别 **(0,0)** / **(0,0.5)** · 两张 `Image` 都 `m_PreserveAspect = 1`（`menu_dump` 的
//      `Alliance Panel --depth 10` 逐行可对）。
//  · `d:/2/tools/decomp_full/BattleAlliancePanel__Open.c`（`GetTranslation` 在 else 支里每次调用）·
//    同场景 `MonoBehaviour` 的 `PlayerLabel` / `TitleLabel` / `Alliance Label` 都带 `Localize` 组件。
//  · 官方本地化两列 = `Unity/数据/游戏数据/官方本地化_en+zh-CN.json` 的 `terms`
//      （`Battle/AlliancePanel/PlayerLabel` = `Name:` / `名称：` 等四条）—— 比的是**官方词条**，
//      ⛔ 不是我们 `Loc.T` 的输出（否则就是自证）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  接进任一宿主的聚合行（同一物理行、**第一个 `//` 之前**）：
//      B117_G10_05_Assert.Run(); _sink.Pass += B117_G10_05_Assert.Pass; _sink.Fail += B117_G10_05_Assert.Fail;
//  ⚠️ 本件**自己建一块临时 Canvas**（战斗侧 `HudCanvas` 静态根一个字不动），断完整棵销毁。
using System;
using CardPresentation;
using UnityEngine;

/// <summary>`B117-G10-05`：联盟徽章托盘等比画框（pivot 贴边 + parentR）与四颗静态文案的换语言刷新。</summary>
public static class B117_G10_05_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B117G1005] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    // ============================================================ 入口

    public static void Run()
    {
        MenuCheck.Section(_sink, "B117-G10-05 ① 联盟徽章托盘：Frame/Badge 画框（等比 · 按 pivot 贴左边 · parentR）");
        try { A_BadgeFit(); }
        catch (Exception e) { Tr(false, "「① 徽章托盘」抛异常（本节没跑完）：" + e.GetType().Name + "：" + e.Message); }

        MenuCheck.Section(_sink, "B117-G10-05 ② 联盟窗四颗静态文案：换语言后重开面板必须跟着换");
        try { B_LocalizedLabels(); }
        catch (Exception e) { Tr(false, "「② 静态文案」抛异常（本节没跑完）：" + e.GetType().Name + "：" + e.Message); }

        Debug.Log(_sink.P + "B117G1005 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ ① 徽章托盘

    static void A_BadgeFit()
    {
        var cvGo = new GameObject("[B117G1005] Probe Canvas", typeof(Canvas));
        var cv = cvGo.GetComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        var hostGo = new GameObject("[B117G1005] Probe Host", typeof(RectTransform));
        hostGo.transform.SetParent(cvGo.transform, false);
        var hostRt = (RectTransform)hostGo.transform;
        hostRt.anchorMin = Vector2.zero; hostRt.anchorMax = Vector2.one;
        hostRt.offsetMin = Vector2.zero; hostRt.offsetMax = Vector2.zero;

        AlliancePanelWindow win = null;
        try
        {
            win = AlliancePanelWindow.Create(hostRt, "Alliance Panel", hostRt);
            Tr(win != null, "A0-前提：`Alliance Panel` 在临时 Canvas 下建出来了（建不出来 ⇒ 本节后面全是空转）");
            if (win == null) return;

            var drawer = FindDeep(win.Root, "Alliance Badge Drawer");
            Tr(drawer != null, "A1-前提：`Alliance Badge Drawer` 节点在");
            if (drawer == null) return;
            var drt = drawer as RectTransform;
            Tr(drt != null, "A2-前提：抽屉是 `RectTransform`（= 画布档；世界档不走 `LocalIn`，本节不适用）");
            if (drt == null) return;

            // ---- 抽屉自己（RT 3503）—— 原版逐字段 ----
            N(drt.sizeDelta.x, 139.705994f, 0.05f, "A3 抽屉宽 = 原版 `m_SizeDelta.x` **139.705994**");
            N(drt.sizeDelta.y, 131.104004f, 0.05f, "A4 抽屉高 = 原版 `m_SizeDelta.y` **131.104004**");
            // 相对 `Alliance` 组中心：原版 `m_AnchoredPosition = (-197.459930, 8.970734)`（anchor/pivot 都是中心）
            var grp = drawer.parent as RectTransform;
            Tr(grp != null, "A5-前提：抽屉的父是 `Alliance` 组（`RectTransform`）");
            if (grp == null) return;
            N(drt.anchoredPosition.x, -197.459930f, 0.05f, "A6 抽屉画心 x（相对 `Alliance` 组中心）= 原版 **−197.459930**");
            N(drt.anchoredPosition.y, 8.970734f, 0.05f, "A7 抽屉画心 y（相对 `Alliance` 组中心）= 原版 **8.970734**");

            // ---- 两层 ----
            var fr = drawer.Find("Frame") as RectTransform;
            var bd = drawer.Find("Badge") as RectTransform;
            Tr(fr != null && bd != null, "A8-前提：抽屉下 `Frame` / `Badge` 都在（原版 `m_Children` 就这两颗）");
            if (fr == null || bd == null) return;
            Tr(fr.GetSiblingIndex() < bd.GetSiblingIndex(), "A9 兄弟序 `Frame` 在 `Badge` 之前（原版逐位）");

            // 原版两层 `m_PreserveAspect = 1`、贴图 1024×1024 ⇒ 高受限 ⇒ 画框 131.104 正方
            N(fr.sizeDelta.x, 131.104f, 0.05f, "A10 `Frame` 画框宽 = 抽屉高 **131.104**（等比收窄，⛔ 不是拉满 139.706）");
            N(fr.sizeDelta.y, 131.104f, 0.05f, "A11 `Frame` 画框高 = **131.104**");
            N(bd.sizeDelta.x, 131.104f, 0.05f, "A12 `Badge` 画框宽 = **131.104**");
            N(bd.sizeDelta.y, 131.104f, 0.05f, "A13 `Badge` 画框高 = **131.104**");

            // `Frame` pivot (0,0) / `Badge` pivot (0,0.5) ⇒ 收窄后**贴左边**：画心 x = −(139.705994−131.104004)/2
            const float wantDx = -(139.705994f - 131.104004f) * 0.5f;      // = −4.300995
            N(fr.anchoredPosition.x, wantDx, 0.05f, "A14 ★ `Frame` 画心 x = **−4.301**（pivot (0,0) ⇒ 贴左边；居中会是 0）");
            N(bd.anchoredPosition.x, wantDx, 0.05f, "A15 ★ `Badge` 画心 x = **−4.301**（pivot (0,0.5) ⇒ 贴左边）");
            N(fr.anchoredPosition.y, 0f, 0.05f, "A16 `Frame` 画心 y = 0（高受限 ⇒ 竖向不缩、无偏移）");
            N(bd.anchoredPosition.y, 0f, 0.05f, "A17 `Badge` 画心 y = 0");

            // ---- 灭自证①：左缘必须落在抽屉左缘上（−139.705994/2 = −69.853）----
            //   改回 `keepAspect` 的**恒居中** ⇒ 画心 0、左缘 −65.55 ⇒ 红（差 4.301 px）。
            float left = fr.anchoredPosition.x - fr.sizeDelta.x * 0.5f;
            N(left, -139.705994f * 0.5f, 0.05f,
              "A18 ★★ 灭自证：`Frame` **左缘 == 抽屉左缘**（−69.853）｜🧨 改回恒居中 ⇒ −65.55 ⇒ 红");
            float leftB = bd.anchoredPosition.x - bd.sizeDelta.x * 0.5f;
            N(leftB, -139.705994f * 0.5f, 0.05f,
              "A19 ★★ 灭自证：`Badge` **左缘 == 抽屉左缘**（−69.853）｜🧨 同上");

            // ---- 灭自证②：画框比抽屉**窄**（真的按比例收窄了）----
            Tr(fr.sizeDelta.x < drt.sizeDelta.x - 1f,
               "A20 ★★ 灭自证：`Frame` 画框宽 < 抽屉宽（139.706）—— 把 `PivotFit` 换成「直接用抽屉 rect」⇒ 相等 ⇒ 红");

            // ---- 灭自证③：`parentR` 必须是抽屉自己 —— 旧代码把它传成 `Alliance` 组 ⇒ 画心 x 会是 −197.45 ----
            Tr(Mathf.Abs(fr.anchoredPosition.x) < 10f,
               "A21 ★★ 灭自证：`Frame` 画心 |x| < 10（相对**抽屉中心**）—— 把实参改回 `AllianceGroupR` ⇒ −197.45 ⇒ 红"
             + "（单机恒走「无联盟」支、这组整块不显示 ⇒ 这条不跑就永远看不见）");
        }
        finally
        {
            if (cvGo != null) UnityEngine.Object.DestroyImmediate(cvGo);
        }
    }

    // ============================================================ ② 四颗静态文案

    /// <summary>官方词条两列（`数据/游戏数据/官方本地化_en+zh-CN.json` 的 `terms`，逐字抄；⛔ 不是 `Loc.T` 的输出）。</summary>
    const string EnName = "Name:", EnTitle = "Title:", EnAlliance = "Alliance:";
    const string EnNotIn = "This player is still not part of an Alliance";
    const string ZhName = "名称：", ZhTitle = "标题：", ZhAlliance = "联盟：";
    const string ZhNotIn = "该玩家仍未加入联盟";

    static void B_LocalizedLabels()
    {
        bool wasPersist = Loc.PersistOverride;
        var prevLang = Loc.Current;
        var cvGo = new GameObject("[B117G1005] Probe Canvas", typeof(Canvas));
        var cv = cvGo.GetComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        var hostGo = new GameObject("[B117G1005] Probe Host", typeof(RectTransform));
        hostGo.transform.SetParent(cvGo.transform, false);
        var hostRt = (RectTransform)hostGo.transform;
        hostRt.anchorMin = Vector2.zero; hostRt.anchorMax = Vector2.one;
        hostRt.offsetMin = Vector2.zero; hostRt.offsetMax = Vector2.zero;

        AlliancePanelWindow win = null;
        try
        {
            // ⛔ 自检不许写玩家的真语言设置（同 `B103A03_Assert` 的口径）
            Loc.PersistOverride = true;

            Loc.SetLanguage(AvailableLanguages.English);
            win = AlliancePanelWindow.Create(hostRt, "Alliance Panel", hostRt);
            Tr(win != null, "B0-前提：`Alliance Panel` 建出来了（建不出来 ⇒ 本节后面全是空转）");
            if (win == null) return;

            var lbName = LabelOf(win.Root, "PlayerLabel");
            var lbTitle = LabelOf(win.Root, "TitleLabel");
            var lbAlli = LabelOf(win.Root, "Alliance Label");
            var lbNotIn = LabelOf(win.Root, "NotInaAllianceText");
            Tr(lbName != null && lbTitle != null && lbAlli != null && lbNotIn != null,
               "B1-前提：四颗静态文案节点都在（`PlayerLabel` / `TitleLabel` / `Alliance Label` / `NotInaAllianceText`）");
            if (lbName == null || lbTitle == null || lbAlli == null || lbNotIn == null) return;

            // 三格全非空 ⇒ `TitleHolder` / `Alliance` 组是**显示**的（与 `BattleAlliancePanel__Open` 的显隐三支一致）
            win.Open("Probe Player", "Probe Title", "Probe Alliance");
            string enName = lbName.Text, enTitle = lbTitle.Text, enAlli = lbAlli.Text, enNotIn = lbNotIn.Text;
            C(enName, EnName, "B2 英文：`PlayerLabel` = 官方 `" + EnName + "`");
            C(enTitle, EnTitle, "B3 英文：`TitleLabel` = 官方 `" + EnTitle + "`");
            C(enAlli, EnAlliance, "B4 英文：`Alliance Label` = 官方 `" + EnAlliance + "`");
            C(enNotIn, EnNotIn, "B5 英文：`NotInaAllianceText` = 官方 `" + EnNotIn + "`");

            // ---- 换语言（战斗内设置窗能改）⇒ 重开面板必须跟着换 ----
            Loc.SetLanguage(AvailableLanguages.Chinese);
            win.Open("Probe Player", "Probe Title", "Probe Alliance");
            string zhName = lbName.Text, zhTitle = lbTitle.Text, zhAlli = lbAlli.Text, zhNotIn = lbNotIn.Text;
            C(zhName, ZhName, "B6 中文：`PlayerLabel` = 官方 `" + ZhName + "`");
            C(zhTitle, ZhTitle, "B7 中文：`TitleLabel` = 官方 `" + ZhTitle + "`");
            C(zhAlli, ZhAlliance, "B8 中文：`Alliance Label` = 官方 `" + ZhAlliance + "`");
            C(zhNotIn, ZhNotIn, "B9 中文：`NotInaAllianceText` = 官方 `" + ZhNotIn + "`");

            // 灭自证：四格两语言必须**各不相同** —— 窗只 `Build` 一次，若 `Apply` 不重取，
            // 中文那四条会停在英文上（= B6~B9 红）⇒ 这四条是那条「同一份语言快照」的判别式。
            Tr(enName != zhName && enTitle != zhTitle && enAlli != zhAlli && enNotIn != zhNotIn,
               "B10 ★★ 灭自证：四格「英文 ≠ 中文」—— 删掉 `Apply` 里那次 `RefreshStaticLabels()` ⇒ 中文档仍是英文 ⇒ 红");
        }
        finally
        {
            Loc.SetLanguage(prevLang);              // 还原语言（会再广播一次，无副作用）
            Loc.PersistOverride = wasPersist;
            if (cvGo != null) UnityEngine.Object.DestroyImmediate(cvGo);
        }
    }

    // ============================================================ 小工具

    /// <summary>按名字递归找节点（`transform.Find` 只找**直接子件** —— 见 CLAUDE.md 的 `2DCard` 那条）。</summary>
    static Transform FindDeep(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var r = FindDeep(root.GetChild(i), name);
            if (r != null) return r;
        }
        return null;
    }

    static Label LabelOf(Transform root, string name)
    {
        var t = FindDeep(root, name);
        return t != null ? t.GetComponent<Label>() : null;
    }
}
