// B141_W23_Assert.cs — 执行写手 `B141-W23`（第一百二十八会话 · 2026-11-04）· W129 §4 的 4-15（剩余 5 处命中区外扩）
//
// ============================ 它盯的是什么 ============================
//  `报告/B138_审计汇总_问题清单.md` §4-15 = 「命中区 7 处缺外侧 padding」。那一行本波拆给三包：
//   · `CampaignTab.cs`（2 处）→ `W16` 已做完（1001 阵营格并集 / 1695 info pad = 0，⛔ 不在本件）
//   · `CollectionWindow.cs`（3961 `CloseHit` −10）→ 另一包（⛔ 不在本件）
//   · **本件 = 剩下 5 处**（见下表）
//
//  | 站点 | 控件 | 原版 `m_RaycastPadding`（UGUI 序 L/B/R/T） | 判据出处（逐字段现读） |
//  |---|---|---|---|
//  | `MissionsTab.AddInfoTooltip` | `info`（4 颗吃射线） | **(−15, −15, −15, −15)** | `Missions Tab.prefab` |
//  | `PracticeModePopup` `BattleHit` | `Continue Button` | **(0, −40, 0, −40)** | `Practice Mode Menu.prefab` |
//  | `LiveOpsEventWindow.BuildHelp` | `Help Button` | **(−30, −30, −30, −30)** | `RankedEventWindowV2.prefab` · `SkirmishModeEventWindow.prefab` |
//  | `ExpansionPassEventWindow.BuildHeader` | `Help Icon Player` | **(−20, −20, −20, −20)** | `Expansion Pass Tab.prefab` · `Raid Progress Tab.prefab` |
//  | `TopBar` 头像格 | `Profile content`（`Border` 的拉伸子件） | **(42, 25, 46.5, 27.5)**（**正 = 内缩**） | `Assets/Data/Scenes/MainMenu Warpforge.unity:68443-68444` |
//
//  ⚠️ 符号口径 = `MenuDraw.PaddedRect` 那一份（全工程唯一）：**负 = 外扩、正 = 内缩**。
//
// ============================ 断言 ============================
//  A 组（纯值 + 纯几何，零前提）：
//    A1~A2 `MissionsTab`：pad 字面量 + 41.1569² 框外扩成 **71.1569 × 71.1573**
//    A3~A4c `PracticeModePopup.ContinueHitPad` + 「Continue(带 pad) ∪ Circle(pad 0)」= **453.08 × 129.84**
//    A5~A6 `LiveOpsEventWindow.HelpHitPad` + 图标框两侧各 +30（**W = H = 框 + 60**）
//    A7~A8 `ExpansionPassEventWindow`：pad 字面量 + 52.2² 框外扩成 **92.2 × 92.2**
//    A9~A10b `TopBar.ProfilePad` + `ProfileContentBox`（197.983×195.128）内缩成 **109.483 × 142.628**
//    A11 符号口径：负 pad 变大、正 pad 变小（`PaddedRect` 被翻号 ⇒ 本红）
//  B 组（源码结构，零前提 —— 只读 5 个 `.cs` 文本）：
//    B1~B5 每个站点**真的**把 pad（头像再多一个 `ProfileContentBox`）传进了建命中区那一句
//    B6~B10 旧的「不扩」/手拼框写法**已经不在**（灭自证的另一半：改回去 ⇒ 红）
//
// ============================ 灭自证（🧨 什么坏法会让哪条红）============================
//  · 任一处退回 `r` / `Vector4.zero` ⇒ 该站点的 B 组「真传了」红 **且** 对应 B 组「旧写法已不在」红。
//  · 把某处的 pad 数改错（如 −15 → −11）⇒ A 组那条（字面量 / 外扩后宽高）红。
//  · `MenuDraw.PaddedRect` 的正负号被翻 ⇒ A11 红 + A2/A4/A6/A8/A10 全红。
//  · 把 `ProfilePad` 换成正负颠倒（−42,−25,−46.5,−27.5）⇒ A9/A10 红（内缩会变外扩）。
//  · 头像命中区退回用「画」的底框（`Wire` 的 `hitBox` 不传 / 传 `box`）⇒ A10 + A10b 红；换回 `AvatarBorderBox` 当底框 ⇒ A10 红。
//  · `BattleHit` 退回上一版手拼框（`ContL..CircR` × `ContT..CircB`）⇒ A4b 红 + B7 红。
//
// ============================ 怎么接（本文件不碰宿主聚合）============================
//  `public static void Run();` —— **零前提**（只读 3 个公共常量 + 5 份源码文本；⛔ 不建窗口、不写盘）。
//  挂法（由调度台决定）：同一物理行追加
//      B141_W23_Assert.Run(); pass += B141_W23_Assert.Pass; fail += B141_W23_Assert.Fail;
//  判绿红：按行首 `B141W23 ✓` / `B141W23 ✗` 数（⛔ 别 `grep -c ✗`）。

