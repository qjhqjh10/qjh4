// B112_T2C_Assert.cs — 把 `B-112` 普查判出来的「期望值 = 我们自己的读取函数」那 13 组，
// 逐组补成**字面量断言**（账 `B-112` 续办 · 执行代理 `B111_T2B` · 2026-11-01）。
//
// 背景（为什么要这个文件）
// ------------------------------------------------------------------
// `报告/B112_T2C.md` 逐条看了 126 条「期望值来自本工程调用」的断言，结论：
//   **恒真自证 0 条**；但 **25 条**的期望值来自一个**自身没有被任何字面量钉死**的判断函数
//   ⇒ 那 25 条证明不了那个函数对不对（改错了，期望值跟着一起错）。
// 本文件把那 13 个函数各钉一条**外部真值**（原版反编译行号 / 原版资产名 / 原版数据表）。
//
// 🔴 本文件**只读、不建窗口、不动场景、不跑播放**，也不改任何实现 ⇒ 挂到哪个宿主都安全。
// 🔴 每条都写了「灭自证：改哪一处实现会让本条红」。**若某条改成自比，它就不再是这个文件。**
// ⚠️ 本文件**不做**的 5 组写在文件尾 §未做，并写清了为什么（拿不到原版真值 ⇒ ⛔ 不编）。
//
// 宿主接线（本文件**不自己接**，宿主不在本代理白名单里）：
//     B112_T2C_Assert.Run();
//     pass += B112_T2C_Assert.Pass; fail += B112_T2C_Assert.Fail;
using System.Text.RegularExpressions;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>`B-112`：13 个「被当期望值用、却没人钉死」的判断函数 —— 逐组补**外部真值**字面量。</summary>
public static class B112_T2C_Assert
{
    public static int Pass;
    public static int Fail;

    static void Check(bool ok, string what)
    {
        if (ok) { Pass++; return; }
        Fail++;
        Debug.LogError("[B112_T2C] ✗ " + what);
    }

    static void CheckEq(string got, string want, string what)
    {
        Check(got == want, what + "（期望 `" + want + "` · 实得 `" + (got ?? "null") + "`）");
    }

    static void CheckEq(int got, int want, string what)
    {
        Check(got == want, what + "（期望 " + want + " · 实得 " + got + "）");
    }

    public static void Run()
    {
        Pass = 0; Fail = 0;
        Debug.Log("[B112_T2C] ===== B-112 字面量断言开始 =====");

        TriggersTable();
        FactionIcons();
        MatchTypesTable();
        BoardSlotsTable();
        FilterTitles();
        RuleCodesNoFallback();
        ForgePointsFormat();
        CardbackDefaults();

        Debug.Log("[B112_T2C] ===== 合计 pass=" + Pass + " fail=" + Fail + " =====");
    }

