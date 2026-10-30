// B101R3_Assert.cs — 执行代理 `B101_R3`（2026-10-30）的断言：**资源接线三条**（A1 轮抽结算页 / A2 战令 Premium Glow /
//   A3 设置窗社交图标）。判据与施工单 → `资料/完全复刻/报告/B101_R3_资源接线施工单.md`。
//
// 用法（宿主里加两行，想并进总数就带后一行）：
//
//     B101R3_Assert.Run();
//     pass += B101R3_Assert.Pass; fail += B101R3_Assert.Fail;
//
// 也可以单独跑：`-executeMethod B101R3_Assert.Run`
// 也可以只跑一组：`RunResourceGate()` / `RunDraft()` / `RunSettings()` / `CheckExpansionPass(Transform winRoot)`
//   （`CheckExpansionPass` 要宿主**已经建出** `ExpansionPassEventWindow` —— 它那棵树要 liveops 数据才建得完）。
//
// ==================================================================
//  期望值从哪来（⛔ 不许拿我们自己的常量证明我们自己的实现）
// ------------------------------------------------------------------
//   · **图名** = 原版 `Image.m_Sprite` 的 `m_Name`（`bundle_menus_assets_all` 现读）；
//   · **像素尺寸** = `Resources/Art/ui_menu/*.png` 的**贴图本身**（`Texture.width/height`）——
//     这是**印刷品/原版图**那一侧的事实，⛔ 不是我们代码里的常量：把实现改回去（画黑块/画不画）这两条立刻红；
//   · **tint** = 原版 `Image.m_Color`（`Rank Skull Empty` 是 (1,0.561,0.561,1)、`Victory_round_back_with light` 是
//     (1,1,1,0.639)）—— 也来自 prefab，不是我们挑的。
// ==================================================================
using System;
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

public static class B101R3_Assert
{
    const string P = "[B101R3] ";

    public static int Pass { get; private set; }
    public static int Fail { get; private set; }

    static bool Ok(bool cond, string msg)
    {
        if (cond) { Pass++; Debug.Log(P + "   ✓ " + msg); return true; }
        Fail++; Debug.LogError(P + "   ✗ " + msg); return false;
    }

    /// <summary>跑全部三组（资源闸 + 轮抽 + 设置窗）。`ExpansionPass` 那组要宿主传窗进来。</summary>
    public static void Run()
    {
        Pass = 0; Fail = 0;
        try
        {
            RunResourceGate();
            RunDraft();
            RunSettings();
        }
        catch (Exception e)
        {
            Ok(false, "断言宿主抛异常（后面的条目没跑完）：" + e);
        }
        finally
        {
            Debug.Log(P + $"=== R3 合计：{Pass} 通过 / {Fail} 失败 ===");
        }
    }

    // ==================================================================
    //  ⓪ 资源闸：这 11 张图必须真在 `Resources/Art/ui_menu/`
    //     （A1 的 5 张 + A3 的 6 张；缺任何一张，下面两组都会连带红）
    // ==================================================================
    struct ArtExpect { public string name; public int w, h; public ArtExpect(string n, int a, int b) { name = n; w = a; h = b; } }

    static readonly ArtExpect[] DraftArt =
    {
        new ArtExpect(DraftModeWindow.ArtBg,       915, 392),   // `Victories_Background_V2`
        new ArtExpect(DraftModeWindow.ArtWinSlot,  128, 128),   // `Rank Skull Empty`
        new ArtExpect(DraftModeWindow.ArtWinMark,  128, 128),   // `Rank Skull`
        new ArtExpect(DraftModeWindow.ArtLossSlot, 256, 256),   // `Victory_round_back_with light`
        new ArtExpect(DraftModeWindow.ArtLossMark, 128, 128),   // `40k_DraftMode_icon_cross`（⛔ 还没导就这条红）
    };

    static readonly ArtExpect[] SocialArt =
    {
        new ArtExpect("Discord-Logo-Color",                         256, 256),
        new ArtExpect("Instagram_icon",                             300, 300),
        new ArtExpect("fb-icon",                                    256, 256),
        new ArtExpect("Twitter_Social_Icon_Rounded_Square_Color",   256, 256),
        new ArtExpect("YouTube_full-color_icon_(2017).svg",         300, 300),
        new ArtExpect(SettingsWindow.ArtExtLink,                     85,  85),
    };

