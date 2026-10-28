// RuleEngineTest_W6.cs — 写手 `W6`（2026-10-28）：`其他_基础设施` 那 17 条「实现已落、断言未加」的断言
//   ＋ `LoadableReference` 三条重复键（`OnBeforeRead` / `OnBeforeWrite` / `WriteJson`）＋ 新加的 `==` / `!=`。
//
// ============================ 这批账从哪来 ============================
// `资料/完全复刻/报告/B91_L3.md` §3·1「只差一处可观察量 / 一条断言」那一组（10 条）
// ＋ 同报告说的 7 条 `VarsGlobal`（落点已修到 `Core/LevelCurve.cs`、状态仍 B）＝ **17 条**：
//   · 385 `GetClosedDeckByCards` · 386 `GetClosedDeckById` · 387 `GetClosedDeckByPackIds` ·
//     389 `GetDeckIdWithPacks` · 405 `GetPackById` · 422 `IsEventDeckFromWarlord`
//   · 506 `ShouldSellCardsFromCrate` · 483 `SellSpareCards` · 484 `SellSpareCardsRecursive` ·
//     546 `<SellSpareCardsRecursive>b__602_0`（完成回调）
//   · 762 `CheckIfPlayerChangedDivision` · 763 `ConvertTierToCheckPrize` · 764 `GetHeroLevel` ·
//     765 `GetNextLevelXP` · 766 `GetPlayerDivision` · 768 `GetXPInLevel` · 769 `GetXPMissingToNextLevel`
// 实现落点（本批**现读**，不是抄报告的旧行号）：
//   · `CardPresentation/Shell/PrebuiltDecks.cs:838/854/873/885/893/913`
//   · `CardPresentation/Shell/CardDetailPopup.cs:3391/3400/3429/3456`（`CardSell`）
//   · `CardPresentation/Core/LevelCurve.cs:61/64/82/101/110/135/144`
//
// ============================ 期望值从哪来（⛔ 不是从我们实现里读回来）============================
//   ① `LevelCurve` 七条：`decomp_full/VarsGlobal__{GetNextLevelXP,GetHeroLevel,GetXPInLevel,
//      GetXPMissingToNextLevel,GetPlayerDivision,ConvertTierToCheckPrize,CheckIfPlayerChangedDivision}.c`
//      —— 逐句读过，期望值在下面**手算成字面量**（等差阈值 / 从高到低扫 / 只算升段）。
//   ② 预组六条：`out_cpp2il_dev_ilspy` + `decomp_full` 的 `PlayerDataManager__GetClosedDeck*` /
//      `GetDeckIdWithPacks` / `GetPackById` / `IsEventDeckFromWarlord` 体 —— 语义是
//      `Where(...).FirstOrDefault()` / `Remove` 逐张 / `packId + ";"` 拼接（**顺序**与**静默跳过查不到的包**
//      都是原版的行为，下面按「顺序」「跳过」断，⛔ 不断「随便命中一副就行」）。
//   ③ 卖卡四条：`decomp_full/PlayerDataManager__{ShouldSellCardsFromCrate,SellSpareCards,
//      _SellSpareCardsRecursive_d__602__MoveNext,__SellSpareCardsRecursive_b__602_0}.c`
//      —— 判据要点 = `GetOwnedCount(crystals) < **1601**`（`0x641`）· `fromInit` 的一次性闸 ·
//      「卖 `min(多余副本, 表中要卖数)`」· 完成回调三件事（清闸 / 清待卖数 / 记时间戳）。
//
// ============================ 写法红线（本项目一天撞过 3 次）============================
//   ① **先写前提断言（硬检查），再写被测断言** —— ⛔ 没有 `if (x != null) Check(...)` 那种「前提不成立就静默跳过」。
//      唯一的两处 `if` 是「前提已经**计了一条 ✗**」之后的早退（同 `Editor/LoadableRefCheck.cs:78` 的既有做法）。
//   ② 断的是**结构**（顺序 / 哪一副 / 扣了几张 / 时间戳是真实墙钟），⛔ 不是「我们自己的常量」。
//   ③ **⌈灭自证⌉**：凡「实现可能连检测口一起改回去」的地方，都补了一条结构上不可能同时满足的断言
//      （见各条 `🧨` 里写明的那一条）。
//   ④ 每条断言带 `🧨`（怎么改会红）。
//
// ⚠️ **本组必须留在 `Run()` 的末尾**：`W6CardSell` 会 `CardProgress.ResetForTest()`（关掉落盘）、
//    `W6ShouldSellCardsFromCrate` 会把 `InventoryManager` 的解包闸置成「已解包」并登记一个 `Inventory<ICurrency>`
//    —— 都是进程级状态，跑在别的用例**之后**才不干扰它们。
using System;
using System.Collections.Generic;
using CardPresentation;

public static partial class RuleEngineTest
{
    // ==============================================================================================
    //  ① 预组卡组六条（385 / 386 / 387 / 389 / 405 / 422）
    // ==============================================================================================

    /// <summary>预组那六条：`GetClosedDeckById` / `GetClosedDeckByCards` / `GetClosedDeckByPackIds` /
    /// `GetDeckIdWithPacks` / `GetPackById` / `IsEventDeckFromWarlord`。
    /// <para>夹具**全部走 `SetEventDecksForTest` / `SetPacksForTest` 的注入口**（⛔ 不读 `Resources` ——
    /// 那会让断言依赖「数据文件在不在」，而本条要验的是**查询语义**）。收尾把两份缓存都还原。</para></summary>
    public static void TestW6PrebuiltDecks()
    {
        Section("🆕 W6：预组/活动卡组的六个查询口（386 by-id 首个命中 · 385 逐张 Remove · 422 督军比对 · 405/389/387 包→牌）");
        W6ClosedDeckById();
        W6ClosedDeckByCards();
        W6IsEventDeckFromWarlord();
        W6PackQueries();
    }

    static PrebuiltDecks.Deck W6Deck(string deckId, string heroId, params string[] cards)
    {
        return new PrebuiltDecks.Deck { deckId = deckId, heroId = heroId, cardIds = cards };
    }

