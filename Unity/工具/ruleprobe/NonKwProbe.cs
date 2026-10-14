using System;
using System.Collections.Generic;
using System.Linq;
using RuleEngine;

/// <summary>
/// `nonkw` —— **`non &lt;关键词&gt;` 的目标筛选**专用探针（台账 `A1462`，2026-10-11 第二十会话）。
///
/// ================================================================
///  这条账要回答什么
/// ================================================================
/// 账文（`资料/任务_卡组卡牌战斗.md` 的 `A1462`）：
/// 「**`non &lt;关键词&gt;` 的目标筛选【疑似整个丢了】**（第十四会话 `A1441` 诊断代理查出 · ⛔ 未坐实）
///    —— `GOF86` 及同形 `Hunting Wolf` / `Fenrisian Wolf`；**全仓 0 处** `non &lt;关键词&gt;` 的目标筛选
///    （`EffectTargetSpec.Raw` **只进日志**、不参与筛选）。」
///
/// ⇒ 本子命令把「**丢没丢 · 丢在哪一层 · 真打会怎样**」逐条量出来（⛔ 只报，不改引擎）。
///
/// ================================================================
///  判据（三层，全部是**外部的**，⛔ 不拿我们自己的实现反填）
/// ================================================================
/// ① **卡面原文**（铁律 7：最终真相 = PnP 成品卡图）—— 本轮**逐张开图亲读**过三张：
///    · `d:/2/Warpforge部队卡片/Orks/3部队/Warpforge_11_Unleashed-Tramplasquig.png`
///      = `〔Stomp〕 Stomp. At the start of each turn attack a random non 〔Flying〕 Flying enemy`
///    · `d:/2/Warpforge部队卡片/Space Wolves/3部队/Warpforge_12_Hunting-Wolf.png`
///      = `〔Ferocity〕 Ferocity: Attack a random non 〔Flying〕 Flying enemy troop`
///    · `d:/2/Warpforge部队卡片/Space Wolves/3部队/Warpforge_16_Fenrisian-Wolf.png`
///      = `Tide 1. Ferocity: Attack a non 〔Flying〕 Flying enemy troop. If it has a Hunt Mark, gain +1〔粉拳〕`
/// ② **原版反编译**（`d:/2/tools/decomp_full/`）—— 原版**确实有**这个筛选的取反开关：
///    `d:/2/Warpforge_code/Scripts/Assembly-CSharp/TargetCriteria.cs`：
///      · `public DefinedTrait traitsFilter;` ＋ `[Tooltip("False => Filter units with Trait; True => without trait")] public bool excludeTrait;`
///      · `public Keyword keywordFilter;` ＋ `public bool excludeRace;`
///    消费点 `d:/2/tools/decomp_full/FilterMethods__MeetsTraitCriteria.c`：
///      `bVar2 = (param_3 ^ 1) & bVar2;`（`param_3` = `excludeTrait`）⇒ **取反是真的会生效的那一位**。
///    ⚠️ 原版里 `Flying` 是**trait**（`RuleCore.cs:3310` 引 `definesTrait.flying = 390`），
///      我们这一侧是**关键词**（`KeywordTable.Flying`）—— 表示法不同、语义同一个。
/// ③ **我们这一侧承载排除的字段**：`EffectTargetSpec.NotKeyword`（唯一一处），
///    消费点 = `EffectResolver.ApplyTargetFilters`（`EffectResolver.cs:1356-1361`：
///    `pool.RemoveAll(u => … u.Has(spec.NotKeyword))`）。
///    ⚠️ 它与**原版**的对应关系：`NotKeyword` ≡ `traitsFilter + excludeTrait=true`。
///    ⇒ **判据 = 「卡面写了 `non X` ⇒ 规格的 `NotKeyword` 必须是 `x`」**，其余字段都不承载「排除」。
///
/// ⚠️ 本探针**不判「该不该用 `KeywordFilter` 取反」这类实现口味** —— 它只量
///    「**卡面那个词有没有落到某个字段上**」，以及「落不上的时候，真打会不会有可见差别」。
/// </summary>
static class NonKwProbe
{
    // ==================================================================
    //  结果模型
    // ==================================================================