    public static void RunResourceGate()
    {
        Debug.Log(P + "--- ⓪ 资源闸：11 张图（A1 五张 + A3 六张）---");
        foreach (var a in DraftArt) GateOne(a, "A1");
        foreach (var a in SocialArt) GateOne(a, "A3");
    }

    static void GateOne(ArtExpect a, string who)
    {
        var t = CardArt.MenuUi(a.name);
        Ok(t != null, $"{who} 取得到 `Resources/Art/ui_menu/{a.name}.png`"
                    + "（`CardArt.MenuUi` 按 `ui_menu/ → ui_deck/ → ui/` 兜底；取不到 = 这一件一定画不出来）");
        if (t == null) return;
        Ok(t.width == a.w && t.height == a.h,
           $"{who} `{a.name}` 的贴图尺寸 = **{a.w}×{a.h}**（实得 {t.width}×{t.height}）"
         + " —— 判据 = 原版 sprite 的 `m_Rect`（⛔ 不是我们代码里的常量）");
    }

    // ==================================================================
    //  ① A1 · 轮抽结算页（`Shell/DraftModeWindow.cs`）
    // ==================================================================
    public static void RunDraft()
    {
        Debug.Log(P + "--- ① A1 轮抽结算页：背景 / 胜场格 / 败场格 ---");
        DraftModeWindow win = null;
        try
        {
            var wm = WindowsManager.Get();
            if (!Ok(wm != null, "前提：拿得到 `WindowsManager`")) return;

            win = DraftModeWindow.Create(wm, 20261030);
            if (!Ok(win != null, "前提：`DraftModeWindow.Create` 建出窗来")) return;

            // ---- 背景层：`Win Marks > Background Back > Background` ----
            var winMarks = FindDeep(win.transform, "Win Marks");
            Ok(winMarks != null, "①a 前提：`Win Marks` 在（结算页那棵树建出来了）");
            if (winMarks == null) return;

            var bb = FindChild(winMarks, "Background Back");
            Ok(bb != null, "①b ★ `Win Marks > Background Back` 在（原版结构；它自己那张 `Highlight Rounded Square` 是**组件禁用**、不该画）");
            var bgNode = bb != null ? FindChild(bb, "Background") : null;
            Ok(bgNode != null, "①c ★★ `Background Back > Background` 在（= 原版 `Victories_Background_V2` 那一层 —— 以前**整层没建**）");
            var bgQ = bgNode != null ? bgNode.GetComponent<ImageQuad>() : null;
            Ok(bgQ != null && bgQ.Texture != null,
               "①d ★★ 背景层**真的画了图**（`ImageQuad.Texture != null`；🧨 画回色块/不建这一层 ⇒ 红）");
            Ok(bgQ != null && bgQ.Texture != null && bgQ.Texture.name == DraftModeWindow.ArtBg,
               "①e ★★ 背景贴图就是 `" + DraftModeWindow.ArtBg + "`"
             + "（实得 `" + (bgQ != null && bgQ.Texture != null ? bgQ.Texture.name : "<null>") + "`）");

            // ---- 胜场格：空槽 `Rank Skull Empty` + 亮格 `Rank Skull` ----
            var winCell = FindChild(winMarks, "Stage Info UI_ref");
            Ok(winCell != null, "①f 前提：胜场格 `Stage Info UI_ref` 在");
            if (winCell != null)
            {
                var slot = FindChild(winCell, "Slot");
                var sq = slot != null ? slot.GetComponent<ImageQuad>() : null;
                Ok(sq != null && sq.Texture != null && sq.Texture.name == DraftModeWindow.ArtWinSlot,
                   "①g ★★ 胜场格**空槽** = `" + DraftModeWindow.ArtWinSlot + "`"
                 + "（实得 `" + (sq != null && sq.Texture != null ? sq.Texture.name : "<null>") + "`）"
                 + " —— 🧨 画回半透明黑块 ⇒ 红");
                Ok(sq != null && Mathf.Abs(sq.Tint.g - 0.561f) < 0.01f && Mathf.Abs(sq.Tint.b - 0.561f) < 0.01f,
                   "①h ★ 空槽 tint 是原版那支**粉**（g/b ≈ 0.561，实得 "
                 + (sq != null ? $"({sq.Tint.r:F3},{sq.Tint.g:F3},{sq.Tint.b:F3})" : "<无>") + "）");

                var mark = FindChild(winCell, "Mark");
                var mq = mark != null ? mark.GetComponent<ImageQuad>() : null;
                Ok(mq != null && mq.Texture != null && mq.Texture.name == DraftModeWindow.ArtWinMark,
                   "①i ★★ 胜场格**亮格** = `" + DraftModeWindow.ArtWinMark + "`"
                 + "（实得 `" + (mq != null && mq.Texture != null ? mq.Texture.name : "<null>") + "`）"
                 + " —— 🧨 画回金色块 ⇒ 红");
            }

            // ---- 败场格：底 `Victory_round_back_with light`（alpha 0.639）+ 亮格 `40k_DraftMode_icon_cross` ----
            var losses = FindDeep(win.transform, "Losses container");
            Ok(losses != null, "①j 前提：`Losses container` 在");
            var lossCell = losses != null ? FindChild(losses, "Losses Info UI_ref") : null;
            Ok(lossCell != null, "①k 前提：败场格 `Losses Info UI_ref` 在");
            if (lossCell != null)
            {
                var slot = FindChild(lossCell, "Slot");
                var sq = slot != null ? slot.GetComponent<ImageQuad>() : null;
                Ok(sq != null && sq.Texture != null && sq.Texture.name == DraftModeWindow.ArtLossSlot,
                   "①l ★★ 败场格底 = `" + DraftModeWindow.ArtLossSlot + "`"
                 + "（实得 `" + (sq != null && sq.Texture != null ? sq.Texture.name : "<null>") + "`）");
                Ok(sq != null && Mathf.Abs(sq.Tint.a - 0.639f) < 0.01f,
                   "①m ★ 败场格底的 alpha = **0.639**（原版 `Image.m_Color.a`；实得 "
                 + (sq != null ? sq.Tint.a.ToString("F3") : "<无>") + "）");
                // 🔴 这一条依赖 `40k_DraftMode_icon_cross` **已导**（`B100_F3` 那批漏了它，本轮补导）。
                var mark = FindChild(lossCell, "Mark");
                var mq = mark != null ? mark.GetComponent<ImageQuad>() : null;
                Ok(mq != null && mq.Texture != null && mq.Texture.name == DraftModeWindow.ArtLossMark,
                   "①n ★★ 败场格亮格 = `" + DraftModeWindow.ArtLossMark + "`"
                 + "（实得 `" + (mq != null && mq.Texture != null ? mq.Texture.name : "<null>") + "`）"
                 + " —— 取不到多半是**这张图还没导**（`工具/import_original_art.py` 的 `MENU_IMAGES`）");
            }

            // ---- 缺图出声：整窗一张都不能缺 ----
            var miss = win.MissingArt;
            Ok(miss != null && miss.Count == 0,
               "①o ★★ 整窗 `MissingArt` = **0**（实得 " + (miss != null ? miss.Count : -1) + " 条"
             + (miss != null && miss.Count > 0 ? "：" + string.Join("、", miss.ToArray()) : "") + "）");
        }
        finally
        {
            if (win != null) { try { UnityEngine.Object.DestroyImmediate(win.gameObject); } catch { } }
            try { DraftModeWindow.ClearRunForTest(); } catch { }
            try { WindowsManager.ClearReuseCacheForTest(); } catch { }
        }
    }

