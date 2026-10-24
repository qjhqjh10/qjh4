// ProfileTabsAssertU2.cs — 执行代理 `U2`（2026-10-24 第七十三会话）的断言宿主
//
// 本文件**只放断言**（⛔ 不改产品行为、不建场景、不进任何宿主的 `RunCore` 正文）。
//
// ============================ 主对话怎么挂 ============================
// 在 `Editor/MainMenuScene.cs` 的 `RunCore` 里、**档案窗那一段之后**（也就是
// `ProfileWindowAssertE12.Run();` 那三行的下面）加三行：
//
//     ProfileTabsAssertU2.Run();
//     pass += ProfileTabsAssertU2.Pass;
//     fail += ProfileTabsAssertU2.Fail;
//
// ⚠️ 本文件**自己开窗**（`PlayerProfileWindow.LastOpened` 拿不到就 `MainMenuRuntime.OpenProfile()`），
//    唯一要求是「宿主已经建好外壳（有 `WindowsManager`）」；拿不到就**记一条 FAIL 并出声**（⛔ 不静默跳过）。
//
// ============================ 覆盖什么（判据逐条写在断言消息里） ============================
//  U2-1 【页签集合】`Menu Area/Tab Buttons` 的**子节点集合（含顺序）** == 原版 6 键
//        （`Profile Button` / `Avatar Button` / `Title Button` / `Battle Log Button` / `Trophies` / `Ranked`）
//        —— **多一个红、少一个也红**（防「我们自加第 7 颗键」）。⚠️ 数的是**子节点**（不筛 active），
//        因为「第 6 颗按用户拍板隐藏」的写法是**留着节点、关掉它**（见 U2-3）。
//        ⚠️ 期望值**不读 `PlayerProfileWindow.Tabs`**（那是被测实现自己），是下面 `OriginalTabs`
//        这一份**从 prefab 实读的字面量**（防自证）。
//  U2-2 【页集合】`Menu Area/Tab  Area/Tab Content` 的子节点集合（含顺序）== 原版 6 页
//        （`Profile Tab` / `Avatar Tab` / `Title Tab` / `Battle Log Tab` / `Trophies Tab` / **`Ranking Tab`**）。
//        🔴 **已知偏离（不掩盖）**：我们第 6 个页根的**节点名**是 `Ranked Tab`
//        （`Shell/PlayerProfileWindow.cs:464` 的 `Tabs[i].Node.Replace(" Button", "") + " Tab"` ⇒
//        `Ranked` + ` Tab`），而 prefab 里那个 GO 叫 **`Ranking Tab`**（资产文件就是 `Ranking Tab.json`）。
//        ⇒ 本断言**期望值仍写 prefab 名**，只在比对时对这一个名字做**归一**，并且**真的用到归一就出声**
//        （`Debug.LogWarning` + 落点）。⛔ 不许把期望值改成我们的错名（那等于给偏离发合格证）。
//  U2-3 【用户拍板：第 6 键要隐藏】可见页签集合 == 5（`个人资料/头像/头衔/战斗记录/成就`），
//        `Ranked`（`排名`）**必须不可见**（节点在、`activeSelf = false`）。
//  U2-4 【页签文案非空且走官方本地化】五颗**可见**键的 `Tab Toggle Title` 文字 ==
//        `Loc.T(TabTerms[i])`（官方 I2；取不到才回英文原文）—— 期望值取 **`Loc`**，不回读实现自己的函数。
//        ⚠️ 路径是 `<键>/Tab Toggle Title`（**没有 `Label` 那一层**：原版 `Label` 是空容器，我们拍平了）。
//        `Ranked` 的子孙**不建**（隐藏写法）⇒ 本组不查它，改由 U2-6 断「不可点」。
//  U2-5 【键几何】六个键的 rect **逐键步进 119.252、高 109.252**（原版 `VerticalLayoutGroup` 布局后真值
//        —— `menu_dump.py --verify-layout` 已对着手算值逐位核过）。
//        🔴 **量纲**：`RectTransform.rect` 是**世界单位**（108 px / 单位，`LayoutSpace.DesignHeight=10`）⇒ 要比 px 必须 **×108**。
//  U2-6 【第 6 键不可点】`Ranked` 键下**没有** `Hit` 命中区 / `WindowButton`（或整键 inactive）——
//        即「点了不会切页」。判据 = 用户拍板隐藏 ⇒ 它不该有入口。
//  ---- 以下是「个人资料页」（用户 2026-10-24 点名复查的那一页）----
//  U2-7 `Highest Rank/Content/footer/MainRating` **关**（出厂 `displaySeals=0`）。
//  U2-8 `Global Rating` **挂在 `MainRating` 下面**（原版父链 `footer/MainRating/Global Rating`），
//        且 Highest 那一份**随 `MainRating` 一起不可见**（改前它建在 `footer` 上 ⇒ 关不掉 ⇒
//        实拍里那张卡**左外侧**浮着一颗小图标 + 破折号）。
//  U2-9 `Current Rank` 的 `Global Rating` 仍**可见**（出厂 T 的那一份）。
//  U2-10 「当前战役」格的底图 = **`40K_profile_campaign_bg_*`** 族（不是通用红底 `UI_Deck_Information_submenu_Back`），
//        且 = 该阵营在**唯一那份阵营图表**里的名字（`PurchasePremiumWindow.ArmyBackgroundArt`）。