    static void W6ClosedDeckById()
    {
        // 386 —— 原版 `fullEventAvailableDecks.Where(x => x != null && x.deckId == deckId).FirstOrDefault()`
        //   ⇒ ① 命中第一副（**顺序**，不是「随便一副」）；② 查不到 ⇒ null。
        PrebuiltDecks.SetEventDecksForTest(new List<PrebuiltDecks.Deck>
        {
            W6Deck("deck_A", "hero_X", "c1"),
            W6Deck("deck_A", "hero_Y", "c2"),   // 同一个 deckId 的第二副（原版 FirstOrDefault 只会拿第一副）
            W6Deck("deck_B", "hero_Z", "c3"),
        });
        CheckTrue(PrebuiltDecks.EventDecks.Count == 3, "（前提）注入的 3 副活动卡组都在（`SetEventDecksForTest` 没丢条目）");

        var got = PrebuiltDecks.GetClosedDeckById("deck_A");
        CheckTrue(got != null, "（前提）`GetClosedDeckById(\"deck_A\")` 取得到一副");
        Check(got != null ? got.heroId : "<null>", "hero_X",
              "386 `GetClosedDeckById`：同 deckId 两副时取**第一副**（原版 `FirstOrDefault` 的顺序语义）"
            + "｜🧨 改成「取最后一副」/「按 heroId 排完再取」⇒ 实得 hero_Y");
        CheckTrue(PrebuiltDecks.GetClosedDeckById("deck_nope") == null,
              "386 `GetClosedDeckById`：查不到 ⇒ null（原版 `FirstOrDefault` 同一档）｜🧨 改成「返回最后一副」⇒ 红");
        CheckTrue(PrebuiltDecks.GetClosedDeckById(null) == null,
              "386 `GetClosedDeckById`：null id ⇒ null（不能抛出，也不能命中「deckId 为 null 的那些」）｜🧨 去掉入参闸 ⇒ 可能 NRE");

        PrebuiltDecks.SetEventDecksForTest(null);   // 清空（收尾）
    }

    static void W6ClosedDeckByCards()
    {
        // 385 —— 原版 `GetClosedDeckByCards(warlord, cards)`：先按**督军**筛，再拷一份 `cardLibraryIds`，
        //   逐张 `Remove(card.uniqueId)`，**一张不剩**才是答案。
        //   ⇒ 三条语义：① 多一张不算（不是「包含」）；② 少一张也不算；③ 逐张 `Remove` 只吃掉**一个**重复项。
        PrebuiltDecks.SetEventDecksForTest(new List<PrebuiltDecks.Deck>
        {
            W6Deck("deck_full", "hero_F", "c1", "c2", "c3"),
            W6Deck("deck_dup",  "hero_D", "d1", "d1", "d2"),   // 同一个 id 出现两次（原版包内自重是合法的）
        });
        CheckTrue(PrebuiltDecks.EventDecks.Count == 2, "（前提）注入的 2 副活动卡组都在");

        var hit = PrebuiltDecks.GetClosedDeckByCards("hero_F", new[] { "c1", "c2", "c3" });
        Check(hit != null ? hit.deckId : "<null>", "deck_full",
              "385 `GetClosedDeckByCards`：牌**恰好**用光 ⇒ 命中那一副｜🧨 改成「包含即命中」⇒ 下面两条会一起红（这条本身还绿，靠下面两条兜）");
        CheckTrue(PrebuiltDecks.GetClosedDeckByCards("hero_F", new[] { "c1", "c2" }) == null,
              "385 `GetClosedDeckByCards`：牌**没用光**（还差 c3）⇒ 不命中（原版要求剩 0 张）｜🧨 改成 `Contains`/`All` 语义 ⇒ 实得 deck_full");
        // 🔴 边界（原版的真实语义，⛔ 别按「集合相等」去理解）：多给一张**副里没有**的牌 ⇒ 那张 `Remove` 是空操作、
        //   剩下的仍然 0 张 ⇒ **照样命中**。原版是「副 ⊆ 待查表」而不是「相等」。
        var superset = PrebuiltDecks.GetClosedDeckByCards("hero_F", new[] { "c1", "c2", "c3", "c4" });
        Check(superset != null ? superset.deckId : "<null>", "deck_full",
              "385 `GetClosedDeckByCards`：多给一张副里没有的牌 ⇒ **仍命中**（原版逐张 `Remove`，多余的 `Remove` 是空操作）"
            + "｜🧨 改成「先比张数 / 要求集合相等」⇒ 本条实得 <null>（这会把原版语义判错，是**过度收紧**）");
        CheckTrue(PrebuiltDecks.GetClosedDeckByCards("hero_OTHER", new[] { "c1", "c2", "c3" }) == null,
              "385 `GetClosedDeckByCards`：**督军不同** ⇒ 不命中（原版先按 `deckHero` 筛）｜🧨 去掉督军比较 ⇒ 实得 deck_full");

        // 重复项：副里有 d1 两次。给 {d1,d2} ⇒ 只吃掉一个 d1、还剩一个 ⇒ 不命中；给 {d1,d1,d2} ⇒ 命中。
        //   ⛔ 这一条挡的是「把待查表去重后再比」（那会让 {d1,d2} 也命中）。
        CheckTrue(PrebuiltDecks.GetClosedDeckByCards("hero_D", new[] { "d1", "d2" }) == null,
              "385 `GetClosedDeckByCards`：副里有**两张 d1**、只给一张 ⇒ 仍不命中（逐张 `Remove` 只吃一个）"
            + "｜🧨 把 `cardIds` 或待查表先去重 ⇒ 这条变绿、上面那条变红");
        var dupHit = PrebuiltDecks.GetClosedDeckByCards("hero_D", new[] { "d1", "d1", "d2" });
        Check(dupHit != null ? dupHit.deckId : "<null>", "deck_dup",
              "385 `GetClosedDeckByCards`：给两张 d1 ⇒ 命中（原版允许自重）｜🧨 改成 `Distinct()` ⇒ 实得 <null>");

        PrebuiltDecks.SetEventDecksForTest(null);
    }

    static void W6IsEventDeckFromWarlord()
    {
        // 422 —— 原版：按 deckId 取 → 有且督军相同 ⇒ true；取不到 / 没有督军 ⇒
        //   `CustomDebug.LogError("Can't find deckFaction")` + false（**照抄那句日志**，所以下面两条 false
        //   会在日志里各打一条 LogError —— 是预期的，⛔ 不是失败）。
        PrebuiltDecks.SetEventDecksForTest(new List<PrebuiltDecks.Deck>
        {
            W6Deck("deck_ok", "hero_K", "c1"),
            W6Deck("deck_nohero", "", "c2"),
        });
        CheckTrue(PrebuiltDecks.EventDecks.Count == 2, "（前提）注入的 2 副活动卡组都在");

        CheckTrue(PrebuiltDecks.IsEventDeckFromWarlord("hero_K", "deck_ok"),
              "422 `IsEventDeckFromWarlord`：督军相同 ⇒ true｜🧨 把比较方向/字段改错 ⇒ 红");
        CheckTrue(!PrebuiltDecks.IsEventDeckFromWarlord("hero_OTHER", "deck_ok"),
              "422 `IsEventDeckFromWarlord`：副找到了但督军不同 ⇒ false（⚠️ 会打一条 LogError，预期的）｜🧨 只判「副在不在」⇒ 红");
        CheckTrue(!PrebuiltDecks.IsEventDeckFromWarlord("hero_K", "deck_missing"),
              "422 `IsEventDeckFromWarlord`：副取不到 ⇒ false + LogError（⚠️ 预期）｜🧨 改成「取不到就 true」⇒ 红");
        CheckTrue(!PrebuiltDecks.IsEventDeckFromWarlord("", "deck_nohero"),
              "422 `IsEventDeckFromWarlord`：副的督军为空 ⇒ false（原版 `!string.IsNullOrEmpty(deckHero)` 那一支）"
            + "｜🧨 去掉「督军非空」判 ⇒ `\"\" == \"\"` 会让它变 true，本条红");

        PrebuiltDecks.SetEventDecksForTest(null);
    }

