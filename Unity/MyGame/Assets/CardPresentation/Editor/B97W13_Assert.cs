// B97W13_Assert.cs — 执行代理 `B97_W13`（2026-10-29 · 第九十七会话 · 波 1 补位）的断言
//
// ============================ 盯的是什么 ============================
//   账 `未分类/其他:295`（`FactionScore.Initialize(CardArmy, int, int)`）的**第二半**：
//   `RankedTab.BuildCard`（`Top4` 那四格 `#N FactionScoreBig`）也接上 `FactionScore`。
//   判据（⛔ 别在这里抄第二份）：
//     · `d:/2/tools/decomp_full/RankedTab__InitializeFactionScores.c` —— 循环里
//       `FactionScore__Initialize(*(top4Factions + 0x20 + i*8), army, value, highestValue)`：
//       **`param_1 + 0x48`（`FactionScore[] top4Factions`）那四格是「先复用树上已有的实例」**，
//       第 5 名往后才 `Instantiate(factionScorePrefab)`。⇒ 原版**四格 `FactionScoreBig` 与列表行 `FactionScoreSmall`
//       走的是同一个 `FactionScore.Initialize`**，我们原来只接了 `BuildRow`（`B97_W2`）那一半。
//     · `d:/2/tools/decomp_full/FactionScore__Initialize.c` 三句：徽记 `GetArmyIcon(faction)` + 两颗 `AllianceRatingDisplay.Initialize`。
//     · 13 条「图名 ↔ 阵营」= 原版 `ArmyIconsSO`（`sharedassets0.assets` 对象 422 的原始字节）→ `Core/ArmyIcons.cs` 那张表。
//
// ============================ 怎么用（宿主）============================
//     B97W13Assert.Run();
//     _sink.Pass += B97W13Assert.Pass; _sink.Fail += B97W13Assert.Fail;
//   · 推荐宿主 = `Editor/MainMenuScene.cs`（`MainMenuScene.Run`），紧跟 `ProfileAssertU2b.Run();` 那两行 ——
//     C 段要一棵**真建出来的档案窗排位页**（本文件自己开窗：`PlayerProfileWindow.LastOpened` 拿不到就
//     `MainMenuRuntime.OpenProfile()`；开不出来 ⇒ 记一条 FAIL 并出声，⛔ 不静默跳过）。
//   · 本文件**不建持久资产**；临时改的 `RankedTab.Top4Data` 在 `finally` 里原样放回并重建（批处理下不写玩家真档）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A 段：把 `FactionOfSprite` 改成「恒返 `Neutral`」/ 扫错字段（`Faction` 而不是 `Sprite`）⇒ A2 全红。
//   · B 段：把 `FactionOfSprite` 认不出那条 `Debug.LogWarning` 删掉 ⇒ B2 红；认不出时返回某个真阵营 ⇒ B1 红。
//   · C 段：`BuildCard` 里 `AddComponent<FactionScore>` / `fs.Bind` / `fs.InitializeOrEmpty(...)` 任一删掉
//           ⇒ C2/C3 红（`icon` 只剩透明占位）；把 `FactionOfSprite` 改成恒返同一个阵营 ⇒ C4（四张换色判别式）红。
//   · ⛔ 期望值一律**不读被测常量**：A 段的 13 对写的是原版 `IconsByArmySO` 的字面量；
//     C 段的分数串是我们**喂进** `Top4Data` 的，图名是 `ArmyIcons.IconName(army)`（= 原版那张表的读口）。
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `W13`：`RankedTab.BuildCard` 的 `FactionScore` 接线 + `FactionOfSprite` 的断言。
/// 计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。</summary>
public static class B97W13Assert
{
    static readonly CheckSink _sink = new CheckSink("[W13] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主并进自己的总数）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    /// <summary>原版 `IconsByArmySO` 的 13 条（**字面量**，⛔ 不是从 `ArmyIcons.All` 现算 —— 那是自证）。
    /// 出处 = `Core/ArmyIcons.cs` 文件头 ② 的原始字节实读（`sharedassets0.assets` 对象 422，
    /// `m_Name = "IconsByArmySO"`、13 × `[int32 army][PPtr]`），交叉印证 = `bundle_armyicons_assets_all`
    /// 那个 `SpriteAtlas`（`Army Icons`）里正好 13 个 `40k_DeckSelection_icon_*`。
    /// ⚠️ 名字**不规则**：`Genestealers` / `SpaceWolves` 不带 `Faction`、`Goff → FactionOrks`。</summary>
    struct Pair { public int Army; public string Sprite; }
    static readonly Pair[] Original =
    {
        new Pair { Army =  10, Sprite = "40k_DeckSelection_icon_FactionUM"               },
        new Pair { Army =  20, Sprite = "40k_DeckSelection_icon_FactionOrks"            },
        new Pair { Army =  30, Sprite = "40k_DeckSelection_icon_FactionSaimHann"        },
        new Pair { Army =  40, Sprite = "40k_DeckSelection_icon_FactionSautekh"         },
        new Pair { Army =  50, Sprite = "40k_DeckSelection_icon_FactionBlackLegion"     },
        new Pair { Army =  60, Sprite = "40k_DeckSelection_icon_FactionLeviathan"       },
        new Pair { Army =  70, Sprite = "40k_DeckSelection_icon_FactionTauEmpire"       },
        new Pair { Army =  80, Sprite = "40k_DeckSelection_icon_FactionSororitas"       },
        new Pair { Army =  90, Sprite = "40k_DeckSelection_icon_Genestealers"           },
        new Pair { Army = 100, Sprite = "40k_DeckSelection_icon_FactionAstraMilitarum"  },
        new Pair { Army = 110, Sprite = "40k_DeckSelection_icon_FactionDarkAngels"      },
        new Pair { Army = 120, Sprite = "40k_DeckSelection_icon_FactionEmperorsChildren"},
        new Pair { Army = 130, Sprite = "40k_DeckSelection_icon_SpaceWolves"            },
    };

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "W13 `RankedTab` 的 `Top4` 接 `FactionScore`（原版 `RankedTab.InitializeFactionScores` 的 `top4Factions` 那四格）");
        SectionA_十三阵营反查();
        SectionB_认不出时出声();
        SectionC_生产接线();
        Debug.Log(_sink.P + "W13 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A 图名 → 阵营（13 条一一对应）
    static void SectionA_十三阵营反查()
    {
        MenuCheck.Section(_sink, "A `FactionOfSprite`：原版 `IconsByArmySO` 的 13 条图名逐条反查回 `CardArmy`");

        C(Original.Length, 13, "A0 **前提断言**：判据样本 = 原版那 13 条（`IconsByArmySO` 原始字节实读）");
        C(ArmyIcons.All.Length, 13,
          "A1 **前提断言**：`ArmyIcons.All` 也是 13 条（两边条数对不上 ⇒ A2 逐条会先在哪里缺一格）");

        var seen = new System.Collections.Generic.HashSet<int>();
        for (int i = 0; i < Original.Length; i++)
        {
            var want = (CardArmy)Original[i].Army;
            var got = RankedTab.FactionOfSprite(Original[i].Sprite);
            C(got, want, "A2[" + i + "] 图名「" + Original[i].Sprite + "」⇒ `" + want + "`（" + Original[i].Army
                       + "）｜🧨 恒返 `Neutral` / 扫 `Faction` 字段而不是 `Sprite` ⇒ 本条红");
            seen.Add((int)got);
        }
        C(seen.Count, 13,
          "A3 ★ 13 条图名映射出 **13 个互不相同**的阵营（一一对应；有两条撞到同一个阵营 ⇒ 反查写成了兜底）");

        // 反向读口交叉核对：`ArmyIcons` 那张表自己也要能由 army 取回同一个图名
        int roundTrip = 0;
        for (int i = 0; i < Original.Length; i++)
            if (ArmyIcons.IconName(Original[i].Army) == Original[i].Sprite) roundTrip++;
        C(roundTrip, 13, "A4 反向读口 `ArmyIcons.IconName(army)` 逐条回得到同一张图名（13/13；两张表不许各说一套）");
    }

    // ============================================================ B 认不出 / 空 ⇒ Neutral + 出声
    static void SectionB_认不出时出声()
    {
        MenuCheck.Section(_sink, "B 未知图名 ⇒ `Neutral`(0) + **出声**（原版 `ArmyIconsSO` 没这一条 ⇒ `GetArmyIcon` 返 null）");

        const string Bogus = "NoSuchSprite_B97W13";
        string got = null;
        Application.LogCallback cb = (m, stack, type) =>
        {
            if (type == LogType.Warning || type == LogType.Error)
                if (m != null && m.Contains(Bogus)) got = m;
        };
        Application.logMessageReceived += cb;
        CardArmy r;
        try { r = RankedTab.FactionOfSprite(Bogus); }
        finally { Application.logMessageReceived -= cb; }

        C(r, (CardArmy)ArmyIcons.Neutral,
          "B1 ★ 认不出的图名 ⇒ `Neutral`(0)（原版 `ArmyIconsSO.TryGetItem` 没命中 ⇒ 返回 null 图、⛔ 不顶替别的阵营）"
        + "｜🧨 改成返回某个真阵营 ⇒ 红");

        Tr(got != null && got.Contains(Bogus),
           "B2 ★ 认不出时**真的出声了**，而且报文里带那个图名（实得：" + (got == null ? "<没捕获到任何 warning>" : "「" + got + "」") + "）"
         + "｜🧨 把 `FactionOfSprite` 的防守分支（那条 `Debug.LogWarning`）删掉 ⇒ 本条红（静默失败 = 红线）");

        // 空 / null：不崩、同样落 Neutral
        CardArmy r2 = RankedTab.FactionOfSprite(null);
        C(r2, (CardArmy)ArmyIcons.Neutral, "B3 `null` 图名 ⇒ `Neutral`(0)、**不抛**（`Top4Entry.FactionArt` 允许空）");
        CardArmy r3 = RankedTab.FactionOfSprite("");
        C(r3, (CardArmy)ArmyIcons.Neutral, "B4 空串图名 ⇒ `Neutral`(0)、**不抛**");
    }

    // ============================================================ C 生产接线（真建出来的那四格）
    static void SectionC_生产接线()
    {
        MenuCheck.Section(_sink, "C 生产接线：`RebuildTop4` 建出来的 `#N FactionScoreBig` 上**有 `FactionScore`**，徽记与分数都按喂进去的数据画");

        var pp = PlayerProfileWindow.LastOpened;
        if (pp == null) pp = MainMenuRuntime.OpenProfile();
        Tr(pp != null, "C0 **前提断言**：档案窗开出来了（`PlayerProfileWindow.LastOpened` 或 `MainMenuRuntime.OpenProfile()`）"
                     + "—— 不成立 ⇒ C1..C7 **没跑**（⛔ 不静默跳过）");
        if (pp == null) return;

        var ranked = pp.Page(WindowTabType.ProfileRanking) as RankedTab;
        Tr(ranked != null, "C1 **前提断言**：排位页是 `RankedTab`（第 6 颗键藏起来了，但页照建）");
        if (ranked == null) return;

        // 四个**不同**阵营 ⇒ 四张徽记必须四张不同的图（同图 = 映射被写死）
        int[] armies = { 10, 50, 80, 130 };
        string[] sprites = new string[4];
        for (int i = 0; i < 4; i++) sprites[i] = ArmyIcons.IconName(armies[i]);
        Tr(sprites[0] != null && sprites[1] != null && sprites[2] != null && sprites[3] != null,
           "C2 **前提断言**：选中的 4 个阵营（10/50/80/130）在 `ArmyIcons` 里都有徽记图名（否则下面比图名是空转）");
        if (sprites[0] == null || sprites[1] == null || sprites[2] == null || sprites[3] == null) return;

        var keep = RankedTab.Top4Data;
        string[] Wanted = { "1111", "2222", "3333", "4444" };
        try
        {
            RankedTab.Top4Data = new[]
            {
                new RankedTab.Top4Entry { FactionArt = sprites[0], Rating = Wanted[0], MaxRating = "901" },
                new RankedTab.Top4Entry { FactionArt = sprites[1], Rating = Wanted[1], MaxRating = "902" },
                new RankedTab.Top4Entry { FactionArt = sprites[2], Rating = Wanted[2], MaxRating = "903" },
                new RankedTab.Top4Entry { FactionArt = sprites[3], Rating = Wanted[3], MaxRating = "904" },
            };
            ranked.RebuildTop4();

            var texNames = new System.Collections.Generic.HashSet<string>();
            for (int i = 1; i <= 4; i++)
            {
                var card = MenuCheck.FindChild(ranked.transform, "#" + i + " FactionScoreBig");
                Tr(card != null && card.gameObject.activeSelf,
                   "C3[" + i + "] 有数据 ⇒ `#" + i + " FactionScoreBig` **画出来了**（`SetActive(true)`）");
                if (card == null) continue;

                var fs = card.GetComponent<CardPresentation.ScoreBars.FactionScore>();
                Tr(fs != null,
                   "C4[" + i + "] ★ `#" + i + "` 上挂着 **`FactionScore`**（原版这四格就是 `RankedTab.top4Factions` 那个数组的元素）"
                 + "｜🧨 把 `BuildCard` 里 `AddComponent<FactionScore>()` 删掉 ⇒ 本条红");

                var iconNode = card.Find("icon");
                var q = iconNode != null ? iconNode.GetComponentInChildren<ImageQuad>(true) : null;
                string texName = q != null && q.Texture != null ? q.Texture.name : null;
                C(texName, sprites[i - 1],
                   "C5[" + i + "] ★ 徽记画的是「" + sprites[i - 1] + "」（= 原版 `ArmyIconsSO` 里 army " + armies[i - 1]
                 + " 那张；实得「" + (texName ?? "<null>") + "」）"
                 + "｜🧨 徽记图改回 `BuildCard` 里自己画 / `fs.Bind` / `fs.InitializeOrEmpty` 任一删掉 ⇒ 只剩透明占位 ⇒ 本条红");
                if (texName != null) texNames.Add(texName);

                C(TextOf(Find(card, "Alliance Rating Display/Individual rating value")), Wanted[i - 1],
                  "C6[" + i + "] 大分 = 喂进去的「" + Wanted[i - 1] + "」（原版那三句里 `ratingDisplay.Initialize(currentScore, …)`；"
                + "⛔ 期望值是**我们喂进去的字符串**，不读实现常量）");
                C(TextOf(Find(card, "MaxRating/Individual rating value")), "90" + i,
                  "C7[" + i + "] 最高分 = 喂进去的「90" + i + "」（第二颗评级 `highestRatingDisplay`；`Big` 那一格的节点名是 `MaxRating`）");
            }

            C(texNames.Count, 4,
              "C8 ★ **灭自证**：四张卡（四个不同阵营）画出来的是 **4 张不同的徽记**（实得 " + texNames.Count + " 张）"
            + "｜🧨 `FactionOfSprite` 写成恒返同一个阵营 ⇒ 四张同图 ⇒ 本条红");
        }
        finally
        {
            RankedTab.Top4Data = keep != null ? keep : new RankedTab.Top4Entry[0];
            ranked.RebuildTop4();
            var c1 = MenuCheck.FindChild(ranked.transform, "#1 FactionScoreBig");
            bool visible = c1 != null && c1.gameObject.activeSelf;
            Tr(c1 != null && visible == RankedTab.HasTop4Data,
               "C9 收尾：`Top4Data` **原样放回**并重建一次，四格显隐回到放回后的那一支（当前 `HasTop4Data` = "
             + RankedTab.HasTop4Data + "、`#1` 亮着 = " + visible + "）");
        }
    }

    // ============================================================ 小工具
    /// <summary>按 `A/B/C` 路径找节点 —— **每段必须是上一段的直接子件**（`RatingRow` 建的就是这种形状）。
    /// ⚠️ 深节点（`#N FactionScoreBig`）一律走 <see cref="MenuCheck.FindChild"/>（真递归）。</summary>
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
}
