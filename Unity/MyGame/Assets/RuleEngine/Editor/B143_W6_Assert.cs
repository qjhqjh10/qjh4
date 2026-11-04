// B143_W6_Assert.cs — 第一百四十三会话 · 执行写手 `B143-W6`（教程 `(tutorial)` 变体卡 · 路线①）
//
// ============================ 它盯的是什么 ============================
//  ① **A 组**：`ExpectedRows.Length` 张「教程专用变体卡」（原版 SO 名带 `(tutorial)` / `(tutortial)`）在 `Resources/tutorial_variants.json` 里的
//     **数值 / traits / 借用插图 id** 是否与**原版 SO 真值**逐条一致。期望值 = 下表**字面量**（抄自
//     `cards_truth.json`，⛔ 不拿被测代码算 —— 否则就是自证）。
//  ② **B 组**：12 副教程牌 **`ExpectedSlots.Length` 个槽**是否**逐槽解析到变体 id**（而不是被 `norm()` 抹掉括号后并成同名池卡）。
//  ③ **C 组**：变体 → `CardDef` 的查表 / 插图别名链路是否通（`TutorialVariantLookup` / `WithTutorialVariants` / `TutorialVariantArtId`）。
//  ④ **D 组**：教程督军的变体（`Uriel Ventris (tutorial)` 30 血 / `Marneus Calgar (tutorial)` / `Sylar Hexscorn (tutorial)`）。
//
// ============================ 原版判据（⛔ 别在这儿抄第二份）============================
//  · 牌组 SO 的 `cardLibraryIds` 里写的是**变体自己的 32 位 GUID**：
//    `d:/2/新解包资源/assets_full/bundle_prebuiltdecks_assets_all/MonoBehaviour/*_Deck0_Tutorial*.json`
//  · 数值真值 = `Assets/RuleEngine/Resources/cards_truth.json`（原版 SO 快照，全部 `(tutorial)`/`(tutortial)` 行）
//  · 并卡点（本笔修掉的）= `工具/gen_prebuilt_decks.py:283` 的 `norm()`（`re.sub(r"\([^)]*\)", " ", s)`）+ `:382-383` 的 t2 档
//  · 施工单 / 逐槽清单 / 三列对照 → `资料/完全复刻/报告/B143_W6_存档数据.md` §八
//
// ============================ 🔴 灭自证（改哪儿 ⇒ 哪条红）============================
//  · 把变体表删掉 / 改名 ⇒ A0 红（0 张），A 组全红。
//  · 把某张变体的数值改回池卡值（如 `Aggressor Sergeant (tutorial)` 8/4/7/6 → 5/5/5/6）⇒ 该行的 `cost/atk/rng` 红。
//  · 把 `gen_prebuilt_decks.py` 的变体旁路删掉（重新并成池卡）⇒ **B 组对应槽红**（该槽读出 `UM6` 之类而不是变体 GUID）。
//  · 把 `BattleDriver.BuildTutorialDeck` 的 `WithTutorialVariants` 拆掉 ⇒ C 组「WithTutorialVariants 解析到变体」红
//    （B 组仍绿 —— 那测的是**数据**，C 组测的是**接线**）。
//  · 把 `artId` 列删掉 / 写成空 ⇒ C 组插图别名红（盘面上会退回占位图）。
//  · 把教程督军改回池卡（Uriel `UM47` 25 血）⇒ D 组红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  宿主 = `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的聚合行**同一物理行**追加（追加的文本必须落在该行第一个 `//` 之前）：
//     `Guard("B143W6_Assert", B143_W6_Assert.Run); _pass += B143_W6_Assert.Pass; _fail += B143_W6_Assert.Fail;`
//  ⚠️ 本件**无副作用**：只读 `Resources` 两张表 + 调纯静态查表口；不建 GameObject、不碰存档、不改静态表。
using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>B143-W6 的断言（**独立类**：类名 == 文件名，唯一入口 <see cref="Run"/>）。</summary>
public static class B143_W6_Assert
{
    static int _pass, _fail;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Section("🆕 B143-W6：教程 `(tutorial)` 变体卡（" + ExpectedRows.Length + " 张）· " + ExpectedSlots.Length + " 个牌组槽 · 督军变体");
        GroupA_VariantTable();
        GroupB_DeckSlots();
        GroupC_LookupAndArt();
        GroupD_Heroes();
        Summary();
    }

    /// <summary>宿主聚合行读这两个口。</summary>
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    // ==================================================================
    //  期望值（**字面量**：抄自 `cards_truth.json` 的全部 `(tutorial)`/`(tutortial)` 行；⛔ 不拿被测代码算）
    // ==================================================================
    struct Row { public string id, name, artId, traits; public int cost, atk, rng, hp, arm; }
    struct Slot { public int stage, role, slot; public string id; }

    static readonly Row[] ExpectedRows =
    {
            new Row { id = "7ae784e51720db24985842a91f91ad6d", name = "Abaddon's Chosen (tutortial)", artId = "BL37", cost = 1, atk = 0, rng = 0, hp = 0, arm = 0, traits = "" },   // ⚠️ 原版名字拼成 `(tutortial)`
            new Row { id = "8d04a976efd628e43ad37d67e57140a0", name = "Aggressor Sergeant (tutorial)", artId = "UM19", cost = 8, atk = 4, rng = 7, hp = 6, arm = 0, traits = "" },
            new Row { id = "711ec03391d6f46a3a07de5ffb409d6b", name = "Devastator Centurion (tutorial)", artId = "UM25", cost = 8, atk = 4, rng = 7, hp = 6, arm = 0, traits = "" },
            new Row { id = "0fca8843c85ec1148b3bb77cc834a010", name = "Firstborn (tutorial)", artId = "UM3", cost = 2, atk = 2, rng = 2, hp = 2, arm = 0, traits = "" },
            new Row { id = "847acecf682cb0748bc4445b52125cb4", name = "Gauss Warrior (tutorial)", artId = "SAU4", cost = 2, atk = 2, rng = 2, hp = 2, arm = 0, traits = "remnant" },
            new Row { id = "8204f59a011f94d5c9ea1bed18ae9c57", name = "Hellblaster (tutorial)", artId = "UM10", cost = 4, atk = 2, rng = 5, hp = 4, arm = 0, traits = "blast" },
            new Row { id = "5b427f1ad98e8534f8590dad5bd560af", name = "Honour Guard (tutorial)", artId = "UM14", cost = 4, atk = 4, rng = 4, hp = 5, arm = 0, traits = "vanguard" },
            new Row { id = "319bfeba880818c4690067f2596cd6f9", name = "Infernal Gaze (tutorial)", artId = "453b0e8f88e094a17866a6979f389ef5", cost = 2, atk = 0, rng = 0, hp = 0, arm = 0, traits = "ephemeral" },
            new Row { id = "f8ba395632901b84397bd14282472c35", name = "Lieutenant Calsius (tutorial)", artId = "UM24", cost = 6, atk = 6, rng = 3, hp = 6, arm = 0, traits = "codex" },
            new Row { id = "d1a037ffd7c538c4aae209e1708aef13", name = "Marneus Calgar (tutorial)", artId = "UM46", cost = 2, atk = 2, rng = 2, hp = 30, arm = 0, traits = "" },
            new Row { id = "77fd626140a8c164fbb708a46956898d", name = "Primaris Chaplain (tutorial)", artId = "UM13", cost = 4, atk = 3, rng = 4, hp = 4, arm = 0, traits = "codex" },
            new Row { id = "d2b16a347052dc541a07c40cd9b40b78", name = "Primaris Impulsor (tutorial)", artId = "UM17", cost = 5, atk = 2, rng = 3, hp = 6, arm = 0, traits = "codex" },
            new Row { id = "eb815e1e93912d24394c7fca6d3558f4", name = "Primaris Intercessor (tutorial)", artId = "UM6", cost = 2, atk = 2, rng = 2, hp = 2, arm = 0, traits = "" },
            new Row { id = "d0ac7c259308ab540aabb5123d0c1633", name = "Primaris Reiver (tutorial)", artId = "UM5", cost = 2, atk = 3, rng = 1, hp = 2, arm = 0, traits = "" },
            new Row { id = "9be8bfe7063f14345966de388c7ba3ec", name = "Primaris Reiver (tutorial1)", artId = "UM5", cost = 2, atk = 3, rng = 1, hp = 2, arm = 0, traits = "" },
            new Row { id = "b888bfd3849c4da48973f8217e41129c", name = "Swooping Hawk (tutorial)", artId = "ASH12", cost = 3, atk = 2, rng = 3, hp = 3, arm = 0, traits = "flying,waystone" },
            new Row { id = "b5ee585c2678220478c4aed4893f4cc4", name = "Sylar Hexscorn (tutorial)", artId = "BL36", cost = 2, atk = 2, rng = 2, hp = 25, arm = 0, traits = "talent" },
            new Row { id = "ec0d25faeb266ea4eaec41ae72b8d8ad", name = "Uriel Ventris (tutorial)", artId = "UM47", cost = 2, atk = 2, rng = 2, hp = 30, arm = 0, traits = "" },
            new Row { id = "7a02359776e89f243b8cf41642dc44c9", name = "Witchblade Warlock (tutorial)", artId = "ASH19", cost = 4, atk = 4, rng = 3, hp = 5, arm = 0, traits = "talent,waystone" },
    };

    // 12 副教程牌里引用变体的槽位（条数 = `ExpectedSlots.Length`；`role` 0 = player / 1 = ai；`slot` = `cardIds` 下标）。
    static readonly Slot[] ExpectedSlots =
    {
            new Slot { stage = 1, role = 0, slot = 0, id = "eb815e1e93912d24394c7fca6d3558f4" },
            new Slot { stage = 1, role = 0, slot = 1, id = "d0ac7c259308ab540aabb5123d0c1633" },
            new Slot { stage = 1, role = 0, slot = 2, id = "9be8bfe7063f14345966de388c7ba3ec" },
            new Slot { stage = 1, role = 0, slot = 3, id = "711ec03391d6f46a3a07de5ffb409d6b" },
            new Slot { stage = 1, role = 0, slot = 4, id = "eb815e1e93912d24394c7fca6d3558f4" },
            new Slot { stage = 1, role = 0, slot = 5, id = "d0ac7c259308ab540aabb5123d0c1633" },
            new Slot { stage = 1, role = 0, slot = 6, id = "d0ac7c259308ab540aabb5123d0c1633" },
            new Slot { stage = 1, role = 0, slot = 14, id = "77fd626140a8c164fbb708a46956898d" },
            new Slot { stage = 1, role = 0, slot = 15, id = "77fd626140a8c164fbb708a46956898d" },
            new Slot { stage = 1, role = 0, slot = 16, id = "77fd626140a8c164fbb708a46956898d" },
            new Slot { stage = 1, role = 0, slot = 17, id = "77fd626140a8c164fbb708a46956898d" },
            new Slot { stage = 1, role = 0, slot = 18, id = "77fd626140a8c164fbb708a46956898d" },
            new Slot { stage = 1, role = 0, slot = 19, id = "77fd626140a8c164fbb708a46956898d" },
            new Slot { stage = 1, role = 1, slot = 0, id = "d0ac7c259308ab540aabb5123d0c1633" },
            new Slot { stage = 1, role = 1, slot = 1, id = "d0ac7c259308ab540aabb5123d0c1633" },
            new Slot { stage = 1, role = 1, slot = 2, id = "eb815e1e93912d24394c7fca6d3558f4" },
            new Slot { stage = 1, role = 1, slot = 3, id = "eb815e1e93912d24394c7fca6d3558f4" },
            new Slot { stage = 1, role = 1, slot = 4, id = "eb815e1e93912d24394c7fca6d3558f4" },
            new Slot { stage = 2, role = 1, slot = 0, id = "0fca8843c85ec1148b3bb77cc834a010" },
            new Slot { stage = 2, role = 1, slot = 1, id = "eb815e1e93912d24394c7fca6d3558f4" },
            new Slot { stage = 2, role = 1, slot = 5, id = "eb815e1e93912d24394c7fca6d3558f4" },
            new Slot { stage = 2, role = 1, slot = 6, id = "0fca8843c85ec1148b3bb77cc834a010" },
            new Slot { stage = 2, role = 1, slot = 7, id = "eb815e1e93912d24394c7fca6d3558f4" },
            new Slot { stage = 2, role = 1, slot = 8, id = "0fca8843c85ec1148b3bb77cc834a010" },
            new Slot { stage = 3, role = 0, slot = 11, id = "847acecf682cb0748bc4445b52125cb4" },
            new Slot { stage = 3, role = 0, slot = 14, id = "847acecf682cb0748bc4445b52125cb4" },
            new Slot { stage = 3, role = 1, slot = 0, id = "847acecf682cb0748bc4445b52125cb4" },
            new Slot { stage = 3, role = 1, slot = 15, id = "847acecf682cb0748bc4445b52125cb4" },
            new Slot { stage = 4, role = 0, slot = 4, id = "319bfeba880818c4690067f2596cd6f9" },
            new Slot { stage = 4, role = 0, slot = 6, id = "7ae784e51720db24985842a91f91ad6d" },
            new Slot { stage = 4, role = 0, slot = 7, id = "319bfeba880818c4690067f2596cd6f9" },
            new Slot { stage = 4, role = 1, slot = 2, id = "f8ba395632901b84397bd14282472c35" },
            new Slot { stage = 4, role = 1, slot = 4, id = "d2b16a347052dc541a07c40cd9b40b78" },
            new Slot { stage = 4, role = 1, slot = 5, id = "5b427f1ad98e8534f8590dad5bd560af" },
            new Slot { stage = 4, role = 1, slot = 13, id = "8204f59a011f94d5c9ea1bed18ae9c57" },
            new Slot { stage = 4, role = 1, slot = 19, id = "f8ba395632901b84397bd14282472c35" },
            new Slot { stage = 4, role = 1, slot = 21, id = "d2b16a347052dc541a07c40cd9b40b78" },
            new Slot { stage = 5, role = 0, slot = 3, id = "7a02359776e89f243b8cf41642dc44c9" },
            new Slot { stage = 5, role = 0, slot = 4, id = "b888bfd3849c4da48973f8217e41129c" },
            new Slot { stage = 5, role = 0, slot = 18, id = "7a02359776e89f243b8cf41642dc44c9" },
            new Slot { stage = 6, role = 1, slot = 8, id = "d2b16a347052dc541a07c40cd9b40b78" },
    };

    // ==================================================================
    //  A 组 · `ExpectedRows.Length` 张变体的数值 / traits / 借用插图
    // ==================================================================
    static void GroupA_VariantTable()
    {
        var list = CardDatabase.LoadTutorialVariants();
        Check(list.Count, ExpectedRows.Length,
              "A0 ★ `Resources/tutorial_variants.json` 必须解析出 **" + ExpectedRows.Length + " 张**（0 张 = 表没生成/读不到）"
            + "｜🧨 删表/改名 ⇒ 本条红");

        var byId = new Dictionary<string, CardDef>(StringComparer.Ordinal);
        foreach (var c in list) if (c != null && !string.IsNullOrEmpty(c.Id)) byId[c.Id] = c;

        foreach (var e in ExpectedRows)
        {
            CardDef c;
            if (!byId.TryGetValue(e.id, out c))
            {
                CheckTrue(false, "A1 找不到变体 `" + e.name + "`（id " + e.id + "）｜🧨 表里缺这张 ⇒ 本条红");
                continue;
            }
            string tag = "A1 `" + e.name + "`";
            Check(c.Name, e.name, tag + " 卡名");
            Check(c.Cost, e.cost, tag + " 费用");
            Check(c.Attack, e.atk, tag + " 近战");
            Check(c.RangedAttack, e.rng, tag + " 远程");
            Check(c.Health, e.hp, tag + " 生命");
            Check(c.ArmourFromPool, e.arm, tag + " 护甲（列值 = `ArmourFromPool`）");
            Check(TraitSet(c), e.traits, tag + " traits（小写、去数字、逗号序）");
            Check(CardDatabase.TutorialVariantArtId(e.id), e.artId, tag + " 借用插图 id");
        }
        GroupA_Texts(byId);
    }

    // ==================================================================
    //  A2 · 中文名 / 卡面描述（2026-11-05 复核意见 A/B 的落地断言）
    //     · 中文名 = SO `refNameId` 指的**那张池卡**的 `nameZh`（原版复用同一份词条）；
    //     · 卡面描述**只**在 SO `cardDescriptionTextReferenceId` 指向某张池卡时才有；没有该引用
    //       ⇒ 原版取值链退回自己 uniqueId ⇒ 官方表查不到 ⇒ **原版本来就是空的**（⛔ 不许从池卡抄）。
    //  🧨 把 `nameZh` 抹掉 ⇒ A2 红；把**期望列里标 `1`** 的那些卡抄上池卡文案 ⇒ 该行 A2 红。
    // ==================================================================
    static readonly string[,] ExpectedTexts =
    {
        { "7ae784e51720db24985842a91f91ad6d", "阿巴顿的选民",       "0" },   // Abaddon's Chosen (tutortial)
        { "8d04a976efd628e43ad37d67e57140a0", "侵略者士官",        "1" },   // Aggressor Sergeant (tutorial)
        { "711ec03391d6f46a3a07de5ffb409d6b", "破坏者百夫长",      "1" },   // Devastator Centurion (tutorial)
        { "0fca8843c85ec1148b3bb77cc834a010", "长子",             "1" },   // Firstborn (tutorial)
        { "847acecf682cb0748bc4445b52125cb4", "高斯战士",          "0" },   // Gauss Warrior (tutorial)
        { "8204f59a011f94d5c9ea1bed18ae9c57", "地狱轰击者",        "0" },   // Hellblaster (tutorial)
        { "5b427f1ad98e8534f8590dad5bd560af", "荣誉卫队",          "0" },   // Honour Guard (tutorial)
        { "319bfeba880818c4690067f2596cd6f9", "地狱凝视",          "0" },   // Infernal Gaze (tutorial)
        { "f8ba395632901b84397bd14282472c35", "副官卡尔修斯",       "0" },   // Lieutenant Calsius (tutorial)
        { "d1a037ffd7c538c4aae209e1708aef13", "马涅乌思·卡尔加",     "1" },   // Marneus Calgar (tutorial)
        { "77fd626140a8c164fbb708a46956898d", "原铸牧师",          "0" },   // Primaris Chaplain (tutorial)
        { "d2b16a347052dc541a07c40cd9b40b78", "原铸冲击者突击艇",    "0" },   // Primaris Impulsor (tutorial)
        { "eb815e1e93912d24394c7fca6d3558f4", "原铸仲裁者",        "1" },   // Primaris Intercessor (tutorial)
        { "d0ac7c259308ab540aabb5123d0c1633", "原铸劫掠者",        "1" },   // Primaris Reiver (tutorial)
        { "9be8bfe7063f14345966de388c7ba3ec", "原铸劫掠者",        "1" },   // Primaris Reiver (tutorial1)
        { "b888bfd3849c4da48973f8217e41129c", "旋鹰",             "0" },   // Swooping Hawk (tutorial)
        { "b5ee585c2678220478c4aed4893f4cc4", "疯狂之法",          "0" },   // Sylar Hexscorn (tutorial)
        { "ec0d25faeb266ea4eaec41ae72b8d8ad", "乌列尔·文特里斯",    "1" },   // Uriel Ventris (tutorial)
        { "7a02359776e89f243b8cf41642dc44c9", "巫术之刃战巫",       "1" },   // Witchblade Warlock (tutorial)
    };

    static void GroupA_Texts(Dictionary<string, CardDef> byId)
    {
        for (int i = 0; i < ExpectedTexts.GetLength(0); i++)
        {
            string id = ExpectedTexts[i, 0], zh = ExpectedTexts[i, 1];
            bool descShouldBeEmpty = ExpectedTexts[i, 2] == "1";
            CardDef c;
            if (!byId.TryGetValue(id, out c)) continue;          // A1 已经报过「找不到」
            Check(c.NameZh, zh, "A2 `" + c.Name + "` 中文名（= SO `refNameId` 那张池卡的 nameZh）");
            Check(string.IsNullOrWhiteSpace(c.Desc), descShouldBeEmpty,
                  "A2 `" + c.Name + "` 卡面描述应为" + (descShouldBeEmpty ? "**空**（SO 没有描述引用 ⇒ 原版本来就是空的）" : "非空（SO 描述引用指向某张池卡）"));
        }
    }

    /// <summary>`CardDef.Keywords` 的键集合（小写、去掉非字母、排序、逗号连）—— 与期望列的写法同口径。</summary>
    static string TraitSet(CardDef c)
    {
        if (c == null || c.Keywords == null) return "";
        var keys = new List<string>();
        foreach (var kv in c.Keywords)
        {
            var s = (kv.Key ?? "").ToLowerInvariant();
            var sb = new System.Text.StringBuilder();
            foreach (char ch in s) if (ch >= 'a' && ch <= 'z') sb.Append(ch);
            if (sb.Length > 0) keys.Add(sb.ToString());
        }
        keys.Sort(StringComparer.Ordinal);
        return string.Join(",", keys.ToArray());
    }

    // ==================================================================
    //  B 组 · 40 个牌组槽必须解析到变体（不是同名池卡）
    // ==================================================================
    static void GroupB_DeckSlots()
    {
        var asset = Resources.Load<TextAsset>("tutorial_decks");
        if (asset == null)
        {
            CheckTrue(false, "B0 ★ 读不到 `Resources/tutorial_decks.json`｜🧨 产物没生成 ⇒ 本条红");
            return;
        }
        var doc = JsonUtility.FromJson<DecksFileDto>(asset.text);
        if (doc == null || doc.stages == null)
        {
            CheckTrue(false, "B0 ★ `tutorial_decks.json` 解析失败（" + asset.text.Length + " 字符）");
            return;
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var e in ExpectedRows) ids.Add(e.id);

        // B1：全表里「属于变体表的 id」出现次数必须恰好 = 40（⛔ 少于 40 = 有槽被并成池卡了）
        int total = 0;
        foreach (var st in doc.stages)
        {
            if (st == null) continue;
            total += CountVariantIds(st.player, ids);
            total += CountVariantIds(st.ai, ids);
        }
        Check(total, ExpectedSlots.Length,
              "B1 ★ 12 副教程牌里属于变体表的槽位**总数**（少于 " + ExpectedSlots.Length + " = 有槽被并成同名池卡）"
            + "｜🧨 删掉 gen_prebuilt_decks 的变体旁路 ⇒ 本条红");

        // B2：逐槽核对（定位到具体是哪一槽掉回池卡）
        foreach (var s in ExpectedSlots)
        {
            var side = SideOf(doc, s.stage, s.role);
            string got = side != null && side.cardIds != null && s.slot < side.cardIds.Length ? side.cardIds[s.slot] : "<越界>";
            Check(got, s.id, "B2 S" + s.stage + " " + (s.role == 0 ? "player" : "ai") + " 槽 " + s.slot
                             + "｜🧨 被并成池卡 ⇒ 本条红");
        }
    }

    static int CountVariantIds(SideDto side, HashSet<string> ids)
    {
        int n = 0;
        if (side == null || side.cardIds == null) return 0;
        foreach (var c in side.cardIds) if (c != null && ids.Contains(c)) n++;
        return n;
    }

    static SideDto SideOf(DecksFileDto doc, int stage, int role)
    {
        foreach (var st in doc.stages)
        {
            if (st == null || st.stage != stage) continue;
            return role == 0 ? st.player : st.ai;
        }
        return null;
    }

    // ==================================================================
    //  C 组 · 查表链路（变体 → CardDef）+ 插图别名
    // ==================================================================
    static void GroupC_LookupAndArt()
    {
        var lookup = CardDatabase.TutorialVariantLookup();
        var combined = CardDatabase.WithTutorialVariants(CardDatabase.DeckLookup(new List<CardDef>(), "Ultramarines"));
        foreach (var e in ExpectedRows)
        {
            var a = lookup(e.id);
            var b = combined(e.id);
            CheckTrue(a != null && a.Name == e.name,
                      "C1 `TutorialVariantLookup('" + e.name + "')` 必须按 id 命中变体（拿到的名字 = "
                    + (a == null ? "<null>" : a.Name) + "）");
            CheckTrue(b != null && b.Id == e.id,
                      "C2 `WithTutorialVariants(...)` 必须先查变体表（'" + e.name + "' ⇒ "
                    + (b == null ? "<null>" : b.Id) + "）");
        }
        CheckTrue(lookup("UM19") == null, "C3 变体查表**只按变体 id** —— 池卡 id `UM19` 必须查不到（⛔ 别让池卡从这条口进来）");
        CheckTrue(CardDatabase.TutorialVariantArtId("UM19") == null, "C4 池卡 id 不该有「借用插图」记录（只变体才有）");
    }

    // ==================================================================
    //  D 组 · 教程督军（变体）
    // ==================================================================
    static void GroupD_Heroes()
    {
        var asset = Resources.Load<TextAsset>("tutorial_decks");
        if (asset == null) return;
        var doc = JsonUtility.FromJson<DecksFileDto>(asset.text);
        if (doc == null || doc.stages == null) return;

        // 判据：原版 SO 的 `deckHero.targetId`（`d:/2/新解包资源/…/*_Deck0_Tutorial*.json`）——
        //   S1 玩家 `Uriel Ventris (tutorial)`（30 血）· S1 AI `Marneus Calgar (tutorial)` · S4 玩家 `Sylar Hexscorn (tutorial)`。
        Check(SideOf(doc, 1, 0) == null ? null : SideOf(doc, 1, 0).heroId, "ec0d25faeb266ea4eaec41ae72b8d8ad",
              "D1 S1 玩家督军 = `Uriel Ventris (tutorial)`（血 30；池卡 `UM47` 只有 25）｜🧨 退回池卡 ⇒ 本条红");
        Check(SideOf(doc, 1, 1) == null ? null : SideOf(doc, 1, 1).heroId, "d1a037ffd7c538c4aae209e1708aef13",
              "D2 S1 AI 督军 = `Marneus Calgar (tutorial)`");
        Check(SideOf(doc, 4, 0) == null ? null : SideOf(doc, 4, 0).heroId, "b5ee585c2678220478c4aed4893f4cc4",
              "D3 S4 玩家督军 = `Sylar Hexscorn (tutorial)`");
        var u = CardDatabase.TutorialVariantLookup()("ec0d25faeb266ea4eaec41ae72b8d8ad");
        Check(u == null ? -1 : u.Health, 30, "D4 `Uriel Ventris (tutorial)` 的血 = 30（原版 SO；池卡 25）");
    }

    // ==================================================================
    //  `tutorial_decks.json` 的最小 DTO（`JsonUtility` 只认字段名）
    // ==================================================================
    [Serializable] class DecksFileDto { public StageDto[] stages; }
    [Serializable] class StageDto { public int stage; public SideDto player; public SideDto ai; }
    [Serializable] class SideDto { public string heroId; public string[] cardIds; }

    // ==================================================================
    static void Section(string title) { Debug.Log("--- " + title + " ---"); }

    static void Check<T>(T actual, T expected, string msg)
    {
        bool ok = EqualityComparer<T>.Default.Equals(actual, expected);
        Report(ok, msg + "（实际 = " + actual + "，期望 = " + expected + "）");
    }

    static void CheckTrue(bool cond, string msg) { Report(cond, msg); }

    static void Report(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("[B143W6] ✓ " + msg); }
        else { _fail++; Debug.LogError("[B143W6] ✗ " + msg); }
    }

    static void Summary()
    {
        if (_fail == 0) Debug.Log($"B143_W6 合计：{_pass} 通过 / 0 失败");
        else Debug.LogError($"B143_W6 合计：{_pass} 通过 / {_fail} 失败");
    }
}
