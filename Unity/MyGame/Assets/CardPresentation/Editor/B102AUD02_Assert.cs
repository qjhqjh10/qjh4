// B102AUD02_Assert.cs — 执行代理 `B102` / `AUD02`（2026-10-30）的断言：**筛选面板小标题没大写**。
//
// 判据（⛔ 不是我们自己的常量，也不拿我们的实现证明我们的实现）：
//   原版 `Deck Editing Menu` 那 5 颗筛选小标题的 TMP `m_FontStyle` 全 = **16**（= `TMPro.FontStyles.UpperCase`）
//   —— UnityPy 直读真包 `bundle_menus_assets_all`，逐颗核过：
//     · `Content Area/Card Display/Card Filters/Scroll View/Viewport/Filters/
//        {Army Filter,Rarity FIlter,Cost Filter,Type Filter}/Title`
//     · `Content Area/Cosmetic Display/Cosmetic FIlter/Filters/Army Filter/Title`
//   五颗的 `m_text` 依次 = `Army` / `Rarity` / `Energy Cost` / `Type` / `Army`
//   —— **`m_text` 是混合大小写、大小写只存在于「渲染档」**（原版画面是 `ARMY` / `RARITY` / …）。
//   我们原来只把文案换成 `Loc.T`（A891 那一批）、没换档 ⇒ 于是印出来是 `Army`。
//
// 用法（宿主里加两行，想并进总数就带后一行）：
//
//     B102AUD02_Assert.Run();
//     pass += B102AUD02_Assert.Pass; fail += B102AUD02_Assert.Fail;
//
// 也可以单独跑：`-executeMethod B102AUD02_Assert.Run`
// 也可以只跑一组（宿主自己拿得到根节点时更准）：
//     B102AUD02_Assert.RunDeck(dr.Root);               // 卡组编辑窗：5 颗（4 卡牌筛选 + 1 卡背筛选）
//     B102AUD02_Assert.RunCollection(cw.PageRoot(0));  // 收藏窗 Cards 页：4 颗
//
// ⚠️ **前提（宿主必须满足，不满足 = 红、⛔ 不是跳过）**：
//   · `Run()` 靠 `FindObjectsByType` 找宿主 ⇒ 调用那一刻那些窗**必须已经把树建出来**
//     （`DeckRuntime.Root != null`；收藏窗要 `PageRoot(0)` 非空）。
//   · **收藏窗更严**：那 4 颗小标题走 `GameWindow.Text`，它头一句 `MenuDraw.Visible(...)` 会把
//     「落在视口外」的文字**整件不建**，同时 `TitleRow` 还有一条 `_fltScroll.Intersects(r)` 的早退
//     ⇒ 收藏窗那组要在**抽屉开着、且滚动到能看见**的那一态跑。
//   · 卡组编辑窗那 5 颗没有可见性闸（`DeckRuntime.Txt` → `Label.Create`），抽屉收着也在。
using System;
using CardPresentation;
using TMPro;
using UnityEngine;

public static class B102AUD02_Assert
{
    const string P = "[B102AUD02] ";

    public static int Pass { get; private set; }
    public static int Fail { get; private set; }

    static bool Ok(bool cond, string msg)
    {
        if (cond) { Pass++; Debug.Log(P + "   ✓ " + msg); return true; }
        Fail++; Debug.LogError(P + "   ✗ " + msg); return false;
    }

    // ---- 卡组编辑窗那 5 颗：节点名 → 文案原文 -------------------------------------------
    // 节点名 = `DeckRuntime.BuildFilters` 的 `"flt_title_" + Text.Replace(" ","_")`（`Energy Cost` → `..._Energy_Cost`），
    // 卡背那颗是写死的 `cosmoflt_title`（D43）。⛔ 名字一个字都别改：`Editor/DeckScene.cs` 也按名找。
    static readonly string[] DeckNode = { "flt_title_Army", "flt_title_Rarity", "flt_title_Energy_Cost", "flt_title_Type", "cosmoflt_title" };
    static readonly string[] DeckSrc = { "Army", "Rarity", "Energy Cost", "Type", "Army" };

    // ---- 收藏窗 Cards 页那 4 颗：节点名 = `CollectionWindow.TitleRow` 的 `"Title " + 英文原名` ------
    static readonly string[] CollNode = { "Title Army", "Title Rarity", "Title Energy Cost", "Title Type" };
    static readonly string[] CollSrc = { "Army", "Rarity", "Energy Cost", "Type" };

    /// <summary>跑全部：自动找宿主窗（卡组编辑窗 / 收藏窗，找到哪几个就跑哪几组）。</summary>
    public static void Run()
    {
        Pass = 0; Fail = 0;
        int groups = 0;
        try
        {
            var dts = UnityEngine.Object.FindObjectsByType<DeckRuntime>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var dt in dts)
            {
                if (dt == null) continue;
                if (dt.Root == null)
                { Ok(false, "（A0 前提）找到一个 `DeckRuntime`，但它的 `Root` 是 null（这个窗还没建树）⇒ 卡组编辑窗这一组跑了 0 条"); continue; }
                groups++; RunDeck(dt.Root);
            }

            var cws = UnityEngine.Object.FindObjectsByType<CollectionWindow>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var cw in cws)
            {
                if (cw == null) continue;
                var page = cw.PageRoot(0);                    // 0 = Cards 页（见 `CollectionWindow.DrawerByPage` 那张表）
                groups++; RunCollection(page != null ? page : cw.transform);
            }

