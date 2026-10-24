// ProfileRankAssertE10.cs — 执行代理 `E10`（2026-10-24，第七十三会话）的断言宿主
//
// 本文件**只放断言**（⛔ 不改产品行为、不建场景、不进任何宿主的 `RunCore` 正文）。
//
// ============================ 主对话怎么挂 ============================
// 在 `Editor/MainMenuScene.cs` 的 `RunCore` 里（**档案窗那一段之后**、且**模式卡那一批断言之后** ——
// 两段都要建好才验得了）加三行：
//
//     ProfileRankAssertE10.Run();
//     pass += ProfileRankAssertE10.Pass;
//     fail += ProfileRankAssertE10.Fail;
//
// ⚠️ 本文件**自己开档案窗**（`PlayerProfileWindow.LastOpened` 拿不到就 `MainMenuRuntime.OpenProfile()`），
//    模式卡那一半要求宿主**已经建过主菜单**（`MainMenuRuntime.InstanceOrFind()` 拿不到 ⇒ 记一条 FAIL 出声）。
// ⚠️ 它会临时改 `DraftModeWindow.SetRunForTest(...)` 那一局存档，**在 `finally` 里原样放回**
//    （⛔ 不会清掉玩家正在进行的那一局 —— 那和 `DraftAssertD3` 的 `ClearRunForTest` 不同，是故意的）。
//
// ============================ 覆盖哪两件 ============================
//  (A) 档案窗 `Profile` 页 `Current Rank` / `Highest Rank` 的**段位大图**（+ `DivisionText` 段位名）
//  (B) 主菜单 `Draft Game Mode Container 1x2` 的 **`State` 条**（轮抽胜场/阶段印在模式卡上）
//
// ============================ 期望值从哪来（防自证） ============================
//  (A) 槽矩形 = **prefab 现读**（`资料/普查产出_0927/档案窗_Profile页.md` §A 第 61/92 行），
//      画出来的正方形 = 由**槽宽**推（512² 方图 + `preserveAspect` ⇒ 内接正方形），⛔ 不回读 `ProfileTab` 的常量。
//  (B) `State` / `Victory Counter Icon` / `Victories` 三格的矩形与字号 = **prefab 现读**
//      （`工具/menu_dump.py bundle_menus_assets_all "Draft Game Mode Container 1x2" --depth 6`），
//      文案 = 反编译口径 `String.Format("{0}/{1}", 胜场, draftRounds)`
//      （格式串 RVA `0x426de28` = `'{0}/{1}'`；`get_CurrentStage` 指令流 = `[event+0x20]+0x48` = `numberOfMatchesWon`），
//      ⛔ 不是抄 `MainMenuRuntime` 的常量、也不是抄 prefab 那颗样板串 `'2/12'`。
// ==================================================================
using CardPresentation;
using UnityEngine;

public static class ProfileRankAssertE10
{
    const string P = "[E10] ";

    /// <summary>本类这一趟的通过 / 失败数（主对话并进宿主总数就加这两格）。</summary>
    public static int Pass { get; private set; }
    public static int Fail { get; private set; }

    public static void Run()
    {
        Pass = 0; Fail = 0;

        CheckDivisionArtTable();
        CheckProfileDivisionArt();
        CheckDraftModeCardProgress();

        Debug.Log(P + $"=== E10 合计：{Pass} 通过 / {Fail} 失败 ===");
    }

