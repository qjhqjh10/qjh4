// RankedArtAssert.cs — 执行代理 `A1`（2026-10-24）的断言宿主：**「等级 → 段位图案」的映射与两处落点**
//
// 本文件**只放断言**（⛔ 不改任何产品行为、不建场景、不进任何 `Editor/*Scene.cs` 宿主）。
//
// 调用方式一（**独立跑，够用**）：`-executeMethod RankedArtAssert.Run`
//   · **阶段 A**（映射表 / 13 张图能不能取到 / 官方词条）**不需要场景**，独立跑就全跑；
//   · **阶段 B**（两个窗口是不是画的同一张 / 几何 / 名次图关着）需要场景里**已经建好**档案窗
//     （排位窗有就一起查，没有就 Skip）。
// 调用方式二（推荐，和别的断言一锅出）：在 `Editor/MainMenuScene.Run` 收尾处加两行 ——
// 与 `AvatarScaleAssertE18` / `DraftAssertD1` 同款：
//
//     RankedArtAssert.Run();
//     pass += RankedArtAssert.Pass; fail += RankedArtAssert.Fail;
//
// ==================================================================
//  用户 2026-10-24 的原话：「**没有映射表，但是有等级图案吧？根据图案和文件名称进行判断，得出对应**」
//  ＋ 调度台定的红线：**⛔ 不许再拿『前缀顺序』当唯一依据**。
//
//  本文件断的事（每一条都写清「🧨 怎么改会红」）：
//   A·1 段位表 = **7 项**，且每项的**两位前缀序号 == 它在表里的位置**（🧨 重排表 ⇒ 红）
//   A·2 **两个实测锚点**：`DivisionArtName(4) == "04-Admiral"` · `DivisionArtName(7) == "07-Legend"`
//       （证据：`账户-个人资料参考图.png` 的「当前排名 金 II」徽记 ↔ 04 = NCC 0.707 / 次优 0.577；
//        `多人模式-经典-界面参考图.png` 左列「排名 / 传奇」徽记 ↔ 07 = NCC **0.939** / 次优 0.751。
//        两个锚点各自还有**独立第二条信号**：金 = `Ranked/Division/4`、传奇 = `Ranked/Division/7`）
//       🧨 把 4 和 7 对调、或整体错一位（1=Rook→2=Rook）⇒ 红。
//   A·3 灭自证：**7 个图名两两不同**，且段位族与罗马族**不相交**（🧨 把 `Roman_*` 混进段位表 ⇒ 红）
//   A·4 7 张段位图**都取得到**、**都是正方形**、**同一尺寸**（原版 512²）；6 张罗马同样（原版 128²）
//       🧨 少导一张 / 把某张导成横图 ⇒ 红（`ArtInternal` 会把缺的记进 `MissingArt`，这里也从 `CardArt` 直取）
//   A·5 官方词条 `Ranked/Division/1..7` **七条都在、七条互不相同、并等于官方那七个数**
//       （现读：Iron/Bronze/Silver/Gold/Platinum/Diamond/Legendary ＝ 铁/铜/银/金/铂金/钻石/传奇）
//       🧨 号错一位（拿 `Ranked/Division/5` 当第 4 档）⇒ 红
//   A·6 名次表 = **6 项** `Roman_I..VI`，6 张都取得到（它们是**小档**，⛔ 不是段位）
//   B·1 **口径一致**：`RankedTab.DivisionArtUsed` == 事件窗那颗 `Content/DivisionImage/Image` 挂的图名
//       == `RankedDivisionInfo.DivisionArtName(DemoDivisionNumber)`（🧨 两个窗口各挑各的 ⇒ 红）
//   B·2 排名页那颗图**真的挂上去了**（`ImageQuad.Texture.name` == 图名）且**画成 133.28²、居中于 769.01/666.00**
//       （原版 prefab：`DivisionImage` = 369.298×133.282 + `m_PreserveAspect = 1` ⇒ 512² 内接 133.28²）
//       🧨 拉满 369.30 宽 / 没画 ⇒ 红
//   B·3 两处 `RankImage`（名次数字）**都是关着的**（名次号来自服务器，参考图判不了「最高段位叠不叠名次」）
//       🧨 有人顺手把它点亮、又没有真名次数据 ⇒ 红
//
//  🔴 **期望值从哪来**（⛔ 不许拿我们自己的常量证明自己的实现）：
//    段位/名次图名 = **解包资源里的原始文件名**（`bundle_rankeddivisionicons_assets_all/Texture2D/`，13 张）；
//    档号 = 官方本地化 `Ranked/Division/N` 的**条数**（7）与**顺序**；
//    几何 = 原版 prefab 的 `m_SizeDelta` / `m_PreserveAspect`（`menu_dump.py` 现读那一行）；
//    两个锚点 = **原版实拍截图**的像素比对（NCC），见 A·2。
//    ⚠️ 阶段 B 的 `Q + L_Art` 那一档**故意不断**：那是我们自己的分层常量（自证），
//      真正要钉的是「画出来在哪、多大」—— 那两条已经用原版 prefab 的绝对像素断了。
// ==================================================================
using CardPresentation;
using UnityEngine;