    static void W6PackQueries()
    {
        // 405 / 389 / 387 —— 三个口都转调 `DraftPacks`（同一份包表）。
        //   夹具 = 3 个包（`key` 与 `packId` **故意不同**，用来分出「先比 key、再退回 packId」那一跳）。
        DraftPacks.SetPacksForTest(new List<DraftPacks.PackDto>
        {
            new DraftPacks.PackDto { key = "k1", packId = "p1", cardIds = new[] { "a1", "a2", "a3" } },
            new DraftPacks.PackDto { key = "k2", packId = "p2", cardIds = new[] { "b1", "b2", "b3" } },
        });
        Check(PrebuiltDecks.GetPackById("k1") != null ? "ok" : "miss", "ok",
              "（前提）包夹具装起来了（`SetPacksForTest` 之后 `GetPackById(\"k1\")` 取得到）");

        // 405 —— 原版 `PrebuiltPacks.FirstOrDefault(x => x.packId == packId)`。
        var byKey = PrebuiltDecks.GetPackById("k1");
        Check(byKey != null ? byKey.packId : "<null>", "p1",
              "405 `GetPackById`：按 `key` 取到的那一份，`packId` 是 p1｜🧨 只按 `packId` 查 ⇒ 实得 <null>");
        var byPackId = PrebuiltDecks.GetPackById("p2");
        Check(byPackId != null ? byPackId.key : "<null>", "k2",
              "405 `GetPackById`：裸 `packId` 也要认（原版那一列就是 `packId`）｜🧨 只比 `key` ⇒ 实得 <null>");
        CheckTrue(PrebuiltDecks.GetPackById("nope") == null,
              "405 `GetPackById`：认不出 ⇒ null（原版 `FirstOrDefault` 同一档）｜🧨 改成「返回第一包」⇒ 红");

        // 389 —— 原版逐个 `GetPackById`，取到的追加 **`pack.packId` + `';'`**；查不到的**静默跳过**；入参 null ⇒ 空串。
        Check(PrebuiltDecks.GetDeckIdWithPacks(new List<string> { "k1", "nope", "k2" }), "p1;p2;",
              "389 `GetDeckIdWithPacks`：拼的是**包的 `packId`** + ';'（不是 key），按入参顺序、认不出的静默跳过"
            + "｜🧨 拼 `key` ⇒ 实得 `k1;k2;`；🧨 中途 return ⇒ 实得 `p1;`");
        Check(PrebuiltDecks.GetDeckIdWithPacks(null), "",
              "389 `GetDeckIdWithPacks`：入参 null ⇒ 空串（原版同一支）｜🧨 直接解引用 ⇒ NRE / 抛异常");
        Check(PrebuiltDecks.GetDeckIdWithPacks(new List<string>()), "",
              "389 `GetDeckIdWithPacks`：空表 ⇒ 空串｜🧨 无条件补一个 ';' ⇒ 实得 \";\"");

        // 387 —— 原版：督军非空 ⇒ 每个 packId 取包 ⇒ 摊平成一副（包内按 `cardIds` 原序、包间按入参顺序）；
        //   `packIds == null` ⇒ 出声 + 空牌组；**督军为空** ⇒ LogError + null。
        var cd = PrebuiltDecks.GetClosedDeckByPackIds("hero_W", new List<string> { "k1", "k2" });
        CheckTrue(cd != null, "（前提）`GetClosedDeckByPackIds` 用两个合法包组出了牌组");
        Check(cd != null ? string.Join(",", cd.cardIds ?? new string[0]) : "<null>", "a1,a2,a3,b1,b2,b3",
              "387 `GetClosedDeckByPackIds`：摊平后的卡 = 包1 三张 + 包2 三张（**按选包顺序、包内原序**）"
            + "｜🧨 按 key 排序 / 报错时截断 / 去重 ⇒ 实得另一串");
        Check(cd != null ? string.Join(",", cd.packIds ?? new string[0]) : "<null>", "p1,p2",
              "387 `GetClosedDeckByPackIds`：`packIds` 回写的也是 `packId`、顺序同入参｜🧨 写 key ⇒ 实得 `k1,k2`");

        var empty = PrebuiltDecks.GetClosedDeckByPackIds("hero_W", null);
        CheckTrue(empty != null && empty.cardIds != null && empty.cardIds.Length == 0,
              "387 `GetClosedDeckByPackIds`：`packIds == null` ⇒ **非 null 的空牌组**（原版 `new ClosedDeck()` 那一支）"
            + "｜🧨 改成返回 null ⇒ 调用方再也分不清「没给包」和「组不出来」");
        CheckTrue(PrebuiltDecks.GetClosedDeckByPackIds("", new List<string> { "k1" }) == null,
              "387 `GetClosedDeckByPackIds`：**督军为空** ⇒ null + LogError（原版 `\"GetClosedDeckByPackIds hero null\"` 那一支；⚠️ 预期日志）｜🧨 不判督军 ⇒ 照样组出牌组，本条红");

        DraftPacks.InvalidateCache();   // 收尾：丢掉夹具缓存（下次 `All` 会回落到真数据）
    }

    // ==============================================================================================
    //  ② 卖多余副本四条（506 / 483 / 484 / 546）
    // ==============================================================================================

    /// <summary>卖卡那四条。夹具 id 用**卡池外的假 id**（`RarityOf` 查不到 ⇒ 落 `common` 档），
    /// 拥有数一律**先读再算差**（⛔ 不 hardcode 卡组上限 —— 那条上限属于别的口，本条只验「扣了几张 / 记了什么」）。</summary>
    public static void TestW6CardSell()
    {
        Section("🆕 W6：卖多余副本（506 水晶阈值 1601 · 483 fromInit 一次性闸 · 484 min(多余,要卖) · 546 完成回调时间戳）");
        W6ShouldSellCardsFromCrate();
        W6SellSpareCardsGates();
        W6SellSpareCardsRecursiveDrain();
        W6SellCompletedCallback();
    }

    const string W6SpareId = "W6_SPARE_TEST";   // 卡池里没有这个 id ⇒ `RarityOf` 落 `common`

