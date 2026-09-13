// RuleEngineTest.cs — 规则引擎自检（纯逻辑，进 play 模式/图形设备都不需要）
//
// 语义基准是 `d:/warpforge/scripts/rule_core.gd` 的那份测试（99 个函数 / 320 条断言）。
// 这里挑**同一批语义**的用例，用同样的输入、对同样的期望值 ——
// 不一致就是移植错了，而不是「我的实现更合理」。
//
// 用法（菜单）：Tools > RuleEngine > 规则引擎自检
// 用法（CLI）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" \
//     -batchmode -quit -projectPath "D:\4\Unity\MyGame" -executeMethod RuleEngineTest.Run \
//     -logFile "d:/4/_tmp_view/ruleengine.log"
//   筛输出：grep "^RE " d:/4/_tmp_view/ruleengine.log
//   退出码：0 = 全过，1 = 有失败（批处理下会写进 Unity 的退出码）
using System;
using System.Collections.Generic;
using System.Text;
using RuleEngine;
using UnityEditor;
using UnityEngine;

public static partial class RuleEngineTest
{
    const string P = "RE ";

    static int _pass, _fail;
    static readonly List<string> _failures = new List<string>();

    // ==================================================================
    //  入口
    // ==================================================================

    [MenuItem("Tools/RuleEngine/规则引擎自检")]
    public static void Run()
    {
        _pass = 0; _fail = 0; _failures.Clear();

        Debug.Log(P + "=== 规则引擎自检 开始 ===");

        Section("棋盘一致性");
        TestBoardMatchesPresentation();

        Section("卡牌数据");
        TestKeywordParsing();
        TestCardDatabase();
        TestOriginalCardPool();

        Section("卡面逐张核对（字段 / 关键词修正）");
        TestCardFaceFixes();

        Section("战术卡文本（能解析 N/448）");
        TestTacticTextCoverage();

        Section("单位卡 desc 的效果文字（查证：接进 EffectText 能认多少 —— 只报数）");
        ReportUnitDescCoverage();

        Section("战术卡能打（解析 → 结算 → 弃牌堆）");
        TestTacticPlay();

        Section("卡组构筑");
        TestDeckRules();
        TestDeckValidation();
        TestDeckIntoBattle();
        TestDeckStoreRoundTrip();
        TestDeckLibrary();

        Section("开局");
        TestNewBattle();

        Section("开局换牌（Mulligan）");
        TestMulligan();

        Section("回合");
        TestBeginTurn();
        TestEnergyIsPerPlayer();

        Section("出牌");
        TestPlayCostRejected();
        TestPlaySlotRejected();
        TestPlayOccupiedRejected();
        TestPlayOk();

        Section("攻击");
        TestMeleeTrade();
        TestArmorMinimumOne();
        TestShieldBlocks();
        TestRangedEatsCounter();
        TestLongRangeNoCounter();

        Section("目标合法性");
        TestVanguardRestriction();
        TestStealthUntargetable();
        TestFlyingMeleeBlocked();

        Section("技能与触发");
        TestEffectSpecParsing();
        TestFlankInvulnVulnerable();
        TestAttackKeywords();
        TestBattleKeywords();
        TestRally();
        TestStrikeAndSlay();
        TestBacklash();
        TestPenitence();
        TestAbility();
        TestAbilityTargetRules();
        TestHealAndDraw();
        TestEffectChainGuard();
        TestStarterCardEffects();
        TestAiUsesAbility();

        Section("造牌（create）");
        TestCreate();

        Section("免费部署（deploy）");
        TestDeploy();

        Section("降费（lowercost）");
        TestLowerCost();

        Section("选牌（choosecard）");
        TestChooseCard();

        Section("回手/回牌库 + 指代抽牌（return / drawref / add）");
        TestReturnAndRefs();

        Section("常驻效果 / 手牌陷阱（回合起止触发）");
        TestPersistentEffects();

        Section("部署时触发 + 持续改费（常驻效果的另两个触发点）");
        TestDeployBuffAndCostMore();

        Section("单位卡 desc 的触发式效果（Rally/Strike/Slay/Backlash/Penitence）");
        TestUnitDescTriggers();

        Section("事件层（When <事件>, …）");
        TestWhenEvents();

        Section("阵营机制（repeat / Oath / Codex）");
        TestFactionMechanics();

        Section("胜负与疲劳");
        TestWinnerByWarlord();
        TestDrawIsDraw();
        TestForfeit();
        TestFatigue();

        Section("确定性");
        TestDeterminism();
        TestSignalDeterminism();

        Section("端到端");
        TestFullGameWithRealCards();

        Section("两个阵营：极限战士 vs 兽人");
        TestFactionBattle();

        // ---- 汇总 ----
        int total = _pass + _fail;
        if (_fail == 0)
        {
            Debug.Log(P + $"=== 结束：{_pass}/{total} 全过 ✅ ===");
        }
        else
        {
            var sb = new StringBuilder();
            sb.Append(P).Append($"=== 结束：{_pass}/{total} 通过，**{_fail} 条失败** ❌ ===");
            foreach (var f in _failures) sb.Append("\n").Append(P).Append("   ✗ ").Append(f);
            Debug.LogError(sb.ToString());
        }

        if (Application.isBatchMode)
            EditorApplication.Exit(_fail == 0 ? 0 : 1);
    }

    // ==================================================================
    //  断言
    // ==================================================================

    static void Section(string title) { Debug.Log(P + $"--- {title} ---"); }

    static void Check<T>(T got, T want, string msg)
    {
        if (EqualityComparer<T>.Default.Equals(got, want))
        {
            _pass++;
            Debug.Log(P + $"   ✓ {msg}");
        }
        else
        {
            _fail++;
            string line = $"{msg} —— 期望 [{want}]，实得 [{got}]";
            _failures.Add(line);
            Debug.LogError(P + $"   ✗ {line}");
        }
    }

    static void CheckCode(int got, int want, string msg)
    {
        Check(got, want, msg + $"（{RuleCodes.Describe(got)}）");
    }

    static void CheckTrue(bool cond, string msg) { Check(cond, true, msg); }

    // ==================================================================
    //  夹具
    //
    //  刻意**走真实的 NewBattle 路径**（shuffle:false），而不是手动拼 BattleContext ——
    //  这样开局逻辑本身也在被验，夹具不会和实现悄悄漂移。
    // ==================================================================

    static CardDef Unit(string name, int cost, int atk, int hp, params string[] kws)
    {
        return new CardDef(name, name, "unit", "", null, "Test",
                           cost, atk, hp, 0, kws);
    }

    static CardDef Ranged(string name, int cost, int atk, int hp, int ranged, params string[] kws)
    {
        return new CardDef(name, name, "unit", "", null, "Test",
                           cost, atk, hp, ranged, kws);
    }

    static CardDef Hero(string name, int atk, int hp)
    {
        return new CardDef(name, name, "hero", "", null, "Test", 0, atk, hp, 0, null);
    }

    /// <summary>一张**战术卡**。`desc` 走 `Core/EffectText` 那套（原版卡面文本的语法）。</summary>
    static CardDef Tactic(string name, int cost, string desc)
    {
        return new CardDef(name, name, "tactic", desc, "common", "Test", cost, 0, 0, 0, null);
    }

    /// <summary>
    /// 组一副「起手就是 hand 的顺序」的牌库。
    /// 抽牌是 `pop_back`，所以要把 hand **反转**放 —— 和 rule_core 的 `_deck()` 一个道理。
    /// 垫牌必须放在**反转后的 hand 前面**（即牌库的开头），否则 pop_back 先抽到的是垫牌。
    /// </summary>
    static List<CardDef> Deck(params CardDef[] hand)
    {
        var d = new List<CardDef> { Hero("FixtureWarlord", 2, 30) };
        for (int i = 0; i < 20; i++) d.Add(Unit("filler" + i, 1, 0, 1));
        for (int i = hand.Length - 1; i >= 0; i--) d.Add(hand[i]);
        return d;
    }

    static BattleContext Battle(CardDef[] hand0, CardDef[] hand1)
    {
        return RuleCore.NewBattle(Deck(hand0), Deck(hand1), seed: 0, shuffle: false);
    }

    /// <summary>带**卡池**的夹具 —— 造牌（`create`）要从卡池里筛候选，不带池子就算不出来。</summary>
    static BattleContext BattlePool(CardDef[] hand0, CardDef[] hand1, IList<CardDef> pool,
                                    string warlordFaction = "Test", int seed = 0)
    {
        return RuleCore.NewBattle(DeckOf(warlordFaction, hand0), Deck(hand1),
                                  seed: seed, shuffle: false, cardPool: pool);
    }

    /// <summary>和 `Deck` 一样，但督军带阵营（造牌要按施放者阵营筛卡）。</summary>
    static List<CardDef> DeckOf(string warlordFaction, params CardDef[] hand)
    {
        var d = new List<CardDef> { HeroOf("FixtureWarlord", warlordFaction, 2, 30) };
        for (int i = 0; i < 20; i++) d.Add(Unit("filler" + i, 1, 0, 1));
        for (int i = hand.Length - 1; i >= 0; i--) d.Add(hand[i]);
        return d;
    }

    static CardDef HeroOf(string name, string faction, int atk, int hp)
    {
        return new CardDef(name, name, "hero", "", null, faction, 0, atk, hp, 0, null);
    }

    static void PassTurn(BattleContext ctx)
    {
        RuleCore.EndTurn(ctx);
        RuleCore.BeginTurn(ctx);
    }

    /// <summary>推进到「P1 的第 n 个回合」（和 rule_test.gd 的 `_to_p1_turn` 同构）</summary>
    static void ToP1Turn(BattleContext ctx, int n)
    {
        int target = 2 * n - 1;
        int guard = 0;
        while (ctx.Turn != target || ctx.Active != 0)
        {
            if (ctx.Turn == 0) RuleCore.BeginTurn(ctx);
            else PassTurn(ctx);
            if (++guard > 40) { CheckTrue(false, "回合推进超限，疑似死循环"); return; }
        }
    }

    static int HandIdx(BattleContext ctx, int p, string name)
    {
        var hand = ctx.Players[p].Hand;
        for (int i = 0; i < hand.Count; i++) if (hand[i].Name == name) return i;
        return -1;
    }

    static UnitState Board(BattleContext ctx, int p, int slot) { return ctx.Players[p].Board[slot]; }