    /// <summary>一处「目标短语里写了 `non X`」。`Path` = 它挂在哪条 op 的哪一栏（能直接指回源码）。</summary>
    public class Hit
    {
        public CardDef Card;
        public string Path;          // 例：`Target2` / `AtTurnOps[0].Target2`
        public string Verb;
        public EffectTargetSpec Spec;
        public string Word;          // 卡面 `non X` 里的那个 `X`（已小写）
        /// <summary>规格里**有没有**承载这个排除（唯一承载处 = `NotKeyword`）。</summary>
        public bool Carried { get { return Spec != null && !string.IsNullOrEmpty(Spec.NotKeyword); } }
        /// <summary>承载在哪个字段（`NotKeyword` / `-`）—— 报告里要指名。</summary>
        public string CarryAt
        {
            get
            {
                if (Spec == null) return "（没有规格）";
                if (!string.IsNullOrEmpty(Spec.NotKeyword)) return "NotKeyword=\"" + Spec.NotKeyword + "\"";
                return "-";
            }
        }
    }

    /// <summary>一处「卡面里有 `non X`，但**不在**任何目标短语里」—— 分两类如实计入（⛔ 不混进上面）。</summary>
    public class Elsewhere
    {
        public CardDef Card;
        public string Where;     // `ChooseWhat`（选择筛选） / `卡面其它处`（未归因）
        public string Text;      // 那一处的原文
    }

    public class Result
    {
        public List<Hit> Hits = new List<Hit>();
        public List<Elsewhere> Others = new List<Elsewhere>();
        public int Scanned;
        /// <summary>夹具的现场说明（`BuildPair` 每次都写 —— 「没摆上」时要能看出为什么，⛔ 不静默）。</summary>
        public string Diag;
        /// <summary>目标短语里写了 `non X` 而**规格没承载**的处数（= `A1462` 要坐实的那个数）。</summary>
        public int Lost { get { return Hits.Count(h => !h.Carried); } }
    }

    /// <summary>最近一次 <see cref="BuildPair"/> 的现场（`P1 手 [..] · rc.. · P1 棋盘 ..`）。</summary>
    public static string Diag;

    // ==================================================================
    //  ① 全池普查
    // ==================================================================

    /// <summary>扫全池：卡面（`desc` ＋ `keywords` 原文）里的 `non &lt;词&gt;` 落在哪一栏，
    /// 以及目标规格有没有承载它。
    /// ⚠️ `rawKeywords` 与 `pool` **同序**（来自 `Program.PoolRawKeywords`，由 `LoadPool` 建）——
    ///    带正文的触发关键词在卡表里是**整条字符串**（`"Sacrifice: Gain +1 Attack"`），
    ///    而 `CardDef.Keywords` 只有规范化之后"的键 ⇒ 要看原文就得靠这一份。</summary>
    public static Result Measure(List<CardDef> pool, List<List<string>> rawKeywords = null)
    {
        var r = new Result();
        if (pool == null) return r;
        var seen = new HashSet<string>();      // `卡|路径|词` ⇒ 同一处只记一次（`desc` 与 `keywords` 常常是同一段正文）
        for (int ci = 0; ci < pool.Count; ci++)
        {
            var c = pool[ci];
            if (c == null) continue;
            r.Scanned++;

            var ops = EffectText.Parse(c.Desc, out _, out _, false);
            if (ops != null) Walk(c, ops, "", r, seen);

            var raw = rawKeywords != null && ci < rawKeywords.Count ? rawKeywords[ci] : null;
            if (raw == null) continue;
            foreach (var item in raw)
            {
                if (string.IsNullOrEmpty(item)) continue;
                string w;
                if (!TryNonPhrase(item, out w)) continue;
                var ops2 = EffectText.Parse(item, out _, out _, false);
                if (ops2 != null) Walk(c, ops2, "[keywords]", r, seen);
            }
        }
        return r;
    }

    /// <summary>递归走 op 树。`path` 逐层拼（`AtTurnOps[0].Target2` 这种）。</summary>
    static void Walk(CardDef c, List<EffectOp> ops, string path, Result r, HashSet<string> seen)
    {
        for (int i = 0; i < ops.Count; i++)
        {
            var op = ops[i];
            if (op == null) continue;
            string p = path + (path.Length > 0 ? "." : "") + (ops.Count > 1 ? "[" + i + "]" : "");
            Note(c, p + "Target", op.Verb, op.Target, r, seen);
            Note(c, p + "Target2", op.Verb, op.Target2, r, seen);
            Note(c, p + "DeathWatchTarget", op.Verb, op.DeathWatchTarget, r, seen);

            // 选择筛选（`Choose a non-Legendary …`）—— **不是目标规格**，单列
            if (!string.IsNullOrEmpty(op.ChooseWhat) && HasNon(op.ChooseWhat))
                r.Others.Add(new Elsewhere { Card = c, Where = "ChooseWhat", Text = op.ChooseWhat });

            if (op.AtTurnOps != null) Walk(c, op.AtTurnOps, p + "AtTurnOps", r, seen);
            if (op.RepeatOps != null) Walk(c, op.RepeatOps, p + "RepeatOps", r, seen);
            if (op.BaseOps != null) Walk(c, op.BaseOps, p + "BaseOps", r, seen);
        }
    }