            if (groups == 0)
                Ok(false, "（A0 前提）场景里既没有 `DeckRuntime` 也没有 `CollectionWindow` ⇒ 一条都没跑（**算红**，⛔ 不是跳过）");
            else
                CheckDefaultStyleHasNoUpperCase();            // A2 · 灭自证（跨组共用一条）
        }
        catch (Exception e)
        {
            Ok(false, "断言抛异常（后面的条目没跑完）：" + e);
        }
        finally
        {
            Debug.Log(P + $"=== AUD02 合计：{Pass} 通过 / {Fail} 失败 ===");
        }
    }

    /// <summary>卡组编辑窗那一组（5 颗：4 卡牌筛选 + 1 卡背筛选）。传 `DeckRuntime.Root`。</summary>
    public static void RunDeck(Transform root)
    {
        Ok(root != null, "（A0 前提 · 卡组编辑窗）根节点在");
        if (root == null) return;
        for (int i = 0; i < DeckNode.Length; i++) CheckOne(root, DeckNode[i], DeckSrc[i], "卡组编辑窗");
    }

    /// <summary>收藏窗 Cards 页那一组（4 颗）。传 `CollectionWindow.PageRoot(0)`（或窗根）。</summary>
    public static void RunCollection(Transform root)
    {
        Ok(root != null, "（A0 前提 · 收藏窗）Cards 页根节点在");
        if (root == null) return;
        for (int i = 0; i < CollNode.Length; i++) CheckOne(root, CollNode[i], CollSrc[i], "收藏窗");
    }

    // ==================================================================
    //  A0 / A1 / A1b —— 一颗标题三条
    // ==================================================================
    static void CheckOne(Transform root, string node, string src, string host)
    {
        var lb = FindLabel(root, node);

        // A0 ★ **硬前提**：节点必须在。⛔ 别写成 `if (lb != null) Ok(...)` ——
        //    「前提不成立 ⇒ 静默跳过」在本仓踩过 3 次（那条断言一次都没跑过、日志里却只有 ✓）。
        if (!Ok(lb != null, "（A0 前提 · " + host + "）小标题节点 `" + node + "` 建出来了")) return;

        // A1 ★ **被测**：`fontStyle` 含 `UpperCase`（原版 `m_FontStyle = 16`）。
        int fs = (int)lb.FontStyleNow;
        Ok((lb.FontStyleNow & FontStyles.UpperCase) == FontStyles.UpperCase,
           "（A1 ★）`" + node + "` 的 fontStyle 含 UpperCase（原版 m_FontStyle=16 · 现读 = " + fs + "）");

        // A1b **反面**：文案本身仍是混合大小写原文 —— `m_text` 原版就是 `Army`，大小写只该落在渲染档。
        //    🧨 这条咬「用 `ToUpper()` 改文案」那种假修：`Title.Text` 一行两用（节点名按它拼），
        //    改了文案 = 两个宿主的按名查找全断（而这正因为 A0 还绿所以看不出来，必须单独咬一条）。
        string want = FilterPanelModel.TitleText(src);
        Ok(lb.Text == want,
           "（A1b 反面）`" + node + "` 的文案仍是混合大小写原文（现读 `" + lb.Text + "` · 期望 `" + want + "`）");
    }

    // ==================================================================
    //  A2 · 🧨 灭自证：UpperCase 这个位**只可能**来自 `ApplyTitleFontStyle`
    //
    //  🧨 改法（谁把这条改红说明什么）：
    //    · 把 `FilterPanelModel.ApplyTitleFontStyle` 里那句 `SetFontStyle(UpperCase)` 删掉 ⇒ **A1 必红**（本文件对 9 颗逐一咬）；
    //    · 反过来，把 `UpperCase` 设成**全局默认**（例如塞进 `TmpFont.NewText`）⇒ **A2 必红**
    //      —— 那样 A1 会「全绿」，但绿的原因不是我们照原版加了这一档，而是所有文字都变大写了。
    //    所以 A1 与 A2 是**结构上不可能同时作弊**的一对。
    // ==================================================================
    static void CheckDefaultStyleHasNoUpperCase()
    {
        GameObject go = null;
        try
        {
            go = new GameObject("wf_aud02_probe");
            var probe = Label.Create(go.transform, "Army", Vector3.zero, 1, Color.white,
                                     new Vector2(0.5f, 0.5f), "wf_aud02_probe_lb");
            if (!Ok(probe != null, "（A2 前提）探针 `Label` 建得出来（建不出来 ⇒ 下面那条无从谈起）")) return;
            Ok((probe.FontStyleNow & FontStyles.UpperCase) == 0,
               "🧨 灭自证：**没被 `ApplyTitleFontStyle` 碰过**的 `Label`，出厂 fontStyle 不含 UpperCase（现读 = "
               + (int)probe.FontStyleNow + "）⇒ A1 那个位只可能来自本次改动、不是全局默认");
        }
        catch (Exception e)
        {
            Ok(false, "（A2）探针抛异常：" + e);
        }
        finally
        {
            if (go != null) UnityEngine.Object.DestroyImmediate(go);
        }
    }

    /// <summary>按节点名找那颗 `Label`（**含未激活**：`true` 那个实参 —— 抽屉收着时节点仍在）。</summary>
    static Label FindLabel(Transform root, string name)
    {
        if (root == null) return null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t.GetComponent<Label>();
        return null;
    }
}