    // ════════════════════════════════════════════════════════════════════════════════════════
    // ① `WhenEventKind.Triggers(kw)` —— **keyword → Kind 的字面量表**（普查里一次带出 7 条）
    //    外部真值：
    //      · **关键词名单** = 原版卡面原文的 7 个关键词（`Ferocity` / `Duty` / `Agenda` /
    //        `Swarm` / `Synapse` / `Mob` / `Sacrifice`）—— 见 `RuleEngine/Data/KeywordTable.cs` 的实现位。
    //      · **短语** = 原版卡面写法（例：`When a friendly troop uses Ferocity` = `Raid Tactics`，
    //        见 `RuleEngineTest.cs:20968` 的注释）。
    //      · `"triggers:"` 前缀是**我们自己的约定**（`WhenEvent.cs:620` `TriggersPrefix`），
    //        这条断的是「契约不许悄悄改」（改了 = 全工程的监听器静默不响）。
    //    灭自证：把 `Triggers` 改成「永远返回常量」⇒ **§①-3 的「7 个 Kind 两两不同」红**；
    //            把 `Trim().ToLowerInvariant()` 删掉 ⇒ §①-2 红；把某个关键词从 `WhenEvents.Parse`
    //            的认字表里删掉 ⇒ 对应那行红。
    // ════════════════════════════════════════════════════════════════════════════════════════
    static void TriggersTable()
    {
        // (原版卡面短语, 关键词, 期望 Kind 的**字面量**)
        var tab = new[]
        {
            new[] { "a friendly unit uses ferocity",            "ferocity",  "triggers:ferocity"  },
            new[] { "a friendly unit uses duty",                "duty",      "triggers:duty"      },
            new[] { "a friendly unit uses agenda",              "agenda",    "triggers:agenda"    },
            new[] { "a friendly unit uses swarm",               "swarm",     "triggers:swarm"     },
            new[] { "a friendly unit uses synapse",             "synapse",   "triggers:synapse"   },
            new[] { "a friendly unit triggers Mob",             "mob",       "triggers:mob"       },
            new[] { "a friendly unit uses sacrifice",           "sacrifice", "triggers:sacrifice" },
        };

        var kinds = new System.Collections.Generic.List<string>();

        // ①-1 每个关键词：Parse 认得出 + Kind = 字面量
        for (int i = 0; i < tab.Length; i++)
        {
            string phrase = tab[i][0], kw = tab[i][1], wantKind = tab[i][2];
            var ev = WhenEvents.Parse(phrase, recordUnknown: false);
            // 前提断言先写（⛔ 不写 `if (ev != null) Check(...)`）
            Check(ev != null, "①-1 前提：`" + phrase + "` 解析得出来（关键词 `" + kw + "`）");
            if (ev == null) continue;
            CheckEq(ev.Kind, wantKind, "①-1 `" + phrase + "` 的 Kind = 字面量 `" + wantKind + "`");
            kinds.Add(ev.Kind);
        }

        // ①-2 归一化契约：大小写/空白必须被吃平（`WhenEvent.cs:626` `Trim().ToLowerInvariant()`）
        CheckEq(WhenEventKind.Triggers("  Ferocity "), "triggers:ferocity",
                "①-2 `Triggers()` 吃平大小写与空白（原版卡面是 `Ferocity`，入参不保证小写）");

        // ①-3 🔴 灭自证：7 个 Kind 必须**两两不同**
        var uniq = new System.Collections.Generic.HashSet<string>(kinds);
        CheckEq(uniq.Count, tab.Length,
                "①-3 灭自证：7 个关键词的 Kind **两两不同**（`Triggers` 退化成常量 ⇒ 本节全绿、只这条红）");
    }