    static void Note(CardDef c, string path, string verb, EffectTargetSpec spec, Result r, HashSet<string> seen)
    {
        if (spec == null) return;
        string w;
        if (!TryNonPhrase(spec.Raw, out w)) return;
        string key = c.Id + "|" + path + "|" + w;
        if (!seen.Add(key)) return;
        r.Hits.Add(new Hit { Card = c, Path = path, Verb = verb, Spec = spec, Word = w });
    }

    /// <summary>卡面里那处 `non &lt;词&gt;` 的词（小写）：**两种写法都要认** ——
    /// 卡面印的是 `non Flying`（空格），而卡表里也有 `non-Flying`（连字符）的写法
    /// （实测 `card_stats.json` 的 `Fenrisian Wolf`）。⛔ 只认一种就会漏。</summary>
    static bool TryNonPhrase(string text, out string word)
    {
        word = null;
        if (string.IsNullOrEmpty(text)) return false;
        string low = text.ToLowerInvariant();
        for (int i = 0; i + 4 <= low.Length; i++)
        {
            if (low[i] != 'n' || low[i + 1] != 'o' || low[i + 2] != 'n') continue;
            if (i > 0 && (char.IsLetterOrDigit(low[i - 1]))) continue;      // 词首才算（别把 `anon` 认进来）
            char sep = i + 3 < low.Length ? low[i + 3] : ' ';
            if (sep != ' ' && sep != '-') continue;
            string rest = low.Substring(i + 4).TrimStart();
            int sp = rest.IndexOf(' ');
            string w = sp < 0 ? rest : rest.Substring(0, sp);
            w = w.Trim().Trim('.', ',', ';', ':', '"', '\'', ')');
            if (w.Length == 0) continue;
            word = w;
            return true;
        }
        return false;
    }

    static bool HasNon(string s) { string w; return TryNonPhrase(s, out w); }

    // ==================================================================
    //  ③ 行为对照用的小夹具
    // ==================================================================

    /// <summary>夹具单位：`id` 兼卡名，0 攻（⛔ 不让它反击打死谁，读数才干净）。</summary>
    static CardDef Fix(string id, int hp, string subtype, params string[] keywords) =>
        new CardDef(id, id, "unit", "", null, "Fixture", 1, 0, hp, 0,
                    keywords == null ? null : keywords.ToList(), subtype: subtype);

    static CardDef HeroOf(string id) =>
        new CardDef(id, id, "hero", "", null, "Fixture", 0, 2, 30, 0, null, subtype: "Warlord");

    /// <summary>建一局：P0 = 被测卡在手上（先手打出），P1 = 槽 3 一个**带 `X` 关键词**的单位、
    /// 槽 6 一个**不带**的单位（两边的 `X`/非 `X` 都由调用方给）。</summary>
    static BattleContext BuildPair(List<CardDef> pool, CardDef target, CardDef withKw, CardDef withoutKw,
                                   out UnitState kwUnit, out UnitState plainUnit, out UnitState self)
    {
        var d0 = new List<CardDef> { HeroOf("FIXWarlord_P0") };
        for (int i = 0; i < 20; i++) d0.Add(Fix("filler" + i, 1, "Infantry"));
        d0.Add(target);                                          // 末尾 = 牌库顶 ⇒ 起手抽到
        var d1 = new List<CardDef> { HeroOf("FIXWarlord_P1") };
        for (int i = 0; i < 20; i++) d1.Add(Fix("fillerB" + i, 1, "Infantry"));
        d1.Add(withKw); d1.Add(withoutKw);                       // 倒着加 ⇒ `withoutKw` 先上手

        var ctx = RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, cardPool: pool);
        ctx.Players[0].Energy = 20; ctx.Players[0].MaxEnergy = 30;
        ctx.Players[1].Energy = 20; ctx.Players[1].MaxEnergy = 30;

