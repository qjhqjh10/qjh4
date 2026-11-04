// B109_E12_Wiring_Assert.cs — 执行代理 `B109_E12`（2026-10-31 · 第一百零九会话）
//
// 「图导进来了但还没接线（导图 ≠ 画出来）」这一族的**独立复核断言**，三组：
//   ① `Shell/DraftModeWindow.cs`   结算页三处：背景 / 胜场格（空槽 + 亮格）/ 败场格（底 + 亮格）
//   ② `Shell/ExpansionPassEventWindow.cs` 的 `Premium Glow`
//   ③ `Shell/SettingsWindow.cs`    社交图标 5 颗 + 支持页外链钮图标 1 组
//
// ============================ 期望值从哪来（⛔ 灭自证）============================
//   下表每一格都是**字面量**，来源 = **原版 prefab 的 `Image.m_Sprite.m_Name` / `Image.m_Color` / 贴图 `m_Rect`**，
//   由 `工具/menu_dump.py` 现读（命令见下）。**⛔ 不从 `DraftModeWindow.ArtBg` / `SettingsWindow.AcSocArt`
//   这些【我们自己的常量】反算** —— 否则有人把常量改成一个「存在、但画错」的图名时本件照样绿（拿自己的实现证明自己）。
//
//   | 位置                         | 原版 sprite (`m_Name`)               | `m_Color`            | `m_Rect`  |
//   |------------------------------|--------------------------------------|----------------------|-----------|
//   | `Background Back/Background` | `Victories_Background_V2`            | 白                   | 915×392   |
//   | `Stage Info UI_ref`（空槽）   | `Rank Skull Empty`                   | (1,0.561,0.561,1)    | 128×128   |
//   | `Stage Info UI_ref/Completed`| `Rank Skull`                         | 白                   | 128×128   |
//   | `Losses Info UI_ref`（底）    | `Victory_round_back_with light`      | (1,1,1,0.639)        | 256×256   |
//   | `Losses Info UI_ref/Fail`    | `40k_DraftMode_icon_cross`           | 白                   | 128×128   |
//   | `Premium Glow`               | `40k_Rewards_icon_base_card SDF`     | 白                   | 128×128   |
//   | 五颗社交钮                   | `Discord-Logo-Color` / `Instagram_icon` / `fb-icon` / `Twitter_Social_Icon_Rounded_Square_Color` / `YouTube_full-color_icon_(2017).svg` | 白 · **PA=1** | 256×256 / 300×300 / 256×256 / 256×256 / 300×300 |
//   | 支持页 `External Link Icon`  | `Button_External_Link`               | 白 · **PA=1**        | 85×85     |
//
//   ⚠️ 落盘名 = 原版切片名把**空格换成下划线**（`CardArt.MenuUi` **不做**这个归一，规矩是调用方换）；
//      带 `.svg` 的那两个**原样保留**（`YouTube_full-color_icon_(2017).svg`）。
//   ⚠️ `Victory_round_v2` / `Victory_skull` **不在本件任何期望里** —— 前者只出现在**别的** `Stage Info UI_ref` 变体
//      （父链不含本窗的 `Draft Mode Ongoing State`），后者原版 0 处引用（判据 → `报告/B101_R3_资源接线施工单.md` §A1）。
//
//   复跑判据（只读）：
//     python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all --rt 7401000208585950046 --depth 3 --relative
//     python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all --rt -4684004561887200571 --depth 2 --relative
//     python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all --rt 7016317461013822149  --depth 2 --relative
//     python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Social Media Links" --depth 1 --relative
//     python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Premium Glow"       --depth 0 --relative
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//   `B109_E12_Wiring_Assert.Run(); pass += B109_E12_Wiring_Assert.Pass; fail += B109_E12_Wiring_Assert.Fail;`
//   接在 `Editor/BattleScene.cs` 那条「合计」聚合行**之前**、**同一物理行追加**（⇒ 行数零变化）。
//   🔴 追加前先看清那一行第一个 `//` 在哪：落在它**之后** = 代码进注释，**这条断言一次都不会跑**（`typecheck`/账闸
//      全绿、退出码 0，而一条都没执行）。
//
// ============================ 边界 ============================
//   · 只读生产代码的数据（结点树 / `ImageQuad.Texture` / `ImageQuad.Tint`），**不写任何游戏状态**；
//   · 建出来的窗在 `finally` 里 `DestroyImmediate` + 还原子检静态口；
//   · ② 那一组要 `Build()` 整棵战令页（它不像另两扇有一份现成的 `Create+Open` 自检夹具）——
//     这一组**段级 catch**，抛异常算红、⛔ 不当跳过。
using System;
using CardPresentation;
using UnityEngine;