public static class RankedArtAssert
{
    const string P = "[A1] ";

    /// <summary>本类这一趟的三档计数（主对话并进宿主合计时加前两个）。</summary>
    public static int Pass { get; private set; }
    public static int Fail { get; private set; }
    /// <summary>需要场景/宿主、而这一趟没有 ⇒ **不算失败**，但**必须出声**（⛔ 不许静默当通过）。</summary>
    public static int Skipped { get; private set; }

    // ---------------- 判据常量（全部来自原版，⛔ 不是我们的实现） ----------------
    /// <summary>官方 `Ranked/Division/1..7` 的中英两列（`Unity/数据/游戏数据/官方本地化_en+zh-CN.json` 现读）。
    /// ⚠️ 语言档不确定 ⇒ 每档接受**两个值之一**（只判「这一档的词条对不对」，不判「现在是哪一档语言」）。</summary>
    static readonly string[][] OfficialDivision =
    {
        new[] { "Iron", "铁" },
        new[] { "Bronze", "铜" },
        new[] { "Silver", "银" },
        new[] { "Gold", "金" },
        new[] { "Platinum", "铂金" },
        new[] { "Diamond", "钻石" },
        new[] { "Legendary", "传奇" },
    };

    /// <summary>原版 `Ranking Tab/Top4/content/center/DivisionImage` 的 prefab 值（`menu_dump.py` 现读）。</summary>
    const float DiL = 584.36f, DiR = 953.66f, DiT = 599.34f, DiB = 732.62f;

    public static void Run()
    {
        Pass = 0; Fail = 0; Skipped = 0;

        CheckDivisionTable();
        CheckRankTable();
        CheckLocalization();
        CheckAnchors();

        // ---- 阶段 B：窗口层（要场景）----
        var tab = FindBuilt<RankedTab>(t => MenuCheck.FindChild(t, "Top4") != null);
        if (tab == null)
        {
            Skip("阶段 B（窗口层）：场景里没有**建好的** `RankedTab`（`Top4` 不在）⇒ 画出来的那一半没验。"
               + "先跑 `MainMenuScene.Run`，或在它的收尾处加两行 `RankedArtAssert.Run();` 再跑。");
        }
        else
        {
            CheckTab(tab);
            CheckEventWindow(tab);
        }

        Debug.Log(P + $"=== A1 合计：{Pass} 通过 / {Fail} 失败 / {Skipped} 跳过 ===");
    }