        // P0 回合：把被测卡打出去（单位卡落场 / 战术卡结算）
        // ⚠️ **落点不能用 4** —— 那是 `BoardSpec.WarlordSlot`（督军格）
        int hi = Find(ctx, 0, target);
        int slot = 2;
        if (target.IsUnit) RuleCore.PlayCard(ctx, 0, hi, slot);
        else RuleCore.PlayTactic(ctx, 0, hi, -1);
        self = OnBoard(ctx, 0, target);
        RuleCore.EndTurn(ctx); RuleCore.BeginTurn(ctx);          // → P1

        // P1 回合：两张对照单位都打出去
        // 🔴 **槽号不能当身份**：本仓的棋盘是「**连续无洞**」模型
        //    （放牌 = 插入推人，见 `RuleCore.MinionManager` 那一族）⇒ 请求的格位**不等于**
        //    最终落格（实测：请求槽 6、落在槽 5）。⚠️ 所以下面按 **卡模板**回找，⛔ 不按下标取。
        ctx.Players[1].Energy = 20;
        int a = Find(ctx, 1, withKw);
        int b = Find(ctx, 1, withoutKw);
        int rcA = RuleCore.PlayCard(ctx, 1, a, 3);
        int rcB = RuleCore.PlayCard(ctx, 1, b, 6);
        kwUnit = OnBoard(ctx, 1, withKw);
        plainUnit = OnBoard(ctx, 1, withoutKw);
        // ⚠️ 夹具没摆上要**出声**（带 rc 与两边棋盘），⛔ 不许静默返回 null 让调用方猜
        Diag = "P1 手 [" + string.Join(",", ctx.Players[1].Hand.Select(h => h.Card.Id)) + "]"
             + " · rc(" + withKw.Id + "→槽3)=" + rcA + " · rc(" + withoutKw.Id + "→槽6)=" + rcB
             + " · P1 棋盘 " + string.Join("|", Enumerable.Range(0, BoardSpec.Size)
                                        .Select(s => s + ":" + (ctx.Players[1].Board[s] == null
                                                    ? "." : ctx.Players[1].Board[s].Name)));
        return ctx;
    }

    /// <summary>按**卡模板**在某一方棋盘上回找一个单位（⛔ 不按槽号 —— 见 `BuildPair` 里的说明）。</summary>
    static UnitState OnBoard(BattleContext ctx, int p, CardDef c)
    {
        for (int s = 0; s < BoardSpec.Size; s++)
        {
            var u = ctx.Players[p].Board[s];
            if (u != null && u.Card == c) return u;
        }
        return null;
    }

    /// <summary>单位在棋盘上的**实际**格位（找不到返回 -1）—— 读数里要打真值，⛔ 别打请求值。</summary>
    static int SlotOf(BattleContext ctx, int p, UnitState u)
    {
        for (int s = 0; s < BoardSpec.Size; s++) if (ReferenceEquals(ctx.Players[p].Board[s], u)) return s;
        return -1;
    }

    static int Find(BattleContext ctx, int p, CardDef c)
    {
        var h = ctx.Players[p].Hand;
        for (int i = 0; i < h.Count; i++) if (h[i].Card == c) return i;
        return -1;
    }

    // ==================================================================
    //  入口
    // ==================================================================

    public static int Run(List<CardDef> pool, List<List<string>> rawKeywords = null)
    {
        var r = Measure(pool, rawKeywords);
        Console.WriteLine("=== nonkw · `non <关键词>` 的目标筛选（`A1462`）· 池 " + pool.Count + " 张 ===");
        Console.WriteLine();
        Console.WriteLine("--- ① 卡面普查（扫了 " + r.Scanned + " 张 · 目标短语里写了 `non X` 的 "
                          + r.Hits.Count + " 处 · 其中规格**没承载**的 " + r.Lost + " 处）---");
        foreach (var h in r.Hits)
        {
            Console.WriteLine("  " + (h.Carried ? "✓ " : "🔴 ") + Pad(h.Card.Id, 10) + Pad(h.Card.Name, 26)
                              + "op=" + Pad(h.Verb, 12) + "在 " + Pad(h.Path, 26)
                              + "卡面写 `non " + h.Word + "` ⇒ 承载处 = " + h.CarryAt);
            Console.WriteLine("        Raw = " + h.Spec.Raw
                              + "\n        规格其它相关字段: Side=" + D(h.Spec.Side) + " Kind=" + D(h.Spec.Kind)
                              + " Count=" + h.Spec.Count + " Random=" + (h.Spec.Random ? 1 : 0)
                              + " KeywordFilter=" + D(h.Spec.KeywordFilter) + " NotKeyword=" + D(h.Spec.NotKeyword));
        }
        if (r.Others.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("  ── 卡面里还有 " + r.Others.Count + " 处 `non X` **不在目标短语里**（如实列出，⛔ 不混进上面那一栏）──");
            foreach (var o in r.Others)
                Console.WriteLine("     " + Pad(o.Card.Id, 10) + Pad(o.Card.Name, 26) + o.Where + " = `" + o.Text + "`");
        }

        // ------------------------------------------------------------------
        Console.WriteLine();
        Console.WriteLine("--- ② 判据与承载字段 ---");
        Console.WriteLine("  卡面（PnP 成品卡图亲读）：`… attack a random non 〔Flying〕 Flying enemy` ——"
                          + "「non Flying」是一个**排除条件**（打谁要排除飞行单位）。");
        Console.WriteLine("  原版（`TargetCriteria.cs`）：`traitsFilter` ＋ `excludeTrait`（`True => without trait`）"
                          + "，消费点 `FilterMethods__MeetsTraitCriteria.c` 的 `bVar2 = (param_3 ^ 1) & bVar2`。");
        Console.WriteLine("  我们这一侧**唯一**承载「排除」的字段 = `EffectTargetSpec.NotKeyword`"
                          + "（消费点 `EffectResolver.ApplyTargetFilters`）。");
        Console.WriteLine("  ⇒ 判据：卡面写了 `non X` ⇒ 那个规格的 `NotKeyword` 必须是 `x`。");
        Console.WriteLine("  ⚠️ **`non-` 在别处【有】实现**（⛔ 别把它当成「全仓没有」）："
                          + "**卡筛选**那一路 `CreatePool.FilterChoose` 认死两个前缀 "
                          + "`non-legendary` / `non-ephemeral`（`CreatePool.cs:303/305`）——"
                          + "上面 §① 单列的那 4 处 `Choose a non-Legendary …` 走的就是它（`B19Test` 有断言）。"
                          + "缺的只有**目标短语**这一路（`ParseTarget`）。");

        // ------------------------------------------------------------------
        Console.WriteLine();
        Console.WriteLine("--- ③ 行为对照（真打 · 引擎自己的两条路）---");
        Behavior(pool, r);

        Console.WriteLine();
        Console.WriteLine("--- ④ 结论 ---");
        if (r.Lost == 0)
        {
            Console.WriteLine("  ① 目标短语里没发现「丢了排除」的情形 ⇒ `A1462` 可以销。");
            return 0;
        }
        Console.WriteLine("  🔴 **丢了，共 " + r.Lost + " 处**（明细见 §①）—— 丢在【解析层】：");
        Console.WriteLine("     `EffectText.ParseTarget` 里**没有** `non <词>` 这一支（它只认 `with <词>` ⇒"
                          + " `ReTargetWith` → `KeywordFilter`，见 `EffectText.cs:7771` 附近）。"
                          + "`non flying` 两个词因此**整段丢掉**：`Raw` 留着原文（`Raw` 只是排查用的字符串、"
                          + "不参与筛选），而 `Side`/`Kind`/`Count` 照常解出来 ⇒ **句子报 OK、卡面不打 `*`**（静默）。");
        Console.WriteLine("     ⚠️ **不是结算层的问题** —— `ApplyTargetFilters` 那一支（`NotKeyword`）是好的，"
                          + "§③ 的对照直接证明「把 `NotKeyword` 补上，候选里就没有飞行单位了」。");
        Console.WriteLine("     ⇒ 修法（**解析层**，`RuleEngine/Core/EffectText.cs` 的 `ParseTarget`）："
                          + "在 `ReTargetWith` 那一段前面加一支认 `non <词>` ⇒ 写进 `spec.NotKeyword`。"
                          + "⛔ 本探针不改引擎，本节只交读数。");
        return 1;
    }

    // ==================================================================
    //  §③ 行为对照
    // ==================================================================

    /// <summary>
    /// 两条测量（都用**真池里的真卡**，夹具另建、种子定死）：
    ///  (a) **判定层**：同一个规格走**引擎自己的** `ApplyTargetFilters`（经 `EffectResolver.ResolveTargets`）——
    ///      摆一个「带 X 的敌人 + 不带 X 的敌人」，比「解析出来的规格」与「只把 `NotKeyword` 补上的规格」
    ///      各筛出谁来。⚠️ 枚举池子时把 `Count` 临时置 0（= `all`）—— **只动这一个字段**，如实标明。
    ///  (b) **真路径**：`GOF86` 在场上跑真实触发（`EndTurn`/`BeginTurn` ⇒ `ResolveAtTurn("turn_start")`），
    ///      用不同的随机数预消耗抽样若干次 ⇒ 数「打出去了 / 空过」各几次。
    ///      空过 = `DoForceAttack` 的 `TryAttackOnce` 只拿到**被随机挑中的那一个**候选，
    ///      而它是飞行单位（近战打不到）⇒ 那一发**什么都没打**（引擎自己会打日志）。
    /// </summary>
    static void Behavior(List<CardDef> pool, Result r)
    {
        // --- (a) 判定层：所有「卡面写了 non X」的规格逐条比 ---
        Console.WriteLine("  (a) 判定层：候选池对照（摆场：槽 3 = 带 X 的单位、槽 6 = 不带 X 的单位，都是 0 攻 6 血）");
        foreach (var h in r.Hits)
        {
            string kw = h.Word;                 // 本池这几处都是 `non Flying` ⇒ X = flying
            var withKw = Fix("FIX_" + kw.ToUpperInvariant(), 6, "Infantry", kw);
            var plain = Fix("FIX_PLAIN", 6, "Infantry");
            var target = h.Card;
            // 夹具要求：被测卡得能落场 / 打出去（`GOF86`/`SW12` 都是单位卡）
            UnitState kwU, plU, selfU;
            BattleContext ctx;
            try
            {
                ctx = BuildPair(pool, target, withKw, plain, out kwU, out plU, out selfU);
            }
            catch (Exception ex)
            {
                Console.WriteLine("     " + h.Card.Id + " —— **夹具建不起来**（" + ex.GetType().Name
                                  + ": " + ex.Message + "）⇒ 这一条 (a) 判不了（如实说，⛔ 不静默）");
                continue;
            }
            if (kwU == null || plU == null)
            {
                Console.WriteLine("     " + h.Card.Id + " —— 夹具没把两个对照单位摆上（槽 3="
                                  + (kwU == null ? "空" : kwU.Name) + " · 槽 6=" + (plU == null ? "空" : plU.Name)
                                  + "）⇒ 判不了（如实说）");
                Console.WriteLine("        现场：" + Diag);
                continue;
            }
            var asParsed = EnumPool(ctx, h.Spec, null, h.Word);
            var withFix = EnumPool(ctx, h.Spec, h.Word, h.Word);
            Console.WriteLine("     " + Pad(h.Card.Id, 10) + h.Card.Name + " · " + h.Path + "（" + h.Verb + "）");
            Console.WriteLine("        对照单位：槽 " + SlotOf(ctx, 1, kwU) + " = " + kwU.Name
                              + "{" + h.Word + "}  ·  槽 " + SlotOf(ctx, 1, plU) + " = " + plU.Name);
            Console.WriteLine("        解析出来的规格（`Count` 临时置 0 以**枚举**池子，别的字段一个没动）"
                              + " ⇒ 候选 = [" + Names(asParsed) + "]"
                              + (asParsed.Count > 1 ? "   ← 🔴 **含飞行单位**" : ""));
            Console.WriteLine("        只把 `NotKeyword` 补成 \"" + h.Word + "\"（探针侧改副本）"
                              + " ⇒ 候选 = [" + Names(withFix) + "]"
                              + (withFix.Count == 1 && withFix[0] == plU ? "   ← ✅ 飞行单位被排除了" : ""));
        }

        // --- (b) 真路径：GOF86 的 at-turn 触发 ---
        var gof = r.Hits.Select(h => h.Card).FirstOrDefault(c => c.Id == "GOF86");
        if (gof == null)
        {
            Console.WriteLine("  (b) 真路径：卡池里没有 `GOF86` ⇒ 这一条跳过（如实说，⛔ 不静默）");
            return;
        }
        Console.WriteLine();
        Console.WriteLine("  (b) 真路径：`GOF86 Unleashed TramplaSquig`"
                          + "（卡面 `At the start of each turn attack a random non Flying enemy`）");
        // ⚠️ **两种跑法都要**：
        //   · `refresh=false` = **原样**跑 ⇒ 实测 **8/8 全落 `ErrExhausted`**
        //     （`BeginTurn` 里 `ResolveAtTurn("turn_start")` 在 `RuleCore.cs:1110`，
        //      而【解疲劳】`u.RefreshForNewTurn()` 在 `:1161` —— **排在它后面**）
        //     ⇒ 那是**另一条**缺陷（回合开始触发的强制攻击永远打不出去），
        //       **不是**本条要量的东西，如实单列。
        //   · `refresh=true` = 探针**调一次引擎自己的【解疲劳】**（`UnitState.RefreshForNewTurn`）
        //     把次序摆成原版那样（原版 `OnTurnStart` 里 `ActivateMinion` 在 `:99`、
        //     **先于**该卡自己的回合开始触发）⇒ 这样才看得到「随机挑中飞行单位 ⇒ 空过」
        //     这个**属于 `non Flying` 丢掉**的后果。
        foreach (bool refresh in new[] { false, true })
        {
            int hitPlain = 0, hitWarlord = 0, hitFly = 0, blocked = 0, other = 0;
            const int Trials = 8;
            var notes = new List<string>();
            for (int k = 0; k < Trials; k++)
            {
                var withKw = Fix("FIX_FLYING", 6, "Infantry", "flying");
                var plain = Fix("FIX_PLAIN", 6, "Infantry");
                UnitState kwU, plU, selfU;
                BattleContext ctx;
                try { ctx = BuildPair(pool, gof, withKw, plain, out kwU, out plU, out selfU); }
                catch (Exception ex) { Console.WriteLine("     （第 " + k + " 次夹具建不起来：" + ex.Message + "）"); continue; }
                if (selfU == null || kwU == null || plU == null)
                {
                    Console.WriteLine("     （第 " + k + " 次没摆齐，跳过：" + Diag + "）");
                    continue;
                }
                // 跑法②：**按引擎自己的【解疲劳】那一步**提前解一次（见上）
                //   ⚠️ **只调这一个引擎自己的方法**（⛔ 不手改 `Exhausted` 这类字段 ——
                //      实测那样会漏掉同一个方法里清的别的位，症状是返回码从 `ErrExhausted`
                //      变成 `ErrNoAttack`（`CannotAttackThisTurn` 还在）⇒ 读数被自己的干预污染）。
                if (refresh) selfU.RefreshForNewTurn();
                // 预消耗随机数 = 同一局换一个「随机挑中谁」的抽样
                for (int j = 0; j < k; j++) ctx.Rng.Next(2);
                int hBeforeKw = kwU.Health, hBeforePl = plU.Health, hBeforeWl = ctx.Players[1].Warlord.Health;
                int evBefore = ctx.Events.Count;
                RuleCore.EndTurn(ctx); RuleCore.BeginTurn(ctx);      // → P0 回合开始 ⇒ `ResolveAtTurn("turn_start")`
                int dKw = hBeforeKw - kwU.Health, dPl = hBeforePl - plU.Health;
                int dWl = hBeforeWl - ctx.Players[1].Warlord.Health;
                string tail = string.Join(" | ", ctx.Events.Skip(evBefore)
                                            .Where(x => x.Contains("攻击") || x.Contains("打不了")));
                // 分类：**「打不了」的原因要按引擎自己的话分**（⛔ 不拿「没变化」一律当空过）
                if (dKw > 0) hitFly++;
                else if (dPl > 0) hitPlain++;
                else if (dWl > 0) hitWarlord++;
                else if (tail.Contains("打不了")) blocked++;
                else other++;
                notes.Add("        k=" + k + " ⇒ 非飞行部队 -" + dPl + " · 督军 -" + dWl + " · 飞行 -" + dKw
                          + " 血" + (tail.Length > 0 ? " ｜ " + Clip(tail, 108) : " ｜（无攻击/打不了日志）"));
            }
            Console.WriteLine("      跑法 " + (refresh ? "②（探针先调一次引擎的【解疲劳】`RefreshForNewTurn`，把次序摆成原版那样）"
                                                       : "①（**原样**，不动任何状态）")
                              + " · " + Trials + " 次抽样（同一夹具、种子 0，只在触发前预消耗 k 次随机数）:");
            foreach (var n in notes) Console.WriteLine(n);
            Console.WriteLine("         ⇒ 打在**非飞行部队**上 " + hitPlain + " 次 · 打在**督军**上 " + hitWarlord
                              + " 次（督军**不是**飞行 ⇒ 卡面 `enemy` 允许）· 打在**飞行**上 " + hitFly
                              + " 次（🔴 不该发生）· 打不了 " + blocked + " 次 · 其它 " + other + " 次");
            if (refresh)
            {
                if (hitFly > 0)
                    Console.WriteLine("         🔴 有 " + hitFly + " 次**打到了飞行单位** —— `non Flying` 丢了、"
                                      + "而且这几次攻击合法（攻击者是远程或同为飞行）⇒ 真打错人。");
                if (blocked > 0)
                    Console.WriteLine("         🔴 打不了的那 " + blocked + " 次 = **随机挑中了飞行单位**："
                                      + "`TryAttackOnce` 只拿到**被挑中的那一个**候选（`Count=1` + `Random`），"
                                      + "近战打不到飞行 ⇒ **没有别的候选可退** ⇒ 这一发**什么都没打**"
                                      + "（引擎自己的话：「目标不合法（Vanguard / Stealth / Flying 限制）」），"
                                      + "而场上**明明有**可打的非飞行敌人。⇒ 这就是「丢了一个筛选」的可见后果。");
                if (hitFly == 0 && blocked == 0)
                    Console.WriteLine("         ⚠️ 这一次抽样里既没打错人也没空过 —— 别据此销账：候选里**本来就**含飞行单位"
                                      + "（§③(a)），8 个样本抽不到就看不见；**判据是 §③(a) 那一栏**。");
            }
            else
            {
                if (blocked == Trials)
                    Console.WriteLine("         🔴 **" + blocked + "/" + Trials + " 全落 `ErrExhausted`** —— 这一条是**另一条缺陷**（`A1462` 之外）："
                                      + "`BeginTurn` 的 `ResolveAtTurn(\"turn_start\")`（`RuleCore.cs:1110`）"
                                      + "**排在【解疲劳】`RefreshForNewTurn`（`:1161`）之前**"
                                      + "⇒ 卡面写着 `At the start of each turn attack …` 这一类（本行测的 `"
                                      + gof.Id + "` 就是）**永远打不出去**。"
                                      + "⛔ 它**不是** `non Flying` 的证据（本行只说明这条路量不到）。");
                else
                    Console.WriteLine("         ⚠️ 原样跑居然有 " + (Trials - blocked) + " 次打出去了 —— "
                                      + "与「`turn_start` 排在解疲劳之前」这句**不符**，去核 `RuleCore.BeginTurn` 的次序。");
            }
        }
    }

    /// <summary>枚举一个规格筛出来的池子：走**引擎自己的** `ResolveTargets`。
    /// ⚠️ `Count` 临时置 0（= `all`）只为**枚举** —— 否则 `Count/1 + Random` 只回一个元素、
    ///    看不见池子里有什么。**只动这一个字段**，`NotKeyword` 只在 `patchNot` 非空时补。</summary>
    static List<UnitState> EnumPool(BattleContext ctx, EffectTargetSpec spec, string patchNot, string word)
    {
        var probe = new EffectTargetSpec
        {
            Raw = spec.Raw, Side = spec.Side, Kind = spec.Kind,
            Count = 0,                       // ⚠️ 只动这一个（枚举用）
            Random = spec.Random,
            KeywordFilter = spec.KeywordFilter,
            NotKeyword = patchNot ?? spec.NotKeyword,
            SubtypeFilter = spec.SubtypeFilter,
            Adjacent = spec.Adjacent, Anchor = spec.Anchor, AnchorInSet = spec.AnchorInSet,
            DamagedOnly = spec.DamagedOnly, PrayedOnly = spec.PrayedOnly,
            NameFilter = spec.NameFilter, Deployed = spec.Deployed,
        };
        // ⚠️ `EffectResolver.cs` 里那个类其实是 `partial class RuleCore`
        //    （所以目标解析要从 `RuleCore` 进，没有 `EffectResolver.X` 这个名字）
        return RuleCore.ResolveTargets(ctx, 0, probe, null, null);
    }

    static string Names(List<UnitState> l) =>
        l == null || l.Count == 0 ? "（空）" : string.Join(", ", l.Select(u => u == null ? "(null)" : u.Name));

    static string D(string s) { return string.IsNullOrEmpty(s) ? "-" : s; }

    static string Pad(string s, int w)
    {
        int vis = 0;
        foreach (char ch in s ?? "") vis += ch > 0x2000 ? 2 : 1;
        var b = new System.Text.StringBuilder(s ?? "");
        while (vis < w) { b.Append(' '); vis++; }
        return b.ToString();
    }

    static string Clip(string s, int n) => string.IsNullOrEmpty(s) || s.Length <= n ? s : s.Substring(0, n) + "…";
}
