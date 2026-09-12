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

        // 现阶段门槛：骨架已通（有卡能完全解析）、且一张卡都没有解析成空表也不报错。
        // 每补一个 handler 就把这个数往上抬（抬的时候顺手在提交信息里记一笔）。
        CheckTrue(cov.Full >= 120, $"完全解析 {cov.Full}/448（门槛 120，逐步抬高）");
        CheckTrue(cov.SegUnknown > 0, "还有不认识的句子 —— 还没做完，如实报出来");
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
        Board(ctx, 0, 1).Exhausted = false;      // 强行解疲劳再打一次
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
        Board(ctx2, 0, 1).Exhausted = false;
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