    // ════════════════════════════════════════════════════════════════════════════════════════
    // ② `DeckRuntime.FactionIcon(army)` / `ArmyIcons.IconName` —— **13 行原版图名表**
    //    外部真值：`IconsByArmySO`（`sharedassets0.assets` 对象 **422**，`m_Name = "IconsByArmySO"`，
    //    260 B = `[int32 count=13] + 13 × [int32 army][PPtr]`）逐条解出的 sprite 名 ——
    //    判据链全文写在 `CardPresentation/Core/ArmyIcons.cs` 文件头 ②。
    //    交叉印证：`bundle_armyicons_assets_all` 里正好 13 个 `40k_DeckSelection_icon_*`，名字一字不差。
    //    灭自证：把表里任意一行的 `Sprite` 抄错 / 把 `Goff → FactionOrks` 这种不规则映射「统一」成前缀拼接 ⇒ 对应行红；
    //            把 `Neutral` 也塞一条默认图 ⇒ §②-2 红。
    // ════════════════════════════════════════════════════════════════════════════════════════
    static void FactionIcons()
    {
        // (CardArmy 枚举值, 阵营名, 原版 sprite 名**字面量**)；顺序 = IconsByArmySO 的序列化顺序
        var tab = new[]
        {
            new object[] {  10, "Ultramarines",     "40k_DeckSelection_icon_FactionUM"                 },
            new object[] {  20, "Goff",             "40k_DeckSelection_icon_FactionOrks"               },
            new object[] {  30, "SaimHann",         "40k_DeckSelection_icon_FactionSaimHann"           },
            new object[] {  40, "Sautekh",          "40k_DeckSelection_icon_FactionSautekh"            },
            new object[] {  50, "BlackLegion",      "40k_DeckSelection_icon_FactionBlackLegion"        },
            new object[] {  60, "Leviathan",        "40k_DeckSelection_icon_FactionLeviathan"          },
            new object[] {  70, "TauEmpire",        "40k_DeckSelection_icon_FactionTauEmpire"          },
            new object[] {  80, "Sororitas",        "40k_DeckSelection_icon_FactionSororitas"          },
            new object[] {  90, "Genestealers",     "40k_DeckSelection_icon_Genestealers"              },
            new object[] { 100, "AstraMilitarum",   "40k_DeckSelection_icon_FactionAstraMilitarum"     },
            new object[] { 110, "DarkAngels",       "40k_DeckSelection_icon_FactionDarkAngels"         },
            new object[] { 120, "EmperorsChildren", "40k_DeckSelection_icon_FactionEmperorsChildren"   },
            // ⚠️ SpaceWolves 与上面的 Genestealers 一样**不带 `Faction` 前缀**（多数派带、这两张不带）—— 原版 asset 名就是这样，
            //    见 `d:/2/新解包资源/assets_full/bundle_armyicons_assets_all/Sprite/`（不存在任何 `…_FactionSpaceWolves`）。
            new object[] { 130, "SpaceWolves",      "40k_DeckSelection_icon_SpaceWolves"               },
        };

        // ②-0 前提：原版 SO 就是 13 条（`IconsByArmySO` 的 count 字段 = 13）
        CheckEq(ArmyIcons.All.Length, 13, "②-0 前提：原版 `IconsByArmySO` 是 13 条（SO 的 int32 count = 13）");

        for (int i = 0; i < tab.Length; i++)
        {
            int army = (int)tab[i][0];
            string faction = (string)tab[i][1];
            string wantSprite = (string)tab[i][2];

            CheckEq(ArmyIcons.IconName(army), wantSprite,
                    "②-1 `CardArmy." + army + "`（`" + faction + "`）的徽记 = 原版 sprite 名");
            CheckEq(ArmyIcons.IconName(faction), wantSprite,
                    "②-1b 按**阵营名**取，同一张（`" + faction + "`）");
            // 同一份表：运行时页头那颗 `hdr_army` 走的就是 `DeckRuntime.FactionIcon`
            CheckEq(DeckRuntime.FactionIcon(faction), wantSprite,
                    "②-1c `DeckRuntime.FactionIcon(\"" + faction + "\")` = 原版 sprite 名（页头 `hdr_army` 的取图口）");
        }

        // ②-2 原版 SO **没有 `Neutral(0)`** ⇒ 取图返回 null；`FactionIcon` 那一层才兜「卡组图标」
        Check(ArmyIcons.IconName(0) == null,
              "②-2 原版 `IconsByArmySO` 没有 `Neutral(0)` ⇒ `ArmyIcons.IconName(0)` 必须 null（⛔ 别拿默认图顶）");
        CheckEq(DeckRuntime.FactionIcon(null), "40k_collection_bt_decks",
                "②-2b `DeckRuntime.FactionIcon` 对认不出的阵营兜「卡组图标」（`DeckRuntime.cs:8761` 的唯一一层兜底）");
    }