    /// <summary>506：`GetOwnedCount(crystals) &lt; 1601` —— 判据 = `PlayerDataManager__ShouldSellCardsFromCrate.c`
    /// （`GetOwnedCount(_, 10)`，`10` = `GameCurrency.crystals`；`0x641` = **1601**）。
    /// <para>要真测到**边界**就得能摆水晶数 —— 本仓的读口是 `CurrencyExtensions.GetOwnedCount`（走
    /// `CurrencyCatalog` + `InventoryManager` 的货币背包），而生产代码从不建它们 ⇒ 本组自己装一个最小夹具。
    /// 🔴 **副作用（如实标）**：`DeserializeInventory(null, null)` 会把门面的「已解包」闸置真（进程级）。
    /// ⇒ 本组**必须留在 `Run()` 末尾**（见文件头）；收尾只撤掉「假货币」，闸位不回翻（原版运行时本来就是真）。</para></summary>
    static void W6ShouldSellCardsFromCrate()
    {
        // 原版常量先单独钉一次：`0x641` = 1601（`decomp_full/PlayerDataManager__ShouldSellCardsFromCrate.c`）。
        Check(CardSell.CrystalThreshold, 1601,
              "506 `ShouldSellCardsFromCrate`：阈值 = **1601**（原版 `.c` 的 `0x641`）｜🧨 改成 1600/2000 ⇒ 红");

        // ---- 夹具：货币名册 + 货币背包 + 已解包闸 ----
        CurrencyCatalog.ClearForTest();
        var crystals = new Currency("crystals", GameCurrency.crystals);
        CurrencyCatalog.Register(crystals);
        var curInv = new Inventory<ICurrency>(x => x != null ? x.GetID() : null);
        curInv.ClearForTest();
        InventoryManager.Register(curInv);
        InventoryManager.DeserializeInventory(null, null);   // ⇒ `finishedUnpack = true`（否则 `GetOwnedCount` 只扫原始列表 = 恒 0）

        bool fixture = CurrencyCatalog.Find(GameCurrency.crystals) == crystals
                       && InventoryManager.GetInventory<ICurrency>() == curInv
                       && CurrencyExtensions.GetOwnedCount(GameCurrency.crystals) == 0;
        CheckTrue(fixture, "（前提）水晶夹具装好且初始拥有数 = 0（名册 / 背包 / 已解包闸三样都对）");
        if (!fixture) return;   // 前提已经计了一条 ✗，后面那三条边界断言没有意义（⛔ 不是静默跳过）

        CheckTrue(CardSell.ShouldSellCardsFromCrate(),
              "506 `ShouldSellCardsFromCrate`：0 颗水晶 ⇒ true（0 < 1601）｜🧨 把比较符号反过来 ⇒ 红");

        CurrencyExtensions.AddCurrency(crystals, 1600);
        Check(CurrencyExtensions.GetOwnedCount(GameCurrency.crystals), 1600,
              "（前提）把水晶调到 1600（`AddCurrency` 真的进了背包）");
        CheckTrue(CardSell.ShouldSellCardsFromCrate(),
              "506 `ShouldSellCardsFromCrate`：**1600** ⇒ true —— 边界下方一个数（这一条 + 下一条合起来就是 `<` 与 `<=` 的分界；"
            + "⛔ 只断「0 ⇒ true」的话，`<` 改成 `<=` 照样全绿）｜🧨 改成 `<=` 时本条仍绿、下条红");

        CurrencyExtensions.AddCurrency(crystals, 1);
        Check(CurrencyExtensions.GetOwnedCount(GameCurrency.crystals), 1601,
              "（前提）把水晶调到 **1601**（边界本身）");
        CheckTrue(!CardSell.ShouldSellCardsFromCrate(),
              "506 `ShouldSellCardsFromCrate`：**1601** ⇒ false（原版 `< 1601` 是严格小于）｜🧨 改成 `<=` ⇒ 本条红");

        CurrencyExtensions.AddCurrency(crystals, 999);
        CheckTrue(!CardSell.ShouldSellCardsFromCrate(),
              "506 `ShouldSellCardsFromCrate`：2600 ⇒ false｜🧨 改成 `!= 1601` / `> 0` 之类 ⇒ 红");

        // 收尾：只撤假货币（背包实例与「已解包」闸留着 —— 见上面 🔴）
        curInv.ClearForTest();
        CurrencyCatalog.ClearForTest();
    }

    /// <summary>483：`fromInit` 的一次性闸 + 「有待卖 ∧ 不在卖 ∧ 无冷却」才起 drain。
    /// 判据 = `PlayerDataManager__SellSpareCards.c`（`initialDuplicatesAlreadyProcessed` 那一支）。</summary>
    static void W6SellSpareCardsGates()
    {
        CardProgress.ResetForTest();      // 关掉落盘（自检绝不能改写玩家存档）
        CardSell.ResetForTest();
        CardProgress.AddOwned(W6SpareId, "common", 100);   // 保证多余副本足够（下面只按「差」断，不关心具体上限）
        int spares0 = CardProgress.Spares(W6SpareId, "common");
        CheckTrue(spares0 >= 4, $"（前提）夹具卡有多余副本 ≥ 4（实得 {spares0}）");

        // 第一次 `fromInit: true` ⇒ 过闸 + 真卖 2 张
        CardSell.SellSpareCards(new Dictionary<string, int> { { W6SpareId, 2 } }, fromInit: true);
        Check(CardSell.LastSoldCopies, 2,
              "483 `SellSpareCards(fromInit:true)`：第一次过闸、卖 2 张｜🧨 把一次性闸写成「恒 return」⇒ 实得 0");
        CheckTrue(CardSell.InitialDuplicatesAlreadyProcessed,
              "483 `SellSpareCards(fromInit:true)`：一次性闸被置真（原版 `initialDuplicatesAlreadyProcessed = true`）｜🧨 忘置 ⇒ 下一条红");
        int sparesAfterFirst = CardProgress.Spares(W6SpareId, "common");
        Check(sparesAfterFirst, spares0 - 2,
              "483 `SellSpareCards(fromInit:true)`：多余副本**真的**少了 2（不是只记数）｜🧨 只改计数不扣库存 ⇒ 红");

        // 第二次 `fromInit: true` ⇒ 被闸挡住：一张都不再卖
        CardSell.SellSpareCards(new Dictionary<string, int> { { W6SpareId, 2 } }, fromInit: true);
        Check(CardSell.LastSoldCopies, 2,
              "483 `SellSpareCards(fromInit:true)` 第二次 ⇒ **闸住**（`LastSoldCopies` 停在 2，说明递归没被再起一次）"
            + "｜🧨 去掉一次性闸 ⇒ 实得 4");
        Check(CardProgress.Spares(W6SpareId, "common"), sparesAfterFirst,
              "483 `SellSpareCards(fromInit:true)` 第二次 ⇒ 多余副本不变｜🧨 去掉闸 ⇒ 再少 2");

        // 第三次 `fromInit: false` ⇒ 那道闸只管 `fromInit` 那一支，普通调用照样卖
        //   （⌈灭自证⌉：如果实现把闸做在「整个方法入口」，上面两条仍然绿、这一条必红）
        CardSell.SellSpareCards(new Dictionary<string, int> { { W6SpareId, 2 } }, fromInit: false);
        Check(CardSell.LastSoldCopies, 2,
              "483 `SellSpareCards(fromInit:false)`：普通调用**不受**一次性闸影响，照卖 2 张｜🧨 把闸挪到方法入口 ⇒ 实得 0");
        Check(CardProgress.Spares(W6SpareId, "common"), sparesAfterFirst - 2,
              "483 `SellSpareCards(fromInit:false)`：多余副本再少 2｜🧨 同上");

        // 空待卖表 + 不在卖 + 无冷却 ⇒ 不空转（我们的口径：⛔ 不出声地全库扫）
        CardSell.ResetForTest();
        CardSell.SellSpareCards(null, fromInit: false);
        Check(CardSell.LastSoldCopies, 0,
              "483 `SellSpareCards(null)`：待卖表为 null ⇒ **不起 drain**（⛔ 全库扫会把升级冗余一起卖光；这条钉住「不空转」）"
            + "｜🧨 改成 null 也调递归 ⇒ 实得 > 0");
    }