    // ==================================================================
    //  A·1 / A·3 · 段位表结构
    // ==================================================================
    static void CheckDivisionTable()
    {
        var want = new[]
        {
            "01-Rook", "02-Veteran", "03-Commander", "04-Admiral",
            "05-Conqueror", "06-Galactic_Threat", "07-Legend",
        };

        Ok(RankedDivisionInfo.DivisionArtCount == 7,
           $"★ A·1 段位表 = **7 项**（原版 7 大段位；实测 {RankedDivisionInfo.DivisionArtCount}）"
         + "｜🧨 少一档 / 多一档 ⇒ 红");

        bool allPrefix = true;
        for (int i = 0; i < want.Length; i++)
        {
            string got = RankedDivisionInfo.DivisionArtName(i + 1);
            if (got != want[i]) allPrefix = false;
            Ok(got == want[i],
               $"★ A·1 第 {i + 1} 档 = `{want[i]}`（原版资源文件名，`bundle_rankeddivisionicons_assets_all/Texture2D/`）"
             + $"—— 实测 `{got ?? "<null>"}`｜🧨 表里换了顺序 / 少了一项 ⇒ 红");
        }
        Ok(allPrefix && RankedDivisionInfo.DivisionArtName(0) == null
                     && RankedDivisionInfo.DivisionArtName(8) == null,
           "★ A·1b 越界（0 与 8）**返回 null**（调用方据此出声、⛔ 不拿别的段位顶替）"
         + $"｜🧨 改成 `Clamp` / 取模 ⇒ 越界会静默画出别的段位 ⇒ 红");

        // A·3 灭自证：两两不同 + 两族不相交
        bool distinct = true;
        for (int i = 0; i < want.Length; i++)
            for (int j = i + 1; j < want.Length; j++)
                if (want[i] == want[j]) distinct = false;
        Ok(distinct, "★ A·3 七个段位图名**两两不同**｜🧨 同一张图被两档共用 ⇒ 红");

        bool disjoint = true;
        for (int r = 1; r <= 6; r++)
        {
            string rn = RankedDivisionInfo.RankArtName(r);
            for (int d = 1; d <= 7; d++)
                if (rn != null && rn == RankedDivisionInfo.DivisionArtName(d)) disjoint = false;
        }
        Ok(disjoint,
           "★ A·3b **段位族（7 张）与名次族（6 张）不相交**（原版是两个 `List`：`divisions` / `ranks`，"
         + "元素同类但用途不同 —— `RankedDivisions__GetDivisionData.c` vs `GetRankData.c`）"
         + "｜🧨 把 `Roman_*` 混进段位表 ⇒ 红");
    }

    // ==================================================================
    //  A·6 · 名次表（罗马数字是「小档」，⛔ 不是段位）
    // ==================================================================
    static void CheckRankTable()
    {
        var want = new[] { "Roman_I", "Roman_II", "Roman_III", "Roman_IV", "Roman_V", "Roman_VI" };
        Ok(RankedDivisionInfo.RankArtCount == 6,
           $"★ A·6 名次表 = **6 项**（官方 `Ranked/Rank/1..6` = `I…VI`，与 6 张图**同数**；"
         + $"实测 {RankedDivisionInfo.RankArtCount}）｜🧨 与段位族混成一张表 ⇒ 红");

        for (int i = 0; i < want.Length; i++)
        {
            string got = RankedDivisionInfo.RankArtName(i + 1);
            Ok(got == want[i],
               $"★ A·6 名次 {i + 1} = `{want[i]}`（原版资源文件名）—— 实测 `{got ?? "<null>"}`");
        }
        Ok(RankedDivisionInfo.RankArtName(0) == null && RankedDivisionInfo.RankArtName(7) == null,
           "★ A·6b 名次号越界（0 与 7）**返回 null**｜🧨 取模 ⇒ 红");

        // 6 张图都取得到 + 正方形 + 同尺寸（原版 128²）
        int w0 = -1; bool allSame = true, allSquare = true, allFound = true;
        for (int i = 0; i < want.Length; i++)
        {
            var tex = CardArt.MenuUi(want[i]);
            if (tex == null) { allFound = false; continue; }
            if (tex.width != tex.height) allSquare = false;
            if (w0 < 0) w0 = tex.width; else if (tex.width != w0) allSame = false;
        }
        Ok(allFound, $"★ A·6c 6 张名次图**都取得到**（`Resources/Art/ui_menu/Roman_I..VI`）"
                   + $"｜🧨 少一张 ⇒ 红（现状：{Describe(want)}）");
        Ok(allSquare && allSame,
           $"★ A·6d 6 张名次图**都是正方形、同一尺寸**（原版 128²；实测首张 {w0}）"
         + "｜🧨 某张被导成横图 / 尺寸不一 ⇒ 按 PA 内接会画错大小 ⇒ 红");
    }