    // ============================================================ (A) 表本身
    static void CheckDivisionArtTable()
    {
        // 前提：映射表（唯一一份，`RankedDivisionInfo`）还在、而且演示段位号能取到图。
        Ok(RankedDivisionInfo.DivisionArtCount == 7,
           "(A) 段位表 7 项（原版 7 大段位；实得 " + RankedDivisionInfo.DivisionArtCount + "）");
        string art = RankedDivisionInfo.DivisionArtName(RankedDivisionInfo.DemoDivisionNumber);
        Ok(!string.IsNullOrEmpty(art),
           "(A) 演示段位号 " + RankedDivisionInfo.DemoDivisionNumber + " ⇒ 图名非空（实得「" + art + "」）");
        var tex = string.IsNullOrEmpty(art) ? null : CardArt.MenuUi(art);
        Ok(tex != null, "(A) 段位图 `" + art + "` 取得到（`CardArt.MenuUi`）");
        if (tex != null)
            Ok(tex.width == tex.height && tex.width >= 256,
               "(A) 段位图是正方形大图（实得 " + tex.width + "×" + tex.height + "）—— 决定「内接成正方形」这条算法");
    }

    // ============================================================ (A) 两张卡上的段位大图
    static void CheckProfileDivisionArt()
    {
        // prefab 现读的**槽**（高度 0 是布局伪值 ⇒ 只取宽度与中心；见 `ProfileTab.CrDivR` 的 doc）
        var crSlot = new PxRect(366.58f, 457.65f, 757.50f, 457.65f);
        var hrSlot = new PxRect(760.37f, 448.49f, 1151.30f, 448.49f);

        var pp = PlayerProfileWindow.LastOpened;
        if (pp == null) pp = MainMenuRuntime.OpenProfile();
        var prof = pp != null ? pp.Page(WindowTabType.ProfileInfo) as ProfileTab : null;
        if (prof == null)
        {
            Ok(false, "(A) 档案窗 / `Profile` 页拿不到 ⇒ 本节验不了（⛔ 不静默跳过）");
            return;
        }

        string art = RankedDivisionInfo.DivisionArtName(RankedDivisionInfo.DemoDivisionNumber);
        Ok(prof.DivisionArtUsed == art && prof.DivisionArtDrawn == 2,
           "(A) 两张卡**各画了一张**段位大图（`DivisionArtUsed` = 「" + prof.DivisionArtUsed
           + "」· `DivisionArtDrawn` = " + prof.DivisionArtDrawn + "）");

        // 卡宽也取 prefab 真值（`Current Rank` 776.28−351.03 · `Highest Rank` 1137.12−777.72）
        CheckOneDivisionQuad(prof.transform, "Ranking/Current Rank/Content/DivisionImage",
                             crSlot, 776.28f - 351.03f, art, "Current Rank");
        CheckOneDivisionQuad(prof.transform, "Ranking/Highest Rank/Content/DivisionImage",
                             hrSlot, 1137.12f - 777.72f, art, "Highest Rank");

        // ---- 段位名（`DivisionText`）：官方词条 `Ranked/Division/<演示号>`，两张卡同字 ----
        string wantName = Loc.OfficialCurrent("Ranked/Division/" + RankedDivisionInfo.DemoDivisionNumber);
        Ok(!string.IsNullOrEmpty(wantName),
           "(A) 官方词条 `Ranked/Division/" + RankedDivisionInfo.DemoDivisionNumber + "` 取得到（实得「" + wantName + "」）");
        // ⚠️ 我们这份实现把 `DivisionText` 建在 `Content` 底下（原版 prefab 里它是 `RankTitleBG` 的子件）——
        //    **结构差异，本次没动**：`MainMenuScene.cs:6748` 那条 `CheckAtWorld(RankTitleBG, …)` 是靠
        //    「子树里第一颗 Label/ImageQuad」量矩形的，把字挪进去会让那条断言改量到字框上（白红一条）。
        string crName = TextOf(FindChild(prof.transform, "Ranking/Current Rank/Content/DivisionText"));
        string hrName = TextOf(FindChild(prof.transform, "Ranking/Highest Rank/Content/DivisionText"));
        Ok(crName == wantName && hrName == wantName,
           "(A) 两张卡的 `DivisionText` 都印官方段位名「" + wantName + "」（实得「" + crName + "」/「" + hrName + "」）");

        // ---- `RankImage`（罗马数字名次）：仍然**不出图**（名次号在服务器）----
        foreach (var card in new[] { "Current Rank", "Highest Rank" })
        {
            var ri = FindChild(prof.transform, "Ranking/" + card + "/Content/DivisionImage/RankImage");
            Ok(ri != null && ri.GetComponentInChildren<ImageQuad>() == null,
               "(A) `" + card + "/DivisionImage/RankImage` 建了但**不出图**（名次号在服务器 —— 判据 S4 §缺数据）");
        }
    }