    /// <summary>484：同步 drain 一遍 —— 每张卖 `min(多余副本, 表中要卖数)`，`表中要卖数 <= 0` ⇒ 卖光多余副本。
    /// 判据 = `_SellSpareCardsRecursive_d__602__MoveNext.c`（`min` 那一跳 + 逐张 `AddOwned(-n)`）。</summary>
    static void W6SellSpareCardsRecursiveDrain()
    {
        CardProgress.ResetForTest();
        CardSell.ResetForTest();
        CardProgress.AddOwned(W6SpareId, "common", 100);
        int spares0 = CardProgress.Spares(W6SpareId, "common");
        CheckTrue(spares0 >= 4, $"（前提）夹具卡有多余副本 ≥ 4（实得 {spares0}）");

        int owned0 = CardProgress.Owned(W6SpareId, "common");
        int gold0 = Wallet.Of("gold");

        // 要卖 2 张 ⇒ 卖 2
        CardSell.SellSpareCardsRecursive(new Dictionary<string, int> { { W6SpareId, 2 } });
        Check(CardSell.LastSoldCopies, 2,
              "484 `SellSpareCardsRecursive`：表中要卖 2 ⇒ 卖 2｜🧨 一张不卖 / 卖成 spares0 张 ⇒ 红");
        // ⌈灭自证⌉：**两个不同的量**（总拥有数 / 多余副本）都要正好少 2。
        //   只改一处（例如 `AddOwned` 改了、`Spares` 口径没变）不可能两条同时绿。
        Check(CardProgress.Owned(W6SpareId, "common"), owned0 - 2,
              "484 `SellSpareCardsRecursive`：**总拥有数**少了 2（真扣库存）｜🧨 只在内存里记数不扣 ⇒ 红");
        Check(CardProgress.Spares(W6SpareId, "common"), spares0 - 2,
              "484 `SellSpareCardsRecursive`：**多余副本**同时少了 2（与上一条是两个独立的量）｜🧨 扣成负数 / 扣错一张 ⇒ 红");
        Check(Wallet.Of("gold") - gold0, CardSell.LastSoldGold,
              "484 `SellSpareCardsRecursive`：金币**真的记账了**，且金额 = 上报的 `LastSoldGold`"
            + "（⚠️ 单价表是我们自定的 🔶，本条只断「记账发生了且与上报一致」，⛔ 不断单价）"
            + "｜🧨 删掉 `Wallet.Grant` ⇒ 左边 0、右边 > 0，红");

        // 要卖 999 张，但只剩 sparesNow 张 ⇒ 只卖得掉那么多（原版 `min` 那一跳）
        int sparesNow = CardProgress.Spares(W6SpareId, "common");
        CheckTrue(sparesNow > 0, $"（前提）还能再卖（剩余 {sparesNow}）");
        CardSell.SellSpareCardsRecursive(new Dictionary<string, int> { { W6SpareId, 999 } });
        Check(CardSell.LastSoldCopies, sparesNow,
              "484 `SellSpareCardsRecursive`：要卖 999 但只有这么多 ⇒ **只卖到 `min(多余, 999)`**（原版 `Math.Min` 那一跳）"
            + "｜🧨 按 999 硬扣 ⇒ 拥有数变负、本条实得 999");
        Check(CardProgress.Spares(W6SpareId, "common"), 0,
              "484 `SellSpareCardsRecursive`：把多余副本卖光后正好归 0（不是负数）｜🧨 超扣 ⇒ 实得负值");

        // `要卖数 <= 0` 那一档 = 「全卖光」（原版 `want` 缺省取 `spares` 的等价物）
        CardProgress.AddOwned(W6SpareId, "common", 5);
        int spares3 = CardProgress.Spares(W6SpareId, "common");
        CheckTrue(spares3 >= 3, $"（前提）又摆出多余副本（实得 {spares3}）");
        CardSell.SellSpareCardsRecursive(new Dictionary<string, int> { { W6SpareId, 0 } });
        Check(CardSell.LastSoldCopies, spares3,
              "484 `SellSpareCardsRecursive`：要卖数 **0** ⇒ 卖光全部多余副本（`want = spares` 那一档）｜🧨 按 0 卖 ⇒ 实得 0");

        // 「已在卖」⇒ 不再起第二条（原版同一档）
        CardProgress.AddOwned(W6SpareId, "common", 5);
        int spares4 = CardProgress.Spares(W6SpareId, "common");
        CardSell.ResetForTest();
        CardSell.IsSellingCards = true;
        CardSell.SellSpareCardsRecursive(new Dictionary<string, int> { { W6SpareId, 2 } });
        Check(CardSell.LastSoldCopies, 0,
              "484 `SellSpareCardsRecursive`：`isSellingCards` 已真 ⇒ **不再起一条**（原版同一档）｜🧨 去掉这道闸 ⇒ 实得 2");
        Check(CardProgress.Spares(W6SpareId, "common"), spares4,
              "484 `SellSpareCardsRecursive`：被闸挡住时**一张都不扣**｜🧨 去掉闸 ⇒ 少 2");
        CardSell.ResetForTest();
    }

    /// <summary>546：完成回调 —— 清 `isSellingCards` / 清待卖条数 / **记时间戳**（Unix 毫秒，真实墙钟）。
    /// 判据 = `PlayerDataManager___SellSpareCardsRecursive_b__602_0.c`。</summary>
    static void W6SellCompletedCallback()
    {
        CardSell.ResetForTest();
        CardSell.IsSellingCards = true;
        PlayerInventoryOps.PendingCardsToSell = 7;
        PlayerInventoryOps.LastInventoryOperationTime = 0;

        long before = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        CardSell.OnSellSpareCardsRecursiveCompleted();
        long stamp = PlayerInventoryOps.LastInventoryOperationTime;
        long now = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;

        CheckTrue(!CardSell.IsSellingCards,
              "546 完成回调：`isSellingCards` 清成 false｜🧨 不清 ⇒ 之后永远卖不了卡（后面每条都会红）");
        Check(PlayerInventoryOps.PendingCardsToSell, 0,
              "546 完成回调：待卖条数清成 0｜🧨 忘清 ⇒ 下次 `SellSpareCards` 的 `pending == 0` 闸进不去");
        // ⌈灭自证⌉：⛔ 不断「时间戳 != 0」（写死一个常量也能过）—— 断它落在【这一次调用前后】的真实墙钟窗口里。
        CheckTrue(stamp >= before && stamp <= now + 1000,
              $"546 完成回调：时间戳 = **真实 Unix 毫秒**（{before} ≤ {stamp} ≤ {now}），不是常量/不是秒"
            + "｜🧨 写成 0 / 写 `DateTime.Now.Ticks` / 写成秒 ⇒ 红");
        CheckTrue(stamp > 0,
              "546 完成回调：时间戳非 0（与上一条一起：既非 0、又在窗口里）｜🧨 写成 0 ⇒ 红");
    }