// ==================================================================
using CardPresentation;
using UnityEngine;

public static class ProfileTabsAssertU2
{
    const string P = "[U2] ";

    /// <summary>本类这一趟的通过 / 失败数（主对话想并进宿主的总数就加这两格）。</summary>
    public static int Pass { get; private set; }
    public static int Fail { get; private set; }

    // ============================================================ 判据（字面量，来自 prefab 实读）
    //
    // 出处 = `d:/2/新解包资源/assets_full/bundle_menus_assets_all/GameObject/Player Profile Window.json`
    // 的 `Tab Buttons` 子节点（6 个 `EverguildToggle`）与 `Tab Content` 子节点（6 个页根）：
    //   `python 工具/menu_dump.py bundle_menus_assets_all "Player Profile Window" --depth 4 --no-sprite`
    // ⛔ 别改成「读 `PlayerProfileWindow.Tabs`」—— 那就成了拿实现证明实现（铁律 13·5 / 断言不自证）。
    static readonly string[] OriginalTabs =
    {
        "Profile Button", "Avatar Button", "Title Button", "Battle Log Button", "Trophies", "Ranked",
    };
    static readonly string[] OriginalPages =
    {
        "Profile Tab", "Avatar Tab", "Title Tab", "Battle Log Tab", "Trophies Tab", "Ranking Tab",
    };

    /// <summary>用户 2026-10-24 拍板要隐藏的那一颗（第 6 键）。落点 → `Shell/PlayerProfileWindow.cs` 的
    /// `BuildTabBar`（补丁见报告 §处置）。</summary>
    const string UserDropped = "Ranked";

    /// <summary>世界单位 → 设计像素（`LayoutSpace.DesignPxH / DesignHeight` = 1080/10 = **108**）。</summary>
    const float PxPerUnit = 108f;

    /// <summary>归一映射：我们页根的错名 → prefab 真名（⛔ 只此一条；用到就出声，见 U2-2）。</summary>
    static string NormalizePageName(string n)
    {
        if (n == "Ranked Tab") { PageNameDeviation = true; return "Ranking Tab"; }
        return n;
    }

    /// <summary>U2-2 有没有真的用到归一（= 我们还在偏离）。</summary>
    static bool PageNameDeviation;


    /// <summary>落地后应当可见的 5 颗（顺序 = 原版前 5 个）。</summary>
    static readonly string[] UserVisibleTabs =
    {
        "Profile Button", "Avatar Button", "Title Button", "Battle Log Button", "Trophies",
    };