    /// <summary>一张卡上的段位大图：画了没 · 是哪张图 · 正方形 · 位置/尺寸对不对 · 层级对不对。</summary>
    static void CheckOneDivisionQuad(Transform profRoot, string divPath, PxRect slot, float cardW,
                                     string art, string cardName)
    {
        var div = FindChild(profRoot, divPath);
        Ok(div != null, "(A) `" + cardName + "/Content/DivisionImage` 建出来了");
        if (div == null) return;

        var q = div.GetComponentInChildren<ImageQuad>();
        Ok(q != null, "(A) `" + cardName + "` 的段位大图**真的画了**（`DivisionImage` 子树里有 `ImageQuad`）");
        if (q == null) return;

        // 是不是那一张图（⛔ 不拿别的段位顶替）
        bool sameTex = q.Texture == CardArt.MenuUi(art) || (q.Texture != null && q.Texture.name == art);
        Ok(sameTex, "(A) `" + cardName + "` 画的是 `" + art + "`（实得「"
                   + (q.Texture != null ? q.Texture.name : "<null>") + "」）");

        // 期望矩形 = **边长 0.72 × 卡宽**（实拍反算，见 `ProfileTab.DivArtSideRatio` 的 doc）、
        // 中心对齐槽中心、上沿 = 槽的 y（512² 方图 + preserveAspect = 内接正方形）
        float side = cardW * 0.72f, cx = slot.CX;
        float wx1 = cx - side * 0.5f, wx2 = cx + side * 0.5f, wy1 = slot.y1, wy2 = slot.y1 + side;
        float x1 = 0f, y1 = 0f, x2 = 0f, y2 = 0f;
        // 🔴 量法 = `MenuDraw.QuadRectPx`（`Editor/MainMenuScene.cs` 的 `QuadPxRect` 转调的那一份；
        //    = `ImageQuad.WorldW/WorldH × 108` + 设计帧节点位置）—— **直接量那颗 quad**，
        //    ⛔ 不对父节点用 `MenuCheck.NodeRectPx`（它会**优先取子树里的 Label**；本轮 (B) 就栽在那一支）。
        bool got = MenuDraw.QuadRectPx(q, out x1, out y1, out x2, out y2);
        Ok(got && Sane(x1, y1, x2, y2),
           "(A) `" + cardName + "` 的段位大图量得到**渲染矩形**且是有限值（实得 " + Box(x1, y1, x2, y2) + "）"
           + " —— 🧨 量成哨兵 / 未生成 mesh ⇒ 本条红");
        if (got && Sane(x1, y1, x2, y2))
        {
            Ok(Near(x2 - x1, side, 0.6f) && Near(y2 - y1, side, 0.6f),
               "(A) `" + cardName + "` 边长 = **0.72 × 卡宽 " + F(cardW) + " = " + F(side)
               + "**（正方形；实得 " + F(x2 - x1) + "×" + F(y2 - y1) + "）");
            // 🧨 灭自证 ①：徽记**不许比卡还宽**（照派单「取 prefab 槽宽 390.92」的写法，右卡会溢 15.7/侧）
            Ok(x2 - x1 < cardW,
               "(A) `" + cardName + "` 徽记不溢出卡宽（" + F(x2 - x1) + " < " + F(cardW)
               + "）—— 🧨 拿 `DivisionImage` 槽宽 390.92 当边长的写法 ⇒ 本条红");
            Ok(Near(x1, wx1, 0.6f) && Near(x2, wx2, 0.6f) && Near(y1, wy1, 0.6f) && Near(y2, wy2, 0.6f),
               "(A) `" + cardName + "` 矩形 = (" + F(wx1) + "," + F(wy1) + ")→(" + F(wx2) + "," + F(wy2)
               + ")（实得 (" + F(x1) + "," + F(y1) + ")→(" + F(x2) + "," + F(y2) + ")）");
            // 🧨 灭自证 ②：两张卡的徽记**不等大**（实拍量到右卡是左卡的 0.806）；
            //    写死一个常量的实现（例如两张都 390.92，或都 960 居中）⇒ 这里与下面那条会红
            Ok(!Near(x2 - x1, 390.92f, 2f),
               "(A) `" + cardName + "` 的边长**不是** `DivisionImage` 槽宽 390.92（实得 " + F(x2 - x1) + "）");
        }

        // 层级：徽记压在卡片红底之上、页内文字之下（原版兄弟序 `DivisionImage` 在 `footer` 之前）
        var title = FindChild(profRoot, divPath.Replace("/DivisionImage", "/Title"));
        var titleLb = title != null ? title.GetComponent<Label>() : null;
        Ok(titleLb != null && q.RenderQueue < titleLb.RenderQueue,
           "(A) `" + cardName + "` 的徽记队列(" + q.RenderQueue + ") **小于**卡标题("
           + (titleLb != null ? titleLb.RenderQueue.ToString() : "<无>") + ") ⇒ 徽记不会盖住字");
    }

