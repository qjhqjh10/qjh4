// B141_W20_Assert.cs — 执行代理 `B141-W20`（第一百二十八会话 · 2026-11-04 · 批次 `B141`）
//
// ============================ 它盯的是什么 ============================
//  ① **4-06** 主菜单模式卡网格列上限 = 原版 **20**（我们原来 8）；
//  ② **4-10（MainMenuRuntime 那一处）** 左导航条文案的纵向档 = 原版 **Capline**（我们原来出厂 `Middle`）；
//  ③ **4-19** 模式区横滚步长：原版 `m_ScrollSensitivity = 1.0`（现核「能滚、但一格 1 px ⇒ 实际靠拖拽」），
//     实现保留 2026-10-18 用户拍板的可用步长 `ModeScrollNotchPx = 120`；
//  ④ **`TOOL-03` 剩余两缺口**：`MainMenuCarouselDrawer.Tick` 的生产泵（原来 0 调用）、
//     第 2 实例 `RankedArmyBonusCard` 复用 `InfiniteScrollRing`；
//  ⑤ 顺手订正：`MainMenuSubmenuWindow.BadgeDy` 的 doc（原写「`pos.y` 取负」是反的）。
//
// ============================ 判据（全部现读）============================
//  · 4-06 → `d:/2/tools/assetripper_out/ExportedProject/Assets/Data/Scenes/MainMenu Warpforge.unity`
//    GameObject `Content`(&28) 的 MB `&1961`：`rows: 2` · `columns: 20` · `itemSize 535×414.4` · `spacing 20×20`。
//  · 4-10 → `d:/2/新解包资源/assets_full/bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/`
//    `MonoBehaviour_1716/1749/1750/1751/1752.json`：`m_VerticalAlignment = 8192 (0x2000 = Capline)`、
//    字体 pid `3485036404935369831`（Pragati）。
//  · 4-19 → 同 bundle 的 `MonoBehaviour_1982.json`：`m_Horizontal 1` · `m_Vertical 0` · `m_ScrollSensitivity 1.0`；
//    uGUI `ScrollRect.OnScroll` 在「horizontal && !vertical」时把 `delta.y` 搬进 `delta.x`
//    （`Library/PackageCache/com.unity.ugui@…/Runtime/UGUI/UI/Core/ScrollRect.cs:665-670`，位移在 :675-676）。
//  · TOOL-03 第 2 实例 → `d:/2/tools/assetripper_out/ExportedProject/Assets/Reward Event Container 1x1.prefab`
//    `UI_InfiniteScroll`（`&114397768327470545`）：`AutoScroll: 1` · `AutoScrollSpeed: {x: -50, y: 0}`；
//    容器字段 `thresholdForAutoScroll: 2`；`_threshold` 吃脚本 `Reset()` 的 100（`UI_InfiniteScroll.cs:367`）。
//  · BadgeDy → `…/Assets/Rewards Base Submenu Variant.prefab` 四颗 `Badge Highlight` 的 `m_AnchoredPosition`：
//    三颗 `{x: 51.7, y: -27.2}` + 一颗 `{x: 51.7, y: 47.9}`（**不是**「pos.y 取负」）。
//
// ============================ 🔴 灭自证（哪一处改动会让哪条红）============================
//  · 4-06：`ModeGridMaxCols` 改回 8 ⇒ A1-1 红；把 `const int MaxCols = ModeGridMaxCols;` 换回字面量 ⇒ A1-2 红。
//  · 4-10：删掉那句 `MenuDraw.SetVAlign(navText, Label.VAlign.Capline, …)` ⇒ A2-1 红；原版场景那五颗若变成
//    非 8192 ⇒ A2-2 红（判据本身变）。
//  · 4-19：把 `ModeScrollNotchPx` 改成 1 ⇒ A3-1 红；把调用点换回字面量 `120f` ⇒ A3-2 红；原版 prefab 若
//    灵敏度不是 1.0 ⇒ A3-3 红。
//  · TOOL-03①：把 `Update()` 里那句 `StepOfferRings(Time.deltaTime)` 删掉（或追加到一行 `//` 之后）
//    ⇒ A4-1 红；`StepOfferRings` 不再调 `_rankedArmyBonusCard.Tick` ⇒ A4-2 红；`StepOfferRingsForTest`
//    不再 `d.Tick(dt)` ⇒ A4-4 的 `Ring.ContentX` 停在 0 ⇒ 红。
//  · TOOL-03②：删掉 `Ring.SetItems(…)/Ring.AutoScroll = …` ⇒ A5-1/A5-3 红；删掉 `Clean()` 里的 `Ring.SetItems(0,…)`
//    ⇒ A5-6 红；`Ring.AutoScroll` 不跟 `AutoScrollEnabled` ⇒ 2 阵营那档（A5-7）红（默认 true）。
//  · BadgeDy：把 doc 退回「`pos.y` 取负」⇒ A6-2 红；原版 prefab 里那颗若不是 +47.9 ⇒ A6-3 红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  本件自成一体：**源码闸 + 纯数据行为**（只建一颗临时卡/抽屉做环的行为测试，不建菜单、不依赖场景）。
//  可单独跑：`-executeMethod B141_W20_Assert.Run`；也可由调度台接进任一 Editor 宿主：
//      `B141_W20_Assert.Run(); pass += B141_W20_Assert.Pass; fail += B141_W20_Assert.Fail;`
//  `Pass` / `Fail` 是**本文件自己的 sink**（不是宿主的）⇒ 宿主必须显式累加；⛔ 别调两次（会重复累计）。
//  ⚠️ A1/A2/A3 的「原版证据」读 `D:/2/...`（本机档案库）；文件不在 ⇒ 那几条**当场红**（⛔ 不静默跳过）。
using System;
using System.IO;
using System.Text;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B141_W20`：4-06 / 4-10 / 4-19 / TOOL-03 剩余 / BadgeDy doc 的断言（源码闸 + 环行为）。</summary>
public static class B141_W20_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B141W20] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void Near(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    // ============================================================ 路径
    const string SrcMainMenu = "CardPresentation/Shell/MainMenuRuntime.cs";
    const string SrcRanked = "CardPresentation/Shell/RankedArmyBonusCard.cs";
    const string SrcSubmenu = "CardPresentation/Shell/MainMenuSubmenuWindow.cs";

    const string OrigMainMenuScene =
        "D:/2/tools/assetripper_out/ExportedProject/Assets/Data/Scenes/MainMenu Warpforge.unity";
    const string OrigSceneMbDir =
        "D:/2/新解包资源/assets_full/bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour";
    const string OrigRewardPrefab =
        "D:/2/tools/assetripper_out/ExportedProject/Assets/Reward Event Container 1x1.prefab";
    const string OrigSubmenuPrefab =
        "D:/2/tools/assetripper_out/ExportedProject/Assets/Rewards Base Submenu Variant.prefab";

    static readonly string[] OrigNavTmp =
    {
        "MonoBehaviour_1716.json", "MonoBehaviour_1749.json", "MonoBehaviour_1750.json",
        "MonoBehaviour_1751.json", "MonoBehaviour_1752.json",
    };

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B141_W20 —— 4-06 / 4-10 / 4-19 / TOOL-03 剩余 / BadgeDy doc");
        Run4_06();
        Run4_10();
        Run4_19();
        RunTool03_Pump();
        RunTool03_RankedRing();
        RunBadgeDyDoc();
        Debug.Log(_sink.P + "B141W20 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A1 · 4-06 网格列上限 = 20
    static void Run4_06()
    {
        try
        {
            MenuCheck.Section(_sink, "A1 ★ 4-06 模式卡网格列上限 = 原版 `FlexibleGridLayout.columns = 20`（我们原来 8）");
            string src = ReadRepo(SrcMainMenu);
            Tr(src != null, "A1-0 **前提**：读得到 `" + SrcMainMenu + "`（读不到时本节等于没验 ⇒ 当场红）");
            if (src == null) return;

            MenuCheck.Check(_sink, MainMenuRuntime.ModeGridMaxCols, 20,
                "A1-1 ★ `MainMenuRuntime.ModeGridMaxCols == 20`（原版 `columns: 20`）"
              + "｜🧨 改回 8 ⇒ 红");

            string code = StripLineComments(src);
            Tr(code.IndexOf("const int MaxCols = ModeGridMaxCols;", StringComparison.Ordinal) >= 0,
                "A1-2 ★ `_placeAllCards` 里的 `const int MaxCols = ModeGridMaxCols;` 在场（= 真的用上了那个常量）"
              + "｜🧨 换回字面量 ⇒ 红");
            Tr(code.IndexOf("const int MaxCols = 8", StringComparison.Ordinal) < 0,
                "A1-3 ★ 旧的 `const int MaxCols = 8` 已不存在");

            // 原版证据：场景 YAML 里 GameObject `Content`(&28) 的 grid 块
            string scene = ReadAbs(OrigMainMenuScene);
            Tr(scene != null, "A1-4 **前提**：读得到原版场景 `" + OrigMainMenuScene + "`（判据本机档案库）");
            if (scene == null) return;
            MenuCheck.Check(_sink, CountOf(scene, "columns: 20"), 1,
                "A1-5 ★ 原版场景里 `columns: 20` **恰好 1 处**（那正是 `GameModes/Content` 的 grid）");
            Tr(scene.IndexOf("itemSize: {x: 535, y: 414.4}", StringComparison.Ordinal) >= 0,
                "A1-6 ★ 原版同一份 grid 的 `itemSize: {x: 535, y: 414.4}` 也在（= 定位到的是同一颗组件）");
            Tr(scene.IndexOf("rows: 2", StringComparison.Ordinal) >= 0,
                "A1-7 ★ 原版 `rows: 2`（两行网格；我们按 first-fit 摆两行，口径不变）");
        }
        catch (Exception ex) { SegFail("A1 4-06", ex, 5); }
    }

    // ============================================================ A2 · 4-10 左导航纵向档 = Capline
    static void Run4_10()
    {
        try
        {
            MenuCheck.Section(_sink, "A2 ★ 4-10 左导航条文案 = 原版 `Capline`（`m_VerticalAlignment 8192 = 0x2000`）");
            string src = ReadRepo(SrcMainMenu);
            Tr(src != null, "A2-0 **前提**：读得到 `" + SrcMainMenu + "`");
            if (src == null) return;

            string code = StripLineComments(src);
            int nav = code.IndexOf("var navText = Text(b,", StringComparison.Ordinal);
            Tr(nav >= 0, "A2-1 **前提**：导航文案那一句是 `var navText = Text(b, …)`（`SetVAlign` 需要那个句柄）");
            int set = code.IndexOf("MenuDraw.SetVAlign(navText, Label.VAlign.Capline", StringComparison.Ordinal);
            Tr(set >= 0 && nav >= 0 && set > nav,
                "A2-2 ★ 紧跟其后调了 `MenuDraw.SetVAlign(navText, Label.VAlign.Capline, textBgR, Label.OrigFace.Pragati)`"
              + "（= 原版五颗的纵向档；⛔ 不是出厂 `Middle`）｜🧨 删掉它 ⇒ 红");
            Tr(code.IndexOf("textBgR, Label.OrigFace.Pragati", StringComparison.Ordinal) >= 0,
                "A2-3 ★ 框高传的是文案条矩形 `textBgR`（高 39.4；原版 TMP 自身 sd.y = 39.39）、字体面 Pragati");

            foreach (var f in OrigNavTmp)
            {
                string p = Path.Combine(OrigSceneMbDir, f);
                string js = ReadAbs(p);
                Tr(js != null, "A2-4 **前提**：读得到原版 TMP `" + f + "`");
                if (js == null) continue;
                Tr(js.IndexOf("\"m_VerticalAlignment\": 8192", StringComparison.Ordinal) >= 0,
                    "A2-5 ★ `" + f + "` 的 `m_VerticalAlignment = 8192`（= 0x2000 = **Capline**）");
                Tr(js.IndexOf("3485036404935369831", StringComparison.Ordinal) >= 0,
                    "A2-6 ★ `" + f + "` 的字体 pid = `3485036404935369831`（Pragati；与 `Label.OrigFace.Pragati` 同源）");
            }
        }
        catch (Exception ex) { SegFail("A2 4-10", ex, 6); }
    }

    // ============================================================ A3 · 4-19 横滚步长 / 原版灵敏度
    static void Run4_19()
    {
        try
        {
            MenuCheck.Section(_sink, "A3 ★ 4-19 模式区横滚步长：原版 `m_ScrollSensitivity = 1.0`，实现保留用户拍板的 120 px/格");
            string src = ReadRepo(SrcMainMenu);
            Tr(src != null, "A3-0 **前提**：读得到 `" + SrcMainMenu + "`");
            if (src == null) return;

            Near(MainMenuRuntime.ModeScrollNotchPx, 120f, 0f,
                "A3-1 ★ `ModeScrollNotchPx = 120`（= 2026-10-18 **用户拍板**的可用步长，⛔ 不是原版值）");
            Near(MainMenuRuntime.OriginalModeScrollSensitivity, 1.0f, 0f,
                "A3-2 ★ `OriginalModeScrollSensitivity = 1.0`（原版那台 ScrollRect 的 `m_ScrollSensitivity` 留痕）");

            string code = StripLineComments(src);
            Tr(code.IndexOf("new MenuScroll(ModeViewport, CardCol0, ModeContentX2, ModeScrollNotchPx)", StringComparison.Ordinal) >= 0,
                "A3-3 ★ 调用点真的用了 `ModeScrollNotchPx` 这个常量（不是散落的字面量）｜🧨 换回 `120f` ⇒ 红");
            Tr(code.IndexOf("ModeContentX2, 120f)", StringComparison.Ordinal) < 0,
                "A3-4 ★ 旧的 `…, ModeContentX2, 120f)` 已不存在");

            // 原版证据：MB_1982 = GameModes 那台 ScrollRect
            string p1982 = Path.Combine(OrigSceneMbDir, "MonoBehaviour_1982.json");
            string js = ReadAbs(p1982);
            Tr(js != null, "A3-5 **前提**：读得到原版 `MonoBehaviour_1982.json`（GameModes 的 ScrollRect）");
            if (js == null) return;
            Tr(js.IndexOf("\"m_ScrollSensitivity\": 1.0", StringComparison.Ordinal) >= 0,
                "A3-6 ★ 原版 `m_ScrollSensitivity = 1.0`");
            Tr(js.IndexOf("\"m_Horizontal\": 1", StringComparison.Ordinal) >= 0
            && js.IndexOf("\"m_Vertical\": 0", StringComparison.Ordinal) >= 0,
                "A3-7 ★ 原版是 **horizontal-only**（`m_Horizontal 1 / m_Vertical 0`）"
              + " ⇒ uGUI `ScrollRect.OnScroll` 会把 `delta.y` 搬进 `delta.x`（`ScrollRect.cs:665-670`）"
              + " ⇒ 一格滚轮 = **1 px**：**能滚，但肉眼几乎不动**，原版这一段实际靠**拖拽**（我们的拖拽照旧）");
        }
        catch (Exception ex) { SegFail("A3 4-19", ex, 7); }
    }

    // ============================================================ A4 · TOOL-03 ① Tick 生产泵
    static void RunTool03_Pump()
    {
        MainMenuCarouselDrawer probe = null;
        GameObject go = null;
        try
        {
            MenuCheck.Section(_sink, "A4 ★ TOOL-03① `MainMenuCarouselDrawer.Tick` 的生产泵（`Update ▸ StepOfferRings`）");
            string src = ReadRepo(SrcMainMenu);
            string srcRanked = ReadRepo(SrcRanked);
            Tr(src != null, "A4-0 **前提**：读得到 `" + SrcMainMenu + "`");
            if (src == null) return;

            string code = StripLineComments(src);
            int upd = code.IndexOf("void Update()", StringComparison.Ordinal);
            string updBody = upd < 0 ? "" : BodyOf(code, code.IndexOf('{', upd));
            Tr(updBody.Length > 2, "A4-1 **前提**：截到了 `Update()` 的方法体（实得 " + updBody.Length + " 字符）");
            Tr(updBody.IndexOf("StepOfferRings(Time.deltaTime)", StringComparison.Ordinal) >= 0,
                "A4-2 ★ `Update()` 真的泵了 `StepOfferRings(Time.deltaTime)`（= 原版 `UI_InfiniteScroll.Update()`）"
              + "｜🧨 删掉这句（或把它追加到行尾 `//` 之后）⇒ 红");

            int st = code.IndexOf("void StepOfferRings(float dt)", StringComparison.Ordinal);
            string stBody = st < 0 ? "" : BodyOf(code, code.IndexOf('{', st));
            Tr(stBody.Length > 2, "A4-3 **前提**：截到了 `StepOfferRings` 的方法体");
            Tr(stBody.IndexOf("_rankedArmyBonusCard.Tick(dt)", StringComparison.Ordinal) >= 0,
                "A4-4 ★ 同一个泵也喂了第 2 实例（`_rankedArmyBonusCard.Tick(dt)`）");

            int bd = code.IndexOf("static MainMenuCarouselDrawer BuildOfferDrawerForMenuCard", StringComparison.Ordinal);
            string bdBody = bd < 0 ? "" : BodyOf(code, code.IndexOf('{', bd));
            Tr(bdBody.Length > 2, "A4-5 **前提**：截到了 `BuildOfferDrawerForMenuCard` 的方法体");
            Tr(bdBody.IndexOf("RegisterOfferDrawerForTick(d)", StringComparison.Ordinal) >= 0,
                "A4-6 ★ 建好的抽屉真的登记进了泵（`RegisterOfferDrawerForTick(d)`；⛔ 只建不泵 = 原来那个 0 调用缺陷）");

            // ---- 行为：同一个泵函数推进环 ----
            MenuCheck.Section(_sink, "A4 · 行为：`StepOfferRingsForTest(dt)` 真的推进已登记抽屉的环");
            go = new GameObject("B141_W20_Probe", typeof(RectTransform));
            probe = go.AddComponent<MainMenuCarouselDrawer>();
            Tr(probe != null, "A4-7a **前提**：`MainMenuCarouselDrawer` 挂得上临时节点（它没有自己的脚本资产 —— "
                            + "`AddComponent` 这条不要求有，见 `CLAUDE.md` §三 那条「嵌套类也能挂」）");
            if (probe == null) return;
            probe.Ring.SetItems(3, 100f);
            probe.Ring.AutoScroll = true;
            int before = MainMenuRuntime.OfferDrawerPumpCountForTest;
            MainMenuRuntime.RegisterOfferDrawerForTick(probe);
            MenuCheck.Check(_sink, MainMenuRuntime.OfferDrawerPumpCountForTest, before + 1,
                "A4-7 **前提**：登记后比登记前多 1 颗（before=" + before + "；⛔ 不整表清空 —— 宿主里可能已有生产抽屉）");

            int n = MainMenuRuntime.StepOfferRingsForTest(0.5f);
            Tr(n >= 1, "A4-8 ★ 泵返回「推了 " + n + " 颗」（≥1；宿主里可能还有生产抽屉，所以不写死 1）");
            Near(probe.Ring.ContentX, -25f, 1e-3f,
                "A4-9 ★ `ContentX` 推进了 `AutoScrollSpeed.x(-50) × dt(0.5)` = −25"
              + "｜🧨 泵里不再调 `d.Tick(dt)` ⇒ 停在 0 ⇒ 红");
            MenuCheck.Check(_sink, probe.Ring.ScrollCount, 1,
                "A4-10 ★ 同一次推进里 `OnScroll()` 跑过 1 趟（= 原版 `onValueChanged` 那条链）");
        }
        catch (Exception ex) { SegFail("A4 TOOL-03①", ex, 10); }
        finally
        {
            MainMenuRuntime.UnregisterOfferDrawerForTick(probe);
            if (go != null) DestroySafe(go);
        }
    }

    // ============================================================ A5 · TOOL-03 ② 第 2 实例复用环
    static void RunTool03_RankedRing()
    {
        var root = new GameObject("B141_W20_RankedProbe", typeof(RectTransform)).transform;
        try
        {
            MenuCheck.Section(_sink, "A5 ★ TOOL-03② `RankedArmyBonusCard` 复用 `InfiniteScrollRing`（threshold=2 · speed (-50,0)）");

            // 原版证据：同一份 prefab 的 UI_InfiniteScroll 块
            string prefab = ReadAbs(OrigRewardPrefab);
            Tr(prefab != null, "A5-0 **前提**：读得到原版 `" + OrigRewardPrefab + "`");
            if (prefab == null) return;
            Tr(prefab.IndexOf("AutoScroll: 1", StringComparison.Ordinal) >= 0,
                "A5-1 ★ 原版 `UI_InfiniteScroll.AutoScroll = 1`");
            Tr(prefab.IndexOf("AutoScrollSpeed: {x: -50, y: 0}", StringComparison.Ordinal) >= 0,
                "A5-2 ★ 原版 `AutoScrollSpeed = (-50, 0)`（❓ 那次没读到 speed 的缺口 —— 本行已从 prefab 现读解出）");
            Tr(prefab.IndexOf("thresholdForAutoScroll: 2", StringComparison.Ordinal) >= 0,
                "A5-3 ★ 原版 `thresholdForAutoScroll = 2`");
            MenuCheck.Check(_sink, RankedArmyBonusCard.ThresholdForAutoScroll, 2,
                "A5-4 ★ 我们的常量 `ThresholdForAutoScroll = 2`");

            var evt3 = new RankedArmyBonusData.Event { title = "T", endsInSeconds = 3600f, affectedArmies = new[] { 10, 50, 110 } };
            var card = RankedArmyBonusCard.Create(root);
            Tr(card != null, "A5-5 **前提**：`Reward Event Container 1x1` 建得出来");
            if (card == null) return;
            card.Initialize(evt3);

            MenuCheck.Check(_sink, card.Ring.ItemCount, 3,
                "A5-6 ★ 环的 `ItemCount` = 3（= `currentArmyImages.Count`；`SetItems` 那把尺）"
              + "｜🧨 删掉 `Initialize` 里的 `Ring.SetItems(…)` ⇒ 停在 0 ⇒ 红");
            Near(card.Ring.RecordOffsetX, 254.90341f + 20f, 1e-3f,
                "A5-7 ★ 槽距 = `sizeDelta.x(254.90341) + HLG spacing(20)` = 274.90341（原版从相邻两件实测，不是常量）");
            Near(card.Ring.AutoScrollSpeed.x, -50f, 0f, "A5-8 ★ 环的横向速度 = −50");
            Near(card.Ring.AutoScrollSpeed.y, 0f, 0f, "A5-9 ★ 环的纵向速度 = 0（原版横向才取 x）");
            Near(card.Ring.Threshold, 100f, 0f,
                "A5-10 ★ `Threshold = 100`（脚本 `Reset()` 的出厂值，`UI_InfiniteScroll.cs:367`；prefab 未序列化该字段）");
            Tr(card.AutoScrollEnabled, "A5-11 **前提**：3 > `thresholdForAutoScroll(2)` ⇒ `AutoScrollEnabled = true`");
            Tr(card.Ring.AutoScroll == card.AutoScrollEnabled,
                "A5-12 ★ 环的 `AutoScroll` = `AutoScrollEnabled`（= 原版 `*(bool*)(scroll+0x22)` 那一位）"
              + "｜🧨 不写这一句 ⇒ 环停在默认 true ⇒ 2 阵营那档红");

            card.Tick(0.5f);
            Near(card.Ring.ContentX, -25f, 1e-3f, "A5-13 ★ `Tick(0.5)` 后 `ContentX = −25`（−50 × 0.5）");
            MenuCheck.Check(_sink, card.Ring.ScrollCount, 1, "A5-14 ★ 同一次 `Tick` 里 `OnScroll()` 跑过 1 趟");

            card.Clean();
            MenuCheck.Check(_sink, card.Ring.ItemCount, 0,
                "A5-15 ★ `Clean()` 后环清空（= 原版 `DestroyAllChildren` 之后 `content.childCount` 归 0）"
              + "｜🧨 删掉 `Clean()` 里的 `Ring.SetItems(0, 0f)` ⇒ 停在 3 ⇒ 红");
            Tr(!card.AutoScrollEnabled, "A5-16 ★ `Clean()` 后 `AutoScrollEnabled = false`");

            // 2 阵营（= threshold，不是 >）⇒ 不自动滚
            var evt2 = new RankedArmyBonusData.Event { title = "T", endsInSeconds = 60f, affectedArmies = new[] { 10, 50 } };
            var card2 = RankedArmyBonusCard.Create(root);
            Tr(card2 != null, "A5-17 **前提**：第二颗卡建得出来（2 阵营档）");
            if (card2 == null) return;
            card2.Initialize(evt2);
            Tr(!card2.AutoScrollEnabled,
                "A5-18 ★ 2 阵营 ⇒ `2 < 2` 为假 ⇒ `AutoScrollEnabled = false`（= 原版那一条严格的 `<`）");
            Tr(!card2.Ring.AutoScroll, "A5-19 ★ 环的 `AutoScroll` 同步为 false｜🧨 未同步 ⇒ 默认 true ⇒ 红");
            card2.Tick(0.5f);
            Near(card2.Ring.ContentX, 0f, 1e-3f, "A5-20 ★ `AutoScroll = false` 时 `Tick` **不推进** `ContentX`（停在 0）");
        }
        catch (Exception ex) { SegFail("A5 TOOL-03②", ex, 12); }
        finally
        {
            if (root != null) DestroySafe(root.gameObject);
        }
    }

    // ============================================================ A6 · BadgeDy doc 订正
    static void RunBadgeDyDoc()
    {
        try
        {
            MenuCheck.Section(_sink, "A6 ★ `MainMenuSubmenuWindow.BadgeDy` 的 doc 符号（原写「`pos.y` 取负」是反的）");
            string src = ReadRepo(SrcSubmenu);
            Tr(src != null, "A6-0 **前提**：读得到 `" + SrcSubmenu + "`");
            if (src == null) return;

            Tr(src.IndexOf("原版 uGUI `pos.y` 原值", StringComparison.Ordinal) >= 0,
                "A6-1 ★ doc 写的是「原版 uGUI `pos.y` **原值**」（BadgeDy 与 pos.y 同号）");
            Tr(src.IndexOf("`pos.y` 取负", StringComparison.Ordinal) < 0,
                "A6-2 ★ 旧的「`pos.y` 取负」说法已不存在｜🧨 退回它 ⇒ 红（那会让下一个人把 47.9 存成 −47.9）");
            Tr(src.IndexOf("cy − spec.BadgeDy", StringComparison.Ordinal) >= 0
            || src.IndexOf("cy - spec.BadgeDy", StringComparison.Ordinal) >= 0,
                "A6-3 ★ 摆放式仍是 `cy − BadgeDy`（正的 BadgeDy = 在键中心**之上**）");

            string prefab = ReadAbs(OrigSubmenuPrefab);
            Tr(prefab != null, "A6-4 **前提**：读得到原版 `" + OrigSubmenuPrefab + "`");
            if (prefab == null) return;
            MenuCheck.Check(_sink, CountOf(prefab, "m_AnchoredPosition: {x: 51.7, y: -27.2}"), 3,
                "A6-5 ★ 原版四颗 `Badge Highlight` 里 **3 颗** `pos.y = −27.2`");
            MenuCheck.Check(_sink, CountOf(prefab, "m_AnchoredPosition: {x: 51.7, y: 47.9}"), 1,
                "A6-6 ★ 第 4 颗（Booster Packs 那页）= **`pos.y = +47.9`**（正的、不是取负）");
        }
        catch (Exception ex) { SegFail("A6 BadgeDy", ex, 6); }
    }

    // ============================================================ 工具
    static string ReadRepo(string rel)
    {
        try
        {
            string full = Path.Combine(Application.dataPath, rel.Replace('/', Path.DirectorySeparatorChar));
            return File.Exists(full) ? File.ReadAllText(full) : null;
        }
        catch { return null; }
    }

    static string ReadAbs(string abs)
    {
        try { return File.Exists(abs) ? File.ReadAllText(abs) : null; }
        catch { return null; }
    }

    static int CountOf(string text, string needle)
    {
        int n = 0, i = 0;
        while ((i = text.IndexOf(needle, i, StringComparison.Ordinal)) >= 0) { n++; i += needle.Length; }
        return n;
    }

    /// <summary>从 `{` 开始按花括号配平截出这一层（⛔ 不做通用词法分析）。
    /// ⚠️ 前提：这一段里没有带花括号的字符串字面量（本批四个方法体都满足；改方法体时**先看一眼**）。</summary>
    static string BodyOf(string text, int braceAt)
    {
        if (braceAt < 0) return "";
        int depth = 0;
        for (int i = braceAt; i < text.Length; i++)
        {
            if (text[i] == '{') depth++;
            else if (text[i] == '}') { depth--; if (depth == 0) return text.Substring(braceAt, i - braceAt + 1); }
        }
        return text.Substring(braceAt);
    }

    /// <summary>剥掉**行注释**（`//` 到行尾）。⚠️ 前提：被扫的源码里字符串字面量不出现 `//`
    /// （本批三段满足）。用它是为了挡「代码被追加到行尾 `//` 之后」那一类静默失效。</summary>
    static string StripLineComments(string s)
    {
        var sb = new StringBuilder(s.Length);
        int i = 0;
        while (i < s.Length)
        {
            if (s[i] == '/' && i + 1 < s.Length && s[i + 1] == '/')
            {
                while (i < s.Length && s[i] != '\n') i++;
                continue;
            }
            sb.Append(s[i]);
            i++;
        }
        return sb.ToString();
    }

    static void DestroySafe(GameObject go)
    {
        if (go == null) return;
#if UNITY_EDITOR
        if (!Application.isPlaying) { UnityEngine.Object.DestroyImmediate(go); return; }
#endif
        UnityEngine.Object.Destroy(go);
    }

    /// <summary>段级 catch：用本文件既有的 `Tr` 口记一次失败（⛔ 不是跳过）。</summary>
    static void SegFail(string section, Exception ex, int rest)
    {
        Tr(false, section + " 段异常（算红，⛔ 不是跳过；其后 " + rest + " 条没跑）: " + ex.GetType().Name + "：" + ex.Message);
    }
}