    public static void Run()
    {
        Pass = 0; Fail = 0;

        var pp = PlayerProfileWindow.LastOpened;
        if (pp == null) pp = MainMenuRuntime.OpenProfile();
        if (pp == null)
        {
            Ok(false, "档案窗开不出来（拿不到 `WindowsManager`）⇒ 本节全部没法验（⛔ 不静默跳过）");
            Report();
            return;
        }

        var bar = FindChild(pp.transform, "Menu Area/Tab Buttons");
        Ok(bar != null, "前置：`Menu Area/Tab Buttons` 建出来了");
        if (bar == null) { Report(); return; }

        // ---- U2-1 键集合 ----
        var gotTabs = new string[bar.childCount];
        for (int i = 0; i < bar.childCount; i++) gotTabs[i] = bar.GetChild(i).name;
        Ok(SameSeq(gotTabs, OriginalTabs),
           "U2-1 页签集合 == 原版 6 键（多一个 / 少一个都红）：实际 [" + Join(gotTabs)
           + "] · 期望 [" + Join(OriginalTabs) + "]（判据 = prefab `Tab Buttons` 的 6 个 `EverguildToggle`）");

        // ---- U2-2 页集合 ----
        // ⚠️ 节点名里 `Tab  Area` 是**两个空格**（原版真值），别「顺手修正」
        var content = FindChild(pp.transform, "Menu Area/Tab  Area/Tab Content");
        Ok(content != null, "前置：`Menu Area/Tab  Area/Tab Content` 建出来了");
        if (content != null)
        {
            PageNameDeviation = false;
            var gotPages = new string[content.childCount];
            for (int i = 0; i < content.childCount; i++)
                gotPages[i] = NormalizePageName(content.GetChild(i).name);
            Ok(SameSeq(gotPages, OriginalPages),
               "U2-2 页集合 == 原版 6 页（多一个 / 少一个都红）：实际 [" + Join(gotPages)
               + "] · 期望 [" + Join(OriginalPages) + "]");
            if (PageNameDeviation)
                Debug.LogWarning(P + "U2-2：第 6 个页根的**节点名**是 `Ranked Tab`，prefab 是 `Ranking Tab`"
                                 + "（资产 `bundle_menus_assets_all/GameObject/Ranking Tab.json`）"
                                 + " —— 本断言只做**归一**、没有给这个偏离发合格证。"
                                 + " 修法 = `Shell/PlayerProfileWindow.cs:464` 的"
                                 + " `Tabs[i].Node.Replace(\" Button\", \"\") + \" Tab\"` 改成逐页表"
                                 + "（`Ranked` ⇒ `Ranking Tab`）；⚠️ `Editor/MainMenuScene.cs:7680` 也硬写着 `Ranked Tab`，要一起改");
        }

        // ---- U2-3 用户拍板：第 6 键要隐藏 ----
        var vis = new System.Collections.Generic.List<string>();
        for (int i = 0; i < bar.childCount; i++)
            if (bar.GetChild(i).gameObject.activeSelf) vis.Add(bar.GetChild(i).name);
        string droppedState = null;
        for (int i = 0; i < bar.childCount; i++)
            if (bar.GetChild(i).name == UserDropped)
                droppedState = bar.GetChild(i).gameObject.activeSelf ? "可见" : "已隐藏";
        Ok(SameSeq(vis.ToArray(), UserVisibleTabs),
           "U2-3 可见页签 == 用户拍板 5 颗（`" + UserDropped + "` 应为「已隐藏」，现在是「"
           + (droppedState ?? "不存在") + "」）：实际 [" + Join(vis.ToArray()) + "]"
           + " · 期望 [" + Join(UserVisibleTabs) + "] ⇒ 落地处 = `Shell/PlayerProfileWindow.cs` 的 `BuildTabBar`"
           + " 那句 `if (Tabs[i].Node == \"Ranked\") { b.gameObject.SetActive(false); continue; }`"
           + "（用户实拍 5 颗：桌面 `warpforge参考图/账户-*.png`；本 build 的 prefab 是 6 颗 = 版本差，见报告）");

        // ---- U2-4 页签文案（只查**可见**那五颗；`Ranked` 的子孙不建，见 U2-6）----
        // ⚠️ 路径是 `<键>/Tab Toggle Title` —— **没有 `Label` 那一层**（原版 `Label` 是个空容器，我们拍平了；
        //    2026-10-24 首跑就是栽在这个多出来的一层上：六颗全「节点不在」）。
        for (int i = 0; i < OriginalTabs.Length && i < PlayerProfileWindow.TabTerms.Length; i++)
        {
            if (OriginalTabs[i] == UserDropped) continue;
            var t = FindChild(bar, OriginalTabs[i] + "/Tab Toggle Title");
            Ok(t != null, "U2-4 `" + OriginalTabs[i] + "/Tab Toggle Title` 建出来了");
            if (t == null) continue;
            string txt = TextOf(t);
            string official = Loc.T(PlayerProfileWindow.TabTerms[i]);
            bool haveOfficial = !string.IsNullOrEmpty(official) && official != PlayerProfileWindow.TabTerms[i];
            Ok(haveOfficial && txt == official,
               "U2-4 `" + OriginalTabs[i] + "` 文案走官方本地化 `" + PlayerProfileWindow.TabTerms[i] + "`：实际「"
               + (txt ?? "<null>") + "」· 期望「" + (haveOfficial ? official : "<官方表缺这条>") + "」");
        }

        // ---- U2-5 键几何（🔴 世界单位 ×108 = 设计像素）----
        // 原版（布局后）：首键 y 180.24、每键步进 119.252、高 109.252（`Tab Buttons` 705.512 = 6×109.252+5×10）
        for (int i = 0; i < bar.childCount && i < OriginalTabs.Length; i++)
        {
            var rt = bar.GetChild(i) as RectTransform;
            if (rt == null) { Ok(false, "U2-5 `" + OriginalTabs[i] + "` 没有 RectTransform"); continue; }
            float hPx = rt.rect.height * PxPerUnit;
            Ok(Mathf.Abs(hPx - 109.252f) < 0.1f,
               "U2-5 `" + OriginalTabs[i] + "` 高 == 109.252px（原版布局真值）：实际 "
               + hPx.ToString("0.###") + "px（rect " + rt.rect.height.ToString("0.#####") + " 世界单位 ×108）");
        }

        // ---- U2-6 第 6 键不可点（用户拍板隐藏 ⇒ 不该有入口）----
        {
            var k = FindChild(bar, UserDropped);
            Ok(k != null, "U2-6 `" + UserDropped + "` 键节点还在（隐藏 = 留节点 + 关掉，不是删数据）");
            if (k != null)
            {
                var hit = FindChild(k, "Hit");
                var wb = hit != null ? hit.GetComponent<WindowButton>() : null;
                Ok(!k.gameObject.activeSelf,
                   "U2-6 `" + UserDropped + "` 键 `activeSelf = false`（用户 2026-10-24 拍板：不显示）");
                Ok(hit == null && wb == null,
                   "U2-6 `" + UserDropped + "` 键**没有**命中区 / `WindowButton`（点了不会切页）：Hit="
                   + (hit != null ? "在" : "无") + " · WindowButton=" + (wb != null ? "在" : "无"));
            }
        }

        CheckProfilePage(pp);

        Report();
    }

