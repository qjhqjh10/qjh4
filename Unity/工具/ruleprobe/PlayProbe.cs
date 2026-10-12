using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RuleEngine;

/// <summary>
/// `play` —— **逐卡真打**：「这张卡打出去，局面的数字对不对」。
///
/// 🔴 **它修的是哪一类【结构性失明】**（台账 `A1431` · 出处
///   `资料/普查产出_第十四会话/逐卡效果检查_方法与边界.md` §1 / §2 / §4）：
///   在本子命令之前，本仓 **9 类**检查手段 —— `ruleprobe` 的 `check`/`scan`/`seg`/`card`/`b19test`/`jackal` ·
///   `RuleEngineTest.Run` 的四份 `_tmp_view` 报表 · `EffectText.Coverage` + `WillRunOps` + `UnimplementedKeywords` ·
///   `zh_crosscheck.py` · `check_pnp_cards.py` · `gen_icon_plan.py` —— **全部只回答
///   「这句英文今天解析成什么样」**，**没有一条回答「这张卡打出去数字对不对」**。
///   ⇒ 「**解析过、结算错**」这一整类（记账 7 个洞里有 4 个是它）**任何全池尺子都照不到**：
///     `check` 报「解析差异 0 行」、`OpHasMechanism` 全通、`Coverage` 四栏全 0。
///   ⚠️ 与 `WillRun.cs`（`A1432`）的分工：那条量「**卡面正文收到没有**」（注册层）；
///     本条量「**收到之后打出去，局面的数字动了没有**」（结算层）。**两条腿互相看不见对方的洞。**
///
/// **做法**（不是判结论，是**把数字摆出来**）：一局固定的、种子化的夹具对战 ——
///   两边各一位督军 + 各 3 张固定部队摆好场 ⇒ **把被测那张卡打进手牌并对准它打一发** ⇒
///   打之前/之后的**局面指纹**逐条对照（能/棋盘每格/手牌·牌库·弃牌堆/伤害累计）。
///
/// 🔴 **它【不】判「对错」** —— 判据在**卡面**（`desc` / `descZh` / PnP 成品卡图）上，
///   本工具**只把读数摆出来**（读数与卡面并排，判决归人 / 调度台）。
///   ⛔ **绝不用我们自己的实现反填期望值**（`逐卡效果检查_方法与边界.md` §四 那条纪律：
///    「一份夹具的判据必须写死在卡面上」，拿实现反填就是「拿我们的实现证明我们的实现」）。
///
/// ⛔ **本文件一行都不改引擎**（`MyGame/Assets/**` 不在本工具的白名单里）。
/// ⚠️ 它是【快速内环 / 旁证】：正式验收仍是 `RuleEngineTest.Run`；但它不跑 Unity（整卡 &lt; 1 秒）。
/// </summary>
class PlayProbe
{
    // ==================================================================
    //  夹具常量（**全池同一份**，所以读数能横向比）
    // ==================================================================

    /// <summary>夹具牌库里的垫牌张数 —— 只为让「牌库还剩几张」这一列有个非零起点。</summary>
    const int FillerCount = 30;

    /// <summary>夹具的起始能量 —— ⛔ 不按卡面费用给，免得「打不出去」里混进「能量不够」这一档噪声。
    /// 全池印得最高的费用现读 **14**（`cost` 列）⇒ 20 留了余量。</summary>
    const int FixtureEnergy = 20;

    /// <summary>夹具的能量上限 —— 🔴 **必须留余量**：`Refill all your Energy` 那一族
    /// （`AM57 High Command`）在「能=上限」时**本来就什么都没发生** ⇒ 拿它当缺陷就是误报。
    /// 30 &gt; 起始 20 ⇒ 灌满看得见。</summary>
    const int FixtureMaxEnergy = 30;

    /// <summary>摆场之后把**我方**三张部队各扣多少血 —— 🔴 不扣的话，全池 `Heal …` 那一族
    /// （`AM52 Regimental Doctrine` / `BL61 Delightful Agonies` / `DA8 Apothecary` …）
    /// **满血治疗 = 什么都不发生** ⇒ 全都会被误列成候选。扣 2 点既让治疗看得见、又不致死。</summary>
    const int FixtureChip = 2;

    /// <summary>夹具手牌张数 —— 3 张摆场部队 + 1 张被测卡。</summary>
    const int FixtureHand = 4;

    // ==================================================================
    //  读数
    // ==================================================================

    /// <summary>一张卡的「真打读数」。⛔ 这里面**没有任何期望值** —— 只有实得。</summary>
    class Reading
    {
        public CardDef C;
        public string How = "";              // 怎么打的（`PlayCard` / `PlayTactic` / 不可打）
        public int Rc = -1;                  // `RuleCore.PlayCard` / `PlayTactic` 的返回码
        public string Note = "";             // 打不出去时的原因（人话）
        public bool Played;                  // `Rc == RuleCodes.OK`
        public List<string> Changes = new List<string>();   // 逐条变化（打前 → 打后）
        public int SelfPlace;                // 其中「这张卡自己落场」占了几条
        public int Paid;                     // 其中**恰好等于打出费用**那一条能量变动占了几条
        public int Mech;                     // 其中「出牌本身」的机械动作（手牌−1 / 进弃牌堆）占了几条
        public int Other;                    // 除上面三类之外的条数 ⇒ **这一格就是「有没有实得」**
        public int ExpectedPay = -1;         // 照卡面应付（卡费 ＋ 付费能力前缀）；-1 = 这一档不算（督军）
        public int Dmg;                      // `DamageToEnemy[0]` 的增量
        public string Crash;                 // 抛异常时的 `类型: 消息`（⛔ 不许静默）
        public bool FaceHasOps;              // 卡面（`desc` 或注册层）上有没有可执行正文
        public string Before = "", After = "";   // 局面指纹（两行）
        public bool HeroMode;                // 督军走「对照局」那条路（见 <see cref="MeasureHero"/>）
        /// <summary>🆕 `A1572`：这一张是**夹具补进手牌**的（不是走自然上手那条路）——
        /// 两种卡**天然**到不了本夹具那一步：临时卡（`ephemeral`）会被回合末的 `SweepEphemeral` 移出游戏、
        /// 防御卡（`defence`）由 `BuildPlayer` 分流给**后手**（而本夹具的 P0 是先手）。
        /// ⚠️ 它是个**读数标记**：这一格为真的行 = 「量具干预过」，判读时与自然上手那批**不等价**。</summary>
        public bool Injected;
        public List<string> ControlChanges = new List<string>();   // 督军专用：空督军对照局的变化
        public string Bucket = "";           // 扫描时算的「档」（见 <see cref="Bucket"/>）
        public int EnergyDelta;              // 能量净变动（读数用）
        public int HandCostDelta;            // 手牌总价净变动（读数用）
        /// <summary>打出去之后**引擎自己的事件日志**末尾几行 —— 🔴 少了它，「什么都没发生」就只能靠猜；
        /// 有了它，引擎自己会说出「查不到 / 没有合法目标 / 这一项没生效」那类话（本仓红线「不许静默失败」）。</summary>
        public List<string> LogTail = new List<string>();
    }

    /// <summary>局面指纹 —— 逐格 / 逐区域。**只打实得数值，不打判据。**
    /// 🔴 手牌/弃牌堆存的是**逐张 id 清单**（⛔ 不是张数）—— 张数会把「打出去的那一张」与
    ///   「效果生成/抽到的那一张」**抵消成 0**，于是「`Create a X in your hand` 那条根本没跑」
    ///   看起来和「跑了」**一模一样**（张数都是 0 差）。同名重复没法靠张数分开，靠 id 清单能。</summary>
    class Snap
    {
        public readonly int[] Energy = new int[2];
        public readonly string[][] Cells = new string[2][];
        public readonly List<string>[] HandIds = { new List<string>(), new List<string>() };
        public readonly List<string>[] DeckIds = { new List<string>(), new List<string>() };
        public readonly List<string>[] DiscIds = { new List<string>(), new List<string>() };
        public readonly int[] HandCost = new int[2];
        /// <summary>🆕 **阵营资源三件**（信仰 / 灵魂石 / 任务点）——
        /// 不加的话 `Gain 5 Spirit Stones`（`ASH61 Infinity Circuit`）这类卡会**被误列成候选**
        /// （它明明改了资源，只是读数里没这一格）。</summary>
        public readonly int[] Faith = new int[2];
        public readonly int[] Spirit = new int[2];
        public readonly int[] Quest = new int[2];
        public readonly int[] Dmg = new int[2];
        public readonly int[] Troops = new int[2];
    }

    // ==================================================================
    //  入口
    // ==================================================================

    /// <summary>`play &lt;id|卡名&gt;` 单卡 · `play all [dump &lt;out.tsv&gt;] [pf &lt;out.txt&gt;]` 全池 ·
    /// 🆕 `play check [&lt;pf.txt&gt;]`（`A1431` 第二笔：**结算层对拍** —— 见 <see cref="Check"/>）。
    /// 返回**退出码**（判据见 <see cref="RunOne"/> 尾部 · <see cref="Sweep"/> 尾部 · <see cref="Check"/> 尾部）。</summary>
    public static int Run(List<CardDef> pool, string key, string dumpPath, string pfPath = null, string basePath = null)
    {
        if (pool == null || pool.Count == 0) { Console.Error.WriteLine("!! 卡池是空的"); return 2; }
        if (string.IsNullOrEmpty(key))
        {
            Console.Error.WriteLine("!! 用法：play <id|卡名> | play all [dump <out.tsv>] [pf <out.txt>] | play check [<pf.txt>]");
            return 2;
        }
        if (string.Equals(key, "all", StringComparison.OrdinalIgnoreCase)) return Sweep(pool, dumpPath, pfPath);
        if (string.Equals(key, "check", StringComparison.OrdinalIgnoreCase)) return Check(pool, basePath);

        var hit = Find(pool, key);
        if (hit == null) return 2;
        var r = Measure(pool, hit);
        Print(r);
        // 🔴 退出码只认**硬失败**（⛔ 不拿「候选」当失败 —— 那会把「红了不用管」训练成习惯）：
        //   · 崩了（引擎抛异常）= 缺陷，必须红；
        //   · `ErrUnimplemented` = 这张卡**根本打不出去**（`CanPlayTactic` 要求
        //     `IsFullyParsed`）⇒ 卡面上写着正文而它连打都打不出去 = 缺陷，必须红。
        //   ⚠️ `ErrNoTargetAvailable`（`A985⑧` 拆出来的那一档）**不算** ——
        //     那是**夹具里没有合法目标**（本夹具只有 `Infantry` 一个兵种、没有隐秘/飞行/载具），
        //     是**量具的限制**，不是卡的缺陷。如实打在读数里，⛔ 不混进失败。
        if (r.Crash != null) return 1;
        if (r.Rc == RuleCodes.ErrUnimplemented) return 1;
        return 0;
    }

    static CardDef Find(List<CardDef> pool, string key)
    {
        // ⚠️ **卡名不是唯一键**（全池 3 个重名）⇒ 先按 `id`（唯一），再按卡名**逐个列出**。
        foreach (var c in pool)
            if (string.Equals(c.Id, key, StringComparison.OrdinalIgnoreCase)) return c;
        var byName = pool.Where(c => string.Equals(c.Name, key, StringComparison.OrdinalIgnoreCase)).ToList();
        if (byName.Count == 1) return byName[0];
        if (byName.Count > 1)
        {
            Console.Error.WriteLine("!! 卡名 `" + key + "` 有 " + byName.Count + " 张同名卡（卡名不是唯一键）—— 请按 `id` 指：");
            foreach (var c in byName) Console.Error.WriteLine("     " + c.Id + "  " + c.Name + "  [" + c.Type + "]");
            return null;
        }
        Console.Error.WriteLine("!! 卡池里没有 `" + key + "`（既不是 id 也不是卡名）");
        return null;
    }