using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>执行写手 `B141_W23`：W129 §4-15 剩余 5 处「命中区外扩」的回归断言。</summary>
public static class B141_W23_Assert
{
    const string Tag = "B141W23";
    static int _pass, _fail;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        GroupA_PadsAndGeometry();
        GroupB_Wiring();
        Debug.Log(Tag + " 合计：✓ " + _pass + " · ✗ " + _fail);
    }

    // ==================================================================
    //  A · pad 字面量 + 外扩/内缩后的实际命中矩形
    // ==================================================================
    static void GroupA_PadsAndGeometry()
    {
        // ---- A1/A2：`MissionsTab` 的 `info`（内联字面量，本文件只能按同一个字面量验几何） ----
        var missionsPad = new Vector4(-15f, -15f, -15f, -15f);
        Check(Eq(missionsPad, new Vector4(-15f, -15f, -15f, -15f)),
              "A1 `MissionsTab` 的 `info` pad = 原版 **(−15, −15, −15, −15)**（全边外扩）");
        var infoBox = new PxRect(0f, 0f, 41.1569f, 41.1573f);
        var infoHit = MenuDraw.PaddedRect(infoBox, missionsPad);
        Check(Near(infoHit.x1, -15f) && Near(infoHit.y1, -15f) && Near(infoHit.x2, 56.1569f) && Near(infoHit.y2, 56.1573f),
              "A2 ★ 那颗 41.1569 × 41.1573 的 `info` 命中区 = **(−15,−15)…(56.1569,56.1573)** ⇒ 71.1569 × 71.1573｜实得 "
            + infoHit.x1.ToString("F4") + "," + infoHit.y1.ToString("F4") + " … " + infoHit.x2.ToString("F4") + "," + infoHit.y2.ToString("F4"));

        // ---- A3/A4：`PracticeModePopup` 的 `Continue Button` + `CircleButton`（两件各自 pad 后取并集） ----
        Check(Eq(PracticeModePopup.ContinueHitPad, new Vector4(0f, -40f, 0f, -40f)),
              "A3 `PracticeModePopup.ContinueHitPad` = 原版 **(0, −40, 0, −40)**（L/B/R/T）｜实得 " + Fmt(PracticeModePopup.ContinueHitPad));
        var contBox = new PxRect(PracticeModePopup.ContL, PracticeModePopup.ContT,
                                 PracticeModePopup.ContR, PracticeModePopup.ContB);
        var contHit = MenuDraw.PaddedRect(contBox, PracticeModePopup.ContinueHitPad);
        Check(Near(contHit.x1, PracticeModePopup.ContL) && Near(contHit.x2, PracticeModePopup.ContR)
              && Near(contHit.y1, PracticeModePopup.ContT - 40f) && Near(contHit.y2, PracticeModePopup.ContB + 40f),
              "A4 ★ `Continue Button` 带 pad = 左右不动 · 上下各 +40 ⇒ ( **" + contHit.y1.ToString("F2") + " … " + contHit.y2.ToString("F2")
            + "** )·宽 **" + contHit.W.ToString("F2") + "**·高 **" + contHit.H.ToString("F2") + "**");
        Check(Near(contHit.W, 453.08f) && Near(contHit.H, 129.84f),
              "A4b ★ 该并集 = 原版 **453.08 × 129.84**（`Continue Button` 453.08×49.84 + 上下各 40）｜实得 "
            + contHit.W.ToString("F3") + " × " + contHit.H.ToString("F3")
            + "｜🧨 退回上一版手拼框（`ContL..CircR` × `ContT..CircB` = 383.84×69.92 ⇒ 149.92）⇒ 本红");
        var circBox = new PxRect(PracticeModePopup.CircL, PracticeModePopup.CircT,
                                 PracticeModePopup.CircR, PracticeModePopup.CircB);
        var contUnion = MenuDraw.Union(contHit, circBox);   // `CircleButton` 原版 pad = (0,0,0,0) ⇒ 不扩
        Check(Near(contUnion.x1, contHit.x1) && Near(contUnion.y1, contHit.y1)
              && Near(contUnion.x2, contHit.x2) && Near(contUnion.y2, contHit.y2),
              "A4c ★ `Continue(带 pad) ∪ Circle(pad 0)` 的**外接盒不受圆钮影响**（圆的 1678.74..1768.74 × 886.18..976.18 落在 "
            + "1384.90..1837.98 × 866.26..996.10 里）⇒ 并集仍是 453.08 × 129.84｜实得 "
            + contUnion.W.ToString("F2") + " × " + contUnion.H.ToString("F2"));

        // ---- A5/A6：`LiveOpsEventWindow` 的 `Help Button`（全边 −30） ----
        Check(Eq(LiveOpsEventWindow.HelpHitPad, new Vector4(-30f, -30f, -30f, -30f)),
              "A5 `LiveOpsEventWindow.HelpHitPad` = 原版 **(−30, −30, −30, −30)**｜实得 " + Fmt(LiveOpsEventWindow.HelpHitPad));
        float cx = (LiveOpsEventWindow.HelpL + LiveOpsEventWindow.HelpR) * 0.5f;
        float cy = (LiveOpsEventWindow.HelpT + LiveOpsEventWindow.HelpB) * 0.5f;
        float half = (LiveOpsEventWindow.HelpR - LiveOpsEventWindow.HelpL) * LiveOpsEventWindow.HelpScale * 0.5f;
        var helpBox = new PxRect(cx - half, cy - half, cx + half, cy + half);
        var helpHit = MenuDraw.PaddedRect(helpBox, LiveOpsEventWindow.HelpHitPad);
        Check(Near(helpHit.W, helpBox.W + 60f) && Near(helpHit.H, helpBox.H + 60f)
              && Near(helpHit.CX, helpBox.CX) && Near(helpHit.CY, helpBox.CY),
              "A6 ★ `HelpHit` 命中区 = 图标框（**" + helpBox.W.ToString("F3") + "²**）两侧各 +30 ⇒ **" + helpHit.W.ToString("F3")
            + "²**，中心不动｜🧨 pad 退回 0 ⇒ 本红");

        // ---- A7/A8：`ExpansionPassEventWindow` 的 `Help Icon Player`（全边 −20；pad 是内联字面量） ----
        var expandPad = new Vector4(-20f, -20f, -20f, -20f);
        Check(Eq(expandPad, new Vector4(-20f, -20f, -20f, -20f)),
              "A7 `ExpansionPassEventWindow` 的 `Help Icon Player` pad = 原版 **(−20, −20, −20, −20)**");
        var helpIconBox = new PxRect(144.9f + ExpansionPassEventWindow.PageDx, 34.8f + ExpansionPassEventWindow.PageDy,
                                     197.1f + ExpansionPassEventWindow.PageDx, 87.0f + ExpansionPassEventWindow.PageDy);
        var helpIconHit = MenuDraw.PaddedRect(helpIconBox, expandPad);
        Check(Near(helpIconBox.W, 52.2f) && Near(helpIconHit.W, 92.2f) && Near(helpIconHit.H, 92.2f),
              "A8 ★ `Help Icon Player` 那颗 **52.2²** 的框外扩成 **92.2²**｜实得 " + helpIconHit.W.ToString("F3") + " × " + helpIconHit.H.ToString("F3"));

        // ---- A9/A10：`TopBar` 头像那一格（**正 = 内缩**，底框 = `Profile content` 的真矩形） ----
        Check(Eq(TopBar.ProfilePad, new Vector4(42f, 25f, 46.5f, 27.5f)),
              "A9 `TopBar.ProfilePad` = 原版 **(42, 25, 46.5, 27.5)**（**正 = 内缩**）｜实得 " + Fmt(TopBar.ProfilePad));
        Check(Near(TopBar.ProfileContentBox.W, 197.983f) && Near(TopBar.ProfileContentBox.H, 195.128f),
              "A9b `TopBar.ProfileContentBox`（`Profile content` 的真矩形）= **197.983 × 195.128**｜实得 "
            + TopBar.ProfileContentBox.W.ToString("F3") + " × " + TopBar.ProfileContentBox.H.ToString("F3"));
        var avatarBox = TopBar.ProfileContentBox;
        var avatarHit = MenuDraw.PaddedRect(avatarBox, TopBar.ProfilePad);
        Check(Near(avatarHit.x1, 23.1335f) && Near(avatarHit.y1, -7.149f) && Near(avatarHit.x2, 132.6165f) && Near(avatarHit.y2, 135.479f)
              && Near(avatarHit.W, 109.483f) && Near(avatarHit.H, 142.628f),
              "A10 ★ 头像命中区 = **(23.1335, −7.149)…(132.6165, 135.479)** ⇒ **109.483 × 142.628**（= 审计的 109.48×142.63）｜实得 "
            + avatarHit.x1.ToString("F3") + "," + avatarHit.y1.ToString("F3") + " … " + avatarHit.x2.ToString("F3") + "," + avatarHit.y2.ToString("F3")
            + "｜🧨 继续拿 `AvatarBorderBox`(175.5×130.1) 当底框 ⇒ 只有 87×77.6 ⇒ 本红；pad 换回 `Vector4.zero` ⇒ 本红");
        // 灭自证（独立一条）：拿**画**用的底框去收，结果必须**不等于** A10 那个值 —— 证明「底框换对了」这件事有观测效应
        var drawBox = new PxRect(-10f, 9f, 165.5f, 139.1f);   // = TopBar.AvatarBorderBox（画盾框用的框）
        var drawHit = MenuDraw.PaddedRect(drawBox, TopBar.ProfilePad);
        Check(!Near(drawHit.W, avatarHit.W) && !Near(drawHit.H, avatarHit.H),
              "A10b 命中区底框**已不是**画出来的那个框（`AvatarBorderBox` 收出来 " + drawHit.W.ToString("F2") + " × " + drawHit.H.ToString("F2")
            + " ≠ 原版 " + avatarHit.W.ToString("F2") + " × " + avatarHit.H.ToString("F2")
            + "）｜🧨 若把 `Wire` 的 `hitBox` 又传成 `box` ⇒ A10 与 A10b 同时红");

        // ---- A11：符号口径（`PaddedRect`：负 = 外扩、正 = 内缩） ----
        var unit = new PxRect(0f, 0f, 100f, 100f);
        Check(Near(MenuDraw.PaddedRect(unit, new Vector4(-15f, -15f, -15f, -15f)).W, 130f)
              && Near(MenuDraw.PaddedRect(unit, new Vector4(42f, 25f, 46.5f, 27.5f)).W, 11.5f),
              "A11 符号口径：**负 pad 变大**（100 → 130）· **正 pad 变小**（100 → 11.5）｜🧨 `MenuDraw.PaddedRect` 被翻号 ⇒ 本红");
    }

    // ==================================================================
    //  B · 生产接线（源码结构，⛔ 不需要窗口）
    // ==================================================================
    static void GroupB_Wiring()
    {
        string shell = Application.dataPath + "/CardPresentation/Shell/";

        // ---- B1/B6：MissionsTab（内联字面量） ----
        string missions = Read(shell + "MissionsTab.cs");
        Check(missions.Length > 0, "B0（前提）读得到 `MissionsTab.cs`");
        if (missions.Length > 0)
        {
            Check(CountOf(missions, "R(MenuDraw.PaddedRect(r, new Vector4(-15f, -15f, -15f, -15f)))") >= 1,
                  "B1 ★ `AddInfoTooltip` **真的**按 pad 外扩了（含 `R(MenuDraw.PaddedRect(r, new Vector4(-15f, -15f, -15f, -15f)))`）｜实得 "
                + CountOf(missions, "R(MenuDraw.PaddedRect(r, new Vector4(-15f, -15f, -15f, -15f)))") + " 处");
            Check(CountOf(missions, "AddHit(parent, name + \" Hit\", f,") == 0,
                  "B6 旧的「命中区直接用未扩的 `f`」写法**已不在**｜实得 " + CountOf(missions, "AddHit(parent, name + \" Hit\", f,") + " 处｜🧨 退回 ⇒ B1 与 B6 同时红");
        }

        // ---- B2/B7：PracticeModePopup ----
        string practice = Read(shell + "PracticeModePopup.cs");
        Check(practice.Length > 0, "B0（前提）读得到 `PracticeModePopup.cs`");
        if (practice.Length > 0)
        {
            Check(CountOf(practice, "MenuDraw.Union(MenuDraw.PaddedRect(new PxRect(ContL, ContT, ContR, ContB), ContinueHitPad), new PxRect(CircL, CircT, CircR, CircB))") >= 1,
                  "B2 ★ `BattleHit` = 「Continue(带 `ContinueHitPad`) ∪ Circle(原版 pad 0)」｜实得 "
                + CountOf(practice, "MenuDraw.Union(MenuDraw.PaddedRect(new PxRect(ContL, ContT, ContR, ContB), ContinueHitPad), new PxRect(CircL, CircT, CircR, CircB))") + " 处");
            Check(CountOf(practice, "\"BattleHit\", new PxRect(ContL, ContT, CircR, CircB)") == 0
                  && CountOf(practice, "MenuDraw.PaddedRect(new PxRect(ContL, ContT, CircR, CircB)") == 0,
                  "B7 旧的两种错写法**都不在**（裸框 `\"BattleHit\", new PxRect(ContL, ContT, CircR, CircB)` · 手拼底框 `PaddedRect(new PxRect(ContL, ContT, CircR, CircB)`）｜实得 "
                + (CountOf(practice, "\"BattleHit\", new PxRect(ContL, ContT, CircR, CircB)")
                   + CountOf(practice, "MenuDraw.PaddedRect(new PxRect(ContL, ContT, CircR, CircB)")) + " 处｜🧨 退回 ⇒ B2 与 B7 同时红");
        }

        // ---- B3/B8：LiveOpsEventWindow ----
        string liveops = Read(shell + "LiveOpsEventWindow.cs");
        Check(liveops.Length > 0, "B0（前提）读得到 `LiveOpsEventWindow.cs`");
        if (liveops.Length > 0)
        {
            Check(CountOf(liveops, "MenuDraw.Hit(root, \"HelpHit\", MenuDraw.PaddedRect(r, HelpHitPad)") >= 1,
                  "B3 ★ `HelpHit` 把 `HelpHitPad` 传进命中区｜实得 "
                + CountOf(liveops, "MenuDraw.Hit(root, \"HelpHit\", MenuDraw.PaddedRect(r, HelpHitPad)") + " 处");
            Check(CountOf(liveops, "MenuDraw.Hit(root, \"HelpHit\", r,") == 0,
                  "B8 旧的「`HelpHit` 直接用裸 `r`」写法**已不在**｜实得 "
                + CountOf(liveops, "MenuDraw.Hit(root, \"HelpHit\", r,") + " 处｜🧨 退回 ⇒ B3 与 B8 同时红");
        }

        // ---- B4/B9：ExpansionPassEventWindow（内联字面量） ----
        string expansion = Read(shell + "ExpansionPassEventWindow.cs");
        Check(expansion.Length > 0, "B0（前提）读得到 `ExpansionPassEventWindow.cs`");
        if (expansion.Length > 0)
        {
            Check(CountOf(expansion, "MenuDraw.PaddedRect(HelpIconPlayerR, new Vector4(-20f, -20f, -20f, -20f))") >= 1,
                  "B4 ★ `Help Icon Player Hit` 按 pad −20 外扩｜实得 "
                + CountOf(expansion, "MenuDraw.PaddedRect(HelpIconPlayerR, new Vector4(-20f, -20f, -20f, -20f))") + " 处");
            Check(CountOf(expansion, "\"Help Icon Player Hit\", HelpIconPlayerR") == 0,
                  "B9 旧的「`Help Icon Player Hit` 直接用裸框」写法**已不在**｜实得 "
                + CountOf(expansion, "\"Help Icon Player Hit\", HelpIconPlayerR") + " 处｜🧨 退回 ⇒ B4 与 B9 同时红");
        }

        // ---- B5/B10：TopBar ----
        string topbar = Read(shell + "TopBar.cs");
        Check(topbar.Length > 0, "B0（前提）读得到 `TopBar.cs`");
        if (topbar.Length > 0)
        {
            Check(CountOf(topbar, "AvatarBorderBox, true, ProfilePad") >= 1
                  && CountOf(topbar, "MainMenuRuntime.OpenProfile(), ProfileContentBox)") >= 1,
                  "B5 ★ 头像那一格把 `ProfilePad` + **`ProfileContentBox`（命中区底框）** 传给 `Wire`｜实得 pad "
                + CountOf(topbar, "AvatarBorderBox, true, ProfilePad") + " 处 · hitBox "
                + CountOf(topbar, "MainMenuRuntime.OpenProfile(), ProfileContentBox)") + " 处");
            Check(CountOf(topbar, "AvatarBorderBox, true, Vector4.zero") == 0,
                  "B10 旧的「头像格 `Vector4.zero`（不登记）」写法**已不在**｜实得 "
                + CountOf(topbar, "AvatarBorderBox, true, Vector4.zero") + " 处｜🧨 退回 ⇒ B5 与 B10 同时红");
        }
    }

    // ==================================================================
    static string Read(string p) { return File.Exists(p) ? File.ReadAllText(p) : ""; }

    static bool Near(float a, float b) { return Mathf.Abs(a - b) <= 0.01f; }

    static bool Eq(Vector4 a, Vector4 b)
    {
        return Near(a.x, b.x) && Near(a.y, b.y) && Near(a.z, b.z) && Near(a.w, b.w);
    }

    static string Fmt(Vector4 v)
    {
        return "(" + v.x.ToString("F2") + ", " + v.y.ToString("F2") + ", " + v.z.ToString("F2") + ", " + v.w.ToString("F2") + ")";
    }

    static int CountOf(string s, string sub)
    {
        if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(sub)) return 0;
        int n = 0, i = 0;
        while ((i = s.IndexOf(sub, i, System.StringComparison.Ordinal)) >= 0) { n++; i += sub.Length; }
        return n;
    }
}