public static class B109_E12_Wiring_Assert
{
    const string P = "[B109_E12] ";

    public static int Pass { get; private set; }
    public static int Fail { get; private set; }

    static bool Ok(bool cond, string msg)
    {
        if (cond) { Pass++; Debug.Log(P + "   ✓ " + msg); return true; }
        Fail++; Debug.LogError(P + "   ✗ " + msg); return false;
    }

    /// <summary>唯一入口。三组**各自 try/catch**（一组抛异常不许把后面的组吞掉 —— 同 `B106_W10` 那条口径）。</summary>
    public static void Run()
    {
        Pass = 0; Fail = 0;
        try
        {
            try { RunDraft(); }          catch (Exception e) { Ok(false, "① Draft 结算页 段异常（算红，⛔ 不是跳过；其后 2 组没跑）: " + e); }
            try { RunExpansionPass(); }  catch (Exception e) { Ok(false, "② 战令 Premium Glow 段异常（算红；其后 1 组没跑）: " + e); }
            try { RunSettings(); }       catch (Exception e) { Ok(false, "③ 设置窗社交图标 段异常（算红）: " + e); }
        }
        finally
        {
            Debug.Log(P + $"=== B109_E12 合计：{Pass} 通过 / {Fail} 失败 ===");
        }
    }

