// ProfileWindowAssertE12.cs — 执行代理 `E12`（2026-10-24）的断言宿主
//
// 本文件**只放断言**（⛔ 不改产品行为、不建场景、不进任何宿主的 `RunCore` 正文）。
//
// ============================ 主对话怎么挂 ============================
// 在 `Editor/MainMenuScene.cs` 的 `RunCore` 里、**档案窗那一段之后**（也就是
// `var tab = pp.Page(WindowTabType.ProfileInfo) as ProfileTab;` 那一片断言的收尾处）加三行：
//
//     ProfileWindowAssertE12.Run();
//     pass += ProfileWindowAssertE12.Pass;
//     fail += ProfileWindowAssertE12.Fail;
//
// ⚠️ **也可以挂在 `Editor/ShellScene.cs`**（它同样有 `WindowsManager` + 档案窗那一段）——
//    本文件**自己开窗**（`PlayerProfileWindow.LastOpened` 拿不到就 `MainMenuRuntime.OpenProfile()`），
//    所以唯一的要求是「宿主已经建好外壳（有 `WindowsManager`）」，**不依赖宿主先开档案窗**。
// ⚠️ 开窗拿不到（`WindowsManager` 不在）时：本文件**记一条 FAIL 并出声**（⛔ 不静默跳过）。
//
// ============================ 覆盖哪八件（判据逐条写在断言消息里） ============================
//  P0-1 `Events` 三格的标题**逐格不同**、且各走官方词条（原版三格：战将精通 / 最高锻造厂等级 / 当前战役）
//  P0-2 `Highest Rank` 卡里补的「传奇点数」两件（`legendary title` + `Legendarey Counter`）在、可见、值为 0
//  P0-3 标签本地化（`Current Rank` / `Highest Rank` / `Player id` / `Level` / `Victories` / `Trophies` / `Legendary Points`）
//  P0-4 个人资料点头像 ⇒ 切到 `Avatar` 页
//  P1-7 头像页格子里那些 `Avatar Name` **建了但关着**（原版 `m_IsActive = F`）
//  P1-8 改名校验（3~25 字 · 去富文本 · 同名禁改）+ **顶栏名字跟着刷**
//  P1-10 排名页四处白块（`Rect(..., null, ...)` ⇒ `CardArt.Solid()`）已去掉
//  P1-9 **不在本文件里** —— 战斗记录那格 Mode 的显示点在 `Shell/MatchLogRow.cs`，那份属于 `E13`（本批不能碰）
//       ⇒ 只有待办、没有断言（报告里写清了要改的那一行与官方词条键）。
//
//  ⚠️ **期望值从哪来（防自证）**：标签的期望值一律取 `Loc.OfficialCurrent(key)`（**官方 I2 表**，
//   `Resources/official_loc_13lang.json`），⛔ 不是拿 `ProfileTab` 自己的常量回读；
//  长度/校验的期望值是**反编译里的字面量**（`0x19` = 25 · `len < 3`）。
// ==================================================================
using CardPresentation;
using UnityEngine;

public static class ProfileWindowAssertE12
{
    const string P = "[E12] ";

    /// <summary>本类这一趟的通过 / 失败数（主对话想并进宿主的总数就加这两格）。</summary>
    public static int Pass { get; private set; }
    public static int Fail { get; private set; }

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
        var prof = pp.Page(WindowTabType.ProfileInfo) as ProfileTab;
        var avatar = pp.Page(WindowTabType.ProfileAvatar) as AvatarTab;
        var ranked = pp.Page(WindowTabType.ProfileRanking) as RankedTab;
        Ok(prof != null, "前置：`Profile Info` 页是 `ProfileTab`（类名照原版）");
        Ok(avatar != null, "前置：`Avatar` 页是 `AvatarTab`");
        Ok(ranked != null, "前置：`Ranking` 页是 `RankedTab`");

        // ---------- P0-1 · Events 三格标题 ----------
        CheckEventTitles(prof);

        // ---------- P0-2 · Highest Rank 里的「传奇点数」 ----------
        CheckLegendaryPoints(prof);

        // ---------- P0-3 · 标签本地化 ----------
        CheckLabels(prof);

        // ---------- P1-10 · 排名页的白块 ----------
        CheckRankedWhiteBlocks(ranked);

        // ---------- P0-4 + P1-7 · 点头像切页 → 头像页格内名字 ----------
        CheckAvatarTabSwitch(pp, prof, avatar);