    // ==================================================================
    //  夹具
    // ==================================================================

    static CardDef HeroOf(string id, string fac) =>
        new CardDef(id, id, "hero", "", null, fac, 0, 2, 30, 0, null, subtype: "Warlord");

    static CardDef Fix(string id, int atk, int hp, int rng, string subtype = "Infantry") =>
        new CardDef(id, id, "unit", "", null, "Fixture", 1, atk, hp, rng, null, subtype: subtype);

    /// <summary>垫牌用到的兵种（**轮着来**）—— `Draw a Beast` / `The next Vehicle` / `Deploy 3 random Drones`
    /// 那一族要**牌库/手里真有那种兵种**才可能有效果，⛔ 全用 `Infantry` 会把它们整族误列成候选。</summary>
    static readonly string[] FillerSubtypes = { "Infantry", "Vehicle", "Beast", "Drone" };

    /// <summary>垫牌里每几张有一张是**战术卡** —— `Draw a Stratagem` / `Your next Stratagem costs 1 less`
    /// 那一族要有「牌库/手里真有战术卡」才可能有效果（⛔ 全给单位 ⇒ 整族误列成候选）。</summary>
    const int FillerTacticEvery = 5;

    /// <summary>牌库 = 督军 + 垫牌 + `hand`（**倒着加**：抽牌从尾部 pop ⇒ `hand[0]` 最先上手，顺序与原样一致）。
    /// 形状照 `BattleProbe.DeckOf`（那份与 `RuleEngineTest` 对齐过）。</summary>
    static List<CardDef> Deck(string fac, params CardDef[] hand)
    {
        var d = new List<CardDef> { HeroOf("FIXWarlord_" + fac, fac) };
        for (int i = 0; i < FillerCount; i++)
        {
            if (i % FillerTacticEvery == FillerTacticEvery - 1)
                d.Add(new CardDef("filler" + i, "filler" + i, "tactic", "", null, "Fixture", 1, 0, 0, 0, null));
            else
                d.Add(Fix("filler" + i, 0, 1, 0, FillerSubtypes[i % FillerSubtypes.Length]));
        }
        for (int i = hand.Length - 1; i >= 0; i--) d.Add(hand[i]);
        return d;
    }

    static int HandIdx(BattleContext ctx, int p, CardDef c)
    {
        var h = ctx.Players[p].Hand;
        for (int i = 0; i < h.Count; i++) if (h[i].Card == c) return i;
        return -1;
    }

    static void SetEnergy(BattleContext ctx, int p)
    {
        ctx.Players[p].Energy = FixtureEnergy;
        ctx.Players[p].MaxEnergy = FixtureMaxEnergy;
    }

    /// <summary>摆场：两边各 3 张固定部队，落在 3 / 6 / 7 三格（**贴着督军格 4、且有相邻对 6-7**）——
    /// 这样「相邻」族与「督军旁边的」族都有活目标。</summary>
    static readonly int[] DeploySlots = { 3, 6, 7 };

    /// <summary>建一局夹具对战。`warlordOverride` 非 null 时用它当 P0 督军（督军档的对照局用）。
    /// 🔴 `injected`（`A1572`）：被测卡**是**靠补手牌才进得了手牌的那种（= 量具干预过）——
    ///    两族：临时卡 / 防御卡，见下面那一段注释。</summary>
    static BattleContext BuildFixture(List<CardDef> pool, CardDef target, CardDef warlordOverride,
                                      out int handIdx, out bool injected)
    {
        var vars = GameplayVariables.Classic.Clone();
        vars.startingHand = FixtureHand;      // ⚠️ 只改「发几张」—— 别的一律照 `Classic`
        injected = false;                     // 见函数头：只有真走补手牌那一步才置真

        // 兵种**故意分散**（Infantry / Vehicle / Beast）—— 卡面上 `a friendly Vehicle` /
        // `your Beasts` 那一族才找得到目标（⛔ 全用 `Infantry` 会把整族误列成「没目标」）。
        var a = Fix("FIX_A", 2, 4, 0, "Infantry");
        var b = Fix("FIX_B", 3, 5, 0, "Vehicle");
        var c = Fix("FIX_C", 1, 4, 0, "Beast");
        var e1 = Fix("FIX_E1", 2, 6, 0, "Infantry");
        var e2 = Fix("FIX_E2", 3, 6, 0, "Vehicle");
        var e3 = Fix("FIX_E3", 1, 6, 0, "Beast");

        // ⚠️ **督军卡不进牌库**（`BuildPlayer` 把第一张 `type == "hero"` 当督军）
        //    ⇒ 督军档走 `warlordOverride`，被测卡不参与发牌（见 `MeasureHero`）。
        var hand = target != null && target.Type != "hero"
            ? new[] { a, b, c, target }
            : new[] { a, b, c };
        var d0 = Deck("P0", hand);
        // 督军档的**对照局**：只把 P0 督军换成指定的那一张，别的逐字不变。
        if (warlordOverride != null) { d0.RemoveAt(0); d0.Insert(0, warlordOverride); }
        var d1 = Deck("P1", e1, e2, e3);

        var ctx = RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, cardPool: pool, vars: vars);

        SetEnergy(ctx, 0);
        for (int i = 0; i < 3; i++)
        {
            int hi = HandIdx(ctx, 0, hand[i]);
            if (hi >= 0) RuleCore.PlayCard(ctx, 0, hi, DeploySlots[i]);
        }
        RuleCore.EndTurn(ctx); RuleCore.BeginTurn(ctx);      // → P1
        SetEnergy(ctx, 1);
        RuleCore.PlayCard(ctx, 1, HandIdx(ctx, 1, e1), DeploySlots[0]);
        RuleCore.PlayCard(ctx, 1, HandIdx(ctx, 1, e2), DeploySlots[1]);
        RuleCore.PlayCard(ctx, 1, HandIdx(ctx, 1, e3), DeploySlots[2]);
        RuleCore.EndTurn(ctx); RuleCore.BeginTurn(ctx);      // → P0（被测卡这一手）