    // ============================================================ ① 轮抽结算页
    static void RunDraft()
    {
        Debug.Log(P + "--- ① `DraftModeWindow` 结算页：背景 / 胜场格 / 败场格 ---");
        DraftModeWindow win = null;
        try
        {
            var wm = WindowsManager.Get();
            if (!Ok(wm != null, "①-前提：拿得到 `WindowsManager`")) return;

            win = DraftModeWindow.Create(wm, 20261031);
            if (!Ok(win != null, "①-前提：`DraftModeWindow.Create` 建出窗")) return;

            // ✅ 2026-11-05 `B143-W8`（E74 剩余 20 颗之 ✅-3）：联盟计分面板 ▸ `No Alliance ▸ Join Alliance text`（原版 40/15）。
            // ⚠️ 本文件跑在 **BattleScene**（`BattleScene.cs:47035`）⇒ E74 的计数必须并进本宿主的 `Pass/Fail`，否则这条只印不计数。
            int e74p0 = B142E74_Assert.Pass, e74f0 = B142E74_Assert.Fail;
            var sp = win.GetComponentInChildren<AllianceEventScorePanel>(true);
            B142E74_Assert.CheckStation(sp != null ? sp.transform : null, "No Alliance/Join Alliance text", 40f, 15f,
                "Alliance Scoring Panel ▸ No Alliance ▸ Join Alliance text");
            Pass += B142E74_Assert.Pass - e74p0; Fail += B142E74_Assert.Fail - e74f0;

            var marks = Deep(win.transform, "Win Marks");
            if (!Ok(marks != null, "①-前提：`Win Marks` 在（结算页那棵树建出来了）")) return;

            // ---- (1) 背景：`Win Marks > Background Back > Background` ----
            var bgBack = Child(marks, "Background Back");
            Ok(bgBack != null, "①a 前提：`Win Marks > Background Back` 在（原版结构；它自己那颗 `Image` 是**组件禁用**、不该画）");
            var bg = bgBack != null ? Child(bgBack, "Background") : null;
            Ok(bg != null, "①b ★ 子层 `Background Back > Background` 在 —— 以前**整层没建**（背景那里什么都没有）");
            CheckArt(bg, "①c", "Victories_Background_V2", 915, 392);
            CheckTint(bg, "①d", new Color(1f, 1f, 1f, 1f));

            // ---- (2) 胜场格：空槽 `Rank Skull Empty` + 亮格 `Rank Skull` ----
            var winCell = Child(marks, "Stage Info UI_ref");
            Ok(winCell != null, "①e 前提：胜场格 `Stage Info UI_ref` 在");
            if (winCell != null)
            {
                CheckArt(Child(winCell, "Slot"), "①f", "Rank_Skull_Empty", 128, 128);
                CheckTint(Child(winCell, "Slot"), "①g", new Color(1f, 0.561f, 0.561f, 1f));
                CheckArt(Child(winCell, "Mark"), "①h", "Rank_Skull", 128, 128);
                CheckTint(Child(winCell, "Mark"), "①i", new Color(1f, 1f, 1f, 1f));
            }

            // ---- (3) 败场格：底 `Victory_round_back_with light`（alpha 0.639）+ 亮格 `40k_DraftMode_icon_cross` ----
            var losses = Deep(win.transform, "Losses container");
            Ok(losses != null, "①j 前提：`Losses container` 在");
            var lossCell = losses != null ? Child(losses, "Losses Info UI_ref") : null;
            Ok(lossCell != null, "①k 前提：败场格 `Losses Info UI_ref` 在");
            if (lossCell != null)
            {
                CheckArt(Child(lossCell, "Slot"), "①l", "Victory_round_back_with_light", 256, 256);
                CheckTint(Child(lossCell, "Slot"), "①m", new Color(1f, 1f, 1f, 0.639f));
                CheckArt(Child(lossCell, "Mark"), "①n", "40k_DraftMode_icon_cross", 128, 128);
                CheckTint(Child(lossCell, "Mark"), "①o", new Color(1f, 1f, 1f, 1f));
            }

            // ---- (4) 整窗一张都不能缺（缺图 = `MenuDraw.Rect` 直接 return null ⇒ 上面那几条本来就会红）----
            var miss = win.MissingArt;
            Ok(miss != null && miss.Count == 0,
               "①p ★★ 整窗 `MissingArt` = **0**（实得 " + (miss != null ? miss.Count : -1) + " 条"
             + (miss != null && miss.Count > 0 ? "：" + string.Join("、", miss.ToArray()) : "") + "）");
        }
        finally
        {
            if (win != null) { try { UnityEngine.Object.DestroyImmediate(win.gameObject); } catch { } }
            try { DraftModeWindow.ClearRunForTest(); } catch { }
            try { WindowsManager.ClearReuseCacheForTest(); } catch { }
        }
    }

    // ============================================================ ② 战令 `Premium Glow`
    static void RunExpansionPass()
    {
        Debug.Log(P + "--- ② `ExpansionPassEventWindow` 的 `Premium Glow` ---");
        ExpansionPassEventWindow win = null;
        try
        {
            var wm = WindowsManager.Get();
            if (!Ok(wm != null, "②-前提：拿得到 `WindowsManager`")) return;

            win = ExpansionPassEventWindow.Create(wm);
            if (!Ok(win != null, "②-前提：`ExpansionPassEventWindow.Create` 建出窗")) return;

            // `Create` 按原版**不自开**（`B103_R1` · AUD03 #120）⇒ 树要自己建。
            win.Build();

            var glow = Deep(win.transform, "Premium Glow");
            Ok(glow != null, "②a ★ 节点 `Premium Glow` 在 —— 以前这里是 `MenuDraw.Node` + 一句 `Note(...)`（只建空节点、不画图）");
            CheckArt(glow, "②b", "40k_Rewards_icon_base_card_SDF", 128, 128);
            CheckTint(glow, "②c", new Color(1f, 1f, 1f, 1f));

            // 灭自证：同族的**非 SDF** 那张（`40k_Rewards_icon_base_card`，328×498）也在 `Resources` 里，
            //   拿它顶上去看起来「有图」、尺寸却不对 ⇒ 单独钉一条。
            var gq = glow != null ? glow.GetComponent<ImageQuad>() : null;
            var got = gq != null && gq.Texture != null ? gq.Texture.name : "<null>";
            Ok(got != "40k_Rewards_icon_base_card",
               "②d ★ 用的**不是** `40k_Rewards_icon_base_card`（那张是 328×498 的抽屉底图，⛔ 别顶；实得 `" + got + "`）");

            Ok(win.MissingArt == null || !win.MissingArt.Contains("40k_Rewards_icon_base_card_SDF"),
               "②e ★ `40k_Rewards_icon_base_card_SDF` **不在**缺图表里（在 ⇒ 这一件没画）");
        }
        finally
        {
            if (win != null) { try { UnityEngine.Object.DestroyImmediate(win.gameObject); } catch { } }
            try { WindowsManager.ClearReuseCacheForTest(); } catch { }
        }
    }