    // ==============================================================================================
    //  ③ 等级 / XP / 段位七条（762 / 763 / 764 / 765 / 766 / 768 / 769）
    // ==============================================================================================

    /// <summary>`VarsGlobal` 那七条（真身在 `CardPresentation/Core/LevelCurve.cs`）。
    /// <para>🔴 三条数（`xpPerLevel` / `maxLevel` / 段位阈值表）在原版是**服务端配置** ⇒ 本机没有。
    /// 所以**夹具自己注入一组**（`Configure(100, 50, {0,100,200})`），期望值按那段 `.c` 的算式**手算成字面量**：
    /// 这样断的是「算得对不对」，⛔ 不是「缺省值是多少」（那是 🔶 自定的，见 `LevelCurve.cs` 文件头）。</para></summary>
    public static void TestW6LevelCurve()
    {
        Section("🆕 W6：等级/XP/段位七条（等差阈值 · 从高到低扫 · 只算升段 · ConvertTier 的 −1）");
        W6LevelCurveArithmetic();
        W6LevelCurveDivisions();
    }

    static void W6LevelCurveArithmetic()
    {
        LevelCurve.Configure(100, 50, null);

        // 765 `GetNextLevelXP` —— 判据 `VarsGlobal__GetNextLevelXP.c`: `return level * xpPerLevel;`
        Check(LevelCurve.GetNextLevelXP(3), 300,
              "765 `GetNextLevelXP(3)` = 3 × 100 = **300**（原版 `level * xpPerLevel`）｜🧨 写成 `level * 100` 后把基数改成 7 仍绿 ⇒ 下一条挡它");
        LevelCurve.Configure(7, 50, null);
        Check(LevelCurve.GetNextLevelXP(3), 21,
              "765 `GetNextLevelXP(3)`（基数注入 7）= **21** ⇒ 乘的是**注入的基数**、不是写死的 100｜🧨 写死 100 ⇒ 实得 300");
        LevelCurve.Configure(100, 50, null);

        // 764 `GetHeroLevel` —— 等差阈值：第 n 级门槛 = n × xpPerLevel，逐级减。
        Check(LevelCurve.GetHeroLevel(0), 1, "764 `GetHeroLevel(0)` = **1**（最低 1 级）｜🧨 从 0 起算 ⇒ 红");
        Check(LevelCurve.GetHeroLevel(99), 1, "764 `GetHeroLevel(99)` = **1**（差 1 点到 2 级）｜🧨 用 `exp/xp` 整除 ⇒ 0");
        Check(LevelCurve.GetHeroLevel(100), 2, "764 `GetHeroLevel(100)` = **2**（刚好够第 1 级门槛）｜🧨 用 `<` 判反 ⇒ 1");
        Check(LevelCurve.GetHeroLevel(299), 2, "764 `GetHeroLevel(299)` = **2**（第 2 级门槛是 200**累计** 300，还差 1）"
                                            + "｜🧨 用「`exp < level*100` 不累计」那种写法 ⇒ 实得 3");
        Check(LevelCurve.GetHeroLevel(300), 3, "764 `GetHeroLevel(300)` = **3**（累计门槛 100+200）｜🧨 门槛算错 ⇒ 实得 2");
        LevelCurve.Configure(100, 5, null);
        Check(LevelCurve.GetHeroLevel(1000000), 5, "764 `GetHeroLevel`：超上限 ⇒ **夹在 `maxLevel` = 5**（原版 `level <= maxLevel` 那个循环边界）｜🧨 不夹 ⇒ 实得很大的数");
        LevelCurve.Configure(100, 50, null);

        // 768 `GetXPInLevel` —— 同一条循环，返回**本级内**还剩多少。
        Check(LevelCurve.GetXPInLevel(0), 0, "768 `GetXPInLevel(0)` = **0**｜🧨 从 1 起减 ⇒ 红");
        Check(LevelCurve.GetXPInLevel(99), 99, "768 `GetXPInLevel(99)` = **99**｜🧨 减一次门槛 ⇒ 负数");
        Check(LevelCurve.GetXPInLevel(100), 0, "768 `GetXPInLevel(100)` = **0**（刚好跨级）｜🧨 不减 ⇒ 100");
        Check(LevelCurve.GetXPInLevel(299), 199, "768 `GetXPInLevel(299)` = **199**（299 − 100）｜🧨 只减第一个门槛 ⇒ 199 恰巧对，见下一条");
        Check(LevelCurve.GetXPInLevel(300), 0, "768 `GetXPInLevel(300)` = **0**（300 − 100 − 200）｜🧨 只减第一个门槛 ⇒ 实得 200");

        // 769 `GetXPMissingToNextLevel` —— 判据 `.c` 末句 `iVar6 - param_2`（本级阈值 − 本级内剩余）。
        Check(LevelCurve.GetXPMissingToNextLevel(0), 100, "769 `GetXPMissingToNextLevel(0)` = **100**（第 1 级门槛 100 − 0）｜🧨 用「下一级门槛」算 ⇒ 200");
        Check(LevelCurve.GetXPMissingToNextLevel(99), 1, "769 `GetXPMissingToNextLevel(99)` = **1**｜🧨 差一 ⇒ 红");
        Check(LevelCurve.GetXPMissingToNextLevel(299), 1, "769 `GetXPMissingToNextLevel(299)` = **1**（本级门槛 200 − 199）｜🧨 用 100 当门槛 ⇒ 负数");
        Check(LevelCurve.GetXPMissingToNextLevel(300), 300, "769 `GetXPMissingToNextLevel(300)` = **300**（第 3 级门槛 300 − 0）｜🧨 用 200 ⇒ 红");

        LevelCurve.ResetForTest();   // 收尾（缺省值也是 🔶 自定的，别留给后面的用例）
    }