    // ==================================================================
    //  A·5 · 官方词条 `Ranked/Division/N`
    // ==================================================================
    static void CheckLocalization()
    {
        var got = new string[7];
        bool allOk = true;
        for (int i = 0; i < 7; i++)
        {
            got[i] = RankedDivisionInfo.DivisionName(i + 1);
            if (string.IsNullOrEmpty(got[i])) allOk = false;
        }
        Ok(allOk, "★ A·5a `Ranked/Division/1..7` **七条都取得到**（`Loc.T`；取不到时 `DivisionName` 会 `LogWarning`＋返回空串）"
                + $"｜实测 = [{string.Join(" / ", got)}]");

        for (int i = 0; i < 7; i++)
        {
            string g = got[i];
            bool hit = g == OfficialDivision[i][0] || g == OfficialDivision[i][1];
            Ok(hit, $"★ A·5b 第 {i + 1} 档的段位名 = 官方 `Ranked/Division/{i + 1}`"
                  + $"（`{OfficialDivision[i][0]}` / `{OfficialDivision[i][1]}`）—— 实测 `{g}`"
                  + "｜🧨 号错一位（拿第 5 档的词条当第 4 档）⇒ 红");
        }

        bool distinct = true;
        for (int i = 0; i < 7; i++)
            for (int j = i + 1; j < 7; j++)
                if (got[i] == got[j]) distinct = false;
        Ok(distinct, "★ A·5c 七条段位名**互不相同**｜🧨 键拼错（少个 `/`）⇒ 全落到同一个兜底值 ⇒ 红");
    }

    // ==================================================================
    //  A·2 · 两个**实测**锚点（本件存在的理由）
    // ==================================================================
    static void CheckAnchors()
    {
        // 证据（原版实拍逐块像素比对，报告 → 资料/普查产出_1024c/A1_段位图案映射.md）：
        //   · 金 II（`账户-个人资料参考图.png`）：04 NCC 0.707 > 05 0.577 > 06 0.541 > 03 0.498 > 07 0.315
        //   · 传奇（`多人模式-经典-界面参考图.png`）：07 NCC 0.939 > 06 0.751 > 05 0.661 > 04 0.566 > 03 0.503
        Ok(RankedDivisionInfo.DivisionArtName(4) == "04-Admiral",
           "★ A·2 **锚点①** `DivisionArtName(4) == \"04-Admiral\"` —— 原版实拍「当前排名 **金 II**」那颗徽记与 "
         + "`04-Admiral` 的掩盖归一化相关 **NCC 0.707**（次优 `05-Conqueror` 0.577），"
         + "且「金」= 官方 `Ranked/Division/4` ⇒ **图案与文字两条独立信号同指 4**"
         + $"—— 实测 `{RankedDivisionInfo.DivisionArtName(4)}`"
         + "｜🧨 4 和 7 对调 / 表整体错一位 ⇒ 红");

        Ok(RankedDivisionInfo.DivisionArtName(7) == "07-Legend",
           "★ A·2 **锚点②** `DivisionArtName(7) == \"07-Legend\"` —— 原版实拍「排名 / **传奇**」那颗徽记与 "
         + "`07-Legend` 的 **NCC 0.939**（次优 `06-Galactic Threat` 0.751）⇒ 基本直接认定；"
         + "且「传奇」= `Ranked/Division/7` = `Legendary`、图名也是 `Legend`"
         + $"—— 实测 `{RankedDivisionInfo.DivisionArtName(7)}`"
         + "｜🧨 把 7 换成 06（两族里最像它的一张）⇒ 红");

        // 灭自证的另一半：两个锚点必须是**不同的两项**（🧨 同一条 `if` 两边都返同一张 ⇒ 上面两条仍可能同时绿）
        Ok(RankedDivisionInfo.DivisionArtName(4) != RankedDivisionInfo.DivisionArtName(7),
           "★ A·2c 锚点①②**不是同一张图**（4 ≠ 7）｜🧨 把映射写成「反正都返回 07-Legend」⇒ 红");

        // 段位表 7 张都取得到 + 正方形 + 同尺寸（原版 512²）
        int w0 = -1; bool allSame = true, allSquare = true, allFound = true;
        for (int d = 1; d <= RankedDivisionInfo.DivisionArtCount; d++)
        {
            string n = RankedDivisionInfo.DivisionArtName(d);
            var tex = CardArt.MenuUi(n);
            if (tex == null) { allFound = false; continue; }
            if (tex.width != tex.height) allSquare = false;
            if (w0 < 0) w0 = tex.width; else if (tex.width != w0) allSame = false;
        }
        Ok(allFound, "★ A·4 7 张段位图**都取得到**（`Resources/Art/ui_menu/01-Rook … 07-Legend`）"
                   + $"｜🧨 少一张（`ArtInternal` 那边会静默空一层）⇒ 红（现状：{DescribeDivision()}）");
        Ok(allSquare && allSame,
           $"★ A·4b 7 张段位图**都是正方形、同一尺寸**（原版 512²；实测首张 {w0}）"
         + "｜🧨 某张被导成横图 ⇒ `m_PreserveAspect` 内接后大小就不对 ⇒ 红");
    }