    /// <summary>把一个单位直接摆到场上（跳过部署流程，只为构造攻击场景）</summary>
    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card, bool exhausted = false)
    {
        var u = new UnitState(card, false) { Exhausted = exhausted };
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    /// <summary>手牌里有没有这个**兵种**的卡（选牌自检用：挑了哪张是随机的，只能按兵种认）</summary>
    static bool HasSubtype(List<CardDef> hand, string subtype)
    {
        foreach (var c in hand) if (c.Subtype == subtype) return true;
        return false;
    }

    /// <summary>某个名字的单位在**哪一格**（找不到返回 -1）。选牌的部署走 `DeployFree`，
    /// 落点是**第一个空格**，所以不能写死槽号。</summary>
    static int SlotOf(BattleContext ctx, int p, string name)
    {
        for (int s = 0; s < BoardSpec.Size; s++)
        {
            var u = ctx.Players[p].Board[s];
            if (u != null && u.Name == name) return s;
        }
        return -1;
    }

    // ==================================================================
    //  棋盘一致性
    // ==================================================================

    /// <summary>
    /// 规则引擎的棋盘和表现层的棋盘**必须是同一个**。
    ///
    /// 这条断言就是冲着「7 格 vs 9 格」那次事故加的：表现层自己拍了 7 格，
    /// 而规则引擎/规则书/原版资源三处都是 9 格 —— 两边各写各的，谁也不会报错，
    /// 直到玩家发现「第 8、9 格放不了牌」才暴露。
    /// 放在 Editor 测试里是因为只有这里能同时看见两个命名空间（Core/ 不依赖 Unity）。
    /// </summary>
    static void TestBoardMatchesPresentation()
    {
        Check(RuleEngine.BoardSpec.Size, CardPresentation.BoardLayout.SlotCount,
              "规则引擎棋盘格数 == 表现层格数");
        Check(RuleEngine.BoardSpec.WarlordSlot, CardPresentation.BoardLayout.WarlordSlot,
              "督军槽位号两边一致");
        Check(RuleEngine.BoardSpec.Size, 9, "棋盘 9 格（规则书 :30 / rule_core.gd:44 / MinionArea）");
        Check(RuleEngine.BoardSpec.WarlordSlot, 4, "督军在中位槽 4");
        Check(RuleEngine.BoardSpec.SlotsPerSide * 2 + 1, RuleEngine.BoardSpec.Size,
              "两侧各 4 格 + 督军 = 9");

        int deployable = 0;
        for (int s = 0; s < RuleEngine.BoardSpec.Size; s++)
            if (RuleEngine.BoardSpec.IsDeployable(s)) deployable++;
        Check(deployable, 8, "可部署格 8 个（规则书「两侧各 4 格共可部署 8 张部队卡」）");
        CheckTrue(!RuleEngine.BoardSpec.IsDeployable(RuleEngine.BoardSpec.WarlordSlot),
                  "督军格不可部署");
    }

    // ==================================================================
    //  卡牌数据
    // ==================================================================

    static void TestKeywordParsing()
    {
        // 卡面写法很脏（146 种不同串）—— 这几条是实测出来的真实形态
        var c = new CardDef("t", "T", "unit", "", null, null, 1, 1, 1, 0,
                            new[] { "Armour 1", "Long Range", "Can't Attack", "Blast 2", "Shield",
                                    "Blood Thirst", "Rally: Deal 3 damage to an enemy", "Armour 2." });
        Check(c.KwValue("armour"), 2, "同一张卡两处 Armour → 后者覆盖（Armour 2. 的句号不影响取数）");
        CheckTrue(c.Has("longrange"), "Long Range → longrange");
        CheckTrue(c.Has("cantattack"), "Can't Attack → cantattack");
        Check(c.KwValue("blast"), 2, "Blast 2 → blast 值 2");
        Check(c.KwValue("shield"), 1, "无数字的关键词「存在即真」取 1");
        CheckTrue(c.Has("bloodthirst"), "Blood Thirst → bloodthirst");
        CheckTrue(c.Has("rally"), "「Rally: 效果文字」只取冒号前的关键词部分");

        var bare = new CardDef("t2", "T2", "unit", "", null, null, 1, 1, 1, 0, new[] { "Armour" });
        Check(bare.KwValue("armour"), 1, "光写 Armour 不带数字 → 1（不是 0）");

        var none = new CardDef("t3", "T3", "unit", "", null, null, 1, 1, 1, 0, null);
        Check(none.Keywords.Count, 0, "keywords 为 null 不炸");
    }

    static void TestCardDatabase()
    {
        var pool = CardDatabase.Load();
        CheckTrue(pool.Count >= 1000, $"卡表加载：{pool.Count} 张（应 ≥ 1000）");

        int units = CardDatabase.Units(pool).Count;
        CheckTrue(units >= 500, $"单位卡 {units} 张（应 ≥ 500）");

        var autarch = CardDatabase.Find(pool, "Autarch");
        CheckTrue(autarch != null, "能找到 Autarch");
        if (autarch != null)
        {
            Check(autarch.Cost, 6, "Autarch 费用 6");
            Check(autarch.Attack, 5, "Autarch 近战 5");
            Check(autarch.Health, 6, "Autarch 生命 6");
            Check(autarch.RangedAttack, 6, "Autarch 远程 6（数据里 armor 字段实为远程，已迁移）");
        }

        var factions = CardDatabase.Factions(pool);
        CheckTrue(factions.Count >= 10, $"阵营 {factions.Count} 个（应 ≥ 10）");

        // 未实现关键词的量化 —— 不是让它静默失效，而是能一眼看到还差多少
        var unimplemented = RuleCore.UnimplementedKeywords(pool);
        Debug.Log(P + $"   全卡池未实现关键词 {unimplemented.Count} 个："
                    + string.Join(" / ", unimplemented.GetRange(0, Math.Min(12, unimplemented.Count)))
                    + (unimplemented.Count > 12 ? " …" : ""));
        CheckTrue(unimplemented.Count > 0, "确实有未实现的关键词（v1 只做 5 个）");
    }

    /// <summary>
    /// **原版卡牌接进对战**（2026-09-12）—— 卡池 → 中文 → 组卡 这条链的数据断言。
    /// 「真的打进一局、卡面长什么样」在 `BattleScene.Run` 里验（那边有画面和截图）。
    /// </summary>
    static void TestOriginalCardPool()
    {
        var pool = CardDatabase.Load();

        // ① 中文并进卡表了没有（`工具/gen_cards_engine.py` 从 `数据/卡牌翻译/zh_cards.json` 并的）
        int zhName = 0, zhDesc = 0;
        foreach (var c in pool)
        {
            if (!string.IsNullOrEmpty(c.NameZh)) zhName++;
            if (!string.IsNullOrEmpty(c.DescZh)) zhDesc++;
        }
        CheckTrue(zhName >= 1100, $"有中文卡名 {zhName}/{pool.Count} 张（应 ≥ 1100）");
        CheckTrue(zhDesc >= 1100, $"有中文效果 {zhDesc}/{pool.Count} 张（应 ≥ 1100）");
        // 没翻译的那几张**不静默**：卡面回英文名，数量在这里报出来
        Debug.Log(P + $"   没有中文的 {pool.Count - zhName} 张（卡面回英文名，已知）");

        var desolation = CardDatabase.Find(pool, "Desolation Marine");
        CheckTrue(desolation != null, "能找到 Desolation Marine");
        if (desolation != null)
        {
            CheckTrue(!string.IsNullOrEmpty(desolation.NameZh),
                      $"中文名拿得到：「{desolation.NameZh}」");
            CheckTrue(desolation.FromOriginalPool, "标了「来自原版卡池」→ 卡面文字取它自己的效果原文");
        }
        // 我们自己设计的卡**不该**被标成原版卡（否则卡面会去显示风味文字）
        var ember = StarterCards.Ember()[0];
        CheckTrue(!ember.FromOriginalPool, "自设计卡没被标成原版卡（卡面仍走引擎关键词那套）");

        // ② 两个阵营的体量 —— 挑它们就是因为单位够多、够凑满 30 张
        int um = CardDatabase.OfFaction(pool, "Ultramarines").Count;
        int goff = CardDatabase.OfFaction(pool, "Goff").Count;
        CheckTrue(um >= 130, $"Ultramarines {um} 张（应 ≥ 130）");
        CheckTrue(goff >= 115, $"Goff {goff} 张（应 ≥ 115）");

        // ③ 阵营名走错池子要**报错**，不能静默替一套（原来 `Of()` 是「不是 Tide 就当 Ember」）
        Check(StarterCards.Of("Ultramarines").Count, 0,
              "StarterCards.Of(原版阵营) 返回空表 —— 不再静默给一套 Ember");
        Check(StarterCards.Of(StarterCards.EmberFaction).Count, 13, "StarterCards.Of(Ember) 照常 13 张");

        // ④ 两个阵营各凑一副，逐条查构成
        foreach (var fac in new[] { "Ultramarines", "Goff" })
        {
            var deck = DeckBuilder.StarterDeck(pool, fac, DeckBuilder.ClassicDeckSize, new System.Random(7));
            Check(deck.Count, DeckBuilder.ClassicDeckSize, $"{fac} 凑满 {DeckBuilder.ClassicDeckSize} 张");
            Check(deck[0].Type, "hero", $"{fac} 第 0 张是督军");
            CheckTrue(deck[0].FromOriginalPool, $"{fac} 的牌全来自原版卡池");

            int notUnit = 0, foreign = 0;
            foreach (var c in deck)
            {
                if (c.Type != "hero" && c.Type != "unit") notUnit++;
                if (c.Faction != fac) foreign++;
            }
            Check(notUnit, 0, $"{fac} 牌组里除督军外**全是单位卡**（战术/防御没混进来）");
            Check(foreign, 0, $"{fac} 牌组全同阵营");
            Debug.Log(P + $"   {fac} 曲线：" + string.Join(" ", DeckBuilder.CostCurve(deck)));
        }

        // ⑤ 稀有度必须以**卡面宝石扫描表**为准，不许退回 OCR 的 `card_stats.json.rarity`
        //
        //    ⚠️ 2026-09-12 抓到的真 bug：`工具/gen_cards_engine.py` 原来写成
        //    `if rarity not in RARITIES:` —— 只在 OCR 值是**非法串**（空串 / `defence`）时才查宝石表，
        //    于是 OCR 写错但「看起来合法」的值原样放行：**140 张被记成 `legendary` 的卡**一直没纠
        //    （改之前 legendary 341 张，宝石实测只有 197 张）。
        //    影响面不只是显示：`DeckRules.CopyLimit` 按稀有度定同名牌上限（传说 1 张、其余 2 张），
        //    所以这 140 张的组卡规则一直是错的。
        //
        //    下面四张是**对着卡面核过**的（底部那颗菱形宝石的颜色）：
        //      `D:/2/Warpforge部队卡片/Aeldari/3部队/Warpforge_08_Night-Spinner.png` → 浅蓝 = common
        //      `…/Aeldari/3部队/Warpforge_03_Shining-Spear.png`                      → 浅蓝 = common
        foreach (var (name, want) in new[]
                 {
                     ("Autarch", "common"),
                     ("Night Spinner", "common"),
                     ("Shining Spear", "common"),
                     ("Ursula Creed", "epic"),
                 })
        {
            var rc = CardDatabase.Find(pool, name);
            CheckTrue(rc != null, $"找得到 {name}");
            if (rc != null) Check(rc.Rarity, want, $"{name} 的稀有度 = {want}（卡面宝石实测）");
        }
        // **同名不同卡**（卡名当 id 的后果）：宝石表按卡名查，重名时后写的赢，
        // 所以这几张必须按「阵营 + 卡名」分开钉住。同样是对着卡面核的：
        //   `Chaos/3部队/Warpforge_44_Terminator-Champion.png`（Black Legion）→ 浅蓝 common
        //   `Chaos/3部队/Warpforge_46_Maulerfiend.png`（Black Legion）          → 紫   epic
        //   `Dark Angels/3部队/Warpforge_26_Bladeguard-Veteran.png`             → 绿   rare
        //   ⚠️ 宝石表里那行**批次 U（= Ultramarines）的 Bladeguard Veteran（common）在我们卡池里没有**
        //      —— `card_stats.json` 上游就缺这张（卡面图 `Ultramarines/3部队/Warpforge_34_…` 是有的），
        //      已记进 `资料/卡牌数据源对账_0912.md` 的「只有文档有」。所以这里只钉 DarkAngels 那张。
        foreach (var (faction, name, want) in new[]
                 {
                     ("BlackLegion", "Terminator Champion", "common"),
                     ("EmperorsChildren", "Terminator Champion", "epic"),
                     ("BlackLegion", "Maulerfiend", "epic"),
                     ("EmperorsChildren", "Maulerfiend", "common"),
                     ("DarkAngels", "Bladeguard Veteran", "rare"),
                 })
        {
            CardDef rc = null;
            foreach (var c in pool)
                if (c.Faction == faction && c.Name == name) { rc = c; break; }
            CheckTrue(rc != null, $"找得到 {faction} 的 {name}");
            if (rc != null) Check(rc.Rarity, want, $"{faction} 的 {name} 稀有度 = {want}（重名分开定档）");
        }
        int legend = 0;
        foreach (var c in pool) if (DeckRules.IsLegendary(c.Rarity)) legend++;
        CheckTrue(legend <= 230, $"传说卡 {legend} 张（应 ≤ 230 —— OCR 误判那版是 341）");

        // ⑥ 数值修正 —— OCR 把**紫圆（远程）**读错/漏读的那几张（`gen_cards_engine.py` 的 `STAT_FIXES`）。
        //    卡面四个圆的出处（原版 prefab 节点名 + 坐标，见 `Core/CardView.cs:121`）：
        //      **费用 = 右上蓝圆 · 近战 = 左下红圆 · 远程 = 左下偏右的紫圆 · 生命 = 右下绿**
        //      **护甲 = 右侧那枚盾牌**（不在任何一个圆里）
        //    ⚠️ 这几张是**抽 43 张开图**时抓到的，不是全量核对 —— 同类错还有多少没人量过。
        foreach (var (name, field, want) in new[]
                 {
                     ("Baneblade Tank", "ranged", 12),       // 图 Astra Militarum/3部队/Warpforge_43_Baneblade-Tank.png：紫圆 12、盾 2
                     ("Haarken Worldclaimer", "ranged", 2),  // 图 Chaos/1督军/Warpforge_3_Haarken-Worldclaimer.png
                     ("Lord Kaphrael", "ranged", 2),         // 图 Emperor_s Children/1督军/Warpforge_01_Lord-Kaphrael.png
                     ("Veldras the Sublime", "ranged", 1),   // 图 Emperor_s Children/3部队/Warpforge_24_Veldras-the-Sublime.png
                     ("Predator Annihilator", "ranged", 7),  // 图 Ultramarines/3部队/predator anihilator.png
                     ("Smothering Decree", "cost", 2),       // 图 Dark Angels/6秘密/IMG_3699.jpg：蓝圆 2
                 })
        {
            var sc = CardDatabase.Find(pool, name);
            CheckTrue(sc != null, $"找得到 {name}");
            if (sc == null) continue;
            int got = field == "ranged" ? sc.RangedAttack : sc.Cost;
            Check(got, want, $"{name} 的 {field} = {want}（卡面实测）");
        }

        // ⑦ 三份 0824 卡表**没收录**的那 9 张（DA 6秘密 / GSC 6破坏卡，手机翻拍没进 OCR 流水线）
        //    —— 稀有度只能对着卡面宝石定：秘密卡橙红=special、破坏卡浅蓝=common。
        foreach (var (name, want) in new[]
                 {
                     ("Convoke the Circle", "special"), ("None Must Know", "special"),
                     ("Obscure Ritual", "special"), ("Rites of Penance", "special"),
                     ("Smothering Decree", "special"),
                     ("Jammed Communications", "common"), ("Poisoned Supplies", "common"),
                     ("Improvised Barricade", "common"), ("Cult Propaganda", "common"),
                 })
        {
            var uc = CardDatabase.Find(pool, name);
            CheckTrue(uc != null, $"找得到 {name}（表外卡）");
            if (uc != null) Check(uc.Rarity, want, $"{name} 稀有度 = {want}（卡面宝石实测）");
        }
    }

    /// <summary>
    /// **战术卡文本解析的覆盖率** —— 这一轮的进度条（`资料/战术卡效果_移植方案.md`）。
    ///
    /// 448 张战术卡的效果文本是英文自然语言，解析器（`Core/EffectText.cs`）照
    /// `rule_core.gd:_resolve_text` 的 handler 顺序一条条试。每加一个 handler，
    /// **「完全解析 N/448」这个数就该往上走** —— 这就是「还差多少」的量化口径。
    ///
    /// ⚠️ 报的是**两个**口径，别混：
    ///   · **完全解析** —— 整条 desc 每一句都认了（这才叫「这张卡能打」）；
    ///   · **部分解析** —— 有话认了、有话没认（**半懂的卡比不懂更危险**，单独报）。
    /// </summary>
    static void TestTacticTextCoverage()
    {
        var pool = CardDatabase.Load();
        // 造牌那条要**真算一遍候选池**（`CreatePool`）—— 池子算不出来就算「没机制」。
        // 卡池只能由调用方给进来（`Core/` 不认识 UnityEngine，读不了 Resources）。
        var cov = EffectText.Coverage(pool, "tactic", pool);

        // ⚠️ 449 而不是 448：2026-09-12 捞回了 `Dark Pact of Fate` —— 它在源表里 `cost: null`
        //    （OCR 没读到），被 `hasStats=false` 挡在卡池外；四条独立证据确认它是真卡
        //    （同 subtype 的三兄弟都在池里且都是 1 费、有中文翻译、效果与规则书 :179 一致）。
        //    收录依据写在 `工具/gen_cards_engine.py` 的 `STATS_EXCEPTIONS` 里。**改这个数要同时改那里。**
        Check(cov.Cards, 449, "战术卡张数");
        // 分类必须**不重不漏**：每一句都恰好落进一个桶（抓计数 bug）
        Check(cov.SegKeyword + cov.SegOk + cov.SegPartial + cov.SegUnknown, cov.SegTotal,
              "分句分类总数 = 分句总数（不重不漏）");
        Debug.Log(P + "   " + cov.Summary());

        // 频次最高的「不认识的句子」= 下一个该实现的 handler（按量排，不按感觉排）
        var top = new List<KeyValuePair<string, int>>(cov.UnknownFreq);
        top.Sort((a, b) => b.Value.CompareTo(a.Value));
        Debug.Log(P + "   最常出现、还没实现的句子（TOP8）：");
        for (int i = 0; i < top.Count && i < 8; i++)
            Debug.Log(P + $"     ×{top[i].Value,-3} {top[i].Key}");

        var topP = new List<KeyValuePair<string, int>>(cov.PartialFreq);
        topP.Sort((a, b) => b.Value.CompareTo(a.Value));
        if (topP.Count > 0)
        {
            Debug.Log(P + "   句型认了、但目标/载荷词表里没有的（TOP5）：");
            for (int i = 0; i < topP.Count && i < 5; i++)
                Debug.Log(P + $"     ×{topP[i].Value,-3} {topP[i].Key}");
        }

        // **第二层：载荷有没有机制** —— 解析得出来但关键词没实现 = 「能打但没用」，是静默失效。
        // 按频次报出来，决定下一步补哪个关键词（`rule_core.gd:52` 的 `KW_IMPLEMENTED` 有 59 个，我们只有 13 个）。
        Debug.Log(P + $"   载荷有机制 {cov.FullAndMechanized}/{cov.Full}（在「完全解析」的卡里再过一层）");
        var topM = new List<KeyValuePair<string, int>>(cov.NoMechFreq);
        topM.Sort((a, b) => b.Value.CompareTo(a.Value));
        for (int i = 0; i < topM.Count && i < 8; i++)
            Debug.Log(P + $"     ×{topM[i].Value,-3} {topM[i].Key}");

        // ---- 「能打」的判据要不要再加一条「动词实现了」？先量出来再决定 ----
        // 现在 `IsFullyParsed` **只看解析**，被三处共用（`CanPlayTactic` / `DeckBuilder.TacticPlayable` /
        // 卡面打 `*`）。于是效果全靠没实现动词的那批卡：不打 `*`、收得进卡组、打得出去、**什么都不发生**。
        // 下面这个数就是「收紧判据会让多少张卡变成不能打」——**决策要数字，不要估计**。
        {
            int allVerbsOk = 0, anyVerbMissing = 0, allVerbsMissing = 0;
            var examples = new List<string>();
            var seen = new HashSet<string>();
            foreach (var c in pool)
            {
                if (c == null || c.Type != "tactic") continue;
                var ops = EffectText.Parse(c.Desc, out var un, out var pa);
                if (un.Count > 0 || pa.Count > 0) continue;         // 只看「完全解析」的
                bool any = false, all = true, missing = false;
                foreach (var op in ops)
                {
                    if (RuleCore.ImplementedEffectVerbs.Contains(op.Verb)) { all = false; any = true; }
                    else { missing = true; }
                }
                if (!missing) allVerbsOk++;
                else { anyVerbMissing++; if (!any) { allVerbsMissing++; if (seen.Add(c.Name)) examples.Add(c.Name); } }
            }
            Debug.Log(P + $"   「能打」判据实测：完全解析的 {cov.Full} 张里，"
                      + $"每条动词都实现了的 **{allVerbsOk}** 张、"
                      + $"有动词没实现的 {anyVerbMissing} 张（其中**整张卡全靠没实现的动词** {allVerbsMissing} 张）");
            if (examples.Count > 0)
                Debug.Log(P + "     整张卡都跑不起来的（严格口径下会变成不能打）："
                          + string.Join("、", examples.ToArray(), 0, System.Math.Min(12, examples.Count)));
            CheckTrue(allVerbsOk > 0, $"有条动词全实现的战术卡（{allVerbsOk} 张）");
        }

        // ---- 按**阵营**看覆盖率：做单个阵营时，这张表就是工作清单 ----
        ReportFactionCoverage(pool);

        // ---- 效果分类表：**这套分类本来只活在代码里**，没有一处写下来过 ----
        // 一张卡的效果 = (动词, 载荷, 目标, 条件, 时机) 的组合。这张表把 449 张按**动词**分堆，
        // 每堆再看载荷/目标/条件。用处有三个：
        //   ① 看「15 个动词」这套分法**对不对**（有些东西混进来了，见下面日志里的告警）
        //   ② 加新效果时知道该往哪一格里放（`资料/卡牌效果管线_计划与交接.md` §七）
        //   ③ 覆盖率的缺口按**格子**看，而不是按句子看
        DumpTaxonomy(pool, cov);

        // 全量落盘 —— TOP8 只够看个热闹，**排优先级要全量**（按频次降序）。
        // 落这里而不是 Assets/：是给人看的排查产物，不是资产。
        DumpUnparsed(cov);

        // 现阶段门槛：骨架已通（有卡能完全解析）、且一张卡都没有解析成空表也不报错。
        // 每补一个 handler 就把这个数往上抬（抬的时候顺手在提交信息里记一笔）。
        CheckTrue(cov.Full >= 120, $"完全解析 {cov.Full}/448（门槛 120，逐步抬高）");
        CheckTrue(cov.SegUnknown > 0, "还有不认识的句子 —— 还没做完，如实报出来");
    }

    // ==================================================================
    //  战术卡「能打」（引擎层：解析 → 结算 → 弃牌堆）
    // ==================================================================

    /// <summary>
    /// 战术卡的**结算链**：`EffectText.Parse` → `RuleCore.PlayTactic` → 状态真的变了 → 进弃牌堆。
    ///
    /// 覆盖三类：合成的卡（可控）、**真卡池里的原版卡**（真数据）、以及**解析不了的卡必须被拒绝**
    /// （不许「扣了费什么都不发生」—— 那是最难查的一类 bug）。
    /// </summary>
    static void TestTacticPlay()
    {
        // ① 伤害类：打掉敌方单位 3 血
        {
            var tac = Tactic("T_Deal", 1, "Deal 3 damage to an enemy");
            var ctx = Battle(new[] { tac, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 3, 3) });
            ToP1Turn(ctx, 1);
            Place(ctx, 1, 0, Unit("Victim", 1, 1, 5));

            Check(EffectText.PickSide(EffectText.Parse(tac.Desc, out _, out _)), "enemy",
                  "「Deal 3 damage to an enemy」要玩家选**敌方**目标");
            int hand0 = ctx.Players[0].Hand.Count;     // ⚠️ 别写死张数：回合开始还会抽 1 张
            int code = RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Deal"), 0);
            Check(code, RuleCodes.OK, "战术卡打出去了");
            Check(Board(ctx, 1, 0).Health, 2, "敌方单位 5 → 2（吃了 3 点）");
            Check(ctx.Players[0].Energy, 1, "扣了 1 能（回合 1 有 2 能）");
            Check(ctx.Players[0].Hand.Count, hand0 - 1, "手牌少了一张");
            Check(ctx.Players[0].Discard.Count, 1, "卡进了弃牌堆");
        }

        // ② 限时增益：`this turn` 的加攻，**回合结束要撤回去**
        {
            var tac = Tactic("T_Buff", 1, "Give +2 attack to a friendly unit this turn");
            var ctx = Battle(new[] { tac, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 0, Unit("Mine", 1, 3, 5));

            var ops = EffectText.Parse(tac.Desc, out _, out _);
            Check(ops.Count, 1, "解析出 1 条效果");
            Check(ops[0].Duration, "turn", "时长 = 本回合");
            Check(EffectText.PickSide(ops), "own", "要选**己方**目标");

            RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Buff"), 0);
            Check(Board(ctx, 0, 0).Attack, 5, "3 攻 → 5 攻（+2）");
            Check(Board(ctx, 0, 0).TempBuffs.Count, 1, "登记了一条限时增益");

            RuleCore.EndTurn(ctx);
            Check(Board(ctx, 0, 0).Attack, 3, "回合结束后 +2 收回去了（3 攻）");
        }

        // ③ **解析不了的卡必须被拒绝**，不许扣费后什么都不发生
        {
            var tac = Tactic("T_Bad", 1, "Frobnicate the whatsit");
            var ctx = Battle(new[] { tac, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            int e0 = ctx.Players[0].Energy;
            int h0 = ctx.Players[0].Hand.Count;
            int code = RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Bad"), -1);
            Check(code, RuleCodes.ErrUnimplemented, "认不出来的战术卡 → 拒绝");
            Check(ctx.Players[0].Energy, e0, "能量**一点没扣**");
            Check(ctx.Players[0].Hand.Count, h0, "牌还在手上");
        }

        // ④ 费用不够 → 拒绝（也不能扣）
        {
            var tac = Tactic("T_Pricey", 9, "Deal 1 damage to an enemy");
            var ctx = Battle(new[] { tac, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            int e0 = ctx.Players[0].Energy;
            Check(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Pricey"), 0), RuleCodes.ErrCost,
                  "9 费的卡在 2 能回合打不出去");
            Check(ctx.Players[0].Energy, e0, "能量没动");
        }

        // ⑤ **真卡池里的原版卡** —— 挑一张「完全解析 + 只打敌方一个单位」的，真打一遍
        {
            var pool = CardDatabase.Load();
            CardDef pick = null;
            List<EffectOp> pickOps = null;
            foreach (var c in pool)
            {
                if (c == null || c.Type != "tactic") continue;
                var ops = EffectText.Parse(c.Desc, out var un, out var pa);
                if (un.Count > 0 || pa.Count > 0) continue;
                bool dealOnly = ops.Count > 0, hasDeal = false;
                foreach (var o in ops)
                    if (o.Verb != "deal") dealOnly = false; else hasDeal = true;
                if (!dealOnly || !hasDeal) continue;
                if (EffectText.PickSide(ops) != "enemy") continue;
                if (ops[0].Amount > 6) continue;                    // 别一下把测试单位打死（上限之外无所谓）
                pick = c; pickOps = ops; break;
            }
            CheckTrue(pick != null, "卡池里找得到「完全解析 + 只打一个敌方单位」的战术卡");
            if (pick != null)
            {
                var ctx = Battle(new[] { pick, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
                ToP1Turn(ctx, 1);
                ctx.Players[0].Energy = pick.Cost;                  // 让它一定付得起
                Place(ctx, 1, 0, Unit("Victim", 1, 1, 30));
                int code = RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, pick.Name), 0);
                Check(code, RuleCodes.OK, $"原版卡「{pick.Name}」打出去了");
                CheckTrue(Board(ctx, 1, 0).Health < 30,
                          $"原版卡「{pick.Name}」真打出了伤害（30 → {Board(ctx, 1, 0).Health}，"
                          + $"op = {pickOps[0]}）");
                Check(ctx.Players[0].Discard.Count, 1, "原版卡也进弃牌堆");
            }
        }

        // ⑥ 目标规则 —— 照原版 `_collect_tactic_targets` 的 pick 分支（`rule_core.gd:4030` 附近）
        {
            // `an enemy` **包含敌方督军**（原版的全体分支只判 `_effect_target_blocked`，不排督军）
            var tac = Tactic("T_Warlord", 1, "Deal 4 damage to an enemy");
            var ctx = Battle(new[] { tac, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            var foeWarlord = ctx.Players[1].Warlord;
            int hp0 = foeWarlord.Health;
            Check(RuleCore.CanPlayCard(ctx, 0, HandIdx(ctx, 0, "T_Warlord"), BoardSpec.WarlordSlot),
                  RuleCodes.OK, "敌方督军格是合法的战术目标（`an enemy` 含督军）");
            RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Warlord"), BoardSpec.WarlordSlot);
            Check(foeWarlord.Health, hp0 - 4, $"敌方督军真的挨了 4 点（{hp0} → {foeWarlord.Health}）");
        }
        {
            // `troop` 类目标**不含督军** —— 规则书：效果目标为 troop 不能影响督军
            var tac = Tactic("T_Troop", 1, "Deal 4 damage to an enemy troop");
            var ctx = Battle(new[] { tac, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            Check(RuleCore.CanPlayCard(ctx, 0, HandIdx(ctx, 0, "T_Troop"), BoardSpec.WarlordSlot),
                  RuleCodes.ErrSlot, "`enemy troop` 不能选敌方督军");
            Place(ctx, 1, 0, Unit("Grunt", 1, 1, 5));
            Check(RuleCore.CanPlayCard(ctx, 0, HandIdx(ctx, 0, "T_Troop"), 0),
                  RuleCodes.OK, "`enemy troop` 能选敌方部队");
        }
        {
            // **隐身的单位不能被敌方效果选中** —— 规则书 Stealth / Camouflage
            var tac = Tactic("T_Stealth", 1, "Deal 4 damage to an enemy troop");
            var ctx = Battle(new[] { tac, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            Place(ctx, 1, 0, Unit("Ghost", 1, 1, 5, "Stealth"));
            Check(RuleCore.CanPlayCard(ctx, 0, HandIdx(ctx, 0, "T_Stealth"), 0),
                  RuleCodes.ErrSlot, "隐身单位不能被敌方战术选中");
        }
    }

    /// <summary>
    /// 2026-09-12 补的三个关键词（战术卡的高频载荷）。
    /// 出处：规则书 :98/:187/:190 与 `rule_core.gd` 的 `_damage_unit:4406` / 部署段 `:2248`。
    /// </summary>
    static void TestFlankInvulnVulnerable()
    {
        var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
        ToP1Turn(ctx, 1);

        // ① 侧翼 / 迅捷：**部署当回合不疲劳**
        //    （规则书 :98「部署当回合不能行动，除非注明，如迅捷/侧翼/狂暴」；:187 侧翼）
        Check(new UnitState(Unit("Plain", 1, 2, 3), false).Exhausted, true,
              "普通单位部署当回合是疲劳的");
        Check(new UnitState(Unit("Flank", 1, 2, 3, "Flank"), false).Exhausted, false,
              "带侧翼的单位部署当回合就能行动");
        Check(new UnitState(Unit("Fast", 1, 2, 3, "Fast"), false).Exhausted, false,
              "带迅捷的也一样（原版 `rule_core.gd:2248` 把两者写在一起）");

        // 走一遍**真部署**：打出去的那张带侧翼，落地就应该能动
        var flankCard = Unit("Flanker", 1, 2, 3, "Flank");
        var ctx2 = Battle(new[] { flankCard, Unit("B", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
        ToP1Turn(ctx2, 1);
        Check(RuleCore.PlayCard(ctx2, 0, HandIdx(ctx2, 0, "Flanker"), 0), RuleCodes.OK,
              "侧翼单位打出去了");
        Check(Board(ctx2, 0, 0).Exhausted, false, "真部署上去的侧翼单位不疲劳");

        // ② 无敌：**伤害完全挡下**（规则书 :190「无法被伤害或摧毁」；原版 `_damage_unit:4414` 返回 0）
        var inv = Place(ctx, 0, 0, Unit("Inv", 1, 1, 3, "Invulnerable"));
        Check(RuleCore.ApplyDamage(ctx, inv, 5, "自检"), 0, "无敌单位受到 0 点伤害");
        Check(inv.Health, 3, "血量一点没掉");

        // ③ 易伤 X：**多加 X 点**（⚠️ 名字容易看反 —— 是加伤，原版 `:4418`）
        var vul = Place(ctx, 0, 1, Unit("Vul", 1, 1, 9, "Vulnerable 2"));
        Check(RuleCore.ApplyDamage(ctx, vul, 3, "自检"), 5, "易伤 2 → 吃 3 点掉 5 点");

        // ④ 易伤与护甲的**先后**：先加伤、再减甲（原版 `_damage_unit` 的函数头写着这个顺序）
        var both = Place(ctx, 0, 2, Unit("Both", 1, 1, 9, "Vulnerable 2", "Armour 1"));
        Check(RuleCore.ApplyDamage(ctx, both, 3, "自检"), 4, "易伤2 + 护甲1 吃 3 点 → 掉 4 点（3+2−1）");
    }

    /// <summary>
    /// **攻击时机上的五个关键词**（2026-09-12 第二批）：星镖 / 爆裂 / 震荡 / 嗜血 / 标记光 / 伪装。
    /// 规则书 :170/:172/:173/:177/:192/:207；顺序照原版 `rule_core.gd` 的攻击段。
    /// </summary>
    static void TestAttackKeywords()
    {
        // ① 星镖 X：**攻击伤害之前**先对目标追加 X 点（规则书 :207）
        {
            var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 0, Unit("Shu", 1, 2, 5, "Shuriken 2"));
            var tgt = Place(ctx, 1, 0, Unit("T", 1, 0, 6));       // 0 攻 → 不反击，算式干净
            Check(RuleCore.DeclareAttack(ctx, 0, 0, 1, 0), RuleCodes.OK, "星镖单位可以攻击");
            Check(tgt.Health, 2, $"星镖2 + 2 攻 = 掉 4 点（6 → {tgt.Health}）");
        }
        // ①b 目标被星镖打死 → **跳过攻击伤害**（原版那支 `target_died`）
        {
            var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 0, Unit("Shu", 1, 2, 5, "Shuriken 5"));
            Place(ctx, 1, 0, Unit("T", 1, 0, 2));
            Check(RuleCore.DeclareAttack(ctx, 0, 0, 1, 0), RuleCodes.OK, "星镖单位攻击（星镖就能打死）");
            Check(Board(ctx, 1, 0), null, "目标死在星镖那一步（攻击伤害跳过，格位空了）");
        }
        // ② 爆裂 X：对目标**相邻的敌方部队**溅射 X 点（规则书 :170）
        {
            var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 0, Unit("Bla", 1, 1, 5, "Blast 2"));
            Place(ctx, 1, 0, Unit("T", 1, 0, 6));
            Place(ctx, 1, 1, Unit("Adj", 1, 0, 6));
            Check(RuleCore.DeclareAttack(ctx, 0, 0, 1, 0), RuleCodes.OK, "爆裂单位可以攻击");
            Check(Board(ctx, 1, 0).Health, 5, "主目标掉 1 点（1 攻）");
            Check(Board(ctx, 1, 1).Health, 4, "相邻的掉 2 点（Blast 2 溅射）");
        }
        // ③ 震荡：**被本单位攻击的单位获得眩晕**（规则书 :177）
        {
            var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 0, Unit("Con", 1, 1, 5, "Concussion"));
            var tgt = Place(ctx, 1, 0, Unit("T", 1, 0, 6));
            Check(tgt.IsStunned, false, "打之前没晕");
            RuleCore.DeclareAttack(ctx, 0, 0, 1, 0);
            Check(tgt.IsStunned, true, "挨了震荡单位一下就晕了");
        }
        // ④ 嗜血：一回合能攻击**两次**（规则书 :172）—— 关键在「达到配额上限才疲劳」
        {
            var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            var atk = Place(ctx, 0, 0, Unit("BT", 1, 1, 9, "Blood Thirst"));
            Place(ctx, 1, 0, Unit("T1", 1, 0, 9));
            Place(ctx, 1, 1, Unit("T2", 1, 0, 9));
            Check(RuleCore.DeclareAttack(ctx, 0, 0, 1, 0), RuleCodes.OK, "嗜血单位第 1 次攻击");
            Check(atk.Exhausted, false, "打完第 1 次**不疲劳**（配额还没满）");
            Check(RuleCore.DeclareAttack(ctx, 0, 0, 1, 1), RuleCodes.OK, "嗜血单位第 2 次攻击");
            Check(atk.Exhausted, true, "打完第 2 次就疲劳了");
            Check(RuleCore.DeclareAttack(ctx, 0, 0, 1, 0), RuleCodes.ErrExhausted, "第 3 次被拒");
        }
        // ④b 没有嗜血的单位打完一次就疲劳（旧行为没被改坏）
        {
            var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            var atk = Place(ctx, 0, 0, Unit("Plain", 1, 1, 9));
            Place(ctx, 1, 0, Unit("T1", 1, 0, 9));
            Check(RuleCore.DeclareAttack(ctx, 0, 0, 1, 0), RuleCodes.OK, "普通单位攻击");
            Check(atk.Exhausted, true, "普通单位打完就疲劳");
        }
        // ⑤ 标记光 X：**远程**伤害 +X，受远程伤害后标记光全部移除（规则书 :192）
        {
            var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 0, Ranged("Shooter", 1, 1, 9, 3));
            var tgt = Place(ctx, 1, 0, Unit("Marked", 1, 0, 9, "Markerlight 2"));
            Check(RuleCore.DeclareAttack(ctx, 0, 0, 1, 0, ranged: true), RuleCodes.OK, "远程攻击");
            Check(tgt.Health, 4, $"远程 3 攻 + 标记光 2 = 掉 5 点（9 → {tgt.Health}）");
            Check(tgt.Has("markerlight"), false, "挨过远程伤害后标记光移除");
        }
        // ⑥ 伪装：**攻击后失去**（规则书 :173「攻击前不能被敌方战术/效果选中」）
        {
            var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            var atk = Place(ctx, 0, 0, Unit("Cam", 1, 1, 9, "Camouflage"));
            Place(ctx, 1, 0, Unit("T", 1, 0, 9));
            Check(atk.Has("camouflage"), true, "开打前有伪装");
            RuleCore.DeclareAttack(ctx, 0, 0, 1, 0);
            Check(atk.Has("camouflage"), false, "攻击之后伪装没了");
        }
    }

    // ==================================================================
    //  关键词机制（2026-09-12 第三批：战场事件系）
    // ==================================================================

    /// <summary>
    /// 猎杀标记 / 黑暗契约 / 兽群 / 哨戒 / 狙击 / 再生 / 压制 / 失明。
    ///
    /// 出处逐条写在 `CardDef.Implemented` 的每一条 doc 里（规则书 :189/:179/:195/:205/:209/:201/:194/:166
    /// 对原版 `rule_core.gd:4562/:1663/:4172/:4280/:4312/:2025/:4209/:4212`）。
    /// **每条都要验「真的改变了局面」，不是「解析器认得这个词」** —— 那正是这一批要解决的问题。
    /// </summary>
    static void TestBattleKeywords()
    {
        // ① 猎杀标记：带标记的敌方部队被摧毁 → 敌方督军挨 X 伤、**击杀者的督军**回 X 血
        //    （规则书 :189；原版 `rule_core.gd:4562`）
        {
            var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 0, Unit("Hunter", 1, 3, 5));
            var prey = Place(ctx, 1, 0, Unit("Prey", 1, 0, 2, "Hunt Mark 2"));
            Check(prey.KwValue("huntmark"), 2, "猎物身上有 2 层标记");

            int foeW0 = ctx.Players[1].Warlord.Health;
            int myW0 = ctx.Players[0].Warlord.Health;
            ctx.Players[0].Warlord.Health = myW0 - 5;       // 先掉 5 血，才看得出「治回来了」
            myW0 = ctx.Players[0].Warlord.Health;

            RuleCore.DeclareAttack(ctx, 0, 0, 1, 0);

            Check(Board(ctx, 1, 0), null, "猎物被摧毁离场");
            Check(ctx.Players[1].Warlord.Health, foeW0 - 2, "敌方督军吃了 2 点（= 标记数）");
            Check(ctx.Players[0].Warlord.Health, myW0 + 2, "我方督军回了 2 点");
        }

        // ② 兽群：场上每有 1 个**友方部队** +1 近战 +1 远程（规则书 :195；原版 `:4172`）
        {
            var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            var beast = Place(ctx, 0, 0, Unit("Beast", 1, 2, 9, "Pack"));
            Check(RuleCore.FieldAttack(ctx, 0, beast, false), 3, "场上 1 个友方部队 → 近战 2+1=3");
            Place(ctx, 0, 1, Unit("Buddy", 1, 1, 5));
            Place(ctx, 0, 2, Unit("Buddy2", 1, 1, 5));
            Check(RuleCore.FieldAttack(ctx, 0, beast, false), 5, "3 个友方部队 → 2+3=5");
            Check(RuleCore.FieldAttack(ctx, 0, beast, true), 3, "远程也 +3（基础 0）");
            Check(ctx.Players[0].Warlord.KwValue("pack"), 0, "督军不参与计数（原版过滤条件）");
            // 督军自己带 Pack 时，数的还是**非督军**的友方部队
            Check(RuleCore.FieldAttack(ctx, 0, ctx.Players[0].Warlord, false),
                  ctx.Players[0].Warlord.Attack, "督军不带 Pack 时不加成");
        }

        // ③ 哨戒 X：被攻击时对**攻击者**先造成 X 伤害，「然后照常结算攻击」（规则书 :205；原版 `:4280`）
        {
            var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            var atk = Place(ctx, 0, 0, Unit("Raider", 1, 3, 10));
            var turret = Place(ctx, 1, 0, Unit("Turret", 1, 0, 9, "Sentry 4"));

            RuleCore.DeclareAttack(ctx, 0, 0, 1, 0);

            Check(atk.Health, 6, "攻击者先挨了 4 点哨戒伤害（10 → 6）");
            Check(turret.Health, 6, "但攻击**照常结算** —— 炮台照样吃 3 点（9 → 6）");
        }

        // ④ 狙击：**远程**攻击会摧毁目标 → **不承受反击**（规则书 :209；原版 `:4312`）
        //    ⚠️ 原版有一条修正：「此前『目标死则不反击』= 近战击杀也免反（规则偏差）+ Sniper 成死代码」
        //       —— 所以这条必须验**近战击杀仍然吃反击**，否则就把那个 bug 又做回来了
        {
            var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            var sniper = Place(ctx, 0, 0, Ranged("Sniper", 1, 0, 10, 5, "Sniper"));
            Place(ctx, 1, 0, Unit("Victim", 1, 9, 3));

            Check(RuleCore.DeclareAttack(ctx, 0, 0, 1, 0, ranged: true), RuleCodes.OK, "远程攻击打出去");
            Check(sniper.Health, 10, "Sniper 远程击杀 → **一点反击都没吃**（9 攻的反击被免了）");

            // 对照组：同样 5 伤打死，但**没有 Sniper** → 照样吃 9 点反击
            var ctx2 = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx2, 1);
            var plain = Place(ctx2, 0, 0, Ranged("Plain", 1, 0, 10, 5));
            Place(ctx2, 1, 0, Unit("Victim", 1, 9, 3));
            RuleCore.DeclareAttack(ctx2, 0, 0, 1, 0, ranged: true);
            Check(plain.Health, 1, "没有 Sniper 的远程攻击者照样吃满反击（10 → 1）");

            // 对照组 2：**近战击杀也不免反击**（原版修掉的那个偏差）
            var ctx3 = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx3, 1);
            var melee = Place(ctx3, 0, 0, Unit("Bruiser", 1, 8, 10));
            Place(ctx3, 1, 0, Unit("Victim", 1, 3, 3));
            RuleCore.DeclareAttack(ctx3, 0, 0, 1, 0);
            Check(melee.Health, 7, "近战击杀**仍然吃反击**（10 → 7）");
        }

        // ⑤ 再生 X：每回合结束时治疗 X（规则书 :201；原版 `:2025`）—— **双方单位都治**
        {
            var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            var mine = Place(ctx, 0, 0, Unit("Regen", 1, 1, 8, "Regeneration 2"));
            var theirs = Place(ctx, 1, 0, Unit("Regen2", 1, 1, 8, "Regeneration 3"));
            mine.Health = 4;
            theirs.Health = 2;

            RuleCore.EndTurn(ctx);

            Check(mine.Health, 6, "我方单位回合结束回 2（4 → 6）");
            Check(theirs.Health, 5, "**对方**单位也回（2 → 5）—— 规则书写的是 each turn");
            // 不能超过上限
            mine.Health = 8;
            RuleCore.BeginTurn(ctx);
            RuleCore.EndTurn(ctx);
            Check(mine.Health, 8, "回血不越过上限");
        }

        // ⑥ 压制：**只禁近战**（规则书 :194；原版 `:4209`），远程照常
        {
            var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 0, Ranged("Pinned", 1, 3, 9, 3, "Pindown"));
            Place(ctx, 1, 0, Unit("T", 1, 0, 9));
            CheckCode(RuleCore.DeclareAttack(ctx, 0, 0, 1, 0, ranged: false),
                      RuleCodes.ErrPindown, "被压制的单位不能近战");
            CheckCode(RuleCore.DeclareAttack(ctx, 0, 0, 1, 0, ranged: true),
                      RuleCodes.OK, "但远程照常打得出去");
        }

        // ⑦ 失明：**远程攻击力视为 0**（规则书 :166；原版 `:4212`），到下一回合结束恢复
        {
            // ⚠️ 用 `an enemy **troop**` 而不是 `an enemy`：后者按原版语义**包含督军**，
            //    「随机一个」在两个候选里掷哪一个是合法的 —— 那样测的就是掷骰不是失明了。
            //    这条测试只想要一个确定的靶子。
            var tac = Tactic("T_Blind", 1, "Blind a random enemy troop");
            var ctx2 = Battle(new[] { tac, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx2, 1);
            var victim = Place(ctx2, 1, 0, Ranged("Shooter", 1, 2, 9, 4));
            Check(RuleCore.PlayTactic(ctx2, 0, HandIdx(ctx2, 0, "T_Blind"), 0), RuleCodes.OK,
                  "「Blind a random enemy troop」打出去了");
            CheckTrue(ReferenceEquals(victim, ctx2.LastTarget), "打中的就是那个部队（不是督军）");
            Check(victim.IsBlind, true, "目标失明了");
            Check(RuleCore.FieldAttack(ctx2, 1, victim, true), 0, "远程攻击力算成 0");
            Check(RuleCore.FieldAttack(ctx2, 1, victim, false), 2, "近战不受失明影响");

            // 到期点：**施放者的下个回合开始时**（和「直到你的下个回合」的限时增益同一个口径）
            Check(victim.BlindTurnEnd, ctx2.Turn + 2, "到期点 = 我的下个回合（当前 + 2：先过对手，再到我）");
            RuleCore.EndTurn(ctx2);                 // P1 的回合结束
            RuleCore.BeginTurn(ctx2);               // ← P2 的回合开始：**这时候必须还瞎着**
            Check(ctx2.Active, 1, "轮到 P2");
            Check(victim.IsBlind, true, "**对手的整个回合里一直失明**（这才是这张牌的用处）");
            RuleCore.EndTurn(ctx2);                 // P2 的回合结束
            RuleCore.BeginTurn(ctx2);               // ← 又是 P1 的回合：到期
            Check(victim.IsBlind, false, "回到施放者的下个回合时恢复");
            Check(RuleCore.FieldAttack(ctx2, 1, victim, true), 4, "远程攻击力回到 4");
        }

        // ⑧ 黑暗契约：四种契约各自的增益**真的加上去了**（规则书 :179；原版 `DARK_PACT_FX:1655`）
        {
            var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            var u = Place(ctx, 0, 0, Unit("Chosen", 1, 2, 5));
            RuleCore.GrantDarkPact(ctx, 0, u, "excess", "测试");

            Check(u.Has(KeywordTable.DarkPact), true, "身上记着黑暗契约");
            Check(RuleCore.PactOf(u), "excess", "记的是「纵欲」那一份");
            Check(u.Attack, 4, "纵欲：+2 近战（2 → 4）");
            Check(u.RangedAttack, 2, "纵欲：+2 远程（0 → 2）");

            // 命运：+2 生命与伪装
            var v = Place(ctx, 0, 1, Unit("Chosen2", 1, 2, 5));
            RuleCore.GrantDarkPact(ctx, 0, v, "fate", "测试");
            Check(v.MaxHealth, 7, "命运：+2 生命上限（5 → 7）");
            Check(v.Has("camouflage"), true, "命运：还给伪装");

            // 再给一份 → **替换**（原版是赋值不是累加），前一份的增益要跟着走
            RuleCore.GrantDarkPact(ctx, 0, v, "resilience", "测试");
            Check(RuleCore.PactOf(v), "resilience", "契约被替换成「韧性」");
            Check(v.Has("camouflage"), false, "命运给的伪装跟着旧契约一起没了");
            Check(v.Attack, 2, "纵欲/命运本来就没动近战，2 保持不变");
            Check(v.Has("regeneration"), true, "韧性：给了再生");

            // `random` 走种子化随机 —— 同一局面必须永远选到同一个
            var w = Place(ctx, 0, 2, Unit("Chosen3", 1, 2, 5));
            RuleCore.GrantDarkPact(ctx, 0, w, "random", "测试");
            CheckTrue(RuleCore.PactOf(w) != null, "随机契约也落地了（种类：" + RuleCore.PactOf(w) + "）");
        }

        // ⑨ 三选一：`Choose one:` 解析出三个选项，结算时**挑一个**（原版 `_resolve_choose:1159`）
        {
            var tac = Tactic("T_Pick", 1, "Choose one: Draw a card; Heal 1 to your Warlord or Draw 2 cards");
            var ops = EffectText.Parse(tac.Desc, out var un, out var pa);
            Check(un.Count, 0, "三选一没有不认识的句子");
            Check(pa.Count, 0, "三选一没有半懂的句子");
            Check(ops.Count, 1, "产出一条 chooseone");
            Check(ops[0].Verb, "chooseone", "动词是 chooseone");
            Check(ops[0].Amount, 3, "拆出三个选项");
            Check(ops[0].Payload.Split('|').Length, 3, "Payload 里存着三段原文");

            var ctx = Battle(new[] { tac, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            var wl = ctx.Players[0].Warlord;
            wl.Health = wl.MaxHealth - 5;
            int hp0 = wl.Health, hand0 = ctx.Players[0].Hand.Count;
            Check(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Pick"), -1), RuleCodes.OK,
                  "三选一打得出去");
            // 三个选项都会改变局面：抽牌 +1 / 督军回 1 / 抽 2 张 —— 至少有一个发生了
            bool changed = ctx.Players[0].Hand.Count != hand0 || wl.Health != hp0;
            CheckTrue(changed, "选中的那一项**真的结算了**（手牌或血量变了）");
        }

        // ⑩ 条件句：`targetsurvives` / `targethasarmour` / `controlcount` / `noeffect`
        //    —— 这四类原先一律「判不了」→ **整条效果不生效**（静默失效），现在要真的分岔
        {
            // `If the target survives, heal 1-5 to it`：打不死才治疗
            var tac = Tactic("T_Surv", 1, "Deal 2 damage to an enemy troop. If the target survives, heal 3 to it");
            var ctx = Battle(new[] { tac, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            var tough = Place(ctx, 1, 0, Unit("Tough", 1, 0, 9));
            Check(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Surv"), 0), RuleCodes.OK, "带条件句的卡打得出去");
            Check(tough.Health, 9, "9 - 2 = 7，条件成立（没被打死）再回 3 → **封顶在上限 9**");

            // 同一个条件在「被打死了」时不成立 —— 用目标只有 2 血重现
            var ctx2 = Battle(new[] { tac, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx2, 1);
            Place(ctx2, 1, 0, Unit("Frail", 1, 0, 2));
            RuleCore.PlayTactic(ctx2, 0, HandIdx(ctx2, 0, "T_Surv"), 0);
            Check(Board(ctx2, 1, 0), null, "被打死了就离场，治疗那句不生效（条件不成立）");
        }
        {
            // `If it has Armour, deal 8 damage instead` —— **替换**语义
            // ① 有护甲：走 instead 那支的 8 点，**原句 2 点不打**
            var tac = Tactic("T_Arm", 1, "Deal 2 damage to an enemy troop. If it has Armour, deal 8 damage instead");
            var ctx = Battle(new[] { tac, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            var armored = Place(ctx, 1, 0, Unit("Ar", 1, 0, 20, "Armour 2"));
            Check(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Arm"), 0), RuleCodes.OK, "条件伤害卡打得出去");
            // 8 伤 - 护甲 2 = 6（2 点那句被替换掉了，所以不是 20-2-6=12）
            Check(armored.Health, 14, "有护甲 → 只走「instead」那支的 8 点（20 → 14）");

            // ② 没有护甲：原句 2 点照打，instead 那支不打
            var ctx3 = Battle(new[] { tac, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx3, 1);
            var plain = Place(ctx3, 1, 0, Unit("Plain", 1, 0, 20));
            RuleCore.PlayTactic(ctx3, 0, HandIdx(ctx3, 0, "T_Arm"), 0);
            Check(plain.Health, 18, "没护甲 → 原句 2 点照打（20 → 18），8 点那句不打");
        }
        {
            // `If you don't control any, create an Intercessor in your hand`
            // —— 本版没有 `create` 造牌，但**条件必须判得出来**，否则整条卡静默失效。
            //    这里只验条件本身：空场时成立、有部队时不成立。
            var seg = EffectText.ParseSegment("If you don't control any, draw a card");
            Check(seg.Kind == EffectText.SegKind.Ok, true, "「you don't control any」现在解得开");
            Check(seg.Ops[0].ConditionKind, "controlcount", "条件类别 = controlcount");
        }

        // ⑪ `for each` 计数层（规则书 :233；原版 `_resolve_for_each:2524` + `_fe_count:1347`）
        //    **三种写法语义不同**，逐种验 —— 光看「解析得了」测不出数对不对
        {
            // ① 后置型 = 重复 N 遍：盘上 2 个友方部队 → 对敌方各打 1 点（打随机目标，共 2 次）
            var tac = Tactic("T_Each", 1, "Deal 1 damage to an enemy troop for each friendly unit");
            var ctx = Battle(new[] { tac, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            var e1 = Place(ctx, 1, 0, Unit("E1", 1, 0, 9));
            var e2 = Place(ctx, 1, 1, Unit("E2", 1, 0, 9));
            Place(ctx, 0, 0, Unit("MineA", 1, 1, 5));
            Place(ctx, 0, 1, Unit("MineB", 1, 1, 5));
            // **数几个**先单独验一次：2 个友方部队（督军不算 —— 卡面写的是 `unit` 不是 `any`）
            var eachOps = EffectText.Parse(tac.Desc, out _, out _);
            Check(eachOps[0].CountScope, "board", "解析出盘面计数");
            Check(eachOps[0].CountRef, "own|troop|all", "数的是「己方部队」（不含督军）");

            Check(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Each"), 0), RuleCodes.OK, "带 for each 的卡打得出去");
            Check(e1.Health + e2.Health, 16, "2 个友方部队 → 打 2 遍 1 伤（18 - 2 = 16）");
        }
        {
            // ② **计数为 0 就是一次都不结算**（不是「退化成 1 次」）
            var tac = Tactic("T_Each2", 1, "Deal 1 damage to an enemy troop for each friendly unit");
            var ctx = Battle(new[] { tac, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            // 把 P1 的督军血打到 0 是不可能的（那是败北），所以改用「敌方受损单位」这种天然为 0 的计数
            var tac2 = Tactic("T_Each3", 1, "Deal 1 damage to an enemy troop for each damaged enemy");
            var ctx2 = Battle(new[] { tac2, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx2, 1);
            var full = Place(ctx2, 1, 0, Unit("Full", 1, 0, 9));
            RuleCore.PlayTactic(ctx2, 0, HandIdx(ctx2, 0, "T_Each3"), 0);
            Check(full.Health, 9, "「每有一个**受损**敌人」而没人受损 → **一下都不打**（9 血没动）");

            // 同一张卡，先把目标打伤 → 计数 1 → 打 1 遍
            var ctx3 = Battle(new[] { tac2, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx3, 1);
            var hurt = Place(ctx3, 1, 0, Unit("Hurt", 1, 0, 9));
            hurt.Health = 5;
            RuleCore.PlayTactic(ctx3, 0, HandIdx(ctx3, 0, "T_Each3"), 0);
            Check(hurt.Health, 4, "有 1 个受损敌人 → 打 1 遍（5 → 4）");
        }
        {
            // ③ 增量型 = 「基础 + 计数 × 增量」，**不是**重复
            //    `Give +2 Melee Attack to a friendly troop, and an additional +2 Melee Attack
            //     for each Dark Pact on it` —— 一份契约时是 **+4**，不是 +2 再来两遍
            var tac = Tactic("T_Add", 1,
                "Give +2 Melee Attack to a friendly troop, and an additional +2 Melee Attack for each Dark Pact on it");
            var ops = EffectText.Parse(tac.Desc, out var un, out var pa);
            Check(un.Count, 0, "增量型没有不认识的句子");
            Check(pa.Count, 0, "增量型没有半懂的句子");
            Check(ops[0].PerCount, 2, "增量 = 2");
            Check(ops[0].CountScope, "darkpact", "计数对象 = 黑暗契约");
            Check(ops[0].CountRef, "it", "数的是**目标身上**的契约");

            var ctx = Battle(new[] { tac, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            var plain = Place(ctx, 0, 0, Unit("Plain", 1, 2, 9));
            RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Add"), 0);
            Check(plain.Attack, 4, "没有契约 → 基础 +2（2 → 4），不是 +6");

            // 给它一份契约再打一次 → 2 + 2 + 2×1 = 6
            RuleCore.GrantDarkPact(ctx, 0, plain, "excess", "测试");
            int atkAfterPact = plain.Attack;      // 纵欲契约本身 +2 近战
            Check(atkAfterPact, 6, "纵欲契约自己 +2 近战（4 → 6）");
            int pc = RuleCore.CountForTest(ctx, 0, plain, ops[0]);
            Check(pc, 1, "**结算层**从目标身上数出 1 份契约（和解析层的 `it` 对得上）");
            ctx.Players[0].Hand.Add(tac);
            ctx.Players[0].Energy = 9;
            // 「a friendly troop」要选目标；契约会让它变成 6 + (2 + 2×1) = 10
            // ⚠️ 第二张要打**同一个目标**（契约在它身上才数得到 1）—— 传格位 0
            int pcode = RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Add"), 0);
            Check(pcode, RuleCodes.OK, "第二张也打出去了");
            Check(plain.Attack, 10, "有 1 份契约 → 基础 2 + 2×1 = +4（6 → 10）");
            Check(plain.KwValue(KeywordTable.DarkPact), 1, "契约还是 1 份（没被重复结算加爆）");
        }
        {
            // ④ `For each troop drawn` —— 数的是**同一张卡抽到的牌**
            var tac = Tactic("T_Drawn", 1, "Draw 2 cards. For each troop drawn, gain 1 Energy");
            var ctx = Battle(new[] { tac, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            // 牌库里的垫牌是 `filler*`（都是 unit 类型）→ 抽 2 张就是 2 个部队
            int energy0 = ctx.Players[0].Energy;
            Check(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Drawn"), -1), RuleCodes.OK, "抽牌 + for each 的卡打得出去");
            Check(ctx.Players[0].Energy, energy0 - 1 + 2, "抽 2 张部队 → 回 2 能（打这张卡花了 1 能）");
        }
        {
            // ⑤ `For each one that dies` —— 数的是本回合阵亡数
            var tac = Tactic("T_Died", 1, "Deal 3 damage to an enemy troop for each one that dies");
            var ctx = Battle(new[] { tac, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            Place(ctx, 1, 0, Unit("Prey", 1, 0, 1));      // 1 血，3 攻一击必杀
            Check(ctx.DiedThisTurn, 0, "开局没人阵亡");

            Place(ctx, 0, 0, Unit("Killer", 1, 3, 5));
            int kcode = RuleCore.DeclareAttack(ctx, 0, 0, 1, 0);
            Check(kcode, RuleCodes.OK, "杀手打得出去");
            Check(Board(ctx, 1, 0), null, "猎物被摧毁、格位空了");
            Check(ctx.DiedThisTurn, 1, "打死了一个 → 本回合阵亡数 1");

            // 现在打这张卡：计数 1 → 打 1 遍 3 点
            Place(ctx, 1, 1, Unit("Next", 1, 0, 9));
            RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Died"), 1);
            Check(Board(ctx, 1, 1).Health, 6, "阵亡 1 个 → 打 1 遍 3 点（9 → 6）");
        }

        // ⑫ 卡池实测：真原版卡里这几类关键词确实存在（别只在合成卡上验）
        {
            var pool = CardDatabase.Load();
            int n = 0;
            foreach (var c in pool)
                if (c != null && c.Keywords.ContainsKey("huntmark")) n++;
            CheckTrue(n > 0, $"卡池里带猎杀标记的卡有 {n} 张");
        }
    }

    // ==================================================================
    //  造牌（`create`）
    // ==================================================================

    /// <summary>
    /// `Create …` —— 造牌。**规格书是规则书附录 B（生成卡的阵营指南）与附录 C（骰子查找表）**
    /// （`资料/规则书/Warpforge_Offline_Rulebook_1_5-3_中文翻译.md:254-320`）。
    ///
    /// 三层都要验，缺一层就会漏掉一类静默失效：
    ///   ① **解析** —— 张数 / 「造什么」原文 / **目的地**（送错人 = 另一张卡）；
    ///   ② **候选池** —— 按卡池实算，和附录 B 的「几种」、附录 C 的名单逐个对；
    ///   ③ **结算** —— 真打一张原版卡，看卡有没有真进手牌、池子对不对、同种子可复现。
    /// </summary>
    static void TestCreate()
    {
        var pool = CardDatabase.Load();

        // ---- ① 解析：三种写法（全卡池实测 28 个分句 / 20 张卡）----
        {
            var op = OneOp("Create a Termagant in your hand");
            Check(op.Verb, "create", "`Create …` → 动词 create");
            Check(op.Amount, 1, "`a Termagant` → 1 张");
            Check(op.Payload, "termagant", "「造什么」存进 Payload");
            Check(op.Dest, "hand", "`in your hand` → 自己手牌");

            op = OneOp("Create three Ultramarines Vehicles in your hand");
            Check(op.Amount, 3, "`three` → 3 张");
            Check(op.Payload, "ultramarines vehicles", "阵营词和兵种词一起留在 Payload 里");

            // 目的地**前置**的写法（`Drone Companion` 卡面就是这个语序）
            op = OneOp("Create in your hand a Gun Drone, Guardian Drone or Marker Drone");
            Check(op.Amount, 1, "目的地前置时数量照样剥得掉");
            Check(op.Dest, "hand", "目的地前置也认得出来");
            CheckTrue(op.Payload.Contains(" or "), "三选一的名单原样留着（结算时随机挑一个）");

            op = OneOp("Create a random Sabotage in the enemy hand");
            Check(op.Dest, "enemyhand", "`in the enemy hand` → **对手**手牌（送到自己手上就是另一张卡）");
            Check(op.Payload, "random sabotage", "`random` 留在原文里（选法由结算层掷）");

            op = OneOp("Create a copy of it at the top of your deck");
            Check(op.Dest, "decktop", "`at the top of your deck` → 牌库顶");
            Check(op.Payload, "copy of it", "`copy of it` 原样记下来（由结算层取上一条效果的目标）");

            op = OneOp("Create one Neophyte Hybrid in your hand for each enemy unit");
            Check(op.Amount, 1, "`one …` → 1 张");
            CheckTrue(!string.IsNullOrEmpty(op.CountRef), "`for each enemy unit` 被计数层剥进 CountRef");

            // ⚠️ **没写目的地必须判失败** —— 默认成手牌就是把牌送错人，最难查的一类
            var seg = EffectText.ParseSegment("Create a Termagant");
            Check(seg.Kind, EffectText.SegKind.Unknown, "没写目的地 → 判「不认识」，不猜成手牌");
        }

        // ---- ② 候选池：卡池实算 vs 规则书附录 B「几种」/ 附录 C 名单 ----
        // ⚠️ 一律走 **卡面原文 → 解析 → Payload → 算池子** 这条真路，
        //    不手写 payload —— 手写的那份和解析器产出的会悄悄不一样（第一版就是这么错的：
        //    手写成 `a gun drone, …`，而解析器早就把冠词当数量剥掉了）。
        {
            // 附录 B「装甲攻势：3 个极限战士载具（**18 种**，各 3 张，54-216 总计）」
            var r = PoolOf(pool, "Create three Ultramarines Vehicles in your hand", "Ultramarines");
            CheckTrue(r.Ok, "`three Ultramarines Vehicles` 的池子算得出来");
            Check(r.Cards.Count, 18, "Ultramarines 载具 18 张 == 附录 B 的「18 种」");
            CheckAll(r.Cards, c => c.Faction == "Ultramarines" && c.Subtype == "Vehicle",
                     "池子里每一张都是 Ultramarines 载具");

            // 附录 B「狂野宿主：2 个随机载具（**20 种**，各 2 张）」
            r = PoolOf(pool, "Create two random Saim-Hann Vehicles in your hand", "SaimHann");
            Check(r.Cards.Count, 20, "SaimHann 载具 20 张 == 附录 B 的「20 种」（`Saim-Hann` 的连字符要归一化掉）");

            // 附录 B「比你们快：3 个兽人载具（**14 种**）」—— 卡面写**种族名** `Ork`，卡池里叫 `Goff`
            r = PoolOf(pool, "Create three random Ork Vehicles in your hand", "Goff");
            Check(r.Cards.Count, 14, "`Ork` → `Goff`（别名表），14 张 == 附录 B 的「14 种」");

            // 附录 B「虫群大军：3 个虫群部队（10 种）」+ 附录 C 的 1d10 名单
            r = PoolOf(pool, "Create 3 random Leviathan troops with Swarm in your hand", "Leviathan");
            Check(r.Cards.Count, 11, "Leviathan 带虫群的**部队** 11 张（附录 B 写 10，差 1 —— 见下）");
            CheckTrue(!r.Cards.Exists(c => c.Name == "Swarming Masses"),
                      "`Swarming Masses` 是战术卡，不在「troops」池里");

            // 附录 B「锈蚀通风口：手牌生成随机带伏击部队」+ 附录 C 的 1d11 名单
            r = PoolOf(pool, "Create a random troop with Ambush in your hand", "Genestealers");
            Check(r.Cards.Count, 11, "基因窃取者带伏击的部队 11 张 == 附录 C「锈蚀通风口(1d11)」的 11 个名字");

            // `Combat Elixir` / `Sabotage` 在原版数据里是**兵种**（subtype）不是卡名 ——
            // 这一点以前在计划文档里记反了（记成「原版数据里没有这张卡」），见文末更正
            r = PoolOf(pool, "Create 3 random Combat Elixir in your hand", "EmperorsChildren");
            // ⚠️ 2026-09-13：这里原来写 5 张、注释是「附录 C 的 1d6 是 6 个名字，**差 1**」。
            //    那差的一张是 `Shivversplint` —— 它的 subtype 在 OCR 源表里被写成了 `Upgrade`。
            //    **卡面逐张核对**（见 `CARD_FACE_FIXES_SRC`）把它修成 `Combat Elixir` 之后，
            //    附录 C 的 6 个名字**全对上了**。见下面 `Shivversplint` 那条。
            Check(r.Cards.Count, 6, "战斗药剂 6 张 == 附录 C「战斗药剂(1d6)」的 6 个名字（2026-09-13 起全中）");
            r = PoolOf(pool, "Create a random Sabotage in the enemy hand", "Genestealers");
            Check(r.Cards.Count, 2, "破坏 2 张（`Improvised Barricade` / `Poisoned Supplies`）");

            // 三选一名单：三个都要在原版数据里找得到
            r = PoolOf(pool, "Create in your hand a Gun Drone, Guardian Drone or Marker Drone", "TauEmpire");
            Check(r.Cards.Count, 3, "`Gun Drone / Guardian Drone / Marker Drone` 三张都在卡池里");

            // 具名卡：原版数据里撇号被剥掉了（`Abaddon's Chosen` → `Abaddons Chosen`）
            r = PoolOf(pool, "Create a copy of Ahnakh-Yth Shrine in your hand", "SaimHann");
            Check(r.Cards.Count, 1, "`Create a copy of <卡名>` → 池子里就那一张");
            r = PoolOf(pool, "For each troop drawn, create a copy of Abaddon's Chosen in your hand", "BlackLegion");
            CheckTrue(r.Ok, "卡面写 `Abaddon's Chosen`、数据里写 `Abaddons Chosen` —— 归一化后查得到");
            Check(r.Cards[0].Name, "Abaddons Chosen", "查到的就是数据里那张");

            // ⚠️ **查不到的卡必须如实报**，不许造效果（本工程红线）。
            //    而且要把「到底是没有，还是名字写岔了」分清楚 —— 2026-09-12 之前计划文档
            //    把下面这五种统统一句「原版数据里没有」带过，**其中三种是错的**。
            {
                // ① 名字写岔了（数据是 OCR + 手抄来的）：报出来，但**不拿近似的顶替**
                //    走 `PoolOf`（真解析）而不是手写 payload —— 手写会把冠词带进去，见 `PoolOf` 的注释
                var a = PoolOf(pool, "Create a Sergeant Taaman in your hand", "DarkAngels");
                CheckTrue(!a.Ok, "`Sergeant Taaman` 按**这个名字**查不到");
                CheckTrue(a.Why.Contains("Sergeant Naaman"),
                          "……但如实报出最接近的是 `Sergeant Naaman`（实体卡名 vs 数据名的抄写差）");

                var b = PoolOf(pool, "Create an Extermination Protocol in your hand", "Sautekh");
                CheckTrue(!b.Ok && b.Why.Contains("Extermination Protocols"),
                          "`Extermination Protocol` 同理，最接近的是 `Extermination Protocols`（多个 s）");

                // ② 真的没有：连近似的都找不到（实体卡表 `卡牌信息权威表_0824.md` 里也没有）
                foreach (string missing in new[] { "Vindicare Assassin", "Vitric Consul" })
                {
                    var mr = PoolOf(pool, "Create a " + missing + " in your hand", "Ultramarines");
                    CheckTrue(!mr.Ok && mr.Why.Contains("原版数据里没有这张卡"),
                              $"`{missing}` 原版数据里没有 —— 如实报，不造效果");
                    CheckTrue(CreatePool.FindNearMiss(pool, missing) == null,
                              $"……`{missing}` 连近似的名字都没有（是真缺，不是抄错）");
                }
            }

            // 没有卡池 → 如实报，**不退化**成从牌库里抽
            var noPool = CreatePool.Resolve(null, "a termagant", "Leviathan");
            CheckTrue(!noPool.Ok && noPool.Why.Contains("卡池"), "不给卡池 → 明说「没有卡池」");
        }

        // ---- ③ 附录 C 的骰子表 vs 卡池实算 —— 逐个名字对，差一个都要报出来 ----
        CheckDiceTables(pool);

        // ---- ④ 结算：真打原版卡 ----
        {
            // `Mercurial Host`（帝皇之子 / 4 费）：`Create 3 random Combat Elixir in your hand`
            var mercurial = CardDatabase.Find(pool, "Mercurial Host");
            CheckTrue(mercurial != null, "卡池里有 `Mercurial Host`");

            var ctx = BattlePool(new[] { mercurial }, new[] { Unit("X", 1, 1, 5) }, pool,
                                 warlordFaction: "EmperorsChildren");
            ToP1Turn(ctx, 3);                       // 4 费，攒够能量
            int before = ctx.Players[0].Hand.Count;
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "Mercurial Host"), -1), RuleCodes.OK,
                      "`Mercurial Host` 打得出去");
            Check(ctx.Players[0].Hand.Count, before - 1 + 3, "手牌 −1（打出去的）+3（造出来的）");
            int elixirs = 0;
            foreach (var c in ctx.Players[0].Hand)
                if (c.Subtype == "Combat Elixir" || c.Subtype == "Elixir") elixirs++;
            Check(elixirs, 3, "造出来的 3 张都是战斗药剂");
            CheckTrue(ctx.Events.Exists(e => e.Contains("造了 3 张")), "日志里说了造了 3 张");

            // **同种子必须造出同样的牌**（对局可复现是硬要求）
            var ctx2 = BattlePool(new[] { mercurial }, new[] { Unit("X", 1, 1, 5) }, pool,
                                  warlordFaction: "EmperorsChildren");
            ToP1Turn(ctx2, 3);
            RuleCore.PlayTactic(ctx2, 0, HandIdx(ctx2, 0, "Mercurial Host"), -1);
            var a = ctx.Players[0].Hand; var b = ctx2.Players[0].Hand;
            bool same = a.Count == b.Count;
            for (int i = 0; same && i < a.Count; i++) same = a[i].Name == b[i].Name;
            CheckTrue(same, "同一个种子造出同样的牌（走 ctx.Rng，不是 UnityEngine.Random）");

            // `Create … in the enemy hand`：牌进的是**对手**手里
            // 用合成卡而不是 `Underground Network`：后者第二句（`For the rest of this battle,
            // Sabotage cards … cost 1 more`）还没实现，整张卡会被 `CanPlayTactic` 拒掉 —— 那条路另外验。
            var foeHand = Tactic("T_FoeCreate", 1, "Create a random Sabotage in the enemy hand");
            var ctx3 = BattlePool(new[] { foeHand }, new[] { Unit("X", 1, 1, 5) }, pool,
                                  warlordFaction: "Genestealers");
            ToP1Turn(ctx3, 1);                      // 1 费
            int foeBefore = ctx3.Players[1].Hand.Count;
            int ownBefore = ctx3.Players[0].Hand.Count;
            CheckCode(RuleCore.PlayTactic(ctx3, 0, HandIdx(ctx3, 0, "T_FoeCreate"), -1),
                      RuleCodes.OK, "`Create … in the enemy hand` 打得出去");
            Check(ctx3.Players[1].Hand.Count, foeBefore + 1, "造出来的破坏牌进了**对手**手牌");
            Check(ctx3.Players[0].Hand.Count, ownBefore - 1, "自己手上一张都没多（只少了打出去的那张）");
            Check(ctx3.Players[1].Hand[ctx3.Players[1].Hand.Count - 1].Subtype, "Sabotage",
                  "送过去的那张确实是破坏");

            // `Create a copy of it at the top of your deck`：`it` = 上一条效果的目标
            var vengeful = CardDatabase.Find(pool, "Vengeful Brethren");
            var troop = Unit("CopyMe", 1, 1, 3);
            var ctx4 = BattlePool(new[] { vengeful, troop }, new[] { Unit("X", 1, 3, 3) }, pool,
                                  warlordFaction: "DarkAngels");
            ToP1Turn(ctx4, 1);                      // 1 费
            Place(ctx4, 0, 0, troop);
            int deckBefore = ctx4.Players[0].Deck.Count;
            CheckCode(RuleCore.PlayTactic(ctx4, 0, HandIdx(ctx4, 0, "Vengeful Brethren"), 0), RuleCodes.OK,
                      "`Vengeful Brethren` 打得出去（选了自己那个部队当目标）");
            Check(ctx4.Players[0].Deck.Count, deckBefore + 1, "复制出来的那张进了自己牌库");
            Check(ctx4.Players[0].Deck[ctx4.Players[0].Deck.Count - 1].Name, "CopyMe",
                  "牌库**顶**（末尾，抽牌从末尾抽）那张就是被复制的那张卡");

            // **没有卡池的对局**：造牌必须如实报「没生效」，不许静默什么都不做
            var noPoolCtx = Battle(new[] { mercurial }, new[] { Unit("X", 1, 1, 5) });
            ToP1Turn(noPoolCtx, 3);
            int h = noPoolCtx.Players[0].Hand.Count;
            RuleCore.PlayTactic(noPoolCtx, 0, HandIdx(noPoolCtx, 0, "Mercurial Host"), -1);
            Check(noPoolCtx.Players[0].Hand.Count, h - 1, "没卡池 → 一张都没造出来");
            CheckTrue(noPoolCtx.Events.Exists(e => e.Contains("没有卡池")),
                      "没卡池 → 日志里明说原因（不静默失败）");
        }
    }

    /// <summary>
    /// **两个阵营打起来顺不顺**（用户 2026-09-12 指定：先做极限战士 + 兽人，然后检查对战）。
    ///
    /// 做法：**极限战士 vs 兽人**各凑一副真卡组，跑若干局完整对局（双方都走 `SimpleAI`，
    /// 而它 2026-09-12 起**会出战术卡了**），然后把整局里所有
    /// 「没生效 / 没实现 / 判不了 / 数不出来」的话**去重收上来**。
    ///
    /// 为什么不能只看「跑完了没崩」：**跑得完不等于跑得对**。
    /// 一张效果没结算的卡照样能让对局正常结束，只在日志里留一行 ——
    /// 这一节就是把那些行拎出来，让「不顺利」有个数字。
    /// </summary>
    static void TestFactionBattle()
    {
        var pool = CardDatabase.Load();
        if (pool.Count == 0) { CheckTrue(false, "卡表没加载上"); return; }

        const int Games = 6;
        int finished = 0, p1 = 0, p2 = 0, draw = 0, turns = 0, tacticsPlayed = 0;
        int eventsScanned = 0;
        int chooses = 0;                  // 选牌**成功**结算的次数（见下面扫日志那段）
        var warns = new Dictionary<string, int>();
        var warnExample = new Dictionary<string, string>();

        for (int g = 0; g < Games; g++)
        {
            // ⚠️ **先手方要换着来**：固定让极限战士先手的话，赢的那方是「先手」还是「阵营强」分不清
            //    （第一版 6:0，看不出名堂）。偶数局极限战士先手、奇数局兽人先手。
            bool umFirst = (g % 2 == 0);
            var dUM = DeckBuilder.StarterDeck(pool, "Ultramarines", DeckBuilder.ClassicDeckSize,
                                              new System.Random(100 + g), unitsOnly: false);
            var dGK = DeckBuilder.StarterDeck(pool, "Goff", DeckBuilder.ClassicDeckSize,
                                              new System.Random(200 + g), unitsOnly: false);
            var d0 = umFirst ? dUM : dGK;
            var d1 = umFirst ? dGK : dUM;
            // ⚠️ **必须传卡池** —— `create` / `deploy` 从全卡池筛候选；不传的话那一族会
            //    「如实报没有卡池然后什么都不做」（引擎的设计如此），这一节就测了个寂寞。
            var ctx = RuleCore.NewBattle(d0, d1, seed: 7000 + g, cardPool: pool);

            int guard = 0;
            while (!ctx.IsOver && guard++ < 300)
            {
                RuleCore.BeginTurn(ctx);
                PlayAiTurn(ctx, ref tacticsPlayed);
                if (ctx.IsOver) break;
                RuleCore.EndTurn(ctx);
            }
            if (ctx.IsOver) finished++;
            turns += ctx.Turn;
            // 按**阵营**记胜负（不是按座位）—— 座位是轮流先手的
            if (ctx.Winner == 3) draw++;
            else if (ctx.Winner != 0)
            {
                string winnerFac = (ctx.Winner - 1 == 0) == umFirst ? "Ultramarines" : "Goff";
                if (winnerFac == "Ultramarines") p1++; else p2++;
            }

            eventsScanned += ctx.Events.Count;
            foreach (string e in ctx.Events)
            {
                // 选牌**成功**结算的次数 —— 成功不打「没生效」，所以得单独数一条
                // （失败的那条走下面 `没生效` 的通道，两者合起来才是这一族的全貌）
                if (e.IndexOf("选了「", System.StringComparison.Ordinal) >= 0) chooses++;

                if (e.IndexOf("没生效", System.StringComparison.Ordinal) < 0
                    && e.IndexOf("没实现", System.StringComparison.Ordinal) < 0
                    && e.IndexOf("没结算", System.StringComparison.Ordinal) < 0
                    && e.IndexOf("判不了", System.StringComparison.Ordinal) < 0
                    && e.IndexOf("数不出来", System.StringComparison.Ordinal) < 0) continue;
                // 归一化：把「N 条」「具体卡名」摘掉，同类只留一条，好看清有几个**种类**
                string key = e;
                int w = key.IndexOf("有 ", System.StringComparison.Ordinal);
                if (w >= 0 && key.IndexOf(" 条效果本版没结算", System.StringComparison.Ordinal) > w)
                    key = key.Substring(0, w) + "有 N 条效果本版没结算：" + Tail(key);
                int n0;
                warns[key] = warns.TryGetValue(key, out n0) ? n0 + 1 : 1;
                if (!warnExample.ContainsKey(key)) warnExample[key] = e;
            }
        }

        Debug.Log(P + $"   两个阵营打了 {Games} 局：完成 {finished} · P1(极限战士) 胜 {p1} · "
                  + $"P2(兽人) 胜 {p2} · 平 {draw} · 平均 {turns / Games} 回合 · "
                  + $"**双方共打出战术卡 {tacticsPlayed} 张** · 其中选牌结算成功 {chooses} 次");

        Check(finished, Games, $"{Games} 局全部打出结果（没有卡死的）");
        CheckTrue(tacticsPlayed > 0, $"战术卡真的被打出来了（{tacticsPlayed} 张）—— AI 不再是只出单位卡");
        CheckTrue(tacticsPlayed >= Games, $"……而且每局平均不止一张（{tacticsPlayed}/{Games}）");

        // 选牌这一族**必须在实战里真跑到** —— 单元自检里跑通 ≠ 真打起来会走到
        // （第十六轮就是这么发现「战术卡用例全绿、真打起来却从来没碰过」的，见
        //  `资料/阵营推进_清单与交接.md` §六）。种子里程碑：极限战士在牌组里有 5 张选牌卡。
        CheckTrue(chooses > 0, $"选牌（`Choose a …`）在实战里真的结算成功过（{chooses} 次）");

        // 把「不顺利」按种类列出来
        var list = new List<KeyValuePair<string, int>>(warns);
        list.Sort((a, b) => b.Value.CompareTo(a.Value));
        Debug.Log(P + $"   对局里出现的「没结算/判不了」共 {list.Count} 种"
                  + $"（扫了 {eventsScanned} 条事件日志）");
        int shown = 0;
        foreach (var kv in list)
        {
            if (shown++ >= 12) { Debug.Log(P + $"     ……还有 {list.Count - 12} 种"); break; }
            Debug.Log(P + $"     ×{kv.Value,-3} {kv.Key}");
        }

        // ⚠️ **「动词没实现」必须是 0** —— 四个动词现在都实现了。
        //    这一条是**回退报警**：以后谁加了新动词却忘了登记，这里立刻红。
        int unimplemented = 0;
        foreach (var kv in list)
            if (kv.Key.IndexOf("的动作「", System.StringComparison.Ordinal) >= 0) unimplemented += kv.Value;
        Check(unimplemented, 0, "整局里**没有**「动词本版没实现」—— 登记的动词表是全的");
    }

    /// <summary>取一段日志里 `：` 之后的内容（归一化警告用的）</summary>
    static string Tail(string e)
    {
        int c = e.IndexOf('：');
        return c >= 0 && c + 1 < e.Length ? e.Substring(c + 1) : "";
    }

    /// <summary>跑完一方的 AI 回合：出牌（含战术卡）→ 攻击。判据全走 `SimpleAI` / 引擎。</summary>
    static void PlayAiTurn(BattleContext ctx, ref int tacticsPlayed)
    {
        int p = ctx.Active;
        for (int guard = 0; guard < 30 && !ctx.IsOver; guard++)
        {
            int ci, sl;
            if (!SimpleAI.NextPlay(ctx, out ci, out sl)) break;
            var card = ctx.Players[p].Hand[ci];
            if (RuleCore.PlayCard(ctx, p, ci, sl) != RuleCodes.OK) break;
            if (!card.IsUnit) tacticsPlayed++;
        }

        for (int guard = 0; guard < 40 && !ctx.IsOver; guard++)
        {
            int a, tp, ts;
            bool ranged;
            if (!SimpleAI.NextAttack(ctx, out a, out tp, out ts, out ranged)) break;
            if (RuleCore.DeclareAttack(ctx, p, a, tp, ts, ranged) != RuleCodes.OK) break;
        }
    }

    /// <summary>
    /// **阵营机制与最后一个动词**：`repeat`（重放）+ `Oath N:`（付费触发）+ `Codex:`（能量为 0 时触发）。
    ///
    /// 三个都有**规则书明文**，不是从卡面猜的：
    ///   · `repeat`  —— `rule_core.gd:2542` → `_resolve_repeat`「把本句之前的效果再来一遍」
    ///   · `Oath X`  —— 规则书 :194「部署时支付 X 能量以触发效果」
    ///   · `Codex`   —— 规则书 :175「你的能量为 0 时触发效果」
    /// </summary>
    static void TestFactionMechanics()
    {
        // ---- `repeat`：**重放的是「本句之前」的效果** ----
        {
            var tac = Tactic("T_Repeat", 1, "Deal 2 damage to an enemy. Repeat this effect");
            var ctx = BattlePool(new[] { tac }, new[] { Unit("X", 1, 2, 5) }, CardDatabase.Load());
            ToP1Turn(ctx, 1);
            Place(ctx, 1, 0, Unit("Victim", 1, 1, 9));
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Repeat"), 0), RuleCodes.OK,
                      "`Deal 2 damage. Repeat this effect` 打得出去");
            Check(Board(ctx, 1, 0).Health, 5, "9 血挨了**两遍** 2 点（9 → 5）：repeat 真的重放了前面那条");
            CheckTrue(ctx.Events.Exists(e => e.Contains("重复一遍")), "日志里说了在重复");

            // `repeat` 前面没有效果 → **如实报**，不许静默什么都不做
            var lone = Tactic("T_RepeatLone", 1, "Repeat this effect");
            var c2 = BattlePool(new[] { lone }, new[] { Unit("X", 1, 1, 5) }, CardDatabase.Load());
            ToP1Turn(c2, 1);
            var ops2 = EffectText.Parse("Repeat this effect", out _, out _);
            CheckTrue(ops2.Count == 1 && ops2[0].RepeatOps != null && ops2[0].RepeatOps.Count == 0,
                      "孤零零一句 `Repeat this effect` → RepeatOps 是空表（不是 null）");
        }

        // ---- `Oath N:`：**付得起才触发**，付不起整条不生效 ----
        {
            var tac = Tactic("T_Oath", 1, "Draw a card. Oath 3: Draw 2 cards");
            var pool = CardDatabase.Load();

            // 第 1 回合 2 能：付 1 打牌 → 剩 1 < 3 → Oath 不触发
            var ctx = BattlePool(new[] { tac }, new[] { Unit("X", 1, 1, 9) }, pool);
            ToP1Turn(ctx, 1);
            int h0 = ctx.Players[0].Hand.Count;
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Oath"), -1), RuleCodes.OK, "Oath 卡打得出去");
            Check(ctx.Players[0].Hand.Count, h0 - 1 + 1, "能量不够付 Oath → 只结算基础的那 1 张");

            // 第 3 回合 4 能：付 1 打牌 → 剩 3 ≥ 3 → Oath 触发
            var ctx2 = BattlePool(new[] { tac }, new[] { Unit("X", 1, 1, 9) }, pool);
            ToP1Turn(ctx2, 3);
            int h1 = ctx2.Players[0].Hand.Count;
            int e1 = ctx2.Players[0].Energy;
            CheckCode(RuleCore.PlayTactic(ctx2, 0, HandIdx(ctx2, 0, "T_Oath"), -1), RuleCodes.OK, "打得出去");
            Check(ctx2.Players[0].Hand.Count, h1 - 1 + 3, "付得起 → 1 张基础 + 2 张 Oath");
            Check(ctx2.Players[0].Energy, e1 - 1 - 3, $"付了卡费 1 + Oath 3（{e1} → {e1 - 4}）");
        }

        // ---- `Codex:`：**能量为 0 时才触发** ----
        {
            var tac = Tactic("T_Codex", 2, "Draw a card. Codex: Draw 2 cards");
            var pool = CardDatabase.Load();

            // 第 1 回合 2 能：正好花光 → Codex 触发
            var ctx = BattlePool(new[] { tac }, new[] { Unit("X", 1, 1, 9) }, pool);
            ToP1Turn(ctx, 1);
            int h0 = ctx.Players[0].Hand.Count;
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Codex"), -1), RuleCodes.OK, "Codex 卡打得出去");
            Check(ctx.Players[0].Energy, 0, "正好花光能量");
            Check(ctx.Players[0].Hand.Count, h0 - 1 + 3, "能量为 0 → Codex 触发（1 + 2 张）");

            // 第 3 回合 4 能：花 2 还剩 2 → Codex 不触发
            var ctx2 = BattlePool(new[] { tac }, new[] { Unit("X", 1, 1, 9) }, pool);
            ToP1Turn(ctx2, 3);
            int h1 = ctx2.Players[0].Hand.Count;
            CheckCode(RuleCore.PlayTactic(ctx2, 0, HandIdx(ctx2, 0, "T_Codex"), -1), RuleCodes.OK, "打得出去");
            Check(ctx2.Players[0].Energy, 2, "还剩 2 能");
            Check(ctx2.Players[0].Hand.Count, h1 - 1 + 1, "能量不为 0 → Codex **不**触发（只有 1 张）");
        }

        // ---- 真卡面：这三个机制在两个阵营的战术卡里确实用到了 ----
        {
            var pool = CardDatabase.Load();
            int oath = 0, codex = 0, rep = 0;
            foreach (var c in pool)
            {
                if (c == null || c.Type != "tactic") continue;
                var ops = EffectText.Parse(c.Desc, out _, out _);
                foreach (var op in ops)
                {
                    if (op.Verb == "repeat") rep++;
                    if (op.Cost > 0 && op.CostKind == "oath") oath++;
                    if (op.ConditionKind == EffectCondition.EnergyZero) codex++;
                }
            }
            Debug.Log(P + $"   阵营机制实测：Oath {oath} 条 · Codex {codex} 条 · repeat {rep} 条");
            CheckTrue(oath > 0, $"卡池里真的有 `Oath N:` 的战术卡（{oath} 条）");
            CheckTrue(codex > 0, $"卡池里真的有 `Codex:` 的战术卡（{codex} 条）");
            CheckTrue(rep > 0, $"卡池里真的有 `repeat` 的战术卡（{rep} 条）");
        }
    }

    /// <summary>
    /// `lowercost` —— **降费**（`Lower the cost of X by N` / `Lower its cost by N` /
    /// `They cost N less` / `Your troops cost N less`）。
    ///
    /// ⚠️ 这一段**存在的理由就是「payload 切错了也照样算成功」**：第一版把四条写法塞进一条大正则，
    /// 其中 `of?` 被当成「`of` 里的 f 可选」，于是 `Lower the cost of all Vehicles…` 的 payload
    /// 变成了 `f all vehicles…`（少了 `o`、多了 `f`），**而解析判定仍然是 Ok**。
    /// 断言**切出来的 payload 长什么样**才抓得住这种错 —— 只断言「认不认识」抓不住。
    /// </summary>
    static void TestLowerCost()
    {
        {
            var op = OneOp("Lower the cost of all Vehicles in your hand and deck by 1");
            Check(op.Verb, "lowercost", "`Lower the cost of …` → 动词 lowercost");
            Check(op.Payload, "all vehicles in your hand and deck", "payload 是**完整的**「谁」（不是切残的半截）");
            Check(op.Amount, 1, "降 1 费");

            op = OneOp("Lower the cost of all Beasts in your hand by 1");
            Check(op.Payload, "all beasts in your hand", "`in your hand` 的位置词也留在 payload 里");
            CheckTrue(op.Payload.StartsWith("all "), "**不能**丢掉开头的 `all`（丢了就筛错）");

            op = OneOp("Lower the cost of Tyrnak and Fenrir by 1");
            Check(op.Payload, "tyrnak and fenrir", "具名卡原样留着（两个名字用 and 连）");

            op = OneOp("Lower its cost by 2");
            Check(op.Amount, 2, "`Lower its cost by 2` → 2 费");
            Check(op.Payload, "(指代上一张)", "`its` 指代上一张（不是当卡名去查）");

            op = OneOp("They cost 1 less");
            Check(op.Payload, "(指代上一张)", "`They cost 1 less` 同样指代上一张");
            Check(op.Duration, "", "没写时长 = 永久");

            op = OneOp("Your troops cost 1 less this turn");
            Check(op.Amount, 1, "主语型 → 1 费");
            Check(op.Payload, "troops", "主语进 payload");
            Check(op.Duration, "turn", "`this turn` → 本回合（到期要撤）");

            op = OneOp("Your Drones cost 1 less for the rest of this battle");
            Check(op.Duration, "", "⚠️ `for the rest of this battle` 是**永久**，不能当成「本回合」");
        }

        // ---- 结算：打折真的生效，而且**只**打在该打的那类牌上 ----
        {
            var pool = CardDatabase.Load();
            var lower = Tactic("T_Lower", 1, "Lower the cost of all Vehicles in your hand by 2");
            var veh = null as CardDef;
            var inf = null as CardDef;
            foreach (var c in pool)
            {
                if (c.Faction != "Ultramarines") continue;
                if (veh == null && c.IsUnit && c.Subtype == "Vehicle") veh = c;
                if (inf == null && c.IsUnit && c.Subtype == "Infantry") inf = c;
            }
            CheckTrue(veh != null && inf != null, "挑得到一张载具和一张步兵当尺子");

            var ctx = BattlePool(new[] { lower, veh, inf }, new[] { Unit("X", 1, 1, 5) }, pool,
                                 warlordFaction: "Ultramarines");
            ToP1Turn(ctx, 1);                       // 1 费，打得起
            int vehBefore = RuleCore.CostOf(ctx, 0, veh);
            int infBefore = RuleCore.CostOf(ctx, 0, inf);
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Lower"), -1), RuleCodes.OK,
                      "`Lower the cost of all Vehicles in your hand by 2` 打得出去");
            Check(RuleCore.CostOf(ctx, 0, veh), vehBefore - 2, $"载具真的便宜了 2（{vehBefore} → {vehBefore - 2}）");
            Check(RuleCore.CostOf(ctx, 0, inf), infBefore, "步兵**没有**被误伤（只降载具）");
            Check(RuleCore.CostOf(ctx, 1, veh), vehBefore, "对手那边不受影响");
            // 卡面印的费用**不能**被改（`CardDef` 是共享对象，改它会污染整个卡池）
            Check(veh.Cost, vehBefore, "`CardDef.Cost` 本身没被动过（费用修正挂在 ctx 上）");
        }

        // ---- 永久 vs 本回合：`this turn` 的到期要撤掉 ----
        {
            var pool = CardDatabase.Load();
            var lower = Tactic("T_LowerTurn", 1, "Your troops cost 1 less this turn");
            var inf = null as CardDef;
            foreach (var c in pool)
                if (c.Faction == "Ultramarines" && c.IsUnit && c.Subtype == "Infantry") { inf = c; break; }

            var ctx = BattlePool(new[] { lower, inf }, new[] { Unit("X", 1, 1, 5) }, pool,
                                 warlordFaction: "Ultramarines");
            ToP1Turn(ctx, 1);
            int before = RuleCore.CostOf(ctx, 0, inf);
            RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_LowerTurn"), -1);
            Check(RuleCore.CostOf(ctx, 0, inf), System.Math.Max(0, before - 1), "本回合内确实便宜了 1");
            PassTurn(ctx);                          // 换边 → 下一个回合开始，限时修正到期
            Check(RuleCore.CostOf(ctx, 0, inf), before, "回合结束后恢复原价（`this turn` 到期撤掉）");
        }
    }

    /// <summary>
    /// 选牌（`Choose a &lt;筛选&gt; [from/in &lt;来源&gt;] [and &lt;动词&gt;]`）——
    /// 权威源 `rule_core.gd:1157 _resolve_choose` + `:925` 候选匹配 + `:1041 _chosen_apply`。
    /// 数据与出处见 `资料/选牌Choose_数据与设计.md`。三层都要验：
    ///
    ///   ① **解析**：实测那 30 条句的「来源 / 筛选 / 动作」逐条钉死。
    ///      ⚠️ 这一族的剥壳**很容易切残而照样算成功**（`lowercost` 就踩过：payload 被切成
    ///      `f all vehicles…` 而判定照样 Ok）—— 所以断言的是**切出来长什么样**，不是「认不认识」。
    ///   ② **负面**：`Choose an effect …`（Leviathan 2 张）必须判**不认识**。
    ///      原版在这里会默认成 `to_hand`（`rule_core.gd:1193`），那是**静默的错误语义**。
    ///   ③ **结算**：候选域的几个来源各跑一遍，重点是**跨句指代** ——
    ///      这一族的动作常写在**下一句**里（`Choose a … . It costs 2 less`），
    ///      靠的正是选牌时把选中的卡写进引用位。
    /// </summary>
    static void TestChooseCard()
    {
        // ---- ① 解析：30 条实测句逐条钉死 ----
        // 格式：句子 | 来源 | 筛选 | 动作 | 死亡窗口 | 复制张数
        var rows = new[]
        {
            "Choose a 2-cost Leviathan troop and deploy it|pool|2-cost leviathan troop|deploy||0",
            "Choose a Dark Angels Secret and add it to your deck|pool|dark angels secret|todeck||0",
            "Choose a Drone and add it to your hand|pool|drone|hand||0",
            "Choose a Genomic Enhancement and put it in your hand|pool|genomic enhancement|hand||0",
            "Choose a Rune and put it in your hand|pool|rune|hand||0",
            "Choose a Sabotage and add it to the enemy hand|pool|sabotage|enemyhand||0",
            "Choose a Sabotage card and add it to your opponent's hand|pool|sabotage card|enemyhand||0",
            "Choose a Sautekh Stratagem and put it in your hand|pool|sautekh stratagem|hand||0",
            "Choose a Stratagem from your deck and draw it|deck|stratagem|draw||0",
            "Choose a card from your deck and draw it|deck|card|draw||0",
            "Choose a card from your deck and put it at the top of your deck|deck|card|decktop||0",
            "Choose a card in your hand and return it to your deck|hand|card|return||0",
            // ⚠️ 这一条**动作是空的**（卡面就到这里，后一句 `When played, gain 3` 是另一件事）。
            //    原版会把它默认成 `to_hand` —— 那是错的（挑的是**对手手里**的牌）
            "Choose a card in your opponent's hand|enemyhand|card|||0",
            "Choose a friendly Infantry that died this game and return it to your deck|dead|friendly infantry|return|all|0",
            "Choose a friendly troop that died since your last turn and deploy it|dead|friendly troop|deploy|since_last_turn|0",
            "Choose a friendly troop that died this battle and deploy it|dead|friendly troop|deploy|all|0",
            "Choose a friendly troop that died this game and put it in your hand|dead|friendly troop|hand|all|0",
            "Choose a non-Legendary Genestealer Cults troop and add it to your hand|pool|non-legendary genestealer cults troop|hand||0",
            "Choose a non-Legendary Ultramarines card and create two copies in your hand|pool|non-legendary ultramarines card|copies||2",
            // ⚠️ 这三条的**动作在下一句**（`Draw it and create a copy …` / `Lower its cost by 2`），
            //    所以 ChooseAct **必须是空串** —— 判成 `hand` 之类的默认值就是静默错语义
            "Choose a troop from your deck|deck|troop|||0",
            "Choose a troop in your deck|deck|troop|||0",
            "Choose a troop in your hand|hand|troop|||0",
            "Choose a troop from your deck and draw it|deck|troop|draw||0",
            "Choose an Astra Militarum Stratagem and add it to your hand|pool|astra militarum stratagem|hand||0",
            "Choose an Astra Militarum Vehicle and put it in your hand|pool|astra militarum vehicle|hand||0",
            "Choose an Invocation and put it in your hand|pool|invocation|hand||0",
            "Choose an Overlord Power and put it in your hand|pool|overlord power|hand||0",
            "Choose an Ultramarines Psychic Power and put it in your hand|pool|ultramarines psychic power|hand||0",
        };
        foreach (string row in rows)
        {
            var f = row.Split('|');
            var op = OneOp(f[0]);
            Check(op.Verb, "choosecard", $"「{f[0]}」→ 动词 choosecard");
            Check(op.ChooseSrc, f[1], $"「{f[0]}」来源");
            Check(op.ChooseWhat, f[2], $"「{f[0]}」筛选字数切出来的**原文**");
            Check(op.ChooseAct, f[3], $"「{f[0]}」动作");
            Check(op.ChooseDeadScope, f[4], $"「{f[0]}」死亡窗口");
            Check(op.ChooseCopies, int.Parse(f[5]), $"「{f[0]}」复制张数");
        }

        // ---- ② 负面：选「效果」不是选牌，必须如实判不认识 ----
        foreach (string s in new[] { "Choose an effect and give it to a friendly troop",
                                     "Choose an effect and give it to all troops in your hand" })
        {
            Check(EffectText.ParseSegment(s).Kind, EffectText.SegKind.Unknown,
                  $"「{s}」必须判**不认识**（候选效果池不在任何文本里，不许默认成进手牌）");
            CheckTrue(!EffectText.IsFullyParsed(s), $"「{s}」整卡不得被判成解析干净");
        }

        // ---- ③a 结算：`pool` 来源 —— 凭空造一张符合筛选的进手牌 ----
        {
            var pool = CardDatabase.Load();
            var t = Tactic("T_ChooseDrone", 1, "Choose a Drone and add it to your hand");
            var ctx = BattlePool(new[] { t }, new[] { Unit("E", 1, 1, 5) }, pool);
            ToP1Turn(ctx, 1);
            int deckBefore = ctx.Players[0].Deck.Count;
            int handBefore = ctx.Players[0].Hand.Count;
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_ChooseDrone"), -1),
                      RuleCodes.OK, "`Choose a Drone and add it to your hand` 打得出去");
            // 打出的战术卡离手、挑来的那张进手 —— 张数持平
            Check(ctx.Players[0].Hand.Count, handBefore, "战术卡离手、挑来的进手（张数持平）");
            CheckTrue(HasSubtype(ctx.Players[0].Hand, "Drone"),
                      "手牌里多了一张 **Drone 兵种**的卡（不是随便一张）");
            Check(ctx.Players[0].Deck.Count, deckBefore,
                  "牌库张数没变（`pool` 来源是**凭空造**，不动牌库）");
        }

        // ---- ③b 结算：`dead` 来源 —— 把阵亡的部队捞回场上 ----
        {
            var pool = CardDatabase.Load();
            // ⚠️ **阵亡者必须取自真实卡池**：选牌的筛选是拿「全卡池」当判据集的
            //    （`CreatePool.FilterChoose` 复用 `CreatePool.Resolve` 的兵种/阵营判定），
            //    夹具里 `new` 出来的假卡**不在池子里，筛不出来** —— 这是判据只有一份的代价。
            CardDef fallen = null;
            foreach (var c in pool)
            {
                if (c.Faction != "Ultramarines" || !c.IsUnit || c.Subtype != "Infantry") continue;
                if (c.Health > 3) continue;
                fallen = c; break;
            }
            CheckTrue(fallen != null, "挑得到一个真实部队当阵亡者");

            var t = Tactic("T_ChooseDead", 1, "Choose a friendly troop that died since your last turn and deploy it");
            var ctx = BattlePool(new[] { t }, new[] { Unit("Killer", 1, 9, 9) }, pool);
            ToP1Turn(ctx, 3);

            Place(ctx, 0, 1, fallen);                              // 自己的小单位
            Place(ctx, 1, 1, Unit("FixtureBrute", 1, 9, 40));      // 对面的 9/9（血厚，别被反杀）
            CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, 1), RuleCodes.OK,
                      "自己的部队撞上去（会被反击打死）");
            Check(Board(ctx, 0, 1), null, "它**真的阵亡**了、格位空出来");
            Check(ctx.DeadUnits.Count, 1, "阵亡登记表里记着 1 条");
            Check(ctx.DeadUnits[0].Owner, 0, "记的是它的主人");
            Check(ctx.DeadUnits[0].Card.Name, fallen.Name, "记的是它那张卡");

            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_ChooseDead"), -1), RuleCodes.OK,
                      "`Choose a friendly troop that died … and deploy it` 打得出去");
            CheckTrue(SlotOf(ctx, 0, fallen.Name) >= 0,
                      $"「{fallen.Name}」被选出来并**重新放到场上**了（槽 {SlotOf(ctx, 0, fallen.Name)}）");
            Check(ctx.DeadUnits.Count, 0,
                  "取走后登记表**空了**（`TakeFromGraveyard` 要同时清 `Discard` 和 `DeadUnits`）");
        }

        // ---- ③c 结算：**跨句指代** —— 动作写在下一句，靠引用位接通 ----
        // 这条是本轮真正的机制：`Choose a Drone and add it to your hand. It costs 2 less`
        // 的 `It` 指的是**刚挑中那张**，走 `ctx.LastCreated` 的 `(指代上一张)` 那条老路。
        {
            var pool = CardDatabase.Load();
            var t = Tactic("T_ChooseCheap", 1,
                           "Choose a Drone and add it to your hand. It costs 2 less");
            var ctx = BattlePool(new[] { t }, new[] { Unit("E", 1, 1, 5) }, pool);
            ToP1Turn(ctx, 1);
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_ChooseCheap"), -1),
                      RuleCodes.OK, "带后续句的选牌卡打得出去");
            CardDef got = null;
            foreach (var c in ctx.Players[0].Hand) if (c.Subtype == "Drone") { got = c; break; }
            CheckTrue(got != null, "挑到的是 Drone");
            if (got != null)
                Check(RuleCore.CostOf(ctx, 0, got), System.Math.Max(0, got.Cost - 2),
                      $"后续句 `It costs 2 less` **作用在刚挑中那张上**（{got.Cost} → {got.Cost - 2}）");
        }

        // ---- ③d `since your last turn` 的**窗口**：上个回合之前死的不算 ----
        {
            var pool = CardDatabase.Load();
            // ⚠️ 用**真实卡池**里的部队当「很久以前死的」那条 —— 用假卡的话，
            //    它本来就会被兵种筛选（拿全池当判据集）筛掉，**这条断言就空转了**。
            CardDef ancient = null;
            foreach (var c in pool)
                if (c.Faction == "Ultramarines" && c.IsUnit && c.Subtype == "Infantry") { ancient = c; break; }
            CheckTrue(ancient != null, "挑得到一个真实部队当「很久以前死的」那条");

            var t = Tactic("T_ChooseDeadWin", 1, "Choose a friendly troop that died since your last turn and deploy it");
            var ctx = BattlePool(new[] { t }, new[] { Unit("E", 1, 1, 5) }, pool);
            ToP1Turn(ctx, 3);
            // 手工登记一条「很久以前死的」—— 死亡回合早于本方最近一次回合开始
            ctx.DeadUnits.Add(new DeadUnit
            {
                Card = ancient,
                Owner = 0,
                DeathTurn = ctx.Players[0].LastTurnStartMark - 1,
            });
            Check(ctx.DeadUnits.Count, 1, "窗口外那条先放着");
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_ChooseDeadWin"), -1),
                      RuleCodes.OK, "窗口外没候选时**卡照样能打**（这是规则书允许的空候选）");
            Check(ctx.DeadUnits.Count, 1,
                  "那条**没被取走**（它在窗口外 = 不是合法候选，不许凑合拿走）");
            Check(SlotOf(ctx, 0, ancient.Name), -1, "它**没有**被放到场上");
        }
    }

    /// <summary>
    /// **常驻效果 / 手牌陷阱（回合起止触发）** —— 规则书英文版 `:39-41`「Persistent Effects」
    /// （中文版 `:32`）：写「For the rest of this battle」的卡**被弃置后依然生效**，
    /// 要单独放一摞备查 —— 也就是**卡本身就是效果来源**。
    ///
    /// 三层都验：
    ///   ① **解析** —— 3 条实测句逐条钉；另外两条**必须判不认识**（见下）；
    ///   ② **登记** —— 打出去进 `ctx.PersistentEffects`，且**只在打出者自己的回合**触发；
    ///   ③ **手牌陷阱** —— 躺在持有者手牌里，持有者回合结束才咬人；而且**不进自动牌组**。
    ///
    /// ⚠️ 两条**故意判不认识**的，是本轮「宁可报、也不装成已生效」的两处：
    ///   · `For the rest of this battle, give Shield to all Drones you deploy` ——
    ///     它是**部署时触发**，不是回合起止；注册一条永远不会被消费的效果 = 骗人
    ///   · `When you play a Stratagem, your Warlord takes 1 damage` ——
    ///     条件从句**不许当主语**，不然会变成「任何时候都掉血」的静默错语义
    /// </summary>
    static void TestPersistentEffects()
    {
        // ---- ① 解析 ----
        {
            var op = OneOp("For the rest of this battle, at the start of your turn, deploy a Shock Trooper");
            Check(op.Verb, "persist", "`For the rest of …` → 动词 persist（登记常驻效果）");
            Check(op.AtTurnPhase, "turn_start", "触发时机 = 回合开始");
            CheckTrue(op.AtTurnOps != null && op.AtTurnOps.Count == 1 && op.AtTurnOps[0].Verb == "deploy",
                      "正文解析出来了（1 条 deploy），**不是**只记了原文");
            // ⚠️ `Payload` 是**小写**的（`Dispatch` 拿到的就是 `low`）—— 和 `ChooseWhat` 一个口径。
            //    日志要打原样的话用 `op.Source`（那是**原文分句**，没改过大小写）。
            Check(op.Payload, "deploy a shock trooper", "正文原文留着（小写，日志要打人话时用 op.Source）");

            op = OneOp("For the rest of the match, at the end of your turn, draw a card");
            Check(op.AtTurnPhase, "turn_end", "`this battle` 与 `the match` 都认");

            var wr = EffectText.ParseSegment(
                "Your Warlord gains: \"At the start of your turn, create a random Combat Elixir in your hand\"");
            CheckTrue(wr.Ops != null && wr.Ops.Count == 1, "`Your Warlord gains: \"…\"` 解析得出 1 条");
            Check(wr.Ops[0].Verb, "persist", "它也是**常驻效果**（原版挂到督军身上，我们让那一方注册）");
            Check(wr.Ops[0].AtTurnPhase, "turn_start", "内层时机 = 回合开始");

            var trap = OneOp("At the end of your turn, your troops take 1 damage");
            Check(trap.Verb, "atturn", "`At the end of your turn, …` → 动词 atturn（手牌陷阱）");
            Check(trap.AtTurnPhase, "turn_end", "触发时机 = 回合结束");
            CheckTrue(trap.AtTurnOps != null && trap.AtTurnOps.Count == 1
                      && trap.AtTurnOps[0].Verb == "deal" && trap.AtTurnOps[0].Amount == 1,
                      "正文 = 打 1 点伤害");
            CheckTrue(trap.AtTurnOps[0].Target != null && trap.AtTurnOps[0].Target.Side == "own",
                      "打的是**自己**那边");
            Check(trap.AtTurnOps[0].Target.Count, 0,
                  "**全体**（`your troops **take**` 是复数动词 —— 单复数从动词看，不靠猜名词）");
        }
        // ⚠️ 上一条 `When you play a Stratagem, …` 必须**判不认识**：
        //    不挡的话 `(.+?)` 会把「什么时候」的从句吃成目标，然后在**任何**时候都结算。
        {
            Check(EffectText.ParseSegment("When you play a Stratagem, your Warlord takes 1 damage").Kind,
                  EffectText.SegKind.Unknown,
                  "带条件从句的 `X takes N damage` **判不认识**（不许把条件当主语）");
            // 但「主语写全」的那种要认 —— 单数动词 = 只打一个
            var one = OneOp("your Warlord takes 2 damage");
            Check(one.Amount, 2, "`your Warlord takes 2 damage` → 2 点");
            Check(one.Target.Count, 1, "单数动词 `takes` → **只打一个**");
        }
        // ---- ③ 同族但**别的触发点**的写法（2026-09-13 第三十轮做掉）----
        // 原来这三条**必须判不认识**（`EffectTargetSpec.Deployed` 还没有、`costmore` 也没实现）。
        // 现在真做了，断言改成**钉「解析出来长什么样」** —— 不只钉「认不认识」：
        // 第十六轮踩过，只钉认不认识会让静默错解析（payload 被切错）照样通过。
        {
            var op = OneOp("For the rest of this battle, give Shield to all Drones you deploy");
            Check(op.Verb, "persist", "`… give Shield to all Drones you deploy` → 登记常驻效果");
            Check(op.AtTurnPhase, "deploy", "触发点是**部署**（不是回合起止）");
            CheckTrue(op.Filter != null && op.Filter.KindWord == "drone", "筛的是 **Drone**");
            CheckTrue(op.AtTurnOps != null && op.AtTurnOps.Count == 1, "正文解析出 1 条");
            Check(op.AtTurnOps[0].Verb, "give", "正文动词 = give");
            Check(op.AtTurnOps[0].Payload, "shield", "载荷切出来是 **shield**（不是空、也不是带前缀的）");
            CheckTrue(op.AtTurnOps[0].Target != null && op.AtTurnOps[0].Target.Deployed,
                      "目标是「**刚部署的那个**」");

            var arm = OneOp("For the rest of the match, give Armour 1 to Vehicles you put in play");
            Check(arm.AtTurnPhase, "deploy", "`you put in play` 也是部署时");
            CheckTrue(arm.Filter != null && arm.Filter.KindWord == "vehicle", "筛的是 **Vehicle**");
            Check(arm.AtTurnOps[0].Payload, "armour 1", "载荷 = armour 1（**数值没被吃掉**）");

            var cm = OneOp("For the rest of this battle, Sabotage cards in the enemy hand cost 1 more");
            Check(cm.Verb, "costmore", "`… Sabotage cards in the enemy hand cost 1 more` → 持续改费");
            Check(cm.Amount, 1, "加 1 费");
            CheckTrue(cm.Target != null && cm.Target.Side == "enemy", "加的是**对手**手里那批");
            Check(cm.Target.Kind, "sabotage", "筛的是 sabotage");

            // ⚠️ **认不出的一律不许硬认** —— 这两条不能因为「看起来像」就被收下：
            //    · 条件从句当主语（老红线）
            //    · 兵种词表里没有的词
            Check(EffectText.ParseSegment("For the rest of this battle, give Shield to all Wizards you deploy").Kind,
                  EffectText.SegKind.Unknown, "兵种词表里没有的词 → 判不认识（不猜）");
            Check(EffectText.ParseSegment("For the rest of this battle, at the start of your turn, do the thing").Kind,
                  EffectText.SegKind.Unknown, "正文认不出的常驻效果 → 整句判不认识（绝不注册一条不会被消费的）");
        }

        // ---- ② 登记 + **只在打出者自己的回合**触发 ----
        {
            var pool = CardDatabase.Load();
            var t = Tactic("T_Persist", 1,
                           "For the rest of this battle, at the start of your turn, deploy a Shock Trooper");
            var ctx = BattlePool(new[] { t }, new[] { Unit("E", 1, 1, 5) }, pool,
                                 warlordFaction: "AstraMilitarum");
            ToP1Turn(ctx, 1);
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Persist"), -1), RuleCodes.OK,
                      "`For the rest of this battle, at the start of your turn, deploy a Shock Trooper` 打得出去");
            Check(ctx.PersistentEffects.Count, 1, "登记了 1 条常驻效果");
            Check(ctx.PersistentEffects[0].Owner, 0, "记的是**打出的那一方**");
            Check(ctx.PersistentEffects[0].Phase, "turn_start", "触发时机 = 回合开始");
            CheckTrue(ctx.PersistentEffects[0].Source != null
                      && ctx.PersistentEffects[0].Source.Name == "T_Persist",
                      "记下了来源卡（规则书要求这类卡留档备查）");

            PassTurn(ctx);                       // → P2 的回合开始
            Check(SlotOf(ctx, 0, "Shock Trooper"), -1,
                  "**对手**的回合开始**不触发** —— 常驻效果只属于打出它的那一方");
            PassTurn(ctx);                       // → P1 的回合开始
            CheckTrue(SlotOf(ctx, 0, "Shock Trooper") >= 0,
                      $"自己的回合开始**真的部署了** Shock Trooper（槽 {SlotOf(ctx, 0, "Shock Trooper")}）");
        }

        // ---- ③ 手牌陷阱：持有者回合结束才咬人，而且不进自动牌组 ----
        {
            var pool = CardDatabase.Load();
            CardDef trap = null;
            foreach (var c in pool) if (c.Name == "Poisoned Supplies") { trap = c; break; }
            CheckTrue(trap != null, "卡池里找得到 `Poisoned Supplies`");
            CheckTrue(EffectText.IsHandTrap(trap.Desc), "它被判成**手牌陷阱**");
            CheckTrue(!DeckBuilder.TacticPlayable(trap),
                      "**不进自动牌组** —— 陷阱卡是塞给对手的，自己牌组里放一张只会每回合坑自己");

            var ctx = BattlePool(new[] { Unit("F1", 1, 1, 1) }, new[] { Unit("E", 1, 1, 5) }, pool);
            ToP1Turn(ctx, 3);
            var victim = Place(ctx, 0, 1, Unit("FixtureTrapVictim", 1, 1, 9));
            ctx.Players[0].Hand.Add(trap);

            // 回合记账：`ToP1Turn` 之后**当前行动方是 P1**，所以第一次 `EndTurn` 结束的**就是 P1 的回合**。
            // 顺序：P1 回合末（**咬**）→ P2 回合末（不咬）→ P1 回合末（**咬**）
            RuleCore.EndTurn(ctx);               // P1 的回合结束（陷阱持有者）
            Check(victim.Health, 8, "**P1 手里**有陷阱 → P1 的回合结束，自己的部队掉 1 血");
            ctx.ClearSignals();
            RuleCore.BeginTurn(ctx);             // → P2 的回合

            RuleCore.EndTurn(ctx);               // P2 的回合结束 —— 陷阱不在 P2 手里
            Check(victim.Health, 8, "**P2 的回合结束不掉血**（陷阱在谁手里，谁的回合结束才生效）");

            RuleCore.BeginTurn(ctx);             // → P1 的回合
            RuleCore.EndTurn(ctx);               // P1 的回合结束：**再咬一次**
            Check(victim.Health, 7, "P1 的下一个回合结束**又咬一次**（常驻在手里，每回合都咬）");
        }
    }

    /// <summary>从卡池里按名字取一张（找不到返回 null）</summary>
    static CardDef PoolCard(IList<CardDef> pool, string name)
    {
        foreach (var c in pool) if (c.Name == name) return c;
        return null;
    }

    /// <summary>
    /// **单位卡 `desc` 里的触发式效果**（`Rally:` / `Strike:` / `Slay:` / `Backlash:` / `Penitence:`）
    /// —— 2026-09-13 第三十一轮。
    ///
    /// **修之前是什么样**：触发式正文只走 <see cref="EffectSpec"/> 那个**封闭文法**
    /// （只有 Damage/Heal/Draw），而原版卡面写的是 `Rally: Stun an enemy` ——
    /// 解析失败 ⇒ 卡面标 `*` ⇒ **打起来静默不动**。
    /// 实测 **89 张卡**在 `keywords` 和 `desc` 两处都登记了触发、正文却没人认。
    ///
    /// ⚠️ 断言**两层都要**：
    ///   ① 正文被解析出来了（`TriggerOps` 非空、动词/载荷切得对）
    ///   ② **它真的改变了局面** —— 只钉① 会漏掉「解析出来了但结算层没分支」，
    ///      那正是本工程反复强调的那类**静默失效**。
    /// </summary>
    static void TestUnitDescTriggers()
    {
        var pool = CardDatabase.Load();

        // ---- ① `Rally: Stun an enemy`（Howling Banshee，Aeldari）----
        {
            var banshee = PoolCard(pool, "Howling Banshee");
            CheckTrue(banshee != null, "卡池里有 `Howling Banshee`");
            if (banshee != null)
            {
                CheckTrue(banshee.TriggerOps(KeywordTable.Rally) != null,
                          "它的 `Rally:` 正文**被解析出来了**（原来走封闭文法，`Stun an enemy` 认不出）");
                Check(banshee.TriggerText(KeywordTable.Rally), "Stun an enemy", "正文原文留着（日志要用）");
                Check(banshee.TriggerOps(KeywordTable.Rally)[0].Verb, "stun", "解析成 `stun` 动词");

                var ctx = BattlePool(new[] { banshee }, new[] { Unit("EFoe", 1, 1, 9) }, pool,
                                     warlordFaction: "SaimHann");
                ToP1Turn(ctx, 4);
                var foe = Place(ctx, 1, 1, Unit("FixtureStunTarget", 1, 1, 9));
                CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "Howling Banshee"), 1), RuleCodes.OK,
                          "从手牌部署 `Howling Banshee`");
                CheckTrue(foe.IsStunned,
                          "**部署触发真的生效了**：对面那个单位被 Stun（只钉「解析出来了」抓不到这条）");
            }
        }

        // ---- ② `Strike: Return this troop to your hand`（Warp Spider）----
        // ⚠️ 这条专门盯**代词**：`this troop` 在解析器里归 `prev`（那是给战术卡的
        //    `Choose … Draw it` 用的），触发式正文里没有「上一句」——
        //    靠 `ResolveOps` 把**触发者自己**种进 `LastTarget`。不种的话它会去回手
        //    **上一张被指过的牌**，而场面上看不出哪里不对（静默错打）。
        {
            var spider = PoolCard(pool, "Warp Spider");
            CheckTrue(spider != null, "卡池里有 `Warp Spider`");
            if (spider != null)
            {
                var ctx = BattlePool(new[] { Unit("P1Filler", 1, 1, 1) }, new[] { Unit("EFoe", 1, 1, 9) }, pool,
                                     warlordFaction: "SaimHann");
                ToP1Turn(ctx, 4);
                // 直接摆上场（不走部署）—— 这一节要验的是 `Strike`，不是 `Rally`；
                // 而且刚部署的单位是疲劳的，打不了。
                var mine = Place(ctx, 0, 1, spider, exhausted: false);
                var foe = Place(ctx, 1, 1, Unit("FixtureStrikeTarget", 1, 1, 9));
                int handBefore = ctx.Players[0].Hand.Count;

                CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, 1), RuleCodes.OK, "用 Warp Spider 攻击");
                CheckTrue(mine.Health > 0, "攻击者活下来了（`Strike` 的前提是本单位存活）");
                Check(SlotOf(ctx, 0, "Warp Spider"), -1,
                      "`Strike:` 触发 → **它自己回手了**（代词 `this troop` 认对了）");
                Check(ctx.Players[0].Hand.Count, handBefore + 1, "手牌 +1");
                Debug.Log(P + $"   ② 打了一下之后：场上 {SlotOf(ctx, 0, "Warp Spider")}、"
                          + $"手牌 {handBefore} → {ctx.Players[0].Hand.Count}");
            }
        }

        // ---- ③ 反向：触发时机**我们没有**的，仍然不许收（收了就是「注册一条没人消费的效果」）----
        {
            var talent = PoolCard(pool, "Howling Banshee Exarch");
            CheckTrue(talent != null, "卡池里有 `Howling Banshee Exarch`（它的 desc 里是 `Strike:`）");
            // 找一张只带 `Talent:` 的督军卡
            CardDef wl = null;
            foreach (var c in pool)
                if (c.Type == "hero" && c.Desc != null && c.Desc.Contains("Talent:")
                    && !c.Desc.Contains("Rally:") && !c.Desc.Contains("Strike:")) { wl = c; break; }
            CheckTrue(wl != null, $"找得到只带 `Talent:` 的督军卡（`{wl?.Name}`）");
            if (wl != null)
            {
                CheckTrue(wl.TriggerOps(KeywordTable.Rally) == null
                          && wl.TriggerOps(KeywordTable.Strike) == null,
                          "`Talent:` **不收** —— 本版没有那个触发时机，收了就是骗玩家");
            }
        }
    }

    ///
    /// <summary>
    /// **部署时触发**（`… give Shield to all Drones you deploy`）与**持续改费**
    /// （`… Sabotage cards in the enemy hand cost 1 more`）—— 2026-09-13 第三十轮。
    ///
    /// **为什么这两件事放在一个方法里**：它们共用同一份**筛选条件**（<see cref="CardCriteria"/>，
    /// 我们的 `TargetCriteria`）。原版也是同一份 —— 部署那条路走 `OtherUnitSummoned` 事件、
    /// 改费那条路走 `HandEffect`，但两边都用 `TargetCriteria` 描述「作用在哪种卡上」。
    ///
    /// ⚠️ **断言必须钉「真的改变了局面」**，不能只钉「登记了一条效果」：
    ///    上一轮（常驻效果）的核心教训就是「注册一条永远不会被消费的效果 = 骗玩家」。
    ///    这里每条都验「该生效的生效了、**不该生效的没有**」——后者才是筛错了会露馅的地方。
    /// </summary>
    static void TestDeployBuffAndCostMore()
    {
        // ---- ① 部署时给：只给**筛中的那个兵种**，别的兵种一张都不给 ----
        {
            var pool = CardDatabase.Load();
            var t = Tactic("T_DeployBuff", 1,
                           "For the rest of this battle, give Shield to all Drones you deploy");
            var drone = new CardDef("FixtureDrone", "FixtureDrone", "unit", "", "common", "Test",
                                    1, 1, 3, 0, null, subtype: "Drone");
            var tank = new CardDef("FixtureTank", "FixtureTank", "unit", "", "common", "Test",
                                   1, 1, 3, 0, null, subtype: "Vehicle");
            var ctx = BattlePool(new[] { t, tank, drone }, new[] { Unit("E", 1, 1, 9) }, pool,
                                 warlordFaction: "TauEmpire");
            ToP1Turn(ctx, 3);

            // ⚠️ **先摆一个「早就在场上」的 Drone**（走 `Place` = 跳过部署流程）。
            //    没有它，这个用例钉不住最关键的那条：卡面写的是「你**部署**的」，
            //    不是「你场上**所有**的」—— 实现要是退回成「查场上所有 Drone」，
            //    下面那条 `droneEarly` 的断言就会亮。
            var early = Place(ctx, 0, 4, new CardDef("FixtureDroneEarly", "FixtureDroneEarly",
                                                    "unit", "", "common", "Test", 1, 1, 3, 0, null,
                                                    subtype: "Drone"));

            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_DeployBuff"), -1), RuleCodes.OK,
                      "`… give Shield to all Drones you deploy` 打得出去");
            Check(ctx.PersistentEffects.Count, 1, "登记了 1 条常驻效果");
            Check(ctx.PersistentEffects[0].Trigger, "deploy", "它是**部署时**触发，不是回合起止");
            CheckTrue(ctx.PersistentEffects[0].Criteria != null
                      && ctx.PersistentEffects[0].Criteria.KindWord == "drone",
                      "筛选条件记下来了（drone）");
            CheckTrue(!early.HasShield,
                      "**登记这条效果不会顺手给场上已有的 Drone 补上**（只对以后部署的生效）");

            // ⚠️ 筛错的话**这条会先炸** —— 部署一张 Vehicle 就不该给护盾
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "FixtureTank"), 1), RuleCodes.OK,
                      "部署一张 Vehicle");
            var tankU = Board(ctx, 0, 1);
            CheckTrue(tankU != null && !tankU.HasShield,
                      "部署的是 **Vehicle** → **不给**护盾（筛的是 Drone；筛错这里就会亮）");

            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "FixtureDrone"), 2), RuleCodes.OK,
                      "部署一张 Drone");
            var droneU = Board(ctx, 0, 2);
            CheckTrue(droneU != null && droneU.HasShield,
                      "部署的是 **Drone** → 常驻效果**真的把护盾加上了**");

            // ⚠️ **不能「一次生效就永远全体生效」**：护盾只给刚部署的那一个，
            //    不许回头把场上**早就躺着**的同类单位也补一遍（那是「打得比卡面宽」）
            CheckTrue(!early.HasShield,
                      "效果生效了也**没顺手给先前那个 Drone 补上**（`you deploy` ≠ `on your board`）");
            CheckTrue(tankU != null && !tankU.HasShield,
                      "后部署的 Drone 生效了，**也没顺手把先前的 Vehicle 补上**");
        }

        // ---- ② 持续改费：加在**对手手里**那类牌上，自己的不加、别的牌不加 ----
        {
            var pool = CardDatabase.Load();
            var t = Tactic("T_CostMore", 1,
                           "For the rest of this battle, Sabotage cards in the enemy hand cost 1 more");
            var sab = new CardDef("FixtureSabotage", "FixtureSabotage", "tactic", "Does nothing",
                                  "common", "Test", 3, 0, 0, 0, null, subtype: "Sabotage");
            var plain = Tactic("FixturePlain", 3, "Does nothing");
            // P1 手里也放一张破坏卡 —— 用来验「**自己手里的不加价**」
            var sab2 = new CardDef("FixtureSabotage2", "FixtureSabotage2", "tactic", "Does nothing",
                                   "common", "Test", 3, 0, 0, 0, null, subtype: "Sabotage");
            var ctx = BattlePool(new[] { t, sab2 }, new[] { sab, plain }, pool,
                                 warlordFaction: "Genestealers");
            ToP1Turn(ctx, 3);

            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_CostMore"), -1), RuleCodes.OK,
                      "`… Sabotage cards in the enemy hand cost 1 more` 打得出去");
            Check(ctx.CostMods.Count, 1, "挂上了 1 条费用修正");
            Check(ctx.CostMods[0].HandOf, 1,
                  "作用在 **P2 的手牌**上（`the enemy hand` —— 极性反了这条会亮）");

            Check(RuleCore.CostOf(ctx, 1, sab), 4, "对手手里的破坏卡 3 → **4**（真的加价了）");
            Check(RuleCore.CostOf(ctx, 1, plain), 3, "对手手里**别的**牌不加价");
            Check(RuleCore.CostOf(ctx, 0, sab2), 3, "**自己手里**的破坏卡不加价");
            Check(RuleCore.CostOf(ctx, 0, t), 1, "自己手里别的牌不加价");
        }

        // ---- ③ 实战检查：**整局里**这个机制真的会触发吗 ----
        // 出处：`资料/阵营推进_清单与交接.md` §六 ——「新做一批卡之后，照着这个测试的思路加一局
        // 实战检查，它是**跑得完 ≠ 跑得对**的那道关」。第十六轮就是靠它抓到
        // 「自动凑的牌组一张战术卡都没有」「AI 只出单位卡」这两个引擎自检抓不到的问题。
        //
        // ⚠️ **引擎是种子化的，同一副牌 + 同一个种子永远同一局** —— 所以这一节**不会 flaky**，
        //    断言可以写死「至少触发过 N 次」。
        //
        // ⚠️ 自动凑的牌组不一定含这几张卡，所以**硬塞**：用 TauEmpire 的牌组改几张。
        //    （7 张 `Drone` 全是 TauEmpire 的 —— `CreatePool` 的对账里写着「钛无人机 7=7」。）
        {
            var pool = CardDatabase.Load();
            CardDef tacticCard = null;
            var drones = new List<CardDef>();
            foreach (var c in pool)
            {
                if (c.Name == "Experimental Drone") tacticCard = c;
                if (c.Faction == "TauEmpire" && c.Subtype == "Drone" && c.IsUnit) drones.Add(c);
            }
            CheckTrue(tacticCard != null && drones.Count > 0,
                      $"卡池里有 `Experimental Drone` 和 TauEmpire 的 Drone（找到 {drones.Count} 张 Drone）");
            if (tacticCard != null && drones.Count > 0)
            {
                int registered = 0, fired = 0, granted = 0;
                for (int g = 0; g < 4; g++)
                {
                    // ⚠️ **牌组里全是 Drone** —— 第一版用 `StarterDeck` 自动凑，结果 4 局里只登记了 1 次，
                    //    而且登记之后部署的那 2 个单位**不是 Drone**（所以本来就不该触发）。
                    //    那样断言就被「部署的不是靶子」搅浑了，看不出机制到底有没有问题。
                    //    换成全 Drone 牌组 ⇒ **登记之后任何一次部署都是 Drone**，断言才有意义。
                    var d0 = new List<CardDef>();
                    CardDef hero = null;
                    foreach (var c in pool)
                        if (c.Faction == "TauEmpire" && c.Type == "hero") { hero = c; break; }
                    CheckTrue(hero != null, "TauEmpire 有督军卡");
                    if (hero == null) break;
                    d0.Add(hero);
                    for (int i = 0; i < 22; i++) d0.Add(drones[i % drones.Count]);
                    d0.Add(tacticCard);      // 抽牌是 `pop_back` ⇒ 放末尾 = 最先抽到

                    var d1 = DeckBuilder.StarterDeck(pool, "Goff", DeckBuilder.ClassicDeckSize,
                                                     new System.Random(400 + g), unitsOnly: false);
                    // ⚠️ `shuffle: false`：这一节要的是「一定跑到」，顺序必须自己说了算
                    var ctx = RuleCore.NewBattle(d0, d1, seed: 9000 + g, shuffle: false, cardPool: pool);

                    int guard = 0, tac = 0;
                    while (!ctx.IsOver && guard++ < 300)
                    {
                        RuleCore.BeginTurn(ctx);
                        PlayAiTurn(ctx, ref tac);
                        if (ctx.IsOver) break;
                        RuleCore.EndTurn(ctx);
                    }
                    foreach (string e in ctx.Events)
                    {
                        if (e.IndexOf("登记了**常驻效果**", System.StringComparison.Ordinal) >= 0
                            && e.IndexOf("部署", System.StringComparison.Ordinal) >= 0) registered++;
                        if (e.IndexOf("部署触发段：", System.StringComparison.Ordinal) >= 0) fired++;
                        if (e.IndexOf("盯上", System.StringComparison.Ordinal) >= 0) granted++;
                    }
                }
                Debug.Log(P + $"   实战：4 局里 `Experimental Drone` 被登记 {registered} 次 · "
                          + $"部署触发段跑了 {fired} 次 · 筛中 Drone {granted} 次");
                CheckTrue(registered > 0, "整局里**真的打出并登记了**这条常驻效果（AI 会打它）");
                CheckTrue(fired > 0, "整局里**部署触发段真的跑过**（不是「登记了但从不消费」）");
                CheckTrue(granted > 0, "整局里**真的筛中了 Drone**（筛选条件在实战数据上成立）");
            }
        }
    }

    /// <summary>按**引用**找在不在（手牌里两张同名卡是两张牌）</summary>
    static bool HasRef(List<CardDef> list, CardDef card)
    {
        foreach (var c in list) if (object.ReferenceEquals(c, card)) return true;
        return false;
    }

    /// <summary>牌库的**顺序**（名字连起来）—— 用来验「洗没洗牌」</summary>
    static string DeckOrder(BattleContext ctx, int p)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var c in ctx.Players[p].Deck) sb.Append(c.Name).Append('|');
        return sb.ToString();
    }

    /// <summary>日志里有没有出现过某句话（负向断言用：老 bug 会留特征串）</summary>
    static bool HasLog(BattleContext ctx, string fragment)
    {
        foreach (string e in ctx.Events)
            if (e.IndexOf(fragment, System.StringComparison.Ordinal) >= 0) return true;
        return false;
    }

    /// <summary>
    /// **回手 / 回牌库（`return`）· `Draw it`（`drawref`）· `add X to your hand`（`create`）**
    /// —— 2026-09-13「按阵营逐个推进」的第一批（Sautekh / DarkAngels / TauEmpire）。
    ///
    /// 三件事都是从覆盖率那一栏里挖出来的，其中**一条是修静默错解析**：
    ///   · `Draw it` 以前掉进 `drawtype`，被当成「抽一种**叫 `it` 的兵种**」——
    ///     `MatchesKind("it")` 恒 false，翻遍牌库一张都找不到、只写一行日志就当无事发生，
    ///     **而覆盖率的「完全解析」和「载荷有机制」两栏都算它通过**。这一条最要紧。
    ///   · `return` 的语义规则书英文版 `:455-457` 写死了（回牌库**要洗**，除非卡面写明「顶」）。
    ///   · `add X to your hand` 卡在 `ReCreate` 只认 `create` 上（`If target dies, …` 的正文）。
    /// </summary>
    static void TestReturnAndRefs()
    {
        // ---- ① 解析：`return` ----
        {
            var op = OneOp("Return a friendly troop to your hand");
            Check(op.Verb, "return", "`Return … to your hand` → 动词 return");
            Check(op.Dest, "hand", "目的地 = 手牌");
            Check(op.Payload, "a friendly troop", "「谁」原样留着");
            CheckTrue(op.Target != null && op.Target.Side == "own",
                      "目标在自己那侧（表现层要靠它决定高亮哪边）");

            op = OneOp("Return a friendly Vehicle to your hand");
            Check(op.Dest, "hand", "`Return a friendly Vehicle to your hand` → 手牌");
            Check(op.Payload, "a friendly vehicle", "「谁」是小写原文");

            op = OneOp("Return a friendly troop and a random enemy troop to the top of their deck");
            Check(op.Verb, "return", "两个目标的写法也认");
            Check(op.Dest, "decktop", "`to the top of their deck` → **牌库顶**（不是洗入）");
            Check(op.Payload, "a friendly troop and a random enemy troop", "**两个**目标都留在 Payload 里");
            CheckTrue(op.Target == null,
                      "两目标时 `Target` 留空 —— 只填第一个会让表现层以为「只有一个目标」");

            // ⚠️ 目的地不纯 → **如实判不认识**。按「包含」匹配会把后半句静默吞掉
            Check(EffectText.ParseSegment("Return a friendly troop to your hand and reduce its cost to 1").Kind,
                  EffectText.SegKind.Unknown,
                  "`… to your hand **and reduce its cost to 1**` 判不认识（那半句没做，不许吞掉装作做了）");
        }

        // ---- ② 解析：`Draw it` 是 `drawref`，**不是** `drawtype("it")` ----
        {
            var r = EffectText.ParseSegment("Draw it and create a copy of it in your hand");
            CheckTrue(r.Ops != null && r.Ops.Count >= 1, "`Draw it and create a copy …` 拆得出效果");
            Check(r.Ops[0].Verb, "drawref", "`Draw it` → 动词 drawref（指代刚选中的那张）");
            foreach (var o in r.Ops)
                CheckTrue(o.Verb != "drawtype" || o.Payload != "it",
                          "**绝不能**再被当成 `drawtype` 且 payload = `it`（旧 bug 的特征）");

            var r2 = EffectText.ParseSegment("Draw 3 cards and lower their cost by 3");
            Check(r2.Ops.Count, 2, "`Draw 3 cards and lower their cost by 3` 拆成 2 条（抽牌 + 降费）");
            Check(r2.Ops[0].Verb, "draw", "第 1 条是抽牌");
            Check(r2.Ops[1].Verb, "lowercost", "第 2 条是降费（`and lower …` 现在切得开了）");
            Check(r2.Ops[1].Payload, "(指代上一张)", "降的是「刚抽到的那批」");
        }

        // ---- ③ 解析：`If target dies, add X to your hand` ----
        {
            var op = OneOp("If target dies, add Extermination Protocol to your hand");
            Check(op.Verb, "create", "`add X to your hand` → 动词 create（`add` 和 `create` 是同一件事）");
            Check(op.Dest, "hand", "目的地 = 手牌（`to` 那族写法）");
            Check(op.Payload, "extermination protocol", "造的是具名卡");
            Check(op.ConditionKind, "targetdies", "条件认出来了（认不出来会静默当成立）");
        }

        // ---- ④ 结算：`return` 真的把单位挪下手牌，而且**后续句的降费作用在它身上** ----
        {
            var pool = CardDatabase.Load();
            CardDef veh = null;
            foreach (var c in pool)
                if (c.Faction == "Ultramarines" && c.Subtype == "Vehicle") { veh = c; break; }
            CheckTrue(veh != null, "挑得到一张真实载具当尺子");

            var t = Tactic("T_Return", 1, "Return a friendly Vehicle to your hand. It costs 4 less");
            var ctx = BattlePool(new[] { t }, new[] { Unit("E", 1, 1, 5) }, pool);
            ToP1Turn(ctx, 3);
            Place(ctx, 0, 1, veh);
            int before = RuleCore.CostOf(ctx, 0, veh);

            // ⚠️ 要传**目标格位**：`return` 是「要选目标」的卡（`PickTarget` 拿得到 spec），
            //    `CanPlayTactic` 对这类卡要求 `targetSlot` 合法且那一格有人。载具放在槽 1。
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Return"), 1), RuleCodes.OK,
                      "`Return a friendly Vehicle to your hand` 打得出去");
            Check(Board(ctx, 0, 1), null, "单位**离开了格位**");
            CheckTrue(HasRef(ctx.Players[0].Hand, veh), "它**进了手牌**");
            CheckTrue(!HasRef(ctx.Players[0].Discard, veh),
                      "回手的那张**不进弃牌堆** —— 回手不是阵亡（弃牌堆里那 1 张是打出去的战术卡自己）");
            Check(ctx.DeadUnits.Count, 0, "**不进阵亡登记表** —— 它没死");
            Check(RuleCore.CostOf(ctx, 0, veh), System.Math.Max(0, before - 4),
                  $"后续句 `It costs 4 less` 作用在**刚回手那张**上（{before} → {before - 4}）");
        }

        // ---- ⑤ 结算：`Draw it` 抽出的是**刚选中的那张**（不是空过） ----
        {
            var pool = CardDatabase.Load();
            var t = Tactic("T_DrawIt", 1, "Choose a troop from your deck. Draw it");
            var ctx = BattlePool(new[] { t }, new[] { Unit("E", 1, 1, 5) }, pool);
            ToP1Turn(ctx, 1);
            int deckBefore = ctx.Players[0].Deck.Count;
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_DrawIt"), -1), RuleCodes.OK,
                      "`Choose a troop from your deck. Draw it` 打得出去");
            Check(ctx.Players[0].Deck.Count, deckBefore - 1, "牌库**真的少了一张**（不是空过）");
            CheckTrue(!HasLog(ctx, "没找到「it」"),
                      "日志里**没有**「翻遍牌库也没找到 it」—— 那是旧 bug 的特征串");
        }

        // ---- ⑥ 规则书 `:477`：**牌库来源的 choose 要洗牌**（未选中的候选归还并洗） ----
        {
            var pool = CardDatabase.Load();
            var t = Tactic("T_ChooseShuffle", 1, "Choose a troop from your deck");
            var ctx = BattlePool(new[] { t }, new[] { Unit("E", 1, 1, 5) }, pool);
            ToP1Turn(ctx, 1);
            int n0 = ctx.Players[0].Deck.Count;
            string order0 = DeckOrder(ctx, 0);

            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_ChooseShuffle"), -1), RuleCodes.OK,
                      "`Choose a troop from your deck`（动作在下一句）打得出去");
            Check(ctx.Players[0].Deck.Count, n0,
                  "这条只挑不动，牌库**张数没变**");
            CheckTrue(DeckOrder(ctx, 0) != order0,
                      "但牌库**顺序变了 = 洗过牌**（规则书英文版 :477）");
        }
    }

    /// <summary>
    /// **卡面逐张核对**（2026-09-13）修掉的两列 —— `subtype`（字段）与 `keywords`。
    ///
    /// 为什么单开一条：这两列错了**不会报错**，只会**悄悄筛错卡** ——
    /// `Hunting Wolf` 卡面印的是「兽」，OCR 源表写成了 `Troop`，
    /// 于是 `抽/造一张兽` **永远抽不到它**，而卡看着是能用的。
    ///
    /// 数据是怎么修的：29 个子代理**逐张看 1118 张卡图**独立抄录（`Unity/资料/卡表核对_卡图提取/`），
    /// 算出修正条目（`Unity/工具/gen_cardface_fixes.py` → `数据/游戏数据/cardface_fixes.json`），
    /// 由 `工具/gen_cards_engine.py` 盖到卡表上。对账见 `资料/卡表逐张核对_与对账.md`。
    ///
    /// ⚠️ **这里钉的是「修好了」，不是「曾经的缺口」** —— 缺口见下面几条注释。
    /// </summary>
    static void TestCardFaceFixes()
    {
        var pool = CardDatabase.Load();

        // ---- ① 不变量：**没有任何单位卡的 subtype 是「卡的类型」** ----
        // 这正是当初串列的形态（`Heavy Intercessor` 被写成 `Unit`、`Daemonette` 被写成 `Troop`）。
        // 钉成不变量之后，将来谁再把类型填进字段列，这条会当场炸。
        var typey = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
                    { "unit", "troop", "soldier", "card", "upgrade" };
        var bad = new List<string>();
        foreach (var c in pool)
            if (c.IsUnit && !string.IsNullOrEmpty(c.Subtype) && typey.Contains(c.Subtype))
                bad.Add(c.Name + "=" + c.Subtype);
        Check(bad.Count, 0, "**没有单位卡的 subtype 是「卡的类型」**"
              + (bad.Count > 0 ? "：" + string.Join("、", bad.ToArray(), 0, System.Math.Min(6, bad.Count)) : ""));

        // ---- ② 逐张钉：这些是卡图上白纸黑字印着的 ----
        Check(SubOf(pool, "Heavy Intercessor"), "Infantry",
              "`Heavy Intercessor` 卡面印 `Infantry`（OCR 源表里曾是 `Unit`）");
        Check(SubOf(pool, "Hunting Wolf"), "Beast",
              "`Hunting Wolf` 卡面印 `Beast`（曾是 `Troop`）—— 它得能被「兽」筛到");
        Check(SubOf(pool, "Fenrisian Wolf"), "Beast", "`Fenrisian Wolf` 同样是兽");
        Check(SubOf(pool, "Daemonette"), "Daemon",
              "`Daemonette` 卡面印 `Daemon`（曾是 `Troop`）—— 恶魔不是兵");
        Check(SubOf(pool, "Shivversplint"), "Combat Elixir",
              "`Shivversplint` 卡面印 `Combat Elixir`（曾是 `Upgrade`）");
        Check(SubOf(pool, "Grim Effigy"), "Defence",
              "`Grim Effigy` 卡面印 `Defence`（曾是 `Spell`）");

        // ---- ③ 字段真的**筛得对**（不只看字段值，要看筛选结果）----
        var beasts = CreatePool.Resolve(pool, "random beast", "SpaceWolves");
        CheckTrue(HasCard(beasts.Cards, "Hunting Wolf") && HasCard(beasts.Cards, "Fenrisian Wolf"),
                  "`Create a random Beast` 现在**真能筛到**这两头狼（修之前筛不到）");

        // ---- ④ 关键词的**数值**：卡面 `Armour 1. Blast 2` ----
        // 修之前 keywords 是 `['Armour','Blast']` —— **数字没了**，`KwValue` 取 0、护甲不生效。
        CheckTrue(HasKwWithValue(pool, "War Walker", "armour", 1),
                  "`War Walker` 的 `Armour` **带上了数值 1**（修之前是光秃秃的 `Armour`）");
        Check(KwValueOf(pool, "War Walker", "armour"), 1,
              "……而且 `UnitState.KwValue(\"armour\")` 真的读得到 1（护甲从「不生效」变成「生效」）");
        CheckTrue(HasKwWithValue(pool, "Vyper", "shuriken", 3), "`Vyper` 的 `Shuriken 3`");
        CheckTrue(HasKwWithValue(pool, "Night Spinner", "blast", 3),
                  "`Night Spinner` 补上了**整个缺失**的 `Blast 3`");

        // ---- ⑤ 反例：**效果里「给别人加」的不许写进这张卡的关键词** ----
        // `Baneblade Tank` 卡面是 `Armour 2. Adjacent units have Armour 1` ——
        // 第二个 Armour 1 是**给邻居的**。算修正表时专门排掉了，这里钉住它没被误写进来。
        var bane = new UnitState(FindCard(pool, "Baneblade Tank"), false);
        Check(bane.KwValue("armour"), 2,
              "`Baneblade Tank` 自己的护甲是 **2** —— 不是邻居那 1 点（那是效果给的）");
    }

    /// <summary>按卡名取 subtype（找不到返回 `(找不到)`，让断言当场显示出来）</summary>
    static string SubOf(IReadOnlyList<CardDef> pool, string name)
    {
        var c = FindCard(pool, name);
        return c == null ? "(找不到该卡)" : c.Subtype;
    }

    static CardDef FindCard(IReadOnlyList<CardDef> pool, string name)
    {
        foreach (var c in pool) if (c != null && c.Name == name) return c;
        return null;
    }

    /// <summary>
    /// 这张卡的关键词 `<词>` **带没带上数值** —— 走引擎真正读的那条路
    /// （`CardDef.Keywords` 是**解析过**的：`"Armour 1"` → 键 `armour`、值 `1`）。
    /// ⚠️ 别去比对原始字符串 —— 卡表里存的是 `"Armour 1"`，引擎读的是 `Keywords["armour"]`，
    ///    两处对不上的时候只有后者算数。
    /// </summary>
    static bool HasKwWithValue(IReadOnlyList<CardDef> pool, string name, string kw, int val)
    {
        return KwValueOf(pool, name, kw) == val;
    }

    /// <summary>走**引擎真正用的那条路**读数值关键词 —— 不是字符串比对，是 `UnitState.KwValue`</summary>
    static int KwValueOf(IReadOnlyList<CardDef> pool, string name, string kw)
    {
        var c = FindCard(pool, name);
        return c == null ? -1 : new UnitState(c, false).KwValue(kw);
    }

    /// <summary>
    /// `Deploy …` —— **免费把单位放进场上**（原版 `rule_core.gd:2970` / `_deploy_unit:3871`）。
    ///
    /// 和造牌同一套候选池（`CreatePool`），规格书同样是附录 B/C。三层都要验：
    ///   ① 解析（张数 / 从哪儿 / 费用区间 / `and` 尾句）；
    ///   ② 候选池和附录 B 的「几种」、附录 C 的名单对上；
    ///   ③ 结算（真打一张原版卡，单位**真的到了场上**、槽位对、疲劳对、满场时不硬塞）。
    /// </summary>
    static void TestDeploy()
    {
        var pool = CardDatabase.Load();

        // ---- ① 解析 ----
        {
            var op = OneOp("Deploy a Battle Sister");
            Check(op.Verb, "deploy", "`Deploy …` → 动词 deploy");
            Check(op.Amount, 1, "`a` → 1 个");
            Check(op.Payload, "battle sister", "部署什么存进 Payload");
            Check(op.DeployFrom, "pool", "默认从**卡池**找");

            // ⚠️ 这句**产出两条** op（部署 + 尾句 give），所以不能用 `OneOp`（它要求恰好 1 条）
            {
                var rr = EffectText.ParseSegment("Deploy three Tempestus Scion and give them Vanguard");
                Check(rr.Kind, EffectText.SegKind.Ok, "`Deploy … and give them Vanguard` 整句解析干净");
                Check(rr.Ops.Count, 2, "拆出 2 条：部署 + 尾句（尾句**不丢**）");
                Check(rr.Ops[0].Verb, "deploy", "第 1 条是部署");
                Check(rr.Ops[0].Amount, 3, "`three` → 3 个");
                Check(rr.Ops[1].Verb, "give", "第 2 条是尾句的 give");
                Check(rr.Ops[1].Target.Side, "prev", "尾句的 `them` 指**刚部署的那批**（不是「己方全体」）");
                Check(rr.Ops[1].Target.Count, 0, "`them` 是复数 → 一批（`it` 才是 1 个）");
            }

            op = OneOp("Deploy 4 random troops from your deck");
            Check(op.DeployFrom, "deck", "`from your deck` → 从牌库");
            CheckTrue(op.Random, "`random` 记下来了");
            Check(op.Payload, "troops", "`from your deck` 从 Payload 里剥掉");

            op = OneOp("Deploy up to 5 friendly Infantry troops that died this game");
            Check(op.DeployFrom, "graveyard", "`that died this game` → 从弃牌堆");
            CheckTrue(op.UpTo, "`up to` 记下来了（至多 N，不够就不凑）");
            Check(op.Amount, 5, "`up to 5` → 5");

            op = OneOp("Deploy 8 random Ork Infantry that cost 4 or less");
            Check(op.CostMax, 4, "`that cost 4 or less` → 上界 4");
            Check(op.CostMin, 0, "……下界不限");
            Check(op.Payload, "ork infantry", "费用从句从 Payload 里剥掉");

            op = OneOp("Deploy an Ultramarines troop that costs 6 or more");
            Check(op.CostMin, 6, "`that costs 6 or more` → 下界 6");
            Check(op.CostMax, 0, "……上界不限");

            op = OneOp("Deploy 4 random 2-cost Leviathan troops");
            Check(op.CostMin, 4 > 0 ? 2 : 2, "`2-cost` → 下界 2");
            Check(op.CostMax, 2, "`2-cost` → **上界也是 2**（恰好 2 费，不是「≤2」——见实证）");
            Check(op.Payload, "leviathan troops", "`2-cost` 从 Payload 里剥掉");
        }

        // ---- ② 候选池：卡池实算 vs 附录 B/C ----
        {
            // 附录 B「装甲攻势…」那类已在 `TestCreate` 验过，这里只验 deploy 独有的三类
            // 附录 B「次元裂隙：3 个随机 2 费部队（**6 种**，各 3 张，上限 36）」
            var r = PoolOf(pool, "Deploy 3 random 2-cost Sautekh troops",
                           "Sautekh", unitsOnly: true, costMin: 2, costMax: 2);
            Check(r.Cards.Count, 6, "Sautekh 恰好 2 费的部队 6 张 == 附录 B 的「6 种」");
            CheckAll(r.Cards, c => c.IsUnit && c.Cost == 2, "池子里每一个都是 2 费单位");

            // 附录 B「不屈远征：1 个 6 费+ 极限战士部队（**25 种**，各 1 张）」
            r = PoolOf(pool, "Deploy an Ultramarines troop that costs 6 or more",
                       "Ultramarines", unitsOnly: true, costMin: 6);
            Check(r.Cards.Count, 25, "Ultramarines 6 费及以上的部队 25 张 == 附录 B 的「25 种」");

            // 附录 B「火箭入侵：至多 8 个 4 费及以下步兵（24 种）」
            // ⚠️ 2026-09-13：这里原来写 23、注释「附录 B 写 24，**差 1**」。差的那张是
            //    **`Banner Nob`**（Goff，4 费）—— 它在 OCR 源表里 subtype 是 `unit`，
            //    **卡面印的是 `Infantry`**（卡面逐张核对发现，见 `CARD_FACE_FIXES_SRC`）。
            //    修掉之后与附录 B 的 24 **完全吻合**。
            r = PoolOf(pool, "Deploy 8 random Ork Infantry that cost 4 or less",
                       "Goff", unitsOnly: true, costMax: 4);
            Check(r.Cards.Count, 24, "Goff 4 费及以下步兵 24 张 == 附录 B 的「24 种」（2026-09-13 起全中）");
            CheckAll(r.Cards, c => c.Faction == "Goff" && c.Subtype == "Infantry" && c.Cost <= 4,
                     "……而且每一张都是高夫步兵且 ≤4 费");

            // `Genestealer Cults` → `Genestealers` 别名
            r = PoolOf(pool, "Deploy 2 random 2-cost Genestealer Cults troops",
                       "Genestealers", unitsOnly: true, costMin: 2, costMax: 2);
            Check(r.Cards.Count, 5, "`Genestealer Cults` 认得出（别名表），恰好 2 费 5 张 == 附录 C 的 1d5");

            // `friendly Infantry troops` —— **两个词都是兵种词**，`Infantry` 才是筛选条件。
            // ⚠️ 旧写法只看最后 1–2 个词当兵种词，于是 `Infantry` 被当成**阵营名**（对不上 →
            //    落回施放者阵营），兵种筛选**静默丢掉**。断言**筛出来的池子**，别断言日志措辞。
            r = PoolOf(pool, "Deploy 8 random Ork Infantry that cost 4 or less", "Goff", unitsOnly: true);
            CheckAll(r.Cards, c => c.Subtype == "Infantry",
                     "`Infantry troops`/`Ork Infantry` 里的 `Infantry` 真的当兵种筛了");

            // 部署只要**单位**：战术卡不该进池子
            r = PoolOf(pool, "Deploy a Sabotage", "Genestealers", unitsOnly: true);
            CheckTrue(!r.Ok, "「部署一张破坏（战术卡）」被拒 —— 部署要的必须是单位卡");
        }

        // ---- ③ 结算 ----
        {
            // `Deploy a Battle Sister` 出自哪张卡都行，用合成卡把变量压到最少
            var tac = Tactic("T_Deploy", 1, "Deploy a Battle Sister");
            var ctx = BattlePool(new[] { tac }, new[] { Unit("X", 1, 1, 5) }, pool,
                                 warlordFaction: "Sororitas");
            ToP1Turn(ctx, 1);
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Deploy"), -1), RuleCodes.OK,
                      "`Deploy a Battle Sister` 打得出去");
            var b = Board(ctx, 0, 0);
            CheckTrue(b != null && b.Name == "Battle Sister", "单位**真的到了场上**（槽 0）");
            CheckTrue(b.Exhausted, "部署当回合**疲劳**（Battle Sister 没有迅捷/侧翼）");
            CheckTrue(b.Card != null && b.Card.Faction == "Sororitas", "来的是卡池里那张真卡");

            // 张数：`Deploy two Storm Guardian`
            var tac2 = Tactic("T_Deploy2", 1, "Deploy two Storm Guardian");
            var ctx2 = BattlePool(new[] { tac2 }, new[] { Unit("X", 1, 1, 5) }, pool,
                                  warlordFaction: "SaimHann");
            ToP1Turn(ctx2, 1);
            RuleCore.PlayTactic(ctx2, 0, HandIdx(ctx2, 0, "T_Deploy2"), -1);
            int onBoard = 0;
            for (int s = 0; s < RuleEngine.BoardSpec.Size; s++)
                if (Board(ctx2, 0, s) != null && Board(ctx2, 0, s).Name == "Storm Guardian") onBoard++;
            Check(onBoard, 2, "`two` → 场上真的多了 2 个");

            // `and give them Vanguard` 的尾句必须一起结算（丢了就是静默失效）
            var tac3 = Tactic("T_Deploy3", 1, "Deploy a Battle Sister and give it Flank");
            var ctx3 = BattlePool(new[] { tac3 }, new[] { Unit("X", 1, 1, 5) }, pool,
                                  warlordFaction: "Sororitas");
            ToP1Turn(ctx3, 1);
            RuleCore.PlayTactic(ctx3, 0, HandIdx(ctx3, 0, "T_Deploy3"), -1);
            var b3 = Board(ctx3, 0, 0);
            CheckTrue(b3 != null && b3.Has("flank"), "`and give it Flank` 的尾句结算了（侧翼挂上了）");

            // `from your deck`：牌库里的那张要**移走**，不能同时留在牌库。
            // ⚠️ 夹具注意：`DeckOf` 把 hand 数组**倒着**放到牌库末尾，起手 3 张从末尾抽 ——
            //    所以要让 `deckSister` 留在牌库，就得给它前面垫 3 张（第一版没垫，它被起手抽进手牌了，
            //    于是「从牌库部署」当然找不到它，测试红而代码是对的）。
            var tac4 = Tactic("T_Deploy4", 1, "Deploy a Battle Sister from your deck");
            var deckSister = CardDatabase.Find(pool, "Battle Sister");
            // 起手 3 张 + 第一个回合开始再抽 1 张 = **抽走 4 张**；`DeckOf` 从 hand[0] 起按顺序被抽，
            // 所以 `deckSister` 要放在 hand[4] 之后才留得住（第一版放在 hand[3]，正好被第 4 抽抽走）
            var ctx4 = BattlePool(new[] { tac4, Unit("pad1", 1, 0, 1), Unit("pad2", 1, 0, 1),
                                          Unit("pad3", 1, 0, 1), Unit("pad4", 1, 0, 1), deckSister },
                                  new[] { Unit("X", 1, 1, 5) }, pool, warlordFaction: "Sororitas");
            ToP1Turn(ctx4, 1);
            int before = ctx4.Players[0].Deck.Count;
            CheckTrue(HandIdx(ctx4, 0, "Battle Sister") < 0, "夹具：那张 Battle Sister 现在在**牌库**里");
            RuleCore.PlayTactic(ctx4, 0, HandIdx(ctx4, 0, "T_Deploy4"), -1);
            Check(ctx4.Players[0].Deck.Count, before - 1, "从牌库部署后，那张牌**离开了牌库**");
            CheckTrue(HandIdx(ctx4, 0, "Battle Sister") < 0, "……也没跑到手牌里去");

            // **满场**：部署不下就不硬塞（`_deploy_unit` 的语义），而且要说出来
            var tac5 = Tactic("T_Deploy5", 1, "Deploy two Storm Guardian");
            var ctx5 = BattlePool(new[] { tac5 }, new[] { Unit("X", 1, 1, 5) }, pool,
                                  warlordFaction: "SaimHann");
            ToP1Turn(ctx5, 1);
            for (int s = 0; s < RuleEngine.BoardSpec.Size; s++)
                if (s != RuleEngine.BoardSpec.WarlordSlot) Place(ctx5, 0, s, Unit("Blocker" + s, 1, 1, 1));
            RuleCore.PlayTactic(ctx5, 0, HandIdx(ctx5, 0, "T_Deploy5"), -1);
            CheckTrue(ctx5.Events.Exists(e => e.Contains("没空格")), "满场 → 日志里说「没空格了」");
            int blockers = 0;
            for (int s = 0; s < RuleEngine.BoardSpec.Size; s++)
                if (Board(ctx5, 0, s) != null && Board(ctx5, 0, s).Name.StartsWith("Blocker")) blockers++;
            Check(blockers, 8, "满场时**没有挤掉任何一个**原有的单位");

            // 同种子可复现
            var tac6 = Tactic("T_Deploy6", 1, "Deploy 2 random 2-cost Sautekh troops");
            var cA = BattlePool(new[] { tac6 }, new[] { Unit("X", 1, 1, 5) }, pool, warlordFaction: "Sautekh");
            var cB = BattlePool(new[] { tac6 }, new[] { Unit("X", 1, 1, 5) }, pool, warlordFaction: "Sautekh");
            ToP1Turn(cA, 1); ToP1Turn(cB, 1);
            RuleCore.PlayTactic(cA, 0, HandIdx(cA, 0, "T_Deploy6"), -1);
            RuleCore.PlayTactic(cB, 0, HandIdx(cB, 0, "T_Deploy6"), -1);
            bool same = true;
            for (int s = 0; s < RuleEngine.BoardSpec.Size; s++)
            {
                var x = Board(cA, 0, s); var y = Board(cB, 0, s);
                if ((x == null) != (y == null)) { same = false; break; }
                if (x != null && x.Name != y.Name) { same = false; break; }
                // 池子是 6 张、只抽 2 张 —— 必须**不重复**
            }
            CheckTrue(same, "同一个种子部署出同样的单位");
            var names = new List<string>();
            for (int s = 0; s < RuleEngine.BoardSpec.Size; s++)
                if (Board(cA, 0, s) != null && Board(cA, 0, s).Name != "FixtureWarlord")
                    names.Add(Board(cA, 0, s).Name);
            Check(names.Count, 2, "`Deploy 2 random …` 部署了 2 个");
            CheckTrue(names[0] != names[1], "一次效果里**不重复**（池子 6 张只抽 2 张）");

            // **不触发 Rally**：规则书 :200 写的是「**从手牌**部署后触发」，
            // 原版也只在 play_card 那条路上触发（`rule_core.gd:2314`）—— 免费部署不触发。
            var rally = new CardDef("RallyGirl", "RallyGirl", "unit",
                                    "", null, "Sororitas", 1, 1, 3, 0,
                                    new[] { "Rally: Draw a card" });
            var tac7 = Tactic("T_Deploy7", 1, "Deploy a Battle Sister");
            var ctx7 = BattlePool(new[] { tac7 }, new[] { Unit("X", 1, 1, 5) }, pool,
                                  warlordFaction: "Sororitas");
            ToP1Turn(ctx7, 1);
            // 把卡池换成带 Rally 的那张，验证「部署了但它不触发」
            var poolR = new List<CardDef>(pool); poolR.Add(rally);
            ctx7.CardPool = poolR;
            int handBefore = ctx7.Players[0].Hand.Count;
            RuleCore.PlayTactic(ctx7, 0, HandIdx(ctx7, 0, "T_Deploy7"), -1);
            Check(ctx7.Players[0].Hand.Count, handBefore - 1,
                  "免费部署**不触发 Rally**（手牌没多 —— 规则书 :200「从手牌部署后」）");
        }
    }

    /// <summary>
    /// **按阵营的战术卡覆盖率** —— 做某个阵营时，这就是工作清单。
    ///
    /// 为什么要单开一层：总数（324/449）看不出「这个阵营还差什么」。
    /// 按阵营切之后，每个阵营缺的**句子**能列出来，那是可以直接开工的清单。
    /// </summary>
    static void ReportFactionCoverage(List<CardDef> pool)
    {
        var facs = CardDatabase.Factions(pool);
        var sb = new StringBuilder();
        sb.AppendLine("# 按阵营的战术卡覆盖率（自动生成，别手改）");
        sb.AppendLine();
        sb.AppendLine("由 `RuleEngineTest.ReportFactionCoverage` 每次跑自检时重写。");
        sb.AppendLine();
        sb.AppendLine("| 阵营 | 战术卡 | 完全解析 | 载荷有机制 | 打不出去的句子（去重） |");
        sb.AppendLine("|---|---|---|---|---|");

        foreach (string fac in facs)
        {
            var mine = new List<CardDef>();
            foreach (var c in pool) if (c != null && c.Faction == fac) mine.Add(c);

            int tac = 0, full = 0, mech = 0;
            var blockers = new Dictionary<string, int>();
            foreach (var c in mine)
            {
                if (c.Type != "tactic") continue;
                tac++;
                var ops = EffectText.Parse(c.Desc, out var un, out var pa);
                bool ok = un.Count == 0 && pa.Count == 0;
                if (ok) full++;
                bool m = ok;
                if (ok)
                    foreach (var op in ops)
                    {
                        if (!RuleCore.ImplementedEffectVerbs.Contains(op.Verb)) { m = false; Bump(blockers, "动词 " + op.Verb); }
                        else if (!string.IsNullOrEmpty(op.Condition) && op.ConditionKind.Length == 0)
                        { m = false; Bump(blockers, "条件 " + op.Condition); }
                        else if (op.Target != null && op.Target.KindUnfilterable) { m = false; Bump(blockers, "兵种过滤 " + op.Target.Raw); }
                        else if (!string.IsNullOrEmpty(op.Payload) && (op.Verb == "give" || op.Verb == "gain" || op.Verb == "lose"))
                        {
                            string why;
                            if (!GivePayload.Mechanized(op.Payload, out why)) { m = false; Bump(blockers, why + " " + op.Payload); }
                        }
                    }
                if (m) mech++;
                foreach (string u in un) Bump(blockers, u);
                foreach (string u2 in pa) Bump(blockers, u2);
            }

            var top = new List<KeyValuePair<string, int>>(blockers);
            top.Sort((x, y) => y.Value.CompareTo(x.Value));
            var list = new List<string>();
            for (int i = 0; i < top.Count && i < 14; i++) list.Add(top[i].Key + "×" + top[i].Value);

            sb.AppendLine($"| {fac} | {tac} | {full} | {mech} | {string.Join("<br>", list.ToArray())} |");
            Debug.Log(P + $"   [{fac,-17}] 战术卡 {tac,3} · 完全解析 {full,3} · 载荷有机制 {mech,3} · 卡点 {top.Count}");
        }

        const string path = "d:/4/_tmp_view/tactic_by_faction.md";
        System.IO.File.WriteAllText(path, sb.ToString(), System.Text.Encoding.UTF8);
        Debug.Log(P + "   按阵营的清单写到 " + path);
    }

    static void Bump(Dictionary<string, int> d, string k)
    {
        int n;
        d[k] = d.TryGetValue(k, out n) ? n + 1 : 1;
    }

    /// <summary>
    /// **效果分类表** —— 449 张战术卡按「动词 → 载荷类别 → 目标」分堆，落盘到
    /// `_tmp_view/tactic_taxonomy.md`，并在日志里报每一堆的量。
    ///
    /// 为什么要它有：这套分类一直**只活在 `Dispatch` 的 handler 顺序里**，没有一处写下来。
    /// 于是「加新效果该放哪一格」只能靠读代码推。写成表之后，缺格、混格、以及
    /// 「哪些格子已经满了、哪些还是空的」一眼能看出来。
    /// </summary>
    /// <summary>
    /// **单位卡的 `desc` 里那些效果文字，拿 `EffectText` 解析能认多少** —— 2026-09-13 查证。
    ///
    /// **为什么要先查这个**：`CardDef` 只解析 `keywords` 里带 `:` 的那几条（走 `EffectSpec` 的
    /// **封闭文法**，只有 Damage/Heal/Draw），**`Desc` 从来没进过 `EffectText`** ——
    /// 所以那批「卡面写着效果、打起来完全不生效」的单位卡是**静默的**。
    /// 交接文档要求「动手前先花半轮查证 desc 该不该由 `EffectText` 解析」，这一节就是那个数。
    ///
    /// ⚠️ **只报数、不断言** —— 它回答的是「值不值得接」，不是「接得对不对」。
    ///    落盘到 `_tmp_view/unit_desc_unparsed.txt`（和战术卡那份分开）。
    /// </summary>
    static void ReportUnitDescCoverage()
    {
        var pool = CardDatabase.Load();
        var sb = new StringBuilder();
        sb.AppendLine("**单位卡 / 督军卡的 desc 拿 EffectText 解析** —— 未覆盖清单（2026-09-13 查证）");
        sb.AppendLine();
        foreach (string t in new[] { "unit", "hero" })
        {
            var cov = EffectText.Coverage(pool, t, pool);
            if (cov.Cards == 0) continue;
            Debug.Log(P + $"   [{t}] " + cov.Summary());
            sb.AppendLine($"## [{t}] " + cov.Summary());
            sb.AppendLine();
            Append(sb, $"① 完全不认识的句子 —— [{t}]", cov.UnknownFreq);
            Append(sb, $"② 半懂（句型认了、词表里没有）—— [{t}]", cov.PartialFreq);
            sb.AppendLine($"### 完全解析不了的卡 —— [{t}]");
            sb.AppendLine("   " + string.Join("、", cov.NoneCards));
            sb.AppendLine();

            var top = new List<KeyValuePair<string, int>>(cov.UnknownFreq);
            top.Sort((a, b) => b.Value.CompareTo(a.Value));
            for (int i = 0; i < top.Count && i < 8; i++)
                Debug.Log(P + $"     ×{top[i].Value,-3} {top[i].Key}");
        }
        const string path = "d:/4/_tmp_view/unit_desc_unparsed.txt";
        System.IO.File.WriteAllText(path, sb.ToString(), System.Text.Encoding.UTF8);
        Debug.Log(P + "   全量清单写到 " + path);

        // ---- 第二层：**收到了正文的触发**（2026-09-13 新开的那条路）----
        // ⚠️ 上面那个 `Coverage` 量的是「把整条 desc 当战术卡解析」，**带 `Rally:` 前缀**，
        //    所以它认不出 —— 那条路和这次做的**不是同一件事**。真正要盯的是这个数：
        //    「有几张卡的触发**真的收到了正文**」（`CardDef.TriggerOps`）。
        int cardsWithOps = 0, opsCount = 0;
        var byKw = new Dictionary<string, int>();
        var sample = new List<string>();
        foreach (var c in pool)
        {
            int n = 0;
            foreach (var kv in c.TriggerTexts)
            {
                n++; opsCount++; Bump(byKw, kv.Key);
                if (sample.Count < 8) sample.Add($"{c.Name} [{kv.Key}] {kv.Value}");
            }
            if (n > 0) cardsWithOps++;
        }
        Debug.Log(P + $"   ★ 触发式效果**收到了正文**的卡：{cardsWithOps} 张 / 共 {opsCount} 条");
        var topK = new List<KeyValuePair<string, int>>(byKw);
        topK.Sort((a, b) => b.Value.CompareTo(a.Value));
        Debug.Log(P + "     按触发点：" + string.Join(" · ",
                  topK.ConvertAll(k => k.Key + "×" + k.Value).ToArray()));
        foreach (string s in sample) Debug.Log(P + "     · " + s);
    }

    ///
    /// <summary>
    /// **事件层**（`When &lt;事件&gt;, &lt;正文&gt;`）—— 2026-09-13 第三十二轮。
    ///
    /// 这一层分**上下两截**，两截都要钉：
    ///   ① **认事件**（`Core/WhenEvent.cs` 解析卡面）—— 认不出就返回 null，**绝不降级**
    ///   ② **发事件**（`EffectResolver.BroadcastWhen` + 四处广播点）—— 没人广播的话
    ///      监听器一辈子不响，**而卡面照样不打 `*`**（静默失效）
    /// 只钉 ① 会漏掉 ② —— 那正是本工程最怕的形状。所以这里**每条都钉到「局面真的变了」**。
    ///
    /// ⚠️ **极性**是这一层最容易反的地方（`When an enemy dies` vs `When a friendly troop dies`
    /// 事件种类完全一样，差别只在归属）—— 所以每条都**正反各钉一次**：
    /// 该触发的变了、**不该触发的没变**。第三十轮的教训：只钉「该生效的生效了」钉不住筛错。
    /// </summary>
    static void TestWhenEvents()
    {
        // ---- ① 解析器本身：**极性不许在解析期被吃掉** ----
        {
            var e1 = WhenEvents.Parse("an enemy dies");
            CheckTrue(e1 != null, "`an enemy dies` 认得出（第一版「先剥谁再判事」会把它剥成裸动词 `dies`）");
            if (e1 != null)
            {
                Check(e1.Kind, WhenEventKind.Die, "事件种类 = die");
                Check(e1.OwnerIs, WhenEvent.RelEnemy,
                      "**极性保留下来了**（`enemy` 被剥掉的话这里会是 -1 = 不限，那就是静默放宽）");
            }

            var e2 = WhenEvents.Parse("a friendly troop dies");
            CheckTrue(e2 != null, "`a friendly troop dies` 认得出");
            if (e2 != null)
            {
                Check(e2.Kind, WhenEventKind.Die, "事件种类 = die");
                Check(e2.OwnerIs, WhenEvent.RelFriendly, "极性 = 友方");
                CheckTrue(e2.Criteria != null && e2.Criteria.KindWord == "troop",
                          "兵种筛选 `troop` 记下来了");
            }

            var e3 = WhenEvents.Parse("you deploy a Vehicle");
            CheckTrue(e3 != null, "`you deploy a Vehicle` 认得出");
            if (e3 != null)
            {
                Check(e3.Kind, WhenEventKind.Deploy, "事件种类 = deploy");
                CheckTrue(e3.Criteria != null && e3.Criteria.KindWord == "vehicle", "兵种筛选 `vehicle`");
            }

            // ⚠️ **认不出的事件必须返回 null** —— 降级成「任意事件」= 那件事一发生就乱触发
            CheckTrue(WhenEvents.Parse("you collect a Spirit Stone") == null,
                      "认不出的事件短语返回 null（**不许降级成「任意事件」**）");
            CheckTrue(WhenEvents.Parse("deployed") == null,
                      "**省主语**的 `When deployed` 也返回 null —— 它的正确语义是「就是它自己」，"
                      + "收成「任何单位部署」就是「打得比卡面宽」（宁可收不到，不许乱触发）");
        }

        // ---- ② 单位卡上的监听器：**事件发生 → 局面真的变了**，且**极性正确** ----
        {
            var pool = CardDatabase.Load();
            // 监听器本体：友方 troop 死 → 自己 +3 攻（钉「真的改了数值」，不只看日志）
            var watcher = new CardDef("FixtureWatcher", "FixtureWatcher", "unit",
                                      "When a friendly troop dies, gain +3 Attack",
                                      "common", "Test", 1, 2, 9, 0, null, subtype: "Infantry");
            // 炮灰 1 血，一击就死
            var fodder = new CardDef("FixtureFodder", "FixtureFodder", "unit", "", "common", "Test",
                                     1, 1, 1, 0, null, subtype: "Infantry");

            CheckTrue(watcher.WhenTriggers.Count == 1,
                      "卡表加载时**监听器被收下来了**（`CardDef.CollectWhenTriggers`）");

            // ---- ②-a **友方** troop 死 —— 该触发 ----
            {
                var foe = new CardDef("FixtureFoe", "FixtureFoe", "unit", "", "common", "Test",
                                      1, 1, 1, 0, null, subtype: "Infantry");
                var ctx = BattlePool(new[] { watcher, foe }, new[] { Unit("EFoe", 1, 1, 1) },
                                     pool, warlordFaction: "Ultramarines");
                ToP1Turn(ctx, 4);
                var w = Place(ctx, 0, 0, watcher, exhausted: true);
                Place(ctx, 0, 1, foe, exhausted: true);
                Place(ctx, 1, 1, Unit("FixtureEnemyKiller", 1, 1, 1), exhausted: true);

                // ⚠️ **不能拿自己人当靶子** —— `IsValidTarget` 禁止（实测返回 `ErrTarget`）。
                //    改用「**敌方单位来打死我方的炮灰**」—— 这才是「友方 troop 死」的真实来源。
                //    ⚠️ 还得**换到对手的回合**（`ErrNotTurn = 4`）：监听器是**被动的**，
                //       它该在「事件发生」时触发，**不管现在是谁的回合** —— 这正是要验的。
                PassTurn(ctx);
                Check(ctx.Active, 1, "换到对手的回合");
                CheckCode(RuleCore.DeclareAttack(ctx, 1, 1, 0, 1), RuleCodes.OK, "对手打死我方炮灰");
                Check(SlotOf(ctx, 0, "FixtureFoe"), -1, "**友方**炮灰确实死了");
                Check(w.Attack, 5,
                      "★ **友方 troop 死 → 监听器真的结算了**（攻 2 → **5**）—— "
                      + "而且是在**对手的回合**里触发的（被动监听不该挑回合）");
            }

            // ---- ②-b **敌方**单位死 —— 监听的是「**友方** troop 死」，所以**不该**触发 ----
            {
                var own = new CardDef("FixtureOwn", "FixtureOwn", "unit", "", "common", "Test",
                                      1, 1, 1, 0, null, subtype: "Infantry");
                var ctx = BattlePool(new[] { watcher, own },
                                     new[] { Unit("EFoe", 1, 1, 9), Unit("FixtureEnemyFodder", 1, 1, 1) },
                                     pool, warlordFaction: "Ultramarines");
                ToP1Turn(ctx, 4);
                var w = Place(ctx, 0, 0, watcher, exhausted: true);
                Place(ctx, 0, 1, own, exhausted: true);          // 我方这张**不能死**（否则监听器该响）
                // ⚠️ **攻击方 9 血、目标 0 攻**：目标是 0 攻所以**不反击**，
                //    而攻击方就算被打也死不了 —— **这一格必须保证「只有敌方那一张死」**，
                //    否则「我方的攻击方吃反击死了」会自己触发监听器，
                //    这条断言就会以「实得 5」失败，而**失败的其实是测试自己**（实测踩到）。
                Place(ctx, 0, 2, Unit("FixtureOurKiller", 1, 9, 9), exhausted: false);
                Place(ctx, 1, 1, Unit("FixtureEnemyFodder", 1, 0, 1), exhausted: true);

                CheckCode(RuleCore.DeclareAttack(ctx, 0, 2, 1, 1), RuleCodes.OK, "我方单位打死敌方炮灰");
                Check(SlotOf(ctx, 1, "FixtureEnemyFodder"), -1, "**敌方**炮灰确实死了");
                Check(SlotOf(ctx, 0, "FixtureOwn"), 1, "我方那张还活着 —— 这次死的是**敌方**的");
                Check(SlotOf(ctx, 0, "FixtureOurKiller"), 2, "我方攻击方也活着（没吃反击 —— 目标 0 攻）");
                Check(w.Attack, 2,
                      "★ **敌方**单位死 → 监听「**友方** troop 死」的那张**不动**"
                      + "（极性反了这条就亮：会变成 5）");
            }
        }

        // ---- ③ 部署事件 + 兵种筛选：**筛错的话先炸** ----
        {
            var pool = CardDatabase.Load();
            var listener = new CardDef("FixtureDeployWatcher", "FixtureDeployWatcher", "unit",
                                       "When you deploy a Vehicle, give it +2 Attack",
                                       "common", "Test", 1, 1, 9, 0, null, subtype: "Infantry");
            var tank = new CardDef("FixtureTank2", "FixtureTank2", "unit", "", "common", "Test",
                                   1, 2, 5, 0, null, subtype: "Vehicle");
            var foot = new CardDef("FixtureFoot", "FixtureFoot", "unit", "", "common", "Test",
                                   1, 2, 5, 0, null, subtype: "Infantry");
            var ctx = BattlePool(new[] { listener, tank, foot }, new[] { Unit("EFoe", 1, 1, 9) },
                                 pool, warlordFaction: "Ultramarines");
            ToP1Turn(ctx, 4);
            Place(ctx, 0, 0, listener, exhausted: true);

            // 先部署**步兵** —— 筛的是 Vehicle，不该触发
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "FixtureFoot"), 1), RuleCodes.OK,
                      "部署一个步兵");
            var footU = Board(ctx, 0, 1);
            CheckTrue(footU != null && footU.Attack == 2,
                      "★ 部署**步兵** → 不给加成（筛错兵种这条先亮）");

            // 再部署**载具** —— 该触发
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "FixtureTank2"), 2), RuleCodes.OK,
                      "部署一个载具");
            var tankU = Board(ctx, 0, 2);
            CheckTrue(tankU != null && tankU.Attack == 4,
                      "★ 部署**载具** → 监听器把它 +2 攻（2 → 4）");
            CheckTrue(footU != null && footU.Attack == 2,
                      "先部署的那个步兵**没被回溯补上**（`deploy` 是那一刻的事）");
        }
    }

    static void DumpTaxonomy(List<CardDef> pool, EffectText.TextCoverage cov)
    {
        // 动词 → （载荷子类 → 张数）
        var byVerb = new Dictionary<string, Dictionary<string, int>>();
        var verbCards = new Dictionary<string, List<string>>();
        var targetKinds = new Dictionary<string, int>();
        var condKinds = new Dictionary<string, int>();
        int cardsWithOps = 0;

        foreach (var c in pool)
        {
            if (c == null || c.Type != "tactic") continue;
            var ops = EffectText.Parse(c.Desc, out _, out _);
            if (ops.Count == 0) continue;
            cardsWithOps++;

            var seenVerbs = new HashSet<string>();
            foreach (var op in ops)
            {
                if (!byVerb.ContainsKey(op.Verb)) { byVerb[op.Verb] = new Dictionary<string, int>(); verbCards[op.Verb] = new List<string>(); }
                string sub = PayloadClassOf(op);
                int n0;
                byVerb[op.Verb][sub] = byVerb[op.Verb].TryGetValue(sub, out n0) ? n0 + 1 : 1;
                if (seenVerbs.Add(op.Verb) && verbCards[op.Verb].Count < 4) verbCards[op.Verb].Add(c.Name);

                if (op.Target != null)
                {
                    string tk = op.Target.Side + "/" + op.Target.Kind
                              + (op.Target.Count == 0 ? "(all)" : "(n" + op.Target.Count + ")")
                              + (op.Target.SubtypeFilter != null ? "[sub:" + op.Target.SubtypeFilter + "]" : "");
                    int n1; targetKinds[tk] = targetKinds.TryGetValue(tk, out n1) ? n1 + 1 : 1;
                }
                if (!string.IsNullOrEmpty(op.ConditionKind))
                {
                    int n2; condKinds[op.ConditionKind] = condKinds.TryGetValue(op.ConditionKind, out n2) ? n2 + 1 : 1;
                }
                else if (!string.IsNullOrEmpty(op.Condition))
                {
                    int n3; string k = "(判不了) " + op.Condition;
                    condKinds[k] = condKinds.TryGetValue(k, out n3) ? n3 + 1 : 1;
                }
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine("# 战术卡效果分类表（自动生成，别手改）");
        sb.AppendLine();
        sb.AppendLine("由 `RuleEngineTest.DumpTaxonomy` 每次跑自检时重写。");
        sb.AppendLine($"战术卡 {cov.Cards} 张 · 解析出效果的 {cardsWithOps} 张 · " + cov.Summary());
        sb.AppendLine();
        sb.AppendLine("## 一、按**动词**分堆（这是结算层的分派单位）");
        sb.AppendLine();
        sb.AppendLine("| 动词 | 结算层实现了吗 | op 数 | 载荷子类 | 例 |");
        sb.AppendLine("|---|---|---|---|---|");

        var verbOrder = new List<string>(byVerb.Keys);
        verbOrder.Sort((x, y) => Total(byVerb[y]) - Total(byVerb[x]));
        foreach (string v in verbOrder)
        {
            var subs = new List<string>();
            var subOrder = new List<string>(byVerb[v].Keys);
            subOrder.Sort((x, y) => byVerb[v][y] - byVerb[v][x]);
            foreach (string sh in subOrder) subs.Add(sh + "×" + byVerb[v][sh]);
            sb.AppendLine($"| `{v}` | {(RuleCore.ImplementedEffectVerbs.Contains(v) ? "✅" : "❌ **没实现**")} "
                        + $"| {Total(byVerb[v])} | {string.Join(" · ", subs.ToArray())} | {string.Join("、", verbCards[v].ToArray())} |");
        }

        sb.AppendLine();
        sb.AppendLine("## 二、按**目标**分堆（`EffectTargetSpec` 的组合）");
        sb.AppendLine();
        var tOrder = new List<string>(targetKinds.Keys);
        tOrder.Sort((x, y) => targetKinds[y] - targetKinds[x]);
        foreach (string t in tOrder) sb.AppendLine($"- `{t}` ×{targetKinds[t]}");

        sb.AppendLine();
        sb.AppendLine("## 三、按**条件**分堆（`EffectCondition`）");
        sb.AppendLine();
        if (condKinds.Count == 0) sb.AppendLine("- （没有带条件的 op）");
        var cOrder = new List<string>(condKinds.Keys);
        cOrder.Sort((x, y) => condKinds[y] - condKinds[x]);
        foreach (string c in cOrder) sb.AppendLine($"- `{c}` ×{condKinds[c]}");

        const string path = "d:/4/_tmp_view/tactic_taxonomy.md";
        System.IO.File.WriteAllText(path, sb.ToString(), System.Text.Encoding.UTF8);

        Debug.Log(P + $"   效果分类表写到 {path}（{byVerb.Count} 个动词 / {targetKinds.Count} 类目标 / {condKinds.Count} 类条件）");
        foreach (string v in verbOrder)
            Debug.Log(P + $"     {v,-12} op {Total(byVerb[v]),-4} "
                      + (RuleCore.ImplementedEffectVerbs.Contains(v) ? "" : "← **没实现**"));
    }

    static int Total(Dictionary<string, int> d) { int n = 0; foreach (var kv in d) n += kv.Value; return n; }

    /// <summary>一个 op 的**载荷子类** —— 分类表的第二层。`give` 那一栏最需要它
    /// （属性增减益 / 关键词授予 / 嵌入效果 是三条完全不同的结算路）。</summary>
    static string PayloadClassOf(EffectOp op)
    {
        if (op.Verb == "give" || op.Verb == "gain" || op.Verb == "lose")
        {
            var ps = GivePayload.Parse(op.Payload);
            if (ps == null) return "(载荷认不出)";
            var parts = new List<string>();
            foreach (var p in ps)
                parts.Add(p.IsEmbedded ? "嵌入效果" : (p.IsKeyword ? "关键词" : "属性增减益"));
            return string.Join("+", parts.ToArray());
        }
        if (op.Verb == "deal" || op.Verb == "heal") return op.AmountMax > 0 ? "区间值" : "定值";
        if (op.Verb == "create") return op.Dest;
        if (op.Verb == "deploy") return op.DeployFrom + (op.CostMin > 0 || op.CostMax > 0 ? "+费用区间" : "");
        if (op.Verb == "chooseone") return "选项数 " + op.Amount;
        if (op.Verb == "repeat") return string.IsNullOrEmpty(op.Payload) ? "无条件" : "有条件";
        if (op.Verb == "stun" || op.Verb == "blind") return "硬控";
        return "";
    }

    /// <summary>附录 C 的骰子表 vs 卡池实算的池子。**差集两个方向都要报** ——
    /// 「书上有、卡池没有」是原版数据缺口，「卡池有、书上没列」是名字对不上或我们的筛子开宽了。</summary>
    static void CheckDiceTables(IReadOnlyList<CardDef> pool)
    {
        var cases = new[]
        {
            // 表名,              卡面原文（走真解析）,                                    施放者阵营,          附录 B 的「几种」
            new[] { "战斗药剂(1d6)",   "Create 3 random Combat Elixir in your hand",        "EmperorsChildren", "6" },
            new[] { "钛无人机(1d7)",   "Create 3 random Drones in your hand",               "TauEmpire",        "7" },
            new[] { "虫群大军(1d10)",  "Create 3 random Leviathan troops with Swarm in your hand", "Leviathan", "10" },
            new[] { "锈蚀通风口(1d11)", "Create a random troop with Ambush in your hand",    "Genestealers",     "11" },
            new[] { "狂野宿主(1d20)",  "Create two random Saim-Hann Vehicles in your hand", "SaimHann",         "20" },
            // ---- deploy 那几条（`Deploy` 和 `Create` 共用同一套候选池）----
            new[] { "次元裂隙(1d6)",   "Deploy 3 random 2-cost Sautekh troops",              "Sautekh",          "6" },
            new[] { "纵欲狂欢(1d5)",   "Deploy three random Emperor's Children Daemons that cost 5 or less",
                                                                                            "EmperorsChildren", "5" },
            new[] { "空中播种(1d5)",   "Deploy 4 random 2-cost Leviathan troops",             "Leviathan",        "5" },
        };

        foreach (var cs in cases)
        {
            string tableName = cs[0];
            string[] book = null;
            foreach (var t in CreatePool.DiceTables) if (t[0] == tableName) book = t;
            CheckTrue(book != null, $"附录 C 里有「{tableName}」这张表");

            var got = PoolOf(pool, cs[1], cs[2]);
            var gotNames = new HashSet<string>();
            foreach (var c in got.Cards) gotNames.Add(CreatePool.Norm(c.Name));
            var bookNames = new HashSet<string>();
            for (int i = 1; i < book.Length; i++) bookNames.Add(CreatePool.Norm(book[i]));

            var onlyBook = new List<string>();
            var onlyBookRaw = new List<string>();
            for (int i = 1; i < book.Length; i++)
                if (!gotNames.Contains(CreatePool.Norm(book[i]))) { onlyBook.Add(CreatePool.Norm(book[i])); onlyBookRaw.Add(book[i]); }
            var onlyData = new List<string>();
            foreach (var n in gotNames) if (!bookNames.Contains(n)) onlyData.Add(n);
            onlyBook.Sort(); onlyData.Sort();

            // 附录 B 的「N 种」是第三个尺子：卡池实算的池子该和它一样大
            // 「书上有、卡池对不上」的，顺带报出卡池里**最接近**的那个名字（多半是换个写法）
            var nearMiss = new List<string>();
            foreach (var n in onlyBookRaw)
            {
                string near = CreatePool.FindNearMiss(pool, n);
                nearMiss.Add(n + "→" + (near != null ? near : "(真没有)"));
            }

            Debug.Log(P + $"   附录C「{tableName}」：卡池算得 {gotNames.Count} 张、"
                      + $"书里列 {bookNames.Count} 个名字、附录B 写「{cs[3]} 种」"
                      + (nearMiss.Count > 0 ? $"；**书里有、卡池对不上**：{string.Join("、", nearMiss)}" : "")
                      + (onlyData.Count > 0 ? $"；**卡池有、书里没列**：{string.Join("、", onlyData)}" : ""));

            CheckTrue(gotNames.Count > 0, $"「{tableName}」的池子在卡池里算得出来");
        }

        // 上面那几处已经查出实打实的差：**如实钉在这儿**，别让它悄悄漂走。
        // 「书上有、卡池没有」= 原版数据缺口；「卡池有、书上没列」= 我们的筛子可能开宽了。
        //
        // ⚠️ 2026-09-13 更正：这一处**原来的差已经修掉了** —— 卡面逐张核对发现
        //    `Shivversplint` 的 subtype 在 OCR 源表里是 `Upgrade`，**卡面印的是 `Combat Elixir`**
        //    （见 `CARD_FACE_FIXES_SRC`）。现在附录 C 的 1d6 六个名字全在池里，
        //    所以这条断言从「钉住缺口」改成「钉住已修」。
        var elixir = CreatePool.Resolve(pool, "random combat elixir", "EmperorsChildren");
        CheckTrue(HasCard(elixir.Cards, "Shivversplint"),
                  "`Shivversplint` **在战斗药剂池里**（2026-09-13 起：subtype 已按卡面改成 `Combat Elixir`）");
        CheckTrue(HasCard(pool, "Shivversplint"), "……而且它本来就在卡池里");

        var swarm = CreatePool.Resolve(pool, "random leviathan troops with swarm", "Leviathan");
        CheckTrue(HasCard(swarm.Cards, "Tyranid Prime"),
                  "`Tyranid Prime` 带虫群、是利维坦部队，但附录 C 的 1d10 名单里没有它（卡池 11 vs 书上 10）");

        // 附录 C 的 1d11 名单里写着 `Acolyte Hybrid`，而卡池里那一格是 `Aberrant`。
        // 查清楚了才敢下结论：**`Acolyte Hybrid` 这张卡在卡池里**，只是**身上没有伏击关键词** ——
        // 所以它进不了「带伏击部队」的池子。这是**数据差异**（名字对不上 / 关键词挂在哪张卡上），
        // 不是我们筛错了。**不做别名映射**（没有第二条证据说这两张是同一张），如实钉在这儿。
        var acolyte = CreatePool.FindByName(pool, "Acolyte Hybrid");
        CheckTrue(acolyte != null, "`Acolyte Hybrid` 这张卡本身在卡池里");
        CheckTrue(!acolyte.Has("ambush"), "……但它身上**没有伏击关键词**，所以不在「带伏击部队」池里");
        CheckTrue(!HasCard(PoolOf(pool, "Create a random troop with Ambush in your hand", "Genestealers").Cards,
                           "Acolyte Hybrid"),
                  "……于是它确实没进那个池子（不是筛子漏了它）");
    }

    /// <summary>
    /// **卡面原文 → 解析 → `Payload` → 候选池**。走真路，不手写 payload ——
    /// 手写的那份和解析器产出的会悄悄不一样（第一版就是这么错的：手写成 `a gun drone, …`，
    /// 而解析器早把冠词当数量剥掉了，于是测试红、代码却是对的）。
    /// </summary>
    static CreatePoolResult PoolOf(IReadOnlyList<CardDef> pool, string cardText, string faction,
                                   bool unitsOnly = false, int costMin = 0, int costMax = 0)
    {
        // `create` 和 `deploy` 共用同一套候选池 —— 两边的卡面文本都从这儿进
        EffectOp found = null;
        foreach (string seg in EffectText.Split(cardText))
        {
            var r = EffectText.ParseSegment(seg);
            if (r.Ops == null) continue;
            foreach (var op in r.Ops)
                if (op.Verb == "create" || op.Verb == "deploy") found = op;
        }
        CheckTrue(found != null, $"「{cardText}」里解析得出 create/deploy");
        // 卡面自带的费用区间**也要带上**（别只信调用方传的）
        if (found.CostMin > costMin) costMin = found.CostMin;
        if (found.CostMax > costMax) costMax = found.CostMax;
        return CreatePool.Resolve(pool, found.Payload, faction, unitsOnly, costMin, costMax);
    }

    static bool HasCard(IEnumerable<CardDef> list, string name)
    {
        string want = CreatePool.Norm(name);
        foreach (var c in list) if (c != null && CreatePool.Norm(c.Name) == want) return true;
        return false;
    }

    static void CheckAll(List<CardDef> list, Func<CardDef, bool> ok, string msg)
    {
        foreach (var c in list)
            if (!ok(c)) { CheckTrue(false, msg + $"（不满足的是 {c.Name}）"); return; }
        CheckTrue(true, msg);
    }

    /// <summary>解析一句话，要求恰好产出 1 条 op</summary>
    static EffectOp OneOp(string seg)
    {
        var r = EffectText.ParseSegment(seg);
        Check(r.Kind, EffectText.SegKind.Ok, $"「{seg}」解析成功");
        Check(r.Ops == null ? 0 : r.Ops.Count, 1, $"「{seg}」解析出 1 条效果");
        return r.Ops[0];
    }

    /// <summary>
    /// 把**全部**未解析/半懂/缺机制的句子按频次降序落盘 → `d:/4/_tmp_view/tactic_unparsed.txt`。
    ///
    /// 为什么要落盘：日志里只印 TOP8，而「下一个该实现哪个 handler」必须**按量排**。
    /// 落 `_tmp_view/` 而不是 `Assets/`：这是排查产物，不是资产。
    /// </summary>
    static void DumpUnparsed(EffectText.TextCoverage cov)
    {
        var sb = new StringBuilder();
        sb.AppendLine("战术卡文本解析 —— 未覆盖清单（按频次降序）");
        sb.AppendLine(cov.Summary());
        sb.AppendLine();

        Append(sb, "① 完全不认识的句子（还没有 handler 认领）", cov.UnknownFreq);
        Append(sb, "② 句型认了、但目标/载荷词表里没有（半懂 —— 比不懂更危险）", cov.PartialFreq);
        Append(sb, "③ 解析得了、但载荷的关键词没有机制（能打但没用）", cov.NoMechFreq);
        // 2026-09-12：原版 `card_stats.json` 里的 `subtype`（兵种）接进来之后，
        // `a friendly Vehicle` 这类**真能筛了**（`EffectTargetSpec.SubtypeFilter` → `ResolveTargets`），
        // 所以这一栏只剩「原版数据里也没有对应兵种」的少数卡（过去是 12 张，现在 1 张）。
        Append(sb, "④ 会生效、但**打得比卡面宽**（兵种词在 `subtype` 里找不到对应值，只能按整个目标池打）",
               cov.ImpreciseFreq);

        sb.AppendLine();
        sb.AppendLine("⑤ 完全解析不了的卡（卡面该打 `*`）：");
        sb.AppendLine("   " + string.Join("、", cov.NoneCards));

        const string path = "d:/4/_tmp_view/tactic_unparsed.txt";
        System.IO.File.WriteAllText(path, sb.ToString(), System.Text.Encoding.UTF8);
        Debug.Log(P + "   全量清单写到 " + path
                  + $"（不认 {cov.UnknownFreq.Count} 种 / 半懂 {cov.PartialFreq.Count} 种 / 缺机制 {cov.NoMechFreq.Count} 种）");
    }

    static void Append(StringBuilder sb, string title, Dictionary<string, int> freq)
    {
        var list = new List<KeyValuePair<string, int>>(freq);
        list.Sort((a, b) => b.Value.CompareTo(a.Value));
        int total = 0;
        foreach (var kv in list) total += kv.Value;
        sb.AppendLine("──── " + title + $"（{list.Count} 种 / {total} 次）");
        foreach (var kv in list) sb.AppendLine($"  ×{kv.Value,-4} {kv.Key}");
        sb.AppendLine();
    }

    /// <summary>
    /// **存档卡组 → 对战**（`DeckBuilder.FromDeck`）。
    /// 卡组编辑器存的是 id；这条链就是把它展开成引擎要的卡表。
    /// </summary>
    static void TestDeckIntoBattle()
    {
        var pool = CardDatabase.Load();
        var um = CardDatabase.OfFaction(pool, "Ultramarines");

        CardDef hero = null, defence = null;
        var unitIds = new List<string>();
        foreach (var c in um)
        {
            if (hero == null && c.Type == "hero") { hero = c; continue; }
            if (defence == null && c.Type == "defence") { defence = c; continue; }
            if (c.Type == "unit" && unitIds.Count < DeckRules.ClassicCards) unitIds.Add(c.Id);
        }
        CheckTrue(hero != null && defence != null && unitIds.Count == DeckRules.ClassicCards,
                  $"选料齐了：督军 {hero?.Name}、防御卡 {defence?.Name}、单位 {unitIds.Count} 张");

        var deck = new PlayerDeck("自检套", hero.Name, defence.Name, unitIds);
        Check(DeckRules.Validate(deck, id => CardDatabase.Find(pool, id)), DeckError.None,
              "这副卡组是合法的（`DeckRules.Validate` 说了算）");

        var skipped = new List<string>();
        var cards = DeckBuilder.FromDeck(pool, deck, skipped);
        Check(cards.Count, 1 + DeckRules.ClassicCards, $"展开成 {cards.Count} 张（1 督军 + {DeckRules.ClassicCards} 单位）");
        Check(cards[0].Type, "hero", "第 0 张是督军（`RuleCore.BuildPlayer` 认这个约定）");
        // 防御卡引擎没有机制 —— 被丢掉，但**必须记下来**，不能悄悄少一张
        Check(skipped.Count, 1, "防御卡被明确记进 skipped（不静默丢）");
        Debug.Log(P + "   引擎还不支持、被丢掉的：" + string.Join("、", skipped));

        var ctx = RuleCore.NewBattle(cards, cards, seed: 4242);
        Check(ctx.Players[0].Warlord.Name, hero.Name, "开出来的局，督军就是卡组里那个");
        Check(ctx.Players[0].Deck.Count + ctx.Players[0].Hand.Count, DeckRules.ClassicCards,
              $"抽牌堆 + 手牌 = {DeckRules.ClassicCards} 张（卡一张没少）");

        // ---- 🆕 战术卡（2026-09-12 起收）：**能解析干净的收下**、解析不了的照样丢并记下来 ----
        // 判据只有一处：`DeckBuilder.TacticPlayable` → `EffectText.IsFullyParsed`
        // （和 `CanPlayTactic` 是同一份，不会出现「牌组收了、出牌又被拒」）
        var mixed = new List<string>(unitIds);
        int kept = 0, droppedTactic = 0;
        foreach (var c in um)
        {
            if (c.Type != "tactic") continue;
            bool ok = DeckBuilder.TacticPlayable(c);
            if (ok && kept < 3) { mixed.Add(c.Id); kept++; }
            else if (!ok && droppedTactic < 2) { mixed.Add(c.Id); droppedTactic++; }
        }
        CheckTrue(kept > 0, $"Ultramarines 里找得到能解析的战术卡（{kept} 张）");
        var deck2 = new PlayerDeck("自检套·混战术", hero.Name, defence.Name, mixed);
        var skipped2 = new List<string>();
        var cards2 = DeckBuilder.FromDeck(pool, deck2, skipped2);
        Check(cards2.Count, 1 + unitIds.Count + kept, $"能解析的战术卡收下了（+{kept} 张）");
        Check(skipped2.Count, 1 + droppedTactic, $"解析不了的战术 {droppedTactic} 张 + 防御 1 张 → 都记进 skipped");
        int tacticsIn = 0;
        foreach (var c in cards2) if (c.Type == "tactic") tacticsIn++;
        Check(tacticsIn, kept, "收下的确实都是战术卡");
    }

    // ==================================================================
    //  开局
    // ==================================================================

    static void TestNewBattle()
    {
        var ctx = Battle(new[] { Unit("A", 1, 1, 1), Unit("B", 1, 1, 1), Unit("C", 1, 1, 1) },
                         new[] { Unit("X", 1, 1, 1), Unit("Y", 1, 1, 1), Unit("Z", 1, 1, 1) });

        Check(ctx.Players[0].Hand.Count, 3, "起手 3 张（先手）");
        Check(ctx.Players[1].Hand.Count, 3, "起手 3 张（后手）");
        Check(ctx.Players[0].Warlord.Health, 30, "督军 30 血");
        Check(ctx.Players[0].Warlord.Attack, 2, "督军 2 攻");
        Check(ctx.Players[0].Energy, 0, "开局能量 0");
        Check(ctx.Active, 0, "先手 = 玩家 1");
        Check(ctx.Turn, 0, "还没开始算回合");

        CheckTrue(ctx.Players[0].Board[BoardSpec.WarlordSlot] == ctx.Players[0].Warlord,
                  "督军就在槽 4，且和 Warlord 是同一个对象");
        Check(Board(ctx, 0, 0), null, "格 0 空");
        Check(Board(ctx, 0, BoardSpec.WarlordSlot).Name, "FixtureWarlord",
              "督军卡被从牌组提出来当了督军（不在牌库里）");
        Check(ctx.Players[0].Deck.Count, 20, "牌库 = 24 - 督军 1 - 起手 3 = 20");

        // 手牌顺序：Deck() 反转摆放 → 抽到的顺序就是传入顺序
        Check(ctx.Players[0].Hand[0].Name, "A", "起手顺序 = 传入顺序（反转摆放生效）");
        Check(ctx.Players[0].Hand[2].Name, "C", "第 3 张也对得上");
    }

    // ==================================================================
    //  开局换牌（Mulligan）
    //
    //  原版：`_SetupMulliganPhase` → `MulliganManager.ActivateMulligan` →（玩家选完）
    //        `_FinishMulliganFirstPhase` → `_FinishMulliganFinalPhase` → `ShuffleDeck`
    //        → `PlayerHand.CompleteMulliganPhase` → `StartBattlePhase`。
    //  规则：规则书 :46「**可弃回任意起手牌后重洗补抽**」。
    // ==================================================================

    /// <summary>某一方手牌的名字（按顺序）—— 换牌的确定性断言要逐张比</summary>
    static string[] HandNames(BattleContext ctx, int p)
    {
        var names = new List<string>();
        foreach (var c in ctx.Players[p].Hand) names.Add(c.Name);
        return names.ToArray();
    }

    /// <summary>「手牌 + 牌库 + 弃牌堆」全部卡名的**多重集合签名** —— 换牌前后必须一模一样
    /// （凭空多一张或少一张，是这一块最容易出的静默错）</summary>
    static string CardsSignature(BattleContext ctx, int p)
    {
        var names = new List<string>();
        var ps = ctx.Players[p];
        foreach (var c in ps.Hand) names.Add(c.Name);
        foreach (var c in ps.Deck) names.Add(c.Name);
        foreach (var c in ps.Discard) names.Add(c.Name);
        names.Sort();
        return string.Join(",", names.ToArray());
    }

    static void TestMulligan()
    {
        var d0 = new[] { Unit("A", 1, 1, 1), Unit("B", 1, 1, 1), Unit("C", 1, 1, 1) };
        var d1 = new[] { Unit("X", 1, 1, 1), Unit("Y", 1, 1, 1), Unit("Z", 1, 1, 1) };

        var ctx = RuleCore.NewBattle(Deck(d0), Deck(d1), seed: 7, shuffle: false, openMulligan: true);
        CheckTrue(ctx.MulliganOpen, "`openMulligan: true` → 开局进换牌阶段");
        Check(ctx.Players[0].Hand.Count, 3, "换牌前：起手 3 张");
        Check(ctx.Players[0].Deck.Count, 20, "换牌前：牌库 20 张");

        string before = CardsSignature(ctx, 0);
        int hand1 = ctx.Players[0].Hand.Count, deck1 = ctx.Players[0].Deck.Count;

        int n = RuleCore.Mulligan(ctx, 0, new List<int> { 1, 2 });
        Check(n, 2, "换掉 2 张");
        Check(ctx.Players[0].Hand.Count, hand1, "**补抽了**：手牌张数不变（3 → 3）");
        Check(ctx.Players[0].Deck.Count, deck1, "牌库张数也不变（弃回 2 + 抽回 2）");
        Check(CardsSignature(ctx, 0), before, "**牌一张不多一张不少**（手牌+牌库+弃牌堆的签名不变）");

        // 越界/重复的下标：忽略，不报错也不重复删
        int m = RuleCore.Mulligan(ctx, 0, new List<int> { 0, 0, 99, -1 });
        Check(m, 1, "重复下标只算一次、越界下标被忽略（换掉 {0,0,99,-1} → 实际 1 张）");
        Check(CardsSignature(ctx, 0), before, "……牌还是不多不少");

        // 空列表 = 不换（原版「什么都不选直接继续」就是这个）
        Check(RuleCore.Mulligan(ctx, 0, new List<int>()), 0, "空列表 → 换 0 张（合法，不是错误）");

        // ⚠️ **不在换牌阶段就换不了** —— 而且必须**报出来**（返回 -1），不能静默成功
        RuleCore.EndMulligan(ctx);
        CheckTrue(!ctx.MulliganOpen, "`EndMulligan` 之后阶段关闭");
        string sig2 = CardsSignature(ctx, 0);
        int after = RuleCore.Mulligan(ctx, 0, new List<int> { 0 });
        Check(after, -1, "对局开始后再换 → 返回 **-1**（不是 0，也不是换成功）");
        Check(CardsSignature(ctx, 0), sig2, "……而且手牌一张没动");

        // 确定性：同样的种子 + 同样的换法 → 同样的结果
        var c1 = RuleCore.NewBattle(Deck(d0), Deck(d1), seed: 7, shuffle: false, openMulligan: true);
        var c2 = RuleCore.NewBattle(Deck(d0), Deck(d1), seed: 7, shuffle: false, openMulligan: true);
        RuleCore.Mulligan(c1, 0, new List<int> { 0 });
        RuleCore.Mulligan(c2, 0, new List<int> { 0 });
        Check(string.Join("/", HandNames(c1, 0)), string.Join("/", HandNames(c2, 0)),
              "同种子同换法 → 同一副手牌（换牌走的是 `ctx.Rng`，对局可复现）");
        // 默认不开：老调用方（规则自检里那一大堆）行为不变
        var plain = RuleCore.NewBattle(Deck(d0), Deck(d1), seed: 7, shuffle: false);
        CheckTrue(!plain.MulliganOpen, "不传 `openMulligan` → **不进换牌阶段**（老用例不受影响）");
    }

    // ==================================================================
    //  回合
    // ==================================================================

    static void TestBeginTurn()
    {
        var ctx = Battle(new[] { Unit("A", 1, 1, 1), Unit("B", 1, 1, 1), Unit("C", 1, 1, 1) },
                         new[] { Unit("X", 1, 1, 1), Unit("Y", 1, 1, 1), Unit("Z", 1, 1, 1) });

        RuleCore.BeginTurn(ctx);
        Check(ctx.Turn, 1, "回合 1");
        Check(ctx.Players[0].Energy, 2, "回合 1 能量 2（初始 1 + 每回合 +1）");
        Check(ctx.Players[0].Hand.Count, 4, "抽 1 张");
        Check(ctx.Players[0].TurnCount, 1, "P1 自己的回合计数 = 1");

        // 解疲劳只作用于**当前行动方**
        Place(ctx, 0, 1, Unit("Mine", 1, 1, 1), exhausted: true);
        Place(ctx, 1, 1, Unit("Theirs", 1, 1, 1), exhausted: true);

        PassTurn(ctx);            // → P2 第 1 回合
        Check(Board(ctx, 1, 1).Exhausted, false, "P2 的回合开始时，P2 的单位解疲劳");
        Check(Board(ctx, 0, 1).Exhausted, true, "P1 的单位在 P2 的回合里不解疲劳");

        PassTurn(ctx);            // → P1 第 2 回合
        Check(Board(ctx, 0, 1).Exhausted, false, "轮到 P1 时己方单位解疲劳");
        Check(ctx.Players[0].Energy, 3, "P1 第 2 回合能量 3（自己的回合数 + 1）");
    }

    static void TestEnergyIsPerPlayer()
    {
        var ctx = Battle(new[] { Unit("A", 1, 1, 1), Unit("B", 1, 1, 1), Unit("C", 1, 1, 1) },
                         new[] { Unit("X", 1, 1, 1), Unit("Y", 1, 1, 1), Unit("Z", 1, 1, 1) });

        RuleCore.BeginTurn(ctx);   // P1 第 1 回合
        Check(ctx.Players[0].Energy, 2, "P1 第 1 回合 = 2 能");
        PassTurn(ctx);             // P2 第 1 回合
        Check(ctx.Players[1].Energy, 2, "P2 第 1 回合也是 2 能（**不是 3**）");
        Check(ctx.Players[1].TurnCount, 1, "P2 自己的回合计数 = 1");
        // ⚠️ 这条最容易写错：按**全局回合数**算的话，后手首回合会变成 2 能、先手第 2 回合变成 3 能，
        //    整局能量曲线偏高。rule_core.gd 记着这条修正。
    }

    // ==================================================================
    //  出牌
    // ==================================================================

    static void TestPlayCostRejected()
    {
        var ctx = Battle(new[] { Unit("Cheap", 2, 1, 1), Unit("Pricey", 5, 5, 5), Unit("Free", 0, 1, 1) },
                         new[] { Unit("X", 1, 1, 1), Unit("Y", 1, 1, 1), Unit("Z", 1, 1, 1) });
        ToP1Turn(ctx, 1);
        Check(ctx.Players[0].Energy, 2, "手上有 2 能");

        int handBefore = ctx.Players[0].Hand.Count;
        int code = RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "Pricey"), 1);
        CheckCode(code, RuleCodes.ErrCost, "5 费卡在 2 能时被拒绝");
        Check(ctx.Players[0].Energy, 2, "拒绝后能量不变");
        Check(ctx.Players[0].Hand.Count, handBefore, "拒绝后手牌不变");
        Check(Board(ctx, 0, 1), null, "拒绝后格位仍空");
    }

    static void TestPlaySlotRejected()
    {
        var ctx = Battle(new[] { Unit("Cheap", 2, 1, 1), Unit("Free", 0, 1, 1), Unit("Free2", 0, 1, 1) },
                         new[] { Unit("X", 1, 1, 1), Unit("Y", 1, 1, 1), Unit("Z", 1, 1, 1) });
        ToP1Turn(ctx, 1);

        CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "Cheap"), BoardSpec.WarlordSlot),
                  RuleCodes.ErrSlot, "督军格不可部署");
        Check(ctx.Players[0].Energy, 2, "非法格不扣费");
        Check(ctx.Players[0].Hand.Count, 4, "非法格不弃牌");

        CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "Cheap"), -1),
                  RuleCodes.ErrSlot, "格位 -1 被拒绝");
        CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "Cheap"), BoardSpec.Size),
                  RuleCodes.ErrSlot, "格位 9（越界）被拒绝");
        Check(ctx.Players[0].Energy, 2, "越界也不扣费");
    }

    static void TestPlayOccupiedRejected()
    {
        var ctx = Battle(new[] { Unit("Free", 0, 1, 1), Unit("Free2", 0, 1, 1), Unit("Free3", 0, 1, 1) },
                         new[] { Unit("X", 1, 1, 1), Unit("Y", 1, 1, 1), Unit("Z", 1, 1, 1) });
        ToP1Turn(ctx, 1);

        CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "Free"), 1), RuleCodes.OK, "格 1 部署");
        CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "Free2"), 1),
                  RuleCodes.ErrSlot, "被占格拒绝（0 费卡，排除了费用干扰）");

        // 全 9 格只有督军格不能放，其余 8 格都能放
        Check(BoardSpec.SlotsPerSide * 2, 8, "两侧共 8 个可部署格");
    }

    static void TestPlayOk()
    {
        var ctx = Battle(new[] { Unit("Grunt", 2, 3, 4), Unit("F", 1, 1, 1), Unit("G", 1, 1, 1) },
                         new[] { Unit("X", 1, 1, 1), Unit("Y", 1, 1, 1), Unit("Z", 1, 1, 1) });
        ToP1Turn(ctx, 2);

        Check(ctx.Players[0].Energy, 3, "P1 第 2 回合 3 能");
        int handBefore = ctx.Players[0].Hand.Count;
        int idx = HandIdx(ctx, 0, "Grunt");
        CheckCode(RuleCore.PlayCard(ctx, 0, idx, 1), RuleCodes.OK, "2 费单位在 3 能时可部署");
        Check(ctx.Players[0].Energy, 1, "能量 3-2=1");

        var u = Board(ctx, 0, 1);
        CheckTrue(u != null, "格 1 有单位了");
        Check(u.Name, "Grunt", "上去的是 Grunt");
        Check(u.Health, 4, "满血上场（不是卡面血量以外的值）");
        Check(u.Exhausted, true, "部署当回合不可行动");
        Check(ctx.Players[0].Hand.Count, handBefore - 1, "手牌少了一张");

        // CanPlayCard 和 PlayCard 必须是同一份判据（表现层拖拽时要实时问它）
        CheckCode(RuleCore.CanPlayCard(ctx, 0, HandIdx(ctx, 0, "F"), 1), RuleCodes.ErrSlot,
                  "CanPlayCard 和 PlayCard 判据一致（被占格）");
        ctx.Players[0].Energy = 0;
        CheckCode(RuleCore.CanPlayCard(ctx, 0, HandIdx(ctx, 0, "F"), 2), RuleCodes.ErrCost,
                  "能量归零后 1 费卡判为能量不足");

        // 非本方回合
        CheckCode(RuleCore.PlayCard(ctx, 1, 0, 1), RuleCodes.ErrNotTurn, "非本方回合不能出牌");

        // 手牌索引越界
        CheckCode(RuleCore.PlayCard(ctx, 0, 99, 2), RuleCodes.ErrBadHand, "手牌索引越界");
    }

    // ==================================================================
    //  攻击
    // ==================================================================

    /// <summary>造一个「P1 有 A、P2 有 B，且轮到 P1」的场景</summary>
    static BattleContext Duel(CardDef a, CardDef b, out UnitState ua, out UnitState ub,
                              int aSlot = 1, int bSlot = 1)
    {
        var ctx = Battle(new[] { a, Unit("F1", 1, 1, 1), Unit("F2", 1, 1, 1) },
                         new[] { b, Unit("G1", 1, 1, 1), Unit("G2", 1, 1, 1) });
        ToP1Turn(ctx, 3);
        ua = Place(ctx, 0, aSlot, a);
        ub = Place(ctx, 1, bSlot, b);
        return ctx;
    }

    static void TestMeleeTrade()
    {
        UnitState a, b;
        var ctx = Duel(Unit("A", 1, 1, 1), Unit("B", 1, 1, 1), out a, out b);

        CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, 1), RuleCodes.OK, "1v1 近战可结算");
        Check(Board(ctx, 0, 1), null, "攻击者被反击同归于尽");
        Check(Board(ctx, 1, 1), null, "目标死亡");
        Check(ctx.Players[0].Discard.Count, 1, "攻击者进弃牌堆");
        Check(ctx.Players[1].Discard.Count, 1, "目标进弃牌堆");
    }

    static void TestArmorMinimumOne()
    {
        UnitState a, b;
        // 3 攻打 1 甲 5 血 → 3-1 = 2 伤 → 剩 3
        var ctx = Duel(Unit("A", 1, 3, 5), Unit("B", 1, 5, 5, "Armour 1"), out a, out b);
        Check(b.Armor, 1, "Armour 1 解析成护甲 1");

        CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, 1), RuleCodes.OK, "近战结算");
        Check(Board(ctx, 1, 1).Health, 3, "3 攻 vs 1 甲 → 2 伤害（5→3）");
        // 反击：B 5 攻打 A 无甲 → 5 伤，A 5 血死
        Check(Board(ctx, 0, 1), null, "反击 5 伤杀死 5 血攻击者");

        // 最低 1：1 攻打 3 甲 → 仍然造成 1
        UnitState c, d;
        var ctx2 = Duel(Unit("C", 1, 1, 5), Unit("D", 1, 5, 5, "Armour 3"), out c, out d);
        Check(d.Armor, 3, "Armour 3 → 护甲 3");
        RuleCore.DeclareAttack(ctx2, 0, 1, 1, 1);
        Check(Board(ctx2, 1, 1).Health, 4, "1 攻打 3 甲 → 减免到最低 **1**（不是 0）");
    }

    static void TestShieldBlocks()
    {
        UnitState a, b;
        // 攻击者做成 20 血，好让它挨得住反击 —— 不然第一刀之后它自己就死了，验不了第二刀
        var ctx = Duel(Unit("A", 1, 3, 20), Unit("B", 1, 5, 5, "Shield"), out a, out b);
        Check(b.HasShield, true, "Shield 关键词 → HasShield");

        RuleCore.DeclareAttack(ctx, 0, 1, 1, 1);
        Check(Board(ctx, 1, 1).Health, 5, "Shield 挡下全部伤害（血没动）");
        Check(Board(ctx, 1, 1).HasShield, false, "Shield 用掉了");
        Check(Board(ctx, 0, 1).Health, 15, "攻击者照样吃了 B 的 5 点反击");

        // 第二次就不再挡了
        // ⚠️ 要连**攻击配额**一起重置（`RefreshForNewTurn` 干的就是这件事）——
        //    只把 `Exhausted` 置 false 的话，新的配额检查（`AttacksThisTurn >= 1`）会把这一刀挡掉
        Board(ctx, 0, 1).RefreshForNewTurn();
        RuleCore.DeclareAttack(ctx, 0, 1, 1, 1);
        Check(Board(ctx, 1, 1).Health, 2, "Shield 只挡一次，第二次照常吃 3 伤（5→2）");
    }

    static void TestRangedEatsCounter()
    {
        UnitState a, b;
        // 远程 2 攻、2 血；目标 1 攻 5 血（远程没打死 → 吃反击）
        var ctx = Duel(Ranged("Sniper", 2, 0, 2, 2), Unit("Rat", 1, 1, 5), out a, out b);
        Check(a.RangedAttack, 2, "远程攻击力 2 进了单位状态");

        CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, 1, ranged: true), RuleCodes.OK, "远程攻击");
        Check(Board(ctx, 1, 1).Health, 3, "远程 2 攻打 5 血目标 → 剩 3");
        Check(Board(ctx, 0, 1).Health, 1, "**远程未击杀 → 照样吃目标反击 1 伤**（2→1）");
        // ⚠️⚠️ 这条最容易写反。直觉是「远程不受反击」，rule_core.gd 有明确修正记录：
        //     「2026-08-21 修正：此前远程完全不吃反击，Long Range/Sniper 成死代码」
    }

    static void TestLongRangeNoCounter()
    {
        UnitState a, b;
        var ctx = Duel(Ranged("LR", 2, 0, 2, 2, "Long Range"), Unit("Rat", 1, 1, 5), out a, out b);
        CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, 1, ranged: true), RuleCodes.OK, "Long Range 远程攻击");
        Check(Board(ctx, 0, 1).Health, 2, "Long Range 免反击（血没动）");

        // 近战攻击者不带 Long Range 时就照常吃反击
        UnitState c, d;
        var ctx2 = Duel(Unit("Melee", 1, 2, 2, "Long Range"), Unit("Rat2", 1, 1, 5), out c, out d);
        RuleCore.DeclareAttack(ctx2, 0, 1, 1, 1, ranged: false);
        Check(Board(ctx2, 0, 1).Health, 1, "Long Range 只在**远程**攻击时免反击，近战照吃");
    }

    // ==================================================================
    //  目标合法性
    // ==================================================================

    static void TestVanguardRestriction()
    {
        var ctx = Battle(new[] { Unit("A", 1, 5, 5), Unit("F1", 1, 1, 1), Unit("F2", 1, 1, 1) },
                         new[] { Unit("Van", 1, 1, 5, "Vanguard"), Unit("Plain", 1, 1, 5), Unit("G2", 1, 1, 1) });
        ToP1Turn(ctx, 3);
        Place(ctx, 0, 1, Unit("A", 1, 5, 5));
        Place(ctx, 1, 1, Unit("Van", 1, 1, 5, "Vanguard"));
        Place(ctx, 1, 2, Unit("Plain", 1, 1, 5));

        CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, 2), RuleCodes.ErrTarget,
                  "敌方有 Vanguard 时，不能打普通单位");
        CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, 1), RuleCodes.OK,
                  "敌方有 Vanguard 时，可以打 Vanguard");

        // Vanguard 死了之后限制解除
        var ctx2 = Battle(new[] { Unit("A", 1, 5, 5), Unit("F1", 1, 1, 1), Unit("F2", 1, 1, 1) },
                          new[] { Unit("Van", 1, 1, 1, "Vanguard"), Unit("Plain", 1, 1, 5), Unit("G2", 1, 1, 1) });
        ToP1Turn(ctx2, 3);
        Place(ctx2, 0, 1, Unit("A", 1, 5, 5));
        Place(ctx2, 1, 1, Unit("Van", 1, 1, 1, "Vanguard"));
        Place(ctx2, 1, 2, Unit("Plain", 1, 1, 5));
        RuleCore.DeclareAttack(ctx2, 0, 1, 1, 1);      // 秒掉 Vanguard
        Check(Board(ctx2, 1, 1), null, "Vanguard 被秒");
        Board(ctx2, 0, 1).RefreshForNewTurn();     // 连攻击配额一起重置（见 TestShieldBlocks 那条注释）
        CheckCode(RuleCore.DeclareAttack(ctx2, 0, 1, 1, 2), RuleCodes.OK,
                  "Vanguard 没了之后就能打普通单位");
    }

    static void TestStealthUntargetable()
    {
        var ctx = Battle(new[] { Unit("A", 1, 5, 5), Unit("F1", 1, 1, 1), Unit("F2", 1, 1, 1) },
                         new[] { Unit("Sneak", 1, 1, 5, "Stealth"), Unit("Plain", 1, 1, 5), Unit("G2", 1, 1, 1) });
        ToP1Turn(ctx, 3);
        Place(ctx, 0, 1, Unit("A", 1, 5, 5));
        Place(ctx, 1, 1, Unit("Sneak", 1, 1, 5, "Stealth"));
        Place(ctx, 1, 2, Unit("Plain", 1, 1, 5));

        CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, 1), RuleCodes.ErrTarget, "隐身单位不可被攻击");
        CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, 2), RuleCodes.OK, "打旁边的普通单位没问题");

        // 隐身单位自己一攻击就现身
        Place(ctx, 0, 3, Unit("Prey", 1, 1, 9));
        PassTurn(ctx);                    // 轮到 P2（顺便把 P2 的单位解疲劳）
        CheckCode(RuleCore.DeclareAttack(ctx, 1, 1, 0, 3), RuleCodes.OK, "隐身单位自己可以攻击");
        Check(Board(ctx, 1, 1).Has("stealth"), false, "隐身单位攻击后现身（失去 Stealth）");
    }

    static void TestFlyingMeleeBlocked()
    {
        var ctx = Battle(new[] { Unit("Ground", 1, 5, 5), Unit("F1", 1, 1, 1), Unit("F2", 1, 1, 1) },
                         new[] { Unit("Bird", 1, 1, 5, "Flying"), Unit("Plain", 1, 1, 5), Unit("G2", 1, 1, 1) });
        ToP1Turn(ctx, 3);
        Place(ctx, 0, 1, Unit("Ground", 1, 5, 5));
        Place(ctx, 1, 1, Unit("Bird", 1, 1, 5, "Flying"));
        Place(ctx, 1, 2, Unit("Plain", 1, 1, 5));

        CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, 1, ranged: false), RuleCodes.ErrTarget,
                  "地面单位**近战**打不到飞行单位");
        // 远程可以
        var ctxR = Battle(new[] { Ranged("Archer", 1, 0, 5, 3), Unit("F1", 1, 1, 1), Unit("F2", 1, 1, 1) },
                          new[] { Unit("Bird", 1, 1, 5, "Flying"), Unit("Plain", 1, 1, 5), Unit("G2", 1, 1, 1) });
        ToP1Turn(ctxR, 3);
        Place(ctxR, 0, 1, Ranged("Archer", 1, 0, 5, 3));
        Place(ctxR, 1, 1, Unit("Bird", 1, 1, 5, "Flying"));
        CheckCode(RuleCore.DeclareAttack(ctxR, 0, 1, 1, 1, ranged: true), RuleCodes.OK,
                  "远程可以打飞行单位");

        // 同为飞行可以近战互相打
        var ctxF = Battle(new[] { Unit("Bird2", 1, 3, 5, "Flying"), Unit("F1", 1, 1, 1), Unit("F2", 1, 1, 1) },
                          new[] { Unit("Bird", 1, 1, 5, "Flying"), Unit("Plain", 1, 1, 5), Unit("G2", 1, 1, 1) });
        ToP1Turn(ctxF, 3);
        Place(ctxF, 0, 1, Unit("Bird2", 1, 3, 5, "Flying"));
        Place(ctxF, 1, 1, Unit("Bird", 1, 1, 5, "Flying"));
        CheckCode(RuleCore.DeclareAttack(ctxF, 0, 1, 1, 1, ranged: false), RuleCodes.OK,
                  "同为飞行的单位可以近战互相攻击");
        // ⚠️ Flying 检查的是**目标**（飞行单位不能被近战打到），不是攻击者。
        //     rule_core.gd 修正过方向：「此前禁止飞行单位近战打地面、却允许地面近战打飞行」—— 正好反了
    }

    // ==================================================================
    //  技能与触发（2026-09-12）
    //
    //  这块以前**根本没有** —— 「部队卡发动技能」「触发效果」两类特效接不上，
    //  不是特效的问题，是引擎里没有这两种事件。这里逐条验：
    //    · 效果文法解析（解析不出来必须是 null，不许静默吞掉）
    //    · 五个触发类关键词的**时机**（照规则书 :161 那张 61 关键词表）
    //    · 主动技能的行动开销、不吃反击、和攻击目标规则的关系
    //    · 事件流本身（谁、在第几格、什么关键词）—— 表现层就靠它
    //    · 效果链的递归截断（反噬/忏悔互相触发天生是个环）
    // ==================================================================

    static bool HasSignal(BattleContext ctx, EvtKind kind, string keyword = null)
    {
        return FindSignal(ctx, kind, keyword) != null;
    }

    static BattleEvent FindSignal(BattleContext ctx, EvtKind kind, string keyword = null)
    {
        foreach (var e in ctx.Signals)
            if (e.Kind == kind && (keyword == null || e.Keyword == keyword)) return e;
        return null;
    }

    static void TestEffectSpecParsing()
    {
        var d = EffectSpec.Parse("Damage 2 EnemyUnit");
        CheckTrue(d != null, "「Damage 2 EnemyUnit」解析成功");
        if (d != null)
        {
            Check(d.Verb, "damage", "动词 = damage");
            Check(d.Amount, 2, "数值 = 2");
            Check(d.Target, EffectTargets.EnemyUnit, "目标 = enemyunit");
        }

        Check(EffectSpec.Parse("Heal 3 OwnWarlord").Target, EffectTargets.OwnWarlord, "Heal → OwnWarlord");
        Check(EffectSpec.Parse("Draw 2").Verb, "draw", "Draw 2 → draw（不用写目标）");
        Check(EffectSpec.Parse("Damage EnemyWarlord").Amount, 1, "省略数值 = 1");
        Check(EffectSpec.Parse("damage 1 enemyunit").Target, EffectTargets.EnemyUnit,
              "大小写不敏感");
        Check(EffectSpec.Parse("Damage 2 EnemyUnit").Short(), "DMG2 UNIT", "卡面小字 DMG2 UNIT");

        // ⚠️ 解析不出来一律 null —— **不给默认值、不猜**。
        //    「Damage 2」这种缺目标的，猜一个就等于替玩家做决定，而这工程最忌讳静默（见文件头）。
        CheckTrue(EffectSpec.Parse("Deal 3 damage to an enemy") == null,
                  "原版那种自然语言 → null（**故意的**，卡面会标 *）");
        CheckTrue(EffectSpec.Parse("Damage 2") == null, "Damage 没写目标 → null（不猜打谁）");
        CheckTrue(EffectSpec.Parse("Damage 2 SomeGuy") == null, "不认识的目标 → null");
        CheckTrue(EffectSpec.Parse("Frobnicate 1 Self") == null, "不认识的动词 → null");
        CheckTrue(EffectSpec.Parse("Damage -1 Self") == null, "负数值 → null");
        CheckTrue(EffectSpec.Parse("") == null, "空串 → null");
        CheckTrue(EffectSpec.Parse(null) == null, "null → null");
    }

    /// <summary>Rally（集结）：「从手牌部署后触发效果」—— 规则书 :200</summary>
    static void TestRally()
    {
        var rallier = Unit("Rallier", 1, 1, 3, "Rally: Damage 2 EnemyWarlord");
        var ctx = Battle(new[] { rallier, Unit("F1", 1, 1, 1), Unit("F2", 1, 1, 1) },
                         new[] { Unit("X", 1, 1, 1), Unit("Y", 1, 1, 1), Unit("Z", 1, 1, 1) });
        ToP1Turn(ctx, 2);

        int foeHp = ctx.Players[1].Warlord.Health;
        ctx.ClearSignals();
        CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "Rallier"), 1), RuleCodes.OK,
                  "Rallier 部署成功");

        Check(ctx.Players[1].Warlord.Health, foeHp - 2, "部署时 Rally 就打出去了（敌方督军 -2）");
        // 事件流：**Play → Deploy → Trigger → Hit**。**挨伤害也要发** ——
        // 表现层光看「血少了」分不出是挨刀、被技能打、还是疲劳。
        // ⚠️ 2026-09-13 起第一条是 `Play`（「打出了这张牌」）—— 加它是为了让**战术卡也有事件**
        //    （以前战术卡打出去一个事件都没有，战斗日志和表现层都看不见它），
        //    单位卡于是变成 Play + Deploy 两条。
        Check(ctx.Signals.Count, 4, "发了四条事件（Play + Deploy + Trigger + Hit）");
        Check(ctx.Signals[0].Kind, EvtKind.Play, "第 1 条是 Play（打出了这张牌）");
        Check(ctx.Signals[0].CardId, "Rallier", "Play 带着卡名");
        Check(ctx.Signals[1].Kind, EvtKind.Deploy, "第 2 条是 Deploy");
        Check(ctx.Signals[1].Slot, 1, "Deploy 带着格位");
        Check(ctx.Signals[1].CardId, "Rallier", "Deploy 带着卡名");
        Check(ctx.Signals[2].Kind, EvtKind.Trigger, "第 3 条是 Trigger");
        Check(ctx.Signals[2].Keyword, KeywordTable.Rally, "Trigger 带着关键词 rally");
        Check(ctx.Signals[2].Slot, 1, "Trigger 带着格位（表现层照它播特效）");
        Check(ctx.Signals[2].CardId, "Rallier", "Trigger 带着卡名");
        Check(ctx.Signals[3].Kind, EvtKind.Hit, "第 4 条是 Hit");
        Check(ctx.Signals[3].Amount, 2, "Hit 带着实际伤害值");
        Check(ctx.Signals[3].Player, 1, "Hit 的归属方是**挨打那边**（敌方督军）");

        // **留档日志**（`Ctx.ActionLog`）也在收：存的是同一份事件流，但**不会被搬走** ——
        // 而且把「出单位卡连着发的 Play + Deploy」**合并成一条**（不然每张单位卡都重复一行）。
        Check(ctx.ActionLog.Count, 3, $"留档日志把 Play+Deploy 合并了（{ctx.ActionLog.Count} 条：Play / Trigger / Hit）");
        Check(ctx.ActionLog[0].Kind, EvtKind.Play, "留档里第 1 条是 Play（不是 Deploy）");

        // 没写 `Rally:` 效果的卡不会「触发了个寂寞」
        var plain = Unit("Plain", 1, 1, 3, KeywordTable.Rally);
        var ctx2 = Battle(new[] { plain, Unit("F1", 1, 1, 1), Unit("F2", 1, 1, 1) },
                          new[] { Unit("X", 1, 1, 1), Unit("Y", 1, 1, 1), Unit("Z", 1, 1, 1) });
        ToP1Turn(ctx2, 2);
        ctx2.ClearSignals();
        RuleCore.PlayCard(ctx2, 0, HandIdx(ctx2, 0, "Plain"), 1);
        CheckTrue(!HasSignal(ctx2, EvtKind.Trigger), "光有 rally 关键词、没写效果 → 不触发（也不发事件）");
    }

    /// <summary>Strike（猛击）攻击后触发 / Slay（斩杀）摧毁单位后触发 —— 规则书 :208 / :214</summary>
    static void TestStrikeAndSlay()
    {
        // ---- Strike：打没打死都算 ----
        UnitState a, b;
        var ctx = Duel(Unit("Striker", 2, 3, 5, "Strike: Damage 1 EnemyWarlord"),
                       Unit("Dummy", 1, 0, 9), out a, out b);
        ctx.ClearSignals();
        int foe = ctx.Players[1].Warlord.Health;
        CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, 1), RuleCodes.OK, "砍一刀（没砍死）");
        Check(Board(ctx, 1, 1).Health, 6, "目标 9 → 6");
        Check(ctx.Players[1].Warlord.Health, foe - 1, "Strike 在攻击后触发（敌方督军 -1）");
        var strike = FindSignal(ctx, EvtKind.Trigger, KeywordTable.Strike);
        CheckTrue(strike != null, "事件流里有 Strike");
        CheckTrue(strike != null && strike.Slot == 1, "Strike 事件带着攻击者的格位");

        // ---- Slay：攻击并**摧毁单位**后触发 ----
        var slayer = Unit("Slayer", 4, 5, 6, "Slay: Damage 2 EnemyWarlord");
        var ctx2 = Duel(slayer, Unit("Victim", 1, 0, 2), out a, out b);
        ctx2.ClearSignals();
        int foe2 = ctx2.Players[1].Warlord.Health;
        CheckCode(RuleCore.DeclareAttack(ctx2, 0, 1, 1, 1), RuleCodes.OK, "斩杀那一刀");
        Check(Board(ctx2, 1, 1), null, "目标被摧毁");
        Check(ctx2.Players[1].Warlord.Health, foe2 - 2, "Slay 触发（敌方督军 -2）");
        CheckTrue(HasSignal(ctx2, EvtKind.Trigger, KeywordTable.Slay), "事件流里有 Slay");

        // ---- 没打死 → 不触发 ----
        var ctx3 = Duel(slayer, Unit("Tank", 1, 0, 9), out a, out b);
        ctx3.ClearSignals();
        int foe3 = ctx3.Players[1].Warlord.Health;
        CheckCode(RuleCore.DeclareAttack(ctx3, 0, 1, 1, 1), RuleCodes.OK, "砍一刀（砍不死）");
        Check(ctx3.Players[1].Warlord.Health, foe3, "没摧毁单位 → Slay 不触发");
        CheckTrue(!HasSignal(ctx3, EvtKind.Trigger, KeywordTable.Slay), "事件流里也没有 Slay");

        // ---- 攻击者自己死了 → 两个都不触发（原文都写着「本单位存活时」）----
        var ctx4 = Duel(slayer, Unit("Brute", 1, 9, 3), out a, out b);
        ctx4.ClearSignals();
        int foe4 = ctx4.Players[1].Warlord.Health;
        CheckCode(RuleCore.DeclareAttack(ctx4, 0, 1, 1, 1), RuleCodes.OK, "同归于尽的一刀");
        Check(Board(ctx4, 0, 1), null, "攻击者阵亡");
        Check(ctx4.Players[1].Warlord.Health, foe4, "攻击者没活下来 → Slay 不触发");
    }

    /// <summary>Backlash（反噬）：「单位死亡时触发效果」—— 规则书 :169</summary>
    static void TestBacklash()
    {
        UnitState a, b;
        var ghost = Unit("Ghost", 2, 0, 3, "Backlash: Damage 2 EnemyWarlord");
        var ctx = Duel(Unit("Killer", 1, 5, 5), ghost, out a, out b);
        ctx.ClearSignals();

        int p1hp = ctx.Players[0].Warlord.Health;     // Ghost 在 P2 场上，反噬打的是 P1 的督军
        CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, 1), RuleCodes.OK, "打死反噬单位");
        Check(Board(ctx, 1, 1), null, "Ghost 阵亡");
        Check(ctx.Players[0].Warlord.Health, p1hp - 2, "Backlash 在阵亡时触发，打到对方督军");

        var death = FindSignal(ctx, EvtKind.Death);
        var back = FindSignal(ctx, EvtKind.Trigger, KeywordTable.Backlash);
        CheckTrue(death != null && death.Player == 1 && death.Slot == 1,
                  "Death 事件带着**原格位**（格位马上就空了，不带上就没法播特效）");
        CheckTrue(back != null && back.Player == 1 && back.Slot == 1,
                  "Backlash 事件也带着原格位（单位已经不在棋盘上了）");

        // 先发 Death 再结算反噬 —— 顺序反了的话表现层会在「人已经没了」之后才收到阵亡
        int di = ctx.Signals.IndexOf(death), bi = ctx.Signals.IndexOf(back);
        CheckTrue(di >= 0 && bi > di, "Death 排在 Backlash 前面");
    }

    /// <summary>Penitence（忏悔）：「受到伤害但未死亡时触发效果」—— 规则书 :196</summary>
    static void TestPenitence()
    {
        UnitState a, b;
        var penitent = Unit("Penitent", 2, 0, 5, "Penitence: Damage 1 EnemyWarlord");

        var ctx = Duel(Unit("Poker", 1, 2, 5), penitent, out a, out b);
        ctx.ClearSignals();
        int p1hp = ctx.Players[0].Warlord.Health;
        CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, 1), RuleCodes.OK, "打一下但没打死");
        CheckTrue(Board(ctx, 1, 1) != null, "Penitent 还活着");
        Check(ctx.Players[0].Warlord.Health, p1hp - 1, "受伤未死 → Penitence 触发");

        // 打死了就不触发 —— 「未死亡」是这条的关键字，那种情况归 Backlash 管
        var ctx2 = Duel(Unit("Poker", 1, 9, 5), penitent, out a, out b);
        ctx2.ClearSignals();
        int p1b = ctx2.Players[0].Warlord.Health;
        CheckCode(RuleCore.DeclareAttack(ctx2, 0, 1, 1, 1), RuleCodes.OK, "一击打死");
        Check(Board(ctx2, 1, 1), null, "Penitent 阵亡");
        Check(ctx2.Players[0].Warlord.Health, p1b, "死了就不走 Penitence（那条只管「未死亡」）");
        CheckTrue(!HasSignal(ctx2, EvtKind.Trigger, KeywordTable.Penitence), "事件流里没有 Penitence");

        // Shield 全挡 = 没受伤 → 也不触发
        var shielded = Unit("Shielded", 2, 0, 5, KeywordTable.Shield, "Penitence: Damage 1 EnemyWarlord");
        var ctx3 = Duel(Unit("Poker", 1, 2, 5), shielded, out a, out b);
        ctx3.ClearSignals();
        int p1c = ctx3.Players[0].Warlord.Health;
        CheckCode(RuleCore.DeclareAttack(ctx3, 0, 1, 1, 1), RuleCodes.OK, "打在盾上");
        Check(ctx3.Players[1].Board[1].Health, 5, "Shield 挡住了，一滴血没掉");
        Check(ctx3.Players[0].Warlord.Health, p1c, "没真受伤 → Penitence 不触发");
    }

    /// <summary>主动技能：花掉一次行动、**不吃反击**</summary>
    static void TestAbility()
    {
        UnitState a, b;
        var caster = Unit("Caster", 3, 2, 5, "Ability: Damage 3 EnemyUnit");
        var ctx = Duel(caster, Unit("Prey", 1, 4, 6), out a, out b);
        ctx.ClearSignals();
        int energy = ctx.Players[0].Energy;

        CheckCode(RuleCore.CanUseAbility(ctx, 0, 1, 1), RuleCodes.OK, "有技能、没疲劳、目标合法 → 可以放");
        CheckCode(RuleCore.UseAbility(ctx, 0, 1, 1), RuleCodes.OK, "放技能成功");

        Check(Board(ctx, 1, 1).Health, 3, "Prey 6 → 3（技能 3 点）");
        CheckTrue(Board(ctx, 0, 1).Exhausted, "放技能花掉了这个单位的行动");
        Check(Board(ctx, 0, 1).Health, 5, "**技能不吃反击** —— 施法者一滴血没掉（Prey 4 攻）");
        Check(ctx.Players[0].Energy, energy, "放技能不花能量");

        CheckTrue(HasSignal(ctx, EvtKind.Ability), "事件流里有 Ability —— 这一类以前压根没有");
        var ab = FindSignal(ctx, EvtKind.Ability);
        Check(ab.Slot, 1, "Ability 事件带着施法者的格位");
        Check(ab.CardId, "Caster", "Ability 事件带着卡名");
        Check(ab.Keyword, KeywordTable.Ability, "Ability 事件带着关键词");
        Check(ab.Amount, 3, "Ability 事件带着数值（卡面/日志用）");

        CheckCode(RuleCore.CanUseAbility(ctx, 0, 1, 1), RuleCodes.ErrExhausted, "一回合只能行动一次");
        CheckCode(RuleCore.CanUseAbility(ctx, 1, 1, 1), RuleCodes.ErrNotTurn, "非本方回合不能放技能");

        // 没有技能的单位
        var ctx2 = Duel(Unit("Mundane", 1, 1, 1), Unit("Dummy", 1, 0, 1), out a, out b);
        CheckCode(RuleCore.CanUseAbility(ctx2, 0, 1), RuleCodes.ErrNoAbility, "卡上没写 Ability → ErrNoAbility");
        CheckCode(RuleCore.UseAbility(ctx2, 0, 1), RuleCodes.ErrNoAbility, "UseAbility 也拒绝");
    }

    /// <summary>
    /// 技能**不是攻击** —— 攻击目标那三条限制里只有「潜行」管得着它。
    /// 三条的原文见 `RuleCore.CanUseAbility` 的注释（规则书 :223 / :188 / :211）。
    /// </summary>
    static void TestAbilityTargetRules()
    {
        UnitState a, b;
        var caster = Unit("Caster", 3, 2, 5, "Ability: Damage 3 EnemyUnit");
        var ctx = Duel(caster, Unit("Guard", 1, 1, 9, KeywordTable.Vanguard), out a, out b);

        CheckCode(RuleCore.CanUseAbility(ctx, 0, 1, 1), RuleCodes.OK,
                  "先锋不挡技能（原文是「不能被选为**攻击目标**」）");

        // 飞行同理 —— 原文是「只能被…以**近战攻击**选中」
        Place(ctx, 1, 2, Unit("Bird", 1, 1, 5, KeywordTable.Flying));
        CheckCode(RuleCore.CanUseAbility(ctx, 0, 1, 2), RuleCodes.OK, "飞行也不挡技能");

        // 潜行是「不能被**任何方式**选中」—— 这条管得着
        Place(ctx, 1, 3, Unit("Lurker", 1, 1, 5, KeywordTable.Stealth));
        CheckCode(RuleCore.CanUseAbility(ctx, 0, 1, 3), RuleCodes.ErrTarget, "潜行挡技能");

        // 目标合法性：空格 / 督军 / 越界
        CheckCode(RuleCore.CanUseAbility(ctx, 0, 1, 8), RuleCodes.ErrTarget, "空格 → 目标非法");
        CheckCode(RuleCore.CanUseAbility(ctx, 0, 1, BoardSpec.WarlordSlot), RuleCodes.ErrTarget,
                  "选不中督军 —— 要打督军的技能，目标栏会直接写 EnemyWarlord");

        // 攻击者自己不存在 / 格位越界
        CheckCode(RuleCore.CanUseAbility(ctx, 0, 0), RuleCodes.ErrNotUnit, "空格上放技能");
        CheckCode(RuleCore.CanUseAbility(ctx, 0, 99), RuleCodes.ErrNotUnit, "越界格位");

        // ⚠️「能不能开始放」和「这个目标行不行」是两件事。
        //    要选目标的技能，表现层得先**进入选目标状态**（那一步还没选呢）——
        //    拿要求目标的判据去问只会得到 ErrTarget，于是永远进不了那个状态（踩过）。
        CheckCode(RuleCore.CanStartAbility(ctx, 0, 1), RuleCodes.OK, "CanStartAbility 不判目标");
        CheckCode(RuleCore.CanUseAbility(ctx, 0, 1), RuleCodes.ErrTarget,
                  "CanUseAbility 不带目标 → 「还没选目标」");
    }

    static void TestHealAndDraw()
    {
        UnitState a, b;
        var medic = Unit("Medic", 2, 1, 4, "Ability: Heal 3 OwnWarlord");

        var ctx = Duel(medic, Unit("Dummy", 1, 0, 1), out a, out b);
        ctx.Players[0].Warlord.Health = 20;
        CheckCode(RuleCore.CanUseAbility(ctx, 0, 1), RuleCodes.OK, "治疗技能不用选目标");
        CheckCode(RuleCore.UseAbility(ctx, 0, 1), RuleCodes.OK, "放出来");
        Check(ctx.Players[0].Warlord.Health, 23, "督军 20 → 23");

        var ctx2 = Duel(medic, Unit("Dummy", 1, 0, 1), out a, out b);
        ctx2.Players[0].Warlord.Health = 29;
        CheckCode(RuleCore.UseAbility(ctx2, 0, 1), RuleCodes.OK, "血快满了也能放");
        Check(ctx2.Players[0].Warlord.Health, 30, "治疗封顶在最大生命 30（不是 32）");

        var scribe = Unit("Scribe", 2, 1, 4, "Ability: Draw 2");
        var ctx3 = Duel(scribe, Unit("Dummy", 1, 0, 1), out a, out b);
        int hand = ctx3.Players[0].Hand.Count;
        CheckCode(RuleCore.UseAbility(ctx3, 0, 1), RuleCodes.OK, "抽牌技能");
        Check(ctx3.Players[0].Hand.Count, hand + 2, "Draw 2 真抽了两张（不是只抽一张）");
    }

    /// <summary>
    /// 效果链的递归截断。
    /// 「受伤 → 打回去 → 那边也受伤 → 再打回来」天生是个环（规则书 :237 就在讲同时触发），
    /// 靠 `BattleContext.MaxEffectChain` 截断，不能指望「保证不会发生」。
    /// </summary>
    static void TestEffectChainGuard()
    {
        UnitState a, b;
        var p1 = Unit("PingPong", 1, 0, 30, "Penitence: Damage 1 EnemyUnit");
        var p2 = Unit("PingPong", 1, 0, 30, "Penitence: Damage 1 EnemyUnit");

        // Poker 放在槽 3，让「敌方最左的部队」落在 PingPong 上，环才转得起来
        var ctx = Duel(Unit("Poker", 1, 3, 9), p2, out a, out b, aSlot: 3, bSlot: 2);
        Place(ctx, 0, 1, p1);
        ctx.ClearSignals();

        CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, 2), RuleCodes.OK, "点着这条链");

        int triggers = 0;
        foreach (var e in ctx.Signals) if (e.Kind == EvtKind.Trigger) triggers++;

        CheckTrue(triggers > 0, $"链真的转起来了（{triggers} 次触发）");
        CheckTrue(triggers <= BattleContext.MaxEffectChain,
                  $"被深度上限截断，没有无限递归（{triggers} ≤ {BattleContext.MaxEffectChain}）");
        Check(ctx.EffectChain, 0, "结算完深度回到 0（不然下一刀就少一层可用额度）");
        CheckTrue(Board(ctx, 0, 1).IsAlive && Board(ctx, 1, 2).IsAlive,
                  "两个乒乓单位都还活着（这条链是「互相点」，不是同归于尽）");
    }

    /// <summary>自己那套卡：效果文字**全部**解析得出来，六类关键词都有人用</summary>
    static void TestStarterCardEffects()
    {
        var all = StarterCards.Both();

        var unparsed = RuleCore.UnparsedEffects(all);
        Check(unparsed.Count, 0, "自己那套卡的效果文字全部解析得出来"
              + (unparsed.Count > 0 ? "（坏在这些：" + string.Join(" / ", unparsed) + "）" : ""));

        int withEffect = 0, withAbility = 0;
        foreach (var c in all)
        {
            if (c.Effects.Count > 0) withEffect++;
            if (c.HasAbility) withAbility++;
        }
        CheckTrue(withEffect >= 5, $"带触发效果的卡 {withEffect} 张（应 ≥ 5）");
        CheckTrue(withAbility >= 3, $"带主动技能的卡 {withAbility} 张（应 ≥ 3）");

        // 六类关键词**每一类都要有卡在用** —— 有一类没人用，它等于没做
        string[] need =
        {
            KeywordTable.Rally, KeywordTable.Strike, KeywordTable.Slay,
            KeywordTable.Backlash, KeywordTable.Penitence, KeywordTable.Ability,
        };
        foreach (var kw in need)
        {
            int n = 0;
            foreach (var c in all)
                if (kw == KeywordTable.Ability ? c.HasAbility : c.Effect(kw) != null) n++;
            CheckTrue(n > 0, $"「{kw}」至少有一张卡在用");
        }
        Debug.Log(P + $"   带效果的卡：触发 {withEffect} 张 / 技能 {withAbility} 张（共 {all.Count} 张）");
    }

    /// <summary>AI 会不会用技能 —— 不会用的话对局里永远看不到那类特效</summary>
    static void TestAiUsesAbility()
    {
        int s, t;

        // ① 治疗类：督军掉血就放，满血不浪费
        var guard = new CardDef("Reef Guard", "Reef Guard", "unit", "", null, "Test", 2, 0, 7, 0,
                                new[] { KeywordTable.Vanguard, "Ability: Heal 2 OwnWarlord" });
        var ctx = ToP2TurnWith(guard);
        CheckTrue(!SimpleAI.NextAbility(ctx, out s, out t), "督军满血 → 不浪费行动去治疗");

        ctx.Players[1].Warlord.Health -= 3;
        CheckTrue(SimpleAI.NextAbility(ctx, out s, out t), "督军掉血 → AI 会放治疗技能");
        Check(s, 1, "选中了场上的 Reef Guard");
        CheckCode(RuleCore.UseAbility(ctx, 1, s, t), RuleCodes.OK, "AI 挑的这一手真放得出来");

        // ② 伤害类：技能伤害 ≥ 攻击力就放 —— 技能不吃反击，没道理去平A
        var brute = new CardDef("Brute", "Brute", "unit", "", null, "Test", 3, 2, 5, 0,
                                new[] { "Ability: Damage 2 EnemyUnit" });
        var ctx2 = ToP2TurnWith(brute);
        Place(ctx2, 0, 1, Unit("Prey", 1, 0, 5));
        CheckTrue(SimpleAI.NextAbility(ctx2, out s, out t), "2 攻打 2 伤技能 → 放技能");
        Check(t, 1, "目标是对面那个单位");
        CheckCode(RuleCore.UseAbility(ctx2, 1, s, t), RuleCodes.OK, "放得出来");

        // ③ 攻击力更高 → 平A更划算
        var big = new CardDef("Big", "Big", "unit", "", null, "Test", 3, 9, 5, 0,
                              new[] { "Ability: Damage 2 EnemyUnit" });
        var ctx3 = ToP2TurnWith(big);
        Place(ctx3, 0, 1, Unit("Prey", 1, 0, 5));
        CheckTrue(!SimpleAI.NextAbility(ctx3, out s, out t), "9 攻打 2 伤技能 → 平A划算，不放技能");

        // ④ 对面场上没部队 → 伤害技能空过，不放
        var ctx4 = ToP2TurnWith(brute);
        CheckTrue(!SimpleAI.NextAbility(ctx4, out s, out t), "对面没有部队可打 → 不放（不白扔一次行动）");
    }

    /// <summary>造一个「轮到 P2 的第 1 回合，P2 槽 1 站着 card」的场景</summary>
    static BattleContext ToP2TurnWith(CardDef card)
    {
        var ctx = Battle(new[] { Unit("F1", 1, 1, 1), Unit("F2", 1, 1, 1), Unit("F3", 1, 1, 1) },
                         new[] { card, Unit("G1", 1, 1, 1), Unit("G2", 1, 1, 1) });
        ToP1Turn(ctx, 2);
        RuleCore.EndTurn(ctx);
        RuleCore.BeginTurn(ctx);          // 轮到 P2
        Place(ctx, 1, 1, card);
        return ctx;
    }

    /// <summary>
    /// 事件流的**确定性**：同种子必须逐条一致。
    /// 事件流是表现层放特效的依据 —— 它要是不可复现，特效的 bug 就没法查。
    /// </summary>
    static void TestSignalDeterminism()
    {
        var a = RunScriptedGame(20260911);
        var b = RunScriptedGame(20260911);
        Check(a.Count, b.Count, $"同种子 → 事件条数一致（{a.Count} 条）");

        bool same = a.Count == b.Count;
        for (int i = 0; same && i < a.Count; i++) same = a[i] == b[i];
        CheckTrue(same, "同种子 → 事件流逐条一致");

        var c = RunScriptedGame(777);
        bool differs = c.Count != a.Count;
        for (int i = 0; !differs && i < c.Count; i++) differs = c[i] != a[i];
        CheckTrue(differs, "换种子 → 事件流不一样（证明随机真的在起作用）");

        // 这两类事件**必须真的出现在对局里** —— 不然「接上了」只是纸上谈兵。
        // 跑满 40 回合（牌库抽空就靠疲劳收场），30 张的牌库足够把带效果的卡都摸到
        bool sawAbility = false, sawTrigger = false;
        foreach (var line in a)
        {
            if (line.Contains("Ability")) sawAbility = true;
            if (line.Contains("Trigger")) sawTrigger = true;
        }
        CheckTrue(sawAbility, $"这段脚本对局里出现了 Ability 事件（部队卡发动技能）");
        CheckTrue(sawTrigger, $"这段脚本对局里出现了 Trigger 事件（触发效果）");

        // 事件种类分布 —— 「怎么一条 Ability 都没有」这种问题一眼能看见
        var kinds = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var line in a)
        {
            var sp = line.Split(' ');
            if (sp.Length < 2) continue;
            int n;
            kinds.TryGetValue(sp[1], out n);
            kinds[sp[1]] = n + 1;
        }
        var parts = new List<string>();
        foreach (var kv in kinds) parts.Add($"{kv.Key}×{kv.Value}");
        Debug.Log(P + $"   事件流 {a.Count} 条：" + string.Join("  ", parts));
    }

    /// <summary>
    /// 跑一段固定脚本的对局，把事件流原样收下来做字符串（好比较）。
    ///
    /// ⚠️ **督军的血被调大了** —— 这一段的目的不是「打完一局分出胜负」
    ///    （那是 <see cref="TestFullGameWithRealCards"/> 的事），而是**跑够回合数**，
    ///    让 30 张牌库里那些带效果的卡都上过场。真实长度的对局十几回合就结束了，
    ///    那点回合数里摸不摸得到 Ironclad / Reef Guard 全看运气，断言会飘。
    /// </summary>
    static List<string> RunScriptedGame(int seed, int turns = 40)
    {
        var ctx = RuleCore.NewBattle(
            DeckBuilder.StarterDeck(StarterCards.Ember(), StarterCards.EmberFaction, 30, new System.Random(1)),
            DeckBuilder.StarterDeck(StarterCards.Tide(), StarterCards.TideFaction, 30, new System.Random(2)),
            seed);

        for (int i = 0; i < 2; i++)
        {
            ctx.Players[i].Warlord.MaxHealth = 999;
            ctx.Players[i].Warlord.Health = 999;
        }

        for (int i = 0; i < turns && !ctx.IsOver; i++)
        {
            RuleCore.BeginTurn(ctx);
            SimpleAI.PlayTurn(ctx);
            if (ctx.IsOver) break;
            RuleCore.EndTurn(ctx);
        }

        var log = new List<string>();
        foreach (var e in ctx.Signals) log.Add(e.ToString());
        return log;
    }

    // ==================================================================
    //  胜负与疲劳
    // ==================================================================

    static void TestWinnerByWarlord()
    {
        var ctx = Battle(new[] { Unit("A", 1, 40, 40), Unit("F1", 1, 1, 1), Unit("F2", 1, 1, 1) },
                         new[] { Unit("X", 1, 1, 1), Unit("Y", 1, 1, 1), Unit("Z", 1, 1, 1) });
        ToP1Turn(ctx, 3);
        Place(ctx, 0, 1, Unit("A", 1, 40, 40));

        Check(RuleCore.CheckWinner(ctx), 0, "开局进行中");
        CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, BoardSpec.WarlordSlot), RuleCodes.OK,
                  "可以攻击敌方督军");
        Check(ctx.Winner, 1, "督军被打死 → P1 胜");
        Check(RuleCore.CheckWinner(ctx), 1, "重复调用 CheckWinner 结果稳定");

        // 不能打自己的督军
        var ctx2 = Battle(new[] { Unit("A", 1, 1, 1), Unit("F1", 1, 1, 1), Unit("F2", 1, 1, 1) },
                          new[] { Unit("X", 1, 1, 1), Unit("Y", 1, 1, 1), Unit("Z", 1, 1, 1) });
        ToP1Turn(ctx2, 3);
        Place(ctx2, 0, 1, Unit("A", 1, 1, 1));
        CheckCode(RuleCore.DeclareAttack(ctx2, 0, 1, 0, BoardSpec.WarlordSlot), RuleCodes.ErrSelf,
                  "不能攻击自己场上的督军");
    }

    /// <summary>
    /// 投降（原版 `BattleResult.Forfeit`）。**这条以前完全没有** —— 用户 2026-09-12 点名要还原。
    /// 判据有三条：① 立刻判对方胜（**不看血量**）② 记下是谁投的 ③ 判过了不再改。
    /// </summary>
    static void TestForfeit()
    {
        var ctx = Battle(new[] { Unit("A", 1, 40, 40), Unit("F1", 1, 1, 1), Unit("F2", 1, 1, 1) },
                         new[] { Unit("X", 1, 1, 1), Unit("Y", 1, 1, 1), Unit("Z", 1, 1, 1) });
        ToP1Turn(ctx, 3);
        Check(ctx.Winner, 0, "开局进行中（谁都没倒）");
        Check(ctx.ForfeitedBy, -1, "还没人投降");

        // P0 投降 → P1（2 号）胜
        Check(RuleCore.Forfeit(ctx, 0), 2, "P0 投降 → P1 胜（**不看督军血量**）");
        Check(ctx.ForfeitedBy, 0, "记下是谁投的");
        CheckTrue(ctx.Players[0].Warlord.Health > 0, "投的时候督军还活着（不是被打死的）");
        CheckTrue(ctx.Events[ctx.Events.Count - 1].Contains("投降"), "事件日志里写明了投降");

        // 已经结束了：第二次投降（连同对方的）都不作数
        Check(RuleCore.Forfeit(ctx, 1), 2, "判过之后再投降不改结果");
        Check(ctx.ForfeitedBy, 0, "也不改「谁投的」");

        // 另一条路：P1 投降 → P0 胜
        var ctx2 = Battle(new[] { Unit("A", 1, 1, 1), Unit("F1", 1, 1, 1), Unit("F2", 1, 1, 1) },
                          new[] { Unit("X", 1, 1, 1), Unit("Y", 1, 1, 1), Unit("Z", 1, 1, 1) });
        Check(RuleCore.Forfeit(ctx2, 1), 1, "P1 投降 → P0 胜");
        Check(ctx2.IsOver, true, "投降之后 `IsOver` 为真");

        // 越界/无效参数：什么都不做（**不能静默判错**）
        var ctx3 = Battle(new[] { Unit("A", 1, 1, 1), Unit("F1", 1, 1, 1), Unit("F2", 1, 1, 1) },
                          new[] { Unit("X", 1, 1, 1), Unit("Y", 1, 1, 1), Unit("Z", 1, 1, 1) });
        Check(RuleCore.Forfeit(ctx3, 7), 0, "非法玩家号 → 返回 0、不改状态");
        Check(ctx3.Winner, 0, "…结果也还是「进行中」");
    }

    static void TestDrawIsDraw()
    {
        var ctx = Battle(new[] { Unit("A", 1, 1, 1), Unit("F1", 1, 1, 1), Unit("F2", 1, 1, 1) },
                         new[] { Unit("X", 1, 1, 1), Unit("Y", 1, 1, 1), Unit("Z", 1, 1, 1) });
        ctx.Players[0].Warlord.Health = 0;
        ctx.Players[1].Warlord.Health = 0;
        Check(RuleCore.CheckWinner(ctx), 3, "双方督军同时倒下 → 平局");
        Check(ctx.Winner, 3, "ctx.Winner 也记成 3");
    }

    static void TestFatigue()
    {
        var ctx = Battle(new[] { Unit("A", 1, 1, 1), Unit("F1", 1, 1, 1), Unit("F2", 1, 1, 1) },
                         new[] { Unit("X", 1, 1, 1), Unit("Y", 1, 1, 1), Unit("Z", 1, 1, 1) });
        ctx.Players[0].Deck.Clear();

        Check(ctx.Players[0].Warlord.Health, 30, "抽空之前督军 30 血");
        RuleCore.Draw(ctx, 0);
        Check(ctx.Players[0].Fatigue, 1, "第 1 次抽空 → 疲劳 1");
        Check(ctx.Players[0].Warlord.Health, 29, "督军挨 1 伤（30→29）");
        RuleCore.Draw(ctx, 0);
        Check(ctx.Players[0].Fatigue, 2, "第 2 次 → 疲劳 2");
        Check(ctx.Players[0].Warlord.Health, 27, "督军再挨 2 伤（29→27）");

        // 疲劳能打死督军 —— 每次抽牌后都必须复查胜负
        ctx.Players[0].Warlord.Health = 1;
        RuleCore.Draw(ctx, 0);
        CheckTrue(ctx.Players[0].Warlord.Health <= 0, "疲劳把督军打死了");
        Check(ctx.Winner, 2, "督军被疲劳打死 → 对方胜（Draw 内部复查了胜负）");
    }

    // ==================================================================
    //  确定性
    // ==================================================================

    static void TestDeterminism()
    {
        var pool = CardDatabase.Load();
        if (pool.Count == 0) { CheckTrue(false, "卡表没加载上，确定性测试跳过"); return; }

        var factions = CardDatabase.Factions(pool);
        string f = factions[0];

        var a = RuleCore.NewBattle(DeckBuilder.StarterDeck(pool, f, 30, new System.Random(7)),
                                   DeckBuilder.StarterDeck(pool, f, 30, new System.Random(9)),
                                   seed: 20260911);
        var b = RuleCore.NewBattle(DeckBuilder.StarterDeck(pool, f, 30, new System.Random(7)),
                                   DeckBuilder.StarterDeck(pool, f, 30, new System.Random(9)),
                                   seed: 20260911);

        Check(a.Players[0].Hand.Count, b.Players[0].Hand.Count, "同种子 → 手牌张数一致");
        Check(a.Players[0].Deck.Count, b.Players[0].Deck.Count, "同种子 → 牌库张数一致");

        bool sameHand = true, sameDeck = true;
        for (int i = 0; i < a.Players[0].Hand.Count; i++)
            if (a.Players[0].Hand[i].Name != b.Players[0].Hand[i].Name) { sameHand = false; break; }
        for (int i = 0; i < a.Players[0].Deck.Count; i++)
            if (a.Players[0].Deck[i].Name != b.Players[0].Deck[i].Name) { sameDeck = false; break; }

        CheckTrue(sameHand, "同种子 → 起手逐张一致（顺序也一致）");
        CheckTrue(sameDeck, "同种子 → 洗牌结果逐张一致");

        // 换个种子应该不一样（防「压根没洗牌」这种假通过）
        var c = RuleCore.NewBattle(DeckBuilder.StarterDeck(pool, f, 30, new System.Random(7)),
                                   DeckBuilder.StarterDeck(pool, f, 30, new System.Random(9)),
                                   seed: 99999);
        bool differs = false;
        for (int i = 0; i < a.Players[0].Deck.Count; i++)
            if (a.Players[0].Deck[i].Name != c.Players[0].Deck[i].Name) { differs = true; break; }
        CheckTrue(differs, "换种子 → 洗牌结果不同（证明真的在洗）");
    }

    // ==================================================================
    //  端到端：用真卡表跑完一局
    // ==================================================================

    static int FirstFreeSlot(PlayerState p)
    {
        for (int s = 0; s < BoardSpec.Size; s++)
            if (BoardSpec.IsDeployable(s) && p.Board[s] == null) return s;
        return -1;
    }

    /// <summary>
    /// 贪心出牌 + 全量攻击。够验证「规则能跑完一局分出胜负」，不是 AI 设计。
    /// 打不死就靠疲劳分出结果 —— 那也顺带验了疲劳。
    /// </summary>
    static void PlayGreedyTurn(BattleContext ctx)
    {
        int p = ctx.Active;
        var ps = ctx.Players[p];

        // 出牌：反复挑「能打得起的最贵的一张」放到第一个空格
        for (int guard = 0; guard < 40 && !ctx.IsOver; guard++)
        {
            int best = -1, bestCost = -1;
            for (int i = 0; i < ps.Hand.Count; i++)
            {
                var card = ps.Hand[i];
                if (!card.IsUnit || card.Cost > ps.Energy) continue;
                if (card.Cost > bestCost) { bestCost = card.Cost; best = i; }
            }
            if (best < 0) break;
            int slot = FirstFreeSlot(ps);
            if (slot < 0) break;
            if (RuleCore.PlayCard(ctx, p, best, slot) != RuleCodes.OK) break;
        }

        // 攻击：每个己方单位打一次，目标按槽位顺序取第一个合法的
        for (int s = 0; s < BoardSpec.Size && !ctx.IsOver; s++)
        {
            var u = ps.Board[s];
            if (u == null || u.Exhausted) continue;
            if (u.IsWarlord && u.Attack <= 0) continue;
            for (int ts = 0; ts < BoardSpec.Size; ts++)
            {
                if (ctx.Players[1 - p].Board[ts] == null) continue;
                if (RuleCore.DeclareAttack(ctx, p, s, 1 - p, ts, ranged: false) == RuleCodes.OK) break;
            }
        }
    }

    static void TestFullGameWithRealCards()
    {
        var pool = CardDatabase.Load();
        if (pool.Count == 0) { CheckTrue(false, "卡表没加载上，端到端测试跳过"); return; }

        var factions = CardDatabase.Factions(pool);
        string f0 = factions[0];
        string f1 = factions.Count > 1 ? factions[1] : factions[0];

        var deck0 = DeckBuilder.StarterDeck(pool, f0, DeckBuilder.ClassicDeckSize, new System.Random(11));
        var deck1 = DeckBuilder.StarterDeck(pool, f1, DeckBuilder.ClassicDeckSize, new System.Random(22));

        Check(deck0.Count, DeckBuilder.ClassicDeckSize, $"P1 牌组凑满 {DeckBuilder.ClassicDeckSize} 张");
        Check(deck0[0].Type, "hero", "牌组第 0 张是督军");

        var curve = DeckBuilder.CostCurve(deck0);
        Debug.Log(P + "   P1 法术力曲线：" + string.Join(" ", curve));

        var ctx = RuleCore.NewBattle(deck0, deck1, seed: 20260911);
        Check(ctx.Players[0].Hand.Count, RuleCore.StartHand, "起手 3 张");

        int turnLimit = 200;
        while (!ctx.IsOver && ctx.Turn < turnLimit)
        {
            RuleCore.BeginTurn(ctx);
            PlayGreedyTurn(ctx);
            if (ctx.IsOver) break;
            RuleCore.EndTurn(ctx);
        }

        CheckTrue(ctx.IsOver, $"打完一局出结果了（共 {ctx.Turn} 回合，上限 {turnLimit}）");
        CheckTrue(ctx.Winner >= 1 && ctx.Winner <= 3, $"赢家 = {ctx.Winner}（1/2 胜，3 平局）");

        string who = ctx.Winner == 3 ? "平局" : $"P{ctx.Winner} 胜";
        Debug.Log(P + $"   对局结果：{who}，{ctx.Turn} 回合；"
                    + $"P1 督军剩 {Math.Max(0, ctx.Players[0].Warlord.Health)}，"
                    + $"P2 督军剩 {Math.Max(0, ctx.Players[1].Warlord.Health)}");
        Debug.Log(P + $"   末 6 条事件：");
        int from = Math.Max(0, ctx.Events.Count - 6);
        for (int i = from; i < ctx.Events.Count; i++) Debug.Log(P + "     · " + ctx.Events[i]);

        // 一局跑完账面要自洽
        CheckTrue(ctx.Players[0].Board[BoardSpec.WarlordSlot] != null, "督军始终留在槽 4 上");
        CheckTrue(ctx.Players[1].Board[BoardSpec.WarlordSlot] != null, "对方督军也在槽 4 上");
    }
}