    static void W6LevelCurveDivisions()
    {
        // 段位阈值表升序 {0, 100, 200}（原版那张表也在服务端 ⇒ 注入）。
        LevelCurve.Configure(100, 50, new[] { 0, 100, 200 });

        // 766 `GetPlayerDivision` —— 判据 `.c`：**从高到低**扫，第一个 `thresholds[i] <= rating` 的 i。
        Check(LevelCurve.GetPlayerDivision(0), 0, "766 `GetPlayerDivision(0)` = **0**（刚好够最低段位）｜🧨 用 `>` ⇒ 扫不到、仍 0（假绿）⇒ 靠下一条");
        Check(LevelCurve.GetPlayerDivision(99), 0, "766 `GetPlayerDivision(99)` = **0**（还差 1 分）｜🧨 用「最近段位」那种圆整 ⇒ 1");
        Check(LevelCurve.GetPlayerDivision(100), 1, "766 `GetPlayerDivision(100)` = **1**｜🧨 用 `<` ⇒ 0");
        Check(LevelCurve.GetPlayerDivision(199), 1, "766 `GetPlayerDivision(199)` = **1**｜🧨 红");
        Check(LevelCurve.GetPlayerDivision(200), 2, "766 `GetPlayerDivision(200)` = **2**｜🧨 红");
        Check(LevelCurve.GetPlayerDivision(999999), 2,
              "766 `GetPlayerDivision(999999)` = **2**（最高段位）"
            + "｜🧨 **从低到高**扫并返回首个命中 ⇒ 实得 0 —— 这一条是「扫的方向」的唯一判据");
        Check(LevelCurve.GetPlayerDivision(-1), 0,
              "766 `GetPlayerDivision(-1)` = **0**（扫不到 ⇒ 原版 `return 0` 兜底）｜🧨 返回 -1 ⇒ 红");

        // 763 `ConvertTierToCheckPrize` —— 判据 `.c`: `(段位计数 - tier) - 1`。
        Check(LevelCurve.ConvertTierToCheckPrize(0), 2, "763 `ConvertTierToCheckPrize(0)` = (3−0)−1 = **2**｜🧨 忘了 −1 ⇒ 3");
        Check(LevelCurve.ConvertTierToCheckPrize(1), 1, "763 `ConvertTierToCheckPrize(1)` = **1**｜🧨 红");
        Check(LevelCurve.ConvertTierToCheckPrize(2), 0, "763 `ConvertTierToCheckPrize(2)` = **0**｜🧨 红");
        Check(LevelCurve.ConvertTierToCheckPrize(3), -1, "763 `ConvertTierToCheckPrize(3)` = **−1**（原版**没有**夹到 0）｜🧨 夹 0 ⇒ 红");

        // 762 `CheckIfPlayerChangedDivision` —— 判据 `.c`: `Division(old) < Division(new)`。
        //   🔴 哪怕方法名叫「changed」，原版**只管升段**（降段 false）。这条照抄，⛔ 别「修」成 `!=`。
        CheckTrue(LevelCurve.CheckIfPlayerChangedDivision(50, 150),
              "762 `CheckIfPlayerChangedDivision`：升段（0 → 1）⇒ true｜🧨 恒 false ⇒ 红");
        // ⌈灭自证⌉：下面这一条**结构上不可能**与「`!=` 实现」同时满足 —— 它就是「照抄原版」的判别式。
        CheckTrue(!LevelCurve.CheckIfPlayerChangedDivision(150, 50),
              "762 `CheckIfPlayerChangedDivision`：**降段（1 → 0）⇒ false** —— 原版只算升段"
            + "｜🧨 按方法名「修」成 `Division(old) != Division(new)` ⇒ 本条红（这正是原版行为）");
        CheckTrue(!LevelCurve.CheckIfPlayerChangedDivision(150, 150),
              "762 `CheckIfPlayerChangedDivision`：段位没变 ⇒ false｜🧨 恒 true ⇒ 红");
        CheckTrue(!LevelCurve.CheckIfPlayerChangedDivision(250, 10),
              "762 `CheckIfPlayerChangedDivision`：2 → 0（跨两段降）⇒ false｜🧨 改成比较分数大小 ⇒ 红");

        // 🔶 空阈值表那一档（**我们的兜底**：原版那份配置本机没有）—— 断「不静默假装有段位」：恒 0。
        LevelCurve.Configure(100, 50, null);
        Check(LevelCurve.GetPlayerDivision(999999), 0,
              "🔶 `GetPlayerDivision` 空阈值表 ⇒ **0**（我们的兜底口径：本机没有服务端配置；原版那一档拿不到判据）"
            + "｜🧨 改成返回 -1 / 抛异常 ⇒ 红");
        CheckTrue(!LevelCurve.CheckIfPlayerChangedDivision(0, 999999),
              "🔶 空阈值表 ⇒ 两边段位都是 0 ⇒ `CheckIfPlayerChangedDivision` 恒 false｜🧨 改成「分数涨了就算升段」⇒ 红");

        LevelCurve.ResetForTest();
    }

    // ==============================================================================================
    //  ④ LoadableReference 三条重复键（296 / 297 / 303）＋ 新加的 `==` / `!=`
    // ==============================================================================================