    // ==================================================================
    //  B·1 / B·2 / B·3 · 排名页（档案窗第 6 页）
    // ==================================================================
    static void CheckTab(RankedTab tab)
    {
        string want = RankedDivisionInfo.DivisionArtName(RankedDivisionInfo.DemoDivisionNumber);
        Ok(!string.IsNullOrEmpty(want),
           "★ B·0 演示段位号 `DemoDivisionNumber = " + RankedDivisionInfo.DemoDivisionNumber
         + "` 在表里**有图**（否则两个窗口都画不出）｜🧨 演示号改成 0/8 ⇒ 红");

        Ok(tab.DivisionArtUsed == want,
           $"★ B·1a 排名页记下的图名 == `RankedDivisionInfo.DivisionArtName({RankedDivisionInfo.DemoDivisionNumber})`"
         + $" = `{want}` —— 实测 `{tab.DivisionArtUsed ?? "<null（没建）>"}`"
         + "｜🧨 排名页自己另挑一套（比如写死 `04-Admiral`）⇒ 与事件窗脱钩 ⇒ 红");

        var di = MenuCheck.FindChild(tab.transform, "DivisionImage");
        if (!Ok(di != null, "★ B·2a 前提：`Top4/center/DivisionImage` 节点在")) return;
        var img = MenuCheck.FindChild(di, "Image");
        var q = img != null ? img.GetComponentInChildren<ImageQuad>(true) : null;
        if (!Ok(q != null, "★ B·2b 前提：`DivisionImage/Image` 上挂了 `ImageQuad`（= 图**真的画了**）"
                        + $"｜🧨 回到「不画段位图」那一版 ⇒ 红"))
            return;

        Ok(q.Texture != null && q.Texture.name == want,
           $"★ B·2c `ImageQuad.Texture.name` == 图名 `{want}` —— 实测 `{(q.Texture != null ? q.Texture.name : "<null>")}`"
         + "｜🧨 挂错图 / 走了 `CardArt.Solid()`（纯白）⇒ 红");

        // 几何：512² 经 `m_PreserveAspect = 1` 内接到 369.298×133.282 的框里 ⇒ 133.282² 居中
        var box = MenuDraw.QuadRectPx(q);
        float wantSide = Mathf.Min(DiR - DiL, DiB - DiT);            // 369.30 vs 133.282 ⇒ 133.282
        float cx = (DiL + DiR) * 0.5f, cy = (DiT + DiB) * 0.5f;
        Ok(Mathf.Abs(box.W - wantSide) <= 1.5f && Mathf.Abs(box.H - wantSide) <= 1.5f,
           $"★ B·2d 画出来 = **{wantSide:F2}²**（原版 prefab `DivisionImage` 369.298×133.282 + `m_PreserveAspect = 1`，"
         + $"512² 的图内接到高 133.282）—— 实测 {box.W:F2}×{box.H:F2}"
         + "｜🧨 拉满 369.30 宽（丢 `keepAspect`）⇒ 红");
        Ok(Mathf.Abs((box.x1 + box.x2) * 0.5f - cx) <= 1.5f && Mathf.Abs((box.y1 + box.y2) * 0.5f - cy) <= 1.5f,
           $"★ B·2e 画出来**居中于** ({cx:F2}, {cy:F2})（原版框心）—— 实测 ({(box.x1 + box.x2) * 0.5f:F2}, "
         + $"{(box.y1 + box.y2) * 0.5f:F2})｜🧨 锚点/内接算反 ⇒ 会贴左上或贴右 ⇒ 红");

        var ri = MenuCheck.FindChild(di, "RankImage");
        Ok(ri != null && !ri.gameObject.activeSelf,
           "★ B·3 排名页 `DivisionImage/RankImage`（罗马数字）**关着** —— 名次号来自服务器，"
         + "且「最高段位叠不叠名次」参考图判不了（`资料/普查产出_1024b/S4_模式窗细节与徽章.md` §缺数据）"
         + $"｜实测 activeSelf = {(ri != null ? ri.gameObject.activeSelf.ToString() : "<节点不在>")}"
         + "｜🧨 没有任何真名次就点亮 ⇒ 红");
    }