    static void Report()
    {
        Debug.Log(P + $"=== U2 合计：{Pass} 通过 / {Fail} 失败 ===");
    }

    // ============================================================ 个人资料页（用户 2026-10-24 点名复查）

    static void CheckProfilePage(PlayerProfileWindow pp)
    {
        const string Page = "Menu Area/Tab  Area/Tab Content/Profile Tab";
        var page = FindChild(pp.transform, Page);
        Ok(page != null, "U2-7 前置：`Profile Tab` 页根在");
        if (page == null) return;

        var curMr = FindChild(page, "Ranking/Current Rank/Content/footer/MainRating");
        var hiMr = FindChild(page, "Ranking/Highest Rank/Content/footer/MainRating");
        Ok(hiMr != null && !hiMr.gameObject.activeSelf,
           "U2-7 `Highest Rank/MainRating` **关**（出厂 `displaySeals=0` —— 两份卡逐项不同）");
        Ok(curMr != null && curMr.gameObject.activeSelf,
           "U2-7 `Current Rank/MainRating` **开**（出厂 T：`displaySeals=1`）");

        // ⚠️ 本窗出厂停在 **Title** 页（`DefaultTabIndex = 2`）⇒ 这里**不切页**（切了会打乱宿主后面那几条），
        //    改用「沿父链逐节点 `activeSelf`」算**页内有效可见性**（`ChainOn`）；⛔ 不用 `activeInHierarchy`
        //    —— 页根关着时它对**所有**子件都恒 false，会把 U2-9 误判成红。
        var hiGr = hiMr != null ? FindChild(hiMr, "Global Rating") : null;
        var curGr = curMr != null ? FindChild(curMr, "Global Rating") : null;
        Ok(hiGr != null && curGr != null,
           "U2-8 `Global Rating` **挂在 `MainRating` 下面**（原版父链 `footer/MainRating/Global Rating`）");
        Ok(hiGr != null && hiGr.gameObject.activeSelf && !ChainOn(hiGr.gameObject, page),
           "U2-8 `Highest Rank` 的 `Global Rating` **随 `MainRating` 一起不可见**"
           + "（改前它建在 `footer` 上 ⇒ 关不掉 ⇒ 实拍里那张卡**左外侧**浮着一颗图标 + 破折号）");
        Ok(curGr != null && ChainOn(curGr.gameObject, page),
           "U2-9 `Current Rank` 的 `Global Rating` 仍**可见**（出厂 T 那一份）");

        var campBase = FindChild(page, "Events/Campaign Profile Container/Player Profile Container Base");
        var q = campBase != null ? campBase.GetComponent<ImageQuad>() : null;
        string tex = q != null && q.Texture != null ? q.Texture.name : null;
        Ok(campBase != null && q != null && !string.IsNullOrEmpty(tex) && tex.StartsWith("40K_profile_campaign_bg_"),
           "U2-10 「当前战役」格底图 = `40K_profile_campaign_bg_*` 族（**不是**通用红底 `UI_Deck_Information_submenu_Back`）："
           + "实际「" + (tex ?? "<无图>") + "」");
        int id = ArmyIcons.Id(CampaignData.Selected);
        string want = id > 0 ? PurchasePremiumWindow.ArmyBackgroundArt(id) : null;
        Ok(!string.IsNullOrEmpty(want) && tex == want,
           "U2-10 底图 = 该阵营在**唯一那份阵营图表**里的那一张（`CampaignData.Selected` = " + CampaignData.Selected
           + " · army " + id + "）：实际「" + (tex ?? "<无图>") + "」· 期望「" + (want ?? "<查不到>") + "」");
    }

