// ProfileAssertU2b.cs — 执行代理 `W13`（2026-10-24 · 账户窗 `U2` 第二批）的断言宿主
//
// 本文件**只放断言**（⛔ 不改产品行为、不建场景、不进任何宿主的 `RunCore` 正文）。
//
// ============================ 主对话怎么挂 ============================
// 在 `Editor/MainMenuScene.cs` 的 `RunCore` 里（**档案窗那一段断言之后** —— 本文件自己开窗、
// 但要求宿主已经建好外壳 / `WindowsManager`）加三行：
//
//     ProfileAssertU2b.Run();
//     _sink.Pass += ProfileAssertU2b.Pass; _sink.Fail += ProfileAssertU2b.Fail;
//
// ⚠️ 本文件**自己开档案窗**（`PlayerProfileWindow.LastOpened` 拿不到就 `MainMenuRuntime.OpenProfile()`）；
//    开不出来 ⇒ 记一条 FAIL 并出声（⛔ 不静默跳过）。
// ⚠️ 它**会临时改** `ProfileData.TitleIndex` / `RankedTab.Top4Data`，**在 `finally` 里原样放回**
//    （批处理下 `ProfileData` 不写盘 —— 见 `ProfileData` 的「本地存档」那一节）。
//
// ============================ 覆盖哪五件（判据逐条写在断言消息里） ============================
//  V1 头衔空态：没装备过 ⇒ **默认装备清单第 1 条**（原版开局必有装备项，服务器下发；本地用已有数据兜）
//  V2 `Top4` 两支：**无数据 ⇒ 四格整格不画**（`SetActive(false)`，⛔ 不留空框/双箭头/破折号）；
//                  **有数据 ⇒ 四格照原版画**（阵营徽记 + 大分 + 最高分）
//  V3 战斗记录页右上那颗自建钮：**整颗落在面板可见轮廓之内**（≥ 138）、**不压列表**（≤ 162.84）、
//     且落在 **X 关窗钮那一条水平带**（118.92..194.52）里
//  V5 头像页格子里的 `Avatar Name`：**建了但全部关着**（原版 `m_IsActive = F` + 运行期只 `set_text`；
//     ✅ 这一件在 `f34ba0a9` 那一版就已经关掉了，本文件把它**钉死**）；
//     **建与不建** = 名字矩形与**原版视口**求交（视口外那几格按 A184/A435 连节点一起不建 —— 15 格里第 3 行那 5 颗）。
//  V6 传奇支 `DivisionImage`：**没有自造子节点 `Body`**、那张 `07-Legend` 的 quad
//     **就在 `DivisionImage` 自己身上**、且尺寸 = `preserveAspect` 内接（330.96² 居中）
//
//  ⚠️ **期望值从哪来（防自证）**：
//   · V3 的 138 / 162.84 / 118.92..194.52 = **实拍量 + prefab 现读**（出处写在每条消息里），
//     ⛔ 不读 `BattleLogTab.PopupButtonRect` 的同名常量去比它自己（那样改坏两边一起变绿）；
//   · V6 的 330.96² / 中心 (955.835, 505.97) = **prefab 的 rect**（`资料/普查产出_0927/档案窗_Profile页.md` §A 第 119 行）
//     + uGUI `preserveAspect` 的语义算出来的字面量，⛔ 不读 `ProfileTab` 的常量；
//   · V2 的分数串是我们**喂进** `Top4Data` 的（证明「有数据那一支真的画了数据」，不是抄实现）。
//   · V5 的视口矩形 654.16, 210.69 → 1680.12, 855.46 = **原版字面量**
//     （`Shell/AvatarTab.cs:151`，同一组四个数 `Editor/MainMenuScene.cs:6760` 已按实读矩形钉过）；
//     ⛔ **不读** `ViewportClip.Resolve` / `MenuDraw.VisibleAbove`（那两条就是被测的那一个闸），
//     求交也在本文件里独立写（`Overlaps`）—— 判据与先例见 `CheckV5` 的注释。
// ==================================================================
using CardPresentation;
using UnityEngine;

public static class ProfileAssertU2b
{
    const string P = "[U2b] ";