    // ════════════════════════════════════════════════════════════════════════════════════════
    // ③ `MatchTypes.For(playMode)` —— **14 行派发表**，用 MatchType 的**数值字面量**断
    //    外部真值：原版 `MatchData..ctor`（RVA `0x736B70`；`playMode` 在 `+0x18`、`matchType` 在 `+0x1C`，
    //    跳转点 VA `0x180736CAE`、表在 RVA `0x736FE0`）逐项读数 →
    //    `资料/普查产出_1014/RO_战场与窗口判据三件.md` §二 ①·B·5。
    //    灭自证：把任意一档 `case` 的返回值改错 ⇒ 对应行红；把 `Skirmish` 也映射成 `Ranked` ⇒ ③-14 红。
    // ════════════════════════════════════════════════════════════════════════════════════════
    static void MatchTypesTable()
    {
        // (GameMode, playMode 号, 期望 matchType 数值**字面量**, 名字)
        var tab = new[]
        {
            new object[] { GameMode.Classic,         0,  10, "Ranked"           },
            new object[] { GameMode.Duel,            1, 150, "Duel"             },
            new object[] { GameMode.PracticeLodge,   2,  90, "Practice"         },
            new object[] { GameMode.Dungeon,         3,  80, "EventAI"          },
            new object[] { GameMode.Tutorial,        4, 100, "Tutorial"         },
            new object[] { GameMode.CutScene,        5, 190, "Scene"            },
            new object[] { GameMode.OfflinePractice, 6,  50, "PracticeOffline"  },
            new object[] { GameMode.ClosedDeck,      7, 110, "ClosedDeck"       },
            new object[] { GameMode.Campaign,        8, 130, "Campaign"         },
            new object[] { GameMode.TutorialReplay,  9, 140, "TutorialReplay"   },
            new object[] { GameMode.Replay,         10, 160, "Replay"           },
            new object[] { GameMode.RankedFriendly, 11, 170, "Unranked"         },
            new object[] { GameMode.OwnDeckTraining,12,  50, "PracticeOffline"  },
            new object[] { GameMode.Skirmish,       13, 200, "FastMode"         },
        };

        for (int i = 0; i < tab.Length; i++)
        {
            var mode = (GameMode)tab[i][0];
            int num = (int)tab[i][1];
            int want = (int)tab[i][2];
            string name = (string)tab[i][3];
            CheckEq((int)MatchTypes.For(mode), want,
                    "③-" + (i + 1) + " `playMode " + num + "`（" + mode + "）⇒ `matchType " + want + "`（" + name + "）");
        }

        // ③-15 原版那张表**只有 14 项**（0~13）⇒ 越界不给真值（我们走 default 出声，不编）
        Check((int)MatchTypes.For((GameMode)14) != 200,
              "③-15 灭自证：`playMode 14` **不许**被当成 Skirmish（原版表只有 14 项）");
    }

    // ════════════════════════════════════════════════════════════════════════════════════════
    // ④ `BoardSlots.SlotOf(side, index)` —— 槽位换算，用**格号字面量**断
    //    外部真值：`BoardSpec.WarlordSlot = 4`（`BoardSpec.cs:30`，卡牌基座 9 格、督军居中），
    //    原版 `MinionManager` 左 −x / 右 +x ⇒ `我们的格号 = 4 + 原版有符号下标`（左 3 / 右 5 是贴督军那格）。
    //    灭自证：把 `side * (index + 1)` 改成 `side * index` ⇒ ④-1/④-2 红；把左右搞反 ⇒ ④-3 红。
    // ════════════════════════════════════════════════════════════════════════════════════════
    static void BoardSlotsTable()
    {
        CheckEq(BoardSpec.WarlordSlot, 4, "④-0 前提：督军占第 **4** 格（`BoardSpec.cs:30` 字面量）");
        CheckEq(BoardSpec.SlotsPerSide, 4, "④-0b 前提：每侧 **4** 格（同上）");

        CheckEq(BoardSlots.SlotOf(BoardSlots.Left, 0), 3, "④-1 左 0 = 格 **3**（贴督军那格）");
        CheckEq(BoardSlots.SlotOf(BoardSlots.Left, 3), 0, "④-2 左 3 = 格 **0**（最外那格）");
        CheckEq(BoardSlots.SlotOf(BoardSlots.Right, 0), 5, "④-3 右 0 = 格 **5**（贴督军那格）");
        CheckEq(BoardSlots.SlotOf(BoardSlots.Right, 3), 8, "④-4 右 3 = 格 **8**（最外那格）");

        // ④-5 互逆：`SideOf` / `IndexInSide` 必须能把格号解回去（两条独立路径，灭自证）
        for (int side = -1; side <= 1; side += 2)
            for (int idx = 0; idx < BoardSpec.SlotsPerSide; idx++)
            {
                int slot = BoardSlots.SlotOf(side, idx);
                Check(BoardSlots.SideOf(slot) == side && BoardSlots.IndexOf(slot) == idx,
                      "④-5 格 " + slot + " 反解回 side=" + side + " idx=" + idx
                      + "（实得 side=" + BoardSlots.SideOf(slot) + " idx=" + BoardSlots.IndexOf(slot) + "）");
            }

        // ④-6 灭自证：左右两档**不许重叠**（`SideOf` 写成恒 Left/恒 Right ⇒ 上面 ④-5 仍可能绿，这条红）
        Check(BoardSlots.SlotOf(-1, 0) != BoardSlots.SlotOf(+1, 0),
              "④-6 灭自证：左 0 与右 0 **不是同一格**");
    }

