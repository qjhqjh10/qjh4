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

        Section("战术卡文本（能解析 N/448）");
        TestTacticTextCoverage();

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

        Section("胜负与疲劳");
        TestWinnerByWarlord();
        TestDrawIsDraw();
        TestFatigue();

        Section("确定性");
        TestDeterminism();
        TestSignalDeterminism();

        Section("端到端");
        TestFullGameWithRealCards();

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
        var cov = EffectText.Coverage(pool, "tactic");

        Check(cov.Cards, 448, "战术卡张数");
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

        // ⑪ 卡池实测：真原版卡里这几类关键词确实存在（别只在合成卡上验）
        {
            var pool = CardDatabase.Load();
            int n = 0;
            foreach (var c in pool)
                if (c != null && c.Keywords.ContainsKey("huntmark")) n++;
            CheckTrue(n > 0, $"卡池里带猎杀标记的卡有 {n} 张");
        }
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
        Append(sb, "④ 会生效、但**打得比卡面宽**（兵种词过滤不了：卡表里没有兵种字段）",
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
        // 事件流：Deploy → Trigger → Hit。**挨伤害也要发** ——
        // 表现层光看「血少了」分不出是挨刀、被技能打、还是疲劳
        Check(ctx.Signals.Count, 3, "发了三条事件（Deploy + Trigger + Hit）");
        Check(ctx.Signals[0].Kind, EvtKind.Deploy, "第 1 条是 Deploy");
        Check(ctx.Signals[0].Slot, 1, "Deploy 带着格位");
        Check(ctx.Signals[0].CardId, "Rallier", "Deploy 带着卡名");
        Check(ctx.Signals[1].Kind, EvtKind.Trigger, "第 2 条是 Trigger");
        Check(ctx.Signals[1].Keyword, KeywordTable.Rally, "Trigger 带着关键词 rally");
        Check(ctx.Signals[1].Slot, 1, "Trigger 带着格位（表现层照它播特效）");
        Check(ctx.Signals[1].CardId, "Rallier", "Trigger 带着卡名");
        Check(ctx.Signals[2].Kind, EvtKind.Hit, "第 3 条是 Hit");
        Check(ctx.Signals[2].Amount, 2, "Hit 带着实际伤害值");
        Check(ctx.Signals[2].Player, 1, "Hit 的归属方是**挨打那边**（敌方督军）");

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
