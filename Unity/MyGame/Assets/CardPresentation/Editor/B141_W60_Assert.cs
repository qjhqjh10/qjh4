// B141_W60_Assert.cs — 执行写手 `B141-W60`（第一百二十八会话 · 2026-11-04）· W129 **5-17 剩余三处**
//
// ============================ 它盯的是什么 ============================
//  W129 5-17 剩下的三处（`报告/B141_W129_HoverArt.md` §❓1/2/3）：
//    ① `Shell/AchievementsMenu.cs` 的 **5 颗成就页签**：悬停图 = `40K_settings_button_selected`，
//       本批改成**逐颗显式传** `hoverArt: ArtTabHover`（改前靠 `WindowButton.HoverNames` 全局表隐式兜底）。
//    ② `Shell/LeaderboardWindow.cs` 的**三份排行榜 prefab 常态图**：本批**回源复核后零改动**（见下面 §反证）。
//    ③ `Shell/ChatPanel.cs` 的 `TabArtHover`：现读与原版 `m_HighlightedSprite` **逐值相同** ⇒ 零改动，只补标记。
//
//  判据（2026-11-04 逐颗现读，`d:/2/tools/assetripper_out/ExportedProject/Assets/*.prefab` 的
//  `Selectable.m_SpriteState` ＋ `EverguildToggle.onSprite/offSprite/spriteToChange` ＋ `Toggle.m_IsOn`）：
//
//  | prefab ▸ 节点族 | 颗 | `m_IsOn` | 常态（`offSprite`） | 选中（`onSprite`/`m_SelectedSprite`） | 悬停（`m_HighlightedSprite`） |
//  |---|---:|---:|---|---|---|
//  | `Player Profile Window` ▸ `Trophies Tab/buttons/Achievement Type Toggle (0..4)` | 5 | 0 | `40K_settings_button` | `40K_settings_button_hover` | **`40K_settings_button_selected`** |
//  | `RankedSkirmishLeaderboardPopup` ▸ `Tab Buttons/*` | 3 | **1** | `40K_settings_button` | `40K_settings_button_hover` | `40K_settings_button_selected` |
//  | `RankedClassicLeaderboardPopup Variant` ▸ `Tab Buttons/*` | 2 | **1** | 同上 | 同上 | 同上 |
//  | `DraftLeaderboardPopup` ▸ `Tab Buttons/*` | 2 | **1** | 同上 | 同上 | 同上 |
//  | `ChatPanel` ▸ `Holder/Chat/Tab Buttons/Orange Tab Toggle` | 1 | **1** | `40K_settings_button` | `40K_settings_button_hover` | `40K_settings_button_selected` |
//
// ============================ 🧨 ② 的反证（本批**拒做**的那一条）============================
//  `报告/B141_W129_HoverArt.md` §❓3 说「排行榜三份 prefab 的常态图是 `_hover`」—— 那句**只读了
//  `m_TargetGraphic.m_Sprite`**。回源读到更下面两层后它不成立：
//    · 这三份 prefab 的页签都是 `EverguildToggle`，`changeSpriteOnValueChange = 1`、
//      `onSprite = 40K_settings_button_hover` · `offSprite = 40K_settings_button`、
//      `spriteToChange` **就是** `m_TargetGraphic`（同一 pathID）。
//    · `EverguildToggle__Awake.c` 末尾：`onValueChanged.AddListener(ToggleSprite)` **之后立刻**
//      `Image.set_sprite(spriteToChange, isOn ? onSprite : offSprite)`；`__ToggleSprite.c` 同式。
//    · 三份排行榜 prefab 的 **3/3 颗 `m_IsOn = 1`**（作者是在「选中」态存盘的）⇒ 那一格 `_hover`
//      是**存盘快照**，不是常态；同一族的档案窗/成就页 **11/11 颗 `m_IsOn = 0`**，`m_TargetGraphic.m_Sprite`
//      存的就是 `40K_settings_button` —— 同一对 on/off，两处存盘状态不同，正好互相反证。
//  ⇒ **未选中页签运行时画 `40K_settings_button`**（= 我们现读的 `ArtTabOff`）⇒ ⛔ 改成 `_hover` 会把
//  「选中/未选中」画成同一张图（画面回归）。本批**零改动**，细节 → `报告/B141_W129_HoverRest.md` §二。
//
// ============================ 断言（17 条）============================
//  A 组（逐站接线 / 逐值，零前提 —— 只读 3 份 `.cs` 文本）：
//    A1  `AchievementsMenu.ArtTabHover = "40K_settings_button_selected"`；A1b 常态/选中两格没被顺手改（`40K_settings_button` / `_hover`）
//    A2  成就页签 `Hit(...)` **显式**传 `ArtTabHover`（归一化空白后 `_tabBg[i],ArtTab,ArtTabHover,clickCue:`）
//    A2b `ArtTabHover` ≠ `ArtTabOn`（悬停 ≠ 选中）
//    A3  排行榜 `ArtTabOff = "40K_settings_button"`（🧨 反证条：⛔ 不是 `_hover`）；A3b `ArtTabOn`/`ArtTabHover` 两格逐值
//    A4  排行榜画底图那一句仍是 `on ? ArtTabOn : ArtTabOff`（on/off 换图，不是单图）
//    A5  `ChatPanel` 四态常量逐值 = `40K_settings_button` / `_hover` / `_selected` / `_pressed`；A5b `Bind(...)` 五格逐位
//    A6  三份文件的**常态图常量**逐字相同（`40K_settings_button`）—— 挡「只改一家」
//  B 组（运行时探针 + 灭自证，零前提）：
//    B1  三张图 `CardArt.MenuUi` 全取得到
//    B2  `Bind(art = 40K_settings_button)`（不传 hoverArt）⇒ 全局表兜出 `_selected`（**口径条**：改前渲染已对）
//    B3  `Bind(art = 40K_settings_button_hover)` ⇒ 也兜出 `_selected`（W34 新表项；即使有人把常态改成 `_hover`，悬停仍对）
//    B4  显式 `hoverArt = 40K_settings_button_selected` ⇒ 读数口 = `_selected`
//    B5  三张真图**都不**记进 `MissingSwapArt`（红线：不许静默缺图）
//    B6  🧨 **灭自证**：显式 `hoverArt = 40K_settings_button_hover` ⇒ 读数口变 `_hover`（⇒ B2/B4 不是恒真）
//    B7  `Bind` 的五格各归其位（`NormalTexForTest`=常态 · `HoverTexForTest`=悬停 · `SelectedTexForTest`=选中）
//
// ============================ 🧨 什么坏法会让哪条红 ============================
//  · 删掉成就页签 `Hit(...)` 里的 `ArtTabHover,` ⇒ **A2** 红。
//  · 把 `AchievementsMenu.ArtTabHover` 改成 `_hover` ⇒ **A1/A2b** 红（B4/B6 仍绿 —— 那是图库层，不是接线层）。
//  · 把 `LeaderboardWindow.ArtTabOff` 改成 `_hover`（= 照 §❓3 做）⇒ **A3** 红 + **A6** 红。
//  · 把排行榜那句 `on ? ArtTabOn : ArtTabOff` 改成恒 `ArtTabOn` ⇒ **A4** 红。
//  · 把 `ChatPanel.TabArtHover` 改成 `_hover` ⇒ **A5** 红。
//  · 删 `Resources/Art/ui_menu/40K_settings_button_selected.png` ⇒ **B1** 红（并让运行时 `MissingSwapArt` 非空）。
//  · `WindowButton.Bind` 忽略 `hoverArt` 实参 ⇒ **B4/B6** 同时红。
//  · 把 `HoverTexForTest` 改成恒返 `_normalTex` ⇒ **B6/B7** 红。
//
// ============================ 怎么接（本文件不碰宿主聚合）============================
//  `public static void Run();` —— **零前提**（只读 3 份源码 + 3 张图 + 一个用完即销毁的探针节点）。
//  挂法（由调度台决定）：同一物理行追加
//      B141_W60_Assert.Run(); pass += B141_W60_Assert.Pass; fail += B141_W60_Assert.Fail;
//  判绿红：按行首 `B141W60 ✓` / `B141W60 ✗` 数（⛔ 别 `grep -c ✗`）。