    // ════════════════════════════════════════════════════════════════════════════════════════
    // ⑤ `FilterPanelModel.TitleTerm` / `TitleText` —— 筛选栏 5 颗小标题
    //    外部真值：原版 `Deck Editing Menu/…/Filters/{Army Filter,Rarity FIlter,Cost Filter,Type Filter}/Title`
    //    与 `…/Cosmetic FIlter/Filters/Army Filter/Title` 的 `m_text` 依次是
    //    **`Army` / `Rarity` / `Energy Cost` / `Type` / `Army`**（UnityPy 直读真包 `bundle_menus_assets_all`，
    //    全文见 `FilterPanelModel.cs` 的 `TitleText` doc）。
    //    灭自证：把某个 `case` 的键改错 ⇒ 对应行红；把 `default` 改成「编一个词条」⇒ ⑤-5 红。
    // ════════════════════════════════════════════════════════════════════════════════════════
    static void FilterTitles()
    {
        // (原版 m_text 字面量, 期望 I2 键字面量)
        var tab = new[]
        {
            new[] { "Army",        "MenuDeck/Filters/Army" },
            new[] { "Rarity",      "MenuDeck/HUD/Rarity"   },
            new[] { "Type",        "MenuDeck/Filters/Type" },
            new[] { "Energy Cost", "Battle/Tips/EnergyCost"},
        };
        for (int i = 0; i < tab.Length; i++)
            CheckEq(FilterPanelModel.TitleTerm(tab[i][0]), tab[i][1],
                    "⑤-" + (i + 1) + " 原版 `m_text` `" + tab[i][0] + "` ⇒ I2 键 `" + tab[i][1] + "`");

        // ⑤-5 认不出的名字**照原样**印（⛔ 不静默编词条）
        CheckEq(FilterPanelModel.TitleText("Not A Filter Title"), "Not A Filter Title",
                "⑤-5 名字对不上任何键 ⇒ 原样返回英文（⛔ 别编词条）");
        // ⑤-6 已知的四个名字**必须**走词条（否则英文档会印中文 / 中文档会印英文）
        Check(FilterPanelModel.TitleText("Army") != "Army",
              "⑤-6 `TitleText(\"Army\")` 必须经过词条（不再原样返回英文）");
    }