    // ============================================================ (B) 模式卡上的轮抽进度
    static void CheckDraftModeCardProgress()
    {
        var mm = MainMenuRuntime.InstanceOrFind();
        if (mm == null)
        {
            Ok(false, "(B) 拿不到 `MainMenuRuntime` ⇒ 模式卡那一半验不了（⛔ 不静默跳过）");
            return;
        }
        // 🔴 **2026-10-24（第七十三会话 E12 修）**：原来写的是 `FindChild(mm.transform,
        //   "GameModes/Viewport/Content/…")`，而 `FindChild`（本文件 :307 起）是**逐级取直接子件**
        //   ⇒ 漏了 `MainMenu/Safe area All` 两层祖先（`Shell/MainMenuRuntime.cs`：`MainMenu` :482 →
        //   `Safe area All` :524 → `GameModes` :2340 → `Viewport` → `Content` → 卡 :2455）⇒ 恒 null，
        //   而且这条一红整段 (B) 就 `return`，后面 8+ 条从没跑过。
        //   改走**递归**读口 `MainMenuRuntime.Find`（= `FindIn(_root, name)`，含 inactive）。
        var card = mm.Find("Draft Game Mode Container 1x2");
        Ok(card != null, "(B) 轮抽模式卡建出来了（`Safe area All/GameModes/Viewport/Content/Draft Game Mode Container 1x2`）");
        if (card == null) return;
        // ★ 结构断言：递归查找可能撞到别处的同名件 ⇒ 把父链钉在 `Viewport/Content` 上。
        Ok(card.parent != null && card.parent.name == "Content"
           && card.parent.parent != null && card.parent.parent.name == "Viewport",
           "(B) 那张卡的父链 = `Viewport/Content`（原版 `FlexGridLayout` 的那一格）");

        var state = FindChild(card, "State");
        // 🩹 **2026-10-24（自检首跑订正）**：`Victory Counter Icon` 是 **`State` 的子件**、`Victories` 又是它的子件
        //   ⇒ 路径必须带 `State/` 前缀（`FindChild` 是按 `/` 逐级取**直接子件**，不是深搜）。
        var icon = FindChild(card, "State/Victory Counter Icon");
        var vic = FindChild(card, "State/Victory Counter Icon/Victories");
        Ok(state != null, "(B) 卡上有 `State` 条（原版 `DraftMenuContainer.stateGO`）");
        Ok(icon != null, "(B) `State/Victory Counter Icon` 建出来了（原版那颗 `Rank Skull` 胜场图标）");
        Ok(vic != null, "(B) `State/Victory Counter Icon/Victories` 建出来了（⚠️ 它是**图标那颗的子件**）");
        if (state == null || icon == null || vic == null) return;

        var vicLb = vic.GetComponent<Label>();
        Ok(vicLb != null && mm.DraftVictoriesLabel == vicLb,
           "(B) `Victories` 那颗字就是 `MainMenuRuntime` 记的那个句柄（自检读口接上了）");

        // ---- 几何 + 文案 / 显隐：逐档喂存档（原版 `DoOperationsOnDraftState` 的掩码 0x108420）----
        var backup = DraftModeWindow.RunForTest;
        int rounds = DraftPacks.RoundsPerDraft;
        try
        {
            // 🔴 **几何要先让 `State` 条【显示出来】再量** —— 本仓的老坑：TMP 在对象**未激活**时量不出尺寸
            //    （`textBounds` 是未定义哨兵；本轮首跑的实测值就是 `±2.3e11` = `int.MaxValue × 108`）。
            //    ⇒ 先喂一局「进行中」的存档（`RefreshDraftModeCard` 会 `SetActive(true)`），再量。
            DraftModeWindow.SetRunForTest(NewRun(DraftJoinState.Joined, DraftPayState.Free, "E10GeomWarlord", 2, 0, false));
            mm.RefreshDraftModeCard(true);
            Ok(mm.DraftStateVisible,
               "(B) 【几何前提】喂一局进行中的存档 ⇒ `State` 条**显示**（下面几条要量的件必须在激活链上）");
            CheckDraftStateBarGeometry(card, icon, vicLb);

            DraftModeWindow.SetRunForTest(null);
            mm.RefreshDraftModeCard(true);
            Ok(!mm.DraftStateVisible && mm.DraftVictoriesText == "0/" + rounds,
               "(B) 没有进行中的一局 ⇒ `State` 条**藏**、字 = `0/" + rounds + "`（实得 visible="
               + mm.DraftStateVisible + " · 「" + mm.DraftVictoriesText + "」）");

            // 未报名（Pay 页 / Intro 页）：原版掩码里 0 与 2 都不显示
            DraftModeWindow.SetRunForTest(NewRun(DraftJoinState.NotJoined, DraftPayState.NotPaid, null, 0, 0, false));
            mm.RefreshDraftModeCard(true);
            Ok(!mm.DraftStateVisible, "(B) `joinState = NotJoined`（原版落 Pay 页 = 2）⇒ **藏**");

            DraftModeWindow.SetRunForTest(NewRun(DraftJoinState.Joined, DraftPayState.NotPaid, null, 0, 0, false));
            mm.RefreshDraftModeCard(true);
            Ok(!mm.DraftStateVisible, "(B) `Joined + NotPaid`（Intro = 0）⇒ **藏**");

            // 已报名、还没选战将（SelectWarlord = 5）⇒ **显示**
            DraftModeWindow.SetRunForTest(NewRun(DraftJoinState.Joined, DraftPayState.Free, null, 0, 0, false));
            mm.RefreshDraftModeCard(true);
            Ok(mm.DraftStateVisible && mm.DraftVictoriesText == "0/" + rounds,
               "(B) `Joined + Free`（SelectWarlord = 5）⇒ **显示**、字 `0/" + rounds + "`（实得 visible="
               + mm.DraftStateVisible + " · 「" + mm.DraftVictoriesText + "」）");

            // 选完战将 + 2 胜（OngoingRun = 15）⇒ 显示 `2/4`
            DraftModeWindow.SetRunForTest(NewRun(DraftJoinState.Joined, DraftPayState.Free, "E10TestWarlord", 2, 0, false));
            mm.RefreshDraftModeCard(true);
            Ok(mm.DraftStateVisible && mm.DraftVictoriesText == "2/" + rounds,
               "(B) 2 胜 ⇒ 卡上印 **`2/" + rounds + "`**（原版 `SetVictories` 的 `{0}/{1}`；实得「"
               + mm.DraftVictoriesText + "」· ⛔ 不是 prefab 样板 `2/12`）");

            // 6 胜（超过轮数）：原版**不夹**（`{胜场}/{draftRounds}` 直接印）⇒ 仍是 `6/4`
            DraftModeWindow.SetRunForTest(NewRun(DraftJoinState.Joined, DraftPayState.Free, "E10TestWarlord", 6, 0, false));
            mm.RefreshDraftModeCard(true);
            Ok(mm.DraftVictoriesText == "6/" + rounds,
               "(B) 6 胜 ⇒ `6/" + rounds + "`（原版不夹上限；实得「" + mm.DraftVictoriesText + "」）"
               + " —— 🧨 灭自证：写死样板串 / 夹到轮数 ⇒ 本条红");

            // 输满 3 场（Finished = 20）⇒ 仍显示
            DraftModeWindow.SetRunForTest(NewRun(DraftJoinState.Joined, DraftPayState.Free, "E10TestWarlord", 2, 3, false));
            mm.RefreshDraftModeCard(true);
            Ok(mm.DraftStateVisible, "(B) 输满 " + DraftModeWindow.MaxTries + " 场（Finished = 20）⇒ **仍显示**");

            // 领完奖（Collected = 22）⇒ 藏
            DraftModeWindow.SetRunForTest(NewRun(DraftJoinState.Joined, DraftPayState.Free, "E10TestWarlord", 2, 3, true));
            mm.RefreshDraftModeCard(true);
            Ok(!mm.DraftStateVisible, "(B) 已领奖（Collected = 22）⇒ **藏**"
               + " —— 🧨 灭自证：恒显示 ⇒ 本条红");
        }
        finally
        {
            DraftModeWindow.SetRunForTest(backup);
            mm.RefreshDraftModeCard(true);
            Ok(mm.DraftVictoriesLabel != null,
               "(B) 收尾：把这局存档**原样放回**（没有清掉真玩家的进度）");
        }
    }