        SetEnergy(ctx, 0);
        // 🔴 **把我方三张部队各扣 2 血**（见 `FixtureChip`）—— 不扣的话满血治疗看不见。
        //    ⚠️ 走引擎自己的 `ApplyDamage`（⛔ 不直接改 `u.Health`）：它会走盾/闪避/触发那一套，
        //       与我们手改出来的状态**不同口径**。它引发的那些副作用都发生在**基线快照之前**。
        for (int s = 0; s < BoardSpec.Size; s++)
        {
            var u = ctx.Players[0].Board[s];
            if (u != null && !u.IsWarlord) RuleCore.ApplyDamage(ctx, u, FixtureChip, "夹具扣血");
        }
        // 🔴 **我方督军也扣 2 点** —— 不扣的话 `Heal 4 to your Warlord`（`SW66`）/`Heal N to your Warlord`
        //    那一族**满血治疗 = 什么都不发生**，会被整族误列成候选（与上面三张同一个道理）。
        RuleCore.ApplyDamage(ctx, ctx.Players[0].Warlord, FixtureChip, "夹具扣血");
        handIdx = target == null ? -1 : HandIdx(ctx, 0, target);
        // 🔴 **被测卡没活到这一手时，按引擎自己的形状补进手牌**（`A1431` 实测：不补会漏掉 **137 张**）：
        //   · **临时卡（`ephemeral`，98 张 `tactic`）** —— `RuleCore.EndTurn:1340` 的 `SweepEphemeral`
        //     在回合结束时把本方手牌里的临时卡**弃掉** ⇒ 跨一个来回之后它**不在手里**
        //     （这是引擎**对的**行为，不是缺陷 —— 是夹具要照顾它）。
        //   · **防御卡（39 张）** —— `BuildPlayer` 把 `type == "defence"` 从牌库里**分流出去**
        //     （不进牌库、由 `GrantDefenceCard` 发给**后手**）⇒ 先手（P0）手里根本不会有它。
        //   ⇒ 形状照引擎自己的 `RuleCore.SetDefensiveCard`：`NewInstance` → `Hand.Add` → `SetupCardInHand`
        //     （⛔ 不自己拼一个「半份实例」，那会与引擎口径不同）。
        //   ⚠️ 这一段跑在 `MeasurePlay` 取**基线快照之前** ⇒ 补进来的那一张**在读数里看得见**。
        if (target != null && handIdx < 0)
        {
            var late = ctx.NewInstance(target);
            ctx.Players[0].Hand.Add(late);
            RuleCore.SetupCardInHand(ctx, 0, late);
            handIdx = HandIdx(ctx, 0, target);
            injected = true;
        }
        return ctx;
    }

    // ==================================================================
    //  单卡读数
    // ==================================================================

    static Reading Measure(List<CardDef> pool, CardDef card)
    {
        var r = new Reading { C = card };
        try
        {
            r.FaceHasOps = FaceHasOps(card);
            r.Bucket = Bucket(card);
            if (card.Type == "hero") MeasureHero(pool, card, r);
            else MeasurePlay(pool, card, r);
        }
        catch (Exception ex)
        {
            // 🔴 引擎抛异常 = 缺陷。**出声**（⛔ 不许静默）—— 但⛔ 不在这里判因由。
            r.Crash = ex.GetType().Name + ": " + ex.Message;
            r.Note = "**真打时引擎抛异常**";
        }
        return r;
    }

    static void MeasurePlay(List<CardDef> pool, CardDef card, Reading r)
    {
        int handIdx;
        bool injected;
        var ctx = BuildFixture(pool, card, null, out handIdx, out injected);
        r.Injected = injected;
        if (handIdx < 0)
        {
            // 🔴 **夹具没接住要出声、而且要说清手里有什么** —— ⛔ 不静默（本仓红线）。
            r.Before = "手牌 = " + List(ctx.Players[0].Hand.Select(x => x.Card.Id).ToList())
                     + " · 牌库 " + ctx.Players[0].Deck.Count + " 张 ["
                     + string.Join(",", ctx.Players[0].Deck.Select(x => x.Card.Id)) + "]"
                     + " · 弃牌 = " + List(ctx.Players[0].Discard.Select(x => x.Card.Id).ToList())
                     + "\n     P0 棋盘 " + Board((from s in Enumerable.Range(0, BoardSpec.Size)
                                                select Cell(ctx.Players[0].Board[s])).ToArray());
            r.Note = "**夹具没接住**：被测卡 `" + card.Id + "` 打出去的时候不在手牌里（" + r.Before + "）";
            return;
        }

        var before = Capture(ctx);
        var inst = ctx.Players[0].Hand[handIdx];
        string playedEntry = HandEntry(inst);   // ⚠️ 必须在打出去【之前】取（打完就离开手牌了）
        // 🔴 **照卡面应付多少**（在打出去【之前】算）—— 用来把「付费那一条能量变动」从
        //    「实得变化」里摘出去。⛔ 别用「能量变了就算付费」那种近似：
        //    `Refill 2 Energy` 类的卡**效果本身就是改能量**，按近似会被整张漏掉。
        //    ⚠️ **应付 ≠ 卡的 `cost`**：卡面上 `N Energy:` 那种**付费能力前缀**是**另付**的
        //    （`EffectOp.Cost > 0 && !CostShared` 的那几条，实测 `SOR6` = 卡费 2 + 前缀 4 = 6）。
        //    ⚠️ 单位卡的 `2 ☀: …` 段**不在部署这一步付**（归 `UseAbility`）⇒ 单位卡不加前缀那部分。
        int expectedPay = RuleCore.CostOf(ctx, 0, inst);
        int instCost = expectedPay;      // 这张牌**自己**的价（= 手牌总价里该减掉的那一份）
        if (!card.IsUnit)
            foreach (var op in EffectText.Parse(card.Desc, out _, out _))
                if (op.Cost > 0 && !op.CostShared) expectedPay += op.Cost;
        r.ExpectedPay = expectedPay;

        if (card.IsUnit)
        {
            int slot = FreeSlot(ctx, 0);
            r.How = "`RuleCore.PlayCard`（落第 " + slot + " 格）";
            r.Rc = RuleCore.PlayCard(ctx, 0, handIdx, slot);
        }
        else
        {
            var ops = EffectText.Parse(card.Desc, out _, out _);
            var spec = EffectText.PickTarget(ops);
            int tslot = spec == null ? -1 : PickSlot(ctx, 0, spec);
            r.How = "`RuleCore.PlayTactic`"
                  + (spec == null ? "（不需选目标）" : "（目标侧=`" + spec.Side + "` 格=" + tslot + "）");
            r.Rc = RuleCore.PlayTactic(ctx, 0, handIdx, tslot);
        }

        var after = Capture(ctx);
        Diff(before, after, r.Changes);
        r.LogTail = ctx.Events.Skip(Math.Max(0, ctx.Events.Count - 14)).ToList();
        r.Before = Fingerprint(before);
        r.After = Fingerprint(after);
        r.Played = r.Rc == RuleCodes.OK;
        if (!r.Played) r.Note = "打不出去：" + CodeName(r.Rc) + "（" + CodeWhy(r.Rc) + "）";

        // 分类（**这几格就是「有没有实得」**）—— 前四类是**「打出去这个动作本身」**，不算效果：
        //   · 能量那一条 —— **恰好等于照卡面应付的**（卡费 ＋ 付费能力前缀）⇒ 只是付费；
        //   · 手牌「减掉被测那一张」与弃牌堆「加上被测那一张」（单位卡不进弃牌堆）；
        //   · 「自身落场」= 我方棋盘上「原本是空格、现在出现了这张卡的 id」的格数；
        //   · 其余一律算实得。
        int eDelta = after.Energy[0] - before.Energy[0];
        int netEnergy = eDelta + expectedPay;
        r.EnergyDelta = eDelta;
        r.HandCostDelta = after.HandCost[0] - before.HandCost[0];
        // 🔴 单位卡：这张卡**刚落地时「应该是」什么样** —— 拿引擎**自己**的初值算
        //    （`new UnitState(CardInstance.Detached(card), false)`，≡ `PlayCard` 里那一句）。
        //    ⚠️ **不能只按「那一格从空变成了这张卡」就一律算「自身落场」** —— 实测 `BL13 Chaos Sergeant`
        //       （卡面 `2/1/2`、`keywords=[rally1]`）落地时是 `4/3/0/2 {darkpact1, rally1}`：
        //       **Rally 的效果就发生在同一格里**，一律当「自身落场」会把**它整个吞掉**（误报成候选）。
        string naiveCell = card.IsUnit ? Cell(new UnitState(CardInstance.Detached(card), false)) : null;
        string handLine = "✋ 手牌 P0 -「" + playedEntry + "」×1";
        string discLine = "🗑 弃牌 P0 +「" + card.Id + "」×1";
        for (int i = 0; i < r.Changes.Count; i++)
        {
            string ch = r.Changes[i];
            if (ch.StartsWith("⚙ 能量 P0"))
            {
                if (netEnergy == 0) { r.Paid++; continue; }
                // 付费之外还有净变化 ⇒ 这一条**是效果**（如实把净额写出来）
                r.Changes[i] = ch + "   ⇒ **照卡面应付 " + expectedPay + " · 除付费外净 "
                             + (netEnergy > 0 ? "+" : "") + netEnergy + "**";
                r.Other++;
                continue;
            }
            if (ch == handLine || (!card.IsUnit && ch == discLine)) { r.Mech++; continue; }
            // 手牌总价：**出牌本身**就会把它减掉「这张牌自己的价」⇒ 差额才是效果（减费/加费那一族）。
            if (ch.StartsWith("💰 手牌总价 P0"))
            {
                int net = (after.HandCost[0] - before.HandCost[0]) + instCost;
                if (net == 0) { r.Mech++; continue; }
                r.Changes[i] = ch + "   ⇒ **除出牌本身外净 " + (net > 0 ? "+" : "") + net + "**";
                r.Other++;
                continue;
            }
            // 单位卡：`PlayCard` 顺手把 `TroopsPlayed[p]` +1（那一步是**出牌本身**，不是效果）
            if (card.IsUnit && ch.StartsWith("🎖 出兵数 P0")) { r.Mech++; continue; }
            if (card.IsUnit && ch.StartsWith("✚ 落场/生成 P0["))
            {
                int arrow = ch.IndexOf(" → ", StringComparison.Ordinal);
                string cellAfter = arrow < 0 ? "" : ch.Substring(arrow + 3);
                if (cellAfter == naiveCell) { r.SelfPlace++; continue; }     // 就是「印的那些数」⇒ 纯落场
                if (cellAfter.StartsWith(card.Id + " "))
                {
                    // 同一格里就带着变化（`Rally:` 那种自指效果）⇒ **是效果**，⛔ 不许被「落场」吞掉
                    r.Changes[i] = ch + "   ⇒ **落地时它自己身上与印的数不同**（印的：`" + naiveCell + "`）";
                    r.Other++;
                    continue;
                }
                r.Other++;      // 生成的是**别的**卡 ⇒ 效果
                continue;
            }
            if (card.IsUnit && ch.Contains("✚ 落场") && ch.Contains("P0[") && ch.Contains(card.Id + " ")) r.SelfPlace++;
            else r.Other++;
        }
        r.Dmg = after.Dmg[0] - before.Dmg[0];
    }

    /// <summary>督军档 —— ⚠️ **督军没有「打出去」这个动作**（`PlayCard`/`PlayTactic` 都只认手牌里的非督军卡）
    /// ⇒ 改测「**这位督军在场上、过完一个来回**局面的实得变化」，并跑一局**空督军对照**
    /// （同种子、同摆场，只把 P0 督军换成一个 `FIXWarlord`）把「与督军无关的变化」减掉。
    /// ⛔ 这不是「我们挑的做法」—— 是因为督军**确实**只能这样测；如实打在读数里。</summary>
    static void MeasureHero(List<CardDef> pool, CardDef card, Reading r)
    {
        r.HeroMode = true;
        r.How = "督军**没有「打出去」这个动作**（`PlayCard`/`PlayTactic` 只处理非 hero）"
              + " ⇒ 本档测的是「**在场 + 一个来回**」的实得变化，并减掉一局【空督军对照】";

        var a = HeroPass(pool, card);
        var b = HeroPass(pool, null);
        r.ControlChanges = b.Changes;
        // 🔴 **净变化 = A − B**（两份逐条字符串相减）—— 两局只差 P0 督军那一张，
        //    所以这一栏才是**这位督军带来的**。⛔ 不拿 A 直接当读数（那里面大半是与督军无关的回合噪声）。
        r.Changes = a.Changes.Where(x => !b.Changes.Contains(x)).ToList();
        // 🔴 **静态那一半要单独算** —— 上面那份 diff 是「同一局内 before→after」，
        //    而**督军自己那一格**从头到尾都在场、数值没变过 ⇒ 它**永远不出现在 diff 里**。
        //    而「这位督军的攻/远/甲/血/关键词」恰恰是它最要紧的读数（例：`ASH3` 是 `2/2/0/40`，
        //    空白对照是 `2/0/0/30`）⇒ 直接比两趟**终局的督军格**。
        string wa = a.End.Cells[0][BoardSpec.WarlordSlot], wb = b.End.Cells[0][BoardSpec.WarlordSlot];
        if (wa != wb)
            r.Changes.Insert(0, "★ 督军格（本卡在场）" + wa + "  ‖ 对照局（空督军）" + wb);
        r.Before = "";
        r.After = Fingerprint(a.End);
        r.Played = true;
        r.Other = r.Changes.Count;      // 督军档不分类（付费/落场那套不适用），全部算「净变化」
    }

    /// <summary>跑一局：督军 = `card`（null ⇒ 空督军对照），过完 <c>P0 结束 → P1 开始+结束 → P0 开始</c>
    /// 一个来回，返回（变化逐条 · 终局快照）。</summary>
    static (List<string> Changes, Snap End) HeroPass(List<CardDef> pool, CardDef card)
    {
        int handIdx;
        bool injected;      // 督军档不用它（这条路上根本没有「补手牌」这一步）
        var ctx = BuildFixture(pool, null, card ?? HeroOf("FIXWarlord_P0", "Fixture"), out handIdx, out injected);
        var before = Capture(ctx);
        RuleCore.EndTurn(ctx); RuleCore.BeginTurn(ctx);      // → P1
        SetEnergy(ctx, 1);
        RuleCore.EndTurn(ctx); RuleCore.BeginTurn(ctx);      // → P0
        var after = Capture(ctx);
        var ch = new List<string>();
        Diff(before, after, ch);
        return (ch, after);
    }

    // ==================================================================
    //  局面指纹 / 差异
    // ==================================================================

    static Snap Capture(BattleContext ctx)
    {
        var s = new Snap();
        for (int p = 0; p < 2; p++)
        {
            var ps = ctx.Players[p];
            s.Energy[p] = ps.Energy;
            s.Cells[p] = new string[BoardSpec.Size];
            for (int i = 0; i < BoardSpec.Size; i++) s.Cells[p][i] = Cell(ps.Board[i]);
            foreach (var c in ps.Hand) s.HandIds[p].Add(HandEntry(c));
            foreach (var c in ps.Deck) s.DeckIds[p].Add(c.Card.Id);
            foreach (var c in ps.Discard) s.DiscIds[p].Add(c.Card.Id);
            s.HandIds[p].Sort(StringComparer.Ordinal);
            s.DeckIds[p].Sort(StringComparer.Ordinal);
            s.DiscIds[p].Sort(StringComparer.Ordinal);
            // 🆕 **手牌总价**（逐张走引擎的 `CostOf`）—— 用来让「手牌减费 / 下一张减费」那一族
            //    （`lowercost` / `next …`）**在读数里看得见**：它们**不改棋盘**，只改价格，
            //    光靠棋盘+区域三样是**照不到**的（记着的已知盲区）。
            int sum = 0;
            foreach (var c in ps.Hand) sum += RuleCore.CostOf(ctx, p, c);
            s.HandCost[p] = sum;
            s.Faith[p] = ps.Faith;
            s.Spirit[p] = ps.SpiritStones;
            s.Quest[p] = ps.QuestPoints;
            s.Dmg[p] = ctx.DamageToEnemy[p];
            s.Troops[p] = ctx.TroopsPlayed[p];
        }
        return s;
    }

    /// <summary>一格 = `id 攻/远/甲/血 {关键词…}`（空格 = `.`）。
    /// ⚠️ 督军那格打 `WARLORD` 而**不打它的 id** —— 那是为了让**对照局**（督军档）的两份指纹可比
    /// （id 不同 ≠ 局面不同）；督军自己的数值与关键词照打。</summary>
    static string Cell(UnitState u)
    {
        if (u == null) return ".";
        var sb = new StringBuilder();
        sb.Append(u.IsWarlord ? "WARLORD" : u.Card.Id);
        sb.Append(' ').Append(u.Attack).Append('/').Append(u.RangedAttack).Append('/')
          .Append(u.Armor).Append('/').Append(u.Health);
        var kw = u.Keywords;
        if (kw != null && kw.Count > 0)
        {
            sb.Append(" {");
            bool first = true;
            foreach (var k in kw.OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                if (!first) sb.Append(", ");
                first = false;
                sb.Append(k.Key);
                if (k.Value != 0) sb.Append(k.Value);
            }
            sb.Append('}');
        }
        return sb.ToString();
    }

    static string Fingerprint(Snap s)
    {
        return "P0 能=" + s.Energy[0] + " 手=" + List(s.HandIds[0]) + " 手牌总价=" + s.HandCost[0]
             + " 库=" + s.DeckIds[0].Count
             + " 弃=" + List(s.DiscIds[0]) + " 伤害=" + s.Dmg[0] + " 出兵=" + s.Troops[0]
             + " 信仰=" + s.Faith[0] + " 灵魂石=" + s.Spirit[0] + " 任务点=" + s.Quest[0]
             + "\n     P0 棋盘 " + Board(s.Cells[0])
             + "\n     P1 棋盘 " + Board(s.Cells[1])
             + "   （P1 能=" + s.Energy[1] + " 手=" + List(s.HandIds[1]) + " 库=" + s.DeckIds[1].Count
             + " 弃=" + List(s.DiscIds[1]) + " 伤害=" + s.Dmg[1] + "）";
    }

    static string List(List<string> l) { return l.Count == 0 ? "(空)" : "[" + string.Join(",", l) + "]"; }

    /// <summary>手牌里一张牌的读数键 —— 🔴 **带上它的手牌增益条数**：
    /// 手牌加成（`Give +1 [Attack] to a random troop in your hand`，`GOF99`）**不改卡的定义、也不改棋盘**，
    /// 不加这一格就**整族照不到**（会被误列成候选）。`*N` = 这张牌身上挂了几条 `HandEffect`。</summary>
    static string HandEntry(CardInstance c)
    {
        int n = c.HandBuffOps == null ? 0 : c.HandBuffOps.Count;
        return n == 0 ? c.Card.Id : c.Card.Id + "*" + n;
    }

    static string Board(string[] cells)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < cells.Length; i++) sb.Append('[').Append(cells[i]).Append(']');
        return sb.ToString();
    }

    /// <summary>逐字段比。**只报实得差异**，⛔ 不在这里判对错。
    /// 区域（手牌/牌库/弃牌堆）按 **id 多重集**比 —— 见 <see cref="Snap"/> 的注释。</summary>
    static void Diff(Snap a, Snap b, List<string> into)
    {
        for (int p = 0; p < 2; p++)
            if (a.Energy[p] != b.Energy[p])
                into.Add("⚙ 能量 P" + p + " " + a.Energy[p] + " → " + b.Energy[p]
                         + "（" + (b.Energy[p] - a.Energy[p]) + "）");
        for (int p = 0; p < 2; p++)
            for (int i = 0; i < BoardSpec.Size; i++)
                if (!string.Equals(a.Cells[p][i], b.Cells[p][i], StringComparison.Ordinal))
                {
                    string tag = b.Cells[p][i] == "." ? "☠ 离场"
                               : a.Cells[p][i] == "." ? "✚ 落场/生成"
                               : "✎ 改动";
                    into.Add(tag + " P" + p + "[" + i + "] " + a.Cells[p][i] + " → " + b.Cells[p][i]);
                }
        for (int p = 0; p < 2; p++)
        {
            DiffIds("✋ 手牌 P" + p, a.HandIds[p], b.HandIds[p], into);
            DiffIds("🂠 牌库 P" + p, a.DeckIds[p], b.DeckIds[p], into);
            DiffIds("🗑 弃牌 P" + p, a.DiscIds[p], b.DiscIds[p], into);
            if (a.HandCost[p] != b.HandCost[p])
                into.Add("💰 手牌总价 P" + p + " " + a.HandCost[p] + " → " + b.HandCost[p]
                         + "（" + (b.HandCost[p] - a.HandCost[p]) + "）");
            if (a.Faith[p] != b.Faith[p]) into.Add("🔮 信仰 P" + p + " " + a.Faith[p] + " → " + b.Faith[p]);
            if (a.Spirit[p] != b.Spirit[p]) into.Add("🔮 灵魂石 P" + p + " " + a.Spirit[p] + " → " + b.Spirit[p]);
            if (a.Quest[p] != b.Quest[p]) into.Add("🔮 任务点 P" + p + " " + a.Quest[p] + " → " + b.Quest[p]);
            if (a.Dmg[p] != b.Dmg[p]) into.Add("☠ 伤害累计 P" + p + " " + a.Dmg[p] + " → " + b.Dmg[p]);
            if (a.Troops[p] != b.Troops[p]) into.Add("🎖 出兵数 P" + p + " " + a.Troops[p] + " → " + b.Troops[p]);
        }
    }

    /// <summary>两个 id 清单（排序过）的多重集差，按 id 出「加了/减了各几张」。</summary>
    static void DiffIds(string label, List<string> a, List<string> b, List<string> into)
    {
        var keys = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var k in a) keys.Add(k);
        foreach (var k in b) keys.Add(k);
        foreach (var k in keys)
        {
            int da = a.Count(x => x == k), db = b.Count(x => x == k);
            if (da == db) continue;
            into.Add(label + " " + (db > da ? "+" : "-") + "「" + k + "」×" + Math.Abs(db - da));
        }
    }

    static int FreeSlot(BattleContext ctx, int p)
    {
        // 先挑贴着督军 / 有相邻的那几格（「相邻」族要有活目标），再退到其余格。
        foreach (int s in new[] { 3, 5, 2, 6, 1, 7, 0, 8 })
            if (ctx.Players[p].Board[s] == null) return s;
        return 3;
    }

    /// <summary>「要选目标」的战术卡：挑一个**该侧真有单位**的格（先非督军格，没有才用督军格 4）。
    /// ⚠️ 这只是**夹具怎么点**，不是判据 —— 选错时 `CanPlayTactic` 会返回 `ErrNoTargetAvailable`，如实打出来。</summary>
    static int PickSlot(BattleContext ctx, int p, EffectTargetSpec spec)
    {
        int side = spec.Side == "enemy" ? 1 - p : p;
        var b = ctx.Players[side].Board;
        for (int i = 0; i < BoardSpec.Size; i++)
            if (i != BoardSpec.WarlordSlot && b[i] != null) return i;
        return BoardSpec.WarlordSlot;
    }

    static bool FaceHasOps(CardDef c)
    {
        var won = EffectText.WillRunOps(c);
        if (won != null && won.Count > 0) return true;
        var ops = EffectText.Parse(c.Desc, out _, out _);
        return ops != null && ops.Count > 0;
    }

    // ==================================================================
    //  档 —— 「**在本夹具里它应不应该有实得变化**」
    // ==================================================================
    //
    // 🔴 **为什么必须有这一层**：`other == 0` 这个判据**本身不区分**
    //   「该发生而没发生」（= `A1431` 那一类）与「**本夹具产生不了那个事件**」。
    //   实测反例（不加这一层时误列进候选）：`AM12 Autocannon Squad` 的 `Regiment: Gain +1 [Ranged]`
    //   —— `Regiment`（团）**不是部署时触发**，它是**攻击时**触发
    //   （`RuleCore.cs:4027-4031`：`团（Regiment）：和 Mob 成对，差别只在远程`；
    //    消费点 = `FireTriggerAt(ctx, attacker, KeywordTable.Regiment, p, atkSlot)`）⇒
    //   本夹具只「打出一发」、不让单位攻击 ⇒ **它不该有变化**，报成候选就是**误报**。
    //   ⇒ 判据全部**转调引擎的公开面**（`EffectText.Split` / `StripLeadingIcons` /
    //     `CardDef.RoutableTriggers` / `CardDef.HandledByOtherLayer`），⛔ 探针里不另写关键词表。
    //
    // 档（打印顺序 = 可疑度从高到低）：
    //   · **A 打出即结算** —— `tactic` / `defence`。判据：这两类的 `WillRunOps` **就是**
    //     `Parse(Desc)`（见 `willrun` README「四条别推翻的取舍」1）⇒ 打出**就是**触发，
    //     没有任何「等事件」可以解释 → **本档的候选最可疑**。
    //   · **C 部署时该发生** —— `unit`，卡面里存在**不带触发头**的正文段（`Head:` 缺失，或无头的
    //     部署时正文），或头是 `rally`（`Rally:` = 部署时触发）⇒ 出牌那一拍就该有变化。
    //   · **B 等事件 / 等主动用** —— `unit`，每一段正文都挂在一个**别的时机**的头上
    //     （`Strike`/`Regiment`/`Mob`/`Slay`/`Duty`/`Agenda`/`Oath`/`Teleport`/`Ambush`/`Codex`/
    //     `Ecstasy`/`When …` …）⇒ **本夹具产生不了那个事件**，`other == 0` 是**预期的**、
    //     ⛔ 不是缺陷（要测它得另写「打一发 / 让它死 / 让它抽到」那种专属夹具）。
    //   · **H 督军档** —— 另法测（见 <see cref="MeasureHero"/>）。
    static string Bucket(CardDef c)
    {
        if (c.Type == "hero") return "H 督军档";
        if (c.Type == "tactic" || c.Type == "defence") return "A 打出即结算";
        bool unguarded = false;
        foreach (var raw in EffectText.Split(c.Desc))
        {
            if (string.IsNullOrEmpty(raw)) continue;
            string s = EffectText.StripLeadingIcons(raw.Trim());
            if (string.IsNullOrEmpty(s)) continue;
            int col = s.IndexOf(':');
            string head = col <= 0 ? null : s.Substring(0, col).Trim().ToLowerInvariant();
            // ⚠️ **不是可执行正文段的直接跳过**（`Waystone.` / `Stealth.` / `Armour 1.` 这种裸关键词句）
            //    —— 不加这一道，`ASH_Death_Spinner_Warp_Spider` 的 `Waystone. Stealth. …` 会被判成
            //    「无头 + 该发生」（那两个词根本没有 `:`，`HeadOfBody` 当然查不到），**整整一族误报**。
            string probe = head == null ? s : s.Substring(col + 1).Trim();
            if (string.IsNullOrEmpty(probe)) continue;
            var ops = EffectText.Parse(probe, out _, out _);
            if (ops == null || ops.Count == 0) continue;

            if (head == null)
            {
                // 🔴 **无头正文不能一律当「部署时该发生」** —— 卡面常常把触发头**印成图标**
                //    （`Agenda:` / `Duty:` / `Artifice:` …），而我们的 `desc` 里那个头**没了**。
                //    实测反例：`DA21 Inner Circle Companion` 的 `Gain Vanguard and 2 Quest Points`
                //    （`keywords=[stealth1, agenda1]`）· `BL40 Dark Apostle` 的
                //    `Give a random Dark Pact to each friendly troop`（`artifice1`）·
                //    `AM27 Bombast Field Gun`（`duty1`/`longrange1`）—— 三张都**不是**部署时该发生的。
                //    ⇒ 判据**转调引擎自己的答案**：这段正文落在 `TriggerTexts` 的哪个头下面
                //      （`CollectBareKeywordBody` 收进去的那一条，`key` 就是那个头）。
                string h = WillRun.HeadOfBody(c, s);
                if (h != null && h != "rally") continue;                       // 已认领在别的时机下
                if (CardDef.HandledByOtherLayer(c, raw) != null) continue;     // `When <事件>, …` 那一族
                unguarded = true;                                             // 没人认领 / 认领在 rally 下
                continue;
            }
            if (head == "rally") { unguarded = true; continue; } // `Rally:` = 部署时触发
            if (Array.IndexOf(CardDef.RoutableTriggers, head) >= 0) continue;   // 别的时机
            if (WillRun.IsPaidHead(head)) continue;             // `N ☀:` / `N [Spirit Stone]:` 付费能力 = 主动用
            if (CardDef.HandledByOtherLayer(c, raw) != null) continue;          // 别的层接手
            unguarded = true;   // 认不出的头 ⇒ ⛔ 不替它豁免（宁可列进可疑档，不静默放过）
        }
        return unguarded ? "C 部署时该发生" : "B 等事件/等主动用";
    }

    // ==================================================================
    //  `A1572`：本夹具【结构上不适用】的两族 —— 可执行的过滤口径
    // ==================================================================
    //
    // 🔴 **为什么要有这一层**：`other == 0` 这个判据问的是「打出去之后局面动没动」，
    //   而**有两族卡在本夹具里压根到不了「打出去」那一步**（不是卡的问题、也不是引擎的问题，
    //   是**夹具的形状**决定的）⇒ 把它们与自然上手那批**混在一起读**就会误报一整族。
    //
    // 两条口径（判据**全部转调引擎的公开面**，⛔ 探针里不另写一份表）：
    //   ① **临时卡（`ephemeral`）** —— 判据 = `CardDef.Has("ephemeral")`（与
    //      `BattleContext.IsEphemeral` 的前半同一处语义，见 `RuleCore.cs:1171`）。
    //      **为什么**：`RuleCore.EndTurn`（`RuleCore.cs:1341`）会调 `SweepEphemeral(ctx, ctx.Active)`
    //      —— 回合结束时把**该方手牌**里的临时卡**移出游戏**（规则书 `:229`「非弃置」）。
    //      本夹具是「**先摆牌库、过两个回合、再对准它打一发**」⇒ 对被测的那张卡来说，
    //      它在**回合 0 结束时就已经不在手牌里了**（实测：98 张全部落在这一档）。
    //      ⇒ 只有「夹具按引擎自己的形状**补手牌**」这条路能接住它。
    //   ② **防御卡（`type == "defence"`）** —— 判据 = `CardDef.Type == "defence"`。
    //      **为什么**：`RuleCore.BuildPlayer`（`RuleCore.cs:506/553`）把 `defence` 从**牌库**里
    //      **分流**出去（`defenceCardOut`），发牌由调用方按**座位**做 —— 只发给**后手**
    //      （`RuleCore.GrantDefenceCard` / `SetDefensiveCard`，见 `RuleCore.cs:448-473` 的说明）。
    //      本夹具的 P0 是**先手** ⇒ 手里永远不会有它（实测：39 张全部落在这一档）。
    //      ⚠️ **补进来之后还有一层不等价**：它被测量时站在**先手**这一侧，
    //         而原版里它只会出现在**后手**手里 ⇒ 这一批的读数「**座位与原版不同**」。
    //
    // 🔴 **这两条口径的用法 = 【标注 ＋ 单列】，⛔ 不是【整族从候选里删掉】** ——
    //    实测（2026-10-11，池 1126 张 · 411 张候选）落在候选里的只有 **3 张临时卡 ＋ 4 张防御卡**，
    //    而逐条看引擎日志，那 7 张**各有别的原因**（3 张是**真缺陷**：`Deploy <二词具名>` 被当兵种筛
    //    ⇒ 一张都筛不到；4 张是**量具盲区**：牌库顶换序 / 牌库减费不在指纹里 / 要「残骸」而夹具没有）。
    //    ⇒ **整族删除会把那 3 张真缺陷一起盖掉**（本仓红线「不许静默失败」＋ 铁律 11「全部都要做」）。
    static string NotApplicable(CardDef c)
    {
        var why = new List<string>();
        if (c.Has("ephemeral")) why.Add("临时卡（`ephemeral`）");
        if (c.Type == "defence") why.Add("防御卡（`defence`，原版只发给后手）");
        return why.Count == 0 ? "" : string.Join(" ＋ ", why);
    }

    /// <summary>`fixtureFit` 列的值（TSV 用，纯 ASCII 好 grep）：空 = 适用 ·
    /// `ephemeral` / `defence` / `ephemeral+defence` = 本夹具对它结构上不适用（见 <see cref="NotApplicable"/>）。</summary>
    static string FitTag(CardDef c)
    {
        var t = new List<string>();
        if (c.Has("ephemeral")) t.Add("ephemeral");
        if (c.Type == "defence") t.Add("defence");
        return string.Join("+", t);
    }

    /// <summary>从引擎自己的事件日志里挑出**最能解释「为什么没变化」**的那一行
    /// （优先「**没生效**」/「空过」/「没有合法目标」/「打不了」—— 引擎自己会说话，⛔ 不用我们猜）。</summary>
    static string WhyLine(Reading r)
    {
        string[] keys = { "没生效", "空过", "没有合法目标", "打不了", "造不出" };
        foreach (var k in keys)
            foreach (var l in r.LogTail)
                if (l.Contains(k)) return l;
        return r.LogTail.Count > 0 ? r.LogTail[r.LogTail.Count - 1] : "（引擎日志里没有说明）";
    }

    // ==================================================================
    //  打印
    // ==================================================================

    static void Print(Reading r)
    {
        var c = r.C;
        Console.WriteLine("=== play " + c.Id + " · " + c.Name + " [" + c.Type + "] ===");
        Console.WriteLine("  卡面（英文）: " + c.Desc);
        Console.WriteLine("  卡面（中文）: " + (string.IsNullOrEmpty(c.DescZh) ? "（这张卡没有中文）" : c.DescZh));
        Console.WriteLine("  印的数值    : cost=" + c.Cost + " attack=" + c.Attack + " ranged=" + c.RangedAttack
                          + " health=" + c.Health + " subtype=" + (c.Subtype ?? "-")
                          + " faction=" + (c.Faction ?? "-") + " rarity=" + (c.Rarity ?? "-"));
        Console.WriteLine("  关键词      : " + (c.Keywords == null || c.Keywords.Count == 0 ? "（无）"
                          : string.Join(", ", c.Keywords.Select(k => k.Key + (k.Value != 0 ? k.Value.ToString() : "")))));
        Console.WriteLine("  怎么打的    : " + r.How);
        Console.WriteLine("  档          : " + r.Bucket + "（见 `play all` 的分档说明；"
                          + "决定 `other=0` 时**该不该当缺陷看**）");
        // 🆕 `A1572`：这一张在本夹具里**结构上适不适用** —— 两族（临时卡 / 防御卡）天然到不了本夹具那一步
        string na = NotApplicable(c);
        Console.WriteLine("  夹具适用性  : " + (na.Length == 0 ? "适用"
                          : "**结构上不适用** —— " + na + "（见 `play all` 的 ③ 节；"
                            + "`other=0` 不能单独当缺陷）"));
        Console.WriteLine();
        if (r.Crash != null)
        {
            Console.WriteLine("  🔴 **真打时抛异常**：" + r.Crash);
            Console.WriteLine("     （这是引擎侧缺陷 —— ⛔ 本工具只报，不改；判决归调度台/引擎侧）");
            return;
        }
        Console.WriteLine("  ── 打出去之前 ──");
        Console.WriteLine(r.Before.Length > 0 ? "     " + r.Before : "     （督军档：没有「打出去」这一步，见上）");
        Console.WriteLine("  ── 打出去之后 ──");
        Console.WriteLine("     " + r.After);
        Console.WriteLine();
        Console.WriteLine("  ── 变化逐条（" + r.Changes.Count + " 条"
                          + (r.HeroMode ? "（= 上面那一趟 **减掉** 下面的空督军对照）"
                                        : "：付费 " + r.Paid + " · 出牌本身 " + r.Mech
                                          + " · 自身落场 " + r.SelfPlace + " · 其它 " + r.Other)
                          + "）──");
        if (r.Changes.Count == 0) Console.WriteLine("     （局面一个字段都没动）");
        foreach (var ch in r.Changes) Console.WriteLine("     " + ch);
        if (r.HeroMode)
        {
            Console.WriteLine("  ── 【空督军】对照局的变化（" + r.ControlChanges.Count + " 条，"
                              + "⛔ 与上面那一栏相减才是这位督军的贡献）──");
            foreach (var ch in r.ControlChanges) Console.WriteLine("     " + ch);
        }
        Console.WriteLine();
        Console.WriteLine("  ── 引擎自己的事件日志（末尾 " + r.LogTail.Count + " 条）—— 🔴「什么都没发生」时先看这里 ──");
        if (r.LogTail.Count == 0) Console.WriteLine("     （一条都没有）");
        foreach (var l in r.LogTail) Console.WriteLine("     " + Clip(l, 150));
        Console.WriteLine();
        Console.WriteLine("  ── 判据（⛔ 判据在**卡面**上，本工具只摆读数、不判对错）──");
        Console.WriteLine("    卡面有没有可执行正文（`desc` 或注册层）: " + (r.FaceHasOps ? "**有**" : "没有"));
        Console.WriteLine("    打出去了吗: " + (r.Played ? "是（rc=0 OK）" : "**否** —— " + r.Note)
                          + (r.Played && r.ExpectedPay >= 0 ? "   （照卡面应付 **" + r.ExpectedPay + "** 能）" : ""));
        // 🆕 `A1572`：这一张是**补进手牌**才测到的 ⇒ 读数带「量具干预」标记（判读时要看得见）
        if (r.Injected)
            Console.WriteLine("    ⚠️ 这一张是**夹具补进手牌**的（不是自然上手）—— 见 `play all` 的 ③ 节："
                              + "临时卡会被回合末的 `SweepEphemeral` 移出游戏、防御卡本来就只发给后手。");
        Console.WriteLine("    实得变化（**除付费 · 出牌本身 · 自身落场**）: " + r.Other + " 条"
                          + (r.Played && r.FaceHasOps && r.Other == 0
                             ? "   ← 🔴 **卡面写着正文、打出去了、局面一个数字都没动** = `A1431` 那一类【结算层失明】的第一候选"
                             : ""));
        Console.WriteLine("    对敌造成的伤害累计: " + r.Dmg
                          + (r.Dmg != 0 ? "（照卡面数）" : ""));
        Console.WriteLine("  ⚠️ 「实得变化 0 条」**不等于缺陷**（可能夹具里没有合法目标 / 效果只在别的时机发生）"
                          + " —— 判决要看卡面，本工具不代替判决。");
    }

    // ==================================================================
    //  全池 sweep
    // ==================================================================

    /// <summary>全池逐卡真打，出一份 TSV + 一段摘要。
    /// ⚠️ 这是 `A1431` 那句「**全池尺子对『解析过、结算错』这一类结构性失明**」的正面回答：
    ///   本仓第一次有一条全池尺子**能看见「打出去之后局面动了没有」**。
    /// ⛔ 输出的是**候选**，判决归调度台/引擎侧（与 `willrun` 同一条纪律）。</summary>
    static int Sweep(List<CardDef> pool, string dumpPath, string pfPath = null)
    {
        var sb = new StringBuilder();
        sb.Append("id\tname\ttype\tbucket\twarlord\tplayRc\trcName\tplayed\tfaceOps\tselfPlace\tmech\tother\t"
                  + "expectedPay\tenergyDelta\thandCostDelta\tdmg\tchanges\tcrash\tnote\t"
                  // 🆕 `A1572` 两列（**只加在末尾** —— 前面的列一个字没动）
                  + "fixtureFit\tinjected\n");
        var pfRows = new List<string>();      // 🆕 `A1431` 第二笔：**结算指纹**（逐卡一行，见 `FingerprintRow`）

        int played = 0, cand = 0, crash = 0, unimpl = 0, cantPlay = 0, heroCnt = 0;
        int naTotal = 0, naInjected = 0;                 // `A1572`：结构上不适用的两族 / 其中靠补手牌的
        var buckets = new Dictionary<string, int>();
        var cands = new List<Reading>();
        var crashes = new List<Reading>();
        var naCands = new List<Reading>();               // 两族里**落进候选**的那些（要单列，⛔ 不静默）

        foreach (var c in pool)
        {
            var r = Measure(pool, c);
            r.Bucket = Bucket(c);
            if (c.Type == "hero") heroCnt++;
            if (r.Crash != null) { crash++; crashes.Add(r); }
            else if (r.Played) played++;
            else
            {
                cantPlay++;
                var k = CodeName(r.Rc);
                buckets[k] = (buckets.TryGetValue(k, out var n) ? n : 0) + 1;
                if (r.Rc == RuleCodes.ErrUnimplemented) unimpl++;
            }
            if (r.Played && r.FaceHasOps && r.Other == 0 && c.Type != "hero")
            {
                cand++; cands.Add(r);
                // `A1572`：落在候选里的那两族**单列**（见 `NotApplicable` 的那段长注释）
                if (FitTag(c).Length > 0) naCands.Add(r);
            }
            if (FitTag(c).Length > 0)
            {
                naTotal++;
                if (r.Injected) naInjected++;
            }

            pfRows.Add(FingerprintRow(c, r));      // 🆕 `A1431` 第二笔

            sb.Append(c.Id).Append('\t').Append(c.Name).Append('\t').Append(c.Type)
              .Append('\t').Append(r.Bucket)
              .Append('\t').Append(r.HeroMode ? "1" : "0")
              .Append('\t').Append(r.Rc)
              .Append('\t').Append(r.HeroMode ? "hero" : CodeName(r.Rc))
              .Append('\t').Append(r.Played ? "1" : "0")
              .Append('\t').Append(r.FaceHasOps ? "1" : "0")
              .Append('\t').Append(r.SelfPlace)
              .Append('\t').Append(r.Mech)
              .Append('\t').Append(r.Other)
              .Append('\t').Append(r.ExpectedPay)
              .Append('\t').Append(r.EnergyDelta)
              .Append('\t').Append(r.HandCostDelta)
              .Append('\t').Append(r.Dmg)
              .Append('\t').Append(r.Changes.Count)
              .Append('\t').Append(r.Crash == null ? "" : r.Crash.Replace('\t', ' ').Replace('\n', ' '))
              .Append('\t').Append(r.Note.Replace('\t', ' ').Replace('\n', ' '))
              // 🆕 `A1572`：本夹具对它**结构上不适用**吗（空 = 适用；`ephemeral` / `defence` / 两者）
              //    + 这一张是不是**补进手牌**才测到的（`1` = 量具干预过）。⛔ 只加列、不改前面的列。
              .Append('\t').Append(FitTag(c))
              .Append('\t').Append(r.Injected ? "1" : "0")
              .Append('\n');
        }

        Console.WriteLine("=== play all · 逐卡真打（不跑 Unity）· 池 " + pool.Count + " 张 ===");
        Console.WriteLine("  夹具（全池同一份，所以读数能横向比）：");
        Console.WriteLine("    · 双方督军各 1（P0 的督军 = 夹具 `FIXWarlord`，**除非被测卡本身是 hero**）");
        Console.WriteLine("    · 双方各 3 张固定部队：我方 `FIX_A` 步 2/4 · `FIX_B` 载具 3/5 · `FIX_C` 兽 1/4，"
                          + "敌方 `FIX_E1/E2/E3` 步/载具/兽 各 6 血");
        Console.WriteLine("      落在 **3 / 6 / 7** 三格（贴着督军格 4、且有相邻对 6-7）；"
                          + "兵种**故意分散**（步/载具/兽）⇒「friendly Vehicle」「your Beasts」那族找得到目标");
        Console.WriteLine("    · 手牌 " + FixtureHand + " 张 = 3 张摆场部队 + **被测那一张**；"
                          + "能 " + FixtureEnergy + "/" + FixtureMaxEnergy
                          + "（上限比起始高 ⇒ `Refill all your Energy` 看得见）；`seed=0`、不洗牌、换牌关");
        Console.WriteLine("    · 摆完场后**我方三张各扣 " + FixtureChip + " 血**（走引擎 `ApplyDamage`）"
                          + " ⇒ `Heal …` 那一族才看得见（满血治疗 = 什么都没发生）");
        Console.WriteLine("  量的是什么：把被测卡**真打一发**，比打前/打后的**局面指纹**"
                          + "（能 · 棋盘每格 攻/远/甲/血+关键词 · 手牌/牌库/弃牌堆**逐张 id** · 手牌总价 · 伤害累计 · 出兵数）。");
        Console.WriteLine("  ⛔ 本工具**不判对错** —— 判据在**卡面**上（`desc`/`descZh`/PnP 卡图），"
                          + "这里摆的是**实得数字**。");
        Console.WriteLine();
        Console.WriteLine("  --- ① 总账 ---");
        Console.WriteLine("    真打成功（rc=0）        " + played + " 张");
        Console.WriteLine("    打不出去                " + cantPlay + " 张"
                          + (cantPlay > 0 ? "（分因：" + string.Join(" · ",
                              buckets.OrderByDescending(kv => kv.Value).Select(kv => kv.Key + " " + kv.Value)) + "）" : ""));
        Console.WriteLine("      ⚠️ 其中 `ErrUnimplemented` **" + unimpl + " 张** = 卡面上写着正文、"
                          + "而 `CanPlayTactic` 就把它挡回去了（`A1431` 的硬失败档）");
        Console.WriteLine("    引擎抛异常              " + crash + " 张" + (crash > 0 ? "  ← 🔴 **必须查**" : ""));
        Console.WriteLine("    督军档（另法测）        " + heroCnt + " 张");
        Console.WriteLine();
        Console.WriteLine("  --- ② 候选（卡面写着正文 · 打得出去 · 局面一个数字都没动 = " + cand + " 张），**按可疑度分档** ---");
        Console.WriteLine("     🔴 **档 A（最可疑，先看它）** —— `tactic`/`defence`：打出**就是**触发，"
                          + "没有「等事件」可以解释");
        Console.WriteLine("     ⚠️ 档 C —— `unit`：卡面里有**不带触发头**的正文（或 `Rally:`）⇒ 部署那一拍就该有变化");
        Console.WriteLine("     ⚪ 档 B —— `unit`：每一句都挂在**别的时机**的头上（`Strike`/`Regiment`/`Duty`/`When …` …）"
                          + "⇒ 本夹具**产生不了那个事件**，`other=0` 是**预期的**，⛔ 不是缺陷");
        Console.WriteLine();
        foreach (var bucket in new[] { "A 打出即结算", "C 部署时该发生", "B 等事件/等主动用" })
        {
            var g = cands.Where(r => r.Bucket == bucket).ToList();
            if (g.Count == 0) continue;
            Console.WriteLine("     ── " + bucket + " · " + g.Count + " 张"
                              + (bucket.StartsWith("A") ? "  ← 🔴 **这一档最可疑**" : "") + " ──");
            foreach (var r in g.Take(bucket.StartsWith("A") ? 120 : 60))
                Console.WriteLine("       " + Pad(r.C.Id, 24) + Pad(r.C.Name, 30) + "[" + r.C.Type + "]  rc="
                                  + CodeName(r.Rc) + "  `" + Clip(r.C.Desc, 66) + "`");
            if (g.Count > (bucket.StartsWith("A") ? 120 : 60))
                Console.WriteLine("       …（还有 " + (g.Count - (bucket.StartsWith("A") ? 120 : 60)) + " 张，看 TSV）");
        }
        Console.WriteLine("     ⛔ **候选 ≠ 缺陷清单** —— 里面还有一类**量具本身的盲区**（本夹具测不到）："
                          + "① 需要专属事件的（档 B 已分开）；② 只改**后续回合**的（`For the rest of this battle, at the start of your turn …`）；"
                          + "③ 需要**特定兵种/关键词**才成立的（本夹具只有 步/载具/兽 三兵种、`Infantry` 无关键词）；"
                          + "④ 需要**上一句承接**的（`A1428` 跨句承接那族）。⇒ **逐张对着卡面看**，判决归调度台/引擎侧。");

        // ------------------------------------------------------------------
        //  `A1572`：本夹具**结构上不适用**的两族 —— 可执行的过滤口径 ＋ 它们里落进候选的逐条
        // ------------------------------------------------------------------
        Console.WriteLine();
        Console.WriteLine("  --- ③ 本夹具【结构上不适用】的两族（`A1572`：**量具口径**，⛔ 不是引擎缺陷）---");
        Console.WriteLine("     口径 ① 临时卡（`ephemeral`，判据 `CardDef.Has(\"ephemeral\")`）："
                          + "`RuleCore.EndTurn` 的 `SweepEphemeral`（`RuleCore.cs:1341`）会在回合末把"
                          + "**该方手牌**里的临时卡**移出游戏**（规则书 `:229`「非弃置」）"
                          + "⇒ 任何「先摆牌库、等它自然上手」的夹具对它天然无效。");
        Console.WriteLine("     口径 ② 防御卡（`type == \"defence\"`，判据 `CardDef.Type`）："
                          + "`RuleCore.BuildPlayer`（`RuleCore.cs:506/553`）把 `defence` 从**牌库**分流出去、"
                          + "只发给**后手** ⇒ 本夹具的 P0（先手）手里永远不会有它。");
        Console.WriteLine("     ⇒ 读法：这两族的行，`other == 0` **不能单独当缺陷**；"
                          + "本夹具靠【按引擎形状补手牌】接住它们（`injected=1` 那一列就是这么来的）。");
        Console.WriteLine("     🔴 **⛔ 但这两族【不整族删除】**（`A1572` 的账文原写「必须从候选里排除」——"
                          + "实测那句**与本夹具的实际形状不符**）：落在候选里的只有下面这 "
                          + naCands.Count + " 张，而它们**各有别的原因**（多为量具盲区，也有**真缺陷**）"
                          + "⇒ 整族删除会连真缺陷一起盖掉（本仓红线「不许静默失败」＋ 铁律 11）。");
        Console.WriteLine("     对照口径（实测）：两族共 " + naTotal + " 张 · 其中 **" + naInjected
                          + " 张**是靠补手牌才测到的（余下 " + (naTotal - naInjected)
                          + " 张是自然上手/落场，不是这一层的问题）");
        Console.WriteLine("     口径自证：两族的行与 `injected=1` 的行**互为同一集合**（今天实测 "
                          + naTotal + " = " + naInjected + "）—— 哪天不再相等，说明引擎的补牌/分流"
                          + "改了形状（该回来重读这一节，⛔ 不是自动豁免）。");
        if (naCands.Count == 0)
            Console.WriteLine("     ✅ 两族**没有一张**落进候选 —— 本夹具对它们已经够用（⛔ 不是「免检」："
                              + "它们仍然只是**结构上不适用**的那一层提示）。");
        else
        {
            Console.WriteLine("     ── 两族里**落进候选**的 " + naCands.Count
                              + " 张（逐条列出 · 附引擎自己那句最能解释「为什么没变化」的话）──");
            foreach (var r in naCands)
                Console.WriteLine("       " + Pad(r.C.Id, 24) + Pad(r.C.Name, 30) + "[" + FitTag(r.C) + "]  "
                                  + "`" + Clip(r.C.Desc, 54) + "`\n"
                                  + "          ⇒ " + Clip(WhyLine(r), 150));
        }
        Console.WriteLine("     ⚠️ 这一节是**提示**、不是判决：真要销掉某一张，判据仍在**卡面**上"
                          + "（`desc`/`descZh`/PnP 成品卡图），⛔ 别拿本节当「已解释、可跳过」。");

        if (crash > 0)
        {
            Console.WriteLine();
            Console.WriteLine("  --- ④ 🔴 引擎抛异常（" + crash + " 张）---");
            foreach (var r in crashes.Take(60))
                Console.WriteLine("       " + r.C.Id + "  " + r.C.Name + "  [" + r.C.Type + "]  " + r.Crash);
        }

        if (!string.IsNullOrEmpty(dumpPath))
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(dumpPath));
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(dumpPath, sb.ToString());
            Console.WriteLine();
            Console.WriteLine("  wrote " + dumpPath + " lines=" + (pool.Count + 1));
        }

        // ------------------------------------------------------------------
        //  🆕 `A1431` 第二笔：**结算指纹** —— 全池读数的**规范化摘要**（`play check` 对拍的那一份）
        // ------------------------------------------------------------------
        Console.WriteLine();
        Console.WriteLine("  --- ⑤ 结算指纹（`A1431` 第二笔 · `play check` 拿它对拍）---");
        Console.WriteLine("     " + FingerprintMd5(pfRows) + "   （" + pfRows.Count + " 张）");
        Console.WriteLine("     含什么：`id|type|warlord|playRc|played|faceOps|自落场|机械|实得|应付|能量Δ|手牌价Δ|伤害|"
                          + "变化条数|crash|档|**变化逐条全文**`。");
        Console.WriteLine("     ⛔ 不含什么：**卡名**（改名的产物，本仓已改过 6 张）· `note`（口径散文）·"
                          + "引擎日志（措辞会变）—— 这三样进来会**天天红**，把「红了不用管」训练成习惯。");
        if (!string.IsNullOrEmpty(pfPath))
        {
            var pdir = Path.GetDirectoryName(Path.GetFullPath(pfPath));
            if (!string.IsNullOrEmpty(pdir)) Directory.CreateDirectory(pdir);
            File.WriteAllText(pfPath, FingerprintFileText(pfRows));
            Console.WriteLine("     wrote " + pfPath + " lines=" + (pfRows.Count + 1));
        }
        else
        {
            Console.WriteLine("     ⚠️ 没写指纹文件 —— 要留一份「被确认的状态」⇒ `play all pf <out.txt>`"
                              + "（写完之后**手改** `PlayProbe.PinnedPlayMd5` = 上面那串，见 `play check`）。");
        }

        Console.WriteLine();
        Console.WriteLine("  判据：**抛异常 = 0 张** ⇒ rc=0；否则 rc=1。");
        Console.WriteLine("  ⚠️「候选」与「打不出去」**都不计入 rc** —— 它们要**对着卡面**逐张判，"
                          + "本工具不代替判决（⛔ 也不许静默：两栏都在上面逐条打了出来）。");
        return crash > 0 ? 1 : 0;
    }

    // ==================================================================
    //  🆕 `A1431` 第二笔：结算层对拍（`play check`）
    // ==================================================================
    //
    // 🔴 **修的是哪一条**：`A1431` 的账文 = 「全池尺子对『**解析过、结算错**』结构性失明」。
    //   第一笔（`E8`，2026-10-11）把 `play` 立了起来 —— 它**能看见**「打出去之后局面动了没有」，
    //   但它**只摆读数、永不红**（`rc` 只认「崩了 / `ErrUnimplemented`」）。
    //   ⇒ 到今天仍有两条**结构性**的洞：
    //     ① **读数不可比**：「这张卡昨天实得 3 条变化、今天实得 0 条」**没有任何东西记得住** ——
    //        结算层的**静默漂移**（`A1388`/`A1407`/`A1426`/`A1428` 那一族**修完又坏回去**）
    //        谁都不会知道；411 张候选逐张对着卡面看**是人的活**，机器这一侧**没有留痕**。
    //     ② **同一个洞的第二层**：`check` 那边「**真回归 ＋ 重生成基线 ⇒ 静默变绿**」，
    //        在**结算层**一模一样地成立（`play` 若也拿一份「基线」对拍，那份基线同样**一条命令就能重写**）。
    //
    // ⇒ 做法（`A1382` 那个形状，一层一层搬下来）：
    //   · **逐卡结算指纹**（<see cref="FingerprintRow"/>）：把 `play all` 的读数压成一行**只含数值**的摘要
    //     （含**变化逐条全文** —— 那是「数字动成什么」），全池一份 ⇒ md5。
    //   · `play check [<pf.txt>]`：① 与**基线文件**逐卡比（出「**哪张卡、哪一格**变了」）；
    //     ② **哨兵**：基线文件的内容 md5 必须等于**源码里的 `PinnedPlayMd5`**
    //     ⇒ `play all pf` 重生成基线**抹不掉它**（重钉只能**手改源码** = 显式确认）。
    //
    // ⚠️ 与 `check` 的**已知差别（如实标着）**：结算读数**依赖卡池内容**（改一张卡的 `desc`
    //   就会改它的读数）⇒ 卡池一改，本条**就会红**（⛔ **不像**哨兵 ⑤ 那样「此时不判」——
    //   那正是 `A1382` 假阴的来源）。**这是有意的**：红了先跑 `play check` 看逐卡差异（全池 ≈5 秒）。
    // ⛔ **它仍不判对错**（判据在**卡面**上）—— 它判的是「**结算结果有没有离开被确认的那一版**」。
    // ⚠️ 它**不进任何 Unity 自检**（不跑 Unity、≈5 秒），也**不该进**（判据在卡面上，机器判不了对错）。
    // ==================================================================

    /// <summary>🆕 钉住的**结算指纹**（= 基线文件里那些**非 `#` 行**的规范化 md5，见 <see cref="FingerprintMd5"/>）。
    ///
    /// **为什么必须有它**（`A1431` 第二笔的②）：`play check` 的判据之一是「与**基线文件**比」，
    /// 而基线文件**同一个工具就能重写**（`play all pf <out>`）⇒ 光靠它 = **自证**。
    /// ⇒ 参照物取**源码里的常量** ⇒ 重生成基线**抹不掉**（与 `Program.PinnedParseMd5` 同一个形状）。
    ///
    /// 🔴 **重钉（⛔ 没有「一条命令重钉」）**：`play check` 会把该抄的**整行**打出来，**手改这一个常量**。
    ///   **重钉前必须先确认这批变化是对的**（对着卡面 / 反编译 / 真机逐张看过）——
    ///   为了「把红抹平」而重钉 = **正是它在抓的那件事**。
    ///
    /// ⚠️ **它现在钉的值（原样交代）**：`7175ec95…` = **2026-10-12（第二十一会话 · 写手 `E5`）当次现读**
    ///   （池 1126 张 · 与同时写下的基线文件 `基线/out_play_baseline.txt` **逐行同源**：
    ///   拿那份文件的非 `#` 行重算 md5 = 同一个值，已复核）。🔴 **本会话另有 3 个写手在飞**
    ///   （改 `RuleEngine/Core/*.cs`、`数据/**`）⇒ 他们的改动一旦落地，**本条会红**，**那是设计**
    ///   （= 「结算读数离开了被确认的那一版，请显式确认」）。⚠️ 它钉的是**当时那一版**，
    ///   ⛔ **不是**「当时就对」—— 那时 411 张候选**还没逐张对过卡面**。
    ///   ⛔ **不许**为了让它变绿而重跑 `play all pf` —— 那只把洞开回来。
    /// </summary>
    public const string PinnedPlayMd5 = "7175ec95237e2f3540381f2b437460b0";

    /// <summary>`play check` 的缺省基线 —— **按名字往上找**（形状照 `Program.CheckMain.DefaultBaseline`，
    /// ⛔ 别拿「程序集目录往上 N 级」写死：换 build 配置 / 换 TFM 就漂）。</summary>
    static string DefaultPlayBaseline()
    {
        var d = new DirectoryInfo(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
        DirectoryInfo dirHit = null;
        while (d != null)
        {
            var f = Path.Combine(d.FullName, "基线", "out_play_baseline.txt");
            if (File.Exists(f)) return f;
            if (dirHit == null && Directory.Exists(Path.Combine(d.FullName, "基线"))) dirHit = d;
            d = d.Parent;
        }
        return dirHit != null
            ? Path.Combine(dirHit.FullName, "基线", "out_play_baseline.txt")
            : "基线/out_play_baseline.txt";
    }

    /// <summary>指纹行的**列名**（与 <see cref="FingerprintRow"/> **同序**，逐字对应 ⇒ 差异能指名到列）。</summary>
    static readonly string[] PfCols = new[]
    {
        "id", "type", "warlord", "playRc", "played", "faceOps", "selfPlace", "mech", "other",
        "expectedPay", "energyDelta", "handCostDelta", "dmg", "changes", "crash", "bucket",
        "fixtureFit", "injected", "changesFull",
    };

    /// <summary>一张卡的**结算指纹行**（`A1431` 第二笔）。列见 <see cref="PfCols"/>。
    /// 🔴 **⛔ 不含**：卡名（改名的产物）· `note`（口径散文）· 引擎日志（措辞会变）——
    ///   那三样进来会**天天红**，把「红了不用管」训练成习惯（`Sentinel` 头注那条教训）。
    /// 🔴 **含**最后一列 `changesFull` = **变化逐条全文**（「数字动成什么」）——
    ///   少了它，「条数一样而内容变了」就照不到（同族坑：只比张数会被抵消，见 <see cref="Snap"/>）。</summary>
    static string FingerprintRow(CardDef c, Reading r)
    {
        return string.Join("\t", new[]
        {
            c.Id, c.Type, r.HeroMode ? "1" : "0", r.Rc.ToString(),
            r.Played ? "1" : "0", r.FaceHasOps ? "1" : "0",
            r.SelfPlace.ToString(), r.Mech.ToString(), r.Other.ToString(), r.ExpectedPay.ToString(),
            r.EnergyDelta.ToString(), r.HandCostDelta.ToString(), r.Dmg.ToString(),
            r.Changes.Count.ToString(), One(r.Crash), r.Bucket,
            One(FitTag(c)), r.Injected ? "1" : "0",
            One(string.Join(";", r.Changes)),
        });
    }

    /// <summary>`TAB`/换行 一律换成空格 —— 指纹文件是**逐行**读的，一格里混进换行会把行读断（静默错位）。</summary>
    static string One(string s) { return s == null ? "" : s.Replace('\t', ' ').Replace('\n', ' ').Replace('\r', ' '); }

    /// <summary>跑一遍全池，出**指纹行**（按 `id` 排序 —— ⛔ 别靠卡池顺序：`Dictionary`/池序都不是判据）。</summary>
    static List<string> FingerprintRows(List<CardDef> pool)
    {
        var rows = new List<string>();
        foreach (var c in pool)
        {
            var r = Measure(pool, c);
            r.Bucket = Bucket(c);
            rows.Add(FingerprintRow(c, r));
        }
        rows.Sort(StringComparer.Ordinal);
        return rows;
    }

    /// <summary>一组指纹行的 md5（规范化：**丢掉空行、按整行字节序排序、`\n` 连接**，UTF-8）。
    /// ⚠️ 排序在**函数内部**做 ⇒ 调用方给什么顺序都一样（⛔ 别指望调用方排好）。</summary>
    static string FingerprintMd5(IEnumerable<string> rows)
    {
        var sorted = rows.Where(x => !string.IsNullOrEmpty(x)).OrderBy(x => x, StringComparer.Ordinal).ToList();
        var sb = new StringBuilder();
        foreach (var r in sorted) sb.Append(r).Append('\n');
        return Md5Utf8(sb.ToString());
    }

    /// <summary>写盘用的文本（头三行是 `#` 注释 —— 读的时候**整行丢掉**，⛔ 不参与 md5）。</summary>
    static string FingerprintFileText(IEnumerable<string> rows)
    {
        var sb = new StringBuilder();
        sb.Append("# ruleprobe `play` 结算指纹（`A1431` 第二笔）—— 一行一张卡；`play all pf <out>` 生成，`play check` 拿它逐卡对拍。\n");
        sb.Append("# 列（TAB 分隔）：" + string.Join("/", PfCols) + "\n");
        sb.Append("# 🔴 本文件里非 `#` 行的 md5 必须等于源码常量 `PlayProbe.PinnedPlayMd5` —— 重生成它抹不掉那条哨兵。\n");
        sb.Append("# ⛔ 本文件是【被人确认过的状态】的快照；为了把红抹平而重跑 `play all pf` = 把洞开回来。\n");
        foreach (var r in rows.Where(x => !string.IsNullOrEmpty(x)).OrderBy(x => x, StringComparer.Ordinal))
            sb.Append(r).Append('\n');
        return sb.ToString();
    }

    static List<string> LoadFingerprintRows(string path)
    {
        var rows = new List<string>();
        foreach (var l in File.ReadAllLines(path))
            if (l.Length != 0 && !l.StartsWith("#", StringComparison.Ordinal)) rows.Add(l);
        return rows;
    }

    static string Md5Utf8(string s)
    {
        using (var md5 = System.Security.Cryptography.MD5.Create())
        {
            var h = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(s));
            var sb = new StringBuilder(h.Length * 2);
            foreach (var x in h) sb.Append(x.ToString("x2"));
            return sb.ToString();
        }
    }

    /// <summary>`play check [&lt;pf.txt&gt;]` —— **结算层对拍**（`A1431` 第二笔）。
    /// 两件事，**两件都算进 rc**（⛔ 不许「文件变了但界面全绿」）：
    ///   ① **逐卡比**：当次现算的指纹 vs 基线文件（出「哪张卡、哪一列、从什么变成什么」）；
    ///   ② **哨兵**：基线文件内容的 md5 vs `PinnedPlayMd5`（源码常量 ⇒ 重生成基线抹不掉）。
    /// 退出码：`0` = 两条都过 · `1` = 有差异或哨兵红 · `2` = 基线缺失/读不到。
    /// ⛔ **它不判对错** —— 它判的是「结算结果有没有离开被确认的那一版」；
    ///   「离开」可能是**真回归**、也可能是**有意改动**，判决看卡面 / 反编译 / 真机。</summary>
    static int Check(List<CardDef> pool, string baselinePath)
    {
        bool wasDefault = string.IsNullOrEmpty(baselinePath);
        if (wasDefault) baselinePath = DefaultPlayBaseline();
        baselinePath = Path.GetFullPath(baselinePath);
        if (!File.Exists(baselinePath))
        {
            Console.Error.WriteLine("!! 没有结算指纹基线：" + baselinePath
                                    + (wasDefault ? "（缺省值 = 程序集目录往上找到的第一条 `基线/out_play_baseline.txt`）" : ""));
            Console.Error.WriteLine("   生成办法：`play all pf <out.txt>`，逐卡对着卡面确认之后，"
                                    + "把 `PlayProbe.PinnedPlayMd5` **手改**成打出来的那串。");
            return 2;
        }

        var live = FingerprintRows(pool);
        var baseRows = LoadFingerprintRows(baselinePath);
        string liveMd5 = FingerprintMd5(live), baseMd5 = FingerprintMd5(baseRows);

        string IdOf(string row) { int p = row.IndexOf('\t'); return p < 0 ? row : row.Substring(0, p); }
        var baseMap = new Dictionary<string, string>();
        foreach (var r in baseRows) baseMap[IdOf(r)] = r;
        var changed = new List<string[]>();
        int added = 0;
        var addedIds = new List<string>();
        foreach (var r in live)
        {
            var id = IdOf(r);
            if (!baseMap.TryGetValue(id, out var b)) { added++; if (addedIds.Count < 20) addedIds.Add(id); continue; }
            if (!string.Equals(b, r, StringComparison.Ordinal)) changed.Add(new[] { id, b, r });
        }
        int removed = 0;
        var removedIds = new List<string>();
        var liveIds = new HashSet<string>(live.Select(IdOf), StringComparer.Ordinal);
        foreach (var kv in baseMap)
            if (!liveIds.Contains(kv.Key)) { removed++; if (removedIds.Count < 20) removedIds.Add(kv.Key); }

        Console.WriteLine("=== play check · 结算层对拍（不跑 Unity）· 池 " + pool.Count + " 张 ===");
        Console.WriteLine("  基线 " + Path.GetFileName(baselinePath) + "（" + baseRows.Count + " 行）·"
                          + " 当次现算 " + live.Count + " 行");
        Console.WriteLine("  结算指纹：当次现算 = **" + liveMd5 + "** · 基线文件 = " + baseMd5
                          + (liveMd5 == baseMd5 ? "（一致）" : "（**不同**）"));
        Console.WriteLine();
        Console.WriteLine("  --- ① 逐卡差异（基线 → 当次）---");
        if (changed.Count == 0 && added == 0 && removed == 0)
            Console.WriteLine("     ✅ 没有一张卡与基线不同（" + live.Count + " 张逐字节一致）");
        else
        {
            Console.WriteLine("     🔴 **" + changed.Count + " 张卡变了** · 新增 " + added + " · 消失 " + removed
                              + (added > 0 ? "（新增：" + string.Join(" · ", addedIds) + "）" : "")
                              + (removed > 0 ? "（消失：" + string.Join(" · ", removedIds) + "）" : ""));
            Console.WriteLine("     逐张（最多列 60 张；`changesFull` 截 160 字）：");
            foreach (var t in changed.Take(60))
            {
                var a = t[1].Split('\t'); var b = t[2].Split('\t');
                Console.WriteLine("       " + Pad(t[0], 24) + "（" + Math.Max(a.Length, b.Length) + " 列对拍）");
                int n = Math.Max(a.Length, b.Length);
                int shown = 0;
                for (int k = 0; k < n; k++)
                {
                    string av = k < a.Length ? a[k] : "";
                    string bv = k < b.Length ? b[k] : "";
                    if (string.Equals(av, bv, StringComparison.Ordinal)) continue;
                    if (PfCols.Length == n) Console.Write("         · " + Pad(PfCols[k], 14));
                    else Console.Write("         · 第 " + k + " 列            ");
                    Console.WriteLine(Clip(av, 160) + "  →  " + Clip(bv, 160));
                    if (++shown >= 8) { Console.WriteLine("         …（这一张还有差异列，⛔ 已截断：看 `play <" + t[0] + ">`）"); break; }
                }
            }
            if (changed.Count > 60) Console.WriteLine("       …（还有 " + (changed.Count - 60) + " 张）");
        }

        Console.WriteLine();
        Console.WriteLine("  --- ② 哨兵：基线文件的内容 vs 源码常量 ---");
        int sentFail = 0;
        if (baseMd5 != PinnedPlayMd5)
        {
            sentFail = 1;
            Console.WriteLine("     ✗ 🔴 **基线文件的内容 ≠ 被确认过的那一版**（`A1431` 第二笔的②）。");
            Console.WriteLine("        基线文件 md5=" + baseMd5 + " · 钉住=" + PinnedPlayMd5);
            Console.WriteLine("        ⚠️ 这一条判的是**基线文件本身**（它与当次现算是否一致见 ①）——"
                              + "「重生成基线把一次真回归抹平」正是它抓的那个动作。");
            Console.WriteLine("        ⇒ 判：`git diff` 那个基线文件 + 看 ① 的逐卡差异：");
            Console.WriteLine("           · **真回归**（某张卡该结算而没结算 / 数字变了）⇒ 去修引擎，⛔ 别改常量；");
            Console.WriteLine("           · **有意且已验过**（逐张对过卡面 / 反编译 / 真机）⇒ **手改**这一行：");
            Console.WriteLine("               public const string PinnedPlayMd5 = \"" + baseMd5 + "\";");
        }
        else
            Console.WriteLine("     ✓ 基线文件的内容与钉住的一致（" + baseMd5.Substring(0, 8) + "…）"
                              + " —— 重生成基线抹不掉这一条");

        Console.WriteLine();
        Console.WriteLine("  判据：① 逐卡全等 且 ② 哨兵过 ⇒ rc=0；否则 rc=1"
                          + "（**已知不适用**：这一步跑的仍是同一个夹具 ⇒ `档 B` 那 274 张"
                          + "「等事件」的卡在这里**照样看不见**，那是**夹具盲区**，⛔ 不是「没有差异」）。");
        return (changed.Count == 0 && added == 0 && removed == 0 && sentFail == 0) ? 0 : 1;
    }

    // ==================================================================
    //  小工具
    // ==================================================================

    /// <summary>返回码 → 名字（照 `RuleCodes` 逐条抄）。⛔ 别只打数字 —— 读数要能一眼读。</summary>
    static string CodeName(int rc)
    {
        switch (rc)
        {
            case 0: return "OK";
            case 1: return "ErrBadHand";
            case 2: return "ErrCost";
            case 3: return "ErrSlot";
            case 4: return "ErrNotTurn";
            case 5: return "ErrNotUnit";
            case 6: return "ErrExhausted";
            case 7: return "ErrNoAttack";
            case 8: return "ErrSelf";
            case 9: return "ErrStunned";
            case 10: return "ErrTarget";
            case 11: return "ErrPindown";
            case 13: return "ErrUnimplemented";
            case 14: return "ErrNoAbility";
            case 15: return "ErrDutyUsed";
            case 16: return "ErrNoAction";
            case 17: return "ErrNotWaystone";
            case 18: return "ErrNoTargetAvailable";
            case 19: return "ErrNotEnoughRoom";
            case -1: return "未打";
            default: return "rc" + rc;
        }
    }

    static string CodeWhy(int rc)
    {
        switch (rc)
        {
            case 2: return "能量不足";
            case 3: return "格位非法/越界";
            case 4: return "非本方回合";
            case 13: return "`IsFullyParsed(desc)` 为假且不是手牌陷阱 ⇒ 本版打不出去";
            case 18: return "**夹具里没有合法目标**（量具限制，多半不是卡的缺陷）";
            case 19: return "棋盘满了";
            default: return "见 `RuleCodes.cs`";
        }
    }

    static string Pad(string s, int w)
    {
        int vis = 0;
        foreach (char ch in s ?? "") vis += ch > 0x2000 ? 2 : 1;
        var b = new StringBuilder(s ?? "");
        while (vis < w) { b.Append(' '); vis++; }
        return b.ToString();
    }

    static string Clip(string s, int n)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Length <= n ? s : s.Substring(0, n) + "…";
    }
}