    // ==================================================================
    //  ② A2 · 战令 `Premium Glow`（`Shell/ExpansionPassEventWindow.cs`）
    //     要宿主**已经建出**那扇窗（它那棵树依赖 liveops 事件数据）。
    // ==================================================================
    public static void CheckExpansionPass(Transform winRoot)
    {
        Debug.Log(P + "--- ② A2 战令 `Premium Glow` ---");
        if (!Ok(winRoot != null, "前提：宿主传进来一棵已建好的 `ExpansionPassEventWindow` 树")) return;

        var glow = FindDeep(winRoot, "Premium Glow");
        Ok(glow != null, "②a ★ 节点 `Premium Glow` 在");
        if (glow == null) return;

        var q = glow.GetComponent<ImageQuad>();
        Ok(q != null && q.Texture != null,
           "②b ★★ `Premium Glow` **真的画了图**（以前只 `Note(...)`、不画；🧨 改回只建节点 ⇒ 红）");
        if (q == null || q.Texture == null) return;
        Ok(q.Texture.name == "40k_Rewards_icon_base_card_SDF",
           "②c ★★ 贴图 = `40k_Rewards_icon_base_card_SDF`（实得 `" + q.Texture.name + "`）"
         + " —— ⛔ 不是 `40k_Rewards_icon_base_card`（那张是另一张，别顶）");
        Ok(q.Texture.width == 128 && q.Texture.height == 128,
           "②d ★ 贴图尺寸 = 128×128（实得 " + q.Texture.width + "×" + q.Texture.height + "）");
    }