    // ==================================================================
    //  B·1b · 事件窗（排位窗左列）—— 口径一致的另一半
    // ==================================================================
    static void CheckEventWindow(RankedTab tab)
    {
        var win = FindBuilt<RankedEventWindow>(t => MenuCheck.FindChild(t, "Ranked Division Info") != null);
        var rdi = win != null ? MenuCheck.FindChild(win.transform, "Ranked Division Info") : null;
        var content = rdi != null ? MenuCheck.FindChild(rdi, "Content") : null;
        var di = content != null ? MenuCheck.FindChild(content, "DivisionImage") : null;
        if (di == null)
        {
            Skip("阶段 B·1b：场景里没有建好的排位事件窗（`Ranked Division Info/Content/DivisionImage` 不在 ——"
               + " 那扇窗要开过才会建）⇒ **两个窗口画的是不是同一张**这一条这一趟没验；"
               + " 它的一半已经由 B·1a 保证（排名页读完 **同一个** `RankedDivisionInfo.DivisionArtName`）。");
            return;
        }

        var img = MenuCheck.FindChild(di, "Image");
        var q = img != null ? img.GetComponentInChildren<ImageQuad>(true) : null;
        if (!Ok(q != null, "★ B·1b 前提：事件窗 `Content/DivisionImage/Image` 挂了 `ImageQuad`")) return;

        string want = tab.DivisionArtUsed;
        Ok(q.Texture != null && q.Texture.name == want,
           $"★ B·1b **口径一致**：事件窗与排名页**画的是同一张** —— 事件窗实测 "
         + $"`{(q.Texture != null ? q.Texture.name : "<null>")}` vs 排名页 `{want ?? "<null>"}`"
         + "｜🧨 两处各挑各的（比如事件窗按 7、排名页按 4）⇒ 红");

        var ri = MenuCheck.FindChild(di, "RankImage");
        Ok(ri != null && !ri.gameObject.activeSelf,
           "★ B·3b 事件窗的 `RankImage` 也**关着**（与排名页同一口径）"
         + $"｜实测 activeSelf = {(ri != null ? ri.gameObject.activeSelf.ToString() : "<节点不在>")}");
    }

    // ==================================================================
    //  小工具
    // ==================================================================
    static bool Ok(bool cond, string msg)
    {
        if (cond) { Pass++; Debug.Log(P + $"   ✓ {msg}"); return true; }
        Fail++; Debug.LogError(P + $"   ✗ {msg}");
        return false;
    }

    static void Skip(string msg)
    {
        Skipped++;
        Debug.LogWarning(P + $"   ↷ （跳过）{msg}");
    }

    static string Describe(string[] names)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < names.Length; i++)
        {
            if (i > 0) sb.Append(" · ");
            sb.Append(CardArt.MenuUi(names[i]) != null ? "有" : "**缺**").Append(names[i]);
        }
        return sb.ToString();
    }

    static string DescribeDivision()
    {
        var names = new string[RankedDivisionInfo.DivisionArtCount];
        for (int i = 0; i < names.Length; i++) names[i] = RankedDivisionInfo.DivisionArtName(i + 1);
        return Describe(names);
    }

    /// <summary>在场景里找一个**建好的** T 实例（含 inactive —— 档案窗只有当前页是激活的）。
    /// `built` = 判定「这一份真的建过」的依据（⛔ 别用 `FindObjectOfType`：它只看激活对象）。</summary>
    static T FindBuilt<T>(System.Func<Transform, bool> built) where T : Component
    {
        var all = Resources.FindObjectsOfTypeAll<T>();
        for (int i = 0; i < all.Length; i++)
        {
            var c = all[i];
            if (c == null) continue;
            var go = c.gameObject;
            if (go == null || !go.scene.IsValid()) continue;      // 跳过资产 / prefab 模板
            if (built(c.transform)) return c;
        }
        return null;
    }
}