    /// <summary>本类这一趟的通过 / 失败数（主对话并进宿主总数就加这两格）。</summary>
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
        var log = pp.Page(WindowTabType.ProfileBattleLog) as BattleLogTab;
        var ranked = pp.Page(WindowTabType.ProfileRanking) as RankedTab;
        Ok(prof != null, "前提：`Profile Info` 页是 `ProfileTab`");
        Ok(avatar != null, "前提：`Avatar` 页是 `AvatarTab`");
        Ok(log != null, "前提：`Battle Log` 页是 `BattleLogTab`");
        Ok(ranked != null, "前提：`Ranking` 页是 `RankedTab`（第 6 颗键藏起来了，但页照建）");

        CheckV1(pp);
        CheckV2(ranked);
        CheckV3(log);
        CheckV5(avatar);
        CheckV6(prof);

        Report();
    }

    static void Report()
    {
        Debug.Log(P + $"=== U2b 合计：{Pass} 通过 / {Fail} 失败 ===");
    }

    // ============================================================ V1 · 头衔空态兜底
    static void CheckV1(PlayerProfileWindow pp)
    {
        var titles = ProfileData.Titles;
        Ok(titles != null && titles.Count > 0,
           "(V1) 前提：称号清单非空（实得 " + (titles != null ? titles.Count : -1) + " 条）");
        if (titles == null || titles.Count == 0) return;

        int keep = ProfileData.TitleIndex;
        try
        {
            // ① 没装备过 ⇒ 兜底成第 1 条
            ProfileData.TitleIndex = -1;
            bool seeded = ProfileTab.EnsureLocalDefaultTitle();
            Ok(seeded && ProfileData.TitleIndex == 0,
               "(V1) 没装备过（−1）⇒ **默认装备清单第 1 条**（实得 `TitleIndex` = " + ProfileData.TitleIndex + "）"
             + "｜🧨 把 `ProfileTab.EnsureLocalDefaultTitle()` 里那句 `ProfileData.TitleIndex = 0` 删掉"
             + "（= 改回「空态」旧写法）⇒ 本条红");
            Ok(ProfileData.TitleName == titles[0].Name,
               "(V1) 兜底后 `TitleName` = 第 1 条的名字「" + titles[0].Name + "」（实得「" + ProfileData.TitleName + "」）"
             + " —— 头衔页左栏那行橙色称号名就是印它");

            // ② 已经装备着 ⇒ 一个字节都不动（⛔ 别无条件写 0）
            const int keepIdx = 5;
            ProfileData.TitleIndex = keepIdx;
            bool again = ProfileTab.EnsureLocalDefaultTitle();
            Ok(!again && ProfileData.TitleIndex == keepIdx,
               "(V1) 已经装备着（" + keepIdx + "）⇒ **一个字节都不动**（实得 " + ProfileData.TitleIndex + "）"
             + "｜🧨 无条件写 0（把「已装备」盖掉）⇒ 本条红");

            // ③ 结构：头衔页的「当前预览」读的就是这同一个值（`TitleTab.OnOpen`）
            var ttab = pp.Page(WindowTabType.ProfileTitle) as TitleTab;
            Ok(ttab != null, "(V1) 前提：`Title` 页实例在");
            if (ttab != null)
            {
                ttab.OnOpen();
                Ok(ttab.PreviewIndex == ProfileData.TitleIndex,
                   "(V1) 头衔页 `PreviewIndex` 跟着 `ProfileData.TitleIndex`（" + ProfileData.TitleIndex
                   + " vs " + ttab.PreviewIndex + "）—— 左栏不会是空的");
            }
        }
        finally
        {
            ProfileData.TitleIndex = keep;
        }
    }

    // ============================================================ V2 · `Top4` 有/无数据两支
    static void CheckV2(RankedTab ranked)
    {
        if (ranked == null) return;
        var keep = RankedTab.Top4Data;
        try
        {
            // ---------- 无数据支（默认就是这一支）----------
            RankedTab.Top4Data = new RankedTab.Top4Entry[0];
            Ok(!RankedTab.HasTop4Data, "(V2) 默认 = **无数据**（`Top4Data` 空 = 服务器没有本地副本）");
            ranked.RebuildTop4();
            for (int i = 1; i <= 4; i++)
            {
                var card = MenuCheck.FindChild(ranked.transform, "#" + i + " FactionScoreBig");
                Ok(card != null,
                   "(V2) 无数据：`#" + i + " FactionScoreBig` **节点在**（结构照原版 prefab，宿主断言按原版矩形量它）");
                Ok(card != null && !card.gameObject.activeSelf,
                   "(V2) 无数据：`#" + i + "` **整格不画**（`SetActive(false)`）—— 原版那格是**四名玩家的大分**，"
                 + "我们没数据就**不留空框 + 双箭头 + 破折号**"
                 + "｜🧨 改回旧写法（四格都亮着、印 `------`）⇒ 本条红");
            }

            // ---------- 有数据支（喂 4 条，看它照原版画）----------
            string factionArt = ArmyIcons.IconName(10);       // `40k_DeckSelection_icon_FactionUM`（= 原版徽记那一族）
            RankedTab.Top4Data = new[]
            {
                new RankedTab.Top4Entry { FactionArt = factionArt, Rating = "1111", MaxRating = "901" },
                new RankedTab.Top4Entry { FactionArt = factionArt, Rating = "2222", MaxRating = "902" },
                new RankedTab.Top4Entry { FactionArt = factionArt, Rating = "3333", MaxRating = "903" },
                new RankedTab.Top4Entry { FactionArt = factionArt, Rating = "4444", MaxRating = "904" },
            };
            Ok(RankedTab.HasTop4Data, "(V2) 喂了 4 条 ⇒ `HasTop4Data` = true");
            ranked.RebuildTop4();
            for (int i = 1; i <= 4; i++)
            {
                var card = MenuCheck.FindChild(ranked.transform, "#" + i + " FactionScoreBig");
                Ok(card != null && card.gameObject.activeSelf,
                   "(V2) 有数据：`#" + i + "` **画出来了**（`SetActive(true)`）"
                 + "｜🧨 恒关（把无数据那一支写死）⇒ 本条红");
                if (card == null) continue;
                string want = new string((char)('0' + i), 4);          // "1111".."4444"
                string got = TextOf(Find(card, "Alliance Rating Display/Individual rating value"));
                Ok(got == want,
                   "(V2) 有数据：`#" + i + "` 的大分 = 喂进去的「" + want + "」（实得「" + got + "」）");
                string gotMax = TextOf(Find(card, "MaxRating/Individual rating value"));
                Ok(gotMax == ("90" + i),
                   "(V2) 有数据：`#" + i + "` 的最高分 = 喂进去的「90" + i + "」（实得「" + gotMax + "」）");
                var iconQ = Find(card, "icon") != null ? Find(card, "icon").GetComponentInChildren<ImageQuad>(true) : null;
                Ok(iconQ != null && iconQ.Texture != null && iconQ.Texture.name == factionArt,
                   "(V2) 有数据：`#" + i + "` 的阵营徽记画的是「" + factionArt + "」（实得「"
                   + (iconQ != null && iconQ.Texture != null ? iconQ.Texture.name : "<null>") + "」）");
            }
        }
        finally
        {
            // ---------- 放回：空数据 + 重建（画面回到「四格不画」）----------
            RankedTab.Top4Data = keep != null ? keep : new RankedTab.Top4Entry[0];
            ranked.RebuildTop4();
            var c1 = MenuCheck.FindChild(ranked.transform, "#1 FactionScoreBig");
            bool visible = c1 != null && c1.gameObject.activeSelf;
            Ok(c1 != null && visible == RankedTab.HasTop4Data,
               "(V2) 收尾：把 `Top4Data` **原样放回**、重建一次，四格的显隐回到放回后的那一支"
             + "（当前 `HasTop4Data` = " + RankedTab.HasTop4Data + "、`#1` 亮着 = " + visible + "）");
        }
    }

    // ============================================================ V3 · 战斗记录页右上那颗钮
    static void CheckV3(BattleLogTab log)
    {
        if (log == null) return;
        var btn = Find(log.transform, "Open Log Popup Button");
        Ok(btn != null, "(V3) 前提：那颗自建钮 `Open Log Popup Button` 在");
        if (btn == null) return;

        var q = btn.GetComponentInChildren<ImageQuad>(true);
        float x1 = 0f, y1 = 0f, x2 = 0f, y2 = 0f;
        bool got = MenuDraw.QuadRectPx(q, out x1, out y1, out x2, out y2) && Sane(x1, y1, x2, y2);
        Ok(got, "(V3) 量得到那颗钮**画出来那块**（实得 " + Box(x1, y1, x2, y2) + "）");
        if (!got) return;

        // ① 面板可见上缘 —— **实拍量**（见 `BattleLogTab.PanelVisibleTop` 的 doc：
        //    `_tmp_view/shots/2026-10-24_第七十四会话/60_battlelog3.png` 六列取样，金边亮线 136.5..138.8）。
        Ok(y1 >= 138f - 0.01f,
           "(V3) 钮的**上沿在面板可见轮廓之内**（y1 = " + F(y1) + " ≥ **138**）"
         + "｜🧨 改回旧写法（`y1 = page.y1 + 4` = 122.92）⇒ 钮骑在红底金边上、上半截在框外 ⇒ 本条红");
        // ② 不压列表：`Matches` 上沿 = page.y1 + 44.0 = **162.84**（`MtPos/MtSz` 那组 prefab 值推出来的，
        //    ⛔ 不读 `BattleLogTab` 的实现常量）。
        Ok(y2 <= 162.84f + 0.01f,
           "(V3) 钮**不压列表**（y2 = " + F(y2) + " ≤ **162.84** = `Matches` 的 prefab 上沿）");
        // ③ 与 X 关窗钮同一水平带（`PlayerProfileWindow` 的 `CloseT/CloseB` = prefab 的 118.92..194.52）。
        Ok(y1 >= 118.92f - 0.01f && y2 <= 194.52f + 0.01f,
           "(V3) 钮整条落在 **X 关窗钮的水平带**里（118.92..194.52；实得 " + F(y1) + ".." + F(y2) + "）");
        // ④ 横向：右上角对齐内容区右缘（原版页根 `ContentR` = 1746.97），整颗在面板里（面板右缘 1824.52）
        Ok(Near(x2, 1746.97f, 0.6f) && x1 <= x2 && x2 <= 1824.52f,
           "(V3) 钮右缘 = 内容区右缘 **1746.97**、整颗在面板内（实得 x " + F(x1) + ".." + F(x2) + "）");
        // ⑤ 字还在（钮高收到 24 之后别把字挤没了）
        string lab = TextOf(Find(btn, "Button Text"));
        Ok(!string.IsNullOrEmpty(lab), "(V3) 钮上的字还在（实得「" + lab + "」）");
    }

    // ============================================================ V5 · 头像页格子里的 `Avatar Name`
    static void CheckV5(AvatarTab avatar)
    {
        if (avatar == null) return;
        var drawer = MenuCheck.FindChild(avatar.transform, "Item Drawer");   // ⚠️ 递归找（它在 `Item Display Panel/Scroll Rect` 底下）
        Ok(drawer != null && drawer.childCount > 0,
           "(V5) 前提：头像网格建出了格子（实得 " + (drawer != null ? drawer.childCount : -1) + " 个）");
        if (drawer == null || drawer.childCount == 0) return;

        int cells = 0, nameNodes = 0, liveNames = 0, expectNames = 0;
        // 几何（**全在设计 px**，与 `ProfilePage.Text` 收的那个 `PxRect` 同一量纲/原点）：
        // · 格子 = **180×180**（`Shell/AvatarTab.cs:164` `CellW/CellH = 180`；那两个是私有常量 ⇒ 按出处写死）
        // · `Avatar Name` 的矩形 = **格子下沿再往下 41.31**（`Shell/AvatarTab.cs:179` `CNmT = 180` / `CNmB = 221.31`，
        //   相对格子左上角 ⇒ 它的上沿 = 格子下沿、高 = 41.31）
        // · 视口 = **原版字面量** `654.16, 210.69 → 1680.12, 855.46`
        //   （`Shell/AvatarTab.cs:151` 的 `VpL/VpT/VpR/VpB`；同一组四个数在
        //    `Editor/MainMenuScene.cs:6760` 已按 `Scroll Rect` 的**实读矩形**钉过一遍）
        const float CellHalf = 90f;                 // 180 ÷ 2
        const float NameBelowCell = 41.31f;         // 221.31 − 180
        var vp = new PxRect(654.16f, 210.69f, 1680.12f, 855.46f);      // 原版 `Scroll Rect` 视口（见上）
        string sampleOut = null;
        for (int i = 0; i < drawer.childCount; i++)
        {
            var cell = drawer.GetChild(i);
            if (cell == null || !cell.name.StartsWith("Avatar Item Small_", System.StringComparison.Ordinal)) continue;
            cells++;

            // ---- 期望：这颗名字**该不该建** = 它的矩形与【原版视口矩形】求交 ----
            //   规矩出处 = `Shell/ProfilePage.cs:162`（`ProfilePage.Text` 首句 `MenuDraw.VisibleAbove`
            //   → A184/A435「整块在视口外 ⇒ 连节点一起不建」）。
            //
            // 🔴🔴 **2026-10-25（`W27`）就地订正（铁律 5）—— 上一版（`W23`）把这两条红的真因搞错了。**
            //   上一版：`ctr = MenuDraw.PosInDesignSpace(cell)`，然后 `ctr.y + 90` 直接当设计 px 用，
            //   再与 `ViewportClip.Resolve(cell, …).RenderClip` 求交。**错在「帧」上，两处同源：**
            //   · `PosInDesignSpace` 给的是**设计世界单位**（可见高恒 `LayoutSpace.DesignHeight = 10` 单位、
            //     `DesignPxH = 1080` px ⇒ 1 px = 1/108 单位），**不是设计 px**；要换必须过一次
            //     `MenuDraw.PixelOfDesign`（= `LayoutSpace.ToDesignPixel`，x 走实测宽、y 是 `PxY` 翻向）——
            //     判据 = `ViewportClip.ClipPx` 的实时那一支自己就是 `PixelOfDesign(PosInDesignSpace(t))`。
            //   · 于是「px 的视口框」比的是「世界单位的矩形」（实测量级 x ≈ −92..88、y ≈ 92..133）
            //     ⇒ **恒不相交**，前提条实得 0。**产品/视口本身没有问题**（实建 10/15 与几何逐位吻合）。
            //
            // 🔴 **现在的口径（独立、且不冤枉生产）**：
            //   · 视口矩形取**原版字面量**（上面那个 `vp`）—— ⛔ **不读** `ViewportClip.Resolve` /
            //     `MenuDraw.VisibleAbove`：那两条正是**被测的那一个闸**，拿它算期望 = 自证
            //     （改坏闸 ⇒ 期望跟着一起变 ⇒ 恒绿）。
            //   · 名字矩形取**格子当下的设计 px 中心**（`PixelOfDesign(PosInDesignSpace(cell))`，
            //     滚动过就跟着走，不把 10 写死），下移 `CellHalf` 再高 `NameBelowCell`。
            //   · 求交**手工算四个比较**：先例 = `Editor/MainMenuScene.cs:18835-18865` 的
            //     `RowsInViewport` / `CellsInViewport`（`MenuDraw.Visible` 那段注释点名「那是自检的
            //     独立算一遍……别去『收』它」）—— 调 `MenuDraw.Visible` 会让「闸与断言一起改坏」也全绿。
            var ctr = MenuDraw.PixelOfDesign(MenuDraw.PosInDesignSpace(cell));      // = 格子矩形的中心（设计 px）
            float nameY1 = ctr.y + CellHalf;                                        // 名字在**格子外、下沿往下**
            float nameY2 = nameY1 + NameBelowCell;
            var nameR = new PxRect(ctr.x - CellHalf, nameY1, ctr.x + CellHalf, nameY2);
            if (Overlaps(nameR, vp)) expectNames++;
            else if (sampleOut == null) sampleOut = "`" + cell.name + "` 的名字矩形 " + nameR.ToString();

            var nm = Find(cell, "Avatar Name");                 // ⚠️ 只在这一格子树里找（别撞到左栏大图那份）
            if (nm == null) continue;
            nameNodes++;
            if (nm.gameObject.activeSelf) liveNames++;
        }
        Ok(cells > 0, "(V5) 认得出格子（名字前缀 `Avatar Item Small_`；实得 " + cells + " 个）");
        Ok(expectNames > 0,
           "(V5) 前提：至少有一格的 `Avatar Name` 矩形落在视口内（否则下一条等于没查；实得 " + expectNames + "）");
        Ok(nameNodes == expectNames,
           "(V5) `Avatar Name` 的**建与不建** = 它的矩形在不在**原版视口**里（= `ProfilePage.Text` 的视口闸，A184/A435）"
         + "（实得建了 " + nameNodes + "/" + cells + "，按视口算应建 " + expectNames
         + "；视口 " + vp.ToString() + (sampleOut != null ? "；视口外例：" + sampleOut : "") + "）"
         + "｜🔴 **2026-10-24（`W23`）就地订正（铁律 5）**：原来断 `nameNodes == cells`（每格都建）—— "
         + "那条前提**不成立**：第三行那 5 格的名字矩形在视口底之外（890.70..932.01 vs 视口底 855.46）"
         + "⇒ 按既定规矩**本来就不建**（A184/A435），⛔ 别为了凑 15 去改 `AvatarTab` 建节点"
         + "｜🔴 **2026-10-25（`W27`）**：上一版拿 `PosInDesignSpace` 的**世界单位**去比**设计 px** 的框"
         + "（少一次 `PixelOfDesign`）⇒ 前提条恒得 0。现在期望值 = 原版视口字面量 + 格子现读的设计 px 矩形"
         + "｜🧨 改成「根本不建」/ 视口闸失灵（该建的也没建）/ 期望值退回世界单位（少换一次）⇒ 本条红");
        Ok(liveNames == 0,
           "(V5) 格子里那颗 `Avatar Name` **全部关着**（原版 `m_IsActive = F` + 运行期只 `set_text`、"
         + "参考图格内无名字；实得亮着 " + liveNames + " 个）"
         + "｜🧨 删掉 `AvatarTab.BuildRows` 里那句 `cellNameLb.gameObject.SetActive(false)` ⇒ 本条红");

        // 对照：左栏大图那份 `Avatar Name` **是显示的**（原版那一份开着、运行期只换字）——
        // 证明上面关的是「格子那份」，⛔ 不是把整页两张一起关掉。
        var big = Find(avatar.transform, "Selected Item Panel/Avatar Menu Item/Avatar Name");
        Ok(big != null && big.gameObject.activeSelf,
           "(V5) 对照：左栏大图那份 `Avatar Name` **还是显示着的**（实得 active="
           + (big != null ? big.gameObject.activeSelf.ToString() : "<节点不在>") + "）");
    }

    // ============================================================ V6 · 传奇支 `DivisionImage` 的挂点
    static void CheckV6(ProfileTab prof)
    {
        if (prof == null) return;
        var di = Find(prof.transform, "Ranking/Legendary Display Profile/Content/DivisionImage");
        Ok(di != null, "(V6) 前提：`Ranking/Legendary Display Profile/Content/DivisionImage` 在");
        if (di == null) return;

        // ① 全树没有自造子节点 `Body`
        Ok(Find(di, "Body") == null,
           "(V6) `DivisionImage` 底下**没有自造子节点 `Body`**（原版全树无此名）"
         + "｜🧨 改回旧写法（quad 挂 `Body`）⇒ 本条红");
        // ② 图就长在 `DivisionImage` 自己身上（`GetComponent`，⛔ 不是 `GetComponentInChildren`）
        var q = di.GetComponent<ImageQuad>();
        Ok(q != null,
           "(V6) 那张 `07-Legend` 的 `ImageQuad` **就在 `DivisionImage` 节点自己身上**"
         + "｜🧨 挂在子节点上 ⇒ 本条红");
        Ok(q != null && q.Texture != null && q.Texture.name == "07-Legend",
           "(V6) 挂的是 `07-Legend`（实得「"
         + (q != null && q.Texture != null ? q.Texture.name : "<null>") + "」）");
        // ③ 尺寸 = `preserveAspect` 内接进 prefab 的 rect：390.92×330.96 ⇒ **330.96² 居中**
        //    （中心 = ((760.37+1151.30)/2, (340.49+671.45)/2) = (955.835, 505.97)；
        //     判据 = prefab 的 `DivisionImage` rect + uGUI `Image.PreserveSpriteAspectRatio` 的语义）。
        float x1 = 0f, y1 = 0f, x2 = 0f, y2 = 0f;
        bool got = MenuDraw.QuadRectPx(q, out x1, out y1, out x2, out y2) && Sane(x1, y1, x2, y2);
        Ok(got, "(V6) 量得到那张图**画出来那块**（实得 " + Box(x1, y1, x2, y2) + "）");
        if (got)
        {
            Ok(Near(x2 - x1, 330.96f, 1f) && Near(y2 - y1, 330.96f, 1f),
               "(V6) 内接成 **330.96²**（= `min(390.92, 330.96)`；实得 " + F(x2 - x1) + "×" + F(y2 - y1) + "）"
             + "｜🧨 改回旧写法（`390.92 × 2 = 781.84²`，错把 `ppu=50` 当成缩放）⇒ 本条红");
            Ok(Near(x1, 790.355f, 1f) && Near(x2, 1121.315f, 1f)
               && Near(y1, 340.49f, 1f) && Near(y2, 671.45f, 1f),
               "(V6) 居中在槽心 **(955.835, 505.97)**、上下沿 = prefab 的 340.49/671.45"
             + "（实得 " + Box(x1, y1, x2, y2) + "）｜🧨 以屏心 960 居中 ⇒ 本条红");
        }
        // ④ 结构：`RankImage` 仍挂在它底下（出厂 F）
        var ri = Find(di, "RankImage");
        Ok(ri != null && !ri.gameObject.activeSelf,
           "(V6) `RankImage` 仍挂在 `DivisionImage` 底下、出厂关着（实得 "
         + (ri != null ? ri.gameObject.activeSelf.ToString() : "<节点不在>") + "）");
    }

    // ============================================================ 小工具
    /// <summary>按 `A/B/C` 路径找节点 —— **每个路径段都必须是上一段的【直接子件】**（⛔ 不递归）。
    /// <para>🔴 **2026-10-24（`W23`）**：V2 那三处原来拿它查 `#N FactionScoreBig`，而那颗的真实深度是 **3**
    /// （`RankedTab → Top4 → content → left-side|right-side → #N FactionScoreBig`，
    /// `Shell/RankedTab.cs:352-372`）⇒ **恒 null**、13 条全红。⇒ 深节点一律用
    /// <see cref="MenuCheck.FindChild"/>（`GetComponentsInChildren`，**真递归**）；
    /// 本函数只留给「每一段都是直接子件」的那些路径（如 `Alliance Rating Display/Individual rating value`）。</para></summary>
    static Transform Find(Transform root, string path)
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

    static bool Near(float a, float b, float tol) { return Mathf.Abs(a - b) <= tol; }

    /// <summary>两个设计 px 矩形**有没有交集**（严格：贴边不算）—— 语义与 `MenuDraw.Visible` 逐条相同，
    /// 但**故意在这里独立写一遍**：自检的期望值不能拿被测实现自己的求交函数来算
    /// （两处一起改坏 ⇒ 断言恒绿 = 自证）。先例 = `Editor/MainMenuScene.cs:18835-18865` 的
    /// `RowsInViewport` / `CellsInViewport`（`MenuDraw.Visible` 的注释点名「那是自检的独立算一遍，
    /// 别去『收』它」）。</summary>
    static bool Overlaps(PxRect r, PxRect c)
    {
        return !(r.x2 <= c.x1 || r.x1 >= c.x2 || r.y2 <= c.y1 || r.y1 >= c.y2);
    }

    static bool Sane(float x1, float y1, float x2, float y2)
    {
        float w = x2 - x1, h = y2 - y1;
        return !float.IsNaN(w) && !float.IsNaN(h) && w > 1f && w < 3000f && h > 1f && h < 3000f;
    }

    static string F(float v) { return v.ToString("F2"); }

    static string Box(float x1, float y1, float x2, float y2)
    {
        return "(" + F(x1) + "," + F(y1) + ")→(" + F(x2) + "," + F(y2) + ") " + F(x2 - x1) + "×" + F(y2 - y1);
    }

    static void Ok(bool cond, string msg)
    {
        if (cond) { Pass++; Debug.Log(P + "OK   " + msg); }
        else { Fail++; Debug.LogError(P + "FAIL " + msg); }
    }
}