    // ==================================================================
    //  ③ A3 · 设置窗社交图标（`Shell/SettingsWindow.cs`）
    // ==================================================================
    public static void RunSettings()
    {
        Debug.Log(P + "--- ③ A3 设置窗五颗 logo + 外链钮图标 ---");
        SettingsWindow win = null;
        try
        {
            var wm = WindowsManager.Get();
            if (!Ok(wm != null, "前提：拿得到 `WindowsManager`")) return;

            win = SettingsWindow.Create(wm);
            if (!Ok(win != null, "前提：`SettingsWindow.Create` 建出窗来")) return;
            // 🔴 2026-10-30 订正（`B102_D1` 三条红 ③a · 测法不成立）：`Create` **不建树** —— `Build()` 只在
            //   `Open()` 第一句里跑（`Shell/SettingsWindow.cs`：`Create` 无 `Build`；`Open()` = `Build(); OpenTab(Current); NetRuntime.Ensure(...)`）
            //   ⇒ 原来只 `Create` 不 `Open`，窗根是空的、`FindDeep` 必然找不到 `Social Media Links`（自检红与实现对错无关）。
            win.Open();

            var links = FindDeep(win.transform, "Social Media Links");
            Ok(links != null, "③a 前提：`Social Media Links` 在");
            if (links == null) return;

            var names = new[] { "Discord Button", "IG Button", "Facebook Button", "Twitter Button", "Youtube Button" };
            var art = SettingsWindow.AcSocArt;
            Ok(art != null && art.Length == names.Length,
               "③b 前提：`AcSocArt` 五条与五个钮一一对应（实得 " + (art != null ? art.Length : -1) + " 条）");
            if (art == null || art.Length != names.Length) return;

            for (int i = 0; i < names.Length; i++)
            {
                var btn = FindChild(links, names[i]);
                Ok(btn != null, $"③c{i} 前提：`{names[i]}` 在");
                var img = btn != null ? FindChild(btn, "Image") : null;
                var q = img != null ? img.GetComponent<ImageQuad>() : null;
                Ok(q != null && q.Texture != null,
                   $"③d{i} ★★ `{names[i]}` 的 `Image` **真的画了图**（🧨 循环里没有 `Rect` 那次（B101_R3 之前）⇒ 红）");
                Ok(q != null && q.Texture != null && q.Texture.name == art[i],
                   $"③e{i} ★★ `{names[i]}` 的贴图 = `{art[i]}`（实得 `"
                 + (q != null && q.Texture != null ? q.Texture.name : "<null>") + "`）");
            }

            var miss = win.MissingArtForTest;
            Ok(miss != null, "③f 前提：读得到 `MissingArt`");
            if (miss != null)
                for (int i = 0; i < art.Length; i++)
                    Ok(!miss.Contains(art[i]), $"③g{i} `{art[i]}` **不在**缺图表里");

            // 外链钮那颗（支持页）：图名早就在代码里、只是以前没有图 ⇒ 现在应当命中
            Ok(CardArt.MenuUi(SettingsWindow.ArtExtLink) != null,
               "③h ★ `" + SettingsWindow.ArtExtLink + "` 取得到（支持页四颗 `External Link Icon` 靠它）");
        }
        finally
        {
            if (win != null) { try { win.Close(); } catch { } try { UnityEngine.Object.DestroyImmediate(win.gameObject); } catch { } }
            try { WindowsManager.ClearReuseCacheForTest(); } catch { }
        }
    }

    // ==================================================================
    //  小工具（按名字递归找；本工程没有 uGUI 运行时，只能用 Transform 走）
    // ==================================================================
    static Transform FindChild(Transform parent, string name)
    {
        if (parent == null || string.IsNullOrEmpty(name)) return null;
        for (int i = 0; i < parent.childCount; i++)
        {
            var c = parent.GetChild(i);
            if (c.name == name) return c;
        }
        return null;
    }

    static Transform FindDeep(Transform root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name)) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var r = FindDeep(root.GetChild(i), name);
            if (r != null) return r;
        }
        return null;
    }
}