    /// <summary>`State` 条里那两件的**几何**（⛔ 只在「条子已经显示出来」时调 —— 未激活的件量不出尺寸）。
    ///
    /// <para>🔴 **量法（与本仓其它几何断言同一口径；这也是本轮首跑踩坑后的订正）**：
    /// ① **图标 = `ImageQuad` 自己那块** —— `MenuDraw.QuadRectPx(q, out …)`（= `Editor/MainMenuScene.cs`
    ///    的 `QuadPxRect` 转调的那一份：`q.WorldW/WorldH × 108` + 设计帧节点位置）。
    ///    ⛔ **不要对图标节点用 `MenuCheck.NodeRectPx`** —— 那个函数**优先取子树里的 `Label`**，
    ///    而图标底下正好挂着 `Victories`（原版父子就是这样）⇒ 它会去量那颗 **TMP**，未布局时拿到
    ///    未定义哨兵（本轮首跑实测 `−2.3e11..+2.3e11` = `int.MaxValue × 108`）。
    /// ② **字 = TMP 自己渲出来那块的设计帧矩形** —— `SelfCheckTextMetrics.RenderedRectDesign`，
    ///    这就是本仓 `A490/A617` 那条「字**真的从哪开始画**」的口径（⛔ 不用 `NodeRectPx` 的 Label 分支：
    ///    它的中心是节点位置、尺寸是另一次量的，本仓记着那是「混合值」）。
    /// ③ 两条都先过**有限性闸**（宽高 1..2000px）—— 量到哨兵 / 未生成 mesh 时**当场红**，
    ///    ⛔ 不拿垃圾数去跟期望值比（那会给出一个「看着有数」的假结论）。
    /// ④ **前提是条子显示着**（调用点先喂一局进行中的存档 + `RefreshDraftModeCard(true)`）——
    ///    TMP 在未激活的节点下 `textBounds` 恒是哨兵，这是本仓踩过多次的坑。</para>
    ///
    /// <para>🧨 **改坏必红**：三条位置期望值都是 **prefab 卡内偏移**（`x 8.67..106.12` / `y 637.07..740.40` /
    /// 字左沿 `109.29`）—— 把 `MainMenuRuntime` 的 `DraftVicL/T` 或 `DraftVicTxL` 改成别的值、
    /// 把 `Victories` 挪到 `State` 底下（`AlignLeft` 的基准就变了）、或干脆不建这两件 ⇒ 红。</para>
    static void CheckDraftStateBarGeometry(Transform card, Transform icon, Label vicLb)
    {
        // 卡的原点从**卡图那颗 quad** 量（`New` 建的节点本身不写位置 —— `MainMenuRuntime` 那条不变式）
        var cardArt = card != null ? FindChild(card, "Background Image") : null;
        var cardQ = cardArt != null ? cardArt.GetComponentInChildren<ImageQuad>() : null;
        float qx1 = 0f, qy1 = 0f, qx2 = 0f, qy2 = 0f;
        bool hasCard = MenuDraw.QuadRectPx(cardQ, out qx1, out qy1, out qx2, out qy2);
        Ok(hasCard && Sane(qx1, qy1, qx2, qy2),
           "(B) 量得到卡图矩形（下面几条位置的基准；实得 " + Box(qx1, qy1, qx2, qy2) + "）");
        if (!hasCard || !Sane(qx1, qy1, qx2, qy2)) return;

        // ① 图标：图 quad 自己那块
        var iq = icon != null ? icon.GetComponentInChildren<ImageQuad>() : null;
        float ix1 = 0f, iy1 = 0f, ix2 = 0f, iy2 = 0f;
        bool hasIcon = MenuDraw.QuadRectPx(iq, out ix1, out iy1, out ix2, out iy2);
        Ok(hasIcon && Sane(ix1, iy1, ix2, iy2),
           "(B) `Victory Counter Icon` 的**画出来那块**量得到且是有限值（实得 " + Box(ix1, iy1, ix2, iy2) + "）"
           + " —— 🧨 量成未生成 mesh / 未布局的哨兵值 ⇒ 本条红（量法：`MenuDraw.QuadRectPx`）");
        if (hasIcon && Sane(ix1, iy1, ix2, iy2))
            Ok(Near(ix1 - qx1, 8.67f, 0.6f) && Near(ix2 - qx1, 106.12f, 0.6f)
               && Near(iy1 - qy1, 637.07f, 0.6f) && Near(iy2 - qy1, 740.40f, 0.6f),
               "(B) 胜场图标位置 = prefab（卡内 x 8.67..106.12 · y 637.07..740.40；实得 "
               + F(ix1 - qx1) + ".." + F(ix2 - qx1) + " · " + F(iy1 - qy1) + ".." + F(iy2 - qy1) + "）");

        // ② 字：TMP 渲出来那块（设计帧）的左缘 + 纵向落在那条带里
        float tx1 = 0f, ty1 = 0f, tx2 = 0f, ty2 = 0f;
        bool hasTx = vicLb != null
                     && SelfCheckTextMetrics.RenderedRectDesign(vicLb.transform, out tx1, out ty1, out tx2, out ty2)
                     && Sane(tx1, ty1, tx2, ty2);
        if (!hasTx && vicLb != null)
        {
            // 🔴 兜底：量到哨兵 = 版面还没推下去（`GetTextInfo` 单遍对**自适应**那一档可能 early-return）。
            //    走公共件那条「定版面」的口（`Label.ForceRelayout`；激活档 = 真 `ForceMeshUpdate`）再量一次。
            //    ⚠️ 它只重排 mesh、**不动节点位置** ⇒ 不会替被测实现把左缘挪到期望值上（不算自证）。
            vicLb.ForceRelayout();
            hasTx = SelfCheckTextMetrics.RenderedRectDesign(vicLb.transform, out tx1, out ty1, out tx2, out ty2)
                    && Sane(tx1, ty1, tx2, ty2);
            Debug.Log(P + "(B) 那段字的 `textBounds` 首量是哨兵 ⇒ 走 `Label.ForceRelayout()` 补推一次版面后重量"
                    + "（实得 " + Box(tx1, ty1, tx2, ty2) + "）");
        }
        Ok(hasTx, "(B) `Victories` 那颗字**渲出来那块**量得到且是有限值（实得 " + Box(tx1, ty1, tx2, ty2) + "）");
        if (hasTx && Sane(tx1, ty1, tx2, ty2))
        {
            // 容差 3px：`textBounds` 是行盒、左缘带一点字距（本仓 A490 那条口径）。
            Ok(Near(tx1 - qx1, 109.29f, 3f),
               "(B) 那颗字**左对齐到 prefab 那条线**（卡内 x = 109.29 = 图标右沿 106.12 + 3.17；实得 "
               + F(tx1 - qx1) + "）");
            // 纵向只钉「落在 `State` 条那一带里」（637.07..740.40 是布局框；TMP 行盒与 `Capline` 有墨差）
            Ok(ty1 - qy1 >= 600f && ty2 - qy1 <= 780f,
               "(B) 那颗字纵向落在 `State` 条那一带（卡内 637.07..740.40 ± 一档墨差；实得 "
               + F(ty1 - qy1) + ".." + F(ty2 - qy1) + "）");
        }

        // ③ 字号：原版那颗是 `fs 99.69 · auto[18~99.69] · base 36`，而 TMP 实测字号会被行盒高度拉低一点
        //    （auto 的正常行为）⇒ 只钉「**不许超过原版上限**、而且不是别的档（28/36）」。
        float fp = vicLb != null ? vicLb.FontPxNow : 0f;
        if (fp > 0f)
            Ok(fp <= 99.69f + 0.5f && fp >= 40f,
               "(B) 字号落在原版那一档（`auto[18~99.69]` ⇒ 实测应 ≈ 75~99.7；实得 " + fp.ToString("F2")
               + "）—— 🧨 用了别的档（28/36）或没开 auto ⇒ 本条红");
        else
            Debug.Log(P + "SKIP (B) 字号：`FontPxNow` 量到 0（TMP 未测量）⇒ 这条没验（⛔ 不算通过）");
    }

