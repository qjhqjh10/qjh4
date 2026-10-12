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
            Console.Error.WriteLine("!! 用法：play <id|卡名> | play all [dump <out.tsv>] [pf <out.txt>] | play check [<pf.txt>]"
                                    + "（`unit` 档的单卡输出**带事件电池**那一节，见 `A1431` 第三笔）");
            return 2;
        }
        if (string.Equals(key, "all", StringComparison.OrdinalIgnoreCase)) return Sweep(pool, dumpPath, pfPath);
        if (string.Equals(key, "check", StringComparison.OrdinalIgnoreCase)) return Check(pool, basePath);

        var hit = Find(pool, key);
        if (hit == null) return 2;
        var r = Measure(pool, hit);
        Print(r);
        // 🆕 `A1431` 第三笔：**单卡也要能看事件电池**（否则「这一张到底有没有被验到」得整池跑一遍才知道）。
        EventReading er = null;
        if (hit.Type == "unit") { er = MeasureEvents(pool, hit); PrintEvents(hit, er); }
        // 🔴 退出码只认**硬失败**（⛔ 不拿「候选」当失败 —— 那会把「红了不用管」训练成习惯）：
        //   · 崩了（引擎抛异常）= 缺陷，必须红；
        //   · `ErrUnimplemented` = 这张卡**根本打不出去**（`CanPlayTactic` 要求
        //     `IsFullyParsed`）⇒ 卡面上写着正文而它连打都打不出去 = 缺陷，必须红。
        //   🆕 **电池抛异常 / 对照局没起步**同样算硬失败（`A1431` 第三笔）—— 那两样意味着
        //     「这一张**没被验到**」，正是本笔要治的那件事，⛔ 不许静默放过。
        //   ⚠️ `ErrNoTargetAvailable`（`A985⑧` 拆出来的那一档）**不算** ——
        //     那是**夹具里没有合法目标**（本夹具只有 `Infantry` 一个兵种、没有隐秘/飞行/载具），
        //     是**量具的限制**，不是卡的缺陷。如实打在读数里，⛔ 不混进失败。
        //   ⚠️ **净差 0 条也不算**（电池抬不到那个事件 ≠ 卡有缺陷）—— 但它**会在上面出声**。
        if (r.Crash != null) return 1;
        if (r.Rc == RuleCodes.ErrUnimplemented) return 1;
        if (er != null && (er.Crash != null || er.Note.Length > 0)) return 1;
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
    //  🆕 `A1431` 第三笔：**事件电池** —— 治「档 B」那一档的【结构性失明】
    // ==================================================================
    //
    // 🔴 **它修的是哪一条**：`play all` / `play check` 的主档量的是「**把卡打出去那一拍**」
    //   局面的数字动没动。而 **档 B**（`Bucket` 的第 3 档）的定义就是
    //   「卡面正文**每一句都挂在别的时机的头上**」（`Strike:` / `Mob:` / `Regiment:` / `Slay:` /
    //   `Duty:` / `Pray:` / `Agenda:` / `Oath N:` / `Teleport:` / `Ambush:` / `Ecstasy N:` /
    //   `At the start of your turn …` …）⇒ **本夹具产生不了那个事件**，`other == 0` 是**预期**的。
    //   ⚠️ **于是那一档的指纹行是一个【常量】**（`other=0` · `changesFull` 空）：
    //   它的结算**今天坏掉、明天修回来**，主档的指纹**一个字符都不会变** ⇒
    //   `play check` 在那一档上是**看着绿、实际没验**（本仓红线「不许静默失败」）。
    //
    // **做法**：把卡打出去**之后**，再**替它走一段事件电池** —— ⛔ 不是我们手改状态，
    //   每一发都走**引擎自己的公开面**：
    //     ① 誓约（`UseOathAbility`）⇒ `Oath N:`。
    //        🔴 **必须排在回合电池之前、同一回合里用** —— `CanUseOathAbility` 有一条
    //        `!OathAllTurns && u.DeployedTurn != ctx.Turn ⇒ ErrExhausted` ⇒ 过了回合就再也用不了
    //        （实测：排在后面时电池自检的 `Oath` 那一对两条净差**都是 0**）；它**不查 `Exhausted`**。
    //     ② **两个来回**（`EndTurn` + `BeginTurn` ×2，P0→P1→P0）⇒ 驱动
    //        `At the start of your turn` / `At the end of your turn` 那一族；
    //        ⚠️ 顺带让 P0 的部队被**引擎自己的** `RefreshForNewTurn` 刷活
    //        （`Core/UnitState.cs:1190`，由 `RuleCore.cs:1160` 在 `BeginTurn` 里调）
    //        ⇒ 后面那两步才跑得动。⛔ 探针**不手改 `Exhausted` / `SummonSickness`**。
    //     ③ 抽一张（`Draw`）⇒ `When you draw a card, …`；
    //     ④ 主动用：`UseAbility`（自定的 `Ability:`）＋ `AvailableAlternative`+`UseAlternative`
    //        ⇒ `Duty:` / `Pray:` / `Ferocity:` / `Agenda:` / `Talent:` / `Artifice:`；
    //     ⑤ **攻击两发**（`DeclareAttack` ×2：一发引擎自己选的型 ＋ 一发**另一种**）⇒
    //        `Strike:` / `Mob:` / `Regiment:` / `Ferocity:` / `Cruelty:`。
    //        🔴 **为什么非两发**：`Mob` 只在**近战**后触发、`Regiment` 只在**远程**后触发
    //        （`RuleCore.cs:4312` / `:4334` 那两条 `if (!ranged…)` / `if (ranged…)`）⇒
    //        只打一发 ⇒ **挂另一个头的整族永远抬不起来**（实测：自检的 `Regiment` 那一对全红）。
    //        第二发走 `forced: true` = 引擎自己的「**不是玩家点出来的**那一档」
    //        （原版 `scriptedAttack(0x34)`，见 `CanAttackNow` 头注：它跳过回合/配额那一段）；
    //        ⛔ 探针**不改 `AttacksThisTurn`**。
    //     ⑥ 打死一个敌人（`ApplyDamage`）⇒ `Slay:` / `When … dies` / 死亡触发那一族。
    //   ⚠️ ④⑤ 互相**不抢**：一个单位至多只有一条主动能力（`AvailableAlternative` 的注释：
    //      全池没有任何一张同时带两个）⇒ 没有主动能力的卡在 ④ 一步都不消耗；
    //      有主动能力的卡在 ④ 用掉行动、⑤ 第一发会返回 `ErrExhausted`，第二发（`forced`）照打。
    //      **每一个返回码都照原样进 `steps` 列**（⛔ 不静默吞掉）。
    //   ⛔ **电池抬不到的**（今天**如实标着**，见 ⑥ 节逐条列出的那批）：**付费段**
    //      （`N ☀:` / `Ecstasy N:` / `Stimulation:` / `Teleport:` 那一族要**主动付费**才进得来）、
    //      `Ambush:`（要在**面朝下**打出那一拍触发）、被动光环（`Your other … troops have +N`，
    //      它改的是**别人**、且**不在打出去那一拍**）。那几族**仍未被电池验到** ——
    //      `play check` 的 ①-c 会**逐条把它们的 id 列出来**，⛔ 不许只印一句「已知不适用」。
    //
    // 🔴 **对照局（⛔ 不是可选项）**：电池抬起来的**事件本身**（两边抽牌、能量刷新、回合计数…）
    //   对**每一张卡**都会产生一串**与卡无关**的变化。不加对照，`net` 就是一锅噪声，
    //   「这一张有没有被验到」这个问题**没有可测量的答案**。
    //   ⇒ 对照 = **同一张卡、同 id、同印数、同兵种、同关键词标记，只把 `desc` 清空**的替身
    //     （见 <see cref="Blank"/>）：两局的**印数/关键词**逐位相同 ⇒ 战斗算术那部分噪声抵消，
    //     剩下的净差**只能来自卡面正文**。净差 = `A.Changes` 减 `B.Changes`（与 `MeasureHero`
    //     的「空督军对照」同一个形状）。
    //
    // ⚠️ **本档只覆盖 `type == "unit"`**（586 张）—— **档 B / 档 C 按定义就是 `unit`**
    //   （`Bucket`：`tactic`/`defence` 直接归 A 档；`hero` 归 H 档，另法测）。
    //   ⛔ 不是「省事」：`tactic`/`defence` 的正文**打出即结算**、主档已经看得到，
    //     而它们的「空正文替身」在 `PlayTactic` 那一道 `IsFullyParsed` 闸上**打不出去**
    //     ⇒ 对照局不成立（那会让净差把**整张卡**算成效果）。
    //
    // ⛔ **它【不】判对错**（判据仍在卡面上）· ⛔ **不改主档一个字** —— 主档的读数、
    //   主档的指纹行、`Sweep` 的候选分档**全部逐字不动**，本档只**在末尾追加**一批行。
    //   这是有意的：主档那 407 张候选**已经逐张对过卡面**，重算它们等于让那批判决作废。

    /// <summary>🔴 **`A1431` 第三笔：电池抬不起来的那些，按【根因】分一格**（⛔ 不是「总之没验到」）。
    ///
    /// 判据**全部转调引擎自己的公开访问器**（`CardDef.SpiritOps` / `FaithOps`），⛔ 不在探针里另写表：
    ///   · **灵魂石能力**（`N [Spirit Stone]: …`）—— 收在 `CardDef.SpiritOps`、代价在 `EffectOp.Cost`；
    ///     结算点 = **部署那一拍**（`RuleCore.PlayCard` → `EffectResolver.ResolveSpiritAbility`）。
    ///     🔴 **根因不在电池**：本夹具**起始派系资源 = 0**（`SpiritStones` / `Faith` 都是 0，
    ///     见 `BuildFixture` —— 它只给能量）⇒ 前缀付不起 ⇒ **主档那一拍就已经结算不了**
    ///     （实测：临时给 P0 灌 10 颗，同一批卡里有 22 张当场变成有净差）。要治得动
    ///     **`BuildFixture` 的起始资源** ⇒ 那会改**主档**每行的「信仰/灵魂石/任务点」三列
    ///     ⇒ **基线整体重钉** ⇒ 是独立的一轮（`A1431` 账文那句「得单独一轮」正是这个形状）。
    ///   · **信仰能力**（`N ☀: …`）—— 同形（`CardDef.FaithOps`，`Faith = 0` 发不起来）。
    ///   · **其余** —— 电池**抬不到那个事件**（`steps` 列里哪一步不是 `rc=0`，或那个事件本夹具没有）。</summary>
    static string EcCause(CardDef c)
    {
        if (c == null) return "?";
        if (c.SpiritOps != null && c.SpiritOps.Count > 0) return "灵魂石能力（夹具起始资源=0）";
        if (c.FaithOps != null && c.FaithOps.Count > 0) return "信仰能力（夹具起始资源=0）";
        return "电池抬不到那个事件";
    }

    /// <summary>`A1431` 第三笔：事件电池走完一趟的产物。</summary>
    class EventPass
    {
        public readonly List<string> Changes = new List<string>();
        public readonly List<string> Steps = new List<string>();
        public int PlayRc = -1;
        public bool Played;
        public Snap End;
    }

    /// <summary>`A1431` 第三笔：**一张卡的事件电池读数**。
    /// 🔴 `Net` 是**净差**（电池局 − 空正文对照局）—— 它才是「这张卡的正文在别的时机上跑没跑」。</summary>
    class EventReading
    {
        public CardDef C;
        public string Bucket = "";
        public string Steps = "";
        public int PlayRc = -1;
        public bool Played;
        public bool CtrlPlayed;          // 对照局起步了吗（没起步 ⇒ 净差不可信，如实标出来）
        public int Raw;                  // 电池局自己的变化条数（含与卡无关的事件噪声）
        public int Ctrl;                 // 对照局的变化条数
        /// <summary>🆕 这一张**在主档里是不是「候选」**（卡面有正文 · 打出去了 · 实得变化 0 条）。
        /// 🔴 **必须由 `Sweep` 从主档那一份 `Reading` 传进来** —— 覆盖面那一节要的是
        ///   「**被主档看着、却看不见**的那一批」到底被电池救回来多少，
        ///   ⛔ 不是「整个 `unit` 档」的条数（那里面一半是**卡面根本没正文**的卡，
        ///   它们 `other=0` 与「失明」无关 —— 混进去会把覆盖面**灌水**）。</summary>
        public bool Cand;
        public List<string> Net = new List<string>();
        public int NetDmg;               // 净伤害（两边 `DamageToEnemy[0]` 之差）
        public string Crash;
        public string Note = "";
    }

    /// <summary>空正文替身 —— **同 id / 同卡名 / 同类型 / 同印数 / 同兵种 / 同关键词标记，
    /// 只把 `desc`（与 `descZh`）清空**。
    ///
    /// 🔴 **为什么是「只清正文」而不是「造一个白单位」**（实测踩过）：电池里有**攻击**那一发，
    ///   而攻防算术吃的是**印的数**（攻/远/甲/血/兵种）与**关键词标记**（`Armour 1` / `Flank` …）
    ///   ⇒ 拿一个 `Fix(…, 0, 1, 0)` 当对照，两局的战斗结果**天然不同** ⇒ 整锅净差全是噪声。
    ///   只清正文 ⇒ 那些部分**逐位相同**，净差**只剩正文**那一份。
    /// ⚠️ 关键词**标记**照留 —— 两族理由，都实测过：
    ///   · 它的正文在 `desc` 里（`KeywordTable.EffectText` 对 `"Rally"` 这种**没有** `':'` 的
    ///     一律返回 null ⇒ 关键词只登记**标记**，正文全靠 `desc`）⇒ 清 `desc` 就清掉了正文；
    ///   · **`Fast` / `Flank` 会改「部署当回合能不能动」那一档**（`HasDeployExemption`）——
    ///     对照局丢了它就会在与被测卡**结构不同**的状态下跑电池（那一步的返回码都不同），
    ///     净差立刻变成噪声。
    /// ⚠️ 还原写法时用 `NumericKeywords` 判「原来印没印数字」—— 规范化字典只存值，
    ///   分不出 `Armour 1`（印了）与 `Armour`（没印），而那一分影响卡面角标那一层。</summary>
    static CardDef Blank(CardDef c)
    {
        var kw = new List<string>();
        if (c.Keywords != null)
            foreach (var kv in c.Keywords)
            {
                bool printed = c.NumericKeywords != null && c.NumericKeywords.Contains(kv.Key);
                kw.Add(printed ? kv.Key + " " + kv.Value : kv.Key);
            }
        return new CardDef(c.Id, c.Name, c.Type, "", c.Rarity, c.Faction,
                           c.Cost, c.Attack, c.Health, c.RangedAttack, kw,
                           subtype: c.Subtype);
    }

    /// <summary>敌人那侧**有单位的**一格（先非督军格；都没有才退督军格）。⛔ 只决定「打哪一格」，
    /// 不是判据 —— 打不打得动由引擎的返回码说了算，如实进 `steps`。</summary>
    static int PickAnyEnemy(BattleContext ctx, int p)
    {
        var b = ctx.Players[1 - p].Board;
        for (int i = 0; i < BoardSpec.Size; i++)
            if (i != BoardSpec.WarlordSlot && b[i] != null) return i;
        return BoardSpec.WarlordSlot;
    }

    /// <summary>走一趟事件电池（<see cref="RunEventPass"/> 的正文与取舍见本区块头注）。</summary>
    static EventPass RunEventPass(List<CardDef> pool, CardDef card)
    {
        var ep = new EventPass();
        int handIdx; bool injected;
        var ctx = BuildFixture(pool, card, null, out handIdx, out injected);
        if (handIdx < 0) { ep.Steps.Add("fixture=miss"); return ep; }

        // ---- ⓪ 出牌（与 `MeasurePlay` 同一条路、同一个落点选择）----
        int slot = -1;
        ep.PlayRc = RuleCore.PlayCard(ctx, 0, handIdx, FreeSlot(ctx, 0), out slot);
        ep.Played = ep.PlayRc == RuleCodes.OK;
        ep.Steps.Add("play=" + ep.PlayRc);
        if (!ep.Played) return ep;              // 打不出去 ⇒ 电池无从起步（主档已经报了这条）

        // 🔴 逐段累积差异（⛔ 不是「末态减初态」）：中间态被改回去的（打伤又治好）那一种
        //    在「末态减初态」里**会自己抵消掉** ⇒ 那一段就白测了。与主档 `Diff(before,after)` 同形。
        var s0 = Capture(ctx);

        // ---- ① 誓约（`Oath N:`）—— 🔴 **必须赶在回合电池【之前】、同一回合里用** ----
        //   判据 = `RuleCore.CanUseOathAbility`：`if (!OathAllTurns(...) && u.DeployedTurn != ctx.Turn)
        //   return ErrExhausted;` ⇒ **只有上场的那个回合**能用（实测：放在回合电池之后 ⇒
        //   电池自检的 `Oath` 那一对两条净差都是 0、`steps` 里是 `oath=6`）。
        //   ⚠️ 它**不查 `Exhausted`**（同那个方法：它查的是 `OathUsesThisTurn` 上限与 `DeployedTurn`）
        //   ⇒ 部署当回合就能用，⛔ 不需要（也不许）手改疲劳位。
        if (slot >= 0 && ctx.Players[0].Board[slot] != null)
            ep.Steps.Add("oath=" + RuleCore.UseOathAbility(ctx, 0, slot));
        var s1 = Capture(ctx);

        // ---- ② 两个来回：P0 → P1 → P0（驱动「回合开始 / 回合结束」那一族）----
        for (int k = 0; k < 2; k++)
        {
            ep.Steps.Add("end" + k + "=" + RuleCore.EndTurn(ctx));
            RuleCore.BeginTurn(ctx);
        }
        var s2 = Capture(ctx);

        // ---- ③ 抽一张 ----
        RuleCore.Draw(ctx, 0);
        ep.Steps.Add("draw=1");
        var s3 = Capture(ctx);

        // ---- ④ 主动用（`Duty:` / `Pray:` / `Ferocity:` / `Agenda:` / `Talent:` / `Artifice:`）----
        int tgt = PickAnyEnemy(ctx, 0);
        if (slot >= 0 && ctx.Players[0].Board[slot] != null)
        {
            ep.Steps.Add("abil=" + RuleCore.UseAbility(ctx, 0, slot, tgt));
            var alt = RuleCore.AvailableAlternative(ctx, 0, slot);
            if (alt != null)
                ep.Steps.Add("alt(" + alt + ")=" + RuleCore.UseAlternative(ctx, 0, slot, alt, tgt));
        }
        var s4 = Capture(ctx);

        // ---- ⑤ 攻击（`Strike:` / `Mob:` / `Regiment:` / `Cruelty:`）----
        //   🔴 **打两发**（一发近战 ＋ 一发远程）—— 判据在引擎里是**分成两条**的：
        //     `RuleCore.cs:4312` `if (!ranged && …) FireTriggerAt(… Mob …)` ·
        //     `:4334` `if (ranged && …) FireTriggerAt(… Regiment …)`
        //     ⇒ **只打一发**时，挂另一个头的整族**在电池里永远抬不起来**
        //     （实测：自检的 `Regiment` 那一对两条净差**都是 0**，红的）。
        //   第一发走引擎自己的自然选择（`CurrentAttackIsRanged`）；第二发是**另一种**，
        //   走 `DeclareAttack(..., forced: true)` = 引擎自己的「**不是玩家点出来的**那一档」
        //   （原版 `scriptedAttack(0x34)`，见 `CanAttackNow` 头注：它跳过回合/配额那一段）——
        //   ⛔ 探针**不手改 `Exhausted` / `AttacksThisTurn`**。两发的返回码**都进 `steps`**。
        if (slot >= 0 && ctx.Players[0].Board[slot] != null)
        {
            bool first = RuleCore.CurrentAttackIsRanged(ctx, 0, slot);
            ep.Steps.Add((first ? "atkR" : "atkM") + "="
                         + RuleCore.DeclareAttack(ctx, 0, slot, 1, PickAnyEnemy(ctx, 0), first));
        }
        var s5 = Capture(ctx);
        if (slot >= 0 && ctx.Players[0].Board[slot] != null)
        {
            // ⚠️ 第二步那两格叫 `atkM2` / `atkR2`（**第二发**）—— ⛔ 别写成 `atkM!` / `atkR!`，
            //    那会和 `!=` 撞脸，读成「atkR 不等于 0」。
            bool second = !RuleCore.CurrentAttackIsRanged(ctx, 0, slot);
            ep.Steps.Add((second ? "atkR2" : "atkM2") + "="
                         + RuleCore.DeclareAttack(ctx, 0, slot, 1, PickAnyEnemy(ctx, 0), second, forced: true));
        }
        var s6 = Capture(ctx);

        // ---- ⑥ 打死一个敌人（`Slay:` / 死亡触发那一族）----
        int kv = PickAnyEnemy(ctx, 0);
        var victim = ctx.Players[1].Board[kv];
        if (victim != null)
            // ⚠️ `kill(dmg)=` 里的值是 **`ApplyDamage` 的返回（实得伤害）**，⛔ **不是 RuleCode**
            //    —— 别跟同一串里那些 `rc` 混着读（`999` 那个数就是这么来的）。
            ep.Steps.Add("kill(dmg)=" + RuleCore.ApplyDamage(ctx, victim, 999, "夹具事件电池"));
        var s7 = Capture(ctx);

        Diff(s0, s1, ep.Changes);
        Diff(s1, s2, ep.Changes);
        Diff(s2, s3, ep.Changes);
        Diff(s3, s4, ep.Changes);
        Diff(s4, s5, ep.Changes);
        Diff(s5, s6, ep.Changes);
        Diff(s6, s7, ep.Changes);
        ep.End = s7;
        return ep;
    }

    /// <summary>`A1431` 第三笔：一张卡的**事件电池读数**（含对照局的净差）。</summary>
    static EventReading MeasureEvents(List<CardDef> pool, CardDef card)
    {
        var r = new EventReading { C = card };
        try
        {
            r.Bucket = Bucket(card);
            var a = RunEventPass(pool, card);
            r.Steps = string.Join(",", a.Steps);
            r.PlayRc = a.PlayRc;
            r.Played = a.Played;
            r.Raw = a.Changes.Count;
            if (!a.Played) { r.Note = "电池没起步（出牌 rc=" + CodeName(a.PlayRc) + "）"; return r; }
            var b = RunEventPass(pool, Blank(card));
            r.CtrlPlayed = b.Played;
            r.Ctrl = b.Changes.Count;
            if (!b.Played)
            {
                // 🔴 **对照局没起步 ⇒ 净差不可信** —— 出声，⛔ 不拿「电池局减 0 条」当净差。
                r.Note = "**对照局没起步**（空正文替身 rc=" + CodeName(b.PlayRc) + "）⇒ 本行净差不可信";
                r.Net = a.Changes;
            }
            else
            {
                r.Net = a.Changes.Where(x => !b.Changes.Contains(x)).ToList();
                if (a.End != null && b.End != null) r.NetDmg = a.End.Dmg[0] - b.End.Dmg[0];
            }
        }
        catch (Exception ex)
        {
            r.Crash = ex.GetType().Name + ": " + ex.Message;
        }
        return r;
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
    //   ⚠️ **付费能力段（`N <货币>:` / `Oath N:`）一律不算「该发生」** —— 它是**主动用**。
    //      判据 = **引擎自己那次 `ParseSegment`**（见 `IsPaidSegment` / ⑥ 自检，`A1599`）。
    //
    // `oldPaidRule` 🆕 `A1599`：**只给自检用** —— `true` = 用**旧那条半截正则**
    //   （`WillRun.IsPaidHead`，只认 `N <货币>:`）重算一遍，好让「旧口径必红」是**机器量出来的**
    //   而不是靠嘴说。⛔ **生产路径一律取缺省 `false`**（判据 = 引擎自己那次 `ParseSegment`）。
    static string Bucket(CardDef c, bool oldPaidRule = false)
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
            // 🔴 **`A1599`（2026-10-12）**：付费前缀的判据**转调引擎自己那一次 `ParseSegment`**
            //    （<see cref="IsPaidSegment"/>，与 `WillRun.AddFaceBody` / `WillRun.Classify` ② 同一处）。
            //    改前用的是 `WillRun.IsPaidHead` —— 那条正则只认 `N <货币>:` 那**半截**，
            //    **漏掉引擎同样认的 `Oath N:`**（`EffectText.ReOathPaid`）⇒ 那些段落到「认不出的头」
            //    那一支 ⇒ **整张卡被归进 C 档**（`oldPaidRule` 那一支就是那时的行为，自检里并排量出来）。
            //    ⛔ **别把它读成「誓约能力没收下来」** —— 它们收得到（`CardDef.OathOps`）；
            //       这一处只决定**档位**（C 还是 B），与「收到没有」是两条腿。
            if (oldPaidRule ? WillRun.IsPaidHead(head) : IsPaidSegment(s)) continue;  // 付费能力 = 主动用
            if (CardDef.HandledByOtherLayer(c, raw) != null) continue;          // 别的层接手
            unguarded = true;   // 认不出的头 ⇒ ⛔ 不替它豁免（宁可列进可疑档，不静默放过）
        }
        return unguarded ? "C 部署时该发生" : "B 等事件/等主动用";
    }

    /// <summary>🆕 **`A1599`：这一段是不是「引擎认的付费前缀段」**（`N &lt;货币&gt;:` / `Oath N:` …）。
    ///
    /// 🔴 **判据 = 引擎自己那一次 `ParseSegment`**（`SegResult.PaidCost &gt; 0`），
    ///    ⛔ 不是探针自己写一条头正则 —— 付费用**两条**语法（`EffectText.RePaid` / `ReOathPaid`），
    ///    任何一条手写正则迟早漏掉其中一条（`WillRun.IsPaidHead` 就漏了 `Oath N:`，实测）。
    ///    ⚠️ 传的是**整段原文**（含 `Head:`），与 `WillRun.AddFaceBody` 里那一处逐字同形。
    /// </summary>
    static bool IsPaidSegment(string seg)
    {
        var segr = EffectText.ParseSegment(seg);
        return segr != null && segr.PaidCost > 0;
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

    /// <summary>🆕 `A1431` 第三笔：**事件电池**的单卡读数（`play &lt;id&gt;` 打在主档读数之后）。
    /// ⛔ 与 <see cref="Print"/> 一样**只摆读数、不判对错**。</summary>
    static void PrintEvents(CardDef c, EventReading r)
    {
        Console.WriteLine();
        Console.WriteLine("=== play " + c.Id + " · 事件电池（`A1431` 第三笔 · 打出去**之后**那一段）===");
        Console.WriteLine("  电池的步骤/返回码 : " + r.Steps);
        Console.WriteLine("  档                : " + r.Bucket
                          + "   · 主档候选（卡面有正文 · 打出去 · 主档实得 0 条）: " + (r.Cand ? "**是**" : "否"));
        Console.WriteLine("  对照局（空正文替身）起步了吗 : " + (r.CtrlPlayed ? "是" : "**否** —— 净差不可信"));
        Console.WriteLine("  电池局自己的变化条数 = " + r.Raw + " · 对照局 = " + r.Ctrl
                          + " ⇒ **净差 = " + r.Net.Count + " 条**（净伤害 " + r.NetDmg + "）");
        if (r.Crash != null) Console.WriteLine("  🔴 **电池抛异常**：" + r.Crash);
        if (r.Note.Length > 0) Console.WriteLine("  🔴 " + r.Note);
        if (r.Net.Count == 0)
            Console.WriteLine("  ⛔ **净差 0 条 = 电池没抬起这一张的正文** ⇒ 它**仍然没被「事件的结算」验到**。"
                              + "根因 = **" + EcCause(c) + "**（见 `play all` 的 ⑥ 节）。");
        else
            foreach (var ch in r.Net) Console.WriteLine("     " + ch);
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
        var ecRows = new List<string>();      // 🆕 `A1431` 第三笔：**事件电池**（`E|` 前缀那一套，见 `FingerprintERow`）
        var ecReads = new List<EventReading>();   // 电池档的读数（末尾那两节要按档统计）

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

            // 🆕 `A1431` 第三笔：**事件电池**（只对 `unit`，见本区块头注）
            if (c.Type == "unit")
            {
                var er = MeasureEvents(pool, c);
                er.Cand = r.Played && r.FaceHasOps && r.Other == 0;   // 见 `EventReading.Cand`
                ecRows.Add(FingerprintERow(c, er));
                ecReads.Add(er);
            }

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
        Console.WriteLine("     主档（打出去那一拍） " + FingerprintMd5(pfRows) + "   （" + pfRows.Count + " 张）");
        Console.WriteLine("     事件电池（`E|` 那一套）" + FingerprintMd5(ecRows) + "   （" + ecRows.Count + " 张 unit）");
        Console.WriteLine("     合起来（= 基线文件的非 `#` 行 · 哨兵钉的就是这一串） "
                          + FingerprintMd5(pfRows.Concat(ecRows)) + "   （" + (pfRows.Count + ecRows.Count) + " 行）");
        Console.WriteLine("     含什么：`id|type|warlord|playRc|played|faceOps|自落场|机械|实得|应付|能量Δ|手牌价Δ|伤害|"
                          + "变化条数|crash|档|**变化逐条全文**`；电池档见 " + string.Join("/", PfECols) + "。");
        Console.WriteLine("     ⛔ 不含什么：**卡名**（改名的产物，本仓已改过 6 张）· `note`（口径散文，电池档除「对照局没起步」外）·"
                          + "引擎日志（措辞会变）—— 这三样进来会**天天红**，把「红了不用管」训练成习惯。");
        if (!string.IsNullOrEmpty(pfPath))
        {
            var pdir = Path.GetDirectoryName(Path.GetFullPath(pfPath));
            if (!string.IsNullOrEmpty(pdir)) Directory.CreateDirectory(pdir);
            File.WriteAllText(pfPath, FingerprintFileText(pfRows, ecRows));
            Console.WriteLine("     wrote " + pfPath + " lines=" + (pfRows.Count + ecRows.Count + 1));
        }
        else
        {
            Console.WriteLine("     ⚠️ 没写指纹文件 —— 要留一份「被确认的状态」⇒ `play all pf <out.txt>`"
                              + "（写完之后**手改** `PlayProbe.PinnedPlayMd5` = 上面那串，见 `play check`）。");
        }

        // ------------------------------------------------------------------
        //  🆕 `A1431` 第三笔：**事件电池**的覆盖面 —— 「档 B」那一档今天到底被验到多少
        // ------------------------------------------------------------------
        Console.WriteLine();
        Console.WriteLine("  --- ⑥ 🔴 事件电池的覆盖面（`A1431` 第三笔 · 治「档 B」的结构性失明）---");
        Console.WriteLine("     量的是什么：打出去**之后**再走一段事件电池（两个来回 / 抽一张 / 主动用 / 攻击一发 / 打死一个），"
                          + "读数 = **电池局 − 空正文对照局**（见 `EventReading.Net`）。");
        Console.WriteLine("     ⛔ 它**不改**上面主档的任何一格；只把新读数**追加**在指纹文件的第二段。");
        foreach (var bucket in new[] { "B 等事件/等主动用", "C 部署时该发生" })
        {
            // ⚠️ 分母 = **候选**（`Cand`：卡面有正文 · 打出去了 · 主档实得 0 条）——
            //    那一批才是「主档看着它、却看不见」的卡；⛔ 拿整个 unit 档当分母是灌水。
            var g = ecReads.Where(r => r.Bucket == bucket && r.Cand).ToList();
            if (g.Count == 0) continue;
            var cov = g.Where(r => r.Net.Count > 0).ToList();
            Console.WriteLine("     ── " + bucket + " · **候选 " + g.Count + " 张**"
                              + "（该档 unit 全档 " + ecReads.Count(r => r.Bucket == bucket) + " 张）· "
                              + "**电池净差 > 0 的 " + cov.Count + " 张**"
                              + (bucket.StartsWith("B") ? "（=「档 B」今天被事件验到的张数）" : "") + " ──");
            var zero = g.Where(r => r.Net.Count == 0).ToList();
            if (zero.Count > 0)
            {
                Console.WriteLine("        🔴 净差 = 0 的 " + zero.Count + " 张 —— 主档那一拍看不见、电池这一趟也没抬起来"
                                  + " ⇒ **这一档里它们仍然没被「事件的结算」验过**（⛔ 不是「没有差异」，"
                                  + "是**没验**；逐张对着卡面看）");
                // 🔴 **按根因分**（判据 = 引擎自己的公开访问器，见 `EcCause`）——
                //    ⛔ 不写「总之没验到」那种没信息量的话：灵魂石/信仰那两族**根因不在电池**。
                foreach (var grp in zero.GroupBy(r => EcCause(r.C)).OrderByDescending(x => x.Count()))
                    Console.WriteLine("          · " + Pad(grp.Key, 30) + " = " + grp.Count() + " 张");
                foreach (var r in zero.Take(60))
                    Console.WriteLine("          " + Pad(r.C.Id, 24) + Pad(r.C.Name, 30)
                                      + " steps=" + Clip(r.Steps, 90));
                if (zero.Count > 60) Console.WriteLine("          …（还有 " + (zero.Count - 60) + " 张，看指纹文件）");
            }
        }
        var ecBad = ecReads.Where(r => r.Note.Length > 0 || r.Crash != null).ToList();
        Console.WriteLine("     对照局没起步 / 电池抛异常的行 = **" + ecBad.Count + " 张**"
                          + (ecBad.Count > 0 ? "  ← 🔴 出声（逐条）" : "（✅ 没有）"));
        foreach (var r in ecBad.Take(30))
            Console.WriteLine("       " + Pad(r.C.Id, 24) + " " + Clip(r.Crash ?? r.Note, 120));

        Console.WriteLine();
        Console.WriteLine("  判据：**抛异常 = 0 张** ⇒ rc=0；否则 rc=1。");
        Console.WriteLine("  ⚠️「候选」与「打不出去」**都不计入 rc** —— 它们要**对着卡面**逐张判，"
                          + "本工具不代替判决（⛔ 也不许静默：两栏都在上面逐条打了出来）。");

        // ------------------------------------------------------------------
        //  🆕 `A1599`：`Bucket` 的【付费前缀判据】自检（**计入 rc**）
        // ------------------------------------------------------------------
        Console.WriteLine();
        int bucketFail = BucketSelfCheck(pool);
        Console.WriteLine("  ⇒ 判据自检（`A1599`）" + (bucketFail == 0 ? "全过 ✅" : "红 " + bucketFail + " 条 🔴")
                          + " —— 它也计入 rc。");

        // ------------------------------------------------------------------
        //  🆕 `A1431` 第三笔：**事件电池**的自检（**计入 rc**）—— 灭自证
        // ------------------------------------------------------------------
        Console.WriteLine();
        int ebFail = EventBatterySelfCheck(pool);
        Console.WriteLine("  ⇒ 判据自检（`A1431` 第三笔 · 事件电池）"
                          + (ebFail == 0 ? "全过 ✅" : "红 " + ebFail + " 条 🔴") + " —— 它也计入 rc。");

        // 🔴 **电池档的「出声」也计入 rc** —— 一行「对照局没起步 / 电池抛异常」= **那一张没验到**，
        //    正是本笔要治的那件事（看着绿、实际没验）⇒ 一律红，⛔ 不许只印一行字。
        if (ecBad.Count > 0) ebFail++;

        return (crash > 0 || bucketFail > 0 || ebFail > 0) ? 1 : 0;
    }

    // ==================================================================
    //  🆕 `A1599`：`Bucket` 付费前缀判据的自检
    // ==================================================================

    /// <summary>🆕 **`A1599`**：`Bucket` 的付费前缀判据自检（返回**红的条数**）。
    ///
    /// 🔴 **为什么非要有它**：`Bucket` 原来用 `WillRun.IsPaidHead`（正则 `^\d+\s*[\[\(]?\s*[a-z☀]`）
    ///   当「这段是不是付费能力」的判据，而**引擎认的付费前缀有两条**（`EffectText.RePaid` /
    ///   `ReOathPaid`）⇒ 那条正则**漏掉 `Oath N:`**（`oath 2` 以字母开头，第一位 `\d` 就不匹配）
    ///   ⇒ 这些段落进「认不出的头」那一支 ⇒ **整张卡被归进 C 档**（=「部署那一拍就该有变化」）。
    ///   ⚠️ **它不是「誓约能力没收下来」** —— 那 28 张（池侧现读）的 `Oath N:` 收得到
    ///   （`CardDef.OathOps`）；这一条只治**档位**（C 还是 B），⛔ 别混成那 18/28 张的假警报。
    ///
    /// **断言 A —— 旧口径必红的那一条**
    ///   夹具 `Oath 2: …`（**一个字都不改**，只让头是引擎认的 `Oath N:`）：
    ///   新口径 ⇒ **B**（付费能力 = 主动用，本夹具产生不了）；旧口径 ⇒ **C**（认不出这个头）。
    ///   ⇒ **旧口径下这条断言结构上不可能绿。**
    /// **断言 B —— 反面对照（覆盖面没丢）**
    ///   `2 ☀: …` / `2 [Spirit Stone]: …`：**新旧两个口径都必须给 B** ——
    ///   挡住「把付费判定整个删掉」那种改法（那种改法会让 A 变绿、B 变红）。
    /// **断言 C —— 池侧（钉子）**
    ///   ① 池里**存在**「引擎认付费、旧正则认不出」的段（= 这条断言今天不是空话）；
    ///   ② **新旧口径给出不同档位的卡 ≥ 1**（= 改动真的改了读数；⛔ 一旦有人把它改回去，这里立刻 0 条 ⇒ 红）；
    ///   ③ 每一张「档位变了」的卡，**都**含至少一条这样的段（= 变化**由这一处引起**，不是别的原因顺带红）。
    ///
    /// ⚠️ 夹具卡**在内存里现造**（`new CardDef(...)`，⛔ 不进卡池、⛔ 不碰 `MyGame/Assets/**`）。
    /// </summary>
    static int BucketSelfCheck(List<CardDef> pool)
    {
        Console.WriteLine("  --- ⑦ 🔴 `Bucket` 付费前缀判据自检（`A1599`：判据 = **引擎自己那次 `ParseSegment`**）---");
        int fail = 0;
        Func<string, string, CardDef> fx = (id, desc) =>
            new CardDef(id, id, "unit", desc, "common", "Fixture", 1, 1, 1, 0, null, subtype: "Troop");

        // ---- 断言 A：`Oath N:` —— 引擎认、旧正则认不出 ⇒ 新 B / 旧 C ----
        foreach (var desc in new[] { "Oath 2: Deal 3 damage to an enemy troop",
                                     "Oath 1: Give Armour 1 to a friendly troop" })
        {
            var f = fx("FX-P1", desc);
            string nw = Bucket(f, false), od = Bucket(f, true);
            int paid = IsPaidSegment(desc) ? 1 : 0;
            bool oldSays = WillRun.IsPaidHead(desc.Substring(0, desc.IndexOf(':')).Trim().ToLowerInvariant());
            bool ok = nw == "B 等事件/等主动用" && od == "C 部署时该发生";
            Console.WriteLine("    " + (ok ? "✅" : "🔴") + " A 「" + desc + "」");
            Console.WriteLine("        新口径（引擎 `ParseSegment`）⇒ **" + nw + "**  ·  旧口径（`IsPaidHead`）⇒ **" + od + "**");
            Console.WriteLine("        引擎说的：`ParseSegment.PaidCost` = " + paid + "（>0 = 付费前缀段）"
                              + "  ·  `WillRun.IsPaidHead(\"" + desc.Substring(0, desc.IndexOf(':')).Trim()
                              + "\")` = " + (oldSays ? "true" : "**false**（← 那条正则漏掉了它）"));
            if (!ok)
            {
                fail++;
                Console.WriteLine("        🔴 期望 **新 = B / 旧 = C** —— 旧口径下这里只能给 C（元凶 = 那条半截正则）；"
                                  + "若新口径也给 C，说明 `Bucket` 又拿形状正则当付费判据了。");
            }
        }

        // ---- 断言 B：反面 —— 老的两条写法【两个口径都必须给 B】（⛔ 覆盖面不许丢）----
        foreach (var desc in new[] { "2 ☀: Deal 3 damage to an enemy troop",
                                     "2 [Spirit Stone]: Deal 3 damage to an enemy troop" })
        {
            var f = fx("FX-P2", desc);
            string nw = Bucket(f, false), od = Bucket(f, true);
            bool ok = nw == "B 等事件/等主动用" && od == "B 等事件/等主动用";
            Console.WriteLine("    " + (ok ? "✅" : "🔴") + " B 「" + desc + "」⇒ 新 **" + nw + "** / 旧 **" + od
                              + "**（**两个口径都必须是 B**）");
            if (!ok) fail++;
        }

        // ---- 断言 C：池侧 —— ① 这类段真的存在 ② 档位真的变了 ③ 变化由它引起 ----
        var changed = new List<string>();
        int segFailingOld = 0;
        foreach (var c in pool)
        {
            if (c == null) continue;
            bool hasSuchSeg = false;
            foreach (var raw in EffectText.Split(c.Desc))
            {
                if (string.IsNullOrEmpty(raw)) continue;
                string s = EffectText.StripLeadingIcons(raw.Trim());
                if (string.IsNullOrEmpty(s)) continue;
                int col = s.IndexOf(':');
                if (col <= 0) continue;
                string head = s.Substring(0, col).Trim().ToLowerInvariant();
                if (IsPaidSegment(s) && !WillRun.IsPaidHead(head)) { hasSuchSeg = true; break; }
            }
            if (hasSuchSeg) segFailingOld++;
            string nw = Bucket(c, false), od = Bucket(c, true);
            if (nw != od)
                changed.Add(c.Id + " [" + c.Type + "] " + od + " → " + nw + (hasSuchSeg ? "" : "  ← 🔴 无此类段"));
        }
        bool okC = segFailingOld > 0 && changed.Count > 0 && !changed.Any(x => x.Contains("← 🔴 无此类段"));
        Console.WriteLine("    " + (okC ? "✅" : "🔴") + " C 池侧（" + pool.Count + " 张）：");
        Console.WriteLine("        ① 含「引擎认付费、`IsPaidHead` 认不出」的段的卡 = **" + segFailingOld
                          + " 张**（必须 > 0：否则本断言是空话）");
        Console.WriteLine("        ② 新旧口径**档位不同**的卡 = **" + changed.Count
                          + " 张**（必须 > 0：⛔ 一旦有人把判据改回那条半截正则，这里就是 0 ⇒ 红）");
        Console.WriteLine("        ③ 每一张都**含**这样的段（否则「档位变了」另有原因，要回来重读）");
        foreach (var x in changed.Take(40)) Console.WriteLine("           · " + x);
        if (changed.Count > 40) Console.WriteLine("           …（还有 " + (changed.Count - 40) + " 张）");
        if (!okC)
        {
            fail++;
            Console.WriteLine("        🔴 期望三条同时成立（见上）。最要紧的一条：⚠️ **别把 `Oath N:` 那批读成"
                              + "「誓约能力没收下来」** —— 它们收得到（`CardDef.OathOps`），这里只关**档位**。");
        }
        Console.WriteLine("    ⇒ 自检 **" + (fail == 0 ? "全过 ✅" : "红 " + fail + " 条 🔴") + "**");
        return fail;
    }

    // ==================================================================
    //  🆕 `A1431` 第三笔：**事件电池**的自检（灭自证）
    // ==================================================================

    /// <summary>🆕 **`A1431` 第三笔**：事件电池的判据自检（返回**红的条数**）。
    ///
    /// 🔴 **它在防的那件事**：「电池看着跑过了、其实**抬不起那个事件**」—— 那正是本笔要治的病
    ///   （一个**看着绿、实际没验**的档）。只断「净差 > 0」**挡不住**它：
    ///   电池里那两个来回**本来就会**产生一串与卡无关的变化（两边抽牌 / 能量刷新 / 回合计数），
    ///   只要对照局算错一点，「净差 > 0」就能**靠噪声**成立。
    ///
    /// **断言 = 结构上不可能靠噪声满足的那一条**：每一对比的是**同一张夹具卡、只把一个数字改掉**
    ///   （`5 → 9`）。若电池**真抬起了**那个事件 ⇒ 两条的净差**必然不同**（伤害/加值不一样了）；
    ///   若电池**压根没抬起**它 ⇒ 两条都是**空的净差**、必然**相同** ⇒ 红。
    ///   ⚠️ 噪声对两条**是一样的**（同种子、同夹具、同事件序列）⇒ 它**帮不了**这一条断言。
    ///
    /// **覆盖面**：每一对都指名一个**事件**（`Strike` / `Regiment` / `Duty` / `Pray` / `Oath` /
    ///   回合开始 / 回合结束 / `Slay`）—— 这些正是「档 B」的成员挂着的那些头。
    ///   ⇒ 哪一条红了，就等于说「**这个事件电池没抬起来**」，档 B 里挂那个头的卡**仍然没被验到**。
    ///
    /// ⚠️ 夹具卡**在内存里现造**（`new CardDef(...)`，⛔ 不进卡池、⛔ 不碰 `MyGame/Assets/**`）。
    /// </summary>
    static int EventBatterySelfCheck(List<CardDef> pool)
    {
        Console.WriteLine("  --- ⑧ 🔴 事件电池判据自检（`A1431` 第三笔 · **灭自证**：同卡改一个数，净差必须不同）---");
        int fail = 0;
        // 夹具单位：`3/2/20`（近战 3 · 远程 2 · **血 20**），费用 1（能落场、能打两下）。
        // 🔴 **血必须够厚**（20）—— 实测踩过：血 4 的夹具在**第一发（近战）**就被反击打死
        //    ⇒ 第二发（远程）根本没单位可打 ⇒ `Regiment:` 那一对两条净差**都是 0**（假红）。
        Func<string, string, string[], CardDef> fx = (id, desc, kw) =>
            new CardDef(id, id, "unit", desc, "common", "Fixture", 1, 3, 20, 2, kw, subtype: "Troop");

        // 每一对 =（这一对在验哪个事件 · 真的正文 · 只把一个数改掉的正文 · 关键词）
        var pairs = new[]
        {
            new object[] { "Strike（攻击那一发）",   "Strike: Deal 5 damage to an enemy troop",                 "Strike: Deal 9 damage to an enemy troop",                 new string[0] },
            new object[] { "Regiment（攻击那一发）", "Regiment: Gain +2 [Ranged]",                              "Regiment: Gain +7 [Ranged]",                              new string[0] },
            new object[] { "Mob（攻击那一发）",      "Mob: Gain +1 [Melee]",                                    "Mob: Gain +6 [Melee]",                                    new string[0] },
            new object[] { "Duty（主动用）",         "Duty: Deal 3 damage to an enemy troop",                   "Duty: Deal 8 damage to an enemy troop",                   new[] { "Duty" } },
            new object[] { "Pray（主动用）",         "Pray: Deal 3 damage to an enemy troop",                   "Pray: Deal 8 damage to an enemy troop",                   new[] { "Pray" } },
            new object[] { "Oath（誓约）",           "Oath 2: Deal 3 damage to an enemy troop",                 "Oath 2: Deal 7 damage to an enemy troop",                 new[] { "Oath" } },
            new object[] { "回合开始",               "At the start of your turn, deal 3 damage to an enemy troop", "At the start of your turn, deal 8 damage to an enemy troop", new string[0] },
            new object[] { "回合结束",               "At the end of your turn, deal 3 damage to a random enemy",  "At the end of your turn, deal 8 damage to a random enemy",  new string[0] },
        };

        foreach (var p in pairs)
        {
            string what = (string)p[0], a = (string)p[1], b = (string)p[2];
            var kw = (string[])p[3];
            var ra = MeasureEvents(pool, fx("FX-EBA", a, kw));
            var rb = MeasureEvents(pool, fx("FX-EBA", b, kw));
            string na = Norm(ra), nb = Norm(rb);
            bool diff = na != nb;
            Console.WriteLine("    " + (diff ? "✅" : "🔴") + " " + Pad(what, 22)
                              + " 净差 " + ra.Net.Count + " 条 vs " + rb.Net.Count + " 条"
                              + (ra.Net.Count == 0 && rb.Net.Count == 0 ? "（**两条都是空的**）" : ""));
            Console.WriteLine("        真：`" + Clip(a, 70) + "`  ⇒ " + Clip(na, 150));
            Console.WriteLine("        改：`" + Clip(b, 70) + "`  ⇒ " + Clip(nb, 150));
            Console.WriteLine("        steps " + Clip(ra.Steps, 110)
                              + (ra.Note.Length > 0 ? "   ⚠️ " + ra.Note : ""));
            if (!diff)
            {
                fail++;
                Console.WriteLine("        🔴 期望两条净差**不同** —— 相同 = **电池没抬起这个事件**"
                                  + "（要么 `steps` 里那一步返回码不是 0，要么那个头根本没被 `WillRun` 认领）"
                                  + " ⇒ 「档 B」里挂这个头的卡**仍然没被验到**。");
            }
        }
        Console.WriteLine("    ⇒ 自检 **" + (fail == 0 ? "全过 ✅" : "红 " + fail + " 条 🔴")
                          + "**（红的每一条 = 那个事件电池没抬起来）");
        return fail;
    }

    /// <summary>归一化净差（给自检**比两条**用）：只留**条数**与**排序后的逐条原文**，
    /// ⛔ 不带卡 id 之类「本来就不同」的东西 —— 否则这一条断言会**恒真**（自证）。</summary>
    static string Norm(EventReading r)
    {
        if (r.Crash != null) return "crash:" + r.Crash;
        return r.Net.Count + "|" + string.Join(";", r.Net.OrderBy(x => x, StringComparer.Ordinal));
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
    //   🆕 **`A1431` 第三笔**：对拍现在是**两套**（①-a 主档 · ①-b **事件电池**，见上面那一整块），
    //     外加 **①-c「电池覆盖了谁」** —— 那一节把「**净差 = 0**」的候选**逐条列出并分根因**，
    //     ⛔ 它治的就是本笔的起因：**一个看着绿、实际没验的档**（主档的 `other=0` 原来在
    //     「档 B」上是**常量**，它坏掉也不会有任何一行变红）。
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
    /// ⚠️ **它现在钉的值** → **见下面那一行常量**（⛔ 别在这里再抄一份 —— 两处迟早不一致）。
    /// 🔴 **2026-10-13 就地订正（铁律 5）**：本段原来写「它现在钉的值（原样交代）：`7175ec95…`
    ///   = 2026-10-12（第二十一会话 · `E5`）当次现读 …… 那时 **411 张**候选还没逐张对过卡面」——
    ///   **那一句已经不成立了**（常量早被第二十三会话重钉过，`411` 现读也是 **407**）。
    ///   错因 = 重钉时**只改了常量、没回头改这段说明**（同一个坑：重写正本最容易漏「夹在里面的那半句」）。
    ///   ⇒ 现在的规矩：**值只写在常量那一行**，本段只交代「为什么必须有它」与「重钉纪律」；
    ///   每次重钉的**归因**写在常量上方的注释块里（那里是**历史**，允许留着）。
    /// ⚠️ **它钉的永远是【当时那一版】，⛔ 不是「当时就对」** —— 逐卡候选要对着卡面判，机器判不了。
    ///   ⛔ **不许**为了让它变绿而重跑 `play all pf` —— 那只把洞开回来。
    /// </summary>
    // 🔴 **2026-10-12 第二十三会话：有意重钉** —— 依据是逐卡差异**逐条归因完毕**，⛔ 不是把红抹平。
    //   ① `GSC_Jammed_Communications` 的 `faceOps 0 → 1` = **本会话 `W1`（`A1686`）** 给前缀形 `When <事件>, …`
    //      产出的 `whenclause` 标记 op（**该卡因此从「空表」变「收到一条」**，正是那笔要的）——
    //      已由 `RuleEngineTest.Run` **5246 / 5246 全过**确认（日志 `_tmp_view/s23_ruleengine2.log`）。
    //   ② `SOR52` / `SOR55` / `SOR66` 的 `other` / `energyDelta` / `changesFull` = **上一会话 `A1605`**
    //      （信仰前缀 8 种写法统一成 `☀`；那三张原来的 `[Energy]` 被认成能量 ⇒ 现在按信仰计）—— 已确认的改动。
    //   ③ `TAU20` + 16 张 `UM*` 的 `bucket C → B` = **上一会话 `A1599`（`W7`）** 改 `IsPaidSegment` 的
    //      直接后果（它自己的报告就写着「16 张档位真变了」）。
    //   ⚠️ **以上 21 张与本会话的 `W2` 结算改动无关** —— 它自己用 8 张逐张真打 A/B 证明过（逐字相同）。
    //   ⚠️ 重钉只能**手改源码**（⛔ 没有自动口子），判据与纪律 → `README.md` 的「改探针输出格式三步」。
    // 🔴 **2026-10-13 第二十四会话：有意重钉（`A1431` 第三笔 · 事件电池）** ——
    //   依据是 **主档逐行【逐字节未变】**，⛔ 不是把红抹平。**机器验过的证据**：
    //     · 重钉前 `git status`（本仓纪律，见 CLAUDE.md 铁律 13·6）：`ruleprobe/PlayProbe.cs`（**我**在改）·
    //       `ruleprobe/WillRun.cs`（**另一路写手 `A1694` 在飞**）· `RuleEngine/Core/CardDef.cs`（**第三路**）。
    //       后两份现读都是**纯文档/自检**改动（`git diff` 逐条看过：`CardDef.cs` 只动 XML doc
    //       里的三个计数；`WillRun.cs` 加的是 `⑨ Source` 自检与 dump 的一列）⇒ 不改结算语义。
    //     · **主档 md5 重钉后仍是 `83655a105f8eeb8111eab9f0fa479fc7`** = 与上一版**逐字符相同**
    //       （旧基线 1126 行 vs 新基线 1126 行，集合差 **0 / 0**）⇒ 这两路的在飞改动**没有被烘进来**。
    //     · 新增的只有 **586 行 `E|` 行**（`type=unit` 的事件电池）⇒ `git diff --numstat` =
    //       **592 / 0**（592 = 586 行 ＋ 6 行 `#` 注释/分段头）—— **纯追加，一行都没改**。
    //   ⚠️ 前面那份「`7175ec95…` / 第二十三会话重钉」的归因记录**保留在 git 历史里**，⛔ 不在这里抄第二份。
    //   ⚠️ 它钉的仍是**当时那一版**、⛔ **不是「当时就对」**：主档那 407 张候选**还没逐张对过卡面**；
    //     而电池那 586 行里有 **189 张候选的净差 = 0**（**没被验到**，逐条列在 `play check` 的 ①-c）。
    //
    // 🔴 **重钉（⛔ 没有「一条命令重钉」）**：`play check` 会把该抄的**整行**打出来，**手改这一个常量**。
    //   **重钉前必须先确认这批变化是对的**（对着卡面 / 反编译 / 真机逐张看过）——
    //   为了「把红抹平」而重钉 = **正是它在抓的那件事**。
    public const string PinnedPlayMd5 = "d1fe0d96b725c6b2b12444527d94b7e4";


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

    // ------------------------------------------------------------------
    //  🆕 `A1431` 第三笔：**事件电池**的指纹行
    // ------------------------------------------------------------------
    //
    // 🔴 **它为什么必须【另起一套行】、⛔ 不许并进主档**：
    //   · 主档那一批（407 张候选）**已经逐张对过卡面**；把它重算一遍 = 让那批判决全部作废
    //     （`A1431` 账文明写着「那会改动全池所有读数 ⇒ 得单独一轮」）。
    //   · 两档量的是**两件事**（打出去那一拍 / 打出去之后的事件），混在一格里
    //     「哪一档红了」就分不出来了。
    //   ⇒ 本档的行**第一列带前缀 `E|`**（见 <see cref="ECPrefix"/>），同一个文件、两套行。

    /// <summary>事件电池行的**第一列前缀** —— 同一个基线文件里区分两套行的**唯一**判据。</summary>
    public const string ECPrefix = "E|";

    /// <summary>这一行是不是事件电池行（判据 = 第一列以 <see cref="ECPrefix"/> 开头）。</summary>
    static bool IsEcRow(string row) { return row != null && row.StartsWith(ECPrefix, StringComparison.Ordinal); }

    /// <summary>事件电池行的**卡 id**（去掉前缀）；非电池行返回 null。</summary>
    static string EcIdOf(string row)
    {
        if (!IsEcRow(row)) return null;
        int p = row.IndexOf('\t');
        return row.Substring(ECPrefix.Length, (p < 0 ? row.Length : p) - ECPrefix.Length);
    }

    /// <summary>事件电池行的**列名**（与 <see cref="FingerprintERow"/> **同序**，逐字对应 ⇒ 差异能指名到列）。</summary>
    static readonly string[] PfECols = new[]
    {
        "id", "type", "bucket", "cand", "playRc", "played", "ctrlPlayed", "steps", "raw", "ctrl",
        "net", "netDmg", "note", "netFull",
    };

    /// <summary>一张卡的**事件电池指纹行**（`A1431` 第三笔）。列见 <see cref="PfECols"/>。
    /// 🔴 **含**最后一列 `netFull` = 净差**逐条全文**（缺了它「条数一样而内容变了」照不到）。
    /// ⛔ **不含**引擎日志（措辞会变 ⇒ 会天天红）。⚠️ `note` 是**口径散文**，但它今天只有
    /// 「对照局没起步」那一种取值（= 出声标记），留着它比丢了强 —— 一旦出现，就是要人看的。</summary>
    static string FingerprintERow(CardDef c, EventReading r)
    {
        return string.Join("\t", new[]
        {
            ECPrefix + c.Id, c.Type, r.Bucket, r.Cand ? "1" : "0", r.PlayRc.ToString(),
            r.Played ? "1" : "0", r.CtrlPlayed ? "1" : "0", One(r.Steps),
            r.Raw.ToString(), r.Ctrl.ToString(), r.Net.Count.ToString(),
            r.NetDmg.ToString(), One(r.Note),
            One(string.Join(";", r.Net)),
        });
    }

    /// <summary>指纹行里**按列名取值**（⭐ 别按下标 —— 列一多，下标就会错位；
    /// 列名找不到 / 行不是那套列 ⇒ 返回 `""`，⛔ 不猜）。</summary>
    static string ColOf(string row, string[] cols, string name)
    {
        int c = Array.IndexOf(cols, name);
        if (c < 0) return "";
        var f = row.Split('\t');
        return c < f.Length ? f[c] : "";
    }

    /// <summary>跑一遍全池，出**两套指纹行**：主档 ＋ 事件电池（`play check` 用）。
    /// 🔴 **必须写在同一个循环里** —— 电池那一档的 `cand` 列要**主档那一份 `Reading`**
    ///   （见 <see cref="EventReading.Cand"/>）；分两个循环各算一遍不但慢一倍，
    ///   两套行还会与同一份读数**对不上**（一个写盘、一个在 `Check` 里现算 ⇒ 天天红）。
    /// ⚠️ 与 `Sweep` 的循环**同形**（那一份要不要 TSV/摘要，所以没合并）—— 改一处要改两处。</summary>
    static void BothFingerprintRows(List<CardDef> pool, out List<string> main, out List<string> ec)
    {
        main = new List<string>();
        ec = new List<string>();
        foreach (var c in pool)
        {
            var r = Measure(pool, c);
            r.Bucket = Bucket(c);
            main.Add(FingerprintRow(c, r));
            if (c != null && c.Type == "unit")
            {
                var er = MeasureEvents(pool, c);
                er.Cand = r.Played && r.FaceHasOps && r.Other == 0;
                ec.Add(FingerprintERow(c, er));
            }
        }
        main.Sort(StringComparer.Ordinal);
        ec.Sort(StringComparer.Ordinal);
    }

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

    /// <summary>写盘用的文本（头几行是 `#` 注释 —— 读的时候**整行丢掉**，⛔ 不参与 md5）。
    /// 🆕 `A1431` 第三笔：**两套行**（主档 ＋ 事件电池 `E|` 前缀那一套）写进同一个文件。</summary>
    static string FingerprintFileText(IEnumerable<string> rows, IEnumerable<string> ecRows = null)
    {
        var sb = new StringBuilder();
        sb.Append("# ruleprobe `play` 结算指纹（`A1431` 第二笔）—— 一行一张卡；`play all pf <out>` 生成，`play check` 拿它逐卡对拍。\n");
        sb.Append("# 列（TAB 分隔）：" + string.Join("/", PfCols) + "\n");
        sb.Append("# 🔴 本文件里非 `#` 行的 md5 必须等于源码常量 `PlayProbe.PinnedPlayMd5` —— 重生成它抹不掉那条哨兵。\n");
        sb.Append("# ⛔ 本文件是【被人确认过的状态】的快照；为了把红抹平而重跑 `play all pf` = 把洞开回来。\n");
        sb.Append("#\n# 🆕 `A1431` 第三笔：下面第二段是【事件电池】的行（第一列带 `E|` 前缀，只覆盖 type=unit）。\n");
        sb.Append("# 列（TAB 分隔）：" + string.Join("/", PfECols) + "\n");
        sb.Append("# 它量的是「打出去**之后**，替它把别的时机的事件走一遍，局面动没动」（治「档 B」那一档的失明）。\n");
        sb.Append("# --- 主档（打出去那一拍）---\n");
        foreach (var r in rows.Where(x => !string.IsNullOrEmpty(x)).OrderBy(x => x, StringComparer.Ordinal))
            sb.Append(r).Append('\n');
        if (ecRows != null)
        {
            sb.Append("# --- 事件电池（打出去之后那一段）---\n");
            foreach (var r in ecRows.Where(x => !string.IsNullOrEmpty(x)).OrderBy(x => x, StringComparer.Ordinal))
                sb.Append(r).Append('\n');
        }
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

        List<string> live, liveE;
        BothFingerprintRows(pool, out live, out liveE);   // 主档 ＋ 事件电池（一个循环里算，见该函数）
        var baseRowsRaw = LoadFingerprintRows(baselinePath);
        // 🔴 两套行**分家对拍**（判据 = 第一列的 `E|` 前缀，见 `IsEcRow`）——
        //    混成一份会把「主档少了一张」误报成「电池多了一张」。⛔ 不靠行序/行数。
        var baseRows = baseRowsRaw.Where(x => !IsEcRow(x)).ToList();
        var baseE = baseRowsRaw.Where(IsEcRow).ToList();
        string liveMd5 = FingerprintMd5(live.Concat(liveE)), baseMd5 = FingerprintMd5(baseRowsRaw);

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

        // ---- 🆕 事件电池那一套（同一套对拍口径，只是换了一份行）----
        var ecBaseMap = new Dictionary<string, string>();
        foreach (var r in baseE) ecBaseMap[IdOf(r)] = r;
        var ecChanged = new List<string[]>();
        int ecAdded = 0, ecRemoved = 0;
        var ecAddedIds = new List<string>();
        foreach (var r in liveE)
        {
            var id = IdOf(r);
            if (!ecBaseMap.TryGetValue(id, out var b)) { ecAdded++; if (ecAddedIds.Count < 20) ecAddedIds.Add(id); continue; }
            if (!string.Equals(b, r, StringComparison.Ordinal)) ecChanged.Add(new[] { id, b, r });
        }
        var liveEIds = new HashSet<string>(liveE.Select(IdOf), StringComparer.Ordinal);
        foreach (var kv in ecBaseMap) if (!liveEIds.Contains(kv.Key)) ecRemoved++;

        Console.WriteLine("=== play check · 结算层对拍（不跑 Unity）· 池 " + pool.Count + " 张 ===");
        Console.WriteLine("  基线 " + Path.GetFileName(baselinePath) + "（主档 " + baseRows.Count
                          + " 行 ＋ 事件电池 " + baseE.Count + " 行）· 当次现算 主档 " + live.Count
                          + " ＋ 电池 " + liveE.Count);
        Console.WriteLine("  结算指纹：当次现算 = **" + liveMd5 + "** · 基线文件 = " + baseMd5
                          + (liveMd5 == baseMd5 ? "（一致）" : "（**不同**）"));
        Console.WriteLine();
        Console.WriteLine("  --- ①-a 逐卡差异（主档 · 打出去那一拍）---");
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

        // ------------------------------------------------------------------
        //  🆕 `A1431` 第三笔：事件电池那一套的对拍 ＋ **覆盖面**
        // ------------------------------------------------------------------
        Console.WriteLine();
        Console.WriteLine("  --- ①-b 逐卡差异（**事件电池** · 打出去**之后**那一段）---");
        if (ecChanged.Count == 0 && ecAdded == 0 && ecRemoved == 0)
            Console.WriteLine("     ✅ 没有一张卡与基线不同（" + liveE.Count + " 张逐字节一致）");
        else
        {
            Console.WriteLine("     🔴 **" + ecChanged.Count + " 张卡变了** · 新增 " + ecAdded + " · 消失 " + ecRemoved
                              + (ecAdded > 0 ? "（新增：" + string.Join(" · ", ecAddedIds) + "）" : ""));
            foreach (var t in ecChanged.Take(60))
            {
                var a = t[1].Split('\t'); var b = t[2].Split('\t');
                Console.WriteLine("       " + Pad(t[0].Substring(ECPrefix.Length), 24)
                                  + "（" + Math.Max(a.Length, b.Length) + " 列对拍）");
                int n = Math.Max(a.Length, b.Length);
                int shown = 0;
                for (int k = 0; k < n; k++)
                {
                    string av = k < a.Length ? a[k] : "";
                    string bv = k < b.Length ? b[k] : "";
                    if (string.Equals(av, bv, StringComparison.Ordinal)) continue;
                    if (PfECols.Length == n) Console.Write("         · " + Pad(PfECols[k], 14));
                    else Console.Write("         · 第 " + k + " 列            ");
                    Console.WriteLine(Clip(av, 160) + "  →  " + Clip(bv, 160));
                    if (++shown >= 8) break;
                }
            }
            if (ecChanged.Count > 60) Console.WriteLine("       …（还有 " + (ecChanged.Count - 60) + " 张）");
        }

        // ---- 🔴 覆盖面：**这一档到底被验到了多少**（⛔ 不许只印一句「已知不适用」就完事）----
        //   ⚠️ 统计的口径 = **主档里的候选**（`cand=1`：卡面有正文 · 打出去了 · 主档实得 0 条）
        //      —— 那一批才是「主档看着它、却看不见」的卡。⛔ 别拿整个 `unit` 档当分母：
        //      里面一半是**卡面根本没正文**的卡，它们 `other=0` 与失明无关，混进去就是灌水。
        var candsE = liveE.Where(r => ColOf(r, PfECols, "cand") == "1").ToList();
        int ecNetPos = candsE.Count(r => ColOf(r, PfECols, "net") != "0" && ColOf(r, PfECols, "net") != "");
        Console.WriteLine();
        Console.WriteLine("  --- ①-c 🔴 事件电池覆盖了谁（`A1431` 第三笔 · **这一档真被验到了几张**）---");
        Console.WriteLine("     分母 = 主档里的**候选**（卡面有正文 · 打出去了 · 主档实得 0 条）= **"
                          + candsE.Count + " 张**（电池行总数 " + liveE.Count + " 张 = `unit` 全档）");
        Console.WriteLine("     其中**电池净差 > 0 的 = " + ecNetPos + " 张**（= 它的正文**真的被那个事件抬起来过**）· "
                          + "净差 = 0 的 = **" + (candsE.Count - ecNetPos) + " 张**");
        Console.WriteLine("     ⚠️ 净差 = 0 的那几张，**主档那一拍看不见、电池这一趟也没抬起来**"
                          + " ⇒ 它们**仍然只被「读数的存在」看着、没被「事件的结算」验过**（逐条列在下面）。");
        var zeroIds = candsE.Where(r => ColOf(r, PfECols, "net") == "0")
                            .Select(r => IdOf(r).Substring(ECPrefix.Length)).ToList();
        for (int i = 0; i < zeroIds.Count; i += 8)
            Console.WriteLine("       " + string.Join(" · ", zeroIds.Skip(i).Take(8)));
        if (zeroIds.Count == 0) Console.WriteLine("       ✅ 一张都没有 —— 主档那份候选**全部**被电池抬起来过");
        if (zeroIds.Count > 0)
        {
            // 按**根因**分（判据 = 引擎自己的公开访问器，见 `EcCause`）——
            // 卡池里按 id 找回来；找不到的（池被换过）单独记一格，⛔ 不当成「没原因」。
            var byId = new Dictionary<string, CardDef>(StringComparer.Ordinal);
            foreach (var c in pool) if (c != null && c.Id != null) byId[c.Id] = c;
            var groups = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (var id in zeroIds)
            {
                string k = byId.TryGetValue(id, out var cc) ? EcCause(cc) : "卡池里查不到这个 id";
                if (!groups.TryGetValue(k, out var l)) groups[k] = l = new List<string>();
                l.Add(id);
            }
            Console.WriteLine("       ⛔ **按根因分**（判据 = 引擎自己的 `CardDef.SpiritOps` / `FaithOps`）：");
            foreach (var kv in groups.OrderByDescending(x => x.Value.Count))
                Console.WriteLine("         · " + Pad(kv.Key, 30) + " = " + kv.Value.Count + " 张");
            Console.WriteLine("       🔴 前两族的根因**不在电池**（夹具起始派系资源 = 0 ⇒ 主档那一拍就结算不了），"
                              + "要治得动 `BuildFixture` 的起始资源 ⇒ **基线整体重钉** ⇒ 那是另一轮（见 `EcCause`）。");
        }
        int ecNote = liveE.Count(r => ColOf(r, PfECols, "note").Length > 0);
        Console.WriteLine("     行内出声（`note` 非空 = 对照局没起步） = **" + ecNote + " 张**"
                          + (ecNote > 0 ? "  ← 🔴 这几张的净差**不可信**" : "（✅ 没有）"));

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
        Console.WriteLine("  判据：①-a 主档逐卡全等 且 ①-b 电池逐卡全等 且 ② 哨兵过 ⇒ rc=0；否则 rc=1。");
        Console.WriteLine("  🔴 `A1431` 第三笔（2026-10-13）**就地订正**：本行原来写着「`档 B` 那 274 张"
                          + "「等事件」的卡在这里**照样看不见**」—— **现在不成立了**："
                          + "事件电池（①-b）就是替那一档把**别的时机**的事件走一遍（见本文件 `RunEventPass`）。");
        Console.WriteLine("  ⚠️ **仍如实标着的边界**：①-a 只看得见「打出去那一拍」；**电池只覆盖 `type=unit`**"
                          + "（`tactic`/`defence` 的正文打出即结算，主档已经看得到）；"
                          + "电池抬不到的那个事件（若 `steps` 里对应那一步不是 `rc=0`）**那一张就没被验到** ——"
                          + "逐张的 `steps` 列就在基线文件里，⛔ 别只看「绿」就以为全档都验过了。");
        bool ecClean = ecChanged.Count == 0 && ecAdded == 0 && ecRemoved == 0;
        return (changed.Count == 0 && added == 0 && removed == 0 && ecClean && sentFail == 0) ? 0 : 1;
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