        // ---------- P1-8 · 改名校验 + 顶栏刷新（放最后：它会改 `ProfileData.PlayerName`，收尾复原） ----------
        CheckRename(prof);

        Report();
    }

    static void Report()
    {
        Debug.Log(P + $"=== E12 合计：{Pass} 通过 / {Fail} 失败 ===");
    }

    // ============================================================ P0-1

    static void CheckEventTitles(ProfileTab prof)
    {
        if (prof == null) { Ok(false, "P0-1：没有 `ProfileTab` ⇒ 验不了 `Events` 三格标题"); return; }
        // ⚠️ 节点名里的**两个空格**是原版真值（`Warlord  Mastery Container`），别「顺手修正」
        string[] containers = { "Warlord  Mastery Container", "Forge Profile Container", "Campaign Profile Container" };
        string[] terms = { ProfileTab.EvTermWarlord, ProfileTab.EvTermForge, ProfileTab.EvTermCampaign };
        string[] cname = { "战将格", "锻造厂格", "战役格" };
        var got = new string[3];
        for (int i = 0; i < 3; i++)
        {
            var t = FindChild(prof.transform, "Events/" + containers[i] + "/Title");
            Ok(t != null, "P0-1：`Events/" + containers[i] + "/Title` 建出来了");
            got[i] = TextOf(t);
            string want = Loc.OfficialCurrent(terms[i]);
            Ok(!string.IsNullOrEmpty(want) && got[i] == want,
               "P0-1：" + cname[i] + "标题 = 官方词条 `" + terms[i] + "` 的当前语档值「" + want + "」（实得「" + got[i] + "」）");
        }
        // 🧨 改坏法：把 `BuildEvents` 里 Forge 那一格再写成 `warlord` 选标题 ⇒ 下面这条立刻红
        Ok(got[0] != got[1] && got[1] != got[2] && got[0] != got[2],
           "P0-1：三格标题**两两不同**（实得「" + got[0] + "」/「" + got[1] + "」/「" + got[2] + "」；"
           + "改前第 2 格与第 1 格同字）");
    }

    // ============================================================ P0-2

    static void CheckLegendaryPoints(ProfileTab prof)
    {
        if (prof == null) { Ok(false, "P0-2：没有 `ProfileTab` ⇒ 验不了「传奇点数」那一行"); return; }
        const string root = "Ranking/Highest Rank/Content/";
        var title = FindChild(prof.transform, root + "legendary title");
        var counter = FindChild(prof.transform, root + "Legendarey Counter");
        var value = FindChild(prof.transform, root + "Legendarey Counter/Rating Text/Individual rating value");
        var icon = FindChild(prof.transform, root + "Legendarey Counter/Rating Text/Main Icon");

        Ok(title != null, "P0-2：`Highest Rank/Content/legendary title` 建出来了（原来**根本没建**）");
        Ok(counter != null, "P0-2：`Highest Rank/Content/Legendarey Counter` 建出来了（⚠️ 原版拼写就是 `Legendarey`）");
        Ok(icon != null, "P0-2：counter 里的 `Main Icon` 建出来了（原版 `40k_UI_icon_ranked_Skirmish`）");
        Ok(title != null && title.gameObject.activeSelf, "P0-2：`legendary title` **可见**（非传奇支它也 ON）");
        Ok(counter != null && counter.gameObject.activeSelf, "P0-2：`Legendarey Counter` **可见**（同上）");

        string wantTitle = Loc.OfficialCurrent(ProfileTab.LegendaryRankTerm);   // `Legendary Points` / 传奇点数
        Ok(!string.IsNullOrEmpty(wantTitle) && TextOf(title) == wantTitle,
           "P0-2：标题走官方词条 `" + ProfileTab.LegendaryRankTerm + "`「" + wantTitle + "」（实得「" + TextOf(title) + "」）");
        Ok(TextOf(value) == ProfileTab.LegendaryPointsValue,
           "P0-2：值是 `" + ProfileTab.LegendaryPointsValue + "`（本地没有传奇点数 ⇒ 0；prefab 那颗 `'32'` 是示例数据）"
           + "（实得「" + TextOf(value) + "」）");

        // 互斥支没被碰坏：`Legendary Display Profile` 仍然关着（判据 `ProfileRankingSection.Initialize:103-111`）
        var leg = FindChild(prof.transform, "Ranking/Legendary Display Profile");
        Ok(leg != null && !leg.gameObject.activeSelf,
           "P0-2：`Legendary Display Profile` **仍然关着**（判据②：与 `Highest Rank` 互斥，我们走非传奇支）");
    }

    // ============================================================ P0-3

    static void CheckLabels(ProfileTab prof)
    {
        if (prof == null) { Ok(false, "P0-3：没有 `ProfileTab` ⇒ 验不了标签"); return; }

        CheckTerm(prof.transform, "Ranking/Current Rank/Content/Title", ProfileTab.RankTermCurrent, null, "排名卡标题");
        CheckTerm(prof.transform, "Ranking/Highest Rank/Content/Title", ProfileTab.RankTermHighest, null, "排名卡标题");

        // `Player id: {0}` / `玩家ID：{0}`（值 = 空态 `-`）
        CheckTerm(prof.transform, "PlayerId/playerIdText", "PlayerProfile/PlayerIdDisplay", "-", "PlayerId 那一行");

        // `Events/*/Level`：战将格印**胜利数**、另两格印**等级**（参考图 `胜利 : 487` / `等级 : 32`）
        CheckTerm(prof.transform, "Events/Warlord  Mastery Container/Level",
                  ProfileTab.EvLevelTermWarlord, "-", "战将格的胜利数那一行");
        CheckTerm(prof.transform, "Events/Forge Profile Container/Level",
                  ProfileTab.EvLevelTermOther, "-", "锻造厂格的等级那一行");
        CheckTerm(prof.transform, "Events/Campaign Profile Container/Level",
                  ProfileTab.EvLevelTermOther, "-", "战役格的等级那一行");

        // 奖杯行底下那行（`Victories text`，装的其实是 `Trophies`）
        CheckTerm(prof.transform, "Ranking/Legendary Display Profile/Content/Player Profile Ranked Trophies Gold/Victories text",
                  ProfileTab.TrophiesTerm, null, "奖杯行文案");

        // 三格 `Title` 之外，`Level` 行**不该再是空串**（改前是 ""）—— 这一条防「词条没接上、静默空着」
        var lvl = TextOf(FindChild(prof.transform, "Events/Forge Profile Container/Level"));
        Ok(!string.IsNullOrEmpty(lvl), "P0-3：`Events/Forge Profile Container/Level` 有字（改前是空串）");
    }

    /// <summary>取某节点的字，与「官方词条（+可选 `string.Format` 的值）」比。
    /// ⚠️ 期望值 = `Loc.OfficialCurrent(key)`（官方 I2 表），⛔ 不读产品侧常量（防自证）。</summary>
    static void CheckTerm(Transform root, string path, string key, string formatArg, string what)
    {
        var t = FindChild(root, path);
        string want = Loc.OfficialCurrent(key);
        if (formatArg != null) want = string.Format(want, formatArg);
        string got = TextOf(t);
        Ok(!string.IsNullOrEmpty(want) && got == want,
           "P0-3：" + what + "（`" + path + "`）= 官方词条 `" + key + "`「" + want + "」（实得「" + got + "」）");
    }

    // ============================================================ P1-10

    static void CheckRankedWhiteBlocks(RankedTab page)
    {
        if (page == null) { Ok(false, "P1-10：没有 `RankedTab` ⇒ 验不了白块"); return; }
        // 白块的机制：`ProfilePage.Rect(art: null, …)` 会落到 `CardArt.Solid()`（**纯白图**）+ 缺省白 tint。
        // ⇒ 修法（2026-10-24 E12）= 那两格仍建 quad，但 tint **全透明**：
        //    · 画面：什么都不画（原版那儿是阵营图，空态没有阵营分 ⇒ 无可画）；
        //    · 结构：`ImageQuad` **留着** —— 宿主 `MenuCheck.NodeRectPx` 对「无 `Label` 也无 `ImageQuad`」的节点
        //      返回 false，而 `Editor/MainMenuScene.cs:7687`/`:7751` 正是拿 `icon` 节点量矩形
        //      ⇒ 删成裸节点会让那两条**与画面无关地变红**（这一条断言同时守住「别退回裸节点」）。
        int iconNodes = 0, iconQuads = 0, opaqueQuads = 0;
        var all = page.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null || all[i].name != "icon") continue;
            iconNodes++;
            var q = all[i].GetComponent<ImageQuad>();
            if (q == null) continue;
            iconQuads++;
            if (q.Tint.a > 0.001f) opaqueQuads++;
        }
        Ok(iconNodes > 0, "P1-10：排名页里那些 `icon` 节点还在（实建 " + iconNodes + " 个）");
        Ok(iconQuads == iconNodes,
           "P1-10：`icon` 节点上**仍带 `ImageQuad`**（" + iconQuads + "/" + iconNodes
           + " 个 —— 宿主那两条量矩形的断言靠它，⛔ 别删成裸节点）");
        Ok(opaqueQuads == 0,
           "P1-10：那些 quad **一个都不不透明**（`Tint.a == 0`）—— 改前是 `CardArt.Solid()` 的白图，"
           + "就是实拍 `14_档案窗_排名页.png` 里那四处白块（实得不透明 " + opaqueQuads + " 个）");
    }

    // ============================================================ P0-4 + P1-7

    static void CheckAvatarTabSwitch(PlayerProfileWindow pp, ProfileTab prof, AvatarTab avatar)
    {
        if (prof == null || pp == null) { Ok(false, "P0-4：没有 `ProfileTab`/宿主窗 ⇒ 验不了切页"); return; }

        // 记下进来时停在哪一页 —— 收尾**原样还回去**（别给宿主留一个「停在别的页」的状态：
        // 宿主后面那一串断言各自假定自己切过页，我们插在中间的改动必须可逆）
        var before = pp.CurrentTab;
        pp.ChangeTab(WindowTabType.ProfileInfo);           // 先回到本页（`Profile` 的命中区就在这一页上）
        prof.GoToAvatarTab();
        Ok(pp.CurrentTab == WindowTabType.ProfileAvatar,
           "P0-4：点 `Profile` 页的头像 ⇒ `CurrentTab` 变成 `ProfileAvatar`（原版 `ProfileTab.<Start>b__9_0` → "
           + "`ChangeTab<AvatarTab>`；实得 " + pp.CurrentTab + "）");
        Ok(avatar != null && avatar.gameObject.activeSelf, "P0-4：`Avatar` 页真的显示出来了（`activeSelf = true`）");
        // 左栏高亮必须跟着走（走 `tabButtons.Click` 而不是直接 `ChangeTab` 的理由）
        Ok(pp.tabButtons != null && pp.tabButtons.CurrentVisualIndex == 1,
           "P0-4：左栏高亮跟到第 2 键（Avatar）—— 走的是 `tabButtons.Click(1)` 那条路，不是裸 `ChangeTab`"
           + (pp.tabButtons != null ? "（实得 " + pp.tabButtons.CurrentVisualIndex + "）" : "（`tabButtons` 为 null）"));

        // ---------- P1-7 · 格子里的名字（原版 `m_IsActive = F`） ----------
        if (avatar != null)
        {
            Ok(avatar.CellNameCount > 0,
               "P1-7：头像格子里**建了** `Avatar Name`（实建 " + avatar.CellNameCount + " 颗 —— 这条与下一条合起来才排得出"
               + "「建了但关着」，光看截图分不清「没建」和「建了关着」）");
            Ok(avatar.ActiveCellNameCount == 0,
               "P1-7：那些 `Avatar Name` **一颗都不激活**（原版 prefab `m_IsActive = F`、运行期只 `set_text`；"
               + "参考图 `账户-头像参考图.png` 格子里也没有名字；实得激活 " + avatar.ActiveCellNameCount + " 颗）");
        }

        // 收尾：还回进来时那一页（`None` 那种异常态才退回 `Profile` 页）
        var back = before == WindowTabType.None ? WindowTabType.ProfileInfo : before;
        pp.ChangeTab(back);
        Ok(pp.CurrentTab == back, "P0-4：验完把页签**还回进来时那一页**（`" + back + "`；实得 " + pp.CurrentTab + "）");
    }

    // ============================================================ P1-8

    static void CheckRename(ProfileTab prof)
    {
        if (prof == null) { Ok(false, "P1-8：没有 `ProfileTab` ⇒ 验不了改名"); return; }

        // ---- 纯函数那层：`StripRichText`（原版 `TextMessageValidationTools.RemoveRichTextTags` 的超集）----
        Ok(ProfileTab.StripRichText("<b>ab</b>c") == "abc",
           "P1-8：`StripRichText(\"<b>ab</b>c\")` = `abc`（实得「" + ProfileTab.StripRichText("<b>ab</b>c") + "」）");
        Ok(ProfileTab.StripRichText("<color=#FF0000>x</color>") == "x",
           "P1-8：带参数的标签也去掉（`<color=#FF0000>x</color>` ⇒ `x`）");
        Ok(ProfileTab.StripRichText("A < B") == "A < B",
           "P1-8：`<` 后面带空白的**不删**（正文里的数学小于号；实得「" + ProfileTab.StripRichText("A < B") + "」）");

        // ---- 长度两条：25 / 3（反编译 `if (0x18 < len) Substring(0, 0x19)` · `if (len < 3) interactable = false`）----
        Ok(ProfileTab.MaxNameLen == 25, "P1-8：`MaxNameLen` = 25（反编译 `Substring(0, 0x19)`；实得 " + ProfileTab.MaxNameLen + "）");
        Ok(ProfileTab.MinNameLen == 3, "P1-8：`MinNameLen` = 3（反编译 `len < 3` ⇒ 钮不可点；实得 " + ProfileTab.MinNameLen + "）");

        string orig = ProfileData.PlayerName;
        var bar = TopBar.LastBuilt;
        Ok(bar != null && bar.PlayerName != null, "P1-8：拿得到顶栏那份 `Parts`（`TopBar.LastBuilt.PlayerName`）");

        // 收尾必须能复原：原名的长度得 ≥ `MinNameLen`，否则 `UiSetName(orig)` 会被「太短」那条拒掉 ⇒
        // 自检就把玩家的名字留在测试串上了。够不着就**别动**，只出声（⛔ 不静默留脏状态）。
        bool canMutate = !string.IsNullOrEmpty(orig) && orig.Length >= ProfileTab.MinNameLen;
        Ok(canMutate, "P1-8：当前名长度 ≥ " + ProfileTab.MinNameLen + " ⇒ 改名那几条能验、且能复原（实得「" + orig + "」）");
        if (!canMutate) return;

        try
        {
            // ① 太短 ⇒ 不改
            prof.UiSetName("ab");
            Ok(ProfileData.PlayerName == orig,
               "P1-8：2 个字的名字**改不进去**（原版 `len < 3` ⇒ 钮是灰的；实得「" + ProfileData.PlayerName + "」）");

            // ② 去富文本 + ③ 顶栏跟着刷
            prof.UiSetName("<b>E12Check</b>");
            Ok(ProfileData.PlayerName == "E12Check",
               "P1-8：`<b>E12Check</b>` ⇒ 存成 `E12Check`（富文本被去掉；实得「" + ProfileData.PlayerName + "」）");
            if (bar != null) TopBar.RefreshPlayerNameIfChanged(bar);
            Ok(bar != null && bar.PlayerName != null && bar.PlayerName.Text == "E12Check",
               "P1-8：顶栏名字**跟着刷**（`TopBar.RefreshPlayerNameIfChanged` 之后那块字 = 新名；实得「"
               + (bar != null && bar.PlayerName != null ? bar.PlayerName.Text : "<没有那颗字>") + "」）");

            // ④ 同名禁改（原版 `interactable = … && text != 当前名`）
            prof.UiSetName("E12Check");
            Ok(ProfileData.PlayerName == "E12Check",
               "P1-8：再提交**同一个名字** ⇒ 不变（实得「" + ProfileData.PlayerName + "」）");

            // ⑤ 超长截到 25
            string longName = new string('Z', 30);
            prof.UiSetName(longName);
            Ok(ProfileData.PlayerName != null && ProfileData.PlayerName.Length == ProfileTab.MaxNameLen
               && ProfileData.PlayerName == new string('Z', ProfileTab.MaxNameLen),
               "P1-8：30 个字的名字**截到 25**（原版 `Substring(0, 0x19)`；实得 " +
               (ProfileData.PlayerName != null ? ProfileData.PlayerName.Length + " 个字" : "null") + "）");
        }
        finally
        {
            // 收尾：改回原来那个名字（⛔ 别把自检写进玩家的名字里），并顺手再验一次「顶栏跟着刷」
            prof.UiSetName(orig);
            if (bar != null) TopBar.RefreshPlayerNameIfChanged(bar);
            Ok(ProfileData.PlayerName == orig,
               "P1-8：收尾复原成原来的名字（实得「" + ProfileData.PlayerName + "」）");
            Ok(bar == null || bar.PlayerName == null || bar.PlayerName.Text == orig,
               "P1-8：复原后顶栏那块字也回到原名（实得「"
               + (bar != null && bar.PlayerName != null ? bar.PlayerName.Text : "<没有那颗字>") + "」）");
        }
    }

    // ============================================================ 小工具

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