    // ════════════════════════════════════════════════════════════════════════════════════════
    // ⑥ `RuleCodes.Describe(code)` —— **不许有真码落进「未知错误码 N」**
    //    外部真值：码表 `RuleCodes` 的 1..30 是**原版拒因**（每一档的出处写在各 `const` 行上）；
    //    `Describe` 落 fallback = 提示行印出「未知错误码 23」这种话，是本工程红线里的静默失败。
    //    灭自证：把 `Names` 里任意一条删掉 ⇒ 对应行红；把 fallback 改成返回空串 ⇒ ⑥-2 红（负控也红）。
    // ════════════════════════════════════════════════════════════════════════════════════════
    static void RuleCodesNoFallback()
    {
        int[] codes =
        {
            RuleCodes.ErrBadHand, RuleCodes.ErrCost, RuleCodes.ErrSlot, RuleCodes.ErrNotTurn,
            RuleCodes.ErrNotUnit, RuleCodes.ErrExhausted, RuleCodes.ErrNoAttack, RuleCodes.ErrSelf,
            RuleCodes.ErrStunned, RuleCodes.ErrTarget, RuleCodes.ErrPindown,
            RuleCodes.ErrUnimplemented, RuleCodes.ErrNoAbility, RuleCodes.ErrDutyUsed,
            RuleCodes.ErrNoAction, RuleCodes.ErrNotWaystone, RuleCodes.ErrNoTargetAvailable,
            RuleCodes.ErrNotEnoughRoom, RuleCodes.ErrNotPlayable,
            RuleCodes.ErrNoMeleeAttack, RuleCodes.ErrNoRangedAttack, RuleCodes.ErrAttackBlock,
            RuleCodes.ErrAttackBlockStealth, RuleCodes.ErrAttackBlockFlying,
            RuleCodes.ErrCantAttackDestroyer, RuleCodes.ErrCantAttackFlank,
            RuleCodes.ErrAttackGiantKiller,
        };
        for (int i = 0; i < codes.Length; i++)
        {
            string s = RuleCodes.Describe(codes[i]);
            Check(!string.IsNullOrEmpty(s) && s.IndexOf("未知错误码", System.StringComparison.Ordinal) < 0,
                  "⑥ 码 " + codes[i] + " 有中文名（实得「" + s + "」）—— 落了 fallback 就是玩家看到「未知错误码」");
        }

        // ⑥-2 负控：**真的没有**的码必须落 fallback（否则 ⑥ 上面那批可能只是「永远返回同一句」）
        CheckEq(RuleCodes.Describe(-12345), "未知错误码 -12345",
                "⑥-2 负控：不存在的码 `-12345` 必须落 fallback（`Describe` 恒返回同一句 ⇒ 这条红）");
    }

    // ════════════════════════════════════════════════════════════════════════════════════════
    // ⑦ `ForgeData.PointsText(army, level)` —— 格式 = 原版 `"{0}/{1}"`
    //    外部真值：原版 `ForgeRewardItemContainer__Initialize.c:200-202` 的格式串（`{currentPoints}/{neededPoints}`，
    //    无空格、一个斜杠）—— 出处同 `ForgeData.cs:232` 上面那段注释。
    //    灭自证：把 `"/"` 换成 `" / "` 或加前缀 ⇒ ⑦-1 红；把分子分母调换 ⇒ ⑦-2 红（两者不再各自可辨）。
    // ════════════════════════════════════════════════════════════════════════════════════════
    static void ForgePointsFormat()
    {
        var rx = new Regex(@"^\d+/\d+$");
        string a = ForgeData.PointsText("SaimHann", 0);
        string b = ForgeData.PointsText("SaimHann", 3);
        Check(rx.IsMatch(a ?? ""), "⑦-1 `PointsText` 的格式 = 原版 `\"{0}/{1}\"`（实得「" + a + "」）");
        Check(rx.IsMatch(b ?? ""), "⑦-1b 换个等级同格式（实得「" + b + "」）");
        Check(a != b,
              "⑦-2 灭自证：不同等级的两格**文本不同**（`level` 参数被忽略 ⇒ 这条红；🆕 2026-11-01 订正："
              + "原来这里写「分子分母调换 ⇒ 红」是错的 —— 调换后格式仍然合法、等级仍不同，抓不住）");
    }

    // ════════════════════════════════════════════════════════════════════════════════════════
    // ⑧ `CardbackTable.DefaultFor(army)` —— 13 个阵营都有默认卡背
    //    外部真值：原版 `ArmyUtilities.defaultCardBacks`（`level0` 的 `ArmyUtilities` pathID 391、
    //    字段偏移 `0x20` → `{2, 420}`）—— 判据链同 `ArmyIcons.cs` 文件头 ②。
    //    ✅ 2026-11-01 订正（B113_G3）：**逐条图名已解出**（13/13 与我们表一致）⇒ 字面量由 `B113_G3_Assert` K 组钉死；本节与之**互补**（本节钉个数/唯一性）。
    //    灭自证：表里漏一个阵营 ⇒ ⑧-1 红；全部返回同一张 ⇒ ⑧-2 红。
    // ════════════════════════════════════════════════════════════════════════════════════════
    static void CardbackDefaults()
    {
        CheckEq(CardbackTable.DefaultCount, 13,
                "⑧-1 默认卡背表 13 个阵营（原版 `defaultCardBacks` 那 13 条）");
        var armies = new[]
        {
            "Ultramarines", "Goff", "SaimHann", "Sautekh", "BlackLegion", "Leviathan", "TauEmpire",
            "Sororitas", "Genestealers", "AstraMilitarum", "DarkAngels", "EmperorsChildren", "SpaceWolves",
        };
        var set = new System.Collections.Generic.HashSet<string>();
        for (int i = 0; i < armies.Length; i++)
        {
            string v = CardbackTable.DefaultFor(armies[i]);
            Check(!string.IsNullOrEmpty(v), "⑧-1b `" + armies[i] + "` 有默认卡背（实得 `" + (v ?? "null") + "`）");
            if (!string.IsNullOrEmpty(v)) set.Add(v);
        }
        CheckEq(set.Count, 13, "⑧-2 灭自证：13 个阵营的默认卡背**两两不同**（全返回同一张 ⇒ 这条红）");
    }