    // ============================================================ ③ 设置窗社交图标
    static void RunSettings()
    {
        Debug.Log(P + "--- ③ `SettingsWindow` 五颗社交 logo + 支持页外链钮图标 ---");
        SettingsWindow win = null;
        try
        {
            var wm = WindowsManager.Get();
            if (!Ok(wm != null, "③-前提：拿得到 `WindowsManager`")) return;

            win = SettingsWindow.Create(wm);
            if (!Ok(win != null, "③-前提：`SettingsWindow.Create` 建出窗")) return;
            // ⚠️ `Create` **不建树** —— `Build()` 只在 `Open()` 第一句里跑（`SettingsWindow：Create` 无 `Build`）。
            win.Open();

            // ---- 五颗 logo（`Social Media Links` 下，HLG 排的）----
            var links = Deep(win.transform, "Social Media Links");
            Ok(links != null, "③a 前提：`Social Media Links` 在");
            if (links != null)
            {
                // 顺序 = 原版 prefab 的兄弟序；名字与图名**都写死字面量**（⛔ 不读 `AcSocNode` / `AcSocArt`）
                CheckSocial(links, "Discord Button",  "①", "Discord-Logo-Color",                       256, 256);
                CheckSocial(links, "IG Button",       "②", "Instagram_icon",                           300, 300);
                CheckSocial(links, "Facebook Button", "③", "fb-icon",                                  256, 256);
                CheckSocial(links, "Twitter Button",  "④", "Twitter_Social_Icon_Rounded_Square_Color", 256, 256);
                CheckSocial(links, "Youtube Button",  "⑤", "YouTube_full-color_icon_(2017).svg",       300, 300);
            }

            // ---- 支持页 `External Link Icon`（原版 `Button_External_Link` · PA=1 · 85×85）----
            //   ⚠️ 支持页在 `Build()` 里**就建好了**（`_pages.Add(BuildSupportPage(...))`），`OpenTab` 只是切 `activeSelf`
            //   ⇒ 不必真的切到 Support 页也能读到（`Deep` 会走 inactive 子树）。
            int found = 0, wrong = 0;
            string wrongDetail = null;
            var supportPage = Deep(win.transform, "Support Tab") ?? win.transform;
            CollectExternalLinkIcons(supportPage, ref found, ref wrong, ref wrongDetail);
            Ok(found == 4, "③f ★ 支持页四颗 `External Link Icon` 都建出来了（实得 " + found + " 颗；0 = 整族没建）");
            Ok(wrong == 0,
               "③g ★★ 每一颗 `External Link Icon` 的贴图 = `Button_External_Link`（实得 " + (found - wrong) + "/" + found
             + (wrong > 0 ? "；第一个不对的：" + wrongDetail : "") + "）");
        }
        finally
        {
            if (win != null) { try { win.Close(); } catch { } try { UnityEngine.Object.DestroyImmediate(win.gameObject); } catch { } }
            try { WindowsManager.ClearReuseCacheForTest(); } catch { }
        }
    }