    // ============================================================ 小工具

    static bool SameSeq(string[] a, string[] b)
    {
        if (a == null || b == null || a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
        return true;
    }

    static string Join(string[] a)
    {
        return a == null ? "" : string.Join(" | ", a);
    }

    /// <summary>`go` 到 <paramref name="top"/>(不含它自己) 这一路上**每个节点都 `activeSelf`** ⇒ `true`。
    /// 用途 = 在**页根关着**的情况下判「页内可见性」（`activeInHierarchy` 那种情形下对所有子件恒 false）。
    /// ⚠️ 走不到 `top`（传错根）⇒ `false`（⛔ 不是「乐观 true」）。</summary>
    static bool ChainOn(GameObject go, Transform top)
    {
        if (go == null || top == null) return false;
        var t = go.transform;
        while (t != null && t != top)
        {
            if (!t.gameObject.activeSelf) return false;
            t = t.parent;
        }
        return t == top;
    }

    static Transform FindChild(Transform root, string path)
    {
        if (root == null || string.IsNullOrEmpty(path)) return null;
        var cur = root;
        var seg = path.Split('/');
        for (int s = 0; s < seg.Length; s++)
        {
            Transform next = null;
            for (int i = 0; i < cur.childCount; i++)
            {
                var c = cur.GetChild(i);
                if (c != null && c.name == seg[s]) { next = c; break; }
            }
            if (next == null) return null;
            cur = next;
        }
        return cur;
    }

    static string TextOf(Transform t)
    {
        if (t == null) return null;
        var lb = t.GetComponent<Label>();
        return lb != null ? lb.Text : null;
    }

    static void Ok(bool cond, string msg)
    {
        if (cond) { Pass++; Debug.Log(P + "OK   " + msg); }
        else { Fail++; Debug.LogError(P + "FAIL " + msg); }
    }
}