    // ════════════════════════════════════════════════════════════════════════════════════════
    // §未做 —— 普查里点名、但**原版真值拿不到**，故⛔ 不写（不许编字面量）
    // ════════════════════════════════════════════════════════════════════════════════════════
    // 1. `RuleCore.AlternativeActionNameFallback`（`B99A1_Assert.cs:286` 的期望值）——
    //    `RuleCore.cs:12417` 的四个值是**我们自己定的中文兜底**（注释自己写着「四个值与 2026-10-25 之前的
    //    硬编码逐字相同」）；而**官方中文列** `ferocity` = **凶残**，不是我们的「狂暴」
    //    （`RuleCore.cs` 那段 doc 明写）。⇒ 把它钉成字面量 = 钉我们自己的翻译，**不是外部真值**。
    //    真正的判据应改成「`AlternativeActionName` 在中文档 == 官方 `Loc.T` 那条」——那是 `Loc` 的事。
    // 2. `BattleDriver.FirstSeatForSeed`（`BattleScene.cs:35866`）—— 原版是**开局预烧 100 个随机数 +
    //    按下标取**（`MatchData.cs:213-228` / `:541-573`），**没有**「种子 → 先手」这个函数；
    //    我们的 `FirstSeatForSeed` 是本地派生。⇒ **原版没有对应真值**，钉它就是钉我们自己。
    // 3. `CardbackTable.DefaultFor` 的**逐条图名** —— 原版 `defaultCardBacks` SO（level0 pathID 391 → `{2,420}`）
    //    ✅ 2026-11-01 订正（B113_G3）：13 个 PPtr **已解出图名**（`工具/read_default_cardbacks.py`；`Resources/Cardbacks.json` 的 `defaults` 自 `868c4604` 2026-10-03 起就在）
    //    ⇒ 该「判据缺口」从写下之日起就不成立；字面量表 → `判据/B113_G3_存档owned与卡背图名.md` §二，已由 `B113_G3_Assert` 钉成 13 条。
    // 4. `RewardClaimFx.Ready(mat)`（`RewardsScene.cs:10859`）—— 是**我们自己的「材质齐否」判定**，
    //    原版没有对应物（原版是 `SetActive(claimRewardParticles, !IsPreview)`）。⇒ 改成直接断
    //    `Ready(...) == true` 即可，但那是**改既有断言**，不在本批白名单。
    // 5. `LevelCurve.MaxLevel`（`RewardsScene.cs:3723`）—— 等级曲线的 4 个标量是**用户 2026-10-26 裁定自拟**
    //    （见 `CLAUDE.md` 铁律 18 那张表）。⇒ 无原版真值，⛔ 不钉。
    // 6. 普查里那两条「弱」的（`B93D3_Assert.cs:246` 按秒泵算出的 `want`、`BattleScene.cs:4344` 字体规则算出的 `want`）——
    //    前者的 `want` 来自计时器格式，后者的 `want` 来自「含汉字 ⇒ CJK」闸；两处都**在该文件里另有主断言**，
    //    本文件再钉一遍属于重复，且会与那两处维护两份。⇒ **未做**，留给他们自己的宿主。
}