using System.IO;
using System.Text.RegularExpressions;
using CardPresentation;
using UnityEngine;

/// <summary>执行写手 `B141_W60`：W129 5-17 剩余三处（成就页签悬停 · 排行榜常态图反证 · 聊天页签四态）的回归断言。</summary>
public static class B141_W60_Assert
{
    const string Tag = "B141W60";
    static int _pass, _fail;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _fail; } }

    /// <summary>三处（成就页签 / 排行榜常态图复核 / 聊天页签复核）—— 其中**只有第 1 处是代码改动**。</summary>
    public const int Sites = 3;
    /// <summary>本批改了代码的站点数（排行榜、聊天的结论 = 现读即对 ⇒ 零改动，见文件头 §反证）。</summary>
    public const int ChangedSites = 1;

    /// <summary>`40K_settings_button` 的悬停图（原版 `m_HighlightedSprite`；成就页 5/5 颗、排行榜 7/7 颗、聊天 1/1 颗同值）。</summary>
    public const string HoverArt = "40K_settings_button_selected";
    /// <summary>常态图（原版 `EverguildToggle.offSprite`）。</summary>
    public const string OffArt = "40K_settings_button";
    /// <summary>选中图（原版 `EverguildToggle.onSprite` / `m_SelectedSprite`）。</summary>
    public const string OnArt = "40K_settings_button_hover";
    /// <summary>按下图（原版 `m_PressedSprite`）。</summary>
    public const string PressedArt = "40K_settings_button_pressed";

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static string Src(string rel)
    {
        try { return File.ReadAllText(Application.dataPath + "/CardPresentation/" + rel); }
        catch (System.Exception e) { Debug.LogError(Tag + " 读不到 `" + rel + "`：" + e.Message); return ""; }
    }

    static bool Has(string s, string needle)
    {
        return !string.IsNullOrEmpty(s) && s.IndexOf(needle, System.StringComparison.Ordinal) >= 0;
    }

    /// <summary>去掉所有空白（空格/制表/换行）—— 跨行实参也能按「连续 token」匹配。</summary>
    static string Squash(string s)
    {
        return string.IsNullOrEmpty(s) ? "" : Regex.Replace(s, @"\s+", "");
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        GroupA_Wiring();
        GroupB_Probes();
        Debug.Log(Tag + " 合计：✓ " + _pass + " · ✗ " + _fail
                  + "（站点 " + Sites + " 个 · 本批改码 " + ChangedSites
                  + " 个 · 悬停图 " + HoverArt + " · 常态图 " + OffArt + "）");
    }

    // ==================================================================
    //  A · 逐站接线 / 逐值（判据 = 四份原版 prefab 的 Selectable + EverguildToggle）
    // ==================================================================
    static void GroupA_Wiring()
    {
        var ach = Squash(Src("Shell/AchievementsMenu.cs"));
        var lb = Squash(Src("Shell/LeaderboardWindow.cs"));
        var chat = Squash(Src("Shell/ChatPanel.cs"));

        // ---- A1/A1b/A2/A2b · AchievementsMenu（原版 `Player Profile Window` ▸ Trophies 5 颗）----
        Check(ach.Length > 0, "A0 前提：`Core/../Shell/AchievementsMenu.cs` 读得到（长度 " + ach.Length + "）");
        Check(Has(ach, "ArtTabHover=\"" + HoverArt + "\""),
              "A1 成就页签**悬停**图 = `" + HoverArt + "`（= 原版 5/5 颗 `m_HighlightedSprite`）");
        Check(Has(ach, "ArtTab=\"" + OffArt + "\"") && Has(ach, "ArtTabOn=\"" + OnArt + "\""),
              "A1b 成就页签**常态**仍是 `" + OffArt + "` · **选中**仍是 `" + OnArt + "`（= 原版 `offSprite`/`onSprite`，三态不混）");
        Check(Has(ach, "_tabBg[i],ArtTab,ArtTabHover,clickCue:WindowSounds.SelectToggleCue"),
              "A2 成就 5 颗页签的 `Hit(...)` **显式**传 `ArtTabHover`（不再靠 `HoverNames` 全局表隐式兜底）");
        Check(!Has(ach, "ArtTabHover=\"" + OnArt + "\""),
              "A2b `ArtTabHover` ≠ `ArtTabOn`（挡「把悬停写成选中」）");
        Check(HoverArt != OnArt, "A2c 常量层：悬停图 `" + HoverArt + "` ≠ 选中图 `" + OnArt + "`");

        // ---- A3/A3b/A4 · LeaderboardWindow（🧨 本批「拒做」那一条的正向守卫）----
        Check(lb.Length > 0, "A3 前提：`Shell/LeaderboardWindow.cs` 读得到（长度 " + lb.Length + "）");
        Check(Has(lb, "ArtTabOff=\"" + OffArt + "\""),
              "A3 🧨 排行榜**常态**图 = `" + OffArt + "`（= 原版 `EverguildToggle.offSprite`；"
              + "⛔ **不是** `m_TargetGraphic.m_Sprite` 里那颗 `_hover` —— 那只是 3/3 颗 `m_IsOn=1` 的存盘快照）");
        Check(Has(lb, "ArtTabOn=\"" + OnArt + "\"") && Has(lb, "ArtTabHover=\"" + HoverArt + "\""),
              "A3b 排行榜**选中**图 = `" + OnArt + "` · **悬停**图 = `" + HoverArt + "`（三份 prefab 7/7 颗）");
        Check(Has(lb, "Rect(node,on?ArtTabOn:ArtTabOff,r,\"button_bg\",QBg,TabBgTint)"),
              "A4 排行榜画底图仍是 `on ? ArtTabOn : ArtTabOff`（选中/未选中**换图**，不是单图）");

        // ---- A5/A5b · ChatPanel（原版 `ChatPanel` ▸ `Orange Tab Toggle` 1 颗，两个频道共用）----
        Check(chat.Length > 0, "A5 前提：`Shell/ChatPanel.cs` 读得到（长度 " + chat.Length + "）");
        Check(Has(chat, "TabArtOff=\"" + OffArt + "\"") && Has(chat, "TabArtOn=\"" + OnArt + "\"")
              && Has(chat, "TabArtHover=\"" + HoverArt + "\"") && Has(chat, "TabArtPressed=\"" + PressedArt + "\""),
              "A5 聊天页签四态 = `" + OffArt + "` / `" + OnArt + "` / `" + HoverArt + "` / `" + PressedArt
              + "`（= 原版 off/on/highlight/pressed，逐值相同 ⇒ 本批零改动）");
        Check(Has(chat, "Bind(_tabBg[idx],TabArtOff,TabArtHover,TabArtPressed,TabArtOn)"),
              "A5b 聊天页签 `Bind(...)` 五格逐位（悬停 = 第 3 格 `TabArtHover` · 选中 = 第 5 格 `TabArtOn`）");

        // ---- A6 · 三份文件同一族的**常态图**逐字相同（挡「只改一家」）----
        Check(Has(ach, "ArtTab=\"" + OffArt + "\"") && Has(lb, "ArtTabOff=\"" + OffArt + "\"")
              && Has(chat, "TabArtOff=\"" + OffArt + "\""),
              "A6 三份文件的**常态图**全是 `" + OffArt + "`（同一族 `EverguildToggle.offSprite`，⛔ 不许只改一家）");
    }

    // ==================================================================
    //  B · 运行时探针 / 灭自证 / 口径
    // ==================================================================
    static void GroupB_Probes()
    {
        var texOff = CardArt.MenuUi(OffArt);
        var texOn = CardArt.MenuUi(OnArt);
        var texHover = CardArt.MenuUi(HoverArt);
        Check(texOff != null && texOn != null && texHover != null,
              "B1 三张图都取得到（`" + OffArt + "` / `" + OnArt + "` / `" + HoverArt + "`）——"
              + " 取不到就会落进 `MissingSwapArt`");

        var root = new GameObject("B141W60_probe");
        try
        {
            var q = ImageQuad.Create(root.transform, texOff, Vector3.zero, 1f, new Vector2(0.5f, 0.5f), "q");
            Check(q != null, "B1b 前提：探针 `ImageQuad` 建出来了（`" + OffArt + "` 非空）");
            if (q == null) return;

            var w = root.AddComponent<WindowButton>();
            Check(w != null, "B1c 前提：探针 `WindowButton` 挂上了");
            if (w == null) return;
            int m0 = WindowButton.MissingSwapArt.Count;

            // ---- B2 · 口径：不传 hoverArt 时全局表兜出 `_selected`（成就页改前就是这样渲染的）----
            w.Bind(q, OffArt);
            Check(w.HoverTexForTest != null && w.HoverTexForTest.name == HoverArt,
                  "B2 `Bind(art: " + OffArt + ")`（不传 hoverArt）⇒ `HoverNames` 表兜出 `" + HoverArt
                  + "`｜实得 " + (w.HoverTexForTest != null ? w.HoverTexForTest.name : "<null>"));

            // ---- B3 · 排行榜那颗「常态图 = _hover」的假设若成真，悬停仍靠 W34 新表项 ----
            w.Bind(q, OnArt);
            Check(w.HoverTexForTest != null && w.HoverTexForTest.name == HoverArt,
                  "B3 `Bind(art: " + OnArt + ")` ⇒ 仍兜出 `" + HoverArt + "`（W34 为「把 hover 图当常态图」的 13 颗补的表项）");

            // ---- B4 · 显式传悬停图 ----
            w.Bind(q, OffArt, HoverArt);
            Check(w.HoverTexForTest != null && w.HoverTexForTest.name == HoverArt,
                  "B4 `Bind(art: " + OffArt + ", hoverArt: " + HoverArt + ")` ⇒ 读数口 = `" + HoverArt + "`");

            // ---- B5 · 真图不记缺图 ----
            Check(WindowButton.MissingSwapArt.Count == m0,
                  "B5 三张真图 ⇒ **不**记进 `MissingSwapArt`｜实得 " + (WindowButton.MissingSwapArt.Count - m0) + " 条");
            Check(!WindowButton.MissingSwapArt.Contains(OffArt + " → " + HoverArt),
                  "B5b `MissingSwapArt` 里没有 `" + OffArt + " → " + HoverArt + "`（成就页/排行榜/聊天共用这一跳）");

            // ---- B6 · 🧨 灭自证：读数口能区分悬停图与选中图 ----
            w.Bind(q, OffArt, OnArt);
            Check(w.HoverTexForTest != null && w.HoverTexForTest.name == OnArt,
                  "B6 🧨 同一颗探针改绑 `hoverArt: " + OnArt + "` ⇒ 读数口**看得见**变化（⇒ B2/B4 不是恒真断言）｜实得 "
                  + (w.HoverTexForTest != null ? w.HoverTexForTest.name : "<null>"));

            // ---- B7 · 五格各归其位（常态 / 悬停 / 选中**不是同一个口**）----
            w.Bind(q, OffArt, HoverArt, PressedArt, OnArt);
            Check(w.NormalTexForTest != null && w.NormalTexForTest.name == OffArt
                  && w.HoverTexForTest != null && w.HoverTexForTest.name == HoverArt
                  && w.SelectedTexForTest != null && w.SelectedTexForTest.name == OnArt,
                  "B7 三格各归其位：常态 `" + (w.NormalTexForTest != null ? w.NormalTexForTest.name : "<null>")
                  + "` · 悬停 `" + (w.HoverTexForTest != null ? w.HoverTexForTest.name : "<null>")
                  + "` · 选中 `" + (w.SelectedTexForTest != null ? w.SelectedTexForTest.name : "<null>") + "`");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