    /// <summary>`LoadableRef&lt;T&gt;` 的三个序列化钩子 + `operator ==` / `!=`。
    /// <para>判据 = `D:/2/tools/decomp_generic_trial/out/LoadableReference_object___OnBeforeRead__101b150.c` /
    /// `…__OnBeforeWrite__4b33b0.c`（全局空桩 `ret 0`）/ `…__WriteJson__101bec0.c` /
    /// `…__op_Equality__101c3f0.c` / `…__op_Inequality__101cec0.c`。</para>
    /// <para>⚠️ 同三钩子在 `CardPresentation/Editor/LoadableRefCheck.cs`（`_run_8_checks.sh` 第 16 条宿主）
    /// 已有更全的一套（36 条）；本组是**本域账行自己的**锚点（账 `其他_基础设施:296·297·303`），不是替代它。</para></summary>
    public static void TestW6LoadableRefHooks()
    {
        Section("🆕 W6：`LoadableRef` 的三个序列化钩子（296/297/303）＋ `==`/`!=` 一对（原版比 targetId、且 null 安全）");

        LoadableRef<W6ProbeItem>.ResetNotesForTest();

        // ── 前提：`WriteJson` 的返回类型真是 `object`（我们拿 `object` 代替 `JToken`） ──
        var wjType = typeof(LoadableRef<W6ProbeItem>).GetMethod("WriteJson");
        CheckTrue(wjType != null && wjType.ReturnType == typeof(object),
              "（前提）`LoadableRef<T>.WriteJson` 存在、返回 `object`（我们拿 `object` 代替原版的 `JToken`）");
        if (wjType == null) return;   // 前提已计 ✗

        // 296 `OnBeforeRead` —— 原版：`if (rawJson.Type == JTokenType.String) targetId = rawJson.Value<string>();`
        //   ⇒ 只有「字符串」那一档才写；其它 token 原样不动；null 直接 NRE（首句就解引用 `rawJson.Type`）。
        var rd = new LoadableRef<W6ProbeItem>("before");
        rd.OnBeforeRead("after");
        Check(rd.TargetId, "after", "296 `OnBeforeRead(字符串)`：`targetId` 取该字符串｜🧨 不写 ⇒ 实得 before");
        rd.OnBeforeRead(42L);
        Check(rd.TargetId, "after",
              "296 `OnBeforeRead(非字符串 token)`：**原样不动**（原版只认 `JTokenType.String` 那一档）"
            + "｜🧨 写成「都 `ToString()` 进去」⇒ 实得 42");
        bool readNre = false;
        try { rd.OnBeforeRead(null); } catch (NullReferenceException) { readNre = true; }
        CheckTrue(readNre,
              "296 `OnBeforeRead(null)` ⇒ **NullReferenceException**（原版首句 `rawJson.Type` 就解引用；照抄，⛔ 别改成静默 no-op）"
            + "｜🧨 加一句 null 判静默返回 ⇒ 本条红");

        // 297 `OnBeforeWrite` —— 原版是**空体**（RVA `0x4B33B0` = 全局空桩，机器码 `ret 0`）。
        var itemW = new W6ProbeItem("nowrite");
        var wr = new LoadableRef<W6ProbeItem>(itemW, W6ProbeIdOf);
        wr.OnBeforeWrite();
        Check(wr.TargetId, "nowrite",
              "297 `OnBeforeWrite`：**无副作用** —— id 不许变（原版空体）｜🧨 顺手清 id / 清本体 ⇒ 红");
        CheckTrue(ReferenceEquals(wr.Reference, itemW),
              "297 `OnBeforeWrite`：本体也不许被清（原版空体）｜🧨 顺手 `reference = null` ⇒ 红");

        // 303 `WriteJson` —— 原版：`[先回填] return new JValue(targetId);`
        //   ⇒ 写出去的是**裸 id 字符串**（不是对象、不是整份引用）。
        var itemJ = new W6ProbeItem("w1");
        var wj = new LoadableRef<W6ProbeItem>(itemJ, W6ProbeIdOf);
        object json = wj.WriteJson();
        CheckTrue(json is string,
              "303 `WriteJson`：返回的是**标量字符串**，不是本体对象（原版 `new JValue(targetId)`）"
            + "｜🧨 改成 `return reference` ⇒ 本条红（这一条是 ⌈灭自证⌉ 的关键：只比 `== \"w1\"` 的话，"
            + "把本体 `ToString()` 成 id 的写法也能绿）");
        Check((string)(json as string ?? "<non-string>"), "w1",
              "303 `WriteJson`：写出的就是 `targetId`｜🧨 写成空串 / 写成 name ⇒ 红");
        var onlyId = new LoadableRef<W6ProbeItem>("w2");     // 只有 id、没有本体，也要写得出来
        object json2 = onlyId.WriteJson();
        CheckTrue(json2 is string && (string)json2 == "w2",
              "303 `WriteJson`：只有 id、没有本体的引用同样写出 id｜🧨 改成「没有本体就写 null」⇒ 红");
        object jsonNull = new LoadableRef<W6ProbeItem>((string)null).WriteJson();
        CheckTrue(jsonNull == null,
              "303 `WriteJson`：id 为 null ⇒ 写 null（原版 `new JValue((string)null)` 序列化出来同样是 null）｜🧨 写出空串 ⇒ 红");

        // ── `operator ==` / `operator !=`（一对；判据 `…__op_Equality__101c3f0.c` / `…__op_Inequality__101cec0.c`）──
        //   原版逐个判「这一侧是不是 null」，再 `String.Equals(a.targetId, b.targetId)`
        //   ⇒ ① 比的是 **targetId 字符串**（不是引用）；② **null 安全**（这一点与 `Equals(T)` 不同，那个传 null 会 NRE）。
        var sameA = new LoadableRef<W6ProbeItem>(new W6ProbeItem("same"), W6ProbeIdOf);
        var sameB = new LoadableRef<W6ProbeItem>(new W6ProbeItem("same"), W6ProbeIdOf);
        var diff = new LoadableRef<W6ProbeItem>(new W6ProbeItem("diff"), W6ProbeIdOf);
        LoadableRef<W6ProbeItem> nullRef = null;

        CheckTrue(sameA == sameB,
              "`==`：两份**不同实例、同 id** ⇒ true（原版比 `targetId` 字符串）"
            + "｜🧨 改成比引用（`ReferenceEquals`）⇒ 红 —— 这正是 `List<LoadableRef<T>>.Contains` 能不能工作的唯一依据");
        CheckTrue(!(sameA == diff), "`==`：id 不同 ⇒ false｜🧨 恒 true ⇒ 红");
        CheckTrue(sameA != diff, "`!=`：与 `==` 互补（id 不同 ⇒ true）｜🧨 `!=` 写成恒 false ⇒ 红");
        CheckTrue(!(sameA != sameB), "`!=`：与 `==` 互补（同 id ⇒ false）｜🧨 红");
        CheckTrue(nullRef == nullRef,
              "`==`：两边都是 null 引用 ⇒ **true**（原版两侧 id 都是 null ⇒ `String.Equals(null,null)`）｜🧨 改成「任一侧 null 就 false」⇒ 红");
        CheckTrue(!(nullRef == sameA),
              "`==`：null 引用 vs 有 id 的引用 ⇒ **false**（不是抛 NRE、也不是 true）｜🧨 直接解引用 ⇒ 抛 NRE");
        // ⌈灭自证⌉：`==` 是 **null 安全**的 —— 这一点把它与 `Equals(T)`（传 null 必 NRE）区分开。
        //   若有人把 `==` 实现成「转调 `Equals`」，上面 `nullRef == nullRef` 那条会抛 NRE ⇒ 红。
        var emptyId = new LoadableRef<W6ProbeItem>((string)null);
        CheckTrue(emptyId == nullRef,
              "`==`：**id 为空的引用与 null 相等**（原版判完 null 后比的是 targetId 字符串，两边都是 null）"
            + "｜🧨 改成「先判引用是否 null 再比」⇒ 红 —— 这是原版那一支的真实语义，照抄");
        CheckTrue(!(emptyId == sameA), "`==`：id 为空 vs id 非空 ⇒ false｜🧨 红");

        LoadableRef<W6ProbeItem>.ResetNotesForTest();
    }

    /// <summary>`LoadableRef&lt;T&gt;` 的夹具条目（`T : class` + 一个 id）。名字带 `W6` 前缀，⛔ 别与别的分册撞。</summary>
    public sealed class W6ProbeItem
    {
        public string Id;
        public W6ProbeItem(string id) { Id = id; }
    }

    /// <summary>🔶 取 id 的口（原版靠 `T : IIdentifiable.GetID()`；我们的 T 是普通 class ⇒ 构造时注入，见 `LoadableRef.cs` 文件头）。</summary>
    static readonly Func<W6ProbeItem, string> W6ProbeIdOf = x => x == null ? null : x.Id;
}
