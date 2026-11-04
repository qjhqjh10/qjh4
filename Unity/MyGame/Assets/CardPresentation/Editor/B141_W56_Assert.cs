// B141_W56_Assert.cs — 执行写手 `B141-W56`（第一百二十八会话 · 波次 `W129` · 条目 `B141-ARMY-SELECTOR-SOFT`）
//
// ============================ 它盯的是什么 ============================
//  判据 = `资料/完全复刻/判据/B141_W129_对账_UI动画.md` §3.1（4-16 相关口径）·
//         报告 `资料/完全复刻/报告/B141_W129_GlowClip3.md` §三（W27 顺手发现）。
//  缺陷 = `Shell/ShowEventArmiesPanel.cs:110` 把 `Army Selector` 视口的 `m_Softness` 写死 `(0,0)`，
//         而原版 **逐窗不同**（铁律 5·c：一个值 ≠ 全部情况）。
//
//  原版逐项现读（`d:/2/tools/assetripper_out/ExportedProject/Assets/*.prefab` 的 `RectMask2D`，2026-11-04；
//  五处的 `m_Padding` 全是 `(0,0,0,0)` ⇒ 本笔只动 `m_Softness`）：
//    · `EnergySinglePlayerOnlyEventWindow` ▸ `Army Selector Panel/Army Selector/Viewport`  `m_Softness (25,0)`
//      （`:6892-6898`，父链逐层解过）—— **本件唯一宿主**（原版 `ShowEventArmiesPanel` 只有一个调用点 = 能量窗 `Open()`）
//    · `SkirmishModeEventWindow` / `RankedEventWindowV2` ▸ `Ranked Army Selector/Army Selector/Viewport`  `(0,52)`
//    · `Campaign Army Selector` / `Campaign Tab` ▸ `…/Viewport`                                    `(0,0)`
//    · `Ranked{Skirmish,Classic}LeaderboardPopup` / `DraftLeaderboardPopup` ▸ `…/Army Selector/Viewport`  `(42,0)`
//    · 另有 `Forge Tab` `(42,0)` · `Practice Mode Menu` `(0,50)`（**不走本件**，各自窗内传）
//
// ============================ 改成了什么 ============================
//  `ShowEventArmiesPanel.Build(...)` 多一个形参 `Vector2Int? softness = null`：
//    · 传了 ⇒ 写进那颗 `ViewportClip.softness`；
//    · **没传 ⇒ `Debug.LogWarning` 出声**（⛔ 不静默 —— 缺省路径本身就是要修的那个缺陷的形状）。
//  能量窗 `BuildArmySelector` 显式传 `new Vector2Int(25, 0)`（`EnergySinglePlayerOnlyEventWindow.cs:948`）。
//  ✅ 2026-11-04（B141-W62）：`Editor/ShellScene.cs:11282` 已补第 5 实参 `softness: new Vector2Int(25, 0)`（本行原写「仍调 4 参 ⇒ 会走出声那一支、期望值仍是 (0,0)」**已过期**），
//     ⇒ 现在两处调用点都显式传参、无「没传 softness」警告；宿主调用点由本文件 §E 的源码闸盯住（删掉第 5 实参 ⇒ E2 红）。
//
// ============================ 灭自证（🧨 每条怎么才能红）============================
//  · A2/A3 拿 **prefab 字面量 (25,0)** 比实例字段（⛔ 不拿实现里的常量当期望）：
//    把 `ShowEventArmiesPanel.Build` 里那句 `p._softness` 改回 `Vector2Int.zero` ⇒ A3 当场红（参数被无视）。
//  · A1 断 `SoftnessGiven` —— 把能量窗第 5 个实参删掉 ⇒ 走兜底 + 出声 ⇒ A1/A2 红（缺省不得被静默用到）。
//  · B1 扫**源码非注释部分**：`ViewportClip.Hang(sel, \"Viewport\"…` 那一行必须带 `p._softness` 且**不含** `Vector2Int.zero`
//    ⇒ 与 A3 **不共因**（A3 看运行结果、B1 看建造口；把常量写死会让两条同时红，改参数名会让 B1 先红）。
//  · D1/D2/D3 直接读**各窗的常量**（不建窗）⇒ 谁把别的窗的值抄成 25 / 52 ⇒ 红；
//    D4/D5 扫源码字面量（Campaign 的 `Vector2Int.zero`、Forge 的 `new Vector2Int(42, 0)`）。
//  · E1/E2 扫宿主 `Editor/ShellScene.cs` 源码：把 `:11282` 的第 5 实参删掉 ⇒ `Build(pRoot);` 回潮 ⇒ 红
//    （= 派活单要求的「灭自证」：删第 5 实参既会让 `Build` 出声、也会让本件红）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  宿主 = `CardPresentation/Editor/ShellScene.cs`（能量窗就在那儿建；同波 `B141W27_Assert` 的宿主是 `MainMenuScene.cs`）。
//  在该宿主**合计行之前、同一物理行**（该行第一个 `//` **之前**）追加：
//      B141W56_Assert.Run(); _sink.Pass += B141W56_Assert.Pass; _sink.Fail += B141W56_Assert.Fail;
//  （计数器变量名以宿主当时那一对为准。⚠️ 追加必须落在第一个 `//` 之前 —— 本仓已 6 次踩「代码落进行尾注释
//   ⇒ 编译过、账绿、断言一条没跑」。）
//  ✅ 2026-11-04（B141-W62）已按上式接线（同一物理行、`B118W24_Assert.Run(...)` 之后、`=== 合计：` 之前）。
//  ⚠️ 本件会建**一扇能量窗**再销毁（`finally` 里 `DestroyImmediate` + `PointerLayer.UnregisterOwnedBy`）。
//
// ============================ 判据 / 报告 ============================
//  · 报告：`资料/完全复刻/报告/B141_W129_ArmySelectorSoft.md`
//  · 原版：`EnergySinglePlayerOnlyEventWindow.prefab:6892-6898`（`m_Padding` / `m_Softness` + 父链）
//          · `SkirmishModeEventWindow.prefab:4505-4512` · `RankedEventWindowV2.prefab:4895-4902`
using System;
using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>执行写手 `B141-W56`：`ShowEventArmiesPanel` 视口软边**逐窗传参**（Energy `(25,0)`）的落地回归断言。
/// <para>⛔ 期望值全是 prefab 实读的字面量，不从被测实现里读回来。</para></summary>
public static class B141W56_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B141W56] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    /// <summary>唯一的入口（宿主聚合行里调）。</summary>
    public static void Run()
    {
        _sink.Pass = 0; _sink.Fail = 0;
        MenuCheck.Section(_sink, "🆕 B141_W56：`Army Selector` 视口软边逐窗传（Energy `(25,0)`；Skirmish/Ranked `(0,52)` · Campaign `(0,0)` · 排行 `(42,0)`）");
        try { WindowsManager.Get(); } catch (Exception e) { Tr(false, "（前提）`WindowsManager.Get()` 抛异常：" + e.GetType().Name + "：" + e.Message); }
        A_EnergyPanel();
        B_SharedComponentSource();
        C_CallSites();
        D_OtherWindows();
        E_HostCallSite();
        Debug.Log(_sink.P + "合计：✓ " + _sink.Pass + " · ✗ " + _sink.Fail);
    }

    // ==================================================================
    //  A · 行为：能量窗那颗视口真的带上 (25,0)
    // ==================================================================
    static void A_EnergyPanel()
    {
        EnergySinglePlayerOnlyEventWindow win = null;
        try
        {
            var wm = WindowsManager.Get();
            Tr(wm != null, "A0（前提）拿得到 `WindowsManager`（`Get()` 缺了就现建宿主）");
            if (wm == null) return;
            win = EnergySinglePlayerOnlyEventWindow.Create(wm);
            Tr(win != null, "A0（前提）`EnergySinglePlayerOnlyEventWindow.Create` 建出窗来");
            if (win == null) return;

            var panel = win.ArmiesPanel;
            Tr(panel != null, "A0（前提）`ArmiesPanel` 建出来了（`BuildArmySelector` → `ShowEventArmiesPanel.Build`）");
            if (panel == null) return;

            C(panel.SoftnessGiven, true,
              "A1 ★ 调用方**真的传了** softness（`SoftnessGiven`）｜🧨 删掉 `EnergySinglePlayerOnlyEventWindow.cs:948` 第 5 个实参 ⇒ 走兜底 ⇒ 红（缺省不得被静默用到）");
            C(panel.Softness, new Vector2Int(25, 0),
              "A2 ★ `ShowEventArmiesPanel` 收下的软边 = **(25,0)**（`EnergySinglePlayerOnlyEventWindow.prefab:6898`）｜🧨 实参改成别的值 ⇒ 红");

            var vc = panel.Viewport;
            Tr(vc != null, "A3（前提）那颗 `Viewport` 上挂着 `ViewportClip`");
            if (vc == null) return;
            C(vc.softness.x, 25, "A3 ★ `RectMask2D.m_Softness.x` = **25**（左右各 25px 渐隐）｜🧨 把 `Build` 里 `p._softness` 改回 `Vector2Int.zero` ⇒ 红（形参被无视）");
            C(vc.softness.y, 0, "A3 ★ `m_Softness.y` = **0**（上下硬边）");
            C(vc.padding, Vector4.zero, "A4 ★ `m_Padding` = **(0,0,0,0)**（原版这颗五处全 0 ⇒ 本笔不传 pad）");
            Tr(vc.State.RenderClip.HasValue, "A5 ★ `State.RenderClip` 非空（真会裁；`(0,0)` 只表示没有渐隐带，不是不裁）");
        }
        catch (Exception e)
        {
            Tr(false, "A 段抛异常（不当成通过）：" + e.GetType().Name + "：" + e.Message);
        }
        finally
        {
            if (win != null)
            {
                try { PointerLayer.UnregisterOwnedBy(win.gameObject); } catch { }   // 编辑模式不派 `OnDestroy` ⇒ 手动摘滚动区
                try { UnityEngine.Object.DestroyImmediate(win.gameObject); } catch { }
            }
        }
    }

    // ==================================================================
    //  B · 共享件源码：软边必须是形参、⛔ 不能写常量
    // ==================================================================
    static void B_SharedComponentSource()
    {
        string src = ReadSrc("CardPresentation/Shell/ShowEventArmiesPanel.cs", "B1");
        if (src != null)
        {
            string line = CodeLineWith(src, "ViewportClip.Hang(sel, \"Viewport\"");
            Tr(line != null, "B1（前提）找得到 `ViewportClip.Hang(sel, \"Viewport\"…)` 那一行");
            if (line != null)
            {
                Tr(line.Contains("p._softness"),
                   "B1 ★ 那一行的 softness 实参是 **`p._softness`**（形参转下来的）｜🧨 改回 `Vector2Int.zero` ⇒ 红");
                Tr(!line.Contains("Vector2Int.zero"),
                   "B1 ★ 那一行**不再**出现 `Vector2Int.zero`（⛔ 不能写常量 —— 原版逐窗不同）");
            }
            C(CountOfCode(src, "Vector2Int? softness = null"), 1,
              "B2 ★ `Build(...)` 的软边形参 `Vector2Int? softness = null` 恰 **1** 处｜🧨 删掉形参（回到写死）⇒ 0 ⇒ 红");
            C(CountOfCode(src, "没传 softness"), 1,
              "B3 ★ 没传时**出声**那一句恰 **1** 处（⛔ 不静默失败）｜🧨 删掉 `Debug.LogWarning` ⇒ 0 ⇒ 红");
        }
    }

    // ==================================================================
    //  C · 调用点：能量窗必须显式传第 5 个实参
    // ==================================================================
    static void C_CallSites()
    {
        string energy = ReadSrc("CardPresentation/Shell/EnergySinglePlayerOnlyEventWindow.cs", "C1");
        if (energy == null) return;

        C(CountOfCode(energy, "ShowEventArmiesPanel.Build("), 1,
          "C1 ★ 能量窗里 `ShowEventArmiesPanel.Build(` 恰 **1** 处（唯一宿主）");
        string line = CodeLineWith(energy, "ShowEventArmiesPanel.Build(");
        Tr(line != null, "C1（前提）找得到那一行");
        if (line == null) return;
        Tr(line.Contains("new Vector2Int(25, 0)"),
           "C2 ★ 那一行带着 `new Vector2Int(25, 0)` 实参（`EnergySinglePlayerOnlyEventWindow.prefab:6898`）｜🧨 删实参 / 改值 ⇒ 红");
        C(line.Split(',').Length >= 5, true,
          "C3 ★ 那一行至少 5 个实参（`parent, qArt, qArt1, qText, softness`；少一个 ⇒ 软边没传）");
    }

    // ==================================================================
    //  D · 另四族（**不走本件**，各自窗内传）—— 值必须还是原版那份
    // ==================================================================
    static void D_OtherWindows()
    {
        C(LiveOpsEventWindow.VpSoft, new Vector2(0f, 52f),
          "D1 ★ `LiveOpsEventWindow.VpSoft` = **(0,52)**（`SkirmishModeEventWindow.prefab:4512` · `RankedEventWindowV2.prefab:4902`）｜🧨 抄成 25 / 42 ⇒ 红");
        C(LeaderboardWindow.ArmyClipSoft, new Vector2(42f, 0f),
          "D2 ★ `LeaderboardWindow.ArmyClipSoft` = **(42,0)**（三扇 `Ranking Display/Content/Army Selector/Viewport` 逐位同值）｜🧨 改成别的 ⇒ 红");
        C(PracticeModePopup.ArmyClipSoft, new Vector2(0f, 50f),
          "D3 ★ `PracticeModePopup.ArmyClipSoft` = **(0,50)**（`Practice Mode Menu ▸ Deck Selector/Army Selector/Viewport`）｜🧨 改成 52 ⇒ 红");

        string campaign = ReadSrc("CardPresentation/Shell/CampaignTab.cs", "D4");
        if (campaign != null)
        {
            string line = CodeLineWith(campaign, "ViewportClip.Hang(sel, \"Viewport\"");
            Tr(line != null, "D4（前提）找得到战役页 `Campaign Army Selector` 那颗 `ViewportClip.Hang(sel, \"Viewport\"…)`");
            if (line != null)
                Tr(line.Contains("Vector2Int.zero"),
                   "D4 ★ 战役页那颗视口仍是 `Vector2Int.zero`（`Campaign Tab.prefab` 的 `m_Softness (0,0)` 硬边）｜🧨 顺手抄成 42/25 ⇒ 红");
        }

        string forge = ReadSrc("CardPresentation/Shell/ForgeTab.cs", "D5");
        if (forge != null)
            C(CountOfCode(forge, "new Vector2Int(42, 0)"), 1,
              "D5 ★ `ForgeTab.SelSoft` = `new Vector2Int(42, 0)` 恰 **1** 处（`Forge Tab ▸ Forge Army Selector/Viewport`）｜🧨 改动 ⇒ 红");
    }

    // ==================================================================
    //  E · 宿主调用点：`Editor/ShellScene.cs` 的自检宿主也必须显式传第 5 实参
    // ==================================================================
    //  ✅ 已复刻（2026-11-04 · B141-W62 · 判据 `EnergySinglePlayerOnlyEventWindow.prefab:6898`）—— 本段是为「灭自证」补的：
    //    把 `ShellScene.cs:11282` 的第 5 实参删掉 ⇒ `Build` 走「没传 softness」出声支 ⇒ E2 当场红。
    static void E_HostCallSite()
    {
        string shell = ReadSrc("CardPresentation/Editor/ShellScene.cs", "E1");
        if (shell == null) return;

        C(CountOfCode(shell, "ShowEventArmiesPanel.Build("), 1,
          "E1 ★ 宿主 `ShellScene.cs` 里 `ShowEventArmiesPanel.Build(` 恰 **1** 处（⑦ 活动阵营图标行的自检宿主）");
        string line = CodeLineWith(shell, "ShowEventArmiesPanel.Build(");
        Tr(line != null, "E1（前提）找得到那一行");
        if (line == null) return;
        Tr(line.Contains("new Vector2Int(25, 0)"),
           "E2 ★ 那一行带着第 5 实参 `new Vector2Int(25, 0)`（`EnergySinglePlayerOnlyEventWindow.prefab:6898`）｜🧨 删掉第 5 实参 ⇒ `Build` 走「没传 softness」出声支 ⇒ 红");
        Tr(!line.Contains("Build(pRoot);"),
           "E2 ★ 那一行**不再是** 4 参调用（不得出现 `Build(pRoot);`）｜🧨 回退成 4 参 ⇒ 红（与上一条不共因：改值只红第一条，回退整句两条一起红）");
    }

    // ==================================================================
    //  读源码（⛔ 只用来看「怎么接的」；期望值一律 prefab 字面量）
    // ==================================================================
    static string ReadSrc(string rel, string tag)
    {
        try
        {
            string p = Path.Combine(Application.dataPath, rel);
            if (!File.Exists(p)) { Tr(false, tag + "0 读得到源码 `" + rel + "`（" + p + "）—— 读不到时源码闸**等于没验** ⇒ 当场红"); return null; }
            return File.ReadAllText(p);
        }
        catch (Exception e)
        {
            Tr(false, tag + "0 读源码 `" + rel + "` 抛异常：" + e.GetType().Name + "：" + e.Message);
            return null;
        }
    }

    /// <summary>数**非注释**片段里 needle 的出现次数（口径同 `B141_W25_Assert.CountOfCode`）。</summary>
    static int CountOfCode(string src, string needle)
    {
        int n = 0, i = 0;
        while (i < src.Length)
        {
            int eol = src.IndexOf('\n', i);
            if (eol < 0) eol = src.Length;
            string line = src.Substring(i, eol - i);
            i = eol + 1;
            int k = line.IndexOf("//", StringComparison.Ordinal);
            string code = k >= 0 ? line.Substring(0, k) : line;
            int from = 0, j;
            while ((j = code.IndexOf(needle, from, StringComparison.Ordinal)) >= 0) { n++; from = j + needle.Length; }
        }
        return n;
    }

    /// <summary>返回**第一条非注释**且包含 needle 的代码行（去掉行尾注释）。</summary>
    static string CodeLineWith(string src, string needle)
    {
        int i = 0;
        while (i < src.Length)
        {
            int eol = src.IndexOf('\n', i);
            if (eol < 0) eol = src.Length;
            string line = src.Substring(i, eol - i);
            i = eol + 1;
            if (line.TrimStart().StartsWith("//", StringComparison.Ordinal)) continue;
            if (line.IndexOf(needle, StringComparison.Ordinal) < 0) continue;
            int k = line.IndexOf("//", StringComparison.Ordinal);
            return k >= 0 ? line.Substring(0, k) : line;
        }
        return null;
    }
}