    static void CheckSocial(Transform links, string buttonName, string tag, string sprite, int w, int h)
    {
        var btn = Child(links, buttonName);
        if (!Ok(btn != null, "③" + tag + "a 前提：`" + buttonName + "` 在")) return;
        var img = Child(btn, "Image");
        // 灭自证：还没接的时候循环里**根本没有 `Rect` 调用** ⇒ 只有 `X Button` 空节点、没有 `Image` 子件。
        if (!Ok(img != null, "③" + tag + "b ★ `" + buttonName + " > Image` 在 —— 以前这五颗**从来没画过图**"))
            return;
        CheckArt(img, "③" + tag + "c", sprite, w, h);
    }

    static void CollectExternalLinkIcons(Transform root, ref int found, ref int wrong, ref string wrongDetail)
    {
        if (root == null) return;
        if (root.name == "External Link Icon")
        {
            found++;
            var q = root.GetComponent<ImageQuad>();
            string got = q != null && q.Texture != null ? q.Texture.name : "<无图>";
            if (got != "Button_External_Link") { wrong++; if (wrongDetail == null) wrongDetail = got; }
        }
        for (int i = 0; i < root.childCount; i++)
            CollectExternalLinkIcons(root.GetChild(i), ref found, ref wrong, ref wrongDetail);
    }

    // ============================================================ 小工具
    /// <summary>`node > ImageQuad`，断「贴图名 / 尺寸 = 原版那个」。<paramref name="node"/> 为 null 时**算红**（不跳过）。</summary>
    static void CheckArt(Transform node, string tag, string sprite, int w, int h)
    {
        if (!Ok(node != null, tag + " 前提：节点在（= `MenuDraw.Rect` 没因缺图返回 null）")) return;
        var q = node.GetComponent<ImageQuad>();
        if (!Ok(q != null, tag + " 前提：节点上挂着 `ImageQuad`")) return;
        if (!Ok(q.Texture != null, tag + " 前提：`ImageQuad.Texture != null`（真的画了图、不是空 quad）")) return;
        string got = q.Texture.name;
        Ok(got == sprite,
           tag + " ★★ 贴图 = `" + sprite + "`（实得 `" + got + "`）—— 期望值是**原版 prefab 的 `m_Sprite.m_Name`**"
         + "（空格→下划线），⛔ 不是我们代码里的常量");
        Ok(q.Texture.width == w && q.Texture.height == h,
           tag + " ★ 贴图尺寸 = " + w + "×" + h + "（实得 " + q.Texture.width + "×" + q.Texture.height
         + "）—— 判据 = 原版 sprite 的 `m_Rect`");
    }

    static void CheckTint(Transform node, string tag, Color want)
    {
        if (!Ok(node != null, tag + " 前提：节点在（才能读 tint）")) return;
        var q = node.GetComponent<ImageQuad>();
        if (!Ok(q != null, tag + " 前提：节点上挂着 `ImageQuad`")) return;
        var got = q.Tint;
        bool near = Mathf.Abs(got.r - want.r) < 0.01f && Mathf.Abs(got.g - want.g) < 0.01f
                 && Mathf.Abs(got.b - want.b) < 0.01f && Mathf.Abs(got.a - want.a) < 0.01f;
        Ok(near, tag + " ★ tint = 原版 `Image.m_Color` (" + Fmt(want) + ")（实得 " + Fmt(got) + "）");
    }

    static string Fmt(Color c) { return "(" + c.r.ToString("F3") + "," + c.g.ToString("F3") + "," + c.b.ToString("F3") + "," + c.a.ToString("F3") + ")"; }

    static Transform Child(Transform parent, string name)
    {
        if (parent == null || string.IsNullOrEmpty(name)) return null;
        for (int i = 0; i < parent.childCount; i++)
        {
            var c = parent.GetChild(i);
            if (c.name == name) return c;
        }
        return null;
    }

    static Transform Deep(Transform root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name)) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var r = Deep(root.GetChild(i), name);
            if (r != null) return r;
        }
        return null;
    }
}