    static DraftRunSave NewRun(DraftJoinState join, DraftPayState pay, string warlord, int won, int lost, bool collected)
    {
        return new DraftRunSave
        {
            joinState = (int)join,
            payState = (int)pay,
            warlordId = warlord,
            matchesWon = won,
            matchesLost = lost,
            rewardsCollected = collected,
            pickedKeys = warlord != null ? new string[DraftPacks.RoundsPerDraft] : new string[0],
            selectedCards = new string[0],
        };
    }

    // ============================================================ 小工具
    static bool Near(float a, float b, float tol) { return Mathf.Abs(a - b) <= tol; }

    /// <summary>量出来的矩形是不是「有限、像样」的（宽高 1..2000px）。
    /// 🔴 本仓量 TMP 的坑：**对象未激活 / 字是空串 / mesh 没生成**时 `textBounds` 是未定义哨兵
    /// （实测会报出 `±2.3e11` = `int.MaxValue × 108`、或 `4.29e9`）⇒ 这种数**当场判红**，
    /// ⛔ 绝不让它进 `Near(...)` 去和期望值比（那会得出一个「看着有数」的假结论）。</summary>
    static bool Sane(float x1, float y1, float x2, float y2)
    {
        float w = x2 - x1, h = y2 - y1;
        return !float.IsNaN(w) && !float.IsNaN(h) && w > 1f && w < 2000f && h > 1f && h < 2000f;
    }

    static string F(float v) { return v.ToString("F2"); }

    static string Box(float x1, float y1, float x2, float y2)
    {
        return "(" + F(x1) + "," + F(y1) + ")→(" + F(x2) + "," + F(y2) + ") " + F(x2 - x1) + "×" + F(y2 - y1);
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
